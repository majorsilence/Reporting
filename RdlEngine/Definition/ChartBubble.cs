
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

#if DRAWINGCOMPAT
using Draw2 = Majorsilence.Forms.Drawing;
#else
using Draw2 = System.Drawing;
#endif


namespace Majorsilence.Reporting.Rdl
{
    ///<summary>
    /// Line chart definition and processing.
    ///</summary>
    [Serializable]
    internal class ChartBubble : ChartBase
    {

        internal ChartBubble(Report r, Row row, Chart c, MatrixCellEntry[,] m, Expression showTooltips, Expression showTooltipsX, Expression _ToolTipYFormat, Expression _ToolTipXFormat)
            : base(r, row, c, m, showTooltips, showTooltipsX, _ToolTipYFormat, _ToolTipXFormat)
        {
        }

        override internal async Task Draw(Report rpt)
        {
            CreateSizedBitmap();

#if !DRAWINGCOMPAT
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using (Draw2.Graphics g1 = Draw2.Graphics.FromImage(_bm))
                {
                    _aStream = new System.IO.MemoryStream();
                    IntPtr HDC = g1.GetHdc();
                    _mf = new Draw2.Imaging.Metafile(_aStream, HDC,
                        new System.Drawing.RectangleF(0, 0, _bm.Width, _bm.Height), Draw2.Imaging.MetafileFrameUnit.Pixel);
                    g1.ReleaseHdc(HDC);
                }
            }

            using (Draw2.Graphics g = Draw2.Graphics.FromImage(_mf != null ? _mf : _bm))
#else
            using (Draw2.Graphics g = Draw2.Graphics.FromImage(_bm))
#endif
            {
                // 06122007AJM Used to Force Higher Quality
                g.InterpolationMode = Draw2.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = Draw2.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = Draw2.Drawing2D.PixelOffsetMode.None;
                g.CompositingQuality = Draw2.Drawing2D.CompositingQuality.HighQuality;
                g.PageUnit = Draw2.GraphicsUnit.Pixel;

                // Adjust the top margin to depend on the title height
                System.Drawing.Size titleSize = await DrawTitleMeasure(rpt, g, ChartDefn.Title);
                Layout.TopMargin = titleSize.Height;

                // 20022008 AJM GJL - Added new required info 
                double ymax = 0, ymin = 0;  // Get the max and min values for the y axis
                (ymax, ymin) = await GetValueMaxMin(rpt, ymax, ymin, 1, 1);

                double xmax = 0, xmin = 0;  // Get the max and min values for the x axis
                (xmax, xmin) = await GetValueMaxMin(rpt, xmax, xmin, 0, 1);

                double bmax = 0, bmin = 0;  // Get the max and min values for the bubble size
                if (ChartDefn.Type == ChartTypeEnum.Bubble)     // only applies to bubble (not scatter)
                    (bmax, bmin) = await GetValueMaxMin(rpt, bmax, bmin, 2, 1);

                await DrawChartStyle(rpt, g);

                // Draw title; routine determines if necessary
                await DrawTitle(rpt, g, ChartDefn.Title, new System.Drawing.Rectangle(0, 0, Layout.Width, Layout.TopMargin));

                // Adjust the left margin to depend on the Value Axis
                System.Drawing.Size vaSize = await ValueAxisSize(rpt, g, ymin, ymax);
                Layout.LeftMargin = vaSize.Width;

                // Draw legend
                System.Drawing.Rectangle lRect = await DrawLegend(rpt, g, false, true);

                // Adjust the bottom margin to depend on the Category Axis
                System.Drawing.Size caSize = await CategoryAxisSize(rpt, g, xmin, xmax);
                Layout.BottomMargin = caSize.Height;

                AdjustMargins(lRect, rpt, g);       // Adjust margins based on legend.

                // Draw Plot area
                await DrawPlotAreaStyle(rpt, g, lRect);

                // Draw Value Axis
                if (vaSize.Width > 0)   // If we made room for the axis - we need to draw it
                    await DrawValueAxis(rpt, g, ymin, ymax,
                        new System.Drawing.Rectangle(Layout.LeftMargin - vaSize.Width, Layout.TopMargin, vaSize.Width, Layout.PlotArea.Height), Layout.LeftMargin, _bm.Width - Layout.RightMargin);

                // Draw Category Axis
                if (caSize.Height > 0)
                    await DrawCategoryAxis(rpt, g, xmin, xmax,
                        new System.Drawing.Rectangle(Layout.LeftMargin, _bm.Height - Layout.BottomMargin, _bm.Width - Layout.LeftMargin - Layout.RightMargin, vaSize.Height),
                        Layout.TopMargin, _bm.Height - Layout.BottomMargin);

                // Draw Plot area data 
                await DrawPlot(rpt, g, xmin, xmax, ymin, ymax, bmin, bmax);
                await DrawLegend(rpt, g, false, false);
            }

        }

