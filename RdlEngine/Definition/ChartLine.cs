
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Draw2 = Majorsilence.Forms.Drawing;


namespace Majorsilence.Reporting.Rdl
{
    ///<summary>
    /// Line chart definition and processing.
    ///</summary>
    [Serializable]
    internal class ChartLine : ChartColumn
    {

        internal ChartLine(Report r, Row row, Chart c, MatrixCellEntry[,] m, Expression showTooltips, Expression showTooltipsX, Expression _ToolTipYFormat, Expression _ToolTipXFormat)
            : base(r, row, c, m, showTooltips, showTooltipsX, _ToolTipYFormat, _ToolTipXFormat)
        {
        }

        override internal async Task Draw(Report rpt)
        {
            CreateSizedBitmap();

            using (Draw2.Graphics g = Draw2.Graphics.FromImage(_bm))
            {
                // 06122007AJM Used to Force Higher Quality
                g.InterpolationMode = Draw2.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = Draw2.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = Draw2.Drawing2D.PixelOffsetMode.None;
                g.CompositingQuality = Draw2.Drawing2D.CompositingQuality.HighQuality;

                // Adjust the top margin to depend on the title height
                System.Drawing.Size titleSize = await DrawTitleMeasure(rpt, g, ChartDefn.Title);
                Layout.TopMargin = titleSize.Height;

                // 20022008 AJM GJL - Added new required info 
                double max = 0, min = 0; // Get the max and min values
                (max, min) = await GetValueMaxMin(rpt, max, min, 0, 1);

                await DrawChartStyle(rpt, g);

                // Draw title; routine determines if necessary
                await DrawTitle(rpt, g, ChartDefn.Title, new System.Drawing.Rectangle(0, 0, Layout.Width, Layout.TopMargin));

                // Adjust the left margin to depend on the Value Axis
                System.Drawing.Size vaSize = await ValueAxisSize(rpt, g, min, max);
                Layout.LeftMargin = vaSize.Width;

                // Draw legend
                System.Drawing.Rectangle lRect = await DrawLegend(rpt, g, ChartDefn.Type == ChartTypeEnum.Area ? false : true, true);

                // Adjust the bottom margin to depend on the Category Axis
                System.Drawing.Size caSize = await CategoryAxisSize(rpt, g);
                Layout.BottomMargin = caSize.Height;

                AdjustMargins(lRect, rpt, g);       // Adjust margins based on legend.

                // Draw Plot area
                await DrawPlotAreaStyle(rpt, g, lRect);
                int intervalCount = 0;
                double incr = 0;
                // Draw Value Axis
                if (vaSize.Width > 0)   // If we made room for the axis - we need to draw it
                    (incr, intervalCount) = await DrawValueAxis(rpt, g, min, max,
                        new System.Drawing.Rectangle(Layout.LeftMargin - vaSize.Width, Layout.TopMargin, vaSize.Width, Layout.PlotArea.Height), Layout.LeftMargin, _bm.Width - Layout.RightMargin);

                // Draw Category Axis
                if (caSize.Height > 0)
                    //09052008ajm passing chart bounds int
                    await DrawCategoryAxis(rpt, g,
                        new System.Drawing.Rectangle(Layout.LeftMargin, _bm.Height - Layout.BottomMargin, Layout.PlotArea.Width, caSize.Height), Layout.TopMargin,
                        caSize.Width);

                // Draw Plot area data 
                if (ChartDefn.Type == ChartTypeEnum.Area)
                {
                    if ((ChartSubTypeEnum)Enum.Parse(typeof(ChartSubTypeEnum), await _ChartDefn.Subtype.EvaluateString(rpt, _row)) == ChartSubTypeEnum.Stacked)
                        await DrawPlotAreaAreaStacked(rpt, g, min, max);
                    else if ((ChartSubTypeEnum)Enum.Parse(typeof(ChartSubTypeEnum), await _ChartDefn.Subtype.EvaluateString(rpt, _row)) == ChartSubTypeEnum.PercentStacked)
                        await DrawPlotAreaAreaPercentStacked(rpt, g);
                    else
                        await DrawPlotAreaArea(rpt, g, min, max);
                }
                else
                {
                    await DrawPlotAreaLine(rpt, g, min, max);
                }
                await DrawLegend(rpt, g, ChartDefn.Type == ChartTypeEnum.Area ? false : true, false);
            }
        }

