// Copyright (C) 2025 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Text;
using Majorsilence.Pdf.Internal;

namespace Majorsilence.Pdf
{
    /// <summary>
    /// Drawing surface for a single PDF page.  Coordinates are in PDF points (1 pt = 1/72 inch)
    /// with the <b>top-left corner as the origin</b> (Y increases downward), which matches screen
    /// and document conventions.  Internally the canvas converts to PDF's bottom-left system.
    /// All drawing methods return <c>this</c> for fluent chaining.
    /// </summary>
    /// <example>
    /// <code>
    /// PdfDocument.Create()
    ///     .AddPage(PageSizes.A4, canvas =>
    ///     {
    ///         canvas
    ///             .DrawRectangle(20, 20, 200, 40, ShapeStyle.Filled(PdfColor.LightGray))
    ///             .DrawText("Hello, PDF!", 24, 44, TextStyle.Default.WithSize(18));
    ///     })
    ///     .Save("output.pdf");
    /// </code>
    /// </example>
    public sealed class PdfCanvas
    {
        private readonly PageData _page;
        private readonly PdfSerializer _ser;
        private readonly FontCache _fontCache;
        private readonly FontRegistry? _registry;
        private StringBuilder Content => _page.Content;

        internal PdfCanvas(PageData page, PdfSerializer ser, FontCache fontCache, FontRegistry? registry = null)
        {
            _page = page;
            _ser = ser;
            _fontCache = fontCache;
            _registry = registry;
        }

        // ── coordinate helper ─────────────────────────────────────────────────

        // Convert top-left Y to PDF bottom-left Y
        private float PdfY(float y) => _page.Height - y;

