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
using Majorsilence.Pdf;
using Majorsilence.Reporting.Rdl.Utility;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// PDF renderer that writes PDF directly using the Majorsilence.Pdf library,
    /// without any third-party PDF dependency.
    /// </summary>
    internal sealed class RenderPdf_Raw : RenderBase
    {
        // ── state ─────────────────────────────────────────────────────────────

        private PdfDocument _doc;
        private PdfCanvas _currentPage;

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
            Report r = base.Report();
            _doc = PdfDocument.Create()
                .WithAuthor(r.Author ?? "")
                .WithTitle(r.Name   ?? "")
                .WithSubject(r.Description ?? "")
                .WithCreator("Majorsilence Reporting - RenderPdf_Raw");
        }

        protected internal override void EndDocument(Stream sg)
        {
            _doc.Save(sg);
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
            var style = StrokeStyle.Default
                .WithWidth(width)
                .WithColor(PdfColor.FromRgb(c.R, c.G, c.B))
                .WithLineStyle(ConvertLineStyle(ls));
            _currentPage.DrawLine(x, y, x2, y2, style);
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
                imgData = DecodeToRgb(im, ref samplesW, ref samplesH);
                if (imgData == null) return;
            }

            _currentPage.DrawImage(imgData, samplesW, samplesH, isJpeg, x, y, width, height);

            AddAnnotations(x, y, height, width, url, tooltip);
            iAddBorder(si, x, y, height, width);
        }

        // ── shapes ────────────────────────────────────────────────────────────

        protected internal override void AddPolygon(Draw2.PointF[] pts, StyleInfo si, string url)
        {
            if (si.BackgroundColor.IsEmpty || pts.Length < 2) return;
            var c = si.BackgroundColor;
            var style = ShapeStyle.Filled(PdfColor.FromRgb(c.R, c.G, c.B));
            var points = new List<(float x, float y)>(pts.Length);
            foreach (var p in pts) points.Add((p.X, p.Y));
            _currentPage.DrawPolygon(points, style);
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
            if (pts.Length >= 2)
            {
                var style = StrokeStyle.Default
                    .WithColor(si.BStyleTop != BorderStyleEnum.None
                        ? PdfColor.FromRgb(si.BColorTop.R, si.BColorTop.G, si.BColorTop.B)
                        : PdfColor.Black)
                    .WithLineStyle(ConvertLineStyle(si.BStyleTop));

                var points = new List<(float x, float y)>(pts.Length);
                foreach (var p in pts) points.Add((p.X, p.Y));
                _currentPage.DrawCurve(points, style);
            }
        }

        protected internal override void AddEllipse(float x, float y, float height, float width,
            StyleInfo si, string url)
        {
            var style = BuildShapeStyle(si);
            _currentPage.DrawEllipse(x, y, width, height, style);
        }

        // ── text ──────────────────────────────────────────────────────────────

        protected internal override void AddText(float x, float y, float height, float width,
            string[] sa, StyleInfo si, float[] tw, bool bWrap, string url, bool bNoClip, string tooltip)
        {
            if (sa == null || sa.Length == 0) return;

            TextStyle baseStyle = ResolveFont(si);

            if (!si.BackgroundColor.IsEmpty && height > 0 && width > 0)
                iAddFillRect(x, y, width, height, si.BackgroundColor);

            for (int i = 0; i < sa.Length; i++)
            {
                string text = sa[i];
                if (string.IsNullOrEmpty(text)) continue;

                float textwidth = _currentPage.MeasureTextWidth(text, baseStyle);

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

                    // DrawText baseline is at startY + FontSize in top-left coordinates
                    _currentPage.DrawText(text, startX, startY + si.FontSize, baseStyle);

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
                else
                {
                    // Vertical text: render rotated
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

                    _currentPage.DrawText(text, startX, startY + si.FontSize, baseStyle.WithVertical());
                }
            }

            AddAnnotations(x, y, height, width, url, tooltip);
            iAddBorder(si, x, y, height, width);
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

        private TextStyle ResolveFont(StyleInfo si)
        {
            bool bold   = si.IsFontBold();
            bool italic = si.FontStyle == FontStyleEnum.Italic;
            string face = NormalizeFace(si.FontFamily);
            string folder = FontFolder; // also sets _liberationFonts / _dejavuFonts

            var color = PdfColor.FromRgb(si.Color.R, si.Color.G, si.Color.B);
            var baseStyle = TextStyle.Default
                .WithSize(si.FontSize)
                .WithColor(color)
                .WithBold(bold)
                .WithItalic(italic);

            // ── standard Type1 fonts (no embedding needed) ───────────────────
            if (face == "Times-Roman")
            {
                string std, ttfFile;
                if (bold && italic)
                {
                    std     = IsOSX ? "TimesNewRomanPS-BoldItalicMT"  : (_liberationFonts ? "Liberation Serif Bold Italic"  : _dejavuFonts ? "DejaVu Serif Condensed Bold Italic"  : "Times-BoldItalic");
                    ttfFile = IsOSX ? "Times New Roman Bold Italic.ttf": (_liberationFonts ? "LiberationSerif-BoldItalic.ttf" : _dejavuFonts ? "DejaVuSerifCondensed-BoldItalic.ttf" : "timesbi.ttf");
                }
                else if (bold)
                {
                    std     = IsOSX ? "TimesNewRomanPS-BoldMT"         : (_liberationFonts ? "Liberation Serif Bold"          : _dejavuFonts ? "DejaVu Serif Condensed Bold"          : "Times-Bold");
                    ttfFile = IsOSX ? "Times New Roman Bold.ttf"        : (_liberationFonts ? "LiberationSerif-Bold.ttf"        : _dejavuFonts ? "DejaVuSerifCondensed-Bold.ttf"        : "timesbd.ttf");
                }
                else if (italic)
                {
                    std     = IsOSX ? "TimesNewRomanPS-ItalicMT"        : (_liberationFonts ? "Liberation Serif Italic"         : _dejavuFonts ? "DejaVu Serif Condensed Italic"         : "Times-Italic");
                    ttfFile = IsOSX ? "Times New Roman Italic.ttf"       : (_liberationFonts ? "LiberationSerif-Italic.ttf"       : _dejavuFonts ? "DejaVuSerifCondensed-Italic.ttf"       : "timesi.ttf");
                }
                else
                {
                    std     = IsOSX ? "TimesNewRomanPSMT"                : (_liberationFonts ? "Liberation Serif"                 : _dejavuFonts ? "DejaVu Serif Condensed"                 : "Times-Roman");
                    ttfFile = IsOSX ? "Times New Roman.ttf"               : (_liberationFonts ? "LiberationSerif-Regular.ttf"       : _dejavuFonts ? "DejaVuSerifCondensed.ttf"               : "times.ttf");
                }
                string path = Path.Combine(folder, ttfFile);
                if (File.Exists(path)) return baseStyle.WithFontFile(path);
                return baseStyle.WithFamily(std);
            }

            if (face == "Symbol")       return baseStyle.WithFamily("Symbol");
            if (face == "ZapfDingbats") return baseStyle.WithFamily("ZapfDingbats");

            if (face == "Courier New")
            {
                string ttfFile;
                if      (bold && italic) ttfFile = IsOSX ? "Courier New Bold Italic.ttf" : (_liberationFonts ? "LiberationMono-BoldItalic.ttf"  : _dejavuFonts ? "DejaVuSansMono-BoldOblique.ttf" : "courbi.ttf");
                else if (bold)           ttfFile = IsOSX ? "Courier New Bold.ttf"        : (_liberationFonts ? "LiberationMono-Bold.ttf"          : _dejavuFonts ? "DejaVuSansMono-Bold.ttf"        : "courbd.ttf");
                else if (italic)         ttfFile = IsOSX ? "Courier New Italic.ttf"      : (_liberationFonts ? "LiberationMono-Italic.ttf"        : _dejavuFonts ? "DejaVuSansMono-Oblique.ttf"     : "couri.ttf");
                else                     ttfFile = IsOSX ? "Courier New.ttf"             : (_liberationFonts ? "LiberationMono-Regular.ttf"       : _dejavuFonts ? "DejaVuSansMono.ttf"             : "cour.ttf");

                string path = Path.Combine(folder, ttfFile);
                if (File.Exists(path)) return baseStyle.WithFontFile(path);
                string stdName = bold && italic ? "Courier-BoldOblique" : bold ? "Courier-Bold" : italic ? "Courier-Oblique" : "Courier";
                return baseStyle.WithFamily(stdName);
            }

            // ── Arial / Helvetica and user-specified faces ────────────────────
            {
                string ttfFile;
                if      (bold && italic) ttfFile = IsOSX ? "Arial Bold Italic.ttf" : (_liberationFonts ? "LiberationSans-BoldItalic.ttf"  : _dejavuFonts ? "DejaVuSansCondensed-BoldOblique.ttf" : "arialbi.ttf");
                else if (bold)           ttfFile = IsOSX ? "Arial Bold.ttf"        : (_liberationFonts ? "LiberationSans-Bold.ttf"          : _dejavuFonts ? "DejaVuSansCondensed-Bold.ttf"        : "arialbd.ttf");
                else if (italic)         ttfFile = IsOSX ? "Arial Italic.ttf"      : (_liberationFonts ? "LiberationSans-Italic.ttf"        : _dejavuFonts ? "DejaVuSansCondensed-Oblique.ttf"     : "ariali.ttf");
                else                     ttfFile = IsOSX ? "Arial.ttf"             : (_liberationFonts ? "LiberationSans-Regular.ttf"       : _dejavuFonts ? "DejaVuSansCondensed.ttf"             : "arial.ttf");

                if (face != "Arial" && face != "Helvetica")
                {
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
                        if (File.Exists(embPath)) return baseStyle.WithFontFile(embPath);
                    }

                    string[] candidates = new[]
                    {
                        Path.Combine(folder, face + ".ttf"),
                        Path.Combine(folder, face.Replace(" ", "") + ".ttf"),
                        Path.Combine(folder, face.ToLowerInvariant() + ".ttf"),
                    };
                    foreach (string c in candidates)
                        if (File.Exists(c)) return baseStyle.WithFontFile(c);
                }

                string arialPath = Path.Combine(folder, ttfFile);
                if (File.Exists(arialPath)) return baseStyle.WithFontFile(arialPath);

                string helvName = bold && italic ? "Helvetica-BoldOblique" : bold ? "Helvetica-Bold" : italic ? "Helvetica-Oblique" : "Helvetica";
                return baseStyle.WithFamily(helvName);
            }
        }

        // ── private: drawing helpers ──────────────────────────────────────────

        private void iAddFillRect(float x, float y, float width, float height, Draw2.Color c)
        {
            _currentPage.DrawRectangle(x, y, width, height,
                ShapeStyle.Filled(PdfColor.FromRgb(c.R, c.G, c.B)));
        }

        private ShapeStyle BuildShapeStyle(StyleInfo si)
        {
            ShapeStyle style = ShapeStyle.Empty;
            if (!si.BackgroundColor.IsEmpty)
            {
                var bc = si.BackgroundColor;
                style = style.WithFill(PdfColor.FromRgb(bc.R, bc.G, bc.B));
            }
            if (si.BStyleTop != BorderStyleEnum.None && si.BWidthTop > 0)
            {
                var sc = si.BColorTop;
                style = style.WithStroke(PdfColor.FromRgb(sc.R, sc.G, sc.B), si.BWidthTop)
                             .WithLineStyle(ConvertLineStyle(si.BStyleTop));
            }
            return style;
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
            if (!string.IsNullOrEmpty(url))
                _currentPage.AddLink(x, y, width, height, url);
            else if (!string.IsNullOrEmpty(tooltip))
                _currentPage.AddTooltip(x, y, width, height, tooltip);
        }

        private static LineStyle ConvertLineStyle(BorderStyleEnum ls)
        {
            switch (ls)
            {
                case BorderStyleEnum.Dashed: return LineStyle.Dashed;
                case BorderStyleEnum.Dotted: return LineStyle.Dotted;
                default:                     return LineStyle.Solid;
            }
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
    }
}
