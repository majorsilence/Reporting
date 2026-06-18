// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

namespace Majorsilence.Pdf
{
    /// <summary>Horizontal alignment of a text run within its available width.</summary>
    public enum TextAlignment { Left, Center, Right }

    /// <summary>Visual decoration applied to a text run.</summary>
    public enum TextDecoration { None, Underline, Strikethrough, Overline }

    /// <summary>
    /// Immutable description of how text should be drawn on a <see cref="PdfCanvas"/>.
    /// Every <c>With*</c> method returns a new instance; the original is unchanged.
    /// </summary>
    /// <example>
    /// <code>
    /// var heading = TextStyle.Default
    ///     .WithFamily("Arial").WithSize(18).WithBold().WithColor(PdfColor.Black);
    ///
    /// var body = TextStyle.Default
    ///     .WithFontFile("/path/to/NotoSans-Regular.ttf").WithSize(11);
    /// </code>
    /// </example>
    public sealed class TextStyle
    {
        // ── properties ────────────────────────────────────────────────────────

        /// <summary>
        /// Font family name.  Mapped to the nearest standard Type-1 font unless
        /// <see cref="FontFilePath"/> is also set, in which case <c>FontFamily</c> is only
        /// used as a display hint.
        /// Standard names: Helvetica, Times-Roman, Courier, Symbol, ZapfDingbats.
        /// </summary>
        public string FontFamily { get; private set; } = "Helvetica";

        /// <summary>
        /// Absolute path to a TrueType or OpenType font file.  When set, the font is
        /// embedded in the PDF and <see cref="FontFamily"/> is used only as metadata.
        /// When <c>null</c>, a standard Type-1 font derived from <see cref="FontFamily"/> is used.
        /// </summary>
        public string? FontFilePath { get; private set; }

        /// <summary>Font size in PDF points (1 pt = 1/72 inch).</summary>
        public float FontSize { get; private set; } = 12f;

        /// <summary>Text colour. Defaults to <see cref="PdfColor.Black"/>.</summary>
        public PdfColor Color { get; private set; } = PdfColor.Black;

        public bool IsBold   { get; private set; }
        public bool IsItalic { get; private set; }

        /// <summary>Horizontal text alignment within the available width.</summary>
        public TextAlignment Alignment { get; private set; } = TextAlignment.Left;

        /// <summary>Visual decoration (underline, strikethrough, overline).</summary>
        public TextDecoration Decoration { get; private set; } = TextDecoration.None;

        /// <summary>
        /// When <c>true</c> the text is rotated 90° counter-clockwise (top-to-bottom writing).
        /// </summary>
        public bool IsVertical { get; private set; }

        // ── construction ──────────────────────────────────────────────────────

        private TextStyle() { }

        /// <summary>A sensible starting point: Helvetica 12 pt, black, left-aligned.</summary>
        public static TextStyle Default { get; } = new TextStyle();

        private TextStyle Clone() => (TextStyle)MemberwiseClone();

        // ── fluent mutators ───────────────────────────────────────────────────

        /// <summary>Use a standard Type-1 font by family name (e.g. "Helvetica", "Times-Roman", "Courier").</summary>
        public TextStyle WithFamily(string family)
        {
            var s = Clone(); s.FontFamily = family ?? "Helvetica"; s.FontFilePath = null; return s;
        }

        /// <summary>
        /// Embed the TrueType / OpenType font at <paramref name="path"/>.
        /// The file is read once per document and cached.
        /// </summary>
        public TextStyle WithFontFile(string path)
        {
            var s = Clone(); s.FontFilePath = path; return s;
        }

        /// <summary>Font size in points.</summary>
        public TextStyle WithSize(float points)
        {
            var s = Clone(); s.FontSize = points; return s;
        }

        /// <summary>Text foreground colour.</summary>
        public TextStyle WithColor(PdfColor color)
        {
            var s = Clone(); s.Color = color; return s;
        }

        public TextStyle WithBold(bool bold = true)
        {
            var s = Clone(); s.IsBold = bold; return s;
        }

        public TextStyle WithItalic(bool italic = true)
        {
            var s = Clone(); s.IsItalic = italic; return s;
        }

        public TextStyle WithAlignment(TextAlignment alignment)
        {
            var s = Clone(); s.Alignment = alignment; return s;
        }

        public TextStyle WithUnderline()      { var s = Clone(); s.Decoration = TextDecoration.Underline;     return s; }
        public TextStyle WithStrikethrough()  { var s = Clone(); s.Decoration = TextDecoration.Strikethrough; return s; }
        public TextStyle WithOverline()       { var s = Clone(); s.Decoration = TextDecoration.Overline;      return s; }
        public TextStyle WithNoDecoration()   { var s = Clone(); s.Decoration = TextDecoration.None;          return s; }

        /// <summary>Rotate text 90° counter-clockwise (top-to-bottom writing direction).</summary>
        public TextStyle WithVertical(bool vertical = true)
        {
            var s = Clone(); s.IsVertical = vertical; return s;
        }
    }
}
