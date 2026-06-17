// Copyright (C) 2025 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

namespace Majorsilence.Pdf
{
    /// <summary>
    /// Immutable description of how a line or shape outline is drawn.
    /// Every <c>With*</c> method returns a new instance; the original is unchanged.
    /// </summary>
    /// <example>
    /// <code>
    /// var thin  = StrokeStyle.Default;
    /// var thick = StrokeStyle.Default.WithWidth(3).WithColor(PdfColor.Red);
    /// var dash  = StrokeStyle.Default.WithWidth(0.5f).Dashed();
    /// </code>
    /// </example>
    public sealed class StrokeStyle
    {
        public float     Width     { get; private set; } = 1f;
        public PdfColor  Color     { get; private set; } = PdfColor.Black;
        public LineStyle LineStyle { get; private set; } = LineStyle.Solid;

        private StrokeStyle() { }

        /// <summary>1 pt solid black line.</summary>
        public static StrokeStyle Default { get; } = new StrokeStyle();

        private StrokeStyle Clone() => (StrokeStyle)MemberwiseClone();

        public StrokeStyle WithWidth(float width)        { var s = Clone(); s.Width     = width;  return s; }
        public StrokeStyle WithColor(PdfColor color)     { var s = Clone(); s.Color     = color;  return s; }
        public StrokeStyle WithLineStyle(LineStyle style) { var s = Clone(); s.LineStyle = style;  return s; }
        public StrokeStyle Solid()                       { var s = Clone(); s.LineStyle = Pdf.LineStyle.Solid;  return s; }
        public StrokeStyle Dashed()                      { var s = Clone(); s.LineStyle = Pdf.LineStyle.Dashed; return s; }
        public StrokeStyle Dotted()                      { var s = Clone(); s.LineStyle = Pdf.LineStyle.Dotted; return s; }
    }
}
