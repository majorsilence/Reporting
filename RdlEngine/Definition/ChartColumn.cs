


//GJL 110208 - Made some changes to allow second scale...


using System;
using System.Collections;
#if DRAWINGCOMPAT
using Draw2 = Majorsilence.Drawing;
#else
using Draw2 = System.Drawing;
#endif
//using System.Windows.Forms;JD 20080514 - Why? 
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading.Tasks;


namespace Majorsilence.Reporting.Rdl
{
    ///<summary>
    /// Column chart definition and processing
    ///</summary>
    internal class ChartColumn : ChartBase
    {
        int _GapSize = 0;		// TODO: hard code for now - 06122007AJM Removed gap so that large category ranges display better

        internal ChartColumn(Report r, Row row, Chart c, MatrixCellEntry[,] m, Expression showTooltips, Expression showTooltipsX, Expression _ToolTipYFormat, Expression _ToolTipXFormat)
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
                    //_mf = new System.Draw2.Imaging.Metafile(_aStream, HDC);
                    _mf = new Draw2.Imaging.Metafile(_aStream, HDC,
                        new Draw2.RectangleF(0, 0, _bm.Width, _bm.Height), Draw2.Imaging.MetafileFrameUnit.Pixel);
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
                Draw2.Size titleSize = await DrawTitleMeasure(rpt, g, ChartDefn.Title);
                Layout.TopMargin = titleSize.Height;

                double max = 0, min = 0; // Get the max and min values
                (max, min) = await GetValueMaxMin(rpt, max, min, 0, 1);

                await DrawChartStyle(rpt, g);

                // Draw title; routine determines if necessary
                await DrawTitle(rpt, g, ChartDefn.Title, new Draw2.Rectangle(0, 0, _bm.Width, Layout.TopMargin));

                // Adjust the left margin to depend on the Value Axis
                Draw2.Size vaSize = await ValueAxisSize(rpt, g, min, max);
                Layout.LeftMargin = vaSize.Width;

                // Adjust the right margin to depend on the Value Axis
                bool Show2ndY = ShowRightYAxis(rpt);
                Draw2.Size vaSize2 = vaSize;

                if (Show2ndY)
                {
                    double rmax = 0, rmin = 0;
                    (rmax, rmin) = await GetMaxMinDataValue(rpt, 0, 2);
                    vaSize2 = await ValueAxisSize(rpt, g, rmin, rmax);
                    Layout.RightMargin = vaSize2.Width;
                }

                // Draw legend
                Draw2.Rectangle lRect = await DrawLegend(rpt, g, false, true);

                // Adjust the bottom margin to depend on the Category Axis
                Draw2.Size caSize = await CategoryAxisSize(rpt, g);
                Layout.BottomMargin = caSize.Height;

                AdjustMargins(lRect, rpt, g);       // Adjust margins based on legend.

                // Draw Plot area
                await DrawPlotAreaStyle(rpt, g, lRect);

                int intervalCount = 0; //GJL - Used to get the interval count out of DrawValueAxis so that we don't recalculate it again.
                double incr = 0.0;  //GJL - As above
                                    // Draw Value Axis //GJL now as by ref params to return the values to the above variables
                if (vaSize.Width > 0)   // If we made room for the axis - we need to draw it
                    (incr, intervalCount) = await DrawValueAxis(rpt, g, min, max,
                        new Draw2.Rectangle(Layout.LeftMargin - vaSize.Width, Layout.TopMargin, vaSize.Width, _bm.Height - Layout.TopMargin - Layout.BottomMargin), Layout.LeftMargin, Layout.Width - Layout.RightMargin);


                //********************************************************************************************************************************************
                //Draw the 2nd value axis - obviously we will only want to do this if we choose a second axis        

                double ScaleFactor = 1.0;
                //Secong value axis            
                if (Show2ndY)
                    ScaleFactor = await Draw2ndValueAxis(rpt, g, min, max, new Draw2.Rectangle(Layout.LeftMargin + Layout.PlotArea.Width, Layout.TopMargin, vaSize2.Width, _bm.Height - Layout.TopMargin - Layout.BottomMargin), Layout.LeftMargin, Layout.Width - Layout.RightMargin, incr, intervalCount, ScaleFactor);

                // Draw Category Axis
                if (caSize.Height > 0)
                    // 090508ajm passing chart bounds in
                    await DrawCategoryAxis(rpt, g,
                        new Draw2.Rectangle(Layout.LeftMargin, _bm.Height - Layout.BottomMargin, Layout.PlotArea.Width, caSize.Height), Layout.TopMargin, caSize.Width);
                if ((ChartSubTypeEnum)Enum.Parse(typeof(ChartSubTypeEnum), await _ChartDefn.Subtype.EvaluateString(rpt, _row)) == ChartSubTypeEnum.Stacked)
                    await DrawPlotAreaStacked(rpt, g, max, min);
                else if ((ChartSubTypeEnum)Enum.Parse(typeof(ChartSubTypeEnum), await _ChartDefn.Subtype.EvaluateString(rpt, _row)) == ChartSubTypeEnum.PercentStacked)
                    await DrawPlotAreaPercentStacked(rpt, g);
                else
                    await DrawPlotAreaPlain(rpt, g, max, min, ScaleFactor);