        async Task DrawPlot(Report rpt, Draw2.Graphics g, double xmin, double xmax, double ymin, double ymax, double bmin, double bmax)
        {
            // Draw Plot area data 
            int maxPointHeight = (int)Layout.PlotArea.Height;
            int maxPointWidth = (int)Layout.PlotArea.Width;



            for (int iCol = 1; iCol <= SeriesCount; iCol++)
            {
                //handle either line scatter or line plot type GJL 020308
                System.Drawing.Point lastPoint = new System.Drawing.Point();
                System.Drawing.Point[] Points = new System.Drawing.Point[2];
                bool isLine = GetPlotType(rpt, iCol, 1).ToUpper() == "LINE";
                for (int iRow = 1; iRow <= CategoryCount; iRow++)
                {
                    double xv = await this.GetDataValue(rpt, iRow, iCol, 0);
                    double yv = await this.GetDataValue(rpt, iRow, iCol, 1);
                    double bv = this.ChartDefn.Type == ChartTypeEnum.Bubble ?
                        await this.GetDataValue(rpt, iRow, iCol, 2) : 0;
                    if (xv < xmin || yv < ymin || xv > xmax || yv > ymax)
                        continue;
                    int x = (int)(((Math.Min(xv, xmax) - xmin) / (xmax - xmin)) * maxPointWidth);
                    int y = (int)(((Math.Min(yv, ymax) - ymin) / (ymax - ymin)) * maxPointHeight);
                    if (y != int.MinValue && x != int.MinValue)
                    {
                        System.Drawing.Point p = new System.Drawing.Point(Layout.PlotArea.Left + x, Layout.PlotArea.Top + (maxPointHeight - y));
                        //GJL 010308 Line subtype scatter plot
                        if ((ChartSubTypeEnum)Enum.Parse(typeof(ChartSubTypeEnum), await _ChartDefn.Subtype.EvaluateString(rpt, _row)) == ChartSubTypeEnum.Line || (ChartSubTypeEnum)Enum.Parse(typeof(ChartSubTypeEnum), await _ChartDefn.Subtype.EvaluateString(rpt, _row)) == ChartSubTypeEnum.SmoothLine || isLine)
                        {
                            if (!(lastPoint.IsEmpty))
                            {
                                Points[0] = lastPoint;
                                Points[1] = p;
                                String LineSize = getLineSize(rpt, iCol, 1);
                                int intLineSize = 2;
                                switch (LineSize)
                                {
                                    case "Small":
                                        intLineSize = 1;
                                        break;
                                    case "Regular":
                                        intLineSize = 2;
                                        break;
                                    case "Large":
                                        intLineSize = 3;
                                        break;
                                    case "Extra Large":
                                        intLineSize = 4;
                                        break;
                                    case "Super Size":
                                        intLineSize = 5;
                                        break;
                                }
                                await DrawLineBetweenPoints(g, rpt, await GetSeriesBrush(rpt, iRow, iCol), Points, intLineSize);
                                //Add a metafilecomment to use as a tooltip GJL 26092008                          

                                if (_showToolTips || _showToolTipsX)
                                {
                                    string display = "";
                                    if (_showToolTipsX) display = xv.ToString(_tooltipXFormat);
                                    if (_showToolTips)
                                    {
                                        if (display.Length > 0) display += " , ";
                                        display += yv.ToString(_tooltipYFormat);
                                    }
                                    String val = "ToolTip:" + display + "|X:" + (int)(p.X - 3) + "|Y:" + (int)(p.Y - 3) + "|W:" + 6 + "|H:" + 6;
                                    g.AddMetafileComment(new System.Text.ASCIIEncoding().GetBytes(val));
                                }


                            }
                        }
                        else
                        {
                            await DrawBubble(rpt, g, await GetSeriesBrush(rpt, iRow, iCol), p, iRow, iCol, bmin, bmax, bv, xv, yv);

                        }
                        lastPoint = p;
                    }
                }
            }
            return;
        }

