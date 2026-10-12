// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;
using System.Collections.Generic;

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
    /// Calls record an element tree; nothing is drawn until <see cref="End"/>, which paginates
    /// the whole document first. That is what allows "Page X of Y" in headers and footers.
    /// Text, tables (between rows, repeating the header row) and boxes split across pages;
    /// rows, canvas regions and keep-together boxes are atomic and throw
    /// <see cref="InvalidOperationException"/> if taller than a whole page's content area.
    /// </remarks>
    public sealed class PdfLayout
    {
        private readonly PdfDocument _doc;
        private readonly PdfPageSize _pageSize;
        private readonly List<LayoutElement> _elements = new List<LayoutElement>();

        private float _marginLeft = 36f;
        private float _marginTop = 36f;
        private float _marginRight = 36f;
        private float _marginBottom = 36f;

        private Action<PdfCanvas, int, int>? _header;
        private float _headerHeight;
        private Action<PdfCanvas, int, int>? _footer;
        private float _footerHeight;

        private PdfCanvas? _firstCanvas;
        private bool _started;
        private bool _ended;

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

        /// <summary>The width of the content area: page width minus left and right margins.</summary>
        public float ContentWidth => _pageSize.Width - _marginLeft - _marginRight;

        private float ContentTop => _marginTop + _headerHeight;
        private float ContentBottom => _pageSize.Height - _marginBottom - _footerHeight;
        private float ContentHeight => ContentBottom - ContentTop;

        /// <summary>
        /// The <see cref="PdfCanvas"/> of the first page, for extension methods (such as
        /// markdown rendering) that need font-metric access via
        /// <see cref="PdfCanvas.MeasureTextWidth"/> or <see cref="PdfCanvas.MeasureTextBoxHeight"/>
        /// to measure content before calling <see cref="Canvas"/>. Font metrics come from the
        /// document's <see cref="FontRegistry"/>, which is shared across all pages, so a
        /// measurement taken here stays valid on any page. Starts the layout's first page if needed.
        /// </summary>
        public PdfCanvas CurrentCanvas { get { EnsureStarted(); return _firstCanvas!; } }

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
            if (draw == null) throw new ArgumentNullException(nameof(draw));
            return WithFooter((canvas, page, _) => draw(canvas, page), height);
        }

        /// <summary>
        /// Like <see cref="WithFooter(Action{PdfCanvas, int}, float)"/> but the callback also
        /// receives the total page count (the second int), for "Page X of Y".
        /// </summary>
        public PdfLayout WithFooter(Action<PdfCanvas, int, int> draw, float height = 24f)
        {
            ThrowIfStarted(nameof(WithFooter));
            _footer = draw ?? throw new ArgumentNullException(nameof(draw));
            _footerHeight = height;
            return this;
        }

        /// <summary>
        /// Reserve <paramref name="height"/> points at the top of every page and invoke
        /// <paramref name="draw"/> there, once per page, with the 1-based page number and the
        /// total page count. Must be called before the first drawing call.
        /// </summary>
        public PdfLayout WithHeader(Action<PdfCanvas, int, int> draw, float height = 24f)
        {
            ThrowIfStarted(nameof(WithHeader));
            _header = draw ?? throw new ArgumentNullException(nameof(draw));
            _headerHeight = height;
            return this;
        }

        /// <summary>
        /// A footer that draws <paramref name="template"/> with <c>{page}</c> and <c>{pages}</c>
        /// replaced by the current page number and the total page count, e.g. "Page {page} of {pages}".
        /// </summary>
        public PdfLayout WithFooterText(string template, TextStyle? style = null, float height = 24f)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            style ??= TextStyle.Default.WithSize(9).WithAlignment(TextAlignment.Center);
            return WithFooter((canvas, page, pages) =>
            {
                string text = template.Replace("{page}", page.ToString()).Replace("{pages}", pages.ToString());
                canvas.DrawTextBox(text, _marginLeft, _pageSize.Height - _marginBottom - height + 6f,
                    ContentWidth, height, style);
            }, height);
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
            _firstCanvas = _doc.AddPage(_pageSize);
        }

        /// <summary>
        /// Records <paramref name="element"/>, failing fast if it can never fit on a page (even an
        /// empty one) so a bad element is reported at the call that added it.
        /// </summary>
        private PdfLayout Add(LayoutElement element, string what)
        {
            EnsureStarted();
            if (_ended) throw new InvalidOperationException("The layout has already been ended.");
            if (element.Measure(_firstCanvas!, ContentWidth, ContentHeight).Kind == FitKind.Wrap)
                throw TooTall(what);
            _elements.Add(element);
            return this;
        }

        private InvalidOperationException TooTall(string what) =>
            new InvalidOperationException(
                $"{what} does not fit within the {ContentHeight:0.##}pt page content area " +
                "(page size minus margins, header and footer). Reduce the content or use a larger page size.");

        /// <summary>
        /// Add word-wrapped text. Text that does not fit in the remaining space on the current page
        /// spills onto as many subsequent pages as needed.
        /// </summary>
        public PdfLayout Text(string text, TextStyle? style = null)
        {
            EnsureStarted();
            if (string.IsNullOrEmpty(text)) return this;
            return Add(new TextElement(text, style ?? TextStyle.Default), "Text");
        }

        /// <summary>Add <paramref name="height"/> points of empty space.</summary>
        public PdfLayout Spacer(float height) => Add(new SpacerElement(height), "Spacer");

        /// <summary>Add a full-width horizontal rule followed by <paramref name="gapAfter"/> points of space.</summary>
        public PdfLayout Line(StrokeStyle? style = null, float gapAfter = 8f) =>
            Add(new LineElement(style, gapAfter), "Line");

        /// <summary>
        /// Add <paramref name="table"/>. A table that does not fit splits between rows; when it has
        /// a header row (a header background is set) the header repeats on each continuation page.
        /// </summary>
        public PdfLayout Table(PdfTable table)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            return Add(new TableElement(table), "Table");
        }

        /// <summary>
        /// Add a row of columns. Atomic: the row's height is the tallest column's height; if that
        /// does not fit in the remaining space on a page, the whole row moves to the next page.
        /// <paramref name="configure"/> runs once, at the time of the call.
        /// </summary>
        public PdfLayout Row(Action<LayoutRow> configure) => Add(LayoutRow.Build(configure), "Row");

        /// <summary>
        /// Add a box with padding, border and/or background around <paramref name="content"/>.
        /// The box splits across pages with its content unless the style is keep-together.
        /// </summary>
        public PdfLayout Box(LayoutBoxStyle style, Action<LayoutColumn> content) =>
            Add(LayoutColumn.BuildBox(style, content), "Box");

        /// <summary>Keep the content together: it moves to the next page whole rather than splitting.</summary>
        public PdfLayout KeepTogether(Action<LayoutColumn> content) =>
            Box(LayoutBoxStyle.Default.WithKeepTogether(), content);

        /// <summary>
        /// Escape hatch for arbitrary drawing in a region of the given height. <paramref name="draw"/>
        /// is invoked when the document is ended, on the page the region landed on.
        /// </summary>
        public PdfLayout Canvas(Action<PdfCanvas, LayoutRect> draw, float height)
        {
            if (draw == null) throw new ArgumentNullException(nameof(draw));
            return Add(new CanvasElement(draw, height), "Canvas region");
        }

        /// <summary>Force a new page regardless of remaining space on the current one.</summary>
        public PdfLayout PageBreak()
        {
            EnsureStarted();
            _elements.Add(new PageBreakElement());
            return this;
        }

        /// <summary>
        /// Finalize the layout: paginates all recorded content, draws every page (with header and
        /// footer) and returns the underlying <see cref="PdfDocument"/> so you can call <c>Save</c>.
        /// </summary>
        public PdfDocument End()
        {
            EnsureStarted();
            if (_ended) return _doc;
            _ended = true;

            var pages = Paginate();
            for (int i = 0; i < pages.Count; i++)
            {
                var canvas = i == 0 ? _firstCanvas! : _doc.AddPage(_pageSize);
                _header?.Invoke(canvas, i + 1, pages.Count);
                foreach (var (element, y, height) in pages[i])
                    element.Draw(canvas, ContentLeft, ContentTop + y, ContentWidth, height);
                _footer?.Invoke(canvas, i + 1, pages.Count);
            }
            return _doc;
        }

        private List<List<(LayoutElement Element, float Y, float Height)>> Paginate()
        {
            var pages = new List<List<(LayoutElement, float, float)>>();
            var current = new List<(LayoutElement, float, float)>();
            float y = 0f;

            var pending = new LinkedList<LayoutElement>(_elements);
            while (pending.First != null)
            {
                var element = pending.First.Value;
                pending.RemoveFirst();

                if (element is PageBreakElement)
                {
                    pages.Add(current);
                    current = new List<(LayoutElement, float, float)>();
                    y = 0f;
                    continue;
                }

                bool pageIsEmpty = current.Count == 0;
                var fit = element.Measure(_firstCanvas!, ContentWidth, ContentHeight - y);
                switch (fit.Kind)
                {
                    case FitKind.Empty:
                        break;
                    case FitKind.Full:
                        current.Add((fit.Head!, y, fit.Height));
                        y += fit.Height;
                        break;
                    case FitKind.Partial:
                        if (pageIsEmpty && fit.Height <= 0f) throw TooTall("Content");
                        current.Add((fit.Head!, y, fit.Height));
                        pending.AddFirst(fit.Tail!);
                        pages.Add(current);
                        current = new List<(LayoutElement, float, float)>();
                        y = 0f;
                        break;
                    default: // Wrap
                        if (pageIsEmpty) throw TooTall("Content");
                        pages.Add(current);
                        current = new List<(LayoutElement, float, float)>();
                        y = 0f;
                        pending.AddFirst(element);
                        break;
                }
            }
            pages.Add(current);
            return pages;
        }
    }
}