                await DrawLegend(rpt, g, false, false);

            }
        }

        async Task DrawPlotAreaPercentStacked(Report rpt, Draw2.Graphics g)
        {
            int barsNeeded = CategoryCount;
            int gapsNeeded = CategoryCount * 2;

            // Draw Plot area data
            double max = 1;

            int widthBar = (int)((Layout.PlotArea.Width - (gapsNeeded * _GapSize)) / barsNeeded);
            int maxBarHeight = (int)(Layout.PlotArea.Height);

            // Loop thru calculating all the data points
            for (int iRow = 1; iRow <= CategoryCount; iRow++)
            {
                int barLoc = (int)(Layout.PlotArea.Left + ((iRow - 1) * ((double)(Layout.PlotArea.Width) / CategoryCount)));
                barLoc += _GapSize; // space before series

                double sum = 0;
                for (int iCol = 1; iCol <= SeriesCount; iCol++)
                {
                    double t = await GetDataValue(rpt, iRow, iCol);
                    if (t.CompareTo(double.NaN) == 0)
                        t = 0;
                    sum += t;
                }
                double v = 0;
                Draw2.Point saveP = Draw2.Point.Empty;
                for (int iCol = 1; iCol <= SeriesCount; iCol++)
                {
                    double t = await GetDataValue(rpt, iRow, iCol);
                    if (t.CompareTo(double.NaN) == 0)
                        t = 0;
                    v += t;

                    int h = (int)((Math.Min(v / sum, max) / max) * maxBarHeight);
                    Draw2.Point p = new Draw2.Point(barLoc, Layout.PlotArea.Top + (maxBarHeight - h));

                    Draw2.Rectangle rect;
                    if (saveP == Draw2.Point.Empty)
                        rect = new Draw2.Rectangle(p, new Draw2.Size(widthBar, h));
                    else
                        rect = new Draw2.Rectangle(p, new Draw2.Size(widthBar, saveP.Y - p.Y));
                    await DrawColumnBar(rpt, g, await GetSeriesBrush(rpt, iRow, iCol), rect, iRow, iCol);

                    //Add a metafilecomment to use as a tooltip GJL 26092008
                    //if (_showToolTips)
                    //{
                    //    String val = "ToolTip:" + t + "|X:" + (int)rect.X + "|Y:" + (int)(rect.Y) + "|W:" + rect.Width + "|H:" + rect.Height;
                    //    g.AddMetafileComment(new System.Text.ASCIIEncoding().GetBytes(val));
                    //}

                    if (_showToolTips)
                    {
                        string display = "";
                        if (display.Length > 0) display += " , ";
                        display += t.ToString(_tooltipYFormat);

                        String val = "ToolTip:" + display + "|X:" + (int)rect.X + "|Y:" + (int)rect.Y + "|W:" + rect.Width + "|H:" + rect.Height;
                        g.AddMetafileComment(new System.Text.ASCIIEncoding().GetBytes(val));
                    }
                    saveP = p;
                }
            }

            return;
        }

        /* This method has been modified to allow for a Line plot type and to draw the chart correctly 
         * with selected plot type. 
         * 06122007AJM
         */
        async Task DrawPlotAreaPlain(Report rpt, Draw2.Graphics g, double max, double min, double ScaleFactor)
        {
            /* Need to adjust bar count to allow for Line plot types
             * 06122007AJM */
            int ColumnCount = 0;
            for (int iCol = 1; iCol <= SeriesCount; iCol++)
            {
                if (GetPlotType(rpt, iCol, 1).ToUpper() != "LINE")
                {
                    ColumnCount++;
                }
            }
            if (ColumnCount == 0) { ColumnCount = 1; } //Handle no bars (All lines)
            int barsNeeded = ColumnCount * CategoryCount;

            int gapsNeeded = CategoryCount * 2;

            // Draw Plot area data
            int widthBar = (int)((Layout.PlotArea.Width - (gapsNeeded * _GapSize)) / barsNeeded);
            int maxBarHeight = (int)(Layout.PlotArea.Height);
            /* The following list has been added to keep track of the
             * previous point for drawing a line intead of a column
             * when the plottype is Line
             * 05122007AJM */
            bool DrawPoint;
            SortedList LastPoints = new SortedList();
            int lineLoc;
            for (int iRow = 1; iRow <= CategoryCount; iRow++)
            {
                int barLoc = (int)(Layout.PlotArea.Left + ((iRow - 1) * ((double)(Layout.PlotArea.Width) / CategoryCount)));

                barLoc += _GapSize;	// space before series
                lineLoc = barLoc + (widthBar * ColumnCount / 2);
                for (int z = 0; z < 2; z++)
                {
                    for (int iCol = 1; iCol <= SeriesCount; iCol++)
                    {
                        /* This for loop has been modified to select if the column should
                         * be drawn based on the Plot type of the column
                         * 05122007AJM */
                        if (GetPlotType(rpt, iCol, iRow).ToUpper() != "LINE")
                        {
                            if (z == 0)
                            {
                                double v = await this.GetDataValue(rpt, iRow, iCol);
                                double tooltipVal = v;
                                if (GetYAxis(rpt, iCol, iRow).ToUpper() != "LEFT")
                                {
                                    //Scale the Y data...
                                    v /= ScaleFactor;
                                }

                                if (v.CompareTo(double.NaN) == 0)
                                    v = min;
                                int h = (int)(((Math.Min(v, max) - min) / (max - min)) * maxBarHeight);

                                await DrawColumnBar(rpt, g, await GetSeriesBrush(rpt, iRow, iCol),
                                    new Draw2.Rectangle(barLoc, Layout.PlotArea.Top + (maxBarHeight - h), widthBar, h), iRow, iCol);

                                //Add a metafilecomment to use as a tooltip GJL 26092008
                                if (_showToolTips)
                                {
                                    String val = "ToolTip:" + tooltipVal.ToString(_tooltipYFormat) + "|X:" + barLoc + "|Y:" + (int)(Layout.PlotArea.Top + (maxBarHeight - h)) + "|W:" + widthBar + "|H:" + h;
                                    g.AddMetafileComment(new System.Text.ASCIIEncoding().GetBytes(val));
                                }



                                barLoc += widthBar;
                            }
                        }
                        else //This is a line type plot
                        {
                            if (z == 1)
                            {
                                double v = await this.GetDataValue(rpt, iRow, iCol);
                                double tooltipVal = v;
                                if (GetYAxis(rpt, iCol, iRow).ToUpper() != "LEFT")
                                {
                                    //Scale the Y data...
                                    v /= ScaleFactor;
                                }

                                DrawPoint = true;

                                if (v.CompareTo(double.NaN) == 0)
                                {
                                    //don't draw null
                                    DrawPoint = false;
                                    //insert empty point
                                    LastPoints[iCol] = null;
                                }
                                if (DrawPoint)
                                {
                                    int h = (int)(((Math.Min(v, max) - min) / (max - min)) * maxBarHeight);

                                    Draw2.Rectangle r = new Draw2.Rectangle(lineLoc, Layout.PlotArea.Top + (maxBarHeight - h), widthBar, h);

                                    Draw2.Point p = new Draw2.Point(r.Left, r.Top);
                                    if (LastPoints[iCol - 1] == null)
                                        LastPoints[iCol - 1] = p;
                                    bool DrawMarker = getNoMarkerVal(rpt, iCol, 1) == false;
                                    await DrawDataPoint(rpt, g, new Draw2.Point(p.X, p.Y - 14), iRow, iCol);  // todo: 14 is arbitrary
                                    if (DrawMarker) { DrawLegendLineMarker(g, await GetSeriesBrush(rpt, iRow, iCol), new Draw2.Pen(await GetSeriesBrush(rpt, iRow, iCol)), SeriesMarker[iCol - 1], p.X - 5, p.Y - 5, 10); }


                                    if (LastPoints.ContainsKey(iCol))
                                    {
                                        Draw2.Point[] Points = new Draw2.Point[2];
                                        Points[0] = p;
                                        Draw2.Point pt = (Draw2.Point)LastPoints[iCol];
                                        Points[1] = new Draw2.Point(pt.X - 1, pt.Y);
                                        // 05052008AJM - Allowing for breaking lines in chart
                                        {
                                            String LineSize = getLineSize(rpt, iCol, 1);
                                            int intLineSize = 1;
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
                                        }
                                    }

                                    //Add a metafilecomment to use as a tooltip GJL 26092008
                                    if (_showToolTips)
                                    {
                                        String val = "ToolTip:" + tooltipVal.ToString(_tooltipYFormat) + "|X:" + (int)(p.X - 5) + "|Y:" + (int)(p.Y - 5) + "|W:10|H:10";
                                        g.AddMetafileComment(new System.Text.ASCIIEncoding().GetBytes(val));
                                    }



                                    LastPoints[iCol] = p;
                                }
                            }
                        }
                    }
                }
            }
        }

        internal void DrawLegendLineMarker(Draw2.Graphics g, Draw2.Brush b, Draw2.Pen p, ChartMarkerEnum marker, int x, int y, int mSize)
        {
            if (b.GetType() == typeof(Draw2.Drawing2D.HatchBrush))
            {
                Draw2.Drawing2D.HatchBrush hb = (Draw2.Drawing2D.HatchBrush)b;
                b = new Draw2.SolidBrush(hb.ForegroundColor);
            }

            Draw2.Pen p2;
            Draw2.PointF[] points;
            switch (marker)
            {
                case ChartMarkerEnum.Bubble:
                case ChartMarkerEnum.Circle:
                    g.FillEllipse(b, x, y, mSize, mSize);
                    break;
                case ChartMarkerEnum.Square:
                    g.FillRectangle(b, x, y, mSize, mSize);
                    break;
                case ChartMarkerEnum.Plus:

                    p2 = new Draw2.Pen(b, 2.0f);
                    g.DrawLine(p2, new Draw2.Point(x + ((mSize + 1) / 2), y), new Draw2.Point(x + ((mSize + 1) / 2), y + mSize));
                    //g.DrawLine(p2, new Point(x + (mSize + 1) / 2, y + (mSize + 1) / 2), new Point(x + mSize, y + (mSize + 1) / 2));
                    break;
                case ChartMarkerEnum.Diamond:
                    points = new Draw2.PointF[5];
                    points[0] = points[4] = new Draw2.PointF(x + ((mSize + 1) / 2), y);	// starting and ending point
                    points[1] = new Draw2.PointF(x, y + ((mSize + 1) / 2));
                    points[2] = new Draw2.PointF(x + ((mSize + 1) / 2), y + mSize);
                    points[3] = new Draw2.PointF(x + mSize, y + ((mSize + 1) / 2));
                    g.FillPolygon(b, points);
                    break;
                case ChartMarkerEnum.Triangle:
                    points = new Draw2.PointF[4];
                    points[0] = points[3] = new Draw2.PointF(x + ((mSize + 1) / 2), y);	// starting and ending point
                    points[1] = new Draw2.PointF(x, y + mSize);
                    points[2] = new Draw2.PointF(x + mSize, y + mSize);
                    g.FillPolygon(b, points);
                    break;
                case ChartMarkerEnum.X:
                    p2 = new Draw2.Pen(b, 2.0f);
                    g.DrawLine(p2, new Draw2.Point(x, y), new Draw2.Point(x + mSize, y + mSize));
                    g.DrawLine(p2, new Draw2.Point(x, y + mSize), new Draw2.Point(x + mSize, y));
                    break;
            }
            return;
        }

        /* This code was copied from the Line drawing class. There may be a better
         * way to do this by reusing the code from the original class but this 
         * will work, the only issue being that if the line drawing is changed then 
         * this function will need to be updated as well
         * 05122007 AJM */
        async Task DrawLineBetweenPoints(Draw2.Graphics g, Report rpt, Draw2.Brush brush, Draw2.Point[] points)
        {
            await DrawLineBetweenPoints(g, rpt, brush, points, 2);
        }


        async Task DrawLineBetweenPoints(Draw2.Graphics g, Report rpt, Draw2.Brush brush, Draw2.Point[] points, int intLineSize)
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

        async Task DrawPlotAreaStacked(Report rpt, Draw2.Graphics g, double max, double min)
        {
            int barsNeeded = CategoryCount;
            int gapsNeeded = CategoryCount * 2;

            int widthBar = (int)((Layout.PlotArea.Width - (gapsNeeded * _GapSize)) / barsNeeded);
            int maxBarHeight = (int)(Layout.PlotArea.Height);

            // Loop thru calculating all the data points
            for (int iRow = 1; iRow <= CategoryCount; iRow++)
            {
                int barLoc = (int)(Layout.PlotArea.Left + ((iRow - 1) * ((double)(Layout.PlotArea.Width) / CategoryCount)));
                barLoc += _GapSize; // space before series

                double v = 0;
                Draw2.Point saveP = Draw2.Point.Empty;
                for (int iCol = 1; iCol <= SeriesCount; iCol++)
                {
                    double t = await GetDataValue(rpt, iRow, iCol);
                    if (t.CompareTo(double.NaN) == 0)
                        t = 0;
                    v += t;

                    int h = (int)(((Math.Min(v, max) - min) / (max - min)) * maxBarHeight);
                    Draw2.Point p = new Draw2.Point(barLoc, Layout.PlotArea.Top + (maxBarHeight - h));

                    Draw2.Rectangle rect;
                    if (saveP == Draw2.Point.Empty)
                        rect = new Draw2.Rectangle(p, new Draw2.Size(widthBar, h));
                    else
                        rect = new Draw2.Rectangle(p, new Draw2.Size(widthBar, saveP.Y - p.Y));
                    await DrawColumnBar(rpt, g, await GetSeriesBrush(rpt, iRow, iCol), rect, iRow, iCol);

                    //Add a metafilecomment to use as a tooltip GJL 26092008
                    if (_showToolTips)
                    {
                        String val = "ToolTip:" + t.ToString(_tooltipYFormat) + "|X:" + (int)rect.X + "|Y:" + (int)(rect.Y) + "|W:" + rect.Width + "|H:" + rect.Height;
                        g.AddMetafileComment(new System.Text.ASCIIEncoding().GetBytes(val));
                    }

                    saveP = p;
                }
            }

            return;
        }

        // Calculate the size of the category axis
        protected async Task<Draw2.Size> CategoryAxisSize(Report rpt, Draw2.Graphics g)
        {
            _LastCategoryWidth = 0;

            Draw2.Size size = Draw2.Size.Empty;
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
            int maxHeight = 0;
            int maxWidth = 0;
            for (int iRow = 1; iRow <= CategoryCount; iRow++)
            {
                (object v, tc) = await this.GetCategoryValue(rpt, iRow);
                Draw2.Size tSize;
                if (s == null)
                    tSize = await Style.MeasureStringDefaults(rpt, g, v, tc, null, int.MaxValue);
                else
                    tSize = await s.MeasureString(rpt, g, v, tc, null, int.MaxValue);

                if (tSize.Height > maxHeight)
                    maxHeight = tSize.Height;

                if (tSize.Width > maxWidth)
                    maxWidth = tSize.Width;

                if (iRow == CategoryCount)
                    _LastCategoryWidth = tSize.Width;
            }
            size.Width = maxWidth;          // set the widest entry

            // Add on the tallest category name
            size.Height += maxHeight;

            // Account for tick marks
            int tickSize = 0;
            if (a.MajorTickMarks == AxisTickMarksEnum.Cross ||
                a.MajorTickMarks == AxisTickMarksEnum.Outside)
                tickSize = this.AxisTickMarkMajorLen;
            else if (a.MinorTickMarks == AxisTickMarksEnum.Cross ||
                a.MinorTickMarks == AxisTickMarksEnum.Outside)
                tickSize = this.AxisTickMarkMinorLen;

            size.Height += tickSize;
            /* This swap is done so that the size will work correctly with rotated
             * (vertical) category names
             * 06122007AJM */
            if (maxWidth > (CategoryCount * SeriesCount) && s == null)  // kas: swap only when no axis style info provided
            {
                int tmp = size.Height;
                size.Height = size.Width;
                size.Width = tmp;
            }
            return size;
        }

        // DrawCategoryAxis 
        //09052008ajm added plottop variable to collect the top of the chart
        protected async Task DrawCategoryAxis(Report rpt, Draw2.Graphics g, Draw2.Rectangle rect, int plotTop, int maxWidth)
        {
            if (this.ChartDefn.CategoryAxis == null)
                return;
            Axis a = this.ChartDefn.CategoryAxis.Axis;
            if (a == null)
                return;
            Style s = a.Style;
            Draw2.Size tSize = await DrawTitleMeasure(rpt, g, a.Title);
            await DrawTitle(rpt, g, a.Title, new Draw2.Rectangle(rect.Left, rect.Bottom - tSize.Height, rect.Width, tSize.Height));

            // Account for tick marks
            int tickSize = 0;
            if (a.MajorTickMarks == AxisTickMarksEnum.Cross ||
                a.MajorTickMarks == AxisTickMarksEnum.Outside)
                tickSize = this.AxisTickMarkMajorLen;
            else if (a.MinorTickMarks == AxisTickMarksEnum.Cross ||
                a.MinorTickMarks == AxisTickMarksEnum.Outside)
                tickSize = this.AxisTickMarkMinorLen;

            int drawWidth;
            int catCount = ChartDefn.Type == ChartTypeEnum.Area ? CategoryCount - 1 : CategoryCount;
            drawWidth = rect.Width / catCount;
            bool mustSize = a.CanOmit && (drawWidth < maxWidth || ChartDefn.Type == ChartTypeEnum.Area);
            int MajorGrid = 0;
            // 15052008AJM Fixed for charts without a set major interval
            if (a.MajorInterval != null)
            {
                // 09052008WRP  get major interval value  
                MajorGrid = (int)await a.MajorInterval.EvaluateDouble(rpt, this._row);
            }
            // 12052008WRP
            // setup month scale gridline plot - must check dealing with date data type
            DateTime CurrentDate = DateTime.Now; //used for checking change in month 
            DateTime OldDate = DateTime.Now; //used for checking change in month 
            DateTime TempDate = DateTime.Now;//working variable
            bool date = false; //used for confirming dealing with date data
            int PreviousLocation = rect.Left; //used to keep track of previous gridline location on x axis - set to x position of category axis at start
            TypeCode tc;
            (object first, tc) = await this.GetCategoryValue(rpt, 1);
            if (first != null)
            {
                switch (tc)
                {
                    case TypeCode.DateTime:
                        date = true;
                        break;
                    default:
                        break;
                }
            }
            if (date) //initialising date values for use with date scale
            {
                CurrentDate = (DateTime)first;
                OldDate = CurrentDate;
                TempDate = CurrentDate;
            }
            for (int iRow = 1; iRow <= CategoryCount; iRow++)
            {
                (object v, tc) = await this.GetCategoryValue(rpt, iRow);
                //make sure we are dealing with datetime type
                if (date)
                {
                    CurrentDate = (DateTime)v;

                }
                int drawLoc = (int)(rect.Left + ((iRow - 1) * ((double)rect.Width / catCount)));

                // Draw the category text
                int skip = 0;
                if (a.Visible && !a.Month)  //18052008WRP only show category labels if not month scale
                {
                    Draw2.Rectangle drawRect;
                    Draw2.Size size = Draw2.Size.Empty;

                    if (mustSize)
                    {	// Area chart - value is centered under the tick mark
                        if (s != null)
                        {
                            size = await s.MeasureString(rpt, g, v, TypeCode.Double, null, int.MaxValue);
                        }
                        else
                        {
                            size = await Style.MeasureStringDefaults(rpt, g, v, TypeCode.Double, null, int.MaxValue);
                        }
                    }

                    if (ChartDefn.Type == ChartTypeEnum.Area)
                    {   // Area chart - value is centered under the tick mark
                        drawRect =
                                new Draw2.Rectangle(drawLoc - (size.Width / 2), rect.Top + tickSize, size.Width, size.Height);
                    }
                    else    // Column/Line charts are just centered in the region.
                        drawRect = new Draw2.Rectangle(drawLoc, rect.Top + tickSize, drawWidth, rect.Height - tSize.Height);

                    if (mustSize && drawRect.Width < size.Width)
                    {
                        skip = (int)(size.Width / drawWidth);
                        drawRect.Width = size.Width;
                    }

                    if (s == null)
                        Style.DrawStringDefaults(g, v, drawRect);
                    else
                        await s.DrawString(rpt, g, v, tc, null, drawRect);
                }

                //09052008WRP Draw major gridlines and place category labels for months scale 
                if (a.Month && date && a.Visible)
                {

                    if (CurrentDate.Month != OldDate.Month)
                    {
                        TempDate = CurrentDate;
                        await DrawCategoryAxisGrid(rpt, g, a.MajorGridLines, new Draw2.Point(drawLoc, rect.Top), new Draw2.Point(drawLoc, plotTop)); //Don't overdraw the Y axis on the first gridline
                        CurrentDate = CurrentDate.AddMonths(OldDate.Month - CurrentDate.Month); // get previous category month value
                        string MonthString = CurrentDate.ToString("MMMM");
                        Draw2.Size lSize = await DrawCategoryTitleMeasure(rpt, g, MonthString, s);
                        int catlabelLoc = (int)((drawLoc - PreviousLocation) / 2) + PreviousLocation - (lSize.Width / 2);
                        await DrawCategoryLabel(rpt, g, MonthString, a.Style, new Draw2.Rectangle(catlabelLoc, rect.Top - (lSize.Height - 25), lSize.Width, lSize.Height));
                        PreviousLocation = drawLoc;
                        OldDate = TempDate;

                    }
                }
                if ((MajorGrid != 0) && ((iRow - 1) % MajorGrid == 0) && !(a.Month))
                //if (((iRow - 1) % ((int)a.MajorInterval.EvaluateDouble(rpt, this.ChartRow.RowNumber)) == 0) && !(a.Month)) 
                {
                    await DrawCategoryAxisGrid(rpt, g, a.MajorGridLines, new Draw2.Point(drawLoc, rect.Top), new Draw2.Point(drawLoc, plotTop));
                }
                // Draw the Major Tick Marks (if necessary)
                DrawCategoryAxisTick(g, true, a.MajorTickMarks, new Draw2.Point(drawLoc, rect.Top));
                iRow += skip;
            }//exit from for loop - no more category data

            if (a.Month && date && a.Visible)// 16052008WRP draw last category label for months scale
            {
                string MonthString = OldDate.ToString("MMMM");
                Draw2.Size lSize = await DrawCategoryTitleMeasure(rpt, g, MonthString, s);
                int catlabelLoc = (int)((rect.Right - PreviousLocation) / 2) + PreviousLocation - (lSize.Width / 2);
                await DrawCategoryLabel(rpt, g, MonthString, a.Style, new Draw2.Rectangle(catlabelLoc, rect.Top - (lSize.Height - 25), lSize.Width, lSize.Height));
            }


            // Draw the end on (if necessary)
            DrawCategoryAxisTick(g, true, a.MajorTickMarks, new Draw2.Point(rect.Right, rect.Top));

            return;
        }



        protected void DrawCategoryAxisTick(Draw2.Graphics g, bool bMajor, AxisTickMarksEnum tickType, Draw2.Point p)
        {
            int len = bMajor ? AxisTickMarkMajorLen : AxisTickMarkMinorLen;
            switch (tickType)
            {
                case AxisTickMarksEnum.Outside:
                    g.DrawLine(Draw2.Pens.Black, new Draw2.Point(p.X, p.Y), new Draw2.Point(p.X, p.Y + len));
                    break;
                case AxisTickMarksEnum.Inside:
                    g.DrawLine(Draw2.Pens.Black, new Draw2.Point(p.X, p.Y), new Draw2.Point(p.X, p.Y - len));
                    break;
                case AxisTickMarksEnum.Cross:
                    g.DrawLine(Draw2.Pens.Black, new Draw2.Point(p.X, p.Y - len), new Draw2.Point(p.X, p.Y + len));
                    break;
                case AxisTickMarksEnum.None:
                default:
                    break;
            }
            return;
        }

        protected async Task DrawCategoryAxisGrid(Report rpt, Draw2.Graphics g, ChartGridLines gl, Draw2.Point s, Draw2.Point e)
        {
            if (gl == null || !gl.ShowGridLines)
                return;

            if (gl.Style != null)
                await gl.Style.DrawStyleLine(rpt, g, null, s, e);
            else
                g.DrawLine(Draw2.Pens.Black, s, e);

            return;
        }

        async Task DrawColumnBar(Report rpt, Draw2.Graphics g, Draw2.Brush brush, Draw2.Rectangle rect, int iRow, int iCol)
        {
            if (rect.Height <= 0)
                return;
            //we want to separate the bars with some whitespace.. GJL 080208
            rect = new Draw2.Rectangle(rect.Left + 2, rect.Top, rect.Width - 3, rect.Height);
            g.FillRectangle(brush, rect);
            g.DrawRectangle(Draw2.Pens.Black, rect);

            if ((ChartSubTypeEnum)Enum.Parse(typeof(ChartSubTypeEnum), await _ChartDefn.Subtype.EvaluateString(rpt, _row)) == ChartSubTypeEnum.Stacked ||
                (ChartSubTypeEnum)Enum.Parse(typeof(ChartSubTypeEnum), await _ChartDefn.Subtype.EvaluateString(rpt, _row)) == ChartSubTypeEnum.PercentStacked)
            {
                await DrawDataPoint(rpt, g, rect, iRow, iCol);
            }
            else
            {
                Draw2.Point p;
                p = new Draw2.Point(rect.Left, rect.Top - 14); // todo: 14 is arbitrary
                await DrawDataPoint(rpt, g, p, iRow, iCol);
            }

            return;
        }

        protected async Task<(double incr, int intervalCount)> DrawValueAxis(Report rpt, Draw2.Graphics g, double min, double max,
                        Draw2.Rectangle rect, int plotLeft, int plotRight)
        {
            double incr = 0;
            int intervalCount = 0;
            if (this.ChartDefn.ValueAxis == null)
                return (incr, intervalCount);
            Axis a = this.ChartDefn.ValueAxis.Axis;
            if (a == null)
                return (incr, intervalCount);
            Style s = a.Style;

            //int intervalCount;
            //double incr;
            (incr, intervalCount) = await SetIncrementAndInterval(rpt, a, min, max);      // Calculate the interval count

            Draw2.Size tSize = await DrawTitleMeasure(rpt, g, a.Title);
            await DrawTitle(rpt, g, a.Title, new Draw2.Rectangle(rect.Left, rect.Top, tSize.Width, rect.Height));

            double v = min;
            for (int i = 0; i < intervalCount + 1; i++)
            {
                int h = (int)(((Math.Min(v, max) - min) / (max - min)) * rect.Height);
                if (h < 0)      // this is really some form of error
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
                    Draw2.Size size = await s.MeasureString(rpt, g, v, TypeCode.Double, null, int.MaxValue);
                    Draw2.Rectangle vRect =
                        new Draw2.Rectangle(rect.Left + tSize.Width, rect.Top + rect.Height - h - (size.Height / 2), rect.Width - tSize.Width, size.Height);
                    await s.DrawString(rpt, g, v, TypeCode.Double, null, vRect);
                }
                else
                {
                    Draw2.Size size = await Style.MeasureStringDefaults(rpt, g, v, TypeCode.Double, null, int.MaxValue);
                    Draw2.Rectangle vRect =
                        new Draw2.Rectangle(rect.Left + tSize.Width, rect.Top + rect.Height - h - (size.Height / 2), rect.Width - tSize.Width, size.Height);
                    Style.DrawStringDefaults(g, v, vRect);
                }

                await DrawValueAxisGrid(rpt, g, a.MajorGridLines, new Draw2.Point(plotLeft, rect.Top + rect.Height - h), new Draw2.Point(plotRight, rect.Top + rect.Height - h));
                await DrawValueAxisTick(rpt, g, true, a.MajorTickMarks, a.MajorGridLines, new Draw2.Point(plotLeft, rect.Top + rect.Height - h));

                v += incr;
            }

            // Draw the end points of the major grid lines
            await DrawValueAxisGrid(rpt, g, a.MajorGridLines, new Draw2.Point(plotLeft, rect.Top), new Draw2.Point(plotLeft, rect.Bottom));
            await DrawValueAxisTick(rpt, g, true, a.MajorTickMarks, a.MajorGridLines, new Draw2.Point(plotLeft, rect.Top));
            await DrawValueAxisGrid(rpt, g, a.MajorGridLines, new Draw2.Point(plotRight, rect.Top), new Draw2.Point(plotRight, rect.Bottom));

            //12052008WRP this line not required adds tick at bottom end of right y axis.
            //DrawValueAxisTick(rpt, g, true, a.MajorTickMarks, a.MajorGridLines, new Point(plotRight, rect.Bottom));

            return (incr, intervalCount);
        }


        //*******************************************************************************************************************************
        //Draws the second value axis
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rpt"></param>
        /// <param name="g"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="rect"></param>
        /// <param name="plotLeft"></param>
        /// <param name="plotRight"></param>
        /// <param name="incr"></param>
        /// <param name="intervalCount"></param>
        /// <param name="ScaleFactor"></param>
        /// <returns>ScaleFactor</returns>
        protected async Task<double> Draw2ndValueAxis(Report rpt, Draw2.Graphics g, double min, double max,
                        Draw2.Rectangle rect, int plotLeft, int plotRight, double incr, int intervalCount, double ScaleFactor)
        {

            if (this.ChartDefn.ValueAxis == null)
                return ScaleFactor;
            Axis a = this.ChartDefn.ValueAxis.Axis;
            if (a == null)
                return ScaleFactor;
            Style s = a.Style;

            double thisMin = 0;
            double thisMax = 0;

            (thisMax, thisMin) = await GetMaxMinDataValue(rpt, 0, 2);
            thisMin = 0; //Stop rescaling the min on autoscale


            //200208AJM GJL Yet another new scale
            _gridIncrs = 10; //PJR 20071113 - grid incrs set & adjusted in here now

            incr = thisMax / _gridIncrs;	// should be range between max and min?
            double log = Math.Floor(Math.Log10(Math.Abs(incr)));


            double logPow = Math.Pow(10, log) * Math.Sign(thisMax);
            double logDig = (int)((incr / logPow) + .5);

            // promote the MSD to either 1, 2, or 5
            if (logDig > 5.0)
                logDig = 10.0;
            else if (logDig > 2.0)
                logDig = 5.0;
            else if (logDig > 1.0)
                logDig = 2.0;
            //PJR 20071113 - reduce scale for large overscale options by decreasing _gridIncrs
            while (thisMax < logDig * logPow * _gridIncrs)
            {
                _gridIncrs--;
            }
            //_gridIncrs++;

            //PJR 20071113 - expand scale so that it is able to fit the max value by increasing _gridIncrs
            while (thisMax > logDig * logPow * _gridIncrs)
            {
                _gridIncrs++;
            }

            double tmpMax = thisMax;
            thisMax = (int)((logDig * logPow * _gridIncrs) + 0.5);
            if (tmpMax > thisMax - ((thisMax / _gridIncrs) * .5))
            {

                thisMax += (thisMax / _gridIncrs);
                _gridIncrs++;
            }
            ScaleFactor = thisMax / max;
            incr = thisMax / _gridIncrs;

            ////19022008AJM New Scaling
            //bool Happy = false;
            //double rIncr;
            //double rInt = intervalCount - 1;
            //double log;
            //double logPow;
            //double logDig;
            //double lMax = max;
            //for (int i = 0; i < 2; i++)
            //{
            //    rIncr = thisMax / rInt;
            //    log = Math.Floor(Math.Log10(Math.Abs(rIncr)));

            //    logPow = Math.Pow(10, log) * Math.Sign(thisMax);
            //    logDig = (int)(rIncr / logPow + .5);

            //    if (logDig > 5.0)
            //        logDig = 10.0;
            //    else if (logDig > 2.0)
            //        logDig = 5.0;
            //    else if (logDig > 1.0)
            //        logDig = 2.0;

            //    thisMax = (int)(logDig * logPow * rInt + 0.5);
            //    ScaleFactor = thisMax / lMax;
            //    if (thisMax < (ScaleFactor * (lMax - (incr / 2))))
            //    {
            //        i ++;
            //    }
            //    thisMax += logDig * logPow;
            //}



            //OK...  thisMax needs to be rounded up to the next interval...         
            //Scale based on thisMax and Max...        

            //ScaleFactor = thisMax / (max - (max / intervalCount)) ;
            //if (thisMax < max)
            //{
            //    ScaleFactor = 1 / ScaleFactor;
            //}
            //ScaleFactor = Math.Round(ScaleFactor, 1);


            //double factor = System.Math.Pow(10, System.Math.Floor(System.Math.Log10(ScaleFactor)));

            //ScaleFactor = System.Math.Round(ScaleFactor / factor,1) * factor;                    
            //    if( ScaleFactor < 1)
            //    { 
            //        while ((ScaleFactor * 10) % 5 != 0 && (ScaleFactor * 10) % 2 != 0 && ScaleFactor != 1)
            //            {
            //                ScaleFactor += 0.1;
            //            }             
            //    }
            //    else if (ScaleFactor < 10)
            //    {
            //        while (ScaleFactor % 5 != 0 || ScaleFactor % 2 != 0)
            //        {
            //            ScaleFactor++;
            //        }
            //    }
            //    else
            //    {
            //        while (ScaleFactor % 5 != 0)
            //        {
            //            ScaleFactor++;
            //        }

            //    }

            //if (thisMax < max)
            //{
            //    ScaleFactor = 1 / ScaleFactor;
            //    ScaleFactor = Math.Round(ScaleFactor, (int)System.Math.Floor(System.Math.Log10(factor)));
            //}


            Draw2.Size tSize = await DrawTitleMeasure(rpt, g, a.Title2);

            // rect.Width = (int)g.VisibleClipBounds.Width - rect.Left + 20;

            await DrawTitle(rpt, g, a.Title2, new Draw2.Rectangle((int)rect.Right - tSize.Width, rect.Top, tSize.Width, rect.Height));

            double v = min;
            for (int i = 0; i < _gridIncrs + 1; i++)
            {
                int h = (int)(((Math.Min(v, thisMax) - thisMin) / (thisMax - thisMin)) * rect.Height);
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
                    Draw2.Size size = await s.MeasureString(rpt, g, v, TypeCode.Double, null, int.MaxValue);
                    Draw2.Rectangle vRect =
                        new Draw2.Rectangle(rect.Left - (int)(tSize.Width * .5), rect.Top + rect.Height - h - (size.Height / 2), rect.Width - tSize.Width, size.Height);
                    await s.DrawString(rpt, g, v, TypeCode.Double, null, vRect);
                }
                else
                {
                    Draw2.Size size = await Style.MeasureStringDefaults(rpt, g, v, TypeCode.Double, null, int.MaxValue);
                    Draw2.Rectangle vRect =
                        new Draw2.Rectangle(rect.Left - (int)(tSize.Width * .5), rect.Top + rect.Height - h - (size.Height / 2), rect.Width - (tSize.Width * 2), size.Height);
                    Style.DrawStringDefaults(g, v, vRect);
                }

                v += incr;

                await DrawValueAxisTick(rpt, g, true, a.MajorTickMarks, a.MajorGridLines, new Draw2.Point(plotRight - (AxisTickMarkMajorLen / 2), rect.Top + rect.Height - h));

            }

            return ScaleFactor;
        }
        //*******************************************************************************************************************************

        protected async Task DrawValueAxisGrid(Report rpt, Draw2.Graphics g, ChartGridLines gl, Draw2.Point s, Draw2.Point e)
        {
            if (gl == null || !gl.ShowGridLines)
                return;

            if (gl.Style != null)
                await gl.Style.DrawStyleLine(rpt, g, null, s, e);
            else
                g.DrawLine(Draw2.Pens.Black, s, e);

            return;
        }

        protected async Task DrawValueAxisTick(Report rpt, Draw2.Graphics g, bool bMajor, AxisTickMarksEnum tickType, ChartGridLines gl, Draw2.Point p)
        {
            if (tickType == AxisTickMarksEnum.None)
                return;

            int len = bMajor ? AxisTickMarkMajorLen : AxisTickMarkMinorLen;
            Draw2.Point s, e;
            switch (tickType)
            {
                case AxisTickMarksEnum.Inside:
                    s = new Draw2.Point(p.X, p.Y);
                    e = new Draw2.Point(p.X + len, p.Y);
                    break;
                case AxisTickMarksEnum.Cross:
                    s = new Draw2.Point(p.X - len, p.Y);
                    e = new Draw2.Point(p.X + len, p.Y);
                    break;
                case AxisTickMarksEnum.Outside:
                default:
                    s = new Draw2.Point(p.X - len, p.Y);
                    e = new Draw2.Point(p.X, p.Y);
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

        //WhichAxis.... 1 = Left, 2 = Right GJL 140208
        protected async Task<Draw2.Size> ValueAxisSize(Report rpt, Draw2.Graphics g, double min, double max)
        {
            Draw2.Size size = Draw2.Size.Empty;

            if (ChartDefn.ValueAxis == null)
                return size;

            Axis a = ChartDefn.ValueAxis.Axis;

            if (a == null)
                return size;

            Draw2.Size minSize;
            Draw2.Size maxSize;
            if (!a.Visible)
            {
                minSize = maxSize = Draw2.Size.Empty;
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
            Draw2.Size titleSize = await DrawTitleMeasure(rpt, g, a.Title);
            size.Width += titleSize.Width;

            return size;
        }
    }
}
