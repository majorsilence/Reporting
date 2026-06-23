using System;
using System.Collections.Generic;
using Majorsilence.Reporting.Rdl;
using Drawing = Majorsilence.Drawing;

namespace Majorsilence.Reporting.UI.RdlAvalonia.Viewer
{
    /// <summary>
    /// Renders report pages to SkiaSharp graphics surface
    /// </summary>
    public class SkiaPageDrawing
    {
        private readonly Pages _pages;
        private readonly float _zoom;

        public SkiaPageDrawing(Pages pages, float zoom = 1.0f)
        {
            _pages = pages;
            _zoom = zoom;
        }

        /// <summary>
        /// Converts a color from the RdlEngine (which may be System.Drawing.Color or Majorsilence.Drawing.Color
        /// depending on build configuration) to Majorsilence.Drawing.Color
        /// </summary>
        private static Drawing.Color ToDrawingColor(dynamic color)
        {
            return Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }
        
        /// <summary>
        /// Gets a default black color
        /// </summary>
        private static Drawing.Color DefaultBlack => Drawing.Color.Black;
        
        /// <summary>
        /// Gets a default empty/transparent color
        /// </summary>
        private static Drawing.Color DefaultEmpty => Drawing.Color.Empty;

        public void Draw(Majorsilence.Drawing.Graphics g, int pageIndex)
        {
            if (_pages == null || pageIndex < 0 || pageIndex >= _pages.PageCount)
            {
                return;
            }

            var page = _pages[pageIndex];
            ProcessPage(g, page);
        }

        private void ProcessPage(Drawing.Graphics g, Page page)
        {
            if (page == null)
            {
                return;
            }

            foreach (PageItem pi in page)
            {
                if (pi == null)
                {
                    continue;
                }

                if (pi is PageTextHtml)
                {
                    ProcessHtml(pi as PageTextHtml, g);
                    continue;
                }

                if (pi is PageLine)
                {
                    var pl = pi as PageLine;
                    DrawLine(pl, g);
                    continue;
                }

                var rect = new Drawing.Rectangle(
                    (int)ConvertXtoPixels(pi.X),
                    (int)ConvertYtoPixels(pi.Y),
                    (int)ConvertXtoPixels(pi.W),
                    (int)ConvertYtoPixels(pi.H)
                );

                if (pi is PageText)
                {
                    var pt = pi as PageText;
                    DrawRectBackground(pi.SI, g, rect);
                    DrawString(pt, g, rect);
                    DrawItemBorders(pi.SI, g, rect);
                }
                else if (pi is PageImage)
                {
                    var i = pi as PageImage;
                    DrawRectBackground(pi.SI, g, rect);
                    DrawImage(i, g, rect);
                    DrawItemBorders(pi.SI, g, rect);
                }
                else if (pi is PageRectangle)
                {
                    DrawRectBackground(pi.SI, g, rect);
                    DrawItemBorders(pi.SI, g, rect);
                }
                else if (pi is PageEllipse)
                {
                    var pe = pi as PageEllipse;
                    DrawEllipse(pe, g, rect);
                }
                else if (pi is PagePie)
                {
                    var pp = pi as PagePie;
                    DrawPie(pp, g, rect);
                }
                else if (pi is PagePolygon)
                {
                    var ppo = pi as PagePolygon;
                    DrawPolygon(ppo, g, rect);
                }
                else if (pi is PageCurve)
                {
                    var pc = pi as PageCurve;
                    DrawCurve(pc, g, rect);
                }
            }
        }

        private void DrawRectBackground(StyleInfo? si, Drawing.Graphics g, Drawing.Rectangle rect)
        {
            if (si == null)
                return;

            var bgColor = ToDrawingColor(si.BackgroundColor);
            if (bgColor != Drawing.Color.Empty)
            {
                using var brush = new Drawing.SolidBrush(bgColor);
                g.FillRectangle(brush, rect);
            }

            if (si.BackgroundImage != null)
                DrawImage(si.BackgroundImage, g, rect);
        }

