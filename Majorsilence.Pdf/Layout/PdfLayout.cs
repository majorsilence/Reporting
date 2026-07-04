// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;

namespace Majorsilence.Pdf.Layout
{
    /// <summary>
    /// A fluent, cursor-based flow layout on top of <see cref="PdfDocument"/> and
    /// <see cref="PdfCanvas"/>. Tracks a top-down Y cursor within a page's content box (page size
    /// minus margins and any reserved footer height) and automatically starts new pages when
    /// content would overflow.
    /// </summary>
    /// <example>
    /// <code>
    /// var doc = PdfDocument.Create().WithTitle("Invoice");
    /// var layout = PdfLayout.Begin(doc, PageSizes.A4)
    ///     .WithMargins(36)
    ///     .WithFooter((canvas, page) =>
    ///         canvas.DrawText($"Page {page}", 36, 806, TextStyle.Default.WithSize(9)));
    ///
    /// layout.Text("ACME Corp", TextStyle.Default.WithSize(20).WithBold())
    ///       .Spacer(12)
    ///       .Row(row =>
    ///       {
    ///           row.Column(0.6f, col => col.Text("Bill To:").Text(customerAddress));
    ///           row.Column(0.4f, col => col.Text($"Invoice #{n}",
    ///               TextStyle.Default.WithAlignment(TextAlignment.Right)));
    ///       })
    ///       .Line()
    ///       .Table(lineItemsTable)
    ///       .Text(termsAndConditions);
    ///
    /// layout.End().Save("invoice.pdf");
    /// </code>
    /// </example>
    /// <remarks>
    /// <b>v1 scope:</b> rows are atomic — a row that does not fit in the remaining space on a
    /// page moves entirely to the next page, and a row taller than a full page's content area
    /// throws <see cref="InvalidOperationException"/>. <see cref="Text"/> is the only element
    /// that splits across pages. There is no flexbox-style grow/shrink and no "keep together"
    /// grouping beyond the row boundary.
    /// </remarks>
    public sealed class PdfLayout
    {
        private readonly PdfDocument _doc;
        private readonly PdfPageSize _pageSize;

        private float _marginLeft = 36f;
        private float _marginTop = 36f;
        private float _marginRight = 36f;
        private float _marginBottom = 36f;

        private Action<PdfCanvas, int>? _footer;
        private float _footerHeight;

        private PdfCanvas? _canvas;
        private float _cursorY;
        private int _pageNumber;
        private bool _started;

        private PdfLayout(PdfDocument doc, PdfPageSize pageSize)
        {
            _doc = doc;
            _pageSize = pageSize;
        }

        /// <summary>Begin a new flow layout on <paramref name="doc"/> using <paramref name="pageSize"/> for every page.</summary>
        public static PdfLayout Begin(PdfDocument doc, PdfPageSize pageSize)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            return new PdfLayout(doc, pageSize);
        }

        private float ContentLeft => _marginLeft;
        private float ContentWidth => _pageSize.Width - _marginLeft - _marginRight;
        private float ContentTop => _marginTop;
        private float ContentBottom => _pageSize.Height - _marginBottom - _footerHeight;
        private float ContentHeight => ContentBottom - ContentTop;

        /// <summary>Set equal margins on all four sides. Must be called before the first drawing call.</summary>
        public PdfLayout WithMargins(float all) => WithMargins(all, all, all, all);

        /// <summary>Set independent margins. Must be called before the first drawing call.</summary>
        public PdfLayout WithMargins(float left, float top, float right, float bottom)
        {
            ThrowIfStarted(nameof(WithMargins));
            _marginLeft = left;
            _marginTop = top;
            _marginRight = right;
            _marginBottom = bottom;
            return this;
        }

        /// <summary>
        /// Reserve <paramref name="height"/> points at the bottom of every page and invoke
        /// <paramref name="draw"/> there, once per page, with the 1-based page number. Must be
        /// called before the first drawing call.
        /// </summary>
        public PdfLayout WithFooter(Action<PdfCanvas, int> draw, float height = 24f)
        {
            ThrowIfStarted(nameof(WithFooter));
            _footer = draw ?? throw new ArgumentNullException(nameof(draw));
            _footerHeight = height;
            return this;
        }

        private void ThrowIfStarted(string memberName)
        {
            if (_started)
                throw new InvalidOperationException($"{memberName} must be called before the first drawing call.");
        }

        private void EnsureStarted()
        {
            if (_started) return;
            _started = true;
            _pageNumber = 1;
            _canvas = _doc.AddPage(_pageSize);
            _cursorY = ContentTop;
        }

        private void NewPage()
        {
            DrawFooterOnCurrentPage();
            _pageNumber++;
            _canvas = _doc.AddPage(_pageSize);
            _cursorY = ContentTop;
        }

        private void DrawFooterOnCurrentPage() => _footer?.Invoke(_canvas!, _pageNumber);