        async Task DrawPlotAreaArea(Report rpt, Draw2.Graphics g, double min, double max)
        {
            // Draw Plot area data 
            int maxPointHeight = (int)Layout.PlotArea.Height;
            double widthCat = ((double)(Layout.PlotArea.Width) / (CategoryCount - 1));
            System.Drawing.Point[] saveP = new System.Drawing.Point[CategoryCount];   // used for drawing lines between points
            for (int iCol = 1; iCol <= SeriesCount; iCol++)
            {
                for (int iRow = 1; iRow <= CategoryCount; iRow++)
                {
                    double v = await this.GetDataValue(rpt, iRow, iCol);

                    int x = (int)(Layout.PlotArea.Left + ((iRow - 1) * widthCat));
                    int y = (int)(((Math.Min(v, max) - min) / (max - min)) * maxPointHeight);
                    System.Drawing.Point p = new System.Drawing.Point(x, Layout.PlotArea.Top + (maxPointHeight - y));
                    saveP[iRow - 1] = p;
                    await DrawLinePoint(rpt, g, await GetSeriesBrush(rpt, iRow, iCol), ChartMarkerEnum.None, p, iRow, iCol);

                    //Add a metafilecomment to use as a tooltip GJL 26092008
                    if (_showToolTips)
                    {
                        String val = "ToolTip:" + v.ToString(_tooltipYFormat) + "|X:" + (int)(p.X - 5) + "|Y:" + (int)(p.Y - 5) + "|W:" + 10 + "|H:" + 10;
                        g.AddMetafileComment(new System.Text.ASCIIEncoding().GetBytes(val));
                    }
                }
                DrawAreaBetweenPoints(g, await GetSeriesBrush(rpt, 1, iCol), saveP, null);
            }
            return;
        }

        async Task DrawPlotAreaAreaPercentStacked(Report rpt, Draw2.Graphics g)
        {
            double max = 1;             // 100% is the max
                                        // Draw Plot area data 
            int maxPointHeight = (int)Layout.PlotArea.Height;
            double widthCat = ((double)(Layout.PlotArea.Width) / (CategoryCount - 1));
            System.Drawing.Point[,] saveAllP = new System.Drawing.Point[CategoryCount, SeriesCount];  // used to collect all data points

            // Loop thru calculating all the data points
            for (int iRow = 1; iRow <= CategoryCount; iRow++)
            {
                int x = (int)(Layout.PlotArea.Left + ((iRow - 1) * widthCat));
                double sum = 0;
                for (int iCol = 1; iCol <= SeriesCount; iCol++)
                {
                    sum += await GetDataValue(rpt, iRow, iCol);
                }
                double v = 0;
                for (int iCol = 1; iCol <= SeriesCount; iCol++)
                {
                    v += await GetDataValue(rpt, iRow, iCol);

                    int y = (int)((Math.Min(v / sum, max) / max) * maxPointHeight);
                    System.Drawing.Point p = new System.Drawing.Point(x, Layout.PlotArea.Top + (maxPointHeight - y));
                    saveAllP[iRow - 1, iCol - 1] = p;
                }
            }

            // Now loop thru and plot all the points
            System.Drawing.Point[] saveP = new System.Drawing.Point[CategoryCount];   // used for drawing lines between points
            System.Drawing.Point[] priorSaveP = new System.Drawing.Point[CategoryCount];
            for (int iCol = 1; iCol <= SeriesCount; iCol++)
            {
                for (int iRow = 1; iRow <= CategoryCount; iRow++)
                {
                    double v = await this.GetDataValue(rpt, iRow, iCol);

                    int x = (int)(Layout.PlotArea.Left + ((iRow - 1) * widthCat));
                    int y = (int)((Math.Min(v, max) / max) * maxPointHeight);
                    System.Drawing.Point p = new System.Drawing.Point(x, Layout.PlotArea.Top + (maxPointHeight - y));
                    saveP[iRow - 1] = saveAllP[iRow - 1, iCol - 1];
                    await DrawLinePoint(rpt, g, await GetSeriesBrush(rpt, iRow, iCol), ChartMarkerEnum.None, p, iRow, iCol);

                    //Add a metafilecomment to use as a tooltip GJL 26092008
                    if (_showToolTips)
                    {
                        String val = "ToolTip:" + v.ToString(_tooltipYFormat) + "|X:" + (int)(p.X - 5) + "|Y:" + (int)(p.Y - 5) + "|W:" + 10 + "|H:" + 10;
                        g.AddMetafileComment(new System.Text.ASCIIEncoding().GetBytes(val));
                    }
                }
                DrawAreaBetweenPoints(g, await GetSeriesBrush(rpt, 1, iCol), saveP, iCol == 1 ? null : priorSaveP);
                // Save prior point values
                for (int i = 0; i < CategoryCount; i++)
                    priorSaveP[i] = saveP[i];
            }
            return;
        }

