// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;
using System.Collections.Generic;

namespace Majorsilence.Pdf.Layout
{
    /// <summary>
    /// Collects the content of a single column inside a <see cref="PdfLayout.Row"/>, or of a
    /// <see cref="Box"/>. Calls only record elements into the layout tree; nothing is measured or
    /// drawn until the document is paginated, so your configuration callback runs exactly once.
    /// </summary>
    public sealed class LayoutColumn
    {
        private readonly List<LayoutElement> _items = new List<LayoutElement>();

        internal LayoutColumn() { }

        internal StackElement ToElement() => new StackElement(_items.ToArray());

        /// <summary>A fixed column width in PDF points, for <see cref="LayoutRow.Column(LayoutColumnWidth, Action{LayoutColumn})"/>.</summary>
        public static LayoutColumnWidth Fixed(float points) => new LayoutColumnWidth(points, isFraction: false);

        /// <summary>Add word-wrapped text.</summary>
        public LayoutColumn Text(string text, TextStyle? style = null)
        {
            if (!string.IsNullOrEmpty(text)) _items.Add(new TextElement(text, style ?? TextStyle.Default));
            return this;
        }

        /// <summary>Add <paramref name="height"/> points of empty space.</summary>
        public LayoutColumn Spacer(float height)
        {
            _items.Add(new SpacerElement(height));
            return this;
        }

        /// <summary>Add a full-width horizontal rule followed by <paramref name="gapAfter"/> points of space.</summary>
        public LayoutColumn Line(StrokeStyle? style = null, float gapAfter = 8f)
        {
            _items.Add(new LineElement(style, gapAfter));
            return this;
        }

        /// <summary>Add <paramref name="table"/>.</summary>
        public LayoutColumn Table(PdfTable table)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            _items.Add(new TableElement(table));
            return this;
        }

        /// <summary>Nest a row inside this column. The nested row's width is this column's width.</summary>
        public LayoutColumn Row(Action<LayoutRow> configure)
        {
            _items.Add(LayoutRow.Build(configure));
            return this;
        }

        /// <summary>Wrap content in a box with padding, border and/or background.</summary>
        public LayoutColumn Box(LayoutBoxStyle style, Action<LayoutColumn> content)
        {
            _items.Add(BuildBox(style, content));
            return this;
        }

        /// <summary>Keep the content together: it moves to the next page whole rather than splitting.</summary>
        public LayoutColumn KeepTogether(Action<LayoutColumn> content) =>
            Box(LayoutBoxStyle.Default.WithKeepTogether(), content);

        /// <summary>Escape hatch for arbitrary drawing in a region of the given height.</summary>
        public LayoutColumn Canvas(Action<PdfCanvas, LayoutRect> draw, float height)
        {
            if (draw == null) throw new ArgumentNullException(nameof(draw));
            _items.Add(new CanvasElement(draw, height));
            return this;
        }

        internal static LayoutElement BuildBox(LayoutBoxStyle style, Action<LayoutColumn> content)
        {
            if (style == null) throw new ArgumentNullException(nameof(style));
            if (content == null) throw new ArgumentNullException(nameof(content));
            var inner = new LayoutColumn();
            content(inner);
            return new BoxElement(style, inner.ToElement());
        }
    }
}