        // ── text ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Draw <paramref name="text"/> with its baseline at (<paramref name="x"/>, <paramref name="y"/>).
        /// When a <see cref="FontRegistry"/> is attached and the style's family is registered, the
        /// text is automatically split into per-font segments so that characters missing from the
        /// primary font are rendered with the first fallback font that has a glyph for them.
        /// </summary>
        public PdfCanvas DrawText(string text, float x, float y, TextStyle? style = null)
        {
            if (string.IsNullOrEmpty(text)) return this;
            style ??= TextStyle.Default;

            if (style.Alignment != TextAlignment.Left)
            {
                float w = MeasureTextWidth(text, style);
                if (style.Alignment == TextAlignment.Right) x -= w;
                else x -= w / 2f;
            }

            float pdfY = PdfY(y);
            float r = style.Color.Rf, g = style.Color.Gf, b = style.Color.Bf;
            var sb = Content;

            // ── resolve font source ───────────────────────────────────────────

            // Explicit file path always wins.
            FontSource? primarySource = style.FontFilePath != null
                ? FontSource.FromPath(style.FontFilePath)
                : _registry?.Resolve(style.FontFamily, style.IsBold, style.IsItalic);

            sb.Append("q\n");

            if (style.IsVertical)
                sb.Append($"{F(0)} {F(1)} {F(-1)} {F(0)} {F(x)} {F(pdfY)} cm\n");
            else
                sb.Append("1 0 0 1 0 0 cm\n");

            sb.Append($"{F(r)} {F(g)} {F(b)} rg\n");

            float totalWidth;

            if (primarySource != null)
            {
                // ── TTF path (explicit file or registry family) ───────────────
                var primaryTtf = _fontCache.GetOrLoad(primarySource);

                // Build fallback chain: registry fallbacks not already used as primary
                var fallbacks = new List<(TrueTypeFont ttf, FontSource src)>();
                if (_registry != null)
                {
                    foreach (var fbSrc in _registry.GetFallbackSources(style.IsBold, style.IsItalic))
                    {
                        if (fbSrc.CacheKey == primarySource.CacheKey) continue;
                        fallbacks.Add((_fontCache.GetOrLoad(fbSrc), fbSrc));
                    }
                }

                // Segment text so each run uses the first font that has every glyph.
                var segments = SegmentByFont(text, primaryTtf, primarySource, fallbacks);

                sb.Append("BT\n");
                if (style.IsVertical)
                    sb.Append($"{F(0)} {F(style.FontSize)} Td\n");
                else
                    sb.Append($"{F(x)} {F(pdfY)} Td\n");

                totalWidth = 0f;
                foreach (var (segText, segTtf, segSrc) in segments)
                {
                    var font = _ser.GetOrAddTtfFont(segSrc.CacheKey, segTtf);
                    sb.Append($"/{font.PdfName} {F(style.FontSize)} Tf\n");
                    sb.Append($"{PdfSerializer.EncodeGlyphIds(segText, segTtf)} Tj\n");
                    totalWidth += segTtf.GetWidthPoint(segText, style.FontSize);
                }

                sb.Append("ET\n");
            }
            else
            {
                // ── Standard Type-1 font path ─────────────────────────────────
                string stdName = ResolveStandardName(style.FontFamily, style.IsBold, style.IsItalic);
                var font = _ser.GetOrAddStandardFont(stdName);

                string encoded = EscapeWinAnsi(text, out char? unmappable);
                if (unmappable.HasValue)
                    throw new InvalidOperationException(
                        $"Character U+{(int)unmappable.Value:X4} ('{unmappable.Value}') cannot be rendered " +
                        $"with the standard font '{style.FontFamily}' because it is not in Windows-1252. " +
                        $"Embed a TrueType font via TextStyle.WithFontFile() or add the family to a FontRegistry.");

                sb.Append("BT\n");
                sb.Append($"/{font.PdfName} {F(style.FontSize)} Tf\n");
                if (style.IsVertical)
                    sb.Append($"{F(0)} {F(style.FontSize)} Td\n");
                else
                    sb.Append($"{F(x)} {F(pdfY)} Td\n");
                sb.Append($"({PdfSerializer.PdfString(encoded)}) Tj\n");
                sb.Append("ET\n");

                totalWidth = MeasureStandardFontWidth(encoded, style);
            }

            // ── decorations ───────────────────────────────────────────────────
            if (style.Decoration != TextDecoration.None)
            {
                float lineY;
                switch (style.Decoration)
                {
                    case TextDecoration.Underline: lineY = pdfY - style.FontSize * 0.1f; break;
                    case TextDecoration.Strikethrough: lineY = pdfY + style.FontSize * 0.3f; break;
                    case TextDecoration.Overline: lineY = pdfY + style.FontSize * 0.9f; break;
                    default: lineY = pdfY; break;
                }
                sb.Append($"{F(r)} {F(g)} {F(b)} RG\n");
                sb.Append("0.5 w\n");
                sb.Append($"{F(x)} {F(lineY)} m {F(x + totalWidth)} {F(lineY)} l S\n");
            }

            sb.Append("Q\n");
            return this;
        }

        // Segment text into runs where each run uses the same font.
        // Primary font is tried first; if its glyph ID is 0 the fallbacks are tried in order.
        private static List<(string text, TrueTypeFont ttf, FontSource src)> SegmentByFont(
            string text,
            TrueTypeFont primaryTtf,
            FontSource primarySrc,
            List<(TrueTypeFont ttf, FontSource src)> fallbacks)
        {
            var result = new List<(string, TrueTypeFont, FontSource)>();
            if (fallbacks.Count == 0)
            {
                result.Add((text, primaryTtf, primarySrc));
                return result;
            }

            var curText = new StringBuilder();
            TrueTypeFont curTtf = primaryTtf;
            FontSource curSrc = primarySrc;

            for (int i = 0; i < text.Length; i++)
            {
                int cp;
                int consumed = 1;
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    cp = char.ConvertToUtf32(text[i], text[i + 1]);
                    consumed = 2;
                }
                else
                {
                    cp = text[i];
                }

                // Pick the first font that has a glyph for this codepoint
                TrueTypeFont chosenTtf = primaryTtf;
                FontSource chosenSrc = primarySrc;
                if (primaryTtf.GetGlyphId(cp) == 0)
                {
                    foreach (var (fbTtf, fbSrc) in fallbacks)
                    {
                        if (fbTtf.GetGlyphId(cp) != 0) { chosenTtf = fbTtf; chosenSrc = fbSrc; break; }
                    }
                    // If nothing found, stay with primary (will emit .notdef box)
                }

                if (chosenSrc.CacheKey != curSrc.CacheKey && curText.Length > 0)
                {
                    result.Add((curText.ToString(), curTtf, curSrc));
                    curText.Clear();
                    curTtf = chosenTtf;
                    curSrc = chosenSrc;
                }
                else if (chosenSrc.CacheKey != curSrc.CacheKey)
                {
                    curTtf = chosenTtf;
                    curSrc = chosenSrc;
                }

                for (int k = 0; k < consumed; k++)
                    curText.Append(text[i + k]);
                i += consumed - 1;
            }

