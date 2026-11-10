// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Majorsilence.Pdf.Internal;
using Majorsilence.Pdf.Text;

namespace Majorsilence.Pdf
{
    // Right-to-left and complex-script text: the bidirectional algorithm puts the line's runs in
    // display order, each run is split by font (the primary, else a fallback with the glyphs) and
    // each piece is shaped, so that what is measured is exactly what is drawn.
    public sealed partial class PdfCanvas
    {
        /// <summary>A piece of a line in one font and direction, shaped, in display order.</summary>
        private readonly struct ComplexItem
        {
            internal ComplexItem(TrueTypeFont ttf, FontSource src, string text, int start, int length, ShapedGlyph[] glyphs)
            {
                Ttf = ttf;
                Src = src;
                Text = text;
                Start = start;
                Length = length;
                Glyphs = glyphs;
            }

            internal TrueTypeFont Ttf { get; }
            internal FontSource Src { get; }
            internal string Text { get; }
            internal int Start { get; }
            internal int Length { get; }
            internal ShapedGlyph[] Glyphs { get; }
        }

        // Text that goes this way: a right-to-left paragraph, right-to-left characters or explicit
        // bidirectional controls, or - when there is a shaper - characters of a script that needs one.
        private bool NeedsComplexLayout(string text, TextStyle style) =>
            style.IsRightToLeft || style.WordSpacing != 0 || UnicodeBidi.HasRightToLeftOrControls(text)
            || (_shaper != null && ComplexScripts.NeedsShaping(text));

        private List<ComplexItem> LayoutComplex(string text, TrueTypeFont primary, FontSource primarySrc, TextStyle style)
        {
            var fallbacks = LoadFallbacks(text, primary, primarySrc, style);
            var items = new List<ComplexItem>();
            foreach (var run in UnicodeBidi.GetVisualRuns(text, style.IsRightToLeft, style.IsBidiOverride))
            {
                bool rtl = run.IsRightToLeft;
                var pieces = SplitByFont(text, run.Start, run.Length, primary, primarySrc, fallbacks);
                if (rtl) pieces.Reverse();       // a right-to-left run displays its last piece first
                foreach (var (start, length, ttf, src) in pieces)
                    items.Add(new ComplexItem(ttf, src, text, start, length, Shape(ttf, text, start, length, rtl)));
            }
            return items;
        }

        private static float ComplexWidth(List<ComplexItem> items, TextStyle style)
        {
            float total = 0f;
            foreach (var item in items)
            {
                long units = 0;
                foreach (var g in item.Glyphs)
                {
                    units += g.XAdvance;
                    if (style.WordSpacing != 0 && IsSpace(item, g)) total += style.WordSpacing;
                }
                total += units / (float)item.Ttf.UnitsPerEm * style.FontSize;
            }
            return total;
        }

        private static bool IsSpace(ComplexItem item, ShapedGlyph g) =>
            g.Cluster >= 0 && g.Cluster < item.Text.Length && item.Text[g.Cluster] == ' ';