        /* This code was copied from the Line drawing class. 
        * 010308 GJL */
        async Task DrawLineBetweenPoints(Draw2.Graphics g, Report rpt, Draw2.Brush brush, System.Drawing.Point[] points)
        {
            await DrawLineBetweenPoints(g, rpt, brush, points, 2);
        }

        async Task DrawLineBetweenPoints(Draw2.Graphics g, Report rpt, Draw2.Brush brush, System.Drawing.Point[] points, int intLineSize)
        {
            if (points.Length <= 1)		// Need at least 2 points
                return;

            Draw2.Pen p = null;
            try
            {
                if (brush.GetType() == typeof(Draw2.Drawing2D.HatchBrush))
                {
                    Draw2.Drawing2D.HatchBrush tmpBrush = (Draw2.Drawing2D.HatchBrush)brush;
                    p = new Draw2.Pen(new Draw2.SolidBrush(tmpBrush.ForegroundColor), intLineSize); //1.5F);    // todo - use line from style ????
                }
                else
                {
                    p = new Draw2.Pen(brush, intLineSize);
                }

                if ((ChartSubTypeEnum)Enum.Parse(typeof(ChartSubTypeEnum), await _ChartDefn.Subtype.EvaluateString(rpt, _row)) == ChartSubTypeEnum.Smooth && points.Length > 2)
                    g.DrawCurve(p, points, 0.5F);
                else
                    g.DrawLines(p, points);
            }
            finally
            {
                if (p != null)
                    p.Dispose();
            }
            return;
        }

        async Task DrawBubble(Report rpt, Draw2.Graphics g, Draw2.Brush brush, System.Drawing.Point p, int iRow, int iCol, double bmin, double bmax, double bv, double xv, double yv)
        {
            Draw2.Pen pen = null;
            int diameter = BubbleSize(rpt, iRow, iCol, bmin, bmax, bv);          // set diameter of bubble

            int radius = diameter / 2;
            try
            {
                if (this.ChartDefn.Type == ChartTypeEnum.Scatter &&
                    brush.GetType() == typeof(Draw2.Drawing2D.HatchBrush))
                {
                    Draw2.Drawing2D.HatchBrush tmpBrush = (Draw2.Drawing2D.HatchBrush)brush;
                    Draw2.SolidBrush br = new Draw2.SolidBrush(tmpBrush.ForegroundColor);
                    pen = new Draw2.Pen(new Draw2.SolidBrush(tmpBrush.ForegroundColor));
                    DrawLegendMarker(g, br, pen, SeriesMarker[iCol - 1], p.X - radius, p.Y - radius, diameter);
                    await DrawDataPoint(rpt, g, new System.Drawing.Point(p.X - 3, p.Y + 3), iRow, iCol);

                }
                else
                {
                    pen = new Draw2.Pen(brush);
                    DrawLegendMarker(g, brush, pen, ChartMarkerEnum.Bubble, p.X - radius, p.Y - radius, diameter);
                    await DrawDataPoint(rpt, g, new System.Drawing.Point(p.X - 3, p.Y + 3), iRow, iCol);
                }
                //Add a metafilecomment to use as a tooltip GJL 26092008               

                if (_showToolTips || _showToolTipsX)
                {
                    string display = "";
                    if (_showToolTipsX) display = xv.ToString(_tooltipXFormat);
                    if (_showToolTips)
                    {
                        if (display.Length > 0) display += " , ";
                        display += yv.ToString(_tooltipYFormat);
                    }
                    String val = "ToolTip:" + display + "|X:" + (int)(p.X - 3) + "|Y:" + (int)(p.Y - 3) + "|W:" + 6 + "|H:" + 6;
                    g.AddMetafileComment(new System.Text.ASCIIEncoding().GetBytes(val));
                }
            }
            finally
            {
                if (pen != null)
                    pen.Dispose();
            }

            return;
        }