        private void DrawItemBorders(StyleInfo? si, Drawing.Graphics g, Drawing.Rectangle rect)
        {
            if (si == null)
                return;

            DrawBorderLine(g, si.BStyleLeft,   ToDrawingColor(si.BColorLeft),   si.BWidthLeft,
                new Drawing.Point(rect.Left,  rect.Top),    new Drawing.Point(rect.Left,  rect.Bottom));
            DrawBorderLine(g, si.BStyleRight,  ToDrawingColor(si.BColorRight),  si.BWidthRight,
                new Drawing.Point(rect.Right, rect.Top),    new Drawing.Point(rect.Right, rect.Bottom));
            DrawBorderLine(g, si.BStyleTop,    ToDrawingColor(si.BColorTop),    si.BWidthTop,
                new Drawing.Point(rect.Left,  rect.Top),    new Drawing.Point(rect.Right, rect.Top));
            DrawBorderLine(g, si.BStyleBottom, ToDrawingColor(si.BColorBottom), si.BWidthBottom,
                new Drawing.Point(rect.Left,  rect.Bottom), new Drawing.Point(rect.Right, rect.Bottom));
        }

        private void DrawBorderLine(Drawing.Graphics g, BorderStyleEnum style, Drawing.Color color,
            float width, Drawing.Point from, Drawing.Point to)
        {
            if (style == BorderStyleEnum.None)
                return;

            // Default to black when the RDL omits an explicit border colour
            var effectiveColor = (color == Drawing.Color.Empty) ? Drawing.Color.Black : color;
            var pen = CreatePen(effectiveColor, style, width);
            if (pen != null)
            {
                g.DrawLine(pen, from, to);
                pen.Dispose();
            }
        }

        private void ProcessHtml(PageTextHtml? pth, Drawing.Graphics g)
        {
            if (pth == null)
            {
                return;
            }

            // PageTextHtml contains nested PageItems that we can process
            // Draw the text content if available
            if (!string.IsNullOrEmpty(pth.Text))
            {
                var rect = new Drawing.Rectangle(
                    (int)ConvertXtoPixels(pth.X),
                    (int)ConvertYtoPixels(pth.Y),
                    (int)ConvertXtoPixels(pth.W),
                    (int)ConvertYtoPixels(pth.H)
                );
                
                var font = GetFont(pth);
                var brush = GetBrush(pth.SI != null ? ToDrawingColor(pth.SI.Color) : DefaultBlack);
                
                if (font != null && brush != null)
                {
                    var stringFormat = GetStringFormat(pth);
                    g.DrawString(pth.Text, font, brush, rect, stringFormat);
                    font.Dispose();
                    brush.Dispose();
                }
            }
        }

        private void DrawLine(PageLine? pl, Drawing.Graphics g)
        {
            if (pl == null || pl.SI == null)
            {
                return;
            }

            var pen = CreatePen(ToDrawingColor(pl.SI.BColorLeft), pl.SI.BStyleLeft, pl.SI.BWidthLeft);
            if (pen != null)
            {
                g.DrawLine(pen,
                    new Drawing.Point((int)ConvertXtoPixels(pl.X), (int)ConvertYtoPixels(pl.Y)),
                    new Drawing.Point((int)ConvertXtoPixels(pl.X2), (int)ConvertYtoPixels(pl.Y2)));
                pen.Dispose();
            }
        }

        private void DrawString(PageText? pt, Drawing.Graphics g, Drawing.Rectangle rect)
        {
            if (pt == null || string.IsNullOrEmpty(pt.Text))
            {
                return;
            }

            var font = GetFont(pt);
            var brush = GetBrush(pt.SI != null ? ToDrawingColor(pt.SI.Color) : DefaultBlack);

            if (font != null && brush != null)
            {
                var stringFormat = GetStringFormat(pt);
                var textRect = ApplyPadding(rect, pt.SI);
                g.DrawString(pt.Text, font, brush, textRect, stringFormat);
                font.Dispose();
                brush.Dispose();
            }
        }