        // The pieces of text[start, start+length) by font, in logical order. A combining mark, a
        // joiner or a variation selector stays in the font of the character before it when that
        // font has a glyph for it, so a letter and its marks are shaped together.
        private static List<(int start, int length, TrueTypeFont ttf, FontSource src)> SplitByFont(
            string text, int start, int length, TrueTypeFont primary, FontSource primarySrc,
            List<(TrueTypeFont ttf, FontSource src)> fallbacks)
        {
            var pieces = new List<(int, int, TrueTypeFont, FontSource)>();
            if (fallbacks.Count == 0)
            {
                pieces.Add((start, length, primary, primarySrc));
                return pieces;
            }

            int end = start + length;
            int pieceStart = start;
            TrueTypeFont curTtf = primary;
            FontSource curSrc = primarySrc;
            bool first = true;
            for (int i = start; i < end; )
            {
                int cp = text[i];
                int consumed = 1;
                if (char.IsHighSurrogate(text[i]) && i + 1 < end && char.IsLowSurrogate(text[i + 1]))
                {
                    cp = char.ConvertToUtf32(text[i], text[i + 1]);
                    consumed = 2;
                }

                TrueTypeFont ttf;
                FontSource src;
                if (!first && ComplexScripts.ClingsToPrevious(cp) && curTtf.GetGlyphId(cp) != 0)
                {
                    ttf = curTtf;
                    src = curSrc;
                }
                else
                {
                    ttf = primary;
                    src = primarySrc;
                    if (primary.GetGlyphId(cp) == 0 && !ComplexScripts.IsInvisibleControl(cp))
                    {
                        foreach (var (fbTtf, fbSrc) in fallbacks)
                            if (fbTtf.GetGlyphId(cp) != 0) { ttf = fbTtf; src = fbSrc; break; }
                    }
                    else if (ComplexScripts.IsInvisibleControl(cp) && !first)
                    {
                        ttf = curTtf;       // a control character never starts a font change
                        src = curSrc;
                    }
                }

                if (first)
                {
                    curTtf = ttf;
                    curSrc = src;
                    first = false;
                }
                else if (src.CacheKey != curSrc.CacheKey)
                {
                    pieces.Add((pieceStart, i - pieceStart, curTtf, curSrc));
                    pieceStart = i;
                    curTtf = ttf;
                    curSrc = src;
                }
                i += consumed;
            }
            pieces.Add((pieceStart, end - pieceStart, curTtf, curSrc));
            return pieces;
        }

        // Shaping results of this canvas, as a line is measured several times while it is wrapped
        // and again when it is drawn.
        private const int MaxShapeCache = 512;
        private readonly Dictionary<(TrueTypeFont, string, int, int, bool), ShapedGlyph[]> _shapeCache =
            new Dictionary<(TrueTypeFont, string, int, int, bool), ShapedGlyph[]>();

        private ShapedGlyph[] Shape(TrueTypeFont ttf, string text, int start, int length, bool rtl)
        {
            var key = (ttf, text, start, length, rtl);
            if (_shapeCache.TryGetValue(key, out var cached))
                return cached;

            ShapedGlyph[]? glyphs = null;
            if (_shaper != null && !ttf.IsColorBitmapOnly)
                glyphs = _shaper.Shape(ttf, text, start, length, rtl);
            glyphs ??= ShapeByCmap(ttf, text, start, length, rtl);

            if (_shapeCache.Count >= MaxShapeCache)
                _shapeCache.Clear();
            _shapeCache[key] = glyphs;
            return glyphs;
        }

        // Without a shaper: a glyph per character from the cmap, mirrored and in reverse order when
        // right to left. Correct for Hebrew and for scripts without contextual forms.
        private static ShapedGlyph[] ShapeByCmap(TrueTypeFont ttf, string text, int start, int length, bool rtl)
        {
            var glyphs = new List<ShapedGlyph>(length);
            int end = start + length;
            for (int i = start; i < end; )
            {
                int cp = text[i];
                int consumed = 1;
                if (char.IsHighSurrogate(text[i]) && i + 1 < end && char.IsLowSurrogate(text[i + 1]))
                {
                    cp = char.ConvertToUtf32(text[i], text[i + 1]);
                    consumed = 2;
                }
                if (!ComplexScripts.IsInvisibleControl(cp))
                {
                    ushort gid = ttf.GetGlyphId(rtl ? UnicodeBidi.Mirror(cp) : cp);
                    if (gid == 0 && rtl) gid = ttf.GetGlyphId(cp);
                    glyphs.Add(new ShapedGlyph(gid, i, ttf.GetAdvanceWidth(gid)));
                }
                i += consumed;
            }
            if (rtl) glyphs.Reverse();
            return glyphs.ToArray();
        }

        /// <summary>The line in display order, for the standard fonts, which are drawn a character at a time.</summary>
        private static string VisualOrder(string text, TextStyle style)
        {
            var sb = new StringBuilder(text.Length);
            foreach (var run in UnicodeBidi.GetVisualRuns(text, style.IsRightToLeft, style.IsBidiOverride))
            {
                if (!run.IsRightToLeft)
                {
                    sb.Append(text, run.Start, run.Length);
                    continue;
                }
                for (int i = run.Start + run.Length - 1; i >= run.Start; i--)
                {
                    int cp = text[i];
                    if (char.IsLowSurrogate(text[i]) && i > run.Start && char.IsHighSurrogate(text[i - 1]))
                    {
                        cp = char.ConvertToUtf32(text[i - 1], text[i]);
                        i--;
                    }
                    if (!ComplexScripts.IsInvisibleControl(cp))
                        sb.Append(char.ConvertFromUtf32(UnicodeBidi.Mirror(cp)));
                }
            }
            return sb.ToString();
        }

