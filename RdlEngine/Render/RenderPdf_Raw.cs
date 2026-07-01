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
using Majorsilence.Pdf.Security;
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
        private PdfCanvas   _currentPage;
        private FontRegistry _fontRegistry;
        private readonly PdfSecurity? _security;
        private readonly PdfSignatureOptions? _signature;

        private readonly int _osPlatform = (int)Environment.OSVersion.Platform;
        private bool _dejavuFonts;
        private bool _liberationFonts;

        // RDL face names that map to a metric-compatible bundled family
        private static readonly Dictionary<string, string> _embeddedFontMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Calibri"]   = "Carlito",
                ["Carlito"]   = "Carlito",
                ["Cambria"]   = "Caladea",
                ["Caladea"]   = "Caladea",
                ["Noto Sans"] = "NotoSans",
                ["NotoSans"]  = "NotoSans",
            };

        // ── construction ──────────────────────────────────────────────────────

        public RenderPdf_Raw(Report report, IStreamGen sg,
            PdfSecurity? security = null,
            PdfSignatureOptions? signature = null)
            : base(report, sg)
        {
            _security  = security;
            _signature = signature;
        }

        // ── RenderBase abstract implementations ───────────────────────────────

        protected internal override void CreateDocument()
        {
            _fontRegistry = BuildFontRegistry();

            Report r = base.Report();
            _doc = PdfDocument.Create()
                .WithAuthor(r.Author ?? "")
                .WithTitle(r.Name   ?? "")
                .WithSubject(r.Description ?? "")
                .WithCreator("Majorsilence Reporting - RenderPdf_Raw")
                .WithFontRegistry(_fontRegistry);

            if (_security  != null) _doc.WithSecurity(_security);
            if (_signature != null) _doc.WithSignature(_signature);
        }

        protected internal override void EndDocument(Stream sg) => _doc.Save(sg);

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
            _currentPage.DrawLine(x, y, x2, y2,
                StrokeStyle.Default
                    .WithWidth(width)
                    .WithColor(PdfColor.FromRgb(c.R, c.G, c.B))
                    .WithLineStyle(ConvertLineStyle(ls)));
        }

        // ── images ────────────────────────────────────────────────────────────

        protected internal override void AddImage(string name, StyleInfo si,
            Imaging.ImageFormat imf, float x, float y, float width, float height,
            Draw2.RectangleF clipRect, byte[] im, int samplesW, int samplesH,
            string url, string tooltip)
        {
            if (im == null || im.Length == 0) return;

            bool isJpeg = im.Length > 3 && im[0] == 0xFF && im[1] == 0xD8 && im[2] == 0xFF;
            byte[] imgData = isJpeg ? im : DecodeToRgb(im, ref samplesW, ref samplesH);
            if (imgData == null) return;

            _currentPage.DrawImage(imgData, samplesW, samplesH, isJpeg, x, y, width, height);
            AddAnnotations(x, y, height, width, url, tooltip);
            iAddBorder(si, x, y, height, width);
        }

        // ── shapes ────────────────────────────────────────────────────────────

        protected internal override void AddPolygon(Draw2.PointF[] pts, StyleInfo si, string url)
        {
            if (si.BackgroundColor.IsEmpty || pts.Length < 2) return;
            var c = si.BackgroundColor;
            var points = new List<(float x, float y)>(pts.Length);
            foreach (var p in pts) points.Add((p.X, p.Y));
            _currentPage.DrawPolygon(points,
                ShapeStyle.Filled(PdfColor.FromRgb(c.R, c.G, c.B)));
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
            if (pts.Length < 2) return;
            var points = new List<(float x, float y)>(pts.Length);
            foreach (var p in pts) points.Add((p.X, p.Y));
            _currentPage.DrawCurve(points,
                StrokeStyle.Default
                    .WithColor(si.BStyleTop != BorderStyleEnum.None
                        ? PdfColor.FromRgb(si.BColorTop.R, si.BColorTop.G, si.BColorTop.B)
                        : PdfColor.Black)
                    .WithLineStyle(ConvertLineStyle(si.BStyleTop)));
        }

        protected internal override void AddEllipse(float x, float y, float height, float width,
            StyleInfo si, string url)
        {
            _currentPage.DrawEllipse(x, y, width, height, BuildShapeStyle(si));
        }

        // ── text ──────────────────────────────────────────────────────────────

        protected internal override void AddText(float x, float y, float height, float width,
            string[] sa, StyleInfo si, float[] tw, bool bWrap, string url, bool bNoClip, string tooltip)
        {
            if (sa == null || sa.Length == 0) return;

            TextStyle baseStyle = ResolveFont(si);

            // RenderBase.MeasureString wraps text using System.Drawing/SkiaSharp metrics, which
            // can differ from Majorsilence.Pdf's embedded TrueType metrics.  Re-wrap here using
            // the actual PDF font metrics so that text fits within cell boundaries.
            float availableW = width - si.PaddingLeft - si.PaddingRight;
            if (!bNoClip && availableW > 0)
                sa = RewrapLines(sa, baseStyle, availableW);

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
                                startX = x + si.PaddingLeft
                                       + (width - si.PaddingLeft - si.PaddingRight) / 2f
                                       - textwidth / 2f;
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
                                startY = y + si.PaddingTop
                                       + (height - si.PaddingTop - si.PaddingBottom) / 2f
                                       - si.FontSize / 2f;
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

                    _currentPage.DrawText(text, startX, startY + si.FontSize, baseStyle);

                    float maxX = width > 0
                        ? Math.Min(x + width, startX + textwidth)
                        : startX + textwidth;

                    switch (si.TextDecoration)
                    {
                        case TextDecorationEnum.Underline:
                            AddLine(startX, startY + si.FontSize + 1,
                                    maxX,   startY + si.FontSize + 1,
                                    1, si.Color, BorderStyleEnum.Solid);
                            break;
                        case TextDecorationEnum.LineThrough:
                            AddLine(startX, startY + si.FontSize / 2f + 1,
                                    maxX,   startY + si.FontSize / 2f + 1,
                                    1, si.Color, BorderStyleEnum.Solid);
                            break;
                        case TextDecorationEnum.Overline:
                            AddLine(startX, startY + 1,
                                    maxX,   startY + 1,
                                    1, si.Color, BorderStyleEnum.Solid);
                            break;
                    }
                }
                else
                {
                    startX += si.FontSize / 4f;
                    switch (si.TextAlign)
                    {
                        case TextAlignEnum.Center:
                            if (height > 0)
                                startY = y + si.PaddingLeft
                                       + (height - si.PaddingLeft - si.PaddingRight) / 2f
                                       - textwidth / 2f;
                            break;
                        case TextAlignEnum.Right:
                            if (width > 0)
                                startY = y + height - textwidth - si.PaddingRight;
                            break;
                    }
                    int rotationDegrees = si.WritingMode switch
                    {
                        WritingModeEnum.tb_lr => 90,
                        WritingModeEnum.rl_bt => 180,
                        WritingModeEnum.tb_rl => 270,
                        _ => 90,
                    };
                    _currentPage.DrawText(text, startX, startY + si.FontSize,
                        baseStyle.WithRotation(rotationDegrees));
                }
            }

            AddAnnotations(x, y, height, width, url, tooltip);
            iAddBorder(si, x, y, height, width);
        }

        // ── PDF-accurate text re-wrapping ─────────────────────────────────────

        // RenderBase wraps text with System.Drawing metrics; we re-check each
        // line against the actual PDF font metrics and split further if needed.
        private string[] RewrapLines(string[] lines, TextStyle style, float maxWidth)
        {
            var result = new List<string>(lines.Length);
            foreach (string line in lines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    result.Add(line);
                    continue;
                }
                if (_currentPage.MeasureTextWidth(line, style) <= maxWidth)
                    result.Add(line);
                else
                    WrapLine(line, style, maxWidth, result);
            }
            return result.ToArray();
        }

        private void WrapLine(string text, TextStyle style, float maxWidth, List<string> output)
        {
            string[] words = text.Split(' ');
            var current = new System.Text.StringBuilder();

            foreach (string word in words)
            {
                if (current.Length == 0)
                {
                    current.Append(word);
                }
                else
                {
                    string candidate = current + " " + word;
                    if (_currentPage.MeasureTextWidth(candidate, style) <= maxWidth)
                    {
                        current.Clear();
                        current.Append(candidate);
                    }
                    else
                    {
                        output.Add(current.ToString());
                        current.Clear();
                        current.Append(word);
                    }
                }
            }
            if (current.Length > 0)
                output.Add(current.ToString());
        }

        // ── font registry construction ────────────────────────────────────────

        private FontRegistry BuildFontRegistry()
        {
            // FontFolder sets _liberationFonts / _dejavuFonts as a side-effect
            string sysFolder = FontFolder;
            var reg = new FontRegistry();

            // ── bundled fonts (always available via Majorsilence.Drawing.Common) ──
#if DRAWINGCOMPAT
            string embDir = Majorsilence.Drawing.FontResourceLoader.GetFontDirectory();
            if (Directory.Exists(embDir))
                reg.AddDirectory(embDir);
#endif

            // ── system fonts — explicit registrations for known naming schemes ──

            if (IsOSX)
            {
                // macOS: "Arial.ttf", "Arial Bold.ttf", etc. (spaces in names)
                TryAddFamily(reg, sysFolder, "Arial",
                    r: "Arial.ttf", b: "Arial Bold.ttf",
                    i: "Arial Italic.ttf", bi: "Arial Bold Italic.ttf");
                TryAddFamily(reg, sysFolder, "Times New Roman",
                    r: "Times New Roman.ttf", b: "Times New Roman Bold.ttf",
                    i: "Times New Roman Italic.ttf", bi: "Times New Roman Bold Italic.ttf");
                TryAddFamily(reg, sysFolder, "Courier New",
                    r: "Courier New.ttf", b: "Courier New Bold.ttf",
                    i: "Courier New Italic.ttf", bi: "Courier New Bold Italic.ttf");
                TryAddFamily(reg, sysFolder, "Georgia",
                    r: "Georgia.ttf", b: "Georgia Bold.ttf",
                    i: "Georgia Italic.ttf", bi: "Georgia Bold Italic.ttf");
            }
            else if (_osPlatform == (int)PlatformID.Unix)
            {
                // Liberation fonts follow FamilyName-Variant.ttf — AddDirectory handles them
                if (_liberationFonts || _dejavuFonts)
                    reg.AddDirectory(sysFolder);

                // Additional common Linux locations
                foreach (string dir in new[]
                {
                    "/usr/share/fonts/truetype/liberation",
                    "/usr/share/fonts/truetype/dejavu",
                    "/usr/share/fonts/truetype/noto",
                    "/usr/share/fonts/noto",
                    "/usr/share/fonts/opentype/noto",
                })
                {
                    if (Directory.Exists(dir) && dir != sysFolder)
                        reg.AddDirectory(dir);
                }
            }
            else
            {
                // Windows: abbreviated filenames (arial.ttf, arialbd.ttf, etc.)
                TryAddFamily(reg, sysFolder, "Arial",
                    r: "arial.ttf", b: "arialbd.ttf",
                    i: "ariali.ttf", bi: "arialbi.ttf");
                TryAddFamily(reg, sysFolder, "Times New Roman",
                    r: "times.ttf", b: "timesbd.ttf",
                    i: "timesi.ttf", bi: "timesbi.ttf");
                TryAddFamily(reg, sysFolder, "Courier New",
                    r: "cour.ttf", b: "courbd.ttf",
                    i: "couri.ttf", bi: "courbi.ttf");
                TryAddFamily(reg, sysFolder, "Calibri",
                    r: "calibri.ttf", b: "calibrib.ttf",
                    i: "calibrii.ttf", bi: "calibriz.ttf");
                TryAddFamily(reg, sysFolder, "Cambria",
                    r: "cambria.ttc", b: "cambriab.ttf",
                    i: "cambriai.ttf", bi: "cambriaz.ttf");
                TryAddFamily(reg, sysFolder, "Georgia",
                    r: "georgia.ttf", b: "georgiab.ttf",
                    i: "georgiai.ttf", bi: "georgiaz.ttf");
                TryAddFamily(reg, sysFolder, "Verdana",
                    r: "verdana.ttf", b: "verdanab.ttf",
                    i: "verdanai.ttf", bi: "verdanaz.ttf");
                TryAddFamily(reg, sysFolder, "Tahoma",
                    r: "tahoma.ttf", b: "tahomabd.ttf",
                    i: null, bi: null);
                TryAddFamily(reg, sysFolder, "Trebuchet MS",
                    r: "trebuc.ttf", b: "trebucbd.ttf",
                    i: "trebucit.ttf", bi: "trebucbi.ttf");
            }

            // ── fallback chain ────────────────────────────────────────────────
            // Prefer wide-coverage fonts; first match wins
            foreach (string fb in new[]
                { "NotoSans", "LiberationSans", "DejaVuSans", "Arial", "Verdana" })
            {
                if (reg.Contains(fb)) { reg.AddFallback(fb); break; }
            }

            return reg;
        }

        // Register a font family only if at least one variant file exists.
        private static void TryAddFamily(FontRegistry reg, string folder,
            string family, string r, string b, string i, string bi)
        {
            string rPath  = r  != null && File.Exists(Path.Combine(folder, r))  ? Path.Combine(folder, r)  : null;
            string bPath  = b  != null && File.Exists(Path.Combine(folder, b))  ? Path.Combine(folder, b)  : null;
            string iPath  = i  != null && File.Exists(Path.Combine(folder, i))  ? Path.Combine(folder, i)  : null;
            string biPath = bi != null && File.Exists(Path.Combine(folder, bi)) ? Path.Combine(folder, bi) : null;

            if (rPath != null || bPath != null || iPath != null || biPath != null)
                reg.AddFamily(family, rPath, bPath, iPath, biPath);
        }

        // ── font resolution ───────────────────────────────────────────────────

        private static string NormalizeFace(string face)
        {
            switch (face?.ToLowerInvariant())
            {
                case "times": case "times-roman": case "times roman":
                case "timesnewroman": case "times new roman":
                case "timesnewromanps": case "timesnewromanpsmt": case "serif":
                    return "Times New Roman";

                case "courier": case "couriernew": case "courier new":
                case "couriernewpsmt": case "monospace":
                    return "Courier New";

                case "symbol":                               return "Symbol";
                case "zapfdingbats": case "wingdings": case "wingding":
                    return "ZapfDingbats";

                default: return face;
            }
        }

        private string MapFaceToRegistryFamily(string face)
        {
            switch (face)
            {
                case "Times New Roman":
                    // Prefer metric-compatible bundled fonts, then system
                    if (_fontRegistry.Contains("LiberationSerif"))  return "LiberationSerif";
                    if (_fontRegistry.Contains("Times New Roman"))  return "Times New Roman";
                    return null;

                case "Courier New":
                    if (_fontRegistry.Contains("LiberationMono"))   return "LiberationMono";
                    if (_fontRegistry.Contains("Courier New"))      return "Courier New";
                    return null;

                case "Symbol":
                case "ZapfDingbats":
                    return null; // handled as standard Type-1 below

                default:
                    // Check metric-compatible substitution table (Calibri→Carlito etc.)
                    if (_embeddedFontMap.TryGetValue(face, out string mapped)
                        && _fontRegistry.Contains(mapped))
                        return mapped;

                    // Try exact name
                    if (_fontRegistry.Contains(face)) return face;

                    // Helvetica/Arial treated as sans-serif default
                    if (_fontRegistry.Contains("LiberationSans")) return "LiberationSans";
                    if (_fontRegistry.Contains("Arial"))          return "Arial";
                    return null;
            }
        }

        private TextStyle ResolveFont(StyleInfo si)
        {
            bool bold   = si.IsFontBold();
            bool italic = si.FontStyle == FontStyleEnum.Italic;
            string face = NormalizeFace(si.FontFamily);

            var baseStyle = TextStyle.Default
                .WithSize(si.FontSize)
                .WithColor(PdfColor.FromRgb(si.Color.R, si.Color.G, si.Color.B))
                .WithBold(bold)
                .WithItalic(italic);

            // Try the registry first — handles embedding, variants, and fallback automatically
            string registryFamily = MapFaceToRegistryFamily(face);
            if (registryFamily != null)
                return baseStyle.WithFamily(registryFamily);

            // Symbol and ZapfDingbats have no TTF equivalent — use standard Type-1
            if (face == "Symbol")       return baseStyle.WithFamily("Symbol");
            if (face == "ZapfDingbats") return baseStyle.WithFamily("ZapfDingbats");

            // Final fallback: standard Helvetica Type-1 (no embedding, WinAnsi only)
            string helvName = bold && italic ? "Helvetica-BoldOblique"
                            : bold           ? "Helvetica-Bold"
                            : italic         ? "Helvetica-Oblique"
                                             : "Helvetica";
            return baseStyle.WithFamily(helvName);
        }

        // ── private drawing helpers ───────────────────────────────────────────

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

        private void AddAnnotations(float x, float y, float height, float width,
            string url, string tooltip)
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

        // ── OS helpers ────────────────────────────────────────────────────────

        private bool IsOSX => System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.OSX);

        private string FontFolder
        {
            get
            {
                if (IsOSX) return "/System/Library/Fonts/Supplemental";

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

                DirectoryInfo winDir = Directory.GetParent(
                    Environment.GetFolderPath(Environment.SpecialFolder.System));
                return Path.Combine(winDir.FullName, "Fonts");
            }
        }

        // ── image decoding ────────────────────────────────────────────────────

        private static byte[] DecodeToRgb(byte[] data, ref int samplesW, ref int samplesH)
        {
            try
            {
                using var ms  = new MemoryStream(data);
#if DRAWINGCOMPAT
                using var img = new Majorsilence.Drawing.Bitmap(ms);
#else
                using var img = new Draw2.Bitmap(Draw2.Image.FromStream(ms));
#endif
                samplesW = img.Width;
                samplesH = img.Height;
                var rgb = new byte[samplesW * samplesH * 3];
                int idx = 0;
                for (int row = 0; row < samplesH; row++)
                    for (int col = 0; col < samplesW; col++)
                    {
                        var px = img.GetPixel(col, row);
                        rgb[idx++] = px.R;
                        rgb[idx++] = px.G;
                        rgb[idx++] = px.B;
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
