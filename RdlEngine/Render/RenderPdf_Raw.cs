/*
 * Copyright (C) 2025 Peter Gill <peter@majorsilence.com>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if DRAWINGCOMPAT
using Draw2 = Majorsilence.Drawing;
using Imaging = Majorsilence.Drawing.Imaging;
#else
using Draw2 = System.Drawing;
using Imaging = System.Drawing.Imaging;
#endif
using System.Text;
using Majorsilence.Pdf;
using Majorsilence.Reporting.Rdl.Utility;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// PDF renderer that writes PDF directly without any third-party PDF library.
    /// Supports: text (standard Type1 and embedded TrueType/OpenType), images (JPEG and
    /// raster), lines, rectangles, polygons, curves, ellipses, and hyperlink annotations.
    /// </summary>
    internal sealed class RenderPdf_Raw : RenderBase
    {
        // ── state ─────────────────────────────────────────────────────────────

        private PdfDocumentWriter _doc;
        private PdfPage _currentPage;

        // Font cache: path → parsed TTF (avoids re-parsing the same file twice)
        private readonly Dictionary<string, TrueTypeFont> _ttfCache =
            new Dictionary<string, TrueTypeFont>(StringComparer.OrdinalIgnoreCase);

        // Same platform/font-folder logic as the iTextSharp renderer
        private readonly int _osPlatform = (int)Environment.OSVersion.Platform;
        private bool _dejavuFonts;
        private bool _liberationFonts;

        private static readonly Dictionary<string, string> _embeddedFontMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Calibri"]   = "Carlito",
                ["Carlito"]   = "Carlito",
                ["Cambria"]   = "Caladea",
                ["Caladea"]   = "Caladea",
                ["Noto Sans"] = "NotoSans",
            };

        // ── construction ──────────────────────────────────────────────────────

        public RenderPdf_Raw(Report report, IStreamGen sg) : base(report, sg) { }

        // ── RenderBase abstract implementations ───────────────────────────────

        protected internal override void CreateDocument()
        {
            _doc = new PdfDocumentWriter();
            Report r = base.Report();
            _doc.SetMetadata(r.Author, r.Name, r.Description, "Majorsilence Reporting - RenderPdf_Raw");
        }

        protected internal override void EndDocument(Stream sg)
        {
            _doc.Write(sg);
            _ttfCache.Clear();
        }

        protected internal override void CreatePage()
        {
            _currentPage = _doc.AddPage(PageSize.xWidth, PageSize.yHeight);
        }

        protected internal override void AfterProcessPage() { }

        protected internal override void AddBookmark(PageText pt) { }

        // ── lines ─────────────────────────────────────────────────────────────

        protected internal override void AddLine(float x, float y, float x2, float y2,
            float width, Draw2.Color c, BorderStyleEnum ls)
        {
            if (width <= 0) return;
            var sb = _currentPage.Content;
            float H = PageSize.yHeight;

            sb.Append($"{Fmt(width)} w\n");
            sb.Append($"{FmtC(c.R)} {FmtC(c.G)} {FmtC(c.B)} RG\n");

            switch (ls)
            {
                case BorderStyleEnum.Dashed:
                    sb.Append($"[{Fmt(width * 3)} {Fmt(width)}] 0 d\n");
                    break;
                case BorderStyleEnum.Dotted:
                    sb.Append($"[{Fmt(width)}] 0 d\n");
                    break;
                default:
                    sb.Append("[] 0 d\n");
                    break;
            }

            sb.Append($"{Fmt(x)} {Fmt(H - y)} m {Fmt(x2)} {Fmt(H - y2)} l S\n");
        }

        // ── images ────────────────────────────────────────────────────────────

        protected internal override void AddImage(string name, StyleInfo si,
            Imaging.ImageFormat imf, float x, float y, float width, float height,
            Draw2.RectangleF clipRect, byte[] im, int samplesW, int samplesH,
            string url, string tooltip)
        {
            if (im == null || im.Length == 0) return;

            bool isJpeg = im.Length > 3 && im[0] == 0xFF && im[1] == 0xD8 && im[2] == 0xFF;
            byte[] imgData = im;

            if (!isJpeg)
            {
                // Decode to raw RGB for non-JPEG formats
                imgData = DecodeToRgb(im, ref samplesW, ref samplesH);
                if (imgData == null) return;
            }

            var imgRes = _doc.GetOrAddImage(imgData, samplesW, samplesH, isJpeg);
            if (!_currentPage.Images.Contains(imgRes))
                _currentPage.Images.Add(imgRes);

            float H  = PageSize.yHeight;
            float tx = x;
            float ty = H - y - height;

            var sb = _currentPage.Content;
            sb.Append("q\n");
            sb.Append($"{Fmt(width)} 0 0 {Fmt(height)} {Fmt(tx)} {Fmt(ty)} cm\n");
            sb.Append($"/{imgRes.PdfName} Do\n");
            sb.Append("Q\n");

            AddAnnotations(x, y, height, width, url, tooltip);
            iAddBorder(si, x, y, height, width);
        }

        // ── shapes ────────────────────────────────────────────────────────────

        protected internal override void AddPolygon(Draw2.PointF[] pts, StyleInfo si, string url)
        {
            if (si.BackgroundColor.IsEmpty || pts.Length < 2) return;
            var sb = _currentPage.Content;
            float H = PageSize.yHeight;
            Draw2.Color c = si.BackgroundColor;
            sb.Append($"{FmtC(c.R)} {FmtC(c.G)} {FmtC(c.B)} rg\n");
            sb.Append($"{Fmt(pts[0].X)} {Fmt(H - pts[0].Y)} m\n");
            for (int i = 1; i < pts.Length; i++)
                sb.Append($"{Fmt(pts[i].X)} {Fmt(H - pts[i].Y)} l\n");
            sb.Append("h B\n");
        }

        protected internal override void AddRectangle(float x, float y, float height, float width,
            StyleInfo si, string url, string tooltip)
        {
            if (height > 0 && width > 0 && !si.BackgroundColor.IsEmpty)
                iAddFillRect(x, y, width, height, si.BackgroundColor);

            iAddBorder(si, x, y, height, width);
            AddAnnotations(x, y, height, width, url, tooltip);
        }

        protected internal override void AddPie(float x, float y, float height, float width,
            StyleInfo si, string url, string tooltip)
        {
            if (height > 0 && width > 0 && !si.BackgroundColor.IsEmpty)
                iAddFillRect(x, y, width, height, si.BackgroundColor);

            iAddBorder(si, x, y, height, width);
            AddAnnotations(x, y, height, width, url, tooltip);
        }

        protected internal override void AddCurve(Draw2.PointF[] pts, StyleInfo si)
        {
            if (pts.Length > 2)
            {
                Draw2.PointF[] tangents = iGetCurveTangents(pts);
                iDoCurve(pts, tangents, si);
            }
            else if (pts.Length == 2)
            {
                AddLine(pts[0].X, pts[0].Y, pts[1].X, pts[1].Y, si);
            }
        }

        protected internal override void AddEllipse(float x, float y, float height, float width,
            StyleInfo si, string url)
        {
            // Approximate ellipse with 4 Bezier curves (Spiro constant k ≈ 0.5523)
            const float k = 0.5523f;
            float H  = PageSize.yHeight;
            float rx = width  / 2f;
            float ry = height / 2f;
            float cx = x + rx;
            float cy = H - (y + ry);

            var sb = _currentPage.Content;

            if (si.BStyleTop != BorderStyleEnum.None)
            {
                SetLineDash(si.BStyleTop);
                var bc = si.BColorTop;
                sb.Append($"{FmtC(bc.R)} {FmtC(bc.G)} {FmtC(bc.B)} RG\n");
            }

            // Build ellipse path
            sb.Append($"{Fmt(cx + rx)} {Fmt(cy)} m\n");
            sb.Append($"{Fmt(cx + rx)} {Fmt(cy + ry * k)} {Fmt(cx + rx * k)} {Fmt(cy + ry)} {Fmt(cx)} {Fmt(cy + ry)} c\n");
            sb.Append($"{Fmt(cx - rx * k)} {Fmt(cy + ry)} {Fmt(cx - rx)} {Fmt(cy + ry * k)} {Fmt(cx - rx)} {Fmt(cy)} c\n");
            sb.Append($"{Fmt(cx - rx)} {Fmt(cy - ry * k)} {Fmt(cx - rx * k)} {Fmt(cy - ry)} {Fmt(cx)} {Fmt(cy - ry)} c\n");
            sb.Append($"{Fmt(cx + rx * k)} {Fmt(cy - ry)} {Fmt(cx + rx)} {Fmt(cy - ry * k)} {Fmt(cx + rx)} {Fmt(cy)} c\n");

            if (!si.BackgroundColor.IsEmpty)
            {
                var fc = si.BackgroundColor;
                sb.Append($"{FmtC(fc.R)} {FmtC(fc.G)} {FmtC(fc.B)} rg\n");
                sb.Append("B\n");
            }
            else
            {
                sb.Append("S\n");
            }
        }

        // ── text ──────────────────────────────────────────────────────────────

        protected internal override void AddText(float x, float y, float height, float width,
            string[] sa, StyleInfo si, float[] tw, bool bWrap, string url, bool bNoClip, string tooltip)
        {
            if (sa == null || sa.Length == 0) return;

            // Resolve font (mirrors iTextSharp renderer logic)
            PdfFontResource fontRes = ResolveFont(si);
            if (fontRes == null) return;

            if (!_currentPage.Fonts.Contains(fontRes))
                _currentPage.Fonts.Add(fontRes);

            float H = PageSize.yHeight;

            // Draw background if needed
            if (!si.BackgroundColor.IsEmpty && height > 0 && width > 0)
                iAddFillRect(x, y, width, height, si.BackgroundColor);

            for (int i = 0; i < sa.Length; i++)
            {
                string text = sa[i];
                if (string.IsNullOrEmpty(text)) continue;

                float textwidth = GetTextWidth(text, fontRes, si.FontSize);

                float startX = x + si.PaddingLeft;
                float startY = y + si.PaddingTop + i * si.FontSize;

                if (si.WritingMode == WritingModeEnum.lr_tb)
                {
                    switch (si.TextAlign)
                    {
                        case TextAlignEnum.Center:
                            if (width > 0)
                                startX = x + si.PaddingLeft + (width - si.PaddingLeft - si.PaddingRight) / 2f - textwidth / 2f;
                            break;
                        case TextAlignEnum.Right:
                            if (width > 0)
                                startX = x + width - textwidth - si.PaddingRight;
                            break;
                    }

                    switch (si.VerticalAlign)
                    {
                        case VerticalAlignEnum.Middle:
                            if (height > 0)
                            {
                                startY = y + si.PaddingTop + (height - si.PaddingTop - si.PaddingBottom) / 2f - si.FontSize / 2f;
                                if (sa.Length > 1)
                                    startY += sa.Length % 2 == 0
                                        ? -(((sa.Length / 2) - i) * si.FontSize) + si.FontSize / 2f
                                        : -(((sa.Length / 2) - i) * si.FontSize);
                            }
                            break;
                        case VerticalAlignEnum.Bottom:
                            if (height > 0)
                                startY = y + height - si.PaddingBottom - si.FontSize * (sa.Length - i);
                            break;
                    }

                    RenderTextHorizontal(text, fontRes, si, startX, startY, H);
                }
                else
                {
                    // Vertical text (tb_rl): offset x slightly, rotate -90°
                    startX += si.FontSize / 4f;
                    switch (si.TextAlign)
                    {
                        case TextAlignEnum.Center:
                            if (height > 0)
                                startY = y + si.PaddingLeft + (height - si.PaddingLeft - si.PaddingRight) / 2f - textwidth / 2f;
                            break;
                        case TextAlignEnum.Right:
                            if (width > 0)
                                startY = y + height - textwidth - si.PaddingRight;
                            break;
                    }

                    RenderTextVertical(text, fontRes, si, startX, startY, H);
                }

                // Underline / overline / strikethrough
                float maxX = width > 0 ? Math.Min(x + width, startX + textwidth) : startX + textwidth;
                switch (si.TextDecoration)
                {
                    case TextDecorationEnum.Underline:
                        AddLine(startX, startY + si.FontSize + 1, maxX, startY + si.FontSize + 1, 1, si.Color, BorderStyleEnum.Solid);
                        break;
                    case TextDecorationEnum.LineThrough:
                        AddLine(startX, startY + si.FontSize / 2f + 1, maxX, startY + si.FontSize / 2f + 1, 1, si.Color, BorderStyleEnum.Solid);
                        break;
                    case TextDecorationEnum.Overline:
                        AddLine(startX, startY + 1, maxX, startY + 1, 1, si.Color, BorderStyleEnum.Solid);
                        break;
                }
            }

            AddAnnotations(x, y, height, width, url, tooltip);
            iAddBorder(si, x, y, height, width);
        }

        // ── private: text rendering ───────────────────────────────────────────

        private void RenderTextHorizontal(string text, PdfFontResource font, StyleInfo si,
            float startX, float startY, float H)
        {
            float textY = H - startY - si.FontSize;
            string encoded = PdfDocumentWriter.EncodeText(text, font);
            var sb = _currentPage.Content;
            var c = si.Color;
            sb.Append("BT\n");
            sb.Append($"/{font.PdfName} {Fmt(si.FontSize)} Tf\n");
            sb.Append($"{FmtC(c.R)} {FmtC(c.G)} {FmtC(c.B)} rg\n");
            sb.Append($"1 0 0 1 {Fmt(startX)} {Fmt(textY)} Tm\n");
            sb.Append($"{encoded} Tj\n");
            sb.Append("ET\n");
        }

        private void RenderTextVertical(string text, PdfFontResource font, StyleInfo si,
            float startX, float startY, float H)
        {
            // Rotate -90° (cos=-1°≈-283°/180°≈ -(π+π/3) — mirror iTextSharp value)
            double rads    = -283.0 / 180.0 * Math.PI;
            double radsCos = Math.Cos(rads);
            double radsSin = Math.Sin(rads);
            string encoded = PdfDocumentWriter.EncodeText(text, font);
            var sb = _currentPage.Content;
            var c = si.Color;
            sb.Append("BT\n");
            sb.Append($"/{font.PdfName} {Fmt(si.FontSize)} Tf\n");
            sb.Append($"{FmtC(c.R)} {FmtC(c.G)} {FmtC(c.B)} rg\n");
            sb.Append($"{Fmt(radsCos)} {Fmt(radsSin)} {Fmt(-radsSin)} {Fmt(radsCos)} {Fmt(startX)} {Fmt(H - startY)} Tm\n");
            sb.Append($"{encoded} Tj\n");
            sb.Append("ET\n");
        }

        // ── private: font resolution ──────────────────────────────────────────

        private string FontFolder
        {
            get
            {
                bool isOSX = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                    System.Runtime.InteropServices.OSPlatform.OSX);

                if (isOSX) return "/System/Library/Fonts/Supplemental";

                if (_osPlatform == (int)PlatformID.Unix)
                {
                    if (Directory.Exists("/usr/share/fonts/truetype/msttcorefonts"))
                        return "/usr/share/fonts/truetype/msttcorefonts";
                    if (Directory.Exists("/usr/share/fonts/truetype/liberation"))
                    { _liberationFonts = true; return "/usr/share/fonts/truetype/liberation"; }
                    if (Directory.Exists("/usr/share/fonts/truetype/dejavu"))
                    { _dejavuFonts = true; return "/usr/share/fonts/truetype/dejavu"; }
                    _liberationFonts = true;
#if DRAWINGCOMPAT
                    return Majorsilence.Drawing.FontResourceLoader.GetFontDirectory();
#else
                    return "/usr/share/fonts";
#endif
                }

                // Windows: %WINDIR%\Fonts
                DirectoryInfo winDir = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.System));
                return Path.Combine(winDir.FullName, "Fonts");
            }
        }

        private bool IsOSX => System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.OSX);

        private static string NormalizeFace(string face)
        {
            switch (face?.ToLowerInvariant())
            {
                case "times": case "times-roman": case "times roman":
                case "timesnewroman": case "times new roman":
                case "timesnewromanps": case "timesnewromanpsmt": case "serif":
                    return "Times-Roman";

                case "courier": case "couriernew": case "courier new":
                case "couriernewpsmt": case "monospace":
                    return "Courier New";

                case "symbol":    return "Symbol";
                case "zapfdingbats": case "wingdings": case "wingding":
                    return "ZapfDingbats";

                default: return face;
            }
        }

        private PdfFontResource ResolveFont(StyleInfo si)
        {
            bool bold   = si.IsFontBold();
            bool italic = si.FontStyle == FontStyleEnum.Italic;
            string face = NormalizeFace(si.FontFamily);
            string folder = FontFolder; // also sets _liberationFonts / _dejavuFonts

            // ── standard Type1 fonts (no embedding needed) ───────────────────
            if (face == "Times-Roman")
            {
                string std;
                string ttfFile;
                if      (bold && italic) { std = IsOSX ? "TimesNewRomanPS-BoldItalicMT"  : (_liberationFonts ? "Liberation Serif Bold Italic"   : _dejavuFonts ? "DejaVu Serif Condensed Bold Italic"   : "Times-BoldItalic"); ttfFile = IsOSX ? "Times New Roman Bold Italic.ttf"  : (_liberationFonts ? "LiberationSerif-BoldItalic.ttf"  : _dejavuFonts ? "DejaVuSerifCondensed-BoldItalic.ttf"  : "timesbi.ttf"); }
                else if (bold)           { std = IsOSX ? "TimesNewRomanPS-BoldMT"         : (_liberationFonts ? "Liberation Serif Bold"           : _dejavuFonts ? "DejaVu Serif Condensed Bold"           : "Times-Bold");       ttfFile = IsOSX ? "Times New Roman Bold.ttf"         : (_liberationFonts ? "LiberationSerif-Bold.ttf"          : _dejavuFonts ? "DejaVuSerifCondensed-Bold.ttf"          : "timesbd.ttf"); }
                else if (italic)         { std = IsOSX ? "TimesNewRomanPS-ItalicMT"       : (_liberationFonts ? "Liberation Serif Italic"         : _dejavuFonts ? "DejaVu Serif Condensed Italic"         : "Times-Italic");     ttfFile = IsOSX ? "Times New Roman Italic.ttf"       : (_liberationFonts ? "LiberationSerif-Italic.ttf"        : _dejavuFonts ? "DejaVuSerifCondensed-Italic.ttf"        : "timesi.ttf"); }
                else                     { std = IsOSX ? "TimesNewRomanPSMT"              : (_liberationFonts ? "Liberation Serif"                 : _dejavuFonts ? "DejaVu Serif Condensed"                 : "Times-Roman");      ttfFile = IsOSX ? "Times New Roman.ttf"              : (_liberationFonts ? "LiberationSerif-Regular.ttf"       : _dejavuFonts ? "DejaVuSerifCondensed.ttf"               : "times.ttf"); }

                string path = Path.Combine(folder, ttfFile);
                if (File.Exists(path)) return GetOrLoadTtfFont(path);
                return _doc.GetOrAddStandardFont(std);
            }

            if (face == "Symbol")      return _doc.GetOrAddStandardFont("Symbol");
            if (face == "ZapfDingbats") return _doc.GetOrAddStandardFont("ZapfDingbats");

            if (face == "Courier New")
            {
                string ttfFile;
                if      (bold && italic) ttfFile = IsOSX ? "Courier New Bold Italic.ttf" : (_liberationFonts ? "LiberationMono-BoldItalic.ttf"    : _dejavuFonts ? "DejaVuSansMono-BoldOblique.ttf" : "courbi.ttf");
                else if (bold)           ttfFile = IsOSX ? "Courier New Bold.ttf"        : (_liberationFonts ? "LiberationMono-Bold.ttf"            : _dejavuFonts ? "DejaVuSansMono-Bold.ttf"        : "courbd.ttf");
                else if (italic)         ttfFile = IsOSX ? "Courier New Italic.ttf"      : (_liberationFonts ? "LiberationMono-Italic.ttf"          : _dejavuFonts ? "DejaVuSansMono-Oblique.ttf"     : "couri.ttf");
                else                     ttfFile = IsOSX ? "Courier New.ttf"             : (_liberationFonts ? "LiberationMono-Regular.ttf"         : _dejavuFonts ? "DejaVuSansMono.ttf"             : "cour.ttf");

                string path = Path.Combine(folder, ttfFile);
                if (File.Exists(path)) return GetOrLoadTtfFont(path);
                string stdName = bold && italic ? "Courier-BoldOblique" : bold ? "Courier-Bold" : italic ? "Courier-Oblique" : "Courier";
                return _doc.GetOrAddStandardFont(stdName);
            }

            // ── Arial / Helvetica ─────────────────────────────────────────────
            {
                string ttfFile;
                if      (bold && italic) ttfFile = IsOSX ? "Arial Bold Italic.ttf" : (_liberationFonts ? "LiberationSans-BoldItalic.ttf"  : _dejavuFonts ? "DejaVuSansCondensed-BoldOblique.ttf" : "arialbi.ttf");
                else if (bold)           ttfFile = IsOSX ? "Arial Bold.ttf"        : (_liberationFonts ? "LiberationSans-Bold.ttf"          : _dejavuFonts ? "DejaVuSansCondensed-Bold.ttf"        : "arialbd.ttf");
                else if (italic)         ttfFile = IsOSX ? "Arial Italic.ttf"      : (_liberationFonts ? "LiberationSans-Italic.ttf"        : _dejavuFonts ? "DejaVuSansCondensed-Oblique.ttf"     : "ariali.ttf");
                else                     ttfFile = IsOSX ? "Arial.ttf"             : (_liberationFonts ? "LiberationSans-Regular.ttf"       : _dejavuFonts ? "DejaVuSansCondensed.ttf"             : "arial.ttf");

                // Try the named face first (user-specified font, e.g. "Calibri")
                if (face != "Arial" && face != "Helvetica")
                {
                    // Check embedded font map (Carlito for Calibri, etc.)
                    if (_embeddedFontMap.TryGetValue(face, out string baseName))
                    {
                        string suffix = bold && italic ? "BoldItalic" : bold ? "Bold" : italic ? "Italic" : "Regular";
                        string embFile = $"{baseName}-{suffix}.ttf";
#if DRAWINGCOMPAT
                        string embFolder = Majorsilence.Drawing.FontResourceLoader.GetFontDirectory();
#else
                        string embFolder = folder;
#endif
                        string embPath = Path.Combine(embFolder, embFile);
                        if (File.Exists(embPath)) return GetOrLoadTtfFont(embPath);
                    }

                    // Try the named font in the system font folder
                    string[] candidates = new[]
                    {
                        Path.Combine(folder, face + ".ttf"),
                        Path.Combine(folder, face.Replace(" ", "") + ".ttf"),
                        Path.Combine(folder, face.ToLowerInvariant() + ".ttf"),
                    };
                    foreach (string c in candidates)
                        if (File.Exists(c)) return GetOrLoadTtfFont(c);

                    // Fall through to Arial/Liberation
                }

                string arialPath = Path.Combine(folder, ttfFile);
                if (File.Exists(arialPath)) return GetOrLoadTtfFont(arialPath);

                // Last resort: use Helvetica standard Type1
                string helvName = bold && italic ? "Helvetica-BoldOblique" : bold ? "Helvetica-Bold" : italic ? "Helvetica-Oblique" : "Helvetica";
                return _doc.GetOrAddStandardFont(helvName);
            }
        }

        private PdfFontResource GetOrLoadTtfFont(string path)
        {
            if (!_ttfCache.TryGetValue(path, out var ttf))
            {
                ttf = new TrueTypeFont(path);
                _ttfCache[path] = ttf;
            }
            return _doc.GetOrAddTtfFont(path, ttf);
        }

        // ── private: drawing helpers ──────────────────────────────────────────

        private void iAddFillRect(float x, float y, float width, float height, Draw2.Color c)
        {
            var sb = _currentPage.Content;
            float H = PageSize.yHeight;
            sb.Append($"{FmtC(c.R)} {FmtC(c.G)} {FmtC(c.B)} rg\n");
            sb.Append($"{Fmt(x)} {Fmt(H - y - height)} {Fmt(width)} {Fmt(height)} re f\n");
        }

        private void iAddBorder(StyleInfo si, float x, float y, float height, float width)
        {
            if (height <= 0 || width <= 0) return;
            float xr = x + width, yb = y + height;
            if (si.BStyleTop    != BorderStyleEnum.None && si.BWidthTop    > 0) AddLine(x,  y,  xr, y,  si.BWidthTop,    si.BColorTop,    si.BStyleTop);
            if (si.BStyleRight  != BorderStyleEnum.None && si.BWidthRight  > 0) AddLine(xr, y,  xr, yb, si.BWidthRight,  si.BColorRight,  si.BStyleRight);
            if (si.BStyleLeft   != BorderStyleEnum.None && si.BWidthLeft   > 0) AddLine(x,  y,  x,  yb, si.BWidthLeft,   si.BColorLeft,   si.BStyleLeft);
            if (si.BStyleBottom != BorderStyleEnum.None && si.BWidthBottom > 0) AddLine(x,  yb, xr, yb, si.BWidthBottom, si.BColorBottom, si.BStyleBottom);
        }

        private void AddAnnotations(float x, float y, float height, float width, string url, string tooltip)
        {
            float H = PageSize.yHeight;
            if (url != null)
                _currentPage.Annots.Add(new PdfAnnotation
                {
                    X1 = x, Y1 = H - y - height, X2 = x + width, Y2 = H - y, Uri = url
                });
            if (!string.IsNullOrEmpty(tooltip) && url == null)
                _currentPage.Annots.Add(new PdfAnnotation
                {
                    X1 = x, Y1 = H - y - height, X2 = x + width, Y2 = H - y, Tooltip = tooltip
                });
        }

        private void SetLineDash(BorderStyleEnum ls)
        {
            var sb = _currentPage.Content;
            switch (ls)
            {
                case BorderStyleEnum.Dashed: sb.Append("[6 2] 0 d\n"); break;
                case BorderStyleEnum.Dotted: sb.Append("[2 2] 0 d\n"); break;
                default:                     sb.Append("[] 0 d\n");    break;
            }
        }

        // ── private: curve helpers (mirrors iTextSharp renderer) ──────────────

        private void iAddCurve(float x0, float y0, float x1, float y1,
                                float x2, float y2, float x3, float y3,
                                StyleInfo si)
        {
            float H  = PageSize.yHeight;
            var sb = _currentPage.Content;

            SetLineDash(si.BStyleTop);
            if (si.BStyleTop != BorderStyleEnum.None)
            {
                var bc = si.BColorTop;
                sb.Append($"{FmtC(bc.R)} {FmtC(bc.G)} {FmtC(bc.B)} RG\n");
            }

            sb.Append($"{Fmt(x0)} {Fmt(H - y0)} m\n");
            sb.Append($"{Fmt(x1)} {Fmt(H - y1)} {Fmt(x2)} {Fmt(H - y2)} {Fmt(x3)} {Fmt(H - y3)} c\n");

            if (!si.BackgroundColor.IsEmpty)
            {
                var fc = si.BackgroundColor;
                sb.Append($"{FmtC(fc.R)} {FmtC(fc.G)} {FmtC(fc.B)} rg\n");
                sb.Append("B\n");
            }
            else
            {
                sb.Append("S\n");
            }
        }

        private void iDoCurve(Draw2.PointF[] pts, Draw2.PointF[] tangents, StyleInfo si)
        {
            for (int i = 0; i < pts.Length - 1; i++)
            {
                int j = i + 1;
                iAddCurve(pts[i].X, pts[i].Y,
                           pts[i].X + tangents[i].X, pts[i].Y + tangents[i].Y,
                           pts[j].X - tangents[j].X, pts[j].Y - tangents[j].Y,
                           pts[j].X, pts[j].Y, si);
            }
        }

        private static Draw2.PointF[] iGetCurveTangents(Draw2.PointF[] pts)
        {
            const float coeff = 0.5f / 3f;
            var tangents = new Draw2.PointF[pts.Length];
            if (pts.Length <= 2) return tangents;
            for (int i = 0; i < pts.Length; i++)
            {
                int r = Math.Min(i + 1, pts.Length - 1);
                int s = Math.Max(i - 1, 0);
                tangents[i].X = coeff * (pts[r].X - pts[s].X);
                tangents[i].Y = coeff * (pts[r].Y - pts[s].Y);
            }
            return tangents;
        }

        // ── private: text width ────────────────────────────────────────────────

        private static float GetTextWidth(string text, PdfFontResource font, float fontSize)
        {
            if (font.IsStandard14)
                return fontSize * 0.55f * text.Length; // rough approximation for standard fonts
            return font.Ttf.GetWidthPoint(text, fontSize);
        }

        // ── private: image decoding ────────────────────────────────────────────

        private static byte[] DecodeToRgb(byte[] data, ref int samplesW, ref int samplesH)
        {
            try
            {
                using var ms  = new MemoryStream(data);
#if DRAWINGCOMPAT
                using var img = (Majorsilence.Drawing.Bitmap)Majorsilence.Drawing.Image.FromStream(ms);
#else
                using var img = new Draw2.Bitmap(Draw2.Image.FromStream(ms));
#endif
                samplesW = img.Width;
                samplesH = img.Height;
                var rgb = new byte[samplesW * samplesH * 3];
                int idx = 0;
                for (int row = 0; row < samplesH; row++)
                {
                    for (int col = 0; col < samplesW; col++)
                    {
                        var px = img.GetPixel(col, row);
                        rgb[idx++] = px.R;
                        rgb[idx++] = px.G;
                        rgb[idx++] = px.B;
                    }
                }
                return rgb;
            }
            catch
            {
                return null;
            }
        }

        // ── private: PDF value formatters ─────────────────────────────────────

        private static string Fmt(float v)  => PdfDocumentWriter.Fmt(v);
        private static string Fmt(double v) => PdfDocumentWriter.Fmt(v);

        // Normalize a byte color channel to a 0–1 PDF real
        private static string FmtC(byte v) =>
            (v / 255f).ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
    }
}
