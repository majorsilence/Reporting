#nullable disable
using System;
using System.Collections;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl.PptxConverter;
using ImageFormat = Majorsilence.Forms.Drawing.Imaging.ImageFormat;

namespace Majorsilence.Reporting.Rdl
{
    ///<summary>
    /// Renders a report to PowerPoint (.pptx), one slide per page, the slide the size of the page.
    /// A page oriented renderer like PDF and TIF. Text boxes become editable text shapes, rectangles,
    /// ellipses, polygons and lines become shapes, and images and charts become pictures.
    ///</summary>
    internal sealed class RenderPptx : IPresent
    {
        private readonly Report _report;
        private readonly Stream _stream;
        private readonly PptxPackage _package = new PptxPackage();

        // The pixel density a generated image (a barcode, say) is asked for: enough to stay sharp on a projector.
        private const float GeneratedImageDpi = 192f;

        public RenderPptx(Report rep, IStreamGen sg)
        {
            _report = rep;
            _stream = sg.GetStream();
        }

        public void Dispose() { }

        public Report Report() => _report;

        public bool IsPagingNeeded() => true;

        public void Start()
        {
            _package.Title = _report.Name;
            _package.Author = _report.Author;
            _package.Description = _report.Description;
            _package.Created = _report.ExecutionTime == default ? DateTime.Now : _report.ExecutionTime;

            float w = _report.ReportDefinition.PageWidth.ToPoints();
            float h = _report.ReportDefinition.PageHeight.ToPoints();
            _package.SlideWidthEmu = SlideSide(w, "width");
            _package.SlideHeightEmu = SlideSide(h, "height");
        }

        private long SlideSide(float points, string which)
        {
            long emu = Emu(points);
            if (emu >= PptxPackage.MinSlideEmu && emu <= PptxPackage.MaxSlideEmu)
                return emu;
            long clamped = Math.Clamp(emu, PptxPackage.MinSlideEmu, PptxPackage.MaxSlideEmu);
            _report.rl.LogError(4, string.Format(CultureInfo.InvariantCulture,
                "The page {0} of {1:0.##}in is outside the 1in to 56in PowerPoint allows for a slide; the slides are {2:0.##}in.",
                which, points / 72f, clamped / (double)PptxPackage.EmuPerPoint / 72.0));
            return clamped;
        }

        public Task End()
        {
            _package.Write(_stream);
            return Task.CompletedTask;
        }

        public async Task RunPages(Pages pgs)
        {
            foreach (Page p in pgs)
            {
                var slide = _package.AddSlide();
                await ProcessItems(pgs, slide, p).ConfigureAwait(false);
            }
        }

        private async Task ProcessItems(Pages pgs, PptxSlide slide, IEnumerable items)
        {
            foreach (PageItem pi in items)
            {
                if (pi is PageTextHtml pth)
                {   // a composite of text pieces, laid out when built
                    await pth.Build(pgs.G).ConfigureAwait(false);
                    await ProcessItems(pgs, slide, pth).ConfigureAwait(false);
                    continue;
                }

                if (pi is PageLine pl)
                {
                    AddLine(slide, pl.X, pl.Y, pl.X2, pl.Y2, pl.SI.BColorLeft, pl.SI.BStyleLeft, pl.SI.BWidthLeft, "Line");
                    continue;
                }

                if (pi.SI.BackgroundImage != null)
                    AddBackgroundImage(pgs, slide, pi);

                switch (pi)
                {
                    case PageText pt:
                        AddText(slide, pt);
                        break;
                    case PageImage img:
                        AddImage(pgs, slide, img);
                        AddBorders(slide, pi, uniformAsOutline: false);
                        break;
                    case PageRectangle _:
                        AddShape(slide, pi, "rect", "Rectangle");
                        break;
                    case PageEllipse _:
                        AddShape(slide, pi, "ellipse", "Ellipse");
                        break;
                    case PagePie pie:
                        AddPie(slide, pie);
                        break;
                    case PagePolygon poly:
                        AddPath(slide, poly, poly.Points, closed: true, "Polygon");
                        break;
                    case PageCurve curve:
                        AddPath(slide, curve, CurvePoints(curve.Points, curve.Tension), closed: false, "Curve");
                        break;
                }
            }
        }

        #region text

