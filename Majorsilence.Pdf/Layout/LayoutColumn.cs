// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;

namespace Majorsilence.Pdf.Layout
{
    /// <summary>
    /// Builds the content of a single column inside a <see cref="PdfLayout.Row"/>. Instances are
    /// supplied to your configuration callback twice — once to measure the column's height and
    /// once to actually draw it — so avoid side effects (mutating captured state) inside the
    /// callback; only the calls you make on the <see cref="LayoutColumn"/> itself should matter.
    /// </summary>
    public sealed class LayoutColumn
    {
        private readonly PdfCanvas _canvas;
        private readonly float _width;
        private readonly bool _measureOnly;
        private readonly float _originX;
        private readonly float _originY;
        private float _cursorY;

        internal LayoutColumn(PdfCanvas canvas, float width, bool measureOnly, float originX, float originY)
        {
            _canvas = canvas;
            _width = width;
            _measureOnly = measureOnly;
            _originX = originX;
            _originY = originY;
        }

        internal float MeasuredHeight => _cursorY;

        /// <summary>A fixed column width in PDF points, for <see cref="LayoutRow.Column(LayoutColumnWidth, Action{LayoutColumn})"/>.</summary>
        public static LayoutColumnWidth Fixed(float points) => new LayoutColumnWidth(points, isFraction: false);

        /// <summary>Draw word-wrapped text at the column's cursor, advancing it by the wrapped height.</summary>
        public LayoutColumn Text(string text, TextStyle? style = null)
        {
            style ??= TextStyle.Default;
            if (string.IsNullOrEmpty(text)) return this;

            float height = _canvas.MeasureTextBoxHeight(text, _width, style);
            if (!_measureOnly)
                _canvas.DrawTextBox(text, _originX, _originY + _cursorY, _width, height + 1f, style);
            _cursorY += height;
            return this;
        }

        /// <summary>Advance the column's cursor by <paramref name="height"/> points without drawing anything.</summary>
        public LayoutColumn Spacer(float height)
        {
            _cursorY += height;
            return this;
        }

        /// <summary>Draw a full-width horizontal rule at the column's cursor.</summary>
        public LayoutColumn Line(StrokeStyle? style = null, float gapAfter = 8f)
        {
            if (!_measureOnly)
                _canvas.DrawLine(_originX, _originY + _cursorY, _originX + _width, _originY + _cursorY, style);
            _cursorY += gapAfter;
            return this;
        }

        /// <summary>Draw <paramref name="table"/> at the column's cursor, advancing it by the table's height.</summary>
        public LayoutColumn Table(PdfTable table)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            float height = _canvas.MeasureTableHeight(table);
            if (!_measureOnly)
                _canvas.DrawTable(table, _originX, _originY + _cursorY, out _);
            _cursorY += height;
            return this;
        }

        /// <summary>Nest a row inside this column. The nested row's width is this column's width.</summary>
        public LayoutColumn Row(Action<LayoutRow> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));
            var nested = new LayoutRow(_canvas, _width, _measureOnly, _originX, _originY + _cursorY);
            configure(nested);
            _cursorY += nested.MeasuredHeight;
            return this;
        }

        /// <summary>Escape hatch for arbitrary drawing at the column's current cursor position.</summary>
        public LayoutColumn Canvas(Action<PdfCanvas, LayoutRect> draw, float height)
        {
            if (draw == null) throw new ArgumentNullException(nameof(draw));
            if (!_measureOnly)
                draw(_canvas, new LayoutRect(_originX, _originY + _cursorY, _width, height));
            _cursorY += height;
            return this;
        }
    }
}
