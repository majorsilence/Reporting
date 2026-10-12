// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;

namespace Majorsilence.Pdf.Layout
{
    /// <summary>
    /// A rectangle in PDF points (top-left origin, matching <see cref="PdfCanvas"/>'s coordinate
    /// system) describing the region reserved for a <see cref="PdfLayout.Canvas"/> escape hatch.
    /// </summary>
    public readonly struct LayoutRect
    {
        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }

        public LayoutRect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    /// <summary>
    /// A column width specification passed to <see cref="LayoutRow.Column(LayoutColumnWidth, Action{LayoutColumn})"/>.
    /// Create one with <see cref="LayoutColumn.Fixed"/>; use a plain <c>float</c> fraction
    /// (0–1) with the other <see cref="LayoutRow.Column(float, Action{LayoutColumn})"/> overload
    /// for width relative to the row's total width.
    /// </summary>
    public readonly struct LayoutColumnWidth
    {
        internal float Value { get; }
        internal bool IsFraction { get; }

        internal LayoutColumnWidth(float value, bool isFraction)
        {
            Value = value;
            IsFraction = isFraction;
        }
    }
}

namespace Majorsilence.Pdf.Layout
{
    /// <summary>
    /// Padding, border, background and keep-together settings for a box created with
    /// <see cref="PdfLayout.Box"/> or <see cref="LayoutColumn.Box"/>. Immutable; each
    /// <c>With…</c> call returns a modified copy.
    /// </summary>
    public sealed class LayoutBoxStyle
    {
        public float PaddingLeft { get; private set; }
        public float PaddingTop { get; private set; }
        public float PaddingRight { get; private set; }
        public float PaddingBottom { get; private set; }
        public float BorderWidth { get; private set; }
        public PdfColor? BorderColor { get; private set; }
        public PdfColor? Background { get; private set; }

        /// <summary>When true the box never splits across pages; it moves to the next page whole.</summary>
        public bool IsKeepTogether { get; private set; }

        /// <summary>Fixed box width in points, capped at the available width; null fills the available width.</summary>
        public float? Width { get; private set; }

        /// <summary>Minimum box height in points; the box is padded out to it.</summary>
        public float MinHeight { get; private set; }

        /// <summary>Where a box narrower than its container sits horizontally.</summary>
        public TextAlignment Alignment { get; private set; } = TextAlignment.Left;

        public static LayoutBoxStyle Default { get; } = new LayoutBoxStyle();

        public LayoutBoxStyle WithWidth(float width) { var s = Clone(); s.Width = width; return s; }

        public LayoutBoxStyle WithMinHeight(float height) { var s = Clone(); s.MinHeight = height; return s; }

        public LayoutBoxStyle WithAlignment(TextAlignment alignment) { var s = Clone(); s.Alignment = alignment; return s; }

        public LayoutBoxStyle WithPadding(float all) => WithPadding(all, all, all, all);

        public LayoutBoxStyle WithPadding(float left, float top, float right, float bottom)
        {
            var s = Clone();
            s.PaddingLeft = left; s.PaddingTop = top; s.PaddingRight = right; s.PaddingBottom = bottom;
            return s;
        }

        public LayoutBoxStyle WithBorder(PdfColor color, float width = 1f)
        {
            var s = Clone();
            s.BorderColor = color; s.BorderWidth = width;
            return s;
        }

        public LayoutBoxStyle WithBackground(PdfColor color)
        {
            var s = Clone();
            s.Background = color;
            return s;
        }

        public LayoutBoxStyle WithKeepTogether(bool keepTogether = true)
        {
            var s = Clone();
            s.IsKeepTogether = keepTogether;
            return s;
        }

        private LayoutBoxStyle Clone() => (LayoutBoxStyle)MemberwiseClone();
    }
}