        private void AddText(PptxSlide slide, PageText pt)
        {
            StyleInfo si = pt.SI;
            string text = pt.Text ?? "";
            bool hasFill = HasFill(si);
            bool hasBorder = HasAnyBorder(si);
            if (text.Length == 0 && !hasFill && !hasBorder)
                return;

            var sb = slide.Shapes;
            int id = slide.NextShapeId();
            float rot = 0;
            string vert = null;
            switch (si.WritingMode)
            {
                case WritingModeEnum.tb_rl: vert = "vert"; break;
                case WritingModeEnum.tb_lr: vert = "vert270"; break;
                case WritingModeEnum.rl_bt: rot = 180; break;
            }

            sb.Append("<p:sp><p:nvSpPr>");
            AppendCNvPr(slide, sb, id, "TextBox " + id.ToString(CultureInfo.InvariantCulture), pt.Tooltip, pt.HyperLink);
            sb.Append("<p:cNvSpPr txBox=\"1\"/><p:nvPr/></p:nvSpPr>");
            sb.Append("<p:spPr>");
            AppendXfrm(sb, pt.X, pt.Y, pt.W, pt.H, rot);
            sb.Append("<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom>");
            AppendFill(sb, si);
            AppendOutline(sb, si);
            sb.Append("</p:spPr>");

            // The text is written whole and PowerPoint wraps it, so it stays editable as one piece of text.
            bool wrap = !pt.NoWrap && !pt.NoClip;
            sb.Append("<p:txBody><a:bodyPr");
            Attr(sb, "wrap", wrap ? "square" : "none");
            Attr(sb, "lIns", Emu(si.PaddingLeft));
            Attr(sb, "tIns", Emu(si.PaddingTop));
            Attr(sb, "rIns", Emu(si.PaddingRight));
            Attr(sb, "bIns", Emu(si.PaddingBottom));
            Attr(sb, "rtlCol", "0");
            Attr(sb, "anchor", si.VerticalAlign == VerticalAlignEnum.Middle ? "ctr" : si.VerticalAlign == VerticalAlignEnum.Bottom ? "b" : "t");
            if (vert != null)
                Attr(sb, "vert", vert);
            sb.Append("><a:noAutofit/></a:bodyPr><a:lstStyle/>");

            string pPr = BuildParagraphProperties(si);
            string rPr = BuildRunProperties(si);
            string[] lines = Majorsilence.Reporting.Rdl.Textbox.LineBreaksAsLineFeeds(text).Split('\n');
            foreach (string line in lines)
            {
                sb.Append("<a:p>").Append(pPr);
                if (line.Length > 0)
                    sb.Append("<a:r>").Append("<a:rPr").Append(rPr).Append("</a:rPr><a:t>").Append(Xml.Escape(line)).Append("</a:t></a:r>");
                sb.Append("<a:endParaRPr").Append(rPr).Append("</a:endParaRPr></a:p>");
            }
            sb.Append("</p:txBody></p:sp>");

            if (!IsUniformBorder(si))
                AddBorders(slide, pt, uniformAsOutline: true);
        }

        private static string BuildParagraphProperties(StyleInfo si)
        {
            string algn = si.TextAlign switch
            {
                TextAlignEnum.Center => "ctr",
                TextAlignEnum.Right => "r",
                TextAlignEnum.Justified => "just",
                _ => "l",
            };
            var sb = new StringBuilder("<a:pPr");
            Attr(sb, "algn", algn);
            if (si.Direction == DirectionEnum.RTL)
                Attr(sb, "rtl", "1");
            sb.Append("/>");
            return sb.ToString();
        }

        // The attributes and children of a:rPr / a:endParaRPr, from after the element name to before its end tag.
        private static string BuildRunProperties(StyleInfo si)
        {
            var sb = new StringBuilder();
            Attr(sb, "lang", LanguageTag(si.Language));
            int size = (int)Math.Round(si.FontSize * 100);
            Attr(sb, "sz", Math.Clamp(size, 100, 400000));
            if (si.IsFontBold())
                Attr(sb, "b", "1");
            if (si.FontStyle == FontStyleEnum.Italic)
                Attr(sb, "i", "1");
            if (si.TextDecoration == TextDecorationEnum.Underline)
                Attr(sb, "u", "sng");
            else if (si.TextDecoration == TextDecorationEnum.LineThrough)
                Attr(sb, "strike", "sngStrike");
            Attr(sb, "dirty", "0");
            sb.Append('>');
            Color c = si.Color.IsEmpty ? Color.Black : si.Color;
            AppendSolidFill(sb, c);
            string face = Xml.Escape(FontFace(si.FontFamily));
            sb.Append("<a:latin typeface=\"").Append(face).Append("\"/><a:cs typeface=\"").Append(face).Append("\"/>");
            return sb.ToString();
        }

        private static string LanguageTag(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                return "en-US";
            try
            {
                string name = CultureInfo.GetCultureInfo(language.Trim()).Name;
                return string.IsNullOrEmpty(name) ? "en-US" : name;
            }
            catch (CultureNotFoundException)
            {
                return "en-US";
            }
        }