        // Writes the shaped line from (x, pdfY); returns its width. Inside the caller's q ... Q and
        // with its colour and rotation already set.
        private float EmitComplex(StringBuilder sb, List<ComplexItem> items, float x, float pdfY, TextStyle style, bool rotated)
        {
            float size = style.FontSize;
            float cursorX = x;
            float totalWidth = 0f;
            bool inBt = false;

            foreach (var item in items)
            {
                var ttf = item.Ttf;
                float scale = size / ttf.UnitsPerEm;

                if (ttf.IsColorBitmapOnly && !rotated)
                {
                    if (inBt) { sb.Append("ET\n"); inBt = false; }
                    foreach (var g in item.Glyphs)
                    {
                        DrawColorGlyph(sb, ttf, item.Src, g.GlyphId, cursorX, pdfY, size);
                        float adv = g.XAdvance * scale;
                        cursorX += adv;
                        totalWidth += adv;
                    }
                    continue;
                }

                if (!inBt)
                {
                    sb.Append("BT\n");
                    if (rotated)
                        sb.Append("0 0 Td\n");
                    else
                        sb.AppendNum(cursorX).Append(' ').AppendNum(pdfY).Append(" Td\n");
                    inBt = true;
                }

                var font = _ser.GetOrAddTtfFont(item.Src.CacheKey, ttf);
                sb.Append('/').Append(font.PdfName).Append(' ').AppendNum(size).Append(" Tf\n");
                float itemWidth = AppendGlyphRun(sb, item, font, size, style.WordSpacing);
                cursorX += itemWidth;
                totalWidth += itemWidth;
            }

            if (inBt) sb.Append("ET\n");
            return totalWidth;
        }

        // The glyphs as TJ arrays, with each glyph moved from where its /W width alone would put
        // it to where the shaper did; a glyph raised or lowered (a mark) is drawn with a text rise.
        private float AppendGlyphRun(StringBuilder sb, ComplexItem item, FontResource font, float size, float wordSpacing)
        {
            const string Hex = "0123456789ABCDEF";
            var ttf = item.Ttf;
            double toThousandths = 1000.0 / ttf.UnitsPerEm;
            var glyphs = item.Glyphs;
            var clusterEnds = ClusterEnds(item);
            var textedClusters = new HashSet<int>();

            double pending = 0;         // displacement owed to the pen, in thousandths of an em (right is positive)
            int rise = 0;               // text rise in force, in font units
            bool open = false;          // a TJ array is open
            bool inHex = false;         // a <...> glyph string is open in it
            long advance = 0;
            float spacing = 0f;         // word spacing added, in points

            void CloseHex()
            {
                if (inHex) { sb.Append('>'); inHex = false; }
            }

            void Flush()
            {
                if (Math.Abs(pending) < 0.005) { pending = 0; return; }
                CloseHex();
                sb.Append(' ').AppendNum((float)Math.Round(-pending, 2)).Append(' ');
                pending = 0;
            }

            for (int gi = 0; gi < glyphs.Length; gi++)
            {
                var g = glyphs[gi];
                font.UsedGlyphs!.Add(g.GlyphId);
                if (g.GlyphId != 0 && textedClusters.Add(g.Cluster) && NeedsShapedText(ttf, g.GlyphId))
                {
                    font.ShapedText ??= new SortedDictionary<ushort, string>();
                    if (!font.ShapedText.ContainsKey(g.GlyphId))
                    {
                        int end = clusterEnds.TryGetValue(g.Cluster, out int e) ? e : item.Start + item.Length;
                        string clusterText = item.Text.Substring(g.Cluster, Math.Max(0, end - g.Cluster));
                        if (clusterText.Length > 0)
                            font.ShapedText[g.GlyphId] = clusterText;
                    }
                }

                if (g.YOffset != rise)
                {
                    if (open)
                    {
                        CloseHex();
                        sb.Append("] TJ\n");
                        open = false;
                    }
                    rise = g.YOffset;
                    sb.AppendNum((float)Math.Round(rise * (double)size / ttf.UnitsPerEm, 3)).Append(" Ts\n");
                }
                if (!open)
                {
                    sb.Append('[');
                    open = true;
                }

                pending += g.XOffset * toThousandths;
                Flush();
                if (!inHex) { sb.Append('<'); inHex = true; }
                ushort gid = g.GlyphId;
                sb.Append(Hex[(gid >> 12) & 0xF]).Append(Hex[(gid >> 8) & 0xF])
                  .Append(Hex[(gid >> 4) & 0xF]).Append(Hex[gid & 0xF]);

                // After the glyph the pen is at its /W width (plus the offset); the shaper's advance is what is owed.
                pending = g.XAdvance * toThousandths - PdfSerializer.PdfWidth(ttf, gid) - g.XOffset * toThousandths;
                advance += g.XAdvance;
                if (wordSpacing != 0 && IsSpace(item, g))
                {
                    pending += wordSpacing * 1000.0 / size;
                    spacing += wordSpacing;
                }
            }

            if (open)
            {
                Flush();        // the next piece in this BT starts where this one's advances end
                CloseHex();
                sb.Append("] TJ\n");
            }
            if (rise != 0)
                sb.Append("0 Ts\n");
            return advance * size / (float)ttf.UnitsPerEm + spacing;
        }

