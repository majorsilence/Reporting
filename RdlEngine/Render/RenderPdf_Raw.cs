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
using Draw2 = Majorsilence.Forms.Drawing;
using Imaging = Majorsilence.Forms.Drawing.Imaging;
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
            float width, System.Drawing.Color c, BorderStyleEnum ls)
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
            System.Drawing.RectangleF clipRect, byte[] im, int samplesW, int samplesH,
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

        protected internal override void AddPolygon(System.Drawing.PointF[] pts, StyleInfo si, string url)
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

        protected internal override void AddCurve(System.Drawing.PointF[] pts, StyleInfo si)
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
            // Vertical (tb-rl) text is always kept as RenderBase.MeasureString's single unwrapped
            // line (see its own WritingMode check) -- the box's width is the rotated run's
            // thickness, not its available length, so wrapping against it here would split one
            // rotated line into several that then get drawn on top of each other.
            float availableW = width - si.PaddingLeft - si.PaddingRight;
            if (!bNoClip && availableW > 0 && si.WritingMode != WritingModeEnum.tb_rl)
                sa = RewrapLines(RejoinSoftWraps(sa), baseStyle, availableW);

            // A textbox that may not grow may not draw outside its own rectangle either.
            // bWrap is the item's CanGrow: when it is false the box keeps the height the
            // report gave it, so wrapped lines past that height belong to nothing and used
            // to be painted straight over whatever sits below - in a table, the next row.
            // Clipping them confines an over-long value to its own cell, which is what
            // CanGrow=false means. Every other renderer already gets this for free: the
            // drawing path hands the string to a layout rectangle and the graphics library
            // clips it. This renderer places each line itself, so it has to say so.
            //
            // The pitch is si.FontSize because that is the pitch the loop below draws at,
            // and never fewer than one line: a single line of text is routinely taller than
            // the cell the report sizes for it, and dropping it would blank the report
            // rather than clip it.
            if (!bWrap && !bNoClip && height > 0 && sa.Length > 1 && si.FontSize > 0)
            {
                float availableH = height - si.PaddingTop - si.PaddingBottom;
                int maxLines = Math.Max(1, (int)Math.Floor(availableH / si.FontSize));
                if (sa.Length > maxLines)
                    sa = sa.Take(maxLines).ToArray();
            }

            if (!si.BackgroundColor.IsEmpty && height > 0 && width > 0)
                iAddFillRect(x, y, width, height, si.BackgroundColor);

            for (int i = 0; i < sa.Length; i++)
            {
                string text = sa[i];
                if (string.IsNullOrEmpty(text)) continue;

                float textwidth = _currentPage.MeasureTextWidth(text, baseStyle);
                float startX = x + si.PaddingLeft;
                float startY = y + si.PaddingTop + i * si.FontSize;

                if (si.WritingMode == WritingModeEnum.lr_tb || si.WritingMode == WritingModeEnum.rl_bt)
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

                    if (si.WritingMode == WritingModeEnum.rl_bt)
                    {
                        // Lay the line out as horizontal text, then turn it half a revolution about
                        // the centre of the box: the line's start lands on the opposite corner and the
                        // text runs back along it, upside down. Alignment and line order flip with it.
                        float cx = x + width / 2f, cy = y + height / 2f;
                        float rx = 2 * cx - startX, ry = 2 * cy - (startY + si.FontSize);
                        float rEndX = 2 * cx - (startX + textwidth);
                        _currentPage.DrawText(text, rx, ry, baseStyle.WithUpsideDown());
                        switch (si.TextDecoration)
                        {
                            case TextDecorationEnum.Underline:
                                AddLine(rEndX, ry - 1, rx, ry - 1, 1, si.Color, BorderStyleEnum.Solid);
                                break;
                            case TextDecorationEnum.LineThrough:
                                {
                                    float ly = 2 * cy - (startY + si.FontSize / 2f + 1);
                                    AddLine(rEndX, ly, rx, ly, 1, si.Color, BorderStyleEnum.Solid);
                                }
                                break;
                            case TextDecorationEnum.Overline:
                                AddLine(rEndX, 2 * cy - (startY + 1), rx, 2 * cy - (startY + 1), 1, si.Color, BorderStyleEnum.Solid);
                                break;
                        }
                        continue;
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
                    _currentPage.DrawText(text, startX, startY + si.FontSize,
                        baseStyle.WithVertical());
                }
            }

            AddAnnotations(x, y, height, width, url, tooltip);
            iAddBorder(si, x, y, height, width);
        }

        // ── PDF-accurate text re-wrapping ─────────────────────────────────────

        // RenderBase breaks a long line after the space that ends a word and keeps that space,
        // while a line that ends a paragraph has its trailing spaces trimmed. So a line ending
        // in a space was split only because the graphics library measured the text too wide
        // for the box, not because the PDF font does. Rejoining those lets RewrapLines break
        // them against the metrics that are actually drawn. Without it, a line measured a
        // fraction too wide lost its last word once the box clipped the second line.
        internal static string[] RejoinSoftWraps(string[] lines)
        {
            var joined = new List<string>(lines.Length);
            var current = new System.Text.StringBuilder();
            foreach (string line in lines)
            {
                current.Append(line);
                if (!string.IsNullOrEmpty(line) && line[line.Length - 1] == ' ')
                    continue;
                joined.Add(current.ToString());
                current.Clear();
            }
            if (current.Length > 0)
                joined.Add(current.ToString());
            return joined.ToArray();
        }

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

            // ── bundled fonts (always available via Majorsilence.Forms.Drawing.Common) ──
            string embDir = Majorsilence.Forms.Drawing.FontResourceLoader.GetFontDirectory();
            if (Directory.Exists(embDir))
                reg.AddDirectory(embDir);

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

            // Every other installed font, under the family name it declares, as on Windows
            // below. Without this, a font installed anywhere but the few folders above, or named
            // any other way, was never found: Microsoft's core fonts install as
            // /usr/share/fonts/truetype/msttcorefonts/Arial.ttf, so a report asking for Arial got
            // the fallback even where Arial was installed. Families registered above keep their
            // registration. Fonts live a folder or more below these roots, so the walk goes down.
            if (IsOSX || _osPlatform == (int)PlatformID.Unix)
            {
                foreach (string dir in InstalledFontRoots())
                    reg.AddInstalledFonts(dir, includeSubfolders: true);
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

                // Every other installed font, under the family name it declares. Without this
                // any family not listed above rendered as Arial - Impact, Segoe UI, Arial Black,
                // and a cheque's MICR font or a barcode font among them. The families above are
                // registered first and are not overridden. Windows installs fonts per user as
                // well as per machine, and a MICR or barcode font is as likely to be in either.
                reg.AddInstalledFonts(sysFolder);
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (!string.IsNullOrEmpty(localAppData))
                    reg.AddInstalledFonts(Path.Combine(localAppData, "Microsoft", "Windows", "Fonts"));
            }

            // ── fallback chain ────────────────────────────────────────────────
            // Prefer wide-coverage fonts; first match wins
            foreach (string fb in new[]
                { "NotoSans", "LiberationSans", "DejaVuSans", "Arial", "Verdana" })
            {
                if (reg.Contains(fb)) { reg.AddFallback(fb); break; }
            }

            AddCjkFallbacks(reg);

            return reg;
        }

        // Latin fonts have no CJK glyphs, so Chinese, Japanese or Korean text in a font the
        // machine lacks - or in a Latin font - would draw as .notdef boxes. A CJK family goes
        // on the end of the fallback chain so those characters come from it instead. The chain
        // is consulted per character and its fonts are only loaded when a string needs them.
        // Only TrueType-outline fonts can be embedded, so Noto Sans CJK (CFF) is not usable.
        private static readonly string[] CjkFallbackFamilies =
        {
            // Windows
            "Microsoft YaHei", "Microsoft JhengHei", "Yu Gothic", "Malgun Gothic",
            "SimSun", "SimHei", "DengXian", "MingLiU", "MS Gothic",
            // Linux
            "WenQuanYi Micro Hei", "WenQuanYi Zen Hei", "Droid Sans Fallback",
            "AR PL UMing CN", "AR PL UKai CN",
            // macOS
            "PingFang SC", "PingFang TC", "STHeiti", "Apple SD Gothic Neo",
        };

        private static readonly string[] CjkFontDirectories =
        {
            "/usr/share/fonts/truetype/wqy",
            "/usr/share/fonts/wenquanyi/wqy-microhei",
            "/usr/share/fonts/wenquanyi/wqy-zenhei",
            "/usr/share/fonts/truetype/droid",
            "/usr/share/fonts/truetype/arphic",
            "/usr/share/fonts/opentype/noto",
            "/System/Library/Fonts",
            "/Library/Fonts",
        };

        private void AddCjkFallbacks(FontRegistry reg)
        {
            // Windows fonts were all registered above; elsewhere the CJK fonts live in
            // folders that are not otherwise scanned.
            if (IsOSX || _osPlatform == (int)PlatformID.Unix)
            {
                foreach (string dir in CjkFontDirectories)
                    reg.AddInstalledFonts(dir);
            }

            foreach (string family in CjkFallbackFamilies)
                if (reg.Contains(family)) reg.AddFallback(family);
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

        // Where Linux and macOS keep installed fonts, system-wide and per user.
        private IEnumerable<string> InstalledFontRoots()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (IsOSX)
            {
                yield return "/System/Library/Fonts";
                yield return "/Library/Fonts";
                if (!string.IsNullOrEmpty(home)) yield return Path.Combine(home, "Library", "Fonts");
            }
            else
            {
                yield return "/usr/share/fonts";
                yield return "/usr/local/share/fonts";
                if (!string.IsNullOrEmpty(home))
                {
                    yield return Path.Combine(home, ".local", "share", "fonts");
                    yield return Path.Combine(home, ".fonts");
                }
            }
        }

        private string MapFaceToRegistryFamily(string face)
        {
            switch (face)
            {
                // The font itself when it is installed, as for every other family below; the
                // metric-compatible Liberation font only when it is not.
                case "Times New Roman":
                    if (_fontRegistry.Contains("Times New Roman"))  return "Times New Roman";
                    if (_fontRegistry.Contains("LiberationSerif"))  return "LiberationSerif";
                    return null;

                case "Courier New":
                    if (_fontRegistry.Contains("Courier New"))      return "Courier New";
                    if (_fontRegistry.Contains("LiberationMono"))   return "LiberationMono";
                    return null;

                case "Symbol":
                case "ZapfDingbats":
                    return null; // handled as standard Type-1 below

                default:
                    // The font itself, when it is there. A metric-compatible substitute is for
                    // when it is not: with every installed font registered, a machine that has
                    // both Calibri and Carlito (LibreOffice installs the latter) would otherwise
                    // draw a report's Calibri in Carlito.
                    if (_fontRegistry.Contains(face)) return face;

                    // Metric-compatible substitution table (Calibri→Carlito etc.)
                    if (_embeddedFontMap.TryGetValue(face, out string mapped)
                        && _fontRegistry.Contains(mapped))
                        return mapped;

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

        private void iAddFillRect(float x, float y, float width, float height, System.Drawing.Color c)
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
            // ToolTips are an interactive-HTML feature; SSRS drops them in PDF output, and a
            // Text annotation would show as a sticky-note icon on every tooltipped item.
            if (!string.IsNullOrEmpty(url))
                _currentPage.AddLink(x, y, width, height, url);
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
                    return Majorsilence.Forms.Drawing.FontResourceLoader.GetFontDirectory();
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
                using var img = new Majorsilence.Forms.Drawing.Bitmap(ms);
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
