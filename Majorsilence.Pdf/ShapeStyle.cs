// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

namespace Majorsilence.Pdf
{
    /// <summary>
    /// Immutable description of how a closed shape (rectangle, ellipse, polygon) is filled
    /// and/or stroked.  Either fill or stroke may be omitted independently.
    /// Every <c>With*</c> method returns a new instance; the original is unchanged.
    /// </summary>
    /// <example>
    /// <code>
    /// // Filled only
    /// var blue = ShapeStyle.Filled(PdfColor.Blue);
    ///
    /// // Stroked only
    /// var outline = ShapeStyle.Stroked(PdfColor.Black, width: 2f);
    ///
    /// // Fill + border
    /// var panel = ShapeStyle.Filled(PdfColor.LightGray)
    ///                       .WithStroke(PdfColor.DarkGray);
    ///
    /// // Dashed border, no fill
    /// var dashed = ShapeStyle.Stroked(PdfColor.Red).WithLineStyle(LineStyle.Dashed);
    /// </code>
    /// </example>
    public sealed class ShapeStyle
    {
        public PdfColor? FillColor    { get; private set; }
        public PdfColor? StrokeColor  { get; private set; }
        public float     StrokeWidth  { get; private set; } = 1f;
        public LineStyle LineStyle    { get; private set; } = LineStyle.Solid;
        /// <summary>Fill opacity: 0.0 = fully transparent, 1.0 = fully opaque (default).</summary>
        public float     FillOpacity   { get; private set; } = 1f;
        /// <summary>Stroke opacity: 0.0 = fully transparent, 1.0 = fully opaque (default).</summary>
        public float     StrokeOpacity { get; private set; } = 1f;

        private ShapeStyle() { }

        // ── factories ─────────────────────────────────────────────────────────

        /// <summary>No fill, no stroke — the shape is invisible (useful as a base).</summary>
        public static ShapeStyle Empty { get; } = new ShapeStyle();

        /// <summary>Solid fill, no stroke.</summary>
        public static ShapeStyle Filled(PdfColor fillColor)
        {
            var s = new ShapeStyle(); s.FillColor = fillColor; return s;
        }

        /// <summary>Stroke only, no fill.</summary>
        public static ShapeStyle Stroked(PdfColor strokeColor, float width = 1f)
        {
            var s = new ShapeStyle(); s.StrokeColor = strokeColor; s.StrokeWidth = width; return s;
        }

        // ── mutators ──────────────────────────────────────────────────────────

        private ShapeStyle Clone() => (ShapeStyle)MemberwiseClone();

        public ShapeStyle WithFill(PdfColor color)       { var s = Clone(); s.FillColor   = color; return s; }
        public ShapeStyle WithNoFill()                   { var s = Clone(); s.FillColor   = null;  return s; }
        public ShapeStyle WithStroke(PdfColor color, float width = 1f)
        {
            var s = Clone(); s.StrokeColor = color; s.StrokeWidth = width; return s;
        }
        public ShapeStyle WithNoStroke()                 { var s = Clone(); s.StrokeColor = null;            return s; }
        public ShapeStyle WithStrokeWidth(float width)  { var s = Clone(); s.StrokeWidth = width;           return s; }
        public ShapeStyle WithLineStyle(LineStyle style) { var s = Clone(); s.LineStyle   = style;           return s; }
        public ShapeStyle Dashed()  => WithLineStyle(LineStyle.Dashed);
        public ShapeStyle Dotted()  => WithLineStyle(LineStyle.Dotted);
        /// <summary>Set the fill opacity (0 = transparent, 1 = opaque).</summary>
        public ShapeStyle WithFillOpacity(float opacity)
        { var s = Clone(); s.FillOpacity   = opacity < 0f ? 0f : opacity > 1f ? 1f : opacity; return s; }
        /// <summary>Set the stroke opacity (0 = transparent, 1 = opaque).</summary>
        public ShapeStyle WithStrokeOpacity(float opacity)
        { var s = Clone(); s.StrokeOpacity = opacity < 0f ? 0f : opacity > 1f ? 1f : opacity; return s; }
        /// <summary>Set both fill and stroke opacity to the same value.</summary>
        public ShapeStyle WithOpacity(float opacity) => WithFillOpacity(opacity).WithStrokeOpacity(opacity);

        // ── queries ───────────────────────────────────────────────────────────

        public bool HasFill   => FillColor.HasValue;
        public bool HasStroke => StrokeColor.HasValue && StrokeWidth > 0;
    }
}