        /// <summary>
        /// Ensures <paramref name="neededHeight"/> points fit in the remaining space on the
        /// current page, starting a new page first if necessary.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="neededHeight"/> exceeds the full page content height — no page size
        /// could ever fit it.
        /// </exception>
        private void EnsureFits(float neededHeight, string what)
        {
            EnsureStarted();
            if (neededHeight > ContentHeight + 0.01f)
                throw new InvalidOperationException(
                    $"{what} of height {neededHeight:0.##}pt does not fit within the {ContentHeight:0.##}pt " +
                    "page content area (page size minus margins and any footer). Reduce the content or use a larger page size.");
            if (_cursorY + neededHeight > ContentBottom + 0.01f)
                NewPage();
        }

        /// <summary>
        /// Draw word-wrapped text at the cursor. Unlike every other element, text that does not
        /// fit in the remaining space on the current page spills onto as many subsequent pages
        /// as needed, using <see cref="PdfCanvas.DrawTextBox"/>'s overflow index.
        /// </summary>
        public PdfLayout Text(string text, TextStyle? style = null)
        {
            EnsureStarted();
            style ??= TextStyle.Default;
            if (string.IsNullOrEmpty(text)) return this;

            string remaining = text;
            while (remaining.Length > 0)
            {
                float available = ContentBottom - _cursorY;
                if (available < style.FontSize)
                {
                    NewPage();
                    available = ContentBottom - _cursorY;
                }

                int overflow = _canvas!.DrawTextBox(remaining, ContentLeft, _cursorY, ContentWidth, available, style);
                if (overflow <= 0)
                {
                    // Not even one line fit in the available space even on a fresh page
                    // (e.g. a font size larger than the whole content height). Avoid looping
                    // forever and surface the same "too tall" signal other elements use.
                    throw new InvalidOperationException(
                        $"Text does not fit within the {ContentHeight:0.##}pt page content area even on an empty page " +
                        $"(style font size {style.FontSize:0.##}pt). Reduce the font size or use a larger page size.");
                }

                string drawn = remaining.Substring(0, overflow);
                _cursorY += _canvas.MeasureTextBoxHeight(drawn, ContentWidth, style);

                if (overflow >= remaining.Length)
                {
                    remaining = string.Empty;
                }
                else
                {
                    remaining = remaining.Substring(overflow);
                    NewPage();
                }
            }
            return this;
        }

        /// <summary>Advance the cursor by <paramref name="height"/> points without drawing anything.</summary>
        public PdfLayout Spacer(float height)
        {
            EnsureFits(height, "Spacer");
            _cursorY += height;
            return this;
        }

        /// <summary>Draw a full-width horizontal rule at the cursor.</summary>
        public PdfLayout Line(StrokeStyle? style = null, float gapAfter = 8f)
        {
            EnsureFits(gapAfter, "Line");
            _canvas!.DrawLine(ContentLeft, _cursorY, ContentLeft + ContentWidth, _cursorY, style);
            _cursorY += gapAfter;
            return this;
        }

        /// <summary>
        /// Draw <paramref name="table"/> at the cursor. Atomic: if the table's full height does
        /// not fit in the remaining space on the current page, the whole table moves to the next
        /// page rather than splitting across pages.
        /// </summary>
        public PdfLayout Table(PdfTable table)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            EnsureStarted();
            float height = _canvas!.MeasureTableHeight(table);
            EnsureFits(height, "Table");
            _canvas.DrawTable(table, ContentLeft, _cursorY, out float actualHeight);
            _cursorY += actualHeight;
            return this;
        }

        /// <summary>
        /// Draw a row of columns at the cursor. Atomic: the row's height is the tallest column's
        /// height; if that does not fit in the remaining space on the current page, the whole row
        /// moves to the next page. <paramref name="configure"/> runs twice — once to measure each
        /// column's height, once to render — so avoid side effects inside it.
        /// </summary>
        public PdfLayout Row(Action<LayoutRow> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));
            EnsureStarted();

            var measurePass = new LayoutRow(_canvas!, ContentWidth, measureOnly: true);
            configure(measurePass);
            float rowHeight = measurePass.MeasuredHeight;

            EnsureFits(rowHeight, "Row");

            var renderPass = new LayoutRow(_canvas!, ContentWidth, measureOnly: false, ContentLeft, _cursorY);
            configure(renderPass);

            _cursorY += rowHeight;
            return this;
        }

        /// <summary>Escape hatch for arbitrary drawing at the cursor, in a reserved region of the given height.</summary>
        public PdfLayout Canvas(Action<PdfCanvas, LayoutRect> draw, float height)
        {
            if (draw == null) throw new ArgumentNullException(nameof(draw));
            EnsureFits(height, "Canvas region");
            draw(_canvas!, new LayoutRect(ContentLeft, _cursorY, ContentWidth, height));
            _cursorY += height;
            return this;
        }

        /// <summary>Force a new page regardless of remaining space on the current one.</summary>
        public PdfLayout PageBreak()
        {
            EnsureStarted();
            NewPage();
            return this;
        }

        /// <summary>
        /// Finalize the layout: draws the footer (if configured) on the last page and returns
        /// the underlying <see cref="PdfDocument"/> so you can call <c>Save</c>.
        /// </summary>
        public PdfDocument End()
        {
            EnsureStarted();
            DrawFooterOnCurrentPage();
            return _doc;
        }
    }
}