        // The generic CSS families RDL allows, named as StyleInfo.GetFontFamily names them.
        private static string FontFace(string family)
        {
            string f = (family ?? "").Trim().Trim('"', '\'');
            switch (f.ToLowerInvariant())
            {
                case "": return "Arial";
                case "serif": return "Times New Roman";
                case "sans-serif": return "Arial";
                case "cursive": return "Comic Sans MS";
                case "fantasy": return "Impact";
                case "monospace":
                case "courier": return "Courier New";
                default: return f;
            }
        }

        #endregion

        #region shapes

        private void AddShape(PptxSlide slide, PageItem pi, string geometry, string name)
        {
            StyleInfo si = pi.SI;
            bool ellipse = geometry != "rect";
            bool outline = ellipse || IsUniformBorder(si);
            if (!HasFill(si) && !(ellipse ? HasBorder(si.BColorTop, si.BStyleTop, si.BWidthTop) : HasAnyBorder(si)) && pi.HyperLink == null)
                return;

            var sb = slide.Shapes;
            int id = slide.NextShapeId();
            sb.Append("<p:sp><p:nvSpPr>");
            AppendCNvPr(slide, sb, id, name + " " + id.ToString(CultureInfo.InvariantCulture), pi.Tooltip, pi.HyperLink);
            sb.Append("<p:cNvSpPr/><p:nvPr/></p:nvSpPr><p:spPr>");
            AppendXfrm(sb, pi.X, pi.Y, pi.W, pi.H, 0);
            sb.Append("<a:prstGeom prst=\"").Append(geometry).Append("\"><a:avLst/></a:prstGeom>");
            AppendFill(sb, si);
            if (outline)
                AppendLine(sb, si.BColorTop, si.BStyleTop, si.BWidthTop);
            else
                sb.Append("<a:ln><a:noFill/></a:ln>");
            sb.Append("</p:spPr></p:sp>");

            if (!outline)
                AddBorders(slide, pi, uniformAsOutline: false);
        }

        private void AddPie(PptxSlide slide, PagePie pie)
        {
            StyleInfo si = pie.SI;
            // GDI angles: degrees clockwise from the x axis, as DrawingML's are.
            double start = pie.StartAngle, sweep = pie.SweepAngle;
            if (sweep < 0)
            {
                start += sweep;
                sweep = -sweep;
            }
            double end = start + Math.Min(sweep, 360);

            var sb = slide.Shapes;
            int id = slide.NextShapeId();
            sb.Append("<p:sp><p:nvSpPr>");
            AppendCNvPr(slide, sb, id, "Pie " + id.ToString(CultureInfo.InvariantCulture), pie.Tooltip, pie.HyperLink);
            sb.Append("<p:cNvSpPr/><p:nvPr/></p:nvSpPr><p:spPr>");
            AppendXfrm(sb, pie.X, pie.Y, pie.W, pie.H, 0);
            sb.Append("<a:prstGeom prst=\"pie\"><a:avLst>")
              .Append("<a:gd name=\"adj1\" fmla=\"val ").Append(Angle(start)).Append("\"/>")
              .Append("<a:gd name=\"adj2\" fmla=\"val ").Append(Angle(end)).Append("\"/>")
              .Append("</a:avLst></a:prstGeom>");
            AppendFill(sb, si);
            AppendLine(sb, si.BColorTop, si.BStyleTop, si.BWidthTop);
            sb.Append("</p:spPr></p:sp>");
        }

        private static string Angle(double degrees)
        {
            double d = degrees % 360;
            if (d < 0)
                d += 360;
            return ((long)Math.Round(d * 60000) % 21600000).ToString(CultureInfo.InvariantCulture);
        }

        // A polygon (closed, filled) or a curve (open, stroked), drawn as a custom geometry over the bounds of its points.
        private void AddPath(PptxSlide slide, PageItem pi, PointF[] points, bool closed, string name)
        {
            if (points == null || points.Length < 2)
                return;
            StyleInfo si = pi.SI;
            if (closed ? !HasFill(si) : !HasBorder(si.BColorLeft, si.BStyleLeft, si.BWidthLeft))
                return;

            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var p in points)
            {
                minX = Math.Min(minX, p.X); minY = Math.Min(minY, p.Y);
                maxX = Math.Max(maxX, p.X); maxY = Math.Max(maxY, p.Y);
            }
            long w = Math.Max(1, Emu(maxX - minX)), h = Math.Max(1, Emu(maxY - minY));