            if (curText.Length > 0)
                result.Add((curText.ToString(), curTtf, curSrc));

            return result;
        }

        // ── lines ─────────────────────────────────────────────────────────────

        /// <summary>Draw a straight line from (x1,y1) to (x2,y2).</summary>
        public PdfCanvas DrawLine(float x1, float y1, float x2, float y2, StrokeStyle? style = null)
        {
            style ??= StrokeStyle.Default;
            AppendStrokeState(style);
            Content.Append($"{F(x1)} {F(PdfY(y1))} m {F(x2)} {F(PdfY(y2))} l S\n");
            ResetDash();
            return this;
        }

        // ── rectangles ───────────────────────────────────────────────────────

        /// <summary>Draw a rectangle with top-left at (x,y) and given width/height.</summary>
        public PdfCanvas DrawRectangle(float x, float y, float width, float height, ShapeStyle? style = null)
        {
            style ??= ShapeStyle.Stroked(PdfColor.Black);
            AppendShapeState(style);
            float pdfY = PdfY(y + height); // bottom edge in PDF coords
            Content.Append($"{F(x)} {F(pdfY)} {F(width)} {F(height)} re\n");
            AppendPaintOp(style);
            ResetDash();
            return this;
        }

        // ── ellipses ─────────────────────────────────────────────────────────

        /// <summary>Draw an ellipse bounded by the given rectangle.</summary>
        public PdfCanvas DrawEllipse(float x, float y, float width, float height, ShapeStyle? style = null)
        {
            style ??= ShapeStyle.Stroked(PdfColor.Black);
            AppendShapeState(style);

            float cx = x + width / 2f;
            float cy = PdfY(y + height / 2f);
            float rx = width / 2f;
            float ry = height / 2f;

            // Approximate circle with 4 cubic Bézier segments (κ ≈ 0.5523)
            const float k = 0.5523f;
            var sb = Content;
            sb.Append($"{F(cx)}        {F(cy + ry)}  m\n");
            sb.Append($"{F(cx + k * rx)} {F(cy + ry)}  {F(cx + rx)} {F(cy + k * ry)} {F(cx + rx)} {F(cy)} c\n");
            sb.Append($"{F(cx + rx)}   {F(cy - k * ry)} {F(cx + k * rx)} {F(cy - ry)} {F(cx)} {F(cy - ry)} c\n");
            sb.Append($"{F(cx - k * rx)} {F(cy - ry)}  {F(cx - rx)} {F(cy - k * ry)} {F(cx - rx)} {F(cy)} c\n");
            sb.Append($"{F(cx - rx)}   {F(cy + k * ry)} {F(cx - k * rx)} {F(cy + ry)} {F(cx)} {F(cy + ry)} c\n");
            AppendPaintOp(style);
            ResetDash();
            return this;
        }

        // ── polygons ──────────────────────────────────────────────────────────

        /// <summary>Draw a closed polygon through the given points.</summary>
        public PdfCanvas DrawPolygon(IReadOnlyList<(float x, float y)> points, ShapeStyle? style = null)
        {
            if (points == null || points.Count < 2) return this;
            style ??= ShapeStyle.Stroked(PdfColor.Black);
            AppendShapeState(style);
            var sb = Content;
            sb.Append($"{F(points[0].x)} {F(PdfY(points[0].y))} m\n");
            for (int i = 1; i < points.Count; i++)
                sb.Append($"{F(points[i].x)} {F(PdfY(points[i].y))} l\n");
            sb.Append("h\n");
            AppendPaintOp(style);
            ResetDash();
            return this;
        }

        // ── curves (open polyline through control points) ─────────────────────

