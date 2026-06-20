// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;
using System.Collections.Generic;

namespace Majorsilence.Pdf
{
    /// <summary>
    /// High-level table descriptor consumed by <see cref="PdfCanvas.DrawTable"/>.
    /// Build via the fluent <c>With*</c> methods; rows are added with <see cref="AddRow"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// var table = new PdfTable(100, 200, 100)
    ///     .WithHeaderBackground(PdfColor.DarkBlue)
    ///     .WithAlternateRowBackground(PdfColor.LightGray)
    ///     .WithBorder(PdfColor.Black)
    ///     .WithCellPadding(4);
    ///
    /// table.AddRow("Name", "Address", "Phone");        // header row
    /// table.AddRow("Alice", "1 Main St", "555-0100");
    /// table.AddRow("Bob",   "2 Oak Ave", "555-0200");
    ///
    /// canvas.DrawTable(table, x: 36, y: 100);
    /// </code>
    /// </example>
    public sealed class PdfTable
    {
        private readonly List<PdfTableRow> _rows = new List<PdfTableRow>();

        /// <summary>Column widths in points.</summary>
        public float[] ColumnWidths { get; }

        /// <summary>Sum of all column widths.</summary>
        public float TotalWidth { get { float w = 0; foreach (float c in ColumnWidths) w += c; return w; } }

        /// <summary>Background colour for the first row (treated as a header when set).</summary>
        public PdfColor? HeaderBackground  { get; private set; }

        /// <summary>Background colour applied to every other body row (zero-indexed even rows after the header).</summary>
        public PdfColor? AlternateRowBackground { get; private set; }

        /// <summary>Padding inside each cell in points (default 4).</summary>
        public float CellPadding { get; private set; } = 4f;

        /// <summary>Stroke width for cell borders (default 0.5). Set to 0 to suppress borders.</summary>
        public float BorderWidth { get; private set; } = 0.5f;

        /// <summary>Border colour (default black). Null suppresses borders.</summary>
        public PdfColor? BorderColor { get; private set; } = PdfColor.Black;

        /// <summary>Default text style for all cells.  Individual cells can override via <see cref="PdfTableCell.Style"/>.</summary>
        public TextStyle CellTextStyle { get; private set; } = TextStyle.Default;

        /// <summary>Text style for header cells (first row when <see cref="HeaderBackground"/> is set).</summary>
        public TextStyle HeaderTextStyle { get; private set; } = TextStyle.Default.WithBold(true);

        public IReadOnlyList<PdfTableRow> Rows => _rows;

        public PdfTable(params float[] columnWidths)
        {
            if (columnWidths == null || columnWidths.Length == 0)
                throw new ArgumentException("At least one column width is required.", nameof(columnWidths));
            ColumnWidths = columnWidths;
        }

        private PdfTable Clone()
        {
            var t = new PdfTable(ColumnWidths)
            {
                HeaderBackground     = HeaderBackground,
                AlternateRowBackground = AlternateRowBackground,
                CellPadding          = CellPadding,
                BorderWidth          = BorderWidth,
                BorderColor          = BorderColor,
                CellTextStyle        = CellTextStyle,
                HeaderTextStyle      = HeaderTextStyle,
            };
            foreach (var r in _rows) t._rows.Add(r);
            return t;
        }

        public PdfTable WithHeaderBackground(PdfColor color)        { var t = Clone(); t.HeaderBackground       = color; return t; }
        public PdfTable WithAlternateRowBackground(PdfColor color)  { var t = Clone(); t.AlternateRowBackground = color; return t; }
        public PdfTable WithCellPadding(float padding)              { var t = Clone(); t.CellPadding            = padding; return t; }
        public PdfTable WithBorder(PdfColor color, float width = 0.5f) { var t = Clone(); t.BorderColor = color; t.BorderWidth = width; return t; }
        public PdfTable WithNoBorder()                              { var t = Clone(); t.BorderColor = null; return t; }
        public PdfTable WithCellTextStyle(TextStyle style)          { var t = Clone(); t.CellTextStyle    = style; return t; }
        public PdfTable WithHeaderTextStyle(TextStyle style)        { var t = Clone(); t.HeaderTextStyle   = style; return t; }

        /// <summary>Append a row whose cells contain plain text.</summary>
        public PdfTable AddRow(params string[] cellText)
        {
            var cells = new PdfTableCell[ColumnWidths.Length];
            for (int i = 0; i < ColumnWidths.Length; i++)
                cells[i] = new PdfTableCell(i < cellText.Length ? (cellText[i] ?? "") : "");
            _rows.Add(new PdfTableRow(cells));
            return this;
        }

        /// <summary>Append a fully specified row (allows per-cell overrides).</summary>
        public PdfTable AddRow(PdfTableRow row)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            _rows.Add(row);
            return this;
        }
    }

    /// <summary>A single row of a <see cref="PdfTable"/>.</summary>
    public sealed class PdfTableRow
    {
        public IReadOnlyList<PdfTableCell> Cells { get; }

        /// <summary>Override the row background colour (overrides even/odd alternation).</summary>
        public PdfColor? BackgroundColor { get; }

        public PdfTableRow(params PdfTableCell[] cells) : this(null, cells) { }

        public PdfTableRow(PdfColor? backgroundColor, params PdfTableCell[] cells)
        {
            BackgroundColor = backgroundColor;
            Cells = cells ?? throw new ArgumentNullException(nameof(cells));
        }
    }

    /// <summary>A single cell in a <see cref="PdfTableRow"/>.</summary>
    public sealed class PdfTableCell
    {
        /// <summary>Text content of the cell.</summary>
        public string Text { get; }

        /// <summary>Optional per-cell text style override.  Null = use the table's default.</summary>
        public TextStyle? Style { get; }

        /// <summary>Optional per-cell background colour override.</summary>
        public PdfColor? BackgroundColor { get; }

        public PdfTableCell(string text, TextStyle? style = null, PdfColor? backgroundColor = null)
        {
            Text            = text ?? "";
            Style           = style;
            BackgroundColor = backgroundColor;
        }
    }
}