            var sb = slide.Shapes;
            int id = slide.NextShapeId();
            sb.Append("<p:sp><p:nvSpPr>");
            AppendCNvPr(slide, sb, id, name + " " + id.ToString(CultureInfo.InvariantCulture), pi.Tooltip, pi.HyperLink);
            sb.Append("<p:cNvSpPr/><p:nvPr/></p:nvSpPr><p:spPr>");
            sb.Append("<a:xfrm><a:off");
            Attr(sb, "x", Emu(minX));
            Attr(sb, "y", Emu(minY));
            sb.Append("/><a:ext");
            Attr(sb, "cx", w);
            Attr(sb, "cy", h);
            sb.Append("/></a:xfrm>");
            sb.Append("<a:custGeom><a:avLst/><a:gdLst/><a:ahLst/><a:cxnLst/><a:rect l=\"0\" t=\"0\" r=\"r\" b=\"b\"/><a:pathLst><a:path");
            Attr(sb, "w", w);
            Attr(sb, "h", h);
            if (!closed)
                Attr(sb, "fill", "none");
            sb.Append('>');
            AppendPoint(sb, "moveTo", points[0], minX, minY);
            if (closed)
            {
                for (int i = 1; i < points.Length; i++)
                    AppendPoint(sb, "lnTo", points[i], minX, minY);
                sb.Append("<a:close/>");
            }
            else
            {   // the points after the first come in threes: two control points and an end point
                int i = 1;
                for (; i + 2 < points.Length; i += 3)
                {
                    sb.Append("<a:cubicBezTo>");
                    AppendPt(sb, points[i], minX, minY);
                    AppendPt(sb, points[i + 1], minX, minY);
                    AppendPt(sb, points[i + 2], minX, minY);
                    sb.Append("</a:cubicBezTo>");
                }
                for (; i < points.Length; i++)
                    AppendPoint(sb, "lnTo", points[i], minX, minY);
            }
            sb.Append("</a:path></a:pathLst></a:custGeom>");
            if (closed)
            {
                AppendFill(sb, si);
                sb.Append("<a:ln><a:noFill/></a:ln>");
            }
            else
            {
                sb.Append("<a:noFill/>");
                AppendLine(sb, si.BColorLeft, si.BStyleLeft, si.BWidthLeft);
            }
            sb.Append("</p:spPr></p:sp>");
        }

        private static void AppendPoint(StringBuilder sb, string element, PointF p, float originX, float originY)
        {
            sb.Append("<a:").Append(element).Append('>');
            AppendPt(sb, p, originX, originY);
            sb.Append("</a:").Append(element).Append('>');
        }

        private static void AppendPt(StringBuilder sb, PointF p, float originX, float originY)
        {
            sb.Append("<a:pt");
            Attr(sb, "x", Emu(p.X - originX));
            Attr(sb, "y", Emu(p.Y - originY));
            sb.Append("/>");
        }

        /// <summary>
        /// A cardinal spline through the points, as GDI+ DrawCurve draws it, given as the first point
        /// followed by the control, control and end point of each Bezier segment.
        /// </summary>
        internal static PointF[] CurvePoints(PointF[] pts, float tension)
        {
            if (pts == null || pts.Length < 2)
                return pts;
            float t = tension / 3f;
            var result = new PointF[1 + (pts.Length - 1) * 3];
            result[0] = pts[0];
            for (int i = 0; i < pts.Length - 1; i++)
            {
                PointF p0 = pts[Math.Max(i - 1, 0)], p1 = pts[i], p2 = pts[i + 1], p3 = pts[Math.Min(i + 2, pts.Length - 1)];
                result[1 + i * 3] = new PointF(p1.X + t * (p2.X - p0.X), p1.Y + t * (p2.Y - p0.Y));
                result[2 + i * 3] = new PointF(p2.X - t * (p3.X - p1.X), p2.Y - t * (p3.Y - p1.Y));
                result[3 + i * 3] = p2;
            }
            return result;
        }

        private void AddLine(PptxSlide slide, float x, float y, float x2, float y2,
            Color c, BorderStyleEnum style, float width, string name)
        {
            if (!HasBorder(c, style, width))
                return;
            var sb = slide.Shapes;
            int id = slide.NextShapeId();
            sb.Append("<p:cxnSp><p:nvCxnSpPr><p:cNvPr");
            Attr(sb, "id", id);
            Attr(sb, "name", name + " " + id.ToString(CultureInfo.InvariantCulture));
            sb.Append("/><p:cNvCxnSpPr/><p:nvPr/></p:nvCxnSpPr><p:spPr><a:xfrm");
            if (x2 < x)
                Attr(sb, "flipH", "1");
            if (y2 < y)
                Attr(sb, "flipV", "1");
            sb.Append("><a:off");
            Attr(sb, "x", Emu(Math.Min(x, x2)));
            Attr(sb, "y", Emu(Math.Min(y, y2)));
            sb.Append("/><a:ext");
            Attr(sb, "cx", Emu(Math.Abs(x2 - x)));
            Attr(sb, "cy", Emu(Math.Abs(y2 - y)));
            sb.Append("/></a:xfrm><a:prstGeom prst=\"line\"><a:avLst/></a:prstGeom>");
            AppendLine(sb, c, style, width);
            sb.Append("</p:spPr></p:cxnSp>");
        }