        /// <summary>
        /// Draw an open curve (approximated as a quadratic Catmull-Rom spline) through the given points.
        /// </summary>
        public PdfCanvas DrawCurve(IReadOnlyList<(float x, float y)> points, StrokeStyle? style = null)
        {
            if (points == null || points.Count < 2) return this;
            style ??= StrokeStyle.Default;
            AppendStrokeState(style);
            var sb = Content;
            sb.Append($"{F(points[0].x)} {F(PdfY(points[0].y))} m\n");
            if (points.Count == 2)
            {
                sb.Append($"{F(points[1].x)} {F(PdfY(points[1].y))} l\n");
            }
            else
            {
                for (int i = 0; i < points.Count - 1; i++)
                {
                    // Catmull-Rom → cubic Bézier
                    var p0 = i > 0 ? points[i - 1] : points[0];
                    var p1 = points[i];
                    var p2 = points[i + 1];
                    var p3 = i + 2 < points.Count ? points[i + 2] : points[i + 1];

                    float cp1x = p1.x + (p2.x - p0.x) / 6f;
                    float cp1y = p1.y + (p2.y - p0.y) / 6f;
                    float cp2x = p2.x - (p3.x - p1.x) / 6f;
                    float cp2y = p2.y - (p3.y - p1.y) / 6f;

                    sb.Append($"{F(cp1x)} {F(PdfY(cp1y))} {F(cp2x)} {F(PdfY(cp2y))} {F(p2.x)} {F(PdfY(p2.y))} c\n");
                }
            }
            sb.Append("S\n");
            ResetDash();
            return this;
        }

        // ── images ───────────────────────────────────────────────────────────

        /// <summary>
        /// Draw an image with top-left at (x,y) scaled to (width × height) points.
        /// <paramref name="imageData"/> must be either raw JPEG bytes or raw RGB24 bytes.
        /// </summary>
        public PdfCanvas DrawImage(byte[] imageData, int pixelWidth, int pixelHeight, bool isJpeg,
                                   float x, float y, float width, float height)
        {
            var img = _ser.GetOrAddImage(imageData, pixelWidth, pixelHeight, isJpeg);
            float pdfY = PdfY(y + height);
            var sb = Content;
            sb.Append("q\n");
            sb.Append($"{F(width)} 0 0 {F(height)} {F(x)} {F(pdfY)} cm\n");
            sb.Append($"/{img.PdfName} Do\n");
            sb.Append("Q\n");
            return this;
        }

        // ── links and tooltips ────────────────────────────────────────────────

        /// <summary>Add a clickable hyperlink over the given area.</summary>
        public PdfCanvas AddLink(float x, float y, float width, float height, string uri)
        {
            _page.Annotations.Add(new PageAnnotation(x, PdfY(y + height), x + width, PdfY(y), uri, null));
            return this;
        }

        /// <summary>Add a tooltip (text annotation) over the given area.</summary>
        public PdfCanvas AddTooltip(float x, float y, float width, float height, string tooltip)
        {
            _page.Annotations.Add(new PageAnnotation(x, PdfY(y + height), x + width, PdfY(y), null, tooltip));
            return this;
        }

        // ── measurement ──────────────────────────────────────────────────────

        /// <summary>Returns the width of <paramref name="text"/> in points when rendered with <paramref name="style"/>.</summary>
        public float MeasureTextWidth(string text, TextStyle? style = null)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            style ??= TextStyle.Default;

            // Explicit TTF file
            if (style.FontFilePath != null)
                return _fontCache.GetOrLoad(style.FontFilePath).GetWidthPoint(text, style.FontSize);

            // Registry-resolved family (may have fallback segments)
            if (_registry != null)
            {
                var src = _registry.Resolve(style.FontFamily, style.IsBold, style.IsItalic);
                if (src != null)
                    return _fontCache.GetOrLoad(src).GetWidthPoint(text, style.FontSize);
            }