        private int BubbleSize(Report rpt, int iRow, int iCol, double minB, double maxB, double bv)
        {
            int diameter = 5;
            if (ChartDefn.Type != ChartTypeEnum.Bubble)
                return diameter;

            int bubbleMax = 30;           // maximum bubble size
            int bubbleMin = 4;          // minimum bubble size

            double diff = maxB - minB;
            double vdiff = bv - minB;

            if (Math.Abs(diff) < 1e-9d)     // very small difference between max and min?
                return diameter;            // just use the smallest 

            diameter = (int)(((vdiff / diff) * (bubbleMax - bubbleMin)) + bubbleMin);

            return diameter;
        }

        // Calculate the size of the value axis; width is max value width + title width
        //										 height is max value height
        protected async Task<System.Drawing.Size> ValueAxisSize(Report rpt, Draw2.Graphics g, double min, double max)
        {
            System.Drawing.Size size = System.Drawing.Size.Empty;
            if (ChartDefn.ValueAxis == null)
                return size;
            Axis a = ChartDefn.ValueAxis.Axis;
            if (a == null)
                return size;

            System.Drawing.Size minSize;
            System.Drawing.Size maxSize;
            if (!a.Visible)
            {
                minSize = maxSize = System.Drawing.Size.Empty;
            }
            else if (a.Style != null)
            {
                minSize = await a.Style.MeasureString(rpt, g, min, TypeCode.Double, null, int.MaxValue);
                maxSize = await a.Style.MeasureString(rpt, g, max, TypeCode.Double, null, int.MaxValue);
            }
            else
            {
                minSize = await Style.MeasureStringDefaults(rpt, g, min, TypeCode.Double, null, int.MaxValue);
                maxSize = await Style.MeasureStringDefaults(rpt, g, max, TypeCode.Double, null, int.MaxValue);
            }
            // Choose the largest
            size.Width = Math.Max(minSize.Width, maxSize.Width);
            size.Height = Math.Max(minSize.Height, maxSize.Height);

            // Now we need to add in the width of the title (if any)
            System.Drawing.Size titleSize = await DrawTitleMeasure(rpt, g, a.Title);
            size.Width += titleSize.Width;

            return size;
        }