        /// <summary>
        /// The four sides of an item's border as lines, for a border a shape's single outline cannot draw:
        /// sides that differ, or an image's (a picture's outline would sit on the image, not on its box).
        /// </summary>
        private void AddBorders(PptxSlide slide, PageItem pi, bool uniformAsOutline)
        {
            StyleInfo si = pi.SI;
            if (pi.W <= 0 || pi.H <= 0)
                return;
            if (uniformAsOutline && IsUniformBorder(si))
                return;
            float l = pi.X, t = pi.Y, r = pi.X + pi.W, b = pi.Y + pi.H;
            AddLine(slide, l, t, r, t, si.BColorTop, si.BStyleTop, si.BWidthTop, "Border");
            AddLine(slide, r, t, r, b, si.BColorRight, si.BStyleRight, si.BWidthRight, "Border");
            AddLine(slide, l, b, r, b, si.BColorBottom, si.BStyleBottom, si.BWidthBottom, "Border");
            AddLine(slide, l, t, l, b, si.BColorLeft, si.BStyleLeft, si.BWidthLeft, "Border");
        }

        #endregion

        #region images

        private void AddImage(Pages pgs, PptxSlide slide, PageImage img)
        {
            StyleInfo si = img.SI;
            float bx = img.X + si.PaddingLeft, by = img.Y + si.PaddingTop;
            float bw = img.W - si.PaddingLeft - si.PaddingRight, bh = img.H - si.PaddingTop - si.PaddingBottom;
            if (bw <= 0 || bh <= 0)
                return;

            byte[] data = img.GetImageData(Pixels(bw), Pixels(bh));
            if (data == null || data.Length == 0)
                return;

            float x = bx, y = by, w = bw, h = bh;
            double cropRight = 0, cropBottom = 0;
            switch (img.Sizing)
            {
                case ImageSizingEnum.FitProportional:
                    if (img.SamplesW > 0 && img.SamplesH > 0)
                    {
                        float ratioIm = (float)img.SamplesH / img.SamplesW;
                        if (ratioIm > bh / bw)
                            w = bh / ratioIm;
                        else
                            h = bw * ratioIm;
                    }
                    break;
                case ImageSizingEnum.Clip:
                    if (img.SamplesW > 0 && img.SamplesH > 0)
                    {   // the image at its own size from the top left corner, cut off at the box's sides
                        float nw = img.SamplesW * 72f / pgs.G.DpiX, nh = img.SamplesH * 72f / pgs.G.DpiY;
                        if (nw > bw)
                            cropRight = 1 - bw / nw;
                        else
                            w = nw;
                        if (nh > bh)
                            cropBottom = 1 - bh / nh;
                        else
                            h = nh;
                    }
                    break;
            }

            AddPicture(slide, data, img.ImgFormat, x, y, w, h, cropRight, cropBottom, img.Tooltip, img.HyperLink, "Picture");
        }