        async Task DrawPlotAreaAreaStacked(Report rpt, Draw2.Graphics g, double min, double max)
        {
            // Draw Plot area data 
            int maxPointHeight = (int)Layout.PlotArea.Height;
            double widthCat = ((double)(Layout.PlotArea.Width) / (CategoryCount - 1));
            System.Drawing.Point[,] saveAllP = new System.Drawing.Point[CategoryCount, SeriesCount];  // used to collect all data points

            // Loop thru calculating all the data points
            for (int iRow = 1; iRow <= CategoryCount; iRow++)
            {
                int x = (int)(Layout.PlotArea.Left + ((iRow - 1) * widthCat));
                double v = 0;
                for (int iCol = 1; iCol <= SeriesCount; iCol++)
                {
                    v += await GetDataValue(rpt, iRow, iCol);
                    int y = (int)(((Math.Min(v, max) - min) / (max - min)) * maxPointHeight);
                    System.Drawing.Point p = new System.Drawing.Point(x, Layout.PlotArea.Top + (maxPointHeight - y));
                    saveAllP[iRow - 1, iCol - 1] = p;
                }
            }

            // Now loop thru and plot all the points
            System.Drawing.Point[] saveP = new System.Drawing.Point[CategoryCount];   // used for drawing lines between points
            System.Drawing.Point[] priorSaveP = new System.Drawing.Point[CategoryCount];
            for (int iCol = 1; iCol <= SeriesCount; iCol++)
            {
                for (int iRow = 1; iRow <= CategoryCount; iRow++)
                {
                    double v = await this.GetDataValue(rpt, iRow, iCol);

                    int x = (int)(Layout.PlotArea.Left + ((iRow - 1) * widthCat));
                    int y = (int)(((Math.Min(v, max) - min) / (max - min)) * maxPointHeight);
                    System.Drawing.Point p = new System.Drawing.Point(x, Layout.PlotArea.Top + (maxPointHeight - y));
                    saveP[iRow - 1] = saveAllP[iRow - 1, iCol - 1];
                    await DrawLinePoint(rpt, g, await GetSeriesBrush(rpt, iRow, iCol), ChartMarkerEnum.None, p, iRow, iCol);
                    //Add a metafilecomment to use as a tooltip GJL 26092008
                    if (_showToolTips)
                    {
                        String val = "ToolTip:" + v.ToString(_tooltipYFormat) + "|X:" + (int)(saveP[iRow - 1].X - 5) + "|Y:" + (int)(saveP[iRow - 1].Y - 5) + "|W:" + 10 + "|H:" + 10;
                        g.AddMetafileComment(new System.Text.ASCIIEncoding().GetBytes(val));
                    }


                }
                DrawAreaBetweenPoints(g, await GetSeriesBrush(rpt, 1, iCol), saveP, iCol == 1 ? null : priorSaveP);
                // Save prior point values
                for (int i = 0; i < CategoryCount; i++)
                    priorSaveP[i] = saveP[i];
            }
            return;
        }