        protected async Task DrawValueAxis(Report rpt, Draw2.Graphics g, double min, double max,
                        System.Drawing.Rectangle rect, int plotLeft, int plotRight)
        {
            if (this.ChartDefn.ValueAxis == null)
                return;
            Axis a = this.ChartDefn.ValueAxis.Axis;
            if (a == null)
                return;
            Style s = a.Style;

            int intervalCount;
            double incr;
            (incr, intervalCount) = await SetIncrementAndInterval(rpt, a, min, max);      // Calculate the interval count

            System.Drawing.Size tSize = await DrawTitleMeasure(rpt, g, a.Title);
            await DrawTitle(rpt, g, a.Title, new System.Drawing.Rectangle(rect.Left, rect.Top, tSize.Width, rect.Height));

            double v = min;
            for (int i = 0; i < intervalCount + 1; i++)
            {
                int h = (int)(((Math.Min(v, max) - min) / (max - min)) * rect.Height);
                if (h < 0)		// this is really some form of error
                {
                    v += incr;
                    continue;
                }

                if (!a.Visible)
                {
                    // nothing to do
                }
                else if (s != null)
                {
                    System.Drawing.Size size = await s.MeasureString(rpt, g, v, TypeCode.Double, null, int.MaxValue);
                    System.Drawing.Rectangle vRect =
                        new System.Drawing.Rectangle(rect.Left + tSize.Width, rect.Top + rect.Height - h - (size.Height / 2), rect.Width - tSize.Width, size.Height);
                    await s.DrawString(rpt, g, v, TypeCode.Double, null, vRect);
                }
                else
                {
                    System.Drawing.Size size = await Style.MeasureStringDefaults(rpt, g, v, TypeCode.Double, null, int.MaxValue);
                    System.Drawing.Rectangle vRect =
                        new System.Drawing.Rectangle(rect.Left + tSize.Width, rect.Top + rect.Height - h - (size.Height / 2), rect.Width - tSize.Width, size.Height);
                    Style.DrawStringDefaults(g, v, vRect);
                }

                await DrawValueAxisGrid(rpt, g, a.MajorGridLines, new System.Drawing.Point(plotLeft, rect.Top + rect.Height - h), new System.Drawing.Point(plotRight, rect.Top + rect.Height - h));
                await DrawValueAxisTick(rpt, g, true, a.MajorTickMarks, a.MajorGridLines, new System.Drawing.Point(plotLeft, rect.Top + rect.Height - h));

                v += incr;
            }

            // Draw the end points of the major grid lines
            await DrawValueAxisGrid(rpt, g, a.MajorGridLines, new System.Drawing.Point(plotLeft, rect.Top), new System.Drawing.Point(plotLeft, rect.Bottom));
            await DrawValueAxisTick(rpt, g, true, a.MajorTickMarks, a.MajorGridLines, new System.Drawing.Point(plotLeft, rect.Top));
            await DrawValueAxisGrid(rpt, g, a.MajorGridLines, new System.Drawing.Point(plotRight, rect.Top), new System.Drawing.Point(plotRight, rect.Bottom));
            await DrawValueAxisTick(rpt, g, true, a.MajorTickMarks, a.MajorGridLines, new System.Drawing.Point(plotRight, rect.Bottom));

            return;
        }

        protected async Task DrawValueAxisGrid(Report rpt, Draw2.Graphics g, ChartGridLines gl, System.Drawing.Point s, System.Drawing.Point e)
        {
            if (gl == null || !gl.ShowGridLines)
                return;

            if (gl.Style != null)
                await gl.Style.DrawStyleLine(rpt, g, null, s, e);
            else
                g.DrawLine(Draw2.Pens.Black, s, e);

            return;
        }

