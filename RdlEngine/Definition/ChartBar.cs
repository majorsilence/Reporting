
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
    /// Bar chart definition and processing.
    ///</summary>
    internal class ChartBar : ChartBase
    {
        int _GapSize = 6;	//  TODO: hard code for now

        internal ChartBar(Report r, Row row, Chart c, MatrixCellEntry[,] m, Expression showTooltips, Expression showTooltipsX, Expression _ToolTipYFormat, Expression _ToolTipXFormat)
            : base(r, row, c, m, showTooltips, showTooltipsX, _ToolTipYFormat, _ToolTipXFormat)
        {
        }

        override internal async Task Draw(Report rpt)
        {
            CreateSizedBitmap();

#if !DRAWINGCOMPAT
            //AJM GJL 14082008 Using Vector Graphics
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

                // Adjust the top margin to depend on the title height
                System.Drawing.Size titleSize = await DrawTitleMeasure(rpt, g, ChartDefn.Title);
                Layout.TopMargin = titleSize.Height;

                double max = 0, min = 0;    // Get the max and min values
                                            // 20022008 AJM GJL - Now requires Y axis identifier
                (max, min) = await GetValueMaxMin(rpt, max, min, 0, 1);

                await DrawChartStyle(rpt, g);

                // Draw title; routine determines if necessary
                await DrawTitle(rpt, g, ChartDefn.Title, new System.Drawing.Rectangle(0, 0, _bm.Width, Layout.TopMargin));

                // Adjust the left margin to depend on the Category Axis
                System.Drawing.Size caSize = await CategoryAxisSize(rpt, g);
                Layout.LeftMargin = caSize.Width;

                // Adjust the bottom margin to depend on the Value Axis
                System.Drawing.Size vaSize = await ValueAxisSize(rpt, g, min, max);
                Layout.BottomMargin = vaSize.Height;

                // Draw legend
                System.Drawing.Rectangle lRect = await DrawLegend(rpt, g, false, true);
                // 20022008 AJM GJL - Requires Rpt and Graphics
                AdjustMargins(lRect, rpt, g);       // Adjust margins based on legend.

                // Draw Plot area
                await DrawPlotAreaStyle(rpt, g, lRect);

                // Draw Value Axis
                if (vaSize.Width > 0)   // If we made room for the axis - we need to draw it
                    await DrawValueAxis(rpt, g, min, max,
                        new System.Drawing.Rectangle(Layout.LeftMargin, _bm.Height - Layout.BottomMargin, _bm.Width - Layout.LeftMargin - Layout.RightMargin, vaSize.Height), Layout.TopMargin, _bm.Height - Layout.BottomMargin);

                // Draw Category Axis
                if (caSize.Height > 0)
                    await DrawCategoryAxis(rpt, g,
                        new System.Drawing.Rectangle(Layout.LeftMargin - caSize.Width, Layout.TopMargin, caSize.Width, _bm.Height - Layout.TopMargin - Layout.BottomMargin));

                if ((ChartSubTypeEnum)Enum.Parse(typeof(ChartSubTypeEnum), await _ChartDefn.Subtype.EvaluateString(rpt, _row)) == ChartSubTypeEnum.Stacked)
                    await DrawPlotAreaStacked(rpt, g, min, max);
                else if ((ChartSubTypeEnum)Enum.Parse(typeof(ChartSubTypeEnum), await _ChartDefn.Subtype.EvaluateString(rpt, _row)) == ChartSubTypeEnum.PercentStacked)
                    await DrawPlotAreaPercentStacked(rpt, g);
                else
                    await DrawPlotAreaPlain(rpt, g, min, max);

                await DrawLegend(rpt, g, false, false);     // after the plot is drawn
            }
        }

        async Task DrawPlotAreaPercentStacked(Report rpt, Draw2.Graphics g)
        {
            int barsNeeded = CategoryCount;
            int gapsNeeded = CategoryCount * 2;

            // Draw Plot area data
            double max = 1;

            int heightBar = (int)((Layout.PlotArea.Height - (gapsNeeded * _GapSize)) / barsNeeded);
            int maxBarWidth = (int)(Layout.PlotArea.Width);

            // Loop thru calculating all the data points
            for (int iRow = 1; iRow <= CategoryCount; iRow++)
            {
                int barLoc = (int)(Layout.PlotArea.Top + ((iRow - 1) * ((double)(Layout.PlotArea.Height) / CategoryCount)));
                barLoc += _GapSize; // space before series

                double sum = 0;
                for (int iCol = 1; iCol <= SeriesCount; iCol++)
                {
                    sum += await GetDataValue(rpt, iRow, iCol);
                }
                double v = 0;
                int saveX = 0;
                double t = 0;
                for (int iCol = 1; iCol <= SeriesCount; iCol++)
                {
                    t = await GetDataValue(rpt, iRow, iCol);
                    v += t;

                    int x = (int)((Math.Min(v / sum, max) / max) * maxBarWidth);

                    System.Drawing.Rectangle rect;
                    rect = new System.Drawing.Rectangle(Layout.PlotArea.Left + saveX, barLoc, x - saveX, heightBar);

                    await DrawColumnBar(rpt, g,
                        await GetSeriesBrush(rpt, iRow, iCol),
                         rect, iRow, iCol);

                    //Add a metafilecomment to use as a tooltip GJL 26092008
                    if (_showToolTips)
                    {
                        String val = "ToolTip:" + t.ToString(_tooltipYFormat) + "|X:" + (int)rect.X + "|Y:" + (int)rect.Y + "|W:" + rect.Width + "|H:" + rect.Height;
                        g.AddMetafileComment(new System.Text.ASCIIEncoding().GetBytes(val));
                    }

                    saveX = x;
                }
            }

            return;
        }

        async Task DrawPlotAreaPlain(Report rpt, Draw2.Graphics g, double min, double max)
        {
            int barsNeeded = SeriesCount * CategoryCount;
            int gapsNeeded = CategoryCount * 2;

            // Draw Plot area data
            int heightBar = (int)((Layout.PlotArea.Height - (gapsNeeded * _GapSize)) / barsNeeded);
            int maxBarWidth = (int)(Layout.PlotArea.Width);

            //int barLoc=Layout.LeftMargin;
            for (int iRow = 1; iRow <= CategoryCount; iRow++)
            {
                int barLoc = (int)(Layout.PlotArea.Top + ((iRow - 1) * ((double)(Layout.PlotArea.Height) / CategoryCount)));
                barLoc += _GapSize; // space before series
                for (int iCol = 1; iCol <= SeriesCount; iCol++)
                {
                    double v = await this.GetDataValue(rpt, iRow, iCol);
                    int x = (int)(((Math.Min(v, max) - min) / (max - min)) * maxBarWidth);

                    await DrawColumnBar(rpt, g, await GetSeriesBrush(rpt, iRow, iCol),
                        new System.Drawing.Rectangle(Layout.PlotArea.Left, barLoc, x, heightBar), iRow, iCol);
                    //Add a metafilecomment to use as a tooltip GJL 26092008
                    if (_showToolTips)
                    {
                        String val = "ToolTip:" + v.ToString(_tooltipYFormat) + "|X:" + (int)Layout.PlotArea.Left + "|Y:" + (int)(barLoc) + "|W:" + x + "|H:" + heightBar;
                        g.AddMetafileComment(new System.Text.ASCIIEncoding().GetBytes(val));
                    }
                    barLoc += heightBar;
                }
            }

            return;
        }

        async Task DrawPlotAreaStacked(Report rpt, Draw2.Graphics g, double min, double max)
        {
            int barsNeeded = CategoryCount;
            int gapsNeeded = CategoryCount * 2;

            int heightBar = (int)((Layout.PlotArea.Height - (gapsNeeded * _GapSize)) / barsNeeded);
            int maxBarWidth = (int)(Layout.PlotArea.Width);

            // Loop thru calculating all the data points
            for (int iRow = 1; iRow <= CategoryCount; iRow++)
            {
                int barLoc = (int)(Layout.PlotArea.Top + ((iRow - 1) * ((double)(Layout.PlotArea.Height) / CategoryCount)));
                barLoc += _GapSize; // space before series

                double v = 0;
                double t = 0;
                int saveX = 0;
                for (int iCol = 1; iCol <= SeriesCount; iCol++)
                {
                    t = await GetDataValue(rpt, iRow, iCol);
                    v += t;

                    int x = (int)(((Math.Min(v, max) - min) / (max - min)) * maxBarWidth);

                    System.Drawing.Rectangle rect;
                    rect = new System.Drawing.Rectangle(Layout.PlotArea.Left + saveX, barLoc, x - saveX, heightBar);

                    await DrawColumnBar(rpt, g, await GetSeriesBrush(rpt, iRow, iCol), rect, iRow, iCol);

                    if (_showToolTips)
                    {
                        String val = "ToolTip:" + v.ToString(_tooltipYFormat) + "|X:" + (int)rect.X + "|Y:" + (int)rect.Y + "|W:" + rect.Width + "|H:" + rect.Height;
                        g.AddMetafileComment(new System.Text.ASCIIEncoding().GetBytes(val));
                    }
                    saveX = x;
                }
            }

            return;
        }

        // Calculate the size of the category axis
        async Task<System.Drawing.Size> CategoryAxisSize(Report rpt, Draw2.Graphics g)
        {
            _LastCategoryWidth = 0;

            System.Drawing.Size size = System.Drawing.Size.Empty;
            if (this.ChartDefn.CategoryAxis == null)
                return size;
            Axis a = this.ChartDefn.CategoryAxis.Axis;
            if (a == null)
                return size;
            Style s = a.Style;

            // Measure the title
            size = await DrawTitleMeasure(rpt, g, a.Title);

            if (!a.Visible)     // don't need to calculate the height
                return size;

            // Calculate the tallest category name
            TypeCode tc;
            int maxWidth = 0;
            for (int iRow = 1; iRow <= CategoryCount; iRow++)
            {
                (object v, tc) = await this.GetCategoryValue(rpt, iRow);
                System.Drawing.Size tSize;
                if (s == null)
                    tSize = await Style.MeasureStringDefaults(rpt, g, v, tc, null, int.MaxValue);

                else
                    tSize = await s.MeasureString(rpt, g, v, tc, null, int.MaxValue);

                if (tSize.Width > maxWidth)
                    maxWidth = tSize.Width;

                if (iRow == CategoryCount)
                    _LastCategoryWidth = tSize.Width;
            }

            // Add on the widest category name
            size.Width += maxWidth;
            return size;
        }

        // DrawCategoryAxis 
        async Task DrawCategoryAxis(Report rpt, Draw2.Graphics g, System.Drawing.Rectangle rect)
        {
            if (this.ChartDefn.CategoryAxis == null)
                return;
            Axis a = this.ChartDefn.CategoryAxis.Axis;
            if (a == null)
                return;
            Style s = a.Style;

            System.Drawing.Size tSize = await DrawTitleMeasure(rpt, g, a.Title);
            await DrawTitle(rpt, g, a.Title,
                new System.Drawing.Rectangle(rect.Left, rect.Top, tSize.Width, rect.Height));

            int drawHeight = rect.Height / CategoryCount;
            TypeCode tc;
            for (int iRow = 1; iRow <= CategoryCount; iRow++)
            {
                (object v, tc) = await this.GetCategoryValue(rpt, iRow);

                int drawLoc = (int)(rect.Top + ((iRow - 1) * ((double)rect.Height / CategoryCount)));

                // Draw the category text
                if (a.Visible)
                {
                    System.Drawing.Rectangle drawRect = new System.Drawing.Rectangle(rect.Left + tSize.Width, drawLoc, rect.Width - tSize.Width, drawHeight);
                    if (s == null)
                        Style.DrawStringDefaults(g, v, drawRect);
                    else
                        await s.DrawString(rpt, g, v, tc, null, drawRect);
                }
                // Draw the Major Tick Marks (if necessary)
                DrawCategoryAxisTick(g, true, a.MajorTickMarks, new System.Drawing.Point(rect.Right, drawLoc));
            }

            // Draw the end on (if necessary)
            DrawCategoryAxisTick(g, true, a.MajorTickMarks, new System.Drawing.Point(rect.Right, rect.Bottom));

            return;
        }

        protected void DrawCategoryAxisTick(Draw2.Graphics g, bool bMajor, AxisTickMarksEnum tickType, System.Drawing.Point p)
        {
            int len = bMajor ? AxisTickMarkMajorLen : AxisTickMarkMinorLen;
            switch (tickType)
            {
                case AxisTickMarksEnum.Outside:
                    g.DrawLine(Draw2.Pens.Black, new System.Drawing.Point(p.X, p.Y), new System.Drawing.Point(p.X - len, p.Y));
                    break;
                case AxisTickMarksEnum.Inside:
                    g.DrawLine(Draw2.Pens.Black, new System.Drawing.Point(p.X, p.Y), new System.Drawing.Point(p.X + len, p.Y));
                    break;
                case AxisTickMarksEnum.Cross:
                    g.DrawLine(Draw2.Pens.Black, new System.Drawing.Point(p.X - len, p.Y), new System.Drawing.Point(p.X + len, p.Y));
                    break;
                case AxisTickMarksEnum.None:
                default:
                    break;
            }
            return;
        }

        async Task DrawColumnBar(Report rpt, Draw2.Graphics g, Draw2.Brush brush, System.Drawing.Rectangle rect, int iRow, int iCol)
        {
            g.FillRectangle(brush, rect);
            g.DrawRectangle(Draw2.Pens.Black, rect);

            if ((ChartSubTypeEnum)Enum.Parse(typeof(ChartSubTypeEnum), await _ChartDefn.Subtype.EvaluateString(rpt, _row)) == ChartSubTypeEnum.Stacked ||
                (ChartSubTypeEnum)Enum.Parse(typeof(ChartSubTypeEnum), await _ChartDefn.Subtype.EvaluateString(rpt, _row)) == ChartSubTypeEnum.PercentStacked)
            {
                await DrawDataPoint(rpt, g, rect, iRow, iCol);
            }
            else
            {
                System.Drawing.Point p;
                p = new System.Drawing.Point(rect.Right, rect.Top);
                await DrawDataPoint(rpt, g, p, iRow, iCol);
            }


            return;
        }

        protected async Task DrawValueAxis(Report rpt, Draw2.Graphics g, double min, double max, System.Drawing.Rectangle rect, int plotTop, int plotBottom)
        {
            if (this.ChartDefn.ValueAxis == null)
                return;
            Axis a = this.ChartDefn.ValueAxis.Axis;
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
            (incr, intervalCount) = await SetIncrementAndInterval(rpt, a, min, max);      // Calculate the interval count

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
                if (size.Height > maxValueHeight)       // Need to keep track of the maximum height
                    maxValueHeight = size.Height;       //   this is probably overkill since it should always be the same??

                await DrawValueAxisGrid(rpt, g, a.MajorGridLines, new System.Drawing.Point(rect.Left + x, plotTop), new System.Drawing.Point(rect.Left + x, plotBottom));
                await DrawValueAxisTick(rpt, g, true, a.MajorTickMarks, a.MajorGridLines, new System.Drawing.Point(rect.Left + x, plotBottom));

                v += incr;
            }

            // Draw the end points of the major grid lines
            await DrawValueAxisGrid(rpt, g, a.MajorGridLines, new System.Drawing.Point(rect.Left, plotTop), new System.Drawing.Point(rect.Left, plotBottom));
            await DrawValueAxisTick(rpt, g, true, a.MajorTickMarks, a.MajorGridLines, new System.Drawing.Point(rect.Left, plotBottom));
            await DrawValueAxisGrid(rpt, g, a.MajorGridLines, new System.Drawing.Point(rect.Right, plotTop), new System.Drawing.Point(rect.Right, plotBottom));
            await DrawValueAxisTick(rpt, g, true, a.MajorTickMarks, a.MajorGridLines, new System.Drawing.Point(rect.Right, plotBottom));

            System.Drawing.Size tSize = await DrawTitleMeasure(rpt, g, a.Title);
            await DrawTitle(rpt, g, a.Title,
                new System.Drawing.Rectangle(rect.Left, rect.Top + maxValueHeight + tickSize, rect.Width, tSize.Height));

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