        private void AddBackgroundImage(Pages pgs, PptxSlide slide, PageItem pi)
        {
            PageImage bg = pi.SI.BackgroundImage;
            byte[] data = bg.GetImageData();
            if (data == null || data.Length == 0)
                return;
            StyleInfo si = pi.SI;
            float bx = pi.X + si.PaddingLeft, by = pi.Y + si.PaddingTop;
            float bw = pi.W - si.PaddingLeft - si.PaddingRight, bh = pi.H - si.PaddingTop - si.PaddingBottom;
            if (bw <= 0 || bh <= 0 || bg.SamplesW <= 0 || bg.SamplesH <= 0)
                return;
            float iw = bg.SamplesW * 72f / pgs.G.DpiX, ih = bg.SamplesH * 72f / pgs.G.DpiY;

            int nx = 1, ny = 1;
            if (bg.Repeat == ImageRepeat.Repeat || bg.Repeat == ImageRepeat.RepeatX)
                nx = Math.Max(1, (int)Math.Floor(bw / iw));
            if (bg.Repeat == ImageRepeat.Repeat || bg.Repeat == ImageRepeat.RepeatY)
                ny = Math.Max(1, (int)Math.Floor(bh / ih));

            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < ny; j++)
                {
                    float x = bx + i * iw, y = by + j * ih;
                    float w = Math.Min(iw, bx + bw - x), h = Math.Min(ih, by + bh - y);
                    if (w <= 0 || h <= 0)
                        continue;
                    AddPicture(slide, data, bg.ImgFormat, x, y, w, h, 1 - w / iw, 1 - h / ih, null, null, "Background");
                }
            }
        }

        private void AddPicture(PptxSlide slide, byte[] data, ImageFormat format, float x, float y, float w, float h,
            double cropRight, double cropBottom, string tooltip, string hyperlink, string name)
        {
            string ext = format == ImageFormat.Jpeg ? "jpeg" : format == ImageFormat.Gif ? "gif" : "png";
            string target = _package.AddMedia(data, ext);
            string rid = slide.AddRelationship(PptxPackage.RelImage, target);

            var sb = slide.Shapes;
            int id = slide.NextShapeId();
            sb.Append("<p:pic><p:nvPicPr>");
            AppendCNvPr(slide, sb, id, name + " " + id.ToString(CultureInfo.InvariantCulture), tooltip, hyperlink);
            sb.Append("<p:cNvPicPr><a:picLocks noChangeAspect=\"1\"/></p:cNvPicPr><p:nvPr/></p:nvPicPr>");
            sb.Append("<p:blipFill><a:blip r:embed=\"").Append(rid).Append("\"/>");
            int r = Crop(cropRight), b = Crop(cropBottom);
            if (r > 0 || b > 0)
            {
                sb.Append("<a:srcRect");
                if (r > 0)
                    Attr(sb, "r", r);
                if (b > 0)
                    Attr(sb, "b", b);
                sb.Append("/>");
            }
            sb.Append("<a:stretch><a:fillRect/></a:stretch></p:blipFill><p:spPr>");
            AppendXfrm(sb, x, y, w, h, 0);
            sb.Append("<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></p:spPr></p:pic>");
        }

        // A fraction of the image cut off, in the thousandths of a percent DrawingML counts it in.
        private static int Crop(double fraction) => fraction <= 0.0001 ? 0 : (int)Math.Round(Math.Min(fraction, 1) * 100000);

        private static int Pixels(float points) => Math.Max(1, (int)Math.Round(points * GeneratedImageDpi / 72f));

        #endregion

        #region DrawingML pieces

        private static void AppendCNvPr(PptxSlide slide, StringBuilder sb, int id, string name, string tooltip, string hyperlink)
        {
            sb.Append("<p:cNvPr");
            Attr(sb, "id", id);
            Attr(sb, "name", name);
            if (!string.IsNullOrEmpty(tooltip))
                Attr(sb, "descr", tooltip);
            string link = ExternalLink(hyperlink);
            if (link == null)
            {
                sb.Append("/>");
                return;
            }
            string rid = slide.AddRelationship(PptxPackage.RelHyperlink, link, external: true);
            sb.Append("><a:hlinkClick r:id=\"").Append(rid).Append('"');
            if (!string.IsNullOrEmpty(tooltip))
                Attr(sb, "tooltip", tooltip);
            sb.Append("/></p:cNvPr>");
        }

        // Only an absolute URL is a link PowerPoint can follow from a slide.
        private static string ExternalLink(string hyperlink)
        {
            if (string.IsNullOrWhiteSpace(hyperlink))
                return null;
            return Uri.TryCreate(hyperlink.Trim(), UriKind.Absolute, out Uri uri) ? uri.AbsoluteUri : null;
        }

        private static void AppendXfrm(StringBuilder sb, float x, float y, float w, float h, float rotation)
        {
            sb.Append("<a:xfrm");
            if (rotation != 0)
                Attr(sb, "rot", (long)Math.Round(rotation * 60000));
            sb.Append("><a:off");
            Attr(sb, "x", Emu(x));
            Attr(sb, "y", Emu(y));
            sb.Append("/><a:ext");
            Attr(sb, "cx", Math.Max(0, Emu(w)));
            Attr(sb, "cy", Math.Max(0, Emu(h)));
            sb.Append("/></a:xfrm>");
        }

        private static bool HasFill(StyleInfo si) => IsVisible(si.BackgroundColor);

        private static void AppendFill(StringBuilder sb, StyleInfo si)
        {
            Color c = si.BackgroundColor;
            if (!IsVisible(c))
            {
                sb.Append("<a:noFill/>");
                return;
            }
            Color end = si.BackgroundGradientEndColor;
            int? angle = si.BackgroundGradientType switch
            {   // the directions the TIF and PDF renderers draw them in
                BackgroundGradientTypeEnum.LeftRight => 0,
                BackgroundGradientTypeEnum.Center => 0,
                BackgroundGradientTypeEnum.HorizontalCenter => 0,
                BackgroundGradientTypeEnum.TopBottom => 5400000,
                BackgroundGradientTypeEnum.VerticalCenter => 5400000,
                BackgroundGradientTypeEnum.DiagonalLeft => 2700000,
                BackgroundGradientTypeEnum.DiagonalRight => 8100000,
                _ => null,
            };
            if (angle == null || !IsVisible(end))
            {
                AppendSolidFill(sb, c);
                return;
            }
            sb.Append("<a:gradFill rotWithShape=\"1\"><a:gsLst><a:gs pos=\"0\">");
            AppendColor(sb, c);
            sb.Append("</a:gs><a:gs pos=\"100000\">");
            AppendColor(sb, end);
            sb.Append("</a:gs></a:gsLst><a:lin ang=\"").Append(angle.Value.ToString(CultureInfo.InvariantCulture))
              .Append("\" scaled=\"0\"/></a:gradFill>");
        }

        private static void AppendSolidFill(StringBuilder sb, Color c)
        {
            sb.Append("<a:solidFill>");
            AppendColor(sb, c);
            sb.Append("</a:solidFill>");
        }

        private static void AppendColor(StringBuilder sb, Color c)
        {
            sb.Append("<a:srgbClr val=\"").Append(c.R.ToString("X2", CultureInfo.InvariantCulture))
              .Append(c.G.ToString("X2", CultureInfo.InvariantCulture)).Append(c.B.ToString("X2", CultureInfo.InvariantCulture)).Append('"');
            if (c.A < 255)
                sb.Append("><a:alpha val=\"").Append((c.A * 100000 / 255).ToString(CultureInfo.InvariantCulture)).Append("\"/></a:srgbClr>");
            else
                sb.Append("/>");
        }

        // A text shape's outline: its border when all four sides are alike, none otherwise (the sides are drawn as lines).
        private static void AppendOutline(StringBuilder sb, StyleInfo si)
        {
            if (IsUniformBorder(si))
                AppendLine(sb, si.BColorTop, si.BStyleTop, si.BWidthTop);
            else
                sb.Append("<a:ln><a:noFill/></a:ln>");
        }

        private static void AppendLine(StringBuilder sb, Color c, BorderStyleEnum style, float width)
        {
            if (!HasBorder(c, style, width))
            {
                sb.Append("<a:ln><a:noFill/></a:ln>");
                return;
            }
            sb.Append("<a:ln");
            Attr(sb, "w", Math.Clamp(Emu(width), 0, 20116800));
            if (style == BorderStyleEnum.Double)
                Attr(sb, "cmpd", "dbl");
            sb.Append('>');
            AppendSolidFill(sb, c);
            string dash = style switch
            {
                BorderStyleEnum.Dashed => "dash",
                BorderStyleEnum.Dotted => "sysDot",
                _ => "solid",
            };
            sb.Append("<a:prstDash val=\"").Append(dash).Append("\"/></a:ln>");
        }

        private static bool IsVisible(Color c) => !c.IsEmpty && c.A > 0;

        private static bool HasBorder(Color c, BorderStyleEnum style, float width) =>
            style != BorderStyleEnum.None && width > 0 && IsVisible(c);

        private static bool HasAnyBorder(StyleInfo si) =>
            HasBorder(si.BColorTop, si.BStyleTop, si.BWidthTop) || HasBorder(si.BColorRight, si.BStyleRight, si.BWidthRight) ||
            HasBorder(si.BColorBottom, si.BStyleBottom, si.BWidthBottom) || HasBorder(si.BColorLeft, si.BStyleLeft, si.BWidthLeft);

        // All four sides drawn alike, or none drawn at all: one outline on the shape does.
        private static bool IsUniformBorder(StyleInfo si)
        {
            bool top = HasBorder(si.BColorTop, si.BStyleTop, si.BWidthTop);
            bool right = HasBorder(si.BColorRight, si.BStyleRight, si.BWidthRight);
            bool bottom = HasBorder(si.BColorBottom, si.BStyleBottom, si.BWidthBottom);
            bool left = HasBorder(si.BColorLeft, si.BStyleLeft, si.BWidthLeft);
            if (!top && !right && !bottom && !left)
                return true;
            if (!(top && right && bottom && left))
                return false;
            return SameSide(si.BColorTop, si.BStyleTop, si.BWidthTop, si.BColorRight, si.BStyleRight, si.BWidthRight)
                && SameSide(si.BColorTop, si.BStyleTop, si.BWidthTop, si.BColorBottom, si.BStyleBottom, si.BWidthBottom)
                && SameSide(si.BColorTop, si.BStyleTop, si.BWidthTop, si.BColorLeft, si.BStyleLeft, si.BWidthLeft);
        }

        private static bool SameSide(Color c1, BorderStyleEnum s1, float w1, Color c2, BorderStyleEnum s2, float w2) =>
            c1.ToArgb() == c2.ToArgb() && DashOf(s1) == DashOf(s2) && Math.Abs(w1 - w2) < 0.01f;

        private static int DashOf(BorderStyleEnum s) =>
            s == BorderStyleEnum.Dashed ? 1 : s == BorderStyleEnum.Dotted ? 2 : s == BorderStyleEnum.Double ? 3 : 0;

        internal static long Emu(float points) => (long)Math.Round(points * (double)PptxPackage.EmuPerPoint);

        private static void Attr(StringBuilder sb, string name, string value) =>
            sb.Append(' ').Append(name).Append("=\"").Append(Xml.Escape(value)).Append('"');

        private static void Attr(StringBuilder sb, string name, long value) =>
            sb.Append(' ').Append(name).Append("=\"").Append(value.ToString(CultureInfo.InvariantCulture)).Append('"');

        #endregion

        #region IPresent members a page oriented renderer has no use for

        public void BodyStart(Body b) { }
        public void BodyEnd(Body b) { }
        public void PageHeaderStart(PageHeader ph) { }
        public void PageHeaderEnd(PageHeader ph) { }
        public void PageFooterStart(PageFooter pf) { }
        public void PageFooterEnd(PageFooter pf) { }
        public Task Textbox(Textbox tb, string t, Row row) => Task.CompletedTask;
        public Task DataRegionNoRows(DataRegion d, string noRowsMsg) => Task.CompletedTask;
        public Task<bool> ListStart(List l, Row r) => Task.FromResult(true);
        public Task ListEnd(List l, Row r) => Task.CompletedTask;
        public Task ListEntryBegin(List l, Row r) => Task.CompletedTask;
        public void ListEntryEnd(List l, Row r) { }
        public Task<bool> TableStart(Table t, Row row) => Task.FromResult(true);
        public Task TableEnd(Table t, Row row) => Task.CompletedTask;
        public void TableBodyStart(Table t, Row row) { }
        public void TableBodyEnd(Table t, Row row) { }
        public void TableFooterStart(Footer f, Row row) { }
        public void TableFooterEnd(Footer f, Row row) { }
        public void TableHeaderStart(Header h, Row row) { }
        public void TableHeaderEnd(Header h, Row row) { }
        public Task TableRowStart(TableRow tr, Row row) => Task.CompletedTask;
        public void TableRowEnd(TableRow tr, Row row) { }
        public Task TableCellStart(TableCell t, Row row) => Task.CompletedTask;
        public void TableCellEnd(TableCell t, Row row) { }
        public Task<bool> MatrixStart(Matrix m, MatrixCellEntry[,] matrix, Row r, int headerRows, int maxRows, int maxCols) => Task.FromResult(true);
        public void MatrixColumns(Matrix m, MatrixColumns mc) { }
        public Task MatrixCellStart(Matrix m, ReportItem ri, int row, int column, Row r, float h, float w, int colSpan) => Task.CompletedTask;
        public Task MatrixCellEnd(Matrix m, ReportItem ri, int row, int column, Row r) => Task.CompletedTask;
        public void MatrixRowStart(Matrix m, int row, Row r) { }
        public void MatrixRowEnd(Matrix m, int row, Row r) { }
        public Task MatrixEnd(Matrix m, Row r) => Task.CompletedTask;
        public Task Chart(Chart c, Row r, ChartBase cb) => Task.CompletedTask;
        public Task Image(Image i, Row r, string mimeType, Stream ior) => Task.CompletedTask;
        public Task Line(Line l, Row r) => Task.CompletedTask;
        public Task<bool> RectangleStart(Rectangle rect, Row r) => Task.FromResult(true);
        public Task RectangleEnd(Rectangle rect, Row r) => Task.CompletedTask;
        public Task Subreport(Subreport s, Row r) => Task.CompletedTask;
        public void GroupingStart(Grouping g) { }
        public void GroupingInstanceStart(Grouping g) { }
        public void GroupingInstanceEnd(Grouping g) { }
        public void GroupingEnd(Grouping g) { }

        #endregion
    }
}