        // A shaped glyph's text goes in the ToUnicode CMap when the cmap gives it none, or only an
        // Arabic presentation form: copied or searched, the text is then the letters that were typed.
        private static bool NeedsShapedText(TrueTypeFont ttf, ushort glyphId)
        {
            if (!ttf.TryGetCharacterFor(glyphId, out int cp)) return true;
            return (cp >= 0xFB50 && cp <= 0xFDFF) || (cp >= 0xFE70 && cp <= 0xFEFF);
        }

        // Where each cluster's text ends: the next cluster start above it, in logical order.
        private static Dictionary<int, int> ClusterEnds(ComplexItem item)
        {
            var starts = new SortedSet<int>();
            foreach (var g in item.Glyphs)
                starts.Add(g.Cluster);
            var ends = new Dictionary<int, int>(starts.Count);
            int? previous = null;
            foreach (int s in starts)
            {
                if (previous.HasValue) ends[previous.Value] = s;
                previous = s;
            }
            if (previous.HasValue) ends[previous.Value] = item.Start + item.Length;
            return ends;
        }

        // A glyph of a colour bitmap font (emoji), as an image on the baseline at x.
        private void DrawColorGlyph(StringBuilder sb, TrueTypeFont ttf, FontSource src, ushort gid, float x, float pdfY, float size)
        {
            string imgKey = src.CacheKey + ":glyph:" + gid.ToString(CultureInfo.InvariantCulture);
            var img = _ser.FindImage(imgKey);
            if (img == null && ttf.TryGetGlyphPng(gid, out var pngBytes, out _, out _, out _, out _)
                && PngDecoder.TryDecode(pngBytes, out var rgba, out int pw, out int ph))
                img = _ser.GetOrAddRgbaImage(imgKey, rgba, pw, ph);
            if (img == null || !ttf.TryGetGlyphPng(gid, out _, out byte gw, out byte gh, out sbyte bx, out sbyte by))
                return;

            float ptPerPx = size / ttf.ColorBitmapPpem;
            float imgW = gw * ptPerPx, imgH = gh * ptPerPx;
            float imgX = x + bx * ptPerPx;
            float imgBottom = pdfY + by * ptPerPx - imgH;
            sb.Append("q\n");
            sb.AppendNum(imgW).Append(" 0 0 ").AppendNum(imgH).Append(' ').AppendNum(imgX).Append(' ').AppendNum(imgBottom).Append(" cm\n");
            sb.Append('/').Append(img.PdfName).Append(" Do\nQ\n");
        }
    }
}