        protected async Task DrawValueAxisTick(Report rpt, Draw2.Graphics g, bool bMajor, AxisTickMarksEnum tickType, ChartGridLines gl, System.Drawing.Point p)
        {
            if (tickType == AxisTickMarksEnum.None)
                return;

            int len = bMajor ? AxisTickMarkMajorLen : AxisTickMarkMinorLen;
            System.Drawing.Point s, e;
            switch (tickType)
            {
                case AxisTickMarksEnum.Inside:
                    s = new System.Drawing.Point(p.X, p.Y);
                    e = new System.Drawing.Point(p.X + len, p.Y);
                    break;
                case AxisTickMarksEnum.Cross:
                    s = new System.Drawing.Point(p.X - len, p.Y);
                    e = new System.Drawing.Point(p.X + len, p.Y);
                    break;
                case AxisTickMarksEnum.Outside:
                default:
                    s = new System.Drawing.Point(p.X - len, p.Y);
                    e = new System.Drawing.Point(p.X, p.Y);
                    break;
            }
            Style style = gl.Style;

            if (style != null)
                await style.DrawStyleLine(rpt, g, null, s, e);
            else
                g.DrawLine(Draw2.Pens.Black, s, e);

            return;
        }
        /////////////////////////
        protected async Task DrawCategoryAxis(Report rpt, Draw2.Graphics g, double min, double max, System.Drawing.Rectangle rect, int plotTop, int plotBottom)
        {
            if (this.ChartDefn.CategoryAxis == null)
                return;
            Axis a = this.ChartDefn.CategoryAxis.Axis;
            if (a == null)
                return;
            Style s = a.Style;

            // Account for tick marks
            int tickSize = 0;
            if (a.MajorTickMarks == AxisTickMarksEnum.Cross ||
                a.MajorTickMarks == AxisTickMarksEnum.Outside)
                tickSize = this.AxisTickMarkMajorLen;
            else if (a.MinorTickMarks == AxisTickMarksEnum.Cross ||
                a.MinorTickMarks == AxisTickMarksEnum.Outside)
                tickSize += this.AxisTickMarkMinorLen;

            int intervalCount;
            double incr;
            (incr, intervalCount)= await SetIncrementAndInterval(rpt, a, min, max);      // Calculate the interval count

            int maxValueHeight = 0;
            double v = min;
            System.Drawing.Size size = System.Drawing.Size.Empty;

            for (int i = 0; i < intervalCount + 1; i++)
            {
                int x = (int)(((Math.Min(v, max) - min) / (max - min)) * rect.Width);

                if (!a.Visible)
                {
                    // nothing to do
                }
                else if (s != null)
                {
                    size = await s.MeasureString(rpt, g, v, TypeCode.Double, null, int.MaxValue);
                    System.Drawing.Rectangle vRect =
                        new System.Drawing.Rectangle(rect.Left + x - (size.Width / 2), rect.Top + tickSize, size.Width, size.Height);
                    await s.DrawString(rpt, g, v, TypeCode.Double, null, vRect);
                }
                else
                {
                    size = await Style.MeasureStringDefaults(rpt, g, v, TypeCode.Double, null, int.MaxValue);
                    System.Drawing.Rectangle vRect =
                        new System.Drawing.Rectangle(rect.Left + x - (size.Width / 2), rect.Top + tickSize, size.Width, size.Height);
                    Style.DrawStringDefaults(g, v, vRect);
                }
                if (size.Height > maxValueHeight)		// Need to keep track of the maximum height
                    maxValueHeight = size.Height;		//   this is probably overkill since it should always be the same??

                await DrawCategoryAxisGrid(rpt, g, a.MajorGridLines, new System.Drawing.Point(rect.Left + x, plotTop), new System.Drawing.Point(rect.Left + x, plotBottom));
                await DrawCategoryAxisTick(rpt, g, true, a.MajorTickMarks, a.MajorGridLines, new System.Drawing.Point(rect.Left + x, plotBottom));

                v += incr;
            }

            // Draw the end points of the major grid lines
            await DrawCategoryAxisGrid(rpt, g, a.MajorGridLines, new System.Drawing.Point(rect.Left, plotTop), new System.Drawing.Point(rect.Left, plotBottom));
            await DrawCategoryAxisTick(rpt, g, true, a.MajorTickMarks, a.MajorGridLines, new System.Drawing.Point(rect.Left, plotBottom));
            await DrawCategoryAxisGrid(rpt, g, a.MajorGridLines, new System.Drawing.Point(rect.Right, plotTop), new System.Drawing.Point(rect.Right, plotBottom));
            await DrawCategoryAxisTick(rpt, g, true, a.MajorTickMarks, a.MajorGridLines, new System.Drawing.Point(rect.Right, plotBottom));

            System.Drawing.Size tSize = await DrawTitleMeasure(rpt, g, a.Title);
            await DrawTitle(rpt, g, a.Title,
                new System.Drawing.Rectangle(rect.Left, rect.Top + maxValueHeight + tickSize, rect.Width, tSize.Height));

            return;
        }