        async Task DrawPlotAreaLine(Report rpt, Draw2.Graphics g, double min, double max)
        {
            // Draw Plot area data 
            int maxPointHeight = (int)Layout.PlotArea.Height;
            double widthCat = ((double)(Layout.PlotArea.Width) / CategoryCount);
            System.Drawing.Point[] saveP = new System.Drawing.Point[CategoryCount];   // used for drawing lines between points
            for (int iCol = 1; iCol <= SeriesCount; iCol++)
            {
                for (int iRow = 1; iRow <= CategoryCount; iRow++)
                {
                    double v = await this.GetDataValue(rpt, iRow, iCol);

                    int x = (int)(Layout.PlotArea.Left + ((iRow - 1) * widthCat) + (widthCat / 2));
                    int y = (int)(((Math.Min(v, max) - min) / (max - min)) * maxPointHeight);
                    System.Drawing.Point p = new System.Drawing.Point(x, Layout.PlotArea.Top + (maxPointHeight - y));
                    saveP[iRow - 1] = p;
                    bool DrawPoint = getNoMarkerVal(rpt, iCol, 1) == false;
                    //dont draw the point if I say not to!
                    if (DrawPoint) { await DrawLinePoint(rpt, g, await GetSeriesBrush(rpt, iRow, iCol), SeriesMarker[iCol - 1], p, iRow, iCol); }

                    //Add a metafilecomment to use as a tooltip GJL 26092008
                    if (_showToolTips)
                    {
                        String val = "ToolTip:" + v.ToString(_tooltipYFormat) + "|X:" + (int)(p.X - 5) + "|Y:" + (int)(p.Y - 5) + "|W:" + 10 + "|H:" + 10;
                        g.AddMetafileComment(new System.Text.ASCIIEncoding().GetBytes(val));
                    }
                }

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
                await DrawLineBetweenPoints(g, rpt, await GetSeriesBrush(rpt, 1, iCol), saveP, intLineSize);
            }
            return;
        }

        void DrawAreaBetweenPoints(Draw2.Graphics g, Draw2.Brush brush, System.Drawing.Point[] points, System.Drawing.Point[] previous)
        {
            if (points.Length <= 1)     // Need at least 2 points
                return;

            Draw2.Pen p = null;
            try
            {
                p = new Draw2.Pen(brush, 1);    // todo - use line from style ????
                g.DrawLines(p, points);
                System.Drawing.PointF[] poly;
                if (previous == null)
                {   // The bottom is the bottom of the chart
                    poly = new System.Drawing.PointF[points.Length + 3];
                    int i = 0;
                    foreach (System.Drawing.Point pt in points)
                    {
                        poly[i++] = pt;
                    }
                    poly[i++] = new System.Drawing.PointF(points[points.Length - 1].X, Layout.PlotArea.Bottom);
                    poly[i++] = new System.Drawing.PointF(points[0].X, Layout.PlotArea.Bottom);
                    poly[i] = new System.Drawing.PointF(points[0].X, points[0].Y);
                }
                else
                {   // The bottom is the previous line
                    poly = new System.Drawing.PointF[(points.Length * 2) + 1];
                    int i = 0;
                    foreach (System.Drawing.Point pt in points)
                    {
                        poly[i] = pt;
                        poly[points.Length + i] = previous[previous.Length - 1 - i];
                        i++;
                    }
                    poly[poly.Length - 1] = poly[0];
                }
                g.FillPolygon(brush, poly);
            }
            finally
            {
                if (p != null)
                    p.Dispose();
            }
            return;
        }

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
                // 20022008 AJM GJL - Added thicker lines


                p = new Draw2.Pen(brush, intLineSize);    // todo - use line from style ????


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

        async Task DrawLinePoint(Report rpt, Draw2.Graphics g, Draw2.Brush brush, ChartMarkerEnum marker, System.Drawing.Point p, int iRow, int iCol)
        {
            Draw2.Pen pen = null;
            try
            {
                pen = new Draw2.Pen(brush);
                // 20022008 AJM GJL - Added bigger points
                DrawLegendMarker(g, brush, pen, marker, p.X - 5, p.Y - 5, 10);
                await DrawDataPoint(rpt, g, new System.Drawing.Point(p.X - 5, p.Y + 5), iRow, iCol);
            }
            finally
            {
                if (pen != null)
                    pen.Dispose();
            }

            return;
        }

    }
}