            return MeasureStandardFontWidth(EscapeWinAnsi(text), style);
        }

        // Width of already-WinAnsi-escaped text using AFM tables.
        private float MeasureStandardFontWidth(string escapedText, TextStyle style)
        {
            var widths = GetStandardFontWidths(style.FontFamily, style.IsBold, style.IsItalic);
            if (widths != null)
            {
                float total = 0f;
                foreach (char c in escapedText)
                {
                    int idx = (int)c;
                    total += (idx >= 0 && idx < widths.Length) ? widths[idx] : 556;
                }
                return total / 1000f * style.FontSize;
            }
            return escapedText.Length * style.FontSize * 0.5f;
        }

        // Returns per-character advance widths in 1/1000 em units for the named standard PDF font,
        // or null if the font name is not recognised.
        private static int[]? GetStandardFontWidths(string family, bool bold, bool italic)
        {
            // Helvetica / Helvetica-Bold / Helvetica-Oblique / Helvetica-BoldOblique
            if (family == "Helvetica" || family == "helvetica")
                return bold ? HelveticaBoldWidths : HelveticaWidths;

            // Times
            if (family == "Times-Roman" || family == "Times")
                return (bold && italic) ? TimesBoldItalicWidths
                     : bold ? TimesBoldWidths
                     : italic ? TimesItalicWidths
                                        : TimesRomanWidths;

            // Courier is monospaced — every glyph is 600
            if (family == "Courier" || family == "Courier-Bold"
                || family == "Courier-Oblique" || family == "Courier-BoldOblique")
                return CourierWidths;

            return null;
        }

        // AFM widths for WinAnsi positions 0-255 (missing/undefined positions use 278).
        // Source: Adobe Core 14 Fonts AFM files.

        private static readonly int[] HelveticaWidths = BuildHelveticaWidths(bold: false);
        private static readonly int[] HelveticaBoldWidths = BuildHelveticaWidths(bold: true);

        private static int[] BuildHelveticaWidths(bool bold)
        {
            // Widths for chars 32-127 (ASCII printable) — same for regular and bold
            // Bold widths are very close; we use the same table as a good approximation.
            var w = new int[256];
            for (int i = 0; i < 256; i++) w[i] = 278; // default
            // Standard ASCII 32-127
            int[] ascii = {
                278,278,355,556,556,889,667,191,333,333,389,584,278,333,278,278, // 32-47
                556,556,556,556,556,556,556,556,556,556,278,278,584,584,584,556, // 48-63
                1015,667,667,722,722,667,611,778,722,278,500,667,556,833,722,778,// 64-79
                667,778,722,667,611,722,667,944,667,667,611,278,278,278,469,556, // 80-95
                333,556,556,500,556,556,278,556,556,222,222,500,222,833,556,556, // 96-111
                556,556,333,500,278,556,500,722,500,500,500,334,260,334,584,333  // 112-127
            };
            Array.Copy(ascii, 0, w, 32, ascii.Length);
            // Selected WinAnsi 0x80-0x9F extras
            w[0x80] = 556; // €
            w[0x82] = 278; // ‚
            w[0x83] = 556; // ƒ
            w[0x84] = 278; // „
            w[0x85] = 1000;// …
            w[0x86] = 556; // †
            w[0x87] = 556; // ‡
            w[0x88] = 278; // ˆ
            w[0x89] = 1000;// ‰
            w[0x8A] = 667; // Š
            w[0x8B] = 278; // ‹
            w[0x8C] = 1000;// Œ
            w[0x8E] = 667; // Ž
            w[0x91] = 222; // '
            w[0x92] = 222; // '
            w[0x93] = 333; // "
            w[0x94] = 333; // "
            w[0x95] = 350; // •
            w[0x96] = 556; // –
            w[0x97] = 1000;// —
            w[0x98] = 333; // ˜
            w[0x99] = 1000;// ™
            w[0x9A] = 556; // š
            w[0x9B] = 278; // ›
            w[0x9C] = 1000;// œ
            w[0x9E] = 556; // ž
            w[0x9F] = 611; // Ÿ
            // Latin-1 supplement 0xA0-0xFF
            int[] lat1 = {
                278,333,556,556,556,556,260,556,333,737,370,556,584,333,737,278, // A0-AF
                400,584,333,333,333,556,537,278,333,333,365,556,834,834,834,611, // B0-BF
                667,667,667,667,667,667,1000,722,667,667,667,667,278,278,278,278,// C0-CF
                722,722,778,778,778,778,778,584,778,722,722,722,722,667,667,611, // D0-DF
                556,556,556,556,556,556,889,500,556,556,556,556,278,278,278,278, // E0-EF
                556,556,556,556,556,556,556,611,556,556,556,556,556,500,556,500  // F0-FF
            };
            Array.Copy(lat1, 0, w, 0xA0, lat1.Length);
            return w;
        }

        private static readonly int[] TimesRomanWidths = BuildTimesWidths(false, false);
        private static readonly int[] TimesBoldWidths = BuildTimesWidths(true, false);
        private static readonly int[] TimesItalicWidths = BuildTimesWidths(false, true);
        private static readonly int[] TimesBoldItalicWidths = BuildTimesWidths(true, true);

        private static int[] BuildTimesWidths(bool bold, bool italic)
        {
            var w = new int[256];
            for (int i = 0; i < 256; i++) w[i] = 250;
            // ASCII 32-127 — Times-Roman
            int[] ascii = {
                250,333,408,500,500,833,778,180,333,333,500,564,250,333,250,278, // 32-47
                500,500,500,500,500,500,500,500,500,500,278,278,564,564,564,444, // 48-63
                921,722,667,667,722,611,556,722,722,333,389,722,611,889,722,722, // 64-79
                556,722,667,556,611,722,722,944,722,722,611,333,278,333,469,500, // 80-95
                333,444,500,444,500,444,333,500,500,278,278,500,278,778,500,500, // 96-111
                500,500,333,389,278,500,500,722,500,500,444,480,200,480,541,333  // 112-127
            };
            if (bold)
            {
                int[] b = {
                    250,333,555,500,500,1000,833,278,333,333,500,570,250,333,250,278,
                    500,500,500,500,500,500,500,500,500,500,333,333,570,570,570,500,
                    930,722,667,722,722,667,611,778,778,389,500,778,667,944,722,778,
                    611,778,722,556,667,722,722,1000,722,722,667,333,278,333,581,500,
                    333,500,556,444,556,444,333,500,556,278,333,556,278,833,556,500,
                    556,556,444,389,333,556,500,722,500,500,444,394,220,394,520,333
                };
                Array.Copy(b, 0, w, 32, b.Length);
            }
            else if (italic)
            {
                int[] it = {
                    250,333,420,500,500,833,778,214,333,333,500,675,250,333,250,278,
                    500,500,500,500,500,500,500,500,500,500,333,333,675,675,675,500,
                    920,611,611,667,722,611,611,722,722,333,444,667,556,833,667,722,
                    611,722,611,500,556,722,611,833,611,556,556,389,278,389,422,500,
                    333,500,500,444,500,444,278,500,500,278,278,444,278,722,500,500,
                    500,500,389,389,278,500,444,667,444,444,389,400,275,400,541,333
                };
                Array.Copy(it, 0, w, 32, it.Length);
            }
            else
            {
                Array.Copy(ascii, 0, w, 32, ascii.Length);
            }
            return w;
        }

        private static readonly int[] CourierWidths = BuildCourierWidths();
        private static int[] BuildCourierWidths()
        {
            var w = new int[256];
            for (int i = 0; i < 256; i++) w[i] = 600; // monospaced
            return w;
        }

        // ── internal helpers ─────────────────────────────────────────────────

        private static string F(float v) => PdfSerializer.Fmt(v);

        private void AppendStrokeState(StrokeStyle style)
        {
            var sb = Content;
            sb.Append($"{F(style.Color.Rf)} {F(style.Color.Gf)} {F(style.Color.Bf)} RG\n");
            sb.Append($"{F(style.Width)} w\n");
            AppendDash(style.LineStyle);
        }

        private void AppendShapeState(ShapeStyle style)
        {
            var sb = Content;
            if (style.HasFill)
            {
                var c = style.FillColor!.Value;
                sb.Append($"{F(c.Rf)} {F(c.Gf)} {F(c.Bf)} rg\n");
            }
            if (style.HasStroke)
            {
                var c = style.StrokeColor!.Value;
                sb.Append($"{F(c.Rf)} {F(c.Gf)} {F(c.Bf)} RG\n");
                sb.Append($"{F(style.StrokeWidth)} w\n");
                AppendDash(style.LineStyle);
            }
        }

        private void AppendDash(LineStyle ls)
        {
            switch (ls)
            {
                case LineStyle.Dashed: Content.Append("[6 3] 0 d\n"); break;
                case LineStyle.Dotted: Content.Append("[1 3] 0 d\n"); break;
                default: Content.Append("[] 0 d\n"); break;
            }
        }

        private void ResetDash() => Content.Append("[] 0 d\n");

        private void AppendPaintOp(ShapeStyle style)
        {
            if (style.HasFill && style.HasStroke)
                Content.Append("B\n");
            else if (style.HasFill)
                Content.Append("f\n");
            else if (style.HasStroke)
                Content.Append("S\n");
            // else nothing (empty/invisible)
        }

        private static string ResolveStandardName(string family, bool bold, bool italic)
        {
            switch (family?.ToLowerInvariant())
            {
                case "times-roman":
                case "times":
                case "times new roman":
                    if (bold && italic) return "Times-BoldItalic";
                    if (bold) return "Times-Bold";
                    if (italic) return "Times-Italic";
                    return "Times-Roman";

                case "courier":
                case "courier new":
                    if (bold && italic) return "Courier-BoldOblique";
                    if (bold) return "Courier-Bold";
                    if (italic) return "Courier-Oblique";
                    return "Courier";

                case "symbol":
                    return "Symbol";

                case "zapfdingbats":
                case "zapf dingbats":
                    return "ZapfDingbats";

                default: // Helvetica / Arial / anything else
                    if (bold && italic) return "Helvetica-BoldOblique";
                    if (bold) return "Helvetica-Bold";
                    if (italic) return "Helvetica-Oblique";
                    return "Helvetica";
            }
        }

        // Windows-1252 maps certain Unicode codepoints to bytes 0x80–0x9F.
        // Latin-1 (ISO-8859-1) leaves that range undefined, so these characters
        // must be translated explicitly when encoding text for /WinAnsiEncoding.
        private static readonly System.Collections.Generic.Dictionary<char, char> Win1252Extras =
            new System.Collections.Generic.Dictionary<char, char>
            {
                { '€', '\x80' }, // €
                { '‚', '\x82' }, // ‚
                { 'ƒ', '\x83' }, // ƒ
                { '„', '\x84' }, // „
                { '…', '\x85' }, // …
                { '†', '\x86' }, // †
                { '‡', '\x87' }, // ‡
                { 'ˆ', '\x88' }, // ˆ
                { '‰', '\x89' }, // ‰
                { 'Š', '\x8A' }, // Š
                { '‹', '\x8B' }, // ‹
                { 'Œ', '\x8C' }, // Œ
                { 'Ž', '\x8E' }, // Ž
                { '‘', '\x91' }, // '
                { '’', '\x92' }, // '
                { '“', '\x93' }, // "
                { '”', '\x94' }, // "
                { '•', '\x95' }, // •
                { '–', '\x96' }, // –
                { '—', '\x97' }, // —
                { '˜', '\x98' }, // ˜
                { '™', '\x99' }, // ™
                { 'š', '\x9A' }, // š
                { '›', '\x9B' }, // ›
                { 'œ', '\x9C' }, // œ
                { 'ž', '\x9E' }, // ž
                { 'Ÿ', '\x9F' }, // Ÿ
            };

        // Overload used by MeasureTextWidth and tests — silent substitution with '?'.
        internal static string EscapeWinAnsi(string text) => EscapeWinAnsi(text, out _);

        // Returns the encoded string and the first character that had no WinAnsi mapping,
        // or null if all characters mapped successfully.
        internal static string EscapeWinAnsi(string text, out char? firstUnmappable)
        {
            firstUnmappable = null;
            var sb = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (c <= 0xFF)
                    sb.Append(c);
                else if (Win1252Extras.TryGetValue(c, out char mapped))
                    sb.Append(mapped);
                else
                {
                    firstUnmappable ??= c;
                    sb.Append('?');
                }
            }
            return sb.ToString();
        }
    }
}
