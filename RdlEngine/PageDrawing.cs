using fyiReporting.RDL;
using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace fyiReporting.RdlPrint
{
    /// <summary>
    /// Helper class used by RdlPrint
    /// </summary>
    public class PageDrawing
    {
        private Pages _pgs;  				// the pages of the report to view

        // During drawing these are set
        float _left;
        float _top;
        float _vScroll;
        float _hScroll;
        float DpiX;
        float DpiY;

        public PageDrawing() : this(null)
        {         
        }

        public PageDrawing(Pages pgs)
        {
            _pgs = pgs;
        }

        /// <summary>
        /// Enabling the SelectTool allows the user to select text and images.  Enabling or disabling
        /// the SelectTool also clears out the current selection.
        /// </summary>
        internal Pages Pgs
        {
            get { return _pgs; }
            set { _pgs = value; }
        }

        /// <summary>
        /// Draw- simple draw of an entire page.  Useful when printing or creating an image.
        /// </summary>
        /// <param name="g"></param>
        /// <param name="page"></param>
        /// <param name="clipRectangle"></param>
        public void Draw(Graphics g, int page, System.Drawing.Rectangle clipRectangle, bool drawBackground, PointF pageOffset)
        {
            DpiX = g.DpiX;			 // this can change (e.g. printing graphics context)
            DpiY = g.DpiY;

            //			g.InterpolationMode = InterpolationMode.HighQualityBilinear;	// try to unfuzz charts
            g.PageUnit = GraphicsUnit.Pixel;
            g.ScaleTransform(1, 1);

            if (!pageOffset.IsEmpty)    // used when correcting for non-printable area on paper
            {
                g.TranslateTransform(pageOffset.X, pageOffset.Y);
            }

            _left = 0;
            _top = 0;
            _hScroll = 0;
            _vScroll = 0;

            RectangleF r = new RectangleF(clipRectangle.X, clipRectangle.Y,
                                            clipRectangle.Width, clipRectangle.Height);

            if (drawBackground)
                g.FillRectangle(Brushes.White, PixelsX(_left), PixelsY(_top),
                    PixelsX(_pgs.PageWidth), PixelsY(_pgs.PageHeight));

            ProcessPage(g, _pgs[page], r, false);
        }

        internal float PixelsX(float x)
        {
            return (float)(x * DpiX / 72.0f);
        }

        internal float PixelsY(float y)
        {
            return (float)(y * DpiY / 72.0f);
        }

        // render all the objects in a page (or any composite object
        private void ProcessPage(Graphics g, IEnumerable p, RectangleF clipRect, bool bHitList)
        {
            // TODO: (Peter) Support can grow and can shrink
            foreach (PageItem pi in p)
            {
                //if (pi is PageTextHtml)
                //{	// PageTextHtml is actually a composite object (just like a page)
                //    if (SelectTool && bHitList)
                //    {
                //        RectangleF hr = new RectangleF(PixelsX(pi.X + _left - _hScroll), PixelsY(pi.Y + _top - _vScroll),
                //                                                            PixelsX(pi.W), PixelsY(pi.H));
                //        _HitList.Add(new HitListEntry(hr, pi));
                //    }
                //    ProcessHtml(pi as PageTextHtml, g, clipRect, bHitList);
                //    continue;
                //}

                if (pi is PageLine)
                {
                    PageLine pl = pi as PageLine;
                    DrawLine(pl.SI.BColorLeft, pl.SI.BStyleLeft, pl.SI.BWidthLeft,
                        g, PixelsX(pl.X + _left - _hScroll), PixelsY(pl.Y + _top - _vScroll),
                        PixelsX(pl.X2 + _left - _hScroll), PixelsY(pl.Y2 + _top - _vScroll));
                    continue;
                }


                RectangleF rect = new RectangleF(PixelsX(pi.X + _left - _hScroll), PixelsY(pi.Y + _top - _vScroll),
                                                                    PixelsX(pi.W), PixelsY(pi.H));

                // Maintain the hit list
                //if (bHitList)
                //{
                //    if (SelectTool)
                //    {   // we need all PageText and PageImage items that have been displayed
                //        if (pi is PageText || pi is PageImage)
                //        {
                //            _HitList.Add(new HitListEntry(rect, pi));
                //        }
                //    }
                //    // Only care about items with links and tips
                //    else if (pi.HyperLink != null || pi.BookmarkLink != null || pi.Tooltip != null)
                //    {
                //        HitListEntry hle;
                //        if (pi is PagePolygon)
                //            hle = new HitListEntry(pi as PagePolygon, _left - _hScroll, _top - _vScroll, this);
                //        else
                //            hle = new HitListEntry(rect, pi);
                //        _HitList.Add(hle);
                //    }
                //}

                if ((pi is PagePolygon) || (pi is PageCurve))
                { // intentionally empty; polygon's rectangles aren't calculated
                }
                else if (!rect.IntersectsWith(clipRect))
                    continue;

                if (pi.SI.BackgroundImage != null)
                {	// put out any background image
                    PageImage i = pi.SI.BackgroundImage;
                    DrawImageBackground(i, pi.SI, g, rect);
                }

                if (pi is PageText)
                {
                    // TODO: enable can shrink, can grow
                    // 2005 spec file, page 9, in the text box has
                    // CanGrow and CanShrink
                    PageText pt = pi as PageText;
                    DrawString(pt, g, rect);
                }
                else if (pi is PageImage)
                {
                    PageImage i = pi as PageImage;
                    DrawImage(i, g, rect);
                }
                else if (pi is PageRectangle)
                {
                    this.DrawBackground(g, rect, pi.SI);
                }
                else if (pi is PageEllipse)
                {
                    PageEllipse pe = pi as PageEllipse;
                    DrawEllipse(pe, g, rect);
                }
                else if (pi is PagePie)
                {
                    PagePie pp = pi as PagePie;
                    DrawPie(pp, g, rect);
                }
                else if (pi is PagePolygon)
                {
                    PagePolygon ppo = pi as PagePolygon;
                    FillPolygon(ppo, g, rect);
                }
                else if (pi is PageCurve)
                {
                    PageCurve pc = pi as PageCurve;
                    DrawCurve(pc.SI.BColorLeft, pc.SI.BStyleLeft, pc.SI.BWidthLeft,
                        g, pc.Points, pc.Offset, pc.Tension);
                }


                DrawBorder(pi, g, rect);
            }
        }

        private void DrawBackground(Graphics g, System.Drawing.RectangleF rect, StyleInfo si)
        {
            LinearGradientBrush linGrBrush = null;
            SolidBrush sb = null;
            HatchBrush hb = null;
            try
            {
                if (si.BackgroundGradientType != BackgroundGradientTypeEnum.None &&
                    !si.BackgroundGradientEndColor.IsEmpty &&
                    !si.BackgroundColor.IsEmpty)
                {
                    Color c = si.BackgroundColor;
                    Color ec = si.BackgroundGradientEndColor;

                    switch (si.BackgroundGradientType)
                    {
                        case BackgroundGradientTypeEnum.LeftRight:
                            linGrBrush = new LinearGradientBrush(rect, c, ec, LinearGradientMode.Horizontal);
                            break;
                        case BackgroundGradientTypeEnum.TopBottom:
                            linGrBrush = new LinearGradientBrush(rect, c, ec, LinearGradientMode.Vertical);
                            break;
                        case BackgroundGradientTypeEnum.Center:
                            linGrBrush = new LinearGradientBrush(rect, c, ec, LinearGradientMode.Horizontal);
                            break;
                        case BackgroundGradientTypeEnum.DiagonalLeft:
                            linGrBrush = new LinearGradientBrush(rect, c, ec, LinearGradientMode.ForwardDiagonal);
                            break;
                        case BackgroundGradientTypeEnum.DiagonalRight:
                            linGrBrush = new LinearGradientBrush(rect, c, ec, LinearGradientMode.BackwardDiagonal);
                            break;
                        case BackgroundGradientTypeEnum.HorizontalCenter:
                            linGrBrush = new LinearGradientBrush(rect, c, ec, LinearGradientMode.Horizontal);
                            break;
                        case BackgroundGradientTypeEnum.VerticalCenter:
                            linGrBrush = new LinearGradientBrush(rect, c, ec, LinearGradientMode.Vertical);
                            break;
                        default:
                            break;
                    }
                }
                if (si.PatternType != patternTypeEnum.None)
                {
                    switch (si.PatternType)
                    {
                        case patternTypeEnum.BackwardDiagonal:
                            hb = new HatchBrush(HatchStyle.BackwardDiagonal, si.Color, si.BackgroundColor);
                            break;
                        case patternTypeEnum.CheckerBoard:
                            hb = new HatchBrush(HatchStyle.LargeCheckerBoard, si.Color, si.BackgroundColor);
                            break;
                        case patternTypeEnum.Cross:
                            hb = new HatchBrush(HatchStyle.Cross, si.Color, si.BackgroundColor);
                            break;
                        case patternTypeEnum.DarkDownwardDiagonal:
                            hb = new HatchBrush(HatchStyle.DarkDownwardDiagonal, si.Color, si.BackgroundColor);
                            break;
                        case patternTypeEnum.DarkHorizontal:
                            hb = new HatchBrush(HatchStyle.DarkHorizontal, si.Color, si.BackgroundColor);
                            break;
                        case patternTypeEnum.DiagonalBrick:
                            hb = new HatchBrush(HatchStyle.DiagonalBrick, si.Color, si.BackgroundColor);
                            break;
                        case patternTypeEnum.HorizontalBrick:
                            hb = new HatchBrush(HatchStyle.HorizontalBrick, si.Color, si.BackgroundColor);
                            break;
                        case patternTypeEnum.LargeConfetti:
                            hb = new HatchBrush(HatchStyle.LargeConfetti, si.Color, si.BackgroundColor);
                            break;
                        case patternTypeEnum.OutlinedDiamond:
                            hb = new HatchBrush(HatchStyle.OutlinedDiamond, si.Color, si.BackgroundColor);
                            break;
                        case patternTypeEnum.SmallConfetti:
                            hb = new HatchBrush(HatchStyle.SmallConfetti, si.Color, si.BackgroundColor);
                            break;
                        case patternTypeEnum.SolidDiamond:
                            hb = new HatchBrush(HatchStyle.SolidDiamond, si.Color, si.BackgroundColor);
                            break;
                        case patternTypeEnum.Vertical:
                            hb = new HatchBrush(HatchStyle.Vertical, si.Color, si.BackgroundColor);
                            break;
                    }
                }

                if (linGrBrush != null)
                {
                    g.FillRectangle(linGrBrush, rect);
                    linGrBrush.Dispose();
                }
                else if (hb != null)
                {
                    g.FillRectangle(hb, rect);
                    hb.Dispose();
                }
                else if (!si.BackgroundColor.IsEmpty)
                {
                    sb = new SolidBrush(si.BackgroundColor);
                    g.FillRectangle(sb, rect);
                    sb.Dispose();
                }
            }
            finally
            {
                if (linGrBrush != null)
                    linGrBrush.Dispose();
                if (sb != null)
                    sb.Dispose();
            }
            return;
        }

        private void DrawBorder(PageItem pi, Graphics g, RectangleF r)
        {
            if (pi.GetType().Name.Equals("PagePie")) return;
            if (r.Height <= 0 || r.Width <= 0)		// no bounding box to use
                return;

            StyleInfo si = pi.SI;

            DrawLine(si.BColorTop, si.BStyleTop, si.BWidthTop, g, r.X, r.Y, r.Right, r.Y);

            DrawLine(si.BColorRight, si.BStyleRight, si.BWidthRight, g, r.Right, r.Y, r.Right, r.Bottom);

            DrawLine(si.BColorLeft, si.BStyleLeft, si.BWidthLeft, g, r.X, r.Y, r.X, r.Bottom);

            DrawLine(si.BColorBottom, si.BStyleBottom, si.BWidthBottom, g, r.X, r.Bottom, r.Right, r.Bottom);

            return;

        }

        private void DrawImage(PageImage pi, Graphics g, RectangleF r)
        {
            Stream strm = null;
            System.Drawing.Image im = null;
            try
            {
                strm = new MemoryStream(pi.GetImageData((int)r.Width, (int)r.Height));
                im = System.Drawing.Image.FromStream(strm);
                DrawImageSized(pi, im, g, r);
            }
            finally
            {
                if (strm != null)
                    strm.Close();
                if (im != null)
                    im.Dispose();
            }

        }
        private void DrawImageBackground(PageImage pi, StyleInfo si, Graphics g, RectangleF r)
        {
            Stream strm = null;
            System.Drawing.Image im = null;
            try
            {
                // http://www.fyireporting.com/forum/viewtopic.php?t=892
                //A.S.> convert pt to px if needed(when printing we need px, when draw preview - pt) 

                RectangleF r2;
                if (g.PageUnit == GraphicsUnit.Pixel)
                {
                    r2 = new RectangleF(r.Left + (si.PaddingLeft * g.DpiX) / 72,
                    r.Top + (si.PaddingTop * g.DpiX) / 72,
                    r.Width - ((si.PaddingLeft + si.PaddingRight) * g.DpiX) / 72,
                    r.Height - ((si.PaddingTop + si.PaddingBottom) * g.DpiX) / 72);
                }
                else
                {
                    // adjust drawing rectangle based on padding
                    r2 = new RectangleF(r.Left + si.PaddingLeft,
                    r.Top + si.PaddingTop,
                    r.Width - si.PaddingLeft - si.PaddingRight,
                    r.Height - si.PaddingTop - si.PaddingBottom);
                }

                strm = new MemoryStream(pi.GetImageData((int)r2.Width, (int)r2.Height));
                im = System.Drawing.Image.FromStream(strm);

                int repeatX = 0;
                int repeatY = 0;
                switch (pi.Repeat)
                {
                    case ImageRepeat.Repeat:
                        repeatX = (int)Math.Floor(r2.Width / pi.SamplesW);
                        repeatY = (int)Math.Floor(r2.Height / pi.SamplesH);
                        break;
                    case ImageRepeat.RepeatX:
                        repeatX = (int)Math.Floor(r2.Width / pi.SamplesW);
                        repeatY = 1;
                        break;
                    case ImageRepeat.RepeatY:
                        repeatY = (int)Math.Floor(r2.Height / pi.SamplesH);
                        repeatX = 1;
                        break;
                    case ImageRepeat.NoRepeat:
                    default:
                        repeatX = repeatY = 1;
                        break;
                }

                //make sure the image is drawn at least 1 times 
                repeatX = Math.Max(repeatX, 1);
                repeatY = Math.Max(repeatY, 1);

                float startX = r2.Left;
                float startY = r2.Top;

                Region saveRegion = g.Clip;
                Region clipRegion = new Region(g.Clip.GetRegionData());
                clipRegion.Intersect(r2);
                g.Clip = clipRegion;

                for (int i = 0; i < repeatX; i++)
                {
                    for (int j = 0; j < repeatY; j++)
                    {
                        float currX = startX + i * pi.SamplesW;
                        float currY = startY + j * pi.SamplesH;
                        g.DrawImage(im, new RectangleF(currX, currY, pi.SamplesW, pi.SamplesH));
                    }
                }
                g.Clip = saveRegion;
            }
            finally
            {
                if (strm != null)
                    strm.Close();
                if (im != null)
                    im.Dispose();
            }
        }
        private void DrawImageSized(PageImage pi, System.Drawing.Image im, Graphics g, RectangleF r)
        {
            float height, width;		// some work variables
            StyleInfo si = pi.SI;

            // adjust drawing rectangle based on padding

            // http://www.fyireporting.com/forum/viewtopic.php?t=892
            //A.S.> convert pt to px if needed(when printing we need px, when draw preview - pt) 

            RectangleF r2;
            if (g.PageUnit == GraphicsUnit.Pixel)
            {
                r2 = new RectangleF(r.Left + (si.PaddingLeft * g.DpiX) / 72,
                r.Top + (si.PaddingTop * g.DpiX) / 72,
                r.Width - ((si.PaddingLeft + si.PaddingRight) * g.DpiX) / 72,
                r.Height - ((si.PaddingTop + si.PaddingBottom) * g.DpiX) / 72);
            }
            else
            {
                // adjust drawing rectangle based on padding
                r2 = new RectangleF(r.Left + si.PaddingLeft,
                r.Top + si.PaddingTop,
                r.Width - si.PaddingLeft - si.PaddingRight,
                r.Height - si.PaddingTop - si.PaddingBottom);
            } 

            System.Drawing.Rectangle ir;	// int work rectangle
            ir = new System.Drawing.Rectangle(Convert.ToInt32(r2.Left), Convert.ToInt32(r2.Top),
                               Convert.ToInt32(r2.Width), Convert.ToInt32(r2.Height));
            switch (pi.Sizing)
            {
                case ImageSizingEnum.AutoSize:
                    // Note: GDI+ will stretch an image when you only provide
                    //  the left/top coordinates.  This seems pretty stupid since
                    //  it results in the image being out of focus even though
                    //  you don't want the image resized.
                    if (g.DpiX == im.HorizontalResolution &&
                        g.DpiY == im.VerticalResolution)
                    {
                        ir = new System.Drawing.Rectangle(Convert.ToInt32(r2.Left), Convert.ToInt32(r2.Top),
                                                        im.Width, im.Height);
                    }
                    g.DrawImage(im, ir);

                    break;
                case ImageSizingEnum.Clip:
                    Region saveRegion = g.Clip;
                    Region clipRegion = new Region(g.Clip.GetRegionData());
                    clipRegion.Intersect(r2);
                    g.Clip = clipRegion;
                    if (g.DpiX == im.HorizontalResolution &&
                        g.DpiY == im.VerticalResolution)
                    {
                        ir = new System.Drawing.Rectangle(Convert.ToInt32(r2.Left), Convert.ToInt32(r2.Top),
                                                        im.Width, im.Height);
                    }
                    g.DrawImage(im, ir);
                    g.Clip = saveRegion;
                    break;
                case ImageSizingEnum.FitProportional:
                    float ratioIm = (float)im.Height / (float)im.Width;
                    float ratioR = r2.Height / r2.Width;
                    height = r2.Height;
                    width = r2.Width;
                    if (ratioIm > ratioR)
                    {	// this means the rectangle width must be corrected
                        width = height * (1 / ratioIm);
                    }
                    else if (ratioIm < ratioR)
                    {	// this means the ractangle height must be corrected
                        height = width * ratioIm;
                    }
                    r2 = new RectangleF(r2.X, r2.Y, width, height);
                    g.DrawImage(im, r2);
                    break;
                case ImageSizingEnum.Fit:
                default:
                    g.DrawImage(im, r2);
                    break;
            }

            //if (SelectTool && pi.AllowSelect && _SelectList.Contains(pi))
            //{
            //    g.FillRectangle(new SolidBrush(Color.FromArgb(50, _SelectItemColor)), ir);
            //}

            return;
        }

        private void DrawLine(Color c, BorderStyleEnum bs, float w, Graphics g,
                                float x, float y, float x2, float y2)
        {
            if (bs == BorderStyleEnum.None || c.IsEmpty || w <= 0)	// nothing to draw
                return;

            float tmpW = w;
            if (g.PageUnit == GraphicsUnit.Pixel)
                tmpW = (tmpW * g.DpiX) / 72;
            Pen p = new Pen(c, tmpW);
            try
            {
                p = new Pen(c, w);
                switch (bs)
                {
                    case BorderStyleEnum.Dashed:
                        p.DashStyle = DashStyle.Dash;
                        break;
                    case BorderStyleEnum.Dotted:
                        p.DashStyle = DashStyle.Dot;
                        break;
                    case BorderStyleEnum.Double:
                    case BorderStyleEnum.Groove:
                    case BorderStyleEnum.Inset:
                    case BorderStyleEnum.Solid:
                    case BorderStyleEnum.Outset:
                    case BorderStyleEnum.Ridge:
                    case BorderStyleEnum.WindowInset:
                    default:
                        p.DashStyle = DashStyle.Solid;
                        break;
                }

                g.DrawLine(p, x, y, x2, y2);
            }
            finally
            {
                if (p != null)
                    p.Dispose();
            }

        }

        private void DrawCurve(Color c, BorderStyleEnum bs, float w, Graphics g,
                                PointF[] points, int Offset, float Tension)
        {
            if (bs == BorderStyleEnum.None || c.IsEmpty || w <= 0)	// nothing to draw
                return;

            Pen p = null;
            try
            {
                p = new Pen(c, w);
                switch (bs)
                {
                    case BorderStyleEnum.Dashed:
                        p.DashStyle = DashStyle.Dash;
                        break;
                    case BorderStyleEnum.Dotted:
                        p.DashStyle = DashStyle.Dot;
                        break;
                    case BorderStyleEnum.Double:
                    case BorderStyleEnum.Groove:
                    case BorderStyleEnum.Inset:
                    case BorderStyleEnum.Solid:
                    case BorderStyleEnum.Outset:
                    case BorderStyleEnum.Ridge:
                    case BorderStyleEnum.WindowInset:
                    default:
                        p.DashStyle = DashStyle.Solid;
                        break;
                }
                PointF[] tmp = new PointF[points.Length];
                for (int i = 0; i < points.Length; i++)
                {

                    tmp[i].X = PixelsX(points[i].X + _left - _hScroll);
                    tmp[i].Y = PixelsY(points[i].Y + _top - _vScroll);
                }

                g.DrawCurve(p, tmp, Offset, tmp.Length - 1, Tension);
            }
            finally
            {
                if (p != null)
                    p.Dispose();
            }

        }

        private void DrawEllipse(PageEllipse pe, Graphics g, RectangleF r)
        {
            StyleInfo si = pe.SI;
            if (!si.BackgroundColor.IsEmpty)
            {
                g.FillEllipse(new SolidBrush(si.BackgroundColor), r);
            }
            if (si.BStyleTop != BorderStyleEnum.None)
            {
                Pen p = new Pen(si.BColorTop, si.BWidthTop);
                switch (si.BStyleTop)
                {
                    case BorderStyleEnum.Dashed:
                        p.DashStyle = DashStyle.Dash;
                        break;
                    case BorderStyleEnum.Dotted:
                        p.DashStyle = DashStyle.Dot;
                        break;
                    case BorderStyleEnum.Double:
                    case BorderStyleEnum.Groove:
                    case BorderStyleEnum.Inset:
                    case BorderStyleEnum.Solid:
                    case BorderStyleEnum.Outset:
                    case BorderStyleEnum.Ridge:
                    case BorderStyleEnum.WindowInset:
                    default:
                        p.DashStyle = DashStyle.Solid;
                        break;
                }
                g.DrawEllipse(p, r);
            }
        }

        private void FillPolygon(PagePolygon pp, Graphics g, RectangleF r)
        {

            StyleInfo si = pp.SI;
            PointF[] tmp = new PointF[pp.Points.Length];
            if (!si.BackgroundColor.IsEmpty)
            //RectangleF(PixelsX(pi.X + _left - _hScroll), PixelsY(pi.Y + _top - _vScroll), 
            //                                                                    PixelsX(pi.W), PixelsY(pi.H))           
            {
                for (int i = 0; i < pp.Points.Length; i++)
                {

                    tmp[i].X = PixelsX(pp.Points[i].X + _left - _hScroll);
                    tmp[i].Y = PixelsY(pp.Points[i].Y + _top - _vScroll);
                }
                g.FillPolygon(new SolidBrush(si.BackgroundColor), tmp);
            }
        }

        private void DrawPie(PagePie pp, Graphics g, RectangleF r)
        {
            StyleInfo si = pp.SI;
            if (!si.BackgroundColor.IsEmpty)
            {
                g.FillPie(new SolidBrush(si.BackgroundColor), (int)r.X, (int)r.Y, (int)r.Width, (int)r.Height, (float)pp.StartAngle, (float)pp.SweepAngle);
            }

            if (si.BStyleTop != BorderStyleEnum.None)
            {
                Pen p = new Pen(si.BColorTop, si.BWidthTop);
                switch (si.BStyleTop)
                {
                    case BorderStyleEnum.Dashed:
                        p.DashStyle = DashStyle.Dash;
                        break;
                    case BorderStyleEnum.Dotted:
                        p.DashStyle = DashStyle.Dot;
                        break;
                    case BorderStyleEnum.Double:
                    case BorderStyleEnum.Groove:
                    case BorderStyleEnum.Inset:
                    case BorderStyleEnum.Solid:
                    case BorderStyleEnum.Outset:
                    case BorderStyleEnum.Ridge:
                    case BorderStyleEnum.WindowInset:
                    default:
                        p.DashStyle = DashStyle.Solid;
                        break;
                }
                g.DrawPie(p, r, pp.StartAngle, pp.SweepAngle);
            }
        }

        private void DrawString(PageText pt, Graphics g, RectangleF r)
        {
            StyleInfo si = pt.SI;
            string s = pt.Text;

            Font drawFont = null;
            StringFormat drawFormat = null;
            Brush drawBrush = null;
            try
            {
                // STYLE
                System.Drawing.FontStyle fs = 0;
                if (si.FontStyle == FontStyleEnum.Italic)
                    fs |= System.Drawing.FontStyle.Italic;

                switch (si.TextDecoration)
                {
                    case TextDecorationEnum.Underline:
                        fs |= System.Drawing.FontStyle.Underline;
                        break;
                    case TextDecorationEnum.LineThrough:
                        fs |= System.Drawing.FontStyle.Strikeout;
                        break;
                    case TextDecorationEnum.Overline:
                    case TextDecorationEnum.None:
                        break;
                }

                // WEIGHT
                switch (si.FontWeight)
                {
                    case FontWeightEnum.Bold:
                    case FontWeightEnum.Bolder:
                    case FontWeightEnum.W500:
                    case FontWeightEnum.W600:
                    case FontWeightEnum.W700:
                    case FontWeightEnum.W800:
                    case FontWeightEnum.W900:
                        fs |= System.Drawing.FontStyle.Bold;
                        break;
                    default:
                        break;
                }
                try
                {
                    drawFont = new Font(si.GetFontFamily(), si.FontSize, fs);	// si.FontSize already in points
                }
                catch (ArgumentException)
                {
                    drawFont = new Font("Arial", si.FontSize, fs);	// if this fails we'll let the error pass thru
                }
                // ALIGNMENT
                drawFormat = new StringFormat();
                switch (si.TextAlign)
                {
                    case TextAlignEnum.Right:
                        drawFormat.Alignment = StringAlignment.Far;
                        break;
                    case TextAlignEnum.Center:
                        drawFormat.Alignment = StringAlignment.Center;
                        break;
                    case TextAlignEnum.Left:
                    default:
                        drawFormat.Alignment = StringAlignment.Near;
                        break;
                }
                if (pt.SI.WritingMode == WritingModeEnum.tb_rl)
                {
                    drawFormat.FormatFlags |= StringFormatFlags.DirectionRightToLeft;
                    drawFormat.FormatFlags |= StringFormatFlags.DirectionVertical;
                }
                switch (si.VerticalAlign)
                {
                    case VerticalAlignEnum.Bottom:
                        drawFormat.LineAlignment = StringAlignment.Far;
                        break;
                    case VerticalAlignEnum.Middle:
                        drawFormat.LineAlignment = StringAlignment.Center;
                        break;
                    case VerticalAlignEnum.Top:
                    default:
                        drawFormat.LineAlignment = StringAlignment.Near;
                        break;
                }
                // draw the background 
                DrawBackground(g, r, si);

                // adjust drawing rectangle based on padding
                // http://www.fyireporting.com/forum/viewtopic.php?t=892
                //A.S.> convert pt to px if needed(when printing we need px, when draw preview - pt) 
                RectangleF r2;
                if (g.PageUnit == GraphicsUnit.Pixel)
                {
                    r2 = new RectangleF(r.Left + (si.PaddingLeft * g.DpiX) / 72,
                    r.Top + (si.PaddingTop * g.DpiX) / 72,
                    r.Width - ((si.PaddingLeft + si.PaddingRight) * g.DpiX) / 72,
                    r.Height - ((si.PaddingTop + si.PaddingBottom) * g.DpiX) / 72);
                }
                else
                {
                    // adjust drawing rectangle based on padding
                   r2 = new RectangleF(r.Left + si.PaddingLeft,
                   r.Top + si.PaddingTop,
                   r.Width - si.PaddingLeft - si.PaddingRight,
                   r.Height - si.PaddingTop - si.PaddingBottom);
                } 

                drawBrush = new SolidBrush(si.Color);
                
                // Handle rotation for non-standard writing modes
                System.Drawing.Drawing2D.GraphicsState graphicsState = null;
                if (si.WritingMode == WritingModeEnum.rl_bt || si.WritingMode == WritingModeEnum.tb_lr)
                {
                    graphicsState = g.Save();
                    
                    // Calculate rotation based on writing mode
                    if (si.WritingMode == WritingModeEnum.rl_bt)
                    {
                        // 180 degree rotation
                        g.TranslateTransform(r.Left + r.Width / 2, r.Top + r.Height / 2);
                        g.RotateTransform(180);
                        g.TranslateTransform(-(r.Left + r.Width / 2), -(r.Top + r.Height / 2));
                    }
                    else if (si.WritingMode == WritingModeEnum.tb_lr)
                    {
                        // 270 degree rotation (90 counter-clockwise)
                        g.TranslateTransform(r.Left, r.Top);
                        g.RotateTransform(270);
                        g.TranslateTransform(-r.Left, -r.Top);
                    }
                }
                
                if (si.TextAlign == TextAlignEnum.Justified)
                {
                    GraphicsExtended.DrawStringJustified(g, pt.Text, drawFont, drawBrush, r2); 
                }
                else if (pt.NoClip)	// request not to clip text
                {
                    g.DrawString(pt.Text, drawFont, drawBrush, new PointF(r.Left, r.Top), drawFormat);
                    //HighlightString(g, pt, new RectangleF(r.Left, r.Top, float.MaxValue, float.MaxValue),
                    //    drawFont, drawFormat);
                }
                else
                {
                    g.DrawString(pt.Text, drawFont, drawBrush, r2, drawFormat);
                    //HighlightString(g, pt, r2, drawFont, drawFormat);
                }
                
                // Restore graphics state if we applied rotation
                if (graphicsState != null)
                {
                    g.Restore(graphicsState);
                }
                
                //if (SelectTool)
                //{
                //    if (pt.AllowSelect && _SelectList.Contains(pt))
                //        g.FillRectangle(new SolidBrush(Color.FromArgb(50, _SelectItemColor)), r2);
                //}
            }
            finally
            {
                if (drawFont != null)
                    drawFont.Dispose();
                if (drawFormat != null)
                    drawFont.Dispose();
                if (drawBrush != null)
                    drawBrush.Dispose();
            }
        }

    }
}