        private Drawing.Rectangle ApplyPadding(Drawing.Rectangle rect, StyleInfo? si)
        {
            if (si == null)
                return rect;

            int left   = (int)ConvertXtoPixels(si.PaddingLeft);
            int top    = (int)ConvertYtoPixels(si.PaddingTop);
            int right  = (int)ConvertXtoPixels(si.PaddingRight);
            int bottom = (int)ConvertYtoPixels(si.PaddingBottom);

            return new Drawing.Rectangle(
                rect.X + left,
                rect.Y + top,
                Math.Max(0, rect.Width  - left - right),
                Math.Max(0, rect.Height - top  - bottom));
        }

        private void DrawImage(PageImage? pi, Drawing.Graphics g, Drawing.Rectangle rect)
        {
            if (pi == null)
            {
                return;
            }

            try
            {
                // Request the image at the exact pixel dimensions of the display
                // rectangle so that custom report items (barcodes, QR codes, etc.)
                // are generated at the right size and not stretched or squashed when
                // rows have different heights.
                var imageData = pi.GetImageData(Math.Max(1, rect.Width), Math.Max(1, rect.Height));
                if (imageData != null && imageData.Length > 0)
                {
                    using var ms = new System.IO.MemoryStream(imageData);
                    using var bitmap = new Drawing.Bitmap(ms);
                    g.DrawImage(bitmap, rect);
                }
            }
            catch
            {
                // Silently ignore image drawing errors
            }
        }

        private void DrawEllipse(PageEllipse? pe, Drawing.Graphics g, Drawing.Rectangle rect)
        {
            if (pe == null || pe.SI == null)
            {
                return;
            }

            var brush = GetBrush(ToDrawingColor(pe.SI.BackgroundColor));
            if (brush != null)
            {
                g.FillEllipse(brush, rect);
                brush.Dispose();
            }

            var pen = CreatePen(ToDrawingColor(pe.SI.BColorLeft), pe.SI.BStyleLeft, pe.SI.BWidthLeft);
            if (pen != null)
            {
                g.DrawEllipse(pen, rect);
                pen.Dispose();
            }
        }

        private void DrawPie(PagePie? pp, Drawing.Graphics g, Drawing.Rectangle rect)
        {
            if (pp == null || pp.SI == null)
            {
                return;
            }

            var brush = GetBrush(ToDrawingColor(pp.SI.BackgroundColor));
            if (brush != null)
            {
                g.FillPie(brush, rect, pp.StartAngle, pp.SweepAngle);
                brush.Dispose();
            }

            var pen = CreatePen(ToDrawingColor(pp.SI.BColorLeft), pp.SI.BStyleLeft, pp.SI.BWidthLeft);
            if (pen != null)
            {
                g.DrawPie(pen, rect, pp.StartAngle, pp.SweepAngle);
                pen.Dispose();
            }
        }

        private void DrawPolygon(PagePolygon? ppo, Drawing.Graphics g, Drawing.Rectangle rect)
        {
            if (ppo == null || ppo.Points == null || ppo.Points.Length < 3)
            {
                return;
            }

            var points = new List<Drawing.PointF>();
            foreach (var pt in ppo.Points)
            {
                points.Add(new Drawing.PointF(
                    ConvertXtoPixels(pt.X),
                    ConvertYtoPixels(pt.Y)
                ));
            }

            var brush = GetBrush(ppo.SI != null ? ToDrawingColor(ppo.SI.BackgroundColor) : DefaultEmpty);
            if (brush != null)
            {
                g.FillPolygon(brush, points.ToArray());
                brush.Dispose();
            }

            var pen = CreatePen(ppo.SI != null ? ToDrawingColor(ppo.SI.BColorLeft) : DefaultBlack, ppo.SI?.BStyleLeft, ppo.SI?.BWidthLeft ?? 1f);
            if (pen != null)
            {
                g.DrawPolygon(pen, points.ToArray());
                pen.Dispose();
            }
        }

