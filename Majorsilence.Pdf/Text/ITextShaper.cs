// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

namespace Majorsilence.Pdf
{
    /// <summary>
    /// One glyph of shaped text. Distances are in the font's design units (see
    /// <see cref="TrueTypeFont.UnitsPerEm"/>), with y increasing upward.
    /// </summary>
    public readonly struct ShapedGlyph
    {
        public ShapedGlyph(ushort glyphId, int cluster, int xAdvance, int xOffset = 0, int yOffset = 0)
        {
            GlyphId = glyphId;
            Cluster = cluster;
            XAdvance = xAdvance;
            XOffset = xOffset;
            YOffset = yOffset;
        }

        /// <summary>The glyph's index in the font.</summary>
        public ushort GlyphId { get; }

        /// <summary>
        /// Offset in the shaped string of the first UTF-16 code unit of the characters this glyph
        /// was made from. Glyphs made from the same characters (a base and its marks drawn as one,
        /// say) share a cluster.
        /// </summary>
        public int Cluster { get; }

        /// <summary>How far the pen moves after the glyph.</summary>
        public int XAdvance { get; }

        /// <summary>How far the glyph is drawn from the pen, horizontally; the pen does not move.</summary>
        public int XOffset { get; }

        /// <summary>How far the glyph is drawn above the baseline (a mark over a letter, say).</summary>
        public int YOffset { get; }
    }

    /// <summary>
    /// Turns characters into positioned glyphs with the font's OpenType layout tables: joining
    /// forms and ligatures for Arabic, reordering and conjuncts for Indic scripts, mark placement,
    /// kerning. Majorsilence.Pdf has no shaper of its own, so that it keeps no native dependency;
    /// give it one with <see cref="PdfDocument.WithTextShaper"/> (HarfBuzz, for example).
    /// Without one, text is drawn a glyph per character from the font's cmap, in the order the
    /// bidirectional algorithm displays it.
    /// </summary>
    /// <remarks>
    /// The canvas calls this from the thread drawing the page. An implementation shared by
    /// documents drawn on several threads at once must allow that.
    /// </remarks>
    public interface ITextShaper
    {
        /// <summary>
        /// Shape <paramref name="length"/> UTF-16 code units of <paramref name="text"/> from
        /// <paramref name="start"/>, all in one font and one direction. The text around the range
        /// is context only (it decides, for instance, how a letter at the range's edge joins).
        /// </summary>
        /// <returns>
        /// The glyphs in the order they are drawn, left to right: for right-to-left text that is
        /// the reverse of the characters' order. <see cref="ShapedGlyph.Cluster"/> offsets are
        /// into <paramref name="text"/>. Null when the text cannot be shaped (the shaper's native
        /// library is missing, say): the canvas then draws it a glyph per character from the cmap.
        /// </returns>
        ShapedGlyph[]? Shape(TrueTypeFont font, string text, int start, int length, bool rightToLeft);
    }
}