        protected async Task DrawCategoryAxisGrid(Report rpt, Draw2.Graphics g, ChartGridLines gl, System.Drawing.Point s, System.Drawing.Point e)
        {
            if (gl == null || !gl.ShowGridLines)
                return;

            if (gl.Style != null)
                await gl.Style.DrawStyleLine(rpt, g, null, s, e);
            else
                g.DrawLine(Draw2.Pens.Black, s, e);

            return;
        }

        protected async Task DrawCategoryAxisTick(Report rpt, Draw2.Graphics g, bool bMajor, AxisTickMarksEnum tickType, ChartGridLines gl, System.Drawing.Point p)
        {
            if (tickType == AxisTickMarksEnum.None)
                return;

            int len = bMajor ? AxisTickMarkMajorLen : AxisTickMarkMinorLen;
            System.Drawing.Point s, e;
            switch (tickType)
            {
                case AxisTickMarksEnum.Inside:
                    s = new System.Drawing.Point(p.X, p.Y);
                    e = new System.Drawing.Point(p.X, p.Y - len);
                    break;
                case AxisTickMarksEnum.Cross:
                    s = new System.Drawing.Point(p.X, p.Y - len);
                    e = new System.Drawing.Point(p.X, p.Y + len);
                    break;
                case AxisTickMarksEnum.Outside:
                default:
                    s = new System.Drawing.Point(p.X, p.Y + len);
                    e = new System.Drawing.Point(p.X, p.Y);
                    break;
            }
            Style style = gl.Style;

            if (style != null)
                await style.DrawStyleLine(rpt, g, null, s, e);
            else
                g.DrawLine(Draw2.Pens.Black, s, e);

            return;
        }

        // Calculate the size of the value axis; width is max value width + title width
        //										 height is max value height
        protected async Task<System.Drawing.Size> CategoryAxisSize(Report rpt, Draw2.Graphics g, double min, double max)
        {
            System.Drawing.Size size = System.Drawing.Size.Empty;
            if (ChartDefn.CategoryAxis == null)
                return size;
            Axis a = ChartDefn.CategoryAxis.Axis;//Not ValueAxis...
            if (a == null)
                return size;

            System.Drawing.Size minSize;
            System.Drawing.Size maxSize;
            if (!a.Visible)
            {
                minSize = maxSize = System.Drawing.Size.Empty;
            }
            else if (a.Style != null)
            {
                minSize = await a.Style.MeasureString(rpt, g, min, TypeCode.Double, null, int.MaxValue);
                maxSize = await a.Style.MeasureString(rpt, g, max, TypeCode.Double, null, int.MaxValue);
            }
            else
            {
                minSize = await Style.MeasureStringDefaults(rpt, g, min, TypeCode.Double, null, int.MaxValue);
                maxSize = await Style.MeasureStringDefaults(rpt, g, max, TypeCode.Double, null, int.MaxValue);
            }
            // Choose the largest
            size.Width = Math.Max(minSize.Width, maxSize.Width);
            size.Height = Math.Max(minSize.Height, maxSize.Height);

            // Now we need to add in the height of the title (if any)
            System.Drawing.Size titleSize = await DrawTitleMeasure(rpt, g, a.Title);
            size.Height += titleSize.Height;

            if (a.MajorTickMarks == AxisTickMarksEnum.Cross ||
                a.MajorTickMarks == AxisTickMarksEnum.Outside)
                size.Height += this.AxisTickMarkMajorLen;
            else if (a.MinorTickMarks == AxisTickMarksEnum.Cross ||
                a.MinorTickMarks == AxisTickMarksEnum.Outside)
                size.Height += this.AxisTickMarkMinorLen;

            return size;
        }
    }
}