        private void DrawCurve(PageCurve? pc, Drawing.Graphics g, Drawing.Rectangle rect)
        {
            if (pc == null || pc.Points == null || pc.Points.Length < 2)
            {
                return;
            }

            var points = new List<Drawing.Point>();
            foreach (var pt in pc.Points)
            {
                points.Add(new Drawing.Point(
                    (int)ConvertXtoPixels(pt.X),
                    (int)ConvertYtoPixels(pt.Y)
                ));
            }

            var pen = CreatePen(pc.SI != null ? ToDrawingColor(pc.SI.BColorLeft) : DefaultBlack, pc.SI?.BStyleLeft, pc.SI?.BWidthLeft ?? 1f);
            if (pen != null)
            {
                g.DrawCurve(pen, points.ToArray(), 0.5f);
                pen.Dispose();
            }
        }

        private float ConvertXtoPixels(float x)
        {
            return x * _zoom;
        }

        private float ConvertYtoPixels(float y)
        {
            return y * _zoom;
        }

        private Drawing.Pen? CreatePen(Drawing.Color color, BorderStyleEnum? style, float width)
        {
            if (color == Drawing.Color.Empty)
            {
                return null;
            }

            // width is already in points
            float penWidth = width > 0 ? width : 1f;

            var pen = new Drawing.Pen(color, penWidth);

            switch (style)
            {
                case BorderStyleEnum.Dashed:
                    pen.DashStyle = Drawing.Drawing2D.DashStyle.Dash;
                    break;
                case BorderStyleEnum.Dotted:
                    pen.DashStyle = Drawing.Drawing2D.DashStyle.Dot;
                    break;
                case BorderStyleEnum.Double:
                    pen.DashStyle = Drawing.Drawing2D.DashStyle.DashDot;
                    break;
                default:
                    pen.DashStyle = Drawing.Drawing2D.DashStyle.Solid;
                    break;
            }

            return pen;
        }

        private Drawing.Brush? GetBrush(Drawing.Color color)
        {
            if (color == Drawing.Color.Empty)
            {
                return null;
            }

            return new Drawing.SolidBrush(color);
        }

        private Drawing.Font? GetFont(PageText? pt)
        {
            if (pt == null || pt.SI == null)
            {
                return null;
            }

            var fontFamily = new Drawing.FontFamily(pt.SI.FontFamily ?? "Arial");
            var fontSize = pt.SI.FontSize > 0 ? pt.SI.FontSize : 12f;
            var fontStyle = Drawing.FontStyle.Regular;

            if (pt.SI.FontWeight == FontWeightEnum.Bold)
            {
                fontStyle |= Drawing.FontStyle.Bold;
            }

            if (pt.SI.FontStyle == FontStyleEnum.Italic)
            {
                fontStyle |= Drawing.FontStyle.Italic;
            }

            switch (pt.SI.TextDecoration)
            {
                case TextDecorationEnum.Underline:
                    fontStyle |= Drawing.FontStyle.Underline;
                    break;
                case TextDecorationEnum.LineThrough:
                    fontStyle |= Drawing.FontStyle.Strikeout;
                    break;
            }

            return new Drawing.Font(fontFamily, fontSize, fontStyle);
        }

        private Drawing.StringFormat GetStringFormat(PageText pt)
        {
            var format = new Drawing.StringFormat();

            if (pt?.SI?.TextAlign != null)
            {
                switch (pt.SI.TextAlign.ToString().ToLower())
                {
                    case "right":
                        format.Alignment = Drawing.StringAlignment.Far;
                        break;
                    case "center":
                        format.Alignment = Drawing.StringAlignment.Center;
                        break;
                    case "left":
                    default:
                        format.Alignment = Drawing.StringAlignment.Near;
                        break;
                }
            }

            if (pt?.SI?.VerticalAlign != null)
            {
                switch (pt.SI.VerticalAlign.ToString().ToLower())
                {
                    case "bottom":
                        format.LineAlignment = Drawing.StringAlignment.Far;
                        break;
                    case "middle":
                        format.LineAlignment = Drawing.StringAlignment.Center;
                        break;
                    case "top":
                    default:
                        format.LineAlignment = Drawing.StringAlignment.Near;
                        break;
                }
            }


            return format;
        }
    }
}

