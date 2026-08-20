
using System;
using System.Collections;
using System.Threading.Tasks;

#if DRAWINGCOMPAT
using Draw2 = Majorsilence.Forms.Drawing;
using Drawing2D = Majorsilence.Forms.Drawing.Drawing2D;
using Imaging = Majorsilence.Forms.Drawing.Imaging;
#else
using Draw2 = System.Drawing;
using Drawing2D = System.Drawing.Drawing2D;
using Imaging = System.Drawing.Imaging;
#endif



namespace Majorsilence.Reporting.Rdl
{
    ///<summary>
    /// Base class of all charts.
    ///</summary>
    internal abstract class ChartBase : IDisposable
    {
        protected Chart _ChartDefn; // GJL 14082008 Using Vector Graphics
        MatrixCellEntry[,] _DataDefn;
        protected Draw2.Bitmap _bm;
#if !DRAWINGCOMPAT
        protected Imaging.Metafile _mf = null; // GJL 14082008 Using Vector Graphics
        public System.IO.MemoryStream _aStream; // GJL 14082008 Using Vector Graphics
#endif
        protected ChartLayout Layout;
        Draw2.Brush[] _SeriesBrush;
        ChartMarkerEnum[] _SeriesMarker;
        protected int _LastCategoryWidth = 0;
        protected Row _row;					// row chart created on
        protected int _gridIncrs = 10; //PJR 20071113 - made global to class so it can be "adjusted" to fit MAX value
        protected bool _showToolTips;
        protected bool _showToolTipsX;
        protected string _tooltipXFormat;
        protected string _tooltipYFormat;

        internal ChartBase(Report r, Row row, Chart c, MatrixCellEntry[,] m, Expression showTooltips, Expression showTooltipsX, Expression _ToolTipYFormat, Expression _ToolTipXFormat)
        {
            _ChartDefn = c;
            _row = row;
            _DataDefn = m;
            _bm = null;
            int width = _ChartDefn.WidthCalc(r, null);
            int height = RSize.PixelsFromPoints(_ChartDefn.HeightOrOwnerHeight);
            Layout = new ChartLayout(width, height);
            _SeriesBrush = null;
            _SeriesMarker = null;

            // HACK: async
            Task.Run(async () => { 
                _showToolTips =  await showTooltips.EvaluateBoolean(r, row);
                _showToolTipsX = await showTooltipsX.EvaluateBoolean(r, row);
                _tooltipYFormat = await _ToolTipYFormat.EvaluateString(r, row);
                _tooltipXFormat = await _ToolTipXFormat.EvaluateString(r, row);
            }).GetAwaiter().GetResult();
        }

        internal virtual Task Draw(Report rpt)
        {
            return Task.CompletedTask;
        }

        protected Row ChartRow
        {
            get { return _row; }
        }

        internal void Save(Report rpt, System.IO.Stream stream, Imaging.ImageFormat im)
        {
            if (_bm == null)
                Draw(rpt);
#if !DRAWINGCOMPAT
            if (_mf != null)
            {
                _mf.Save(stream, im);
            }
            else
            {
                _bm.Save(stream, im);
            }
#else
            _bm.Save(stream, im);
#endif
        }

        internal Draw2.Image Image(Report rpt) // GJL 14082008 Using Vector Graphics
        {
            if (_bm == null)
                Draw(rpt);
#if !DRAWINGCOMPAT
            return _mf == null? _bm : _mf;
#else
            return _bm;
#endif

        }

        protected Draw2.Bitmap CreateSizedBitmap()
        {
            if (_bm != null)
            {
                _bm.Dispose();
                _bm = null;
            }
            _bm = new Draw2.Bitmap(Layout.Width, Layout.Height);
            return _bm;
        }

        protected Draw2.Bitmap CreateSizedBitmap(int W, int H)
        {
            if (_bm != null)
            {
                _bm.Dispose();
                _bm = null;
            }
            _bm = new Draw2.Bitmap(W, H);
            return _bm;
        }

        protected int AxisTickMarkMajorLen
        {
            get { return 6; }
        }

        protected int AxisTickMarkMinorLen
        {
            get { return 3; }
        }

        protected int CategoryCount
        {
            get { return (_DataDefn.GetLength(0) - 1); }
        }

        protected Chart ChartDefn
        {
            get { return _ChartDefn; }
        }

        protected MatrixCellEntry[,] DataDefn
        {
            get { return _DataDefn; }
        }

        protected async Task<Draw2.Brush[]> SeriesBrush(Report rpt, Row row, ReportDefn defn)
        {
            if (_SeriesBrush == null)
                _SeriesBrush = await GetSeriesBrushes(rpt, row, defn);  // These are all from Brushes class; so no Dispose should be used
            return _SeriesBrush;
        }

        protected ChartMarkerEnum[] SeriesMarker
        {
            get
            {
                if (_SeriesMarker == null)
                    _SeriesMarker = GetSeriesMarkers();
                return _SeriesMarker;
            }
        }

        protected int SeriesCount
        {
            get { return (_DataDefn.GetLength(1) - 1); }
        }

        protected async Task DrawChartStyle(Report rpt, Draw2.Graphics g)
        {
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, Layout.Width, Layout.Height);
            if (_ChartDefn.Style == null)
            {
                g.FillRectangle(Draw2.Brushes.White, rect);
            }
            else
            {
                Row r = FirstChartRow(rpt);
                await _ChartDefn.Style.DrawBorder(rpt, g, r, rect);
                await _ChartDefn.Style.DrawBackground(rpt, g, r, rect);
            }

            return;
        }

        // Draws the Legend and then returns the rectangle it drew in
        protected async Task<System.Drawing.Rectangle> DrawLegend(Report rpt, Draw2.Graphics g, bool bMarker, bool bBeforePlotDrawn)
        {
            Legend l = _ChartDefn.Legend;
            if (l == null)
                return System.Drawing.Rectangle.Empty;
            if (!l.Visible)
                return System.Drawing.Rectangle.Empty;
            if (_ChartDefn.SeriesGroupings == null)
                return System.Drawing.Rectangle.Empty;
            if (bBeforePlotDrawn)
            {
                if (this.IsLegendInsidePlotArea())
                    return System.Drawing.Rectangle.Empty;
            }
            else if (!IsLegendInsidePlotArea())         // Only draw legend after if inside the plot
                return System.Drawing.Rectangle.Empty;

            Draw2.Font drawFont = null;
            Draw2.Brush drawBrush = null;
            Draw2.StringFormat drawFormat = null;

            // calculated bounding rectangle of the legend
            System.Drawing.Rectangle rRect;
            Style s = l.Style;
            try     // no matter what we want to dispose of the graphic resources
            {
                if (s == null)
                {
                    drawFont = new Draw2.Font("Arial", 10);
                    drawBrush = new Draw2.SolidBrush(System.Drawing.Color.Black);
                    drawFormat = new Draw2.StringFormat();
                    drawFormat.Alignment = Draw2.StringAlignment.Near;
                }
                else
                {
                    drawFont = await s.GetFont(rpt, null);
                    drawBrush = await s.GetBrush(rpt, null);
                    drawFormat = await s.GetStringFormat(rpt, null, Draw2.StringAlignment.Near);
                }

                int x, y, h;
                int maxTextWidth, maxTextHeight;
                drawFormat.FormatFlags |= Draw2.StringFormatFlags.NoWrap;
                System.Drawing.Size[] sizes;

                (sizes, maxTextWidth, maxTextHeight) = await DrawLegendMeasure(rpt, g, drawFont, drawFormat,
                 new System.Drawing.SizeF(Layout.Width, Layout.Height));
                int boxSize = (int)(maxTextHeight * .8);
                int totalItemWidth = 0;         // width of a legend item
                int totalWidth, totalHeight;    // final height and width of legend

                // calculate the height and width of the rectangle
                switch (l.Layout)
                {
                    case LegendLayoutEnum.Row:
                        // we need to loop thru all the width
                        totalWidth = 0;
                        for (int i = 0; i < SeriesCount; i++)
                        {
                            if (sizes[i].Width != 0)  //14052008WRP when legend valeus are 0 don't add extra boxsize
                                totalWidth += (sizes[i].Width + (boxSize * 2));
                        }
                        totalHeight = (int)(maxTextHeight + (maxTextHeight * .1));
                        h = totalHeight;
                        totalItemWidth = maxTextWidth + (boxSize * 2);
                        drawFormat.Alignment = Draw2.StringAlignment.Near;    // Force alignment to near
                        break;
                    case LegendLayoutEnum.Table:
                        // for table we simplify to have TWO columns (i.e. don't do anything too tricky
                        totalWidth = totalItemWidth = (maxTextWidth + (boxSize * 2)) * 2;     // make width twice as big as longest entry
                        h = (int)(maxTextHeight + (maxTextHeight * .1));
                        totalHeight = h * (SeriesCount + (SeriesCount % 2)) / 2;
                        break;
                    case LegendLayoutEnum.Column:
                    default:
                        totalWidth = totalItemWidth = maxTextWidth + (boxSize * 2);
                        h = (int)(maxTextHeight + (maxTextHeight * .1));
                        totalHeight = h * SeriesCount;
                        break;
                }

                // calculate the location of the legend rectangle
                if (this.IsLegendInsidePlotArea())
                    switch (l.Position)
                    {
                        case LegendPositionEnum.BottomCenter:
                            x = Layout.PlotArea.X + (Layout.PlotArea.Width / 2) - (totalWidth / 2);
                            y = Layout.PlotArea.Y + Layout.PlotArea.Height - totalHeight - 2;
                            break;
                        case LegendPositionEnum.BottomLeft:
                        case LegendPositionEnum.LeftBottom:
                            x = Layout.PlotArea.X + 2;
                            y = Layout.PlotArea.Y + Layout.PlotArea.Height - totalHeight - 2;
                            break;
                        case LegendPositionEnum.BottomRight:
                        case LegendPositionEnum.RightBottom:
                            x = Layout.PlotArea.X + Layout.PlotArea.Width - totalWidth;
                            y = Layout.PlotArea.Y + Layout.PlotArea.Height - totalHeight - 2;
                            break;
                        case LegendPositionEnum.LeftCenter:
                            x = Layout.PlotArea.X + 2;
                            y = Layout.PlotArea.Y + (Layout.PlotArea.Height / 2) - (totalHeight / 2);
                            break;
                        case LegendPositionEnum.LeftTop:
                        case LegendPositionEnum.TopLeft:
                            x = Layout.PlotArea.X + 2;
                            y = Layout.PlotArea.Y + 2;
                            break;
                        case LegendPositionEnum.RightCenter:
                            x = Layout.PlotArea.X + Layout.PlotArea.Width - totalWidth - 2;
                            y = Layout.PlotArea.Y + (Layout.PlotArea.Height / 2) - (totalHeight / 2);
                            break;
                        case LegendPositionEnum.TopCenter:
                            x = Layout.PlotArea.X + (Layout.PlotArea.Width / 2) - (totalWidth / 2);
                            y = Layout.PlotArea.Y + +2;
                            break;
                        case LegendPositionEnum.TopRight:
                        case LegendPositionEnum.RightTop:
                        default:
                            x = Layout.PlotArea.X + Layout.PlotArea.Width - totalWidth - 2;
                            y = Layout.PlotArea.Y + +2;
                            break;
                    }
                else switch (l.Position)
                    {
                        case LegendPositionEnum.BottomCenter:
                            x = (Layout.Width / 2) - (totalWidth / 2);
                            y = Layout.Height - totalHeight - 2;
                            break;
                        case LegendPositionEnum.BottomLeft:
                        case LegendPositionEnum.LeftBottom:
                            if (IsLegendInsidePlotArea())
                                x = Layout.LeftMargin;
                            else
                                x = 0;
                            y = Layout.Height - totalHeight - 2;
                            break;
                        case LegendPositionEnum.BottomRight:
                        case LegendPositionEnum.RightBottom:
                            x = Layout.Width - totalWidth;
                            y = Layout.Height - totalHeight - 2;
                            break;
                        case LegendPositionEnum.LeftCenter:
                            x = 2;
                            y = (Layout.Height / 2) - (totalHeight / 2);
                            break;
                        case LegendPositionEnum.LeftTop:
                        case LegendPositionEnum.TopLeft:
                            x = 2;
                            y = Layout.TopMargin + 2;
                            break;
                        case LegendPositionEnum.RightCenter:
                            x = Layout.Width - totalWidth - 2;
                            y = (Layout.Height / 2) - (totalHeight / 2);
                            break;
                        case LegendPositionEnum.TopCenter:
                            x = (Layout.Width / 2) - (totalWidth / 2);
                            y = Layout.TopMargin + 2;
                            break;
                        case LegendPositionEnum.TopRight:
                        case LegendPositionEnum.RightTop:
                        default:
                            x = Layout.Width - totalWidth - 2;
                            y = Layout.TopMargin + 2;
                            break;
                    }

                // We now know enough to calc the bounding rectangle of the legend
                rRect = new System.Drawing.Rectangle(x - 1, y - 1, totalWidth + 2, totalHeight + 2);
                if (s != null)
                {
                    await s.DrawBackground(rpt, g, null, rRect);  // draw (or not draw) background 
                    await s.DrawBorder(rpt, g, null, rRect);      // draw (or not draw) border depending on style
                }

                int saveX = x;
                ChartMarkerEnum cm = this.ChartDefn.Type == ChartTypeEnum.Bubble ? ChartMarkerEnum.Bubble : ChartMarkerEnum.None;
                for (int iCol = 1; iCol <= SeriesCount; iCol++)
                {
                    string c = await GetSeriesValue(rpt, iCol);
                    if (c != "") //14052008WRP Cater for empty strings in the legend
                    {
                        System.Drawing.Rectangle rect;
                        // 20022008 AJM GJL - Draw correct legend icon (Column\Line)
                        Type t = null;
                        t = GetSeriesBrush(rpt, 1, iCol).GetType();
                        /* The following 2 lines have been added to draw the correct legend for column chart with line plot types
                         * 06122007AJM */
                        cm = ChartMarkerEnum.None;
                        bool isLine = GetPlotType(rpt, iCol, 1).ToUpper() == "LINE";

                        if ((bMarker || isLine) || (this.ChartDefn.Type == ChartTypeEnum.Scatter && t == typeof(Drawing2D.HatchBrush)))
                            cm = SeriesMarker[iCol - 1];
                        if (isLine && this.ChartDefn.Type == ChartTypeEnum.Scatter)
                        {
                            cm = ChartMarkerEnum.Line;
                        }

                        bool NoMarker = getNoMarkerVal(rpt, iCol, 1);
                        if (NoMarker) { cm = ChartMarkerEnum.Line; }

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


                        Draw2.SolidBrush b;
                        switch (l.Layout)
                        {
                            case LegendLayoutEnum.Row:
                                rect = new System.Drawing.Rectangle(x + boxSize + (boxSize / 2), y, totalItemWidth - boxSize - (boxSize / 2), h);
                                if (c != "") //14052008WRP to cater for empty strings in the legend
                                {
                                    g.DrawString(c, drawFont, drawBrush, rect, drawFormat);

                                    if ((cm != ChartMarkerEnum.None || this.ChartDefn.Type == ChartTypeEnum.Scatter) && (t == typeof(Drawing2D.HatchBrush))) //GJL 110208 - Don't draw pattern for lines or Bubbles
                                    {
                                        Drawing2D.HatchBrush hb = (Drawing2D.HatchBrush)await GetSeriesBrush(rpt, 1, iCol);
                                        b = new Draw2.SolidBrush(hb.ForegroundColor);

                                        DrawLegendBox(g, b,
                                        cm, x, y + 1, boxSize, intLineSize);
                                    }
                                    else
                                    {
                                        DrawLegendBox(g, await GetSeriesBrush(rpt, 1, iCol),
                                            cm, x, y + 1, boxSize, intLineSize);
                                    }


                                    x += (sizes[iCol - 1].Width + (boxSize * 2));
                                }
                                break;
                            case LegendLayoutEnum.Table:
                                rect = new System.Drawing.Rectangle(x + boxSize + (boxSize / 2), y, maxTextWidth, h);
                                g.DrawString(c, drawFont, drawBrush, rect, drawFormat);

                                if (cm != ChartMarkerEnum.None && (t == typeof(Drawing2D.HatchBrush))) //GJL 110208 - Don't draw pattern for lines
                                {
                                    Drawing2D.HatchBrush hb = (Drawing2D.HatchBrush)await GetSeriesBrush(rpt, 1, iCol);
                                    b = new Draw2.SolidBrush(hb.ForegroundColor);

                                    DrawLegendBox(g, /*GetSeriesBrushesExcel(iCol - 1)*/ b,
                                    cm, x, y + 1, boxSize, intLineSize);
                                }
                                else
                                { DrawLegendBox(g, await GetSeriesBrush(rpt, 1, iCol), cm, x + 1, y, boxSize, intLineSize); }


                                if (iCol % 2 == 0)
                                {
                                    y += h;
                                    x = saveX;
                                }
                                else
                                {
                                    x = saveX + (rRect.Width / 2);
                                }
                                break;
                            case LegendLayoutEnum.Column:
                            default:
                                rect = new System.Drawing.Rectangle(x + boxSize + (boxSize / 2), y, maxTextWidth, h);
                                g.DrawString(c, drawFont, drawBrush, rect, drawFormat);


                                if (cm != ChartMarkerEnum.None && (t == typeof(Drawing2D.HatchBrush)))       //GJL 110208 - Don't draw pattern for lines                          
                                {
                                    Drawing2D.HatchBrush hb = (Drawing2D.HatchBrush)await GetSeriesBrush(rpt, 1, iCol);
                                    b = new Draw2.SolidBrush(hb.ForegroundColor);

                                    DrawLegendBox(g, /*GetSeriesBrushesExcel(iCol - 1)*/ b,
                                    cm, x, y + 1, boxSize, intLineSize);
                                }
                                else
                                {
                                    DrawLegendBox(g, await GetSeriesBrush(rpt, 1, iCol),
                                        cm, x + 1, y, boxSize, intLineSize);
                                }

                                y += h;
                                break;
                        }
                    }
                }
            }
            finally
            {
                if (drawFont != null)
                    drawFont.Dispose();
                if (drawBrush != null)
                    drawBrush.Dispose();
                if (drawFormat != null)
                    drawFormat.Dispose();
            }
            if (s != null)
                rRect = await s.PaddingAdjust(rpt, null, rRect, true);
            return rRect;

        }

        void DrawLegendBox(Draw2.Graphics g, Draw2.Brush b, ChartMarkerEnum marker, int x, int y, int boxSize)
        {
            DrawLegendBox(g, b, marker, x, y, boxSize, 2);
        }

        void DrawLegendBox(Draw2.Graphics g, Draw2.Brush b, ChartMarkerEnum marker, int x, int y, int boxSize, int intLineSize)
        {
            Draw2.Pen p = null;
            int mSize = boxSize / 2;        // Marker size is 1/2 of box size	
            try
            {
                if (marker < ChartMarkerEnum.Count)
                {
                    p = new Draw2.Pen(b, intLineSize);
                    if (this.ChartDefn.Type != ChartTypeEnum.Scatter)
                    {
                        g.DrawLine(p, new System.Drawing.Point(x, y + ((boxSize + 1) / 2)), new System.Drawing.Point(x + boxSize, y + ((boxSize + 1) / 2)));
                    }
                    x = x + ((boxSize - mSize) / 2);
                    y = y + ((boxSize - mSize) / 2);
                    if (mSize % 2 == 0)
                        mSize++;
                }

                if (marker == ChartMarkerEnum.None)
                {
                    g.FillRectangle(b, x, y, boxSize, boxSize);
                }
                else if (marker == ChartMarkerEnum.Bubble)
                {
                    g.FillEllipse(b, x, y, boxSize, boxSize);
                }
                else if (marker == ChartMarkerEnum.Line)
                {
                    // this is only to draw lines for line plot types on scatter charts
                    p = new Draw2.Pen(b, intLineSize);
                    g.DrawLine(p, new System.Drawing.Point(x, y + ((boxSize + 1) / 2)), new System.Drawing.Point(x + boxSize, y + ((boxSize + 1) / 2)));
                }
                else
                {
                    DrawLegendMarker(g, b, p, marker, x, y, mSize);
                }
            }
            finally
            {
                if (p != null)
                    p.Dispose();
            }
        }

        internal void DrawLegendMarker(Draw2.Graphics g, Draw2.Brush b, Draw2.Pen p, ChartMarkerEnum marker, int x, int y, int mSize)
        {
            System.Drawing.PointF[] points;
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
                    // 20022008 AJM GJL - Changed to line - plus is hard to see
                    p = new Draw2.Pen(p.Brush, 2);
                    g.DrawLine(p, new System.Drawing.Point(x + ((mSize + 1) / 2), y), new System.Drawing.Point(x + ((mSize + 1) / 2), y + mSize));
                    //g.DrawLine(p, new Point(x + (mSize + 1)/2, y + (mSize+1)/2), new Point(x + mSize, y + (mSize+1)/2));
                    break;
                case ChartMarkerEnum.Diamond:
                    points = new System.Drawing.PointF[5];
                    points[0] = points[4] = new System.Drawing.Point(x + ((mSize + 1) / 2), y);    // starting and ending point
                    points[1] = new System.Drawing.PointF(x, y + ((mSize + 1) / 2));
                    points[2] = new System.Drawing.PointF(x + ((mSize + 1) / 2), y + mSize);
                    points[3] = new System.Drawing.PointF(x + mSize, y + ((mSize + 1) / 2));
                    g.FillPolygon(b, points);
                    break;
                case ChartMarkerEnum.Triangle:
                    points = new System.Drawing.PointF[4];
                    points[0] = points[3] = new System.Drawing.PointF(x + ((mSize + 1) / 2), y);   // starting and ending point
                    points[1] = new System.Drawing.PointF(x, y + mSize);
                    points[2] = new System.Drawing.PointF(x + mSize, y + mSize);
                    g.FillPolygon(b, points);
                    break;
                case ChartMarkerEnum.X:
                    p = new Draw2.Pen(p.Brush, 2);// 20022008 AJM GJL
                    g.DrawLine(p, new System.Drawing.Point(x, y), new System.Drawing.Point(x + mSize, y + mSize));
                    g.DrawLine(p, new System.Drawing.Point(x, y + mSize), new System.Drawing.Point(x + mSize, y));// 20022008 AJM GJL
                    break;
            }
            return;
        }

        // Measures the Legend and then returns the rectangle it drew in
        protected async Task<(System.Drawing.Size[] sizes, int maxWidth, int maxHeight)> DrawLegendMeasure(Report rpt, Draw2.Graphics g, Draw2.Font f, Draw2.StringFormat sf, System.Drawing.SizeF maxSize)
        {
            System.Drawing.Size[] sizes = new System.Drawing.Size[SeriesCount];
            int maxHeight = 0;
            int maxWidth = maxHeight = 0;

            for (int iCol = 1; iCol <= SeriesCount; iCol++)
            {
                string c = await GetSeriesValue(rpt, iCol);
                if (c != "")  //14052008WRP cater for empty strings in legend names
                {
                    System.Drawing.SizeF ms = g.MeasureString(c, f, maxSize, sf);
                    sizes[iCol - 1] = new System.Drawing.Size((int)Math.Ceiling(ms.Width),
                                             (int)Math.Ceiling(ms.Height));
                    if (sizes[iCol - 1].Width > maxWidth)
                        maxWidth = sizes[iCol - 1].Width;
                    if (sizes[iCol - 1].Height > maxHeight)
                        maxHeight = sizes[iCol - 1].Height;
                }

            }
            return (sizes, maxWidth, maxHeight);
        }

        protected async Task DrawPlotAreaStyle(Report rpt, Draw2.Graphics g, System.Drawing.Rectangle crect)
        {
            if (_ChartDefn.PlotArea == null || _ChartDefn.PlotArea.Style == null)
                return;
            System.Drawing.Rectangle rect = Layout.PlotArea;
            Style s = _ChartDefn.PlotArea.Style;

            Row r = FirstChartRow(rpt);

            if (rect.IntersectsWith(crect))
            {
                // This occurs when the legend is drawn inside the plot area
                //    we don't want to draw in the legend
                Draw2.Region rg = null;
                try
                {
                    //				rg = new Region(rect);	// TODO: this doesn't work; nothing draws
                    //				rg.Complement(crect);
                    //				Region saver = g.Clip;
                    //				g.Clip = rg;
                    await s.DrawBackground(rpt, g, r, rect);
                    //				g.Clip = saver;
                }
                finally
                {
                    if (rg != null)
                        rg.Dispose();
                }
            }
            else
                await s.DrawBackground(rpt, g, r, rect);

            return;
        }

        protected async Task DrawTitle(Report rpt, Draw2.Graphics g, Title t, System.Drawing.Rectangle rect)
        {
            if (t == null)
                return;

            if (t.Caption == null)
                return;

            Row r = FirstChartRow(rpt);
            object title = await t.Caption.Evaluate(rpt, r);
            if (t.Style != null)
            {
                await t.Style.DrawString(rpt, g, title, t.Caption.GetTypeCode(), r, rect);
                await t.Style.DrawBorder(rpt, g, r, rect);
            }
            else
                Style.DrawStringDefaults(g, title, rect);

            return;
        }

        protected async Task<System.Drawing.Size> DrawTitleMeasure(Report rpt, Draw2.Graphics g, Title t)
        {
            System.Drawing.Size size = System.Drawing.Size.Empty;

            if (t == null || t.Caption == null)
                return size;

            Row r = FirstChartRow(rpt);
            object title = await t.Caption.Evaluate(rpt, r);
            if (t.Style != null)
                size = await t.Style.MeasureString(rpt, g, title, t.Caption.GetTypeCode(), r, int.MaxValue);
            else
                size = await Style.MeasureStringDefaults(rpt, g, title, t.Caption.GetTypeCode(), r, int.MaxValue);

            return size;
        }

        //15052008WRP - Draw category month labels
        protected async Task DrawCategoryLabel(Report rpt, Draw2.Graphics g, string t, Style a, System.Drawing.Rectangle rect)
        {

            if (t == null)
                return;

            Row r = FirstChartRow(rpt);

            if (a != null)
            {
                await a.DrawString(rpt, g, t, t.GetTypeCode(), r, rect);
                await a.DrawBorder(rpt, g, r, rect);
            }
            else
                Style.DrawStringDefaults(g, t, rect);
            return;
        }

        //15052008WRP - Measure category title size
        protected async Task<System.Drawing.Size> DrawCategoryTitleMeasure(Report rpt, Draw2.Graphics g, string t, Style a)
        {
            System.Drawing.Size size = System.Drawing.Size.Empty;
            Row r = FirstChartRow(rpt);

            if (t == null || t == "")
                return size;

            if (a != null)
                size = await a.MeasureString(rpt, g, t, t.GetTypeCode(), r, int.MaxValue);
            else
                size = await Style.MeasureStringDefaults(rpt, g, t, t.GetTypeCode(), r, int.MaxValue);

            return size;

        }

        protected async Task<(object value, TypeCode tc)> GetCategoryValue(Report rpt, int row)
        {
            MatrixCellEntry mce = _DataDefn[row, 0];
            TypeCode tc;
            if (mce == null)
            {
                tc = TypeCode.String;
                return ("", tc);                  // Not sure what this really means TODO:
            }

            Row lrow;
            this._ChartDefn.ChartMatrix.SetMyData(rpt, mce.Data);       // Must set this for evaluation
            if (mce.Data.Data.Count > 0)
                lrow = mce.Data.Data[0];
            else
                lrow = null;
            ChartExpression ce = (ChartExpression)(mce.DisplayItem);

            object v = await ce.Value.Evaluate(rpt, lrow);
            tc = ce.Value.GetTypeCode();
            return (v, tc);
        }

        protected async Task<double> GetDataValue(Report rpt, int row, int col)
        {
            return await GetDataValue(rpt, row, col, 0);
        }

        /* Added this function to return the plot type
		 * 05122007AJM */
        protected string GetPlotType(Report rpt, int row, int col)
        {
            try
            {
                if (this is ChartColumn || this is ChartBubble)
                {
                    return ((ChartExpression)_DataDefn[col, row].DisplayItem).PlotType.Source;
                }
            }
            catch //(Exception e)
            {
                //ignore 
            }
            return "Auto";
        }
        // 20022008 AJM GJL
        protected string GetYAxis(Report rpt, int row, int col)
        {
            try
            {
                if (this is ChartColumn)
                {
                    return ((ChartExpression)_DataDefn[col, row].DisplayItem).YAxis.Source;
                }
            }
            catch //(Exception e)
            {
                //ignore 
            }
            return "Left";
        }

        protected bool getNoMarkerVal(Report rpt, int row, int col)
        {
            try
            {
                return Boolean.Parse(((ChartExpression)_DataDefn[col, row].DisplayItem).NoMarker.Source);

            }
            catch
            {
            }
            return false;
        }


        protected String getLineSize(Report rpt, int row, int col)
        {
            try
            {
                return ((ChartExpression)_DataDefn[col, row].DisplayItem).LineSize.Source;

            }
            catch
            {
            }
            return "Regular";
        }

        protected String getColour(Report rpt, int row, int col)
        {
            try
            {
                return ((ChartExpression)_DataDefn[row, col].DisplayItem).Colour.Source;
            }
            catch
            {
            }
            return "";
        }
        protected async Task<double> GetDataValue(Report rpt, int row, int col, int xyb)
        {
            MatrixCellEntry mce = _DataDefn[row, col];
            if (mce == null)
                return 0;					// Not sure what this really means TODO:
            if (mce.Value != double.MinValue && xyb == 0)
                return mce.Value;

            // Calculate this value; usually a fairly expensive operation
            //   due to the common use of aggregate values.  We need to
            //   go thru the data more than once if we have to auto scale.
            Row lrow;
            this._ChartDefn.ChartMatrix.SetMyData(rpt, mce.Data);		// Must set this for evaluation
            if (mce.Data.Data.Count > 0)
                lrow = mce.Data.Data[0];
            else
                lrow = null;
            ChartExpression ce = (ChartExpression)(mce.DisplayItem);

            double v = double.MinValue;
            if (xyb == 0)
            {
                v = await ce.Value.EvaluateDouble(rpt, lrow);
                mce.Value = v;					// cache so we don't need to calculate again
            }
            else if (xyb == 1)
                v = await ce.Value2.EvaluateDouble(rpt, lrow);
            else if (xyb == 2)
                v = await ce.Value3.EvaluateDouble(rpt, lrow);

            return v;
        }
        protected async Task<string> GetDataValueString(Report rpt, int row, int col)
        {
            MatrixCellEntry mce = _DataDefn[row, col];
            if (mce == null)
                return null;					// Not sure what this really means TODO:

            // Calculate this value; usually a fairly expensive operation
            //   due to the common use of aggregate values.  We need to
            //   go thru the data more than once if we have to auto scale.
            Row lrow;
            this._ChartDefn.ChartMatrix.SetMyData(rpt, mce.Data);		// Must set this for evaluation
            if (mce.Data.Data.Count > 0)
                lrow = mce.Data.Data[0];
            else
                lrow = null;
            ChartExpression ce = (ChartExpression)(mce.DisplayItem);

            string v = await ce.Value.EvaluateString(rpt, lrow);
            return v;
        }

        protected async Task<Draw2.Brush> GetSeriesBrush(Report rpt, int row, int col)
        {
            Draw2.Brush br = (await SeriesBrush(rpt, _row, ChartDefn.OwnerReport))[col - 1];            // this will be the default brush

            //  obtain the rows we're acting upon
            MatrixCellEntry mce = _DataDefn[row, col];
            if (mce == null)
                return br;

            ChartExpression ce = (ChartExpression)(mce.DisplayItem);
            if (ce.DP == null || ce.DP.Style == null || ce.DP.Style.BackgroundColor == null)
                return br;

            this._ChartDefn.ChartMatrix.SetMyData(rpt, mce.Data);		// Must set this for evaluation
            Row lrow = (mce.Data.Data.Count > 0) ? mce.Data.Data[0] : null;

            Style s = ce.DP.Style;
            string sc = await s.BackgroundColor.EvaluateString(rpt, lrow);

            System.Drawing.Color rc = XmlUtil.ColorFromHtml(sc, System.Drawing.Color.Empty, rpt);
            if (rc != System.Drawing.Color.Empty)
                br = new Draw2.SolidBrush(rc);

            return br;
        }

        protected async Task DrawDataPoint(Report rpt, Draw2.Graphics g, System.Drawing.Point p, int row, int col)
        {
            await DrawDataPoint(rpt, g, p, System.Drawing.Rectangle.Empty, row, col);
        }

        protected async Task DrawDataPoint(Report rpt, Draw2.Graphics g, System.Drawing.Rectangle rect, int row, int col)
        {
            await DrawDataPoint(rpt, g, System.Drawing.Point.Empty, rect, row, col);
        }

        async Task DrawDataPoint(Report rpt, Draw2.Graphics g, System.Drawing.Point p, System.Drawing.Rectangle rect, int row, int col)
        {
            MatrixCellEntry mce = _DataDefn[row, col];
            if (mce == null)
                return;                 // Not sure what this really means TODO:

            ChartExpression ce = (ChartExpression)(mce.DisplayItem);
            DataPoint dp = ce.DP;

            if (dp.DataLabel == null || !dp.DataLabel.Visible)
                return;

            // Calculate the DataPoint value; usually a fairly expensive operation
            //   due to the common use of aggregate values.  
            Row lrow;
            this._ChartDefn.ChartMatrix.SetMyData(rpt, mce.Data);       // Must set this for evaluation
            if (mce.Data.Data.Count > 0)
                lrow = mce.Data.Data[0];
            else
                lrow = null;

            object v = null;
            TypeCode tc;
            if (dp.DataLabel.Value == null)
            {       // No DataLabel value specified so we use the actual value
                v = ce.Value.EvaluateDouble(rpt, lrow);
                tc = TypeCode.Double;
            }
            else
            {       // Evaluate the DataLable value for the display
                v = await dp.DataLabel.Value.Evaluate(rpt, lrow);
                tc = dp.DataLabel.Value.GetTypeCode();
            }

            if (dp.DataLabel.Style == null)
            {
                if (rect == System.Drawing.Rectangle.Empty)
                {
                    System.Drawing.Size size = await Style.MeasureStringDefaults(rpt, g, v, tc, lrow, int.MaxValue);
                    rect = new System.Drawing.Rectangle(p, size);
                }
                Style.DrawStringDefaults(g, v, rect);
            }
            else
            {
                if (rect == System.Drawing.Rectangle.Empty)
                {
                    System.Drawing.Size size = await dp.DataLabel.Style.MeasureString(rpt, g, v, tc, lrow, int.MaxValue);
                    rect = new System.Drawing.Rectangle(p, size);
                }
                await dp.DataLabel.Style.DrawString(rpt, g, v, tc, lrow, rect);
            }

            return;
        }

        protected async Task<string> GetSeriesValue(Report rpt, int iCol)
        {
            MatrixCellEntry mce = _DataDefn[0, iCol];
            Row lrow;
            if (mce.Data.Data.Count > 0)
                lrow = mce.Data.Data[0];
            else
                lrow = null;
            ChartExpression ce = (ChartExpression)(mce.DisplayItem);

            string v = ce.ChartLabel == null ?
                await ce.Value.EvaluateString(rpt, lrow) :
                await ce.ChartLabel.EvaluateString(rpt, lrow);

            return v;
        }

        // 20022008 AJM GJL - Should the Second Y axis be shown?
        protected bool ShowRightYAxis(Report rpt)
        {
            for (int iRow = 1; iRow <= CategoryCount; iRow++)
            {
                for (int iCol = 1; iCol <= SeriesCount; iCol++)
                {
                    if (GetYAxis(rpt, iCol, 1).ToUpper() == "RIGHT")
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        protected async Task<(double max, double min)> GetMaxMinDataValue(Report rpt, int xyb, int WhichYAxis)
        {
            if ((ChartSubTypeEnum)Enum.Parse(typeof(ChartSubTypeEnum), await _ChartDefn.Subtype.EvaluateString(rpt, _row)) == ChartSubTypeEnum.Stacked)
            {
                var result = await GetMaxMinDataValueStacked(rpt);
                return (result.max, result.min);
            }
            double min = double.MaxValue;
            double max = double.MinValue;

            double v = 0;// 20022008 AJM GJL
            for (int iRow = 1; iRow <= CategoryCount; iRow++)
            {
                for (int iCol = 1; iCol <= SeriesCount; iCol++)
                {
                    // 20022008 AJM GJL
                    if (WhichYAxis == 2)
                    {
                        if (GetYAxis(rpt, iCol, 1).ToUpper() == "RIGHT")
                        {
                            v = await GetDataValue(rpt, iRow, iCol, xyb);
                            if (v < min)
                                min = v;
                            if (v > max)
                                max = v;

                        }
                    }
                    else
                    {
                        if (GetYAxis(rpt, iCol, 1).ToUpper() != "RIGHT")
                        {
                            v = await GetDataValue(rpt, iRow, iCol, xyb);
                            if (v < min)
                                min = v;
                            if (v > max)
                                max = v;

                        }
                    }
                }
            }
            return (max, min);
        }

        async Task<(double max, double min)> GetMaxMinDataValueStacked(Report rpt)
        {
            double min = double.MaxValue;
            double max = double.MinValue;

            double v;
            for (int iRow = 1; iRow <= CategoryCount; iRow++)
            {
                v = 0;
                for (int iCol = 1; iCol <= SeriesCount; iCol++)
                {
                    v += await GetDataValue(rpt, iRow, iCol);
                }
                if (v < min)
                    min = v;
                if (v > max)
                    max = v;
            }

            return (max, min);
        }

        protected async Task<Draw2.Brush[]> GetSeriesBrushes(Report rpt, Row row, ReportDefn defn)
        {
            Draw2.Brush[] b = new Draw2.Brush[SeriesCount];

            for (int i = 0; i < SeriesCount; i++)
            {
                // TODO: In general all the palettes could use a good going over
                //   both in terms of the colors in the lists and their order
                switch (ChartPalette.GetStyle(await ChartDefn.Palette.EvaluateString(rpt, row), defn.rl))
                {
                    case ChartPaletteEnum.Default:
                        b[i] = GetSeriesBrushesExcel(i); break;
                    case ChartPaletteEnum.EarthTones:
                        b[i] = GetSeriesBrushesEarthTones(i); break;
                    case ChartPaletteEnum.Excel:
                        b[i] = GetSeriesBrushesExcel(i); break;
                    case ChartPaletteEnum.GrayScale:
                        b[i] = GetSeriesBrushesGrayScale(i); break;
                    case ChartPaletteEnum.Light:
                        b[i] = GetSeriesBrushesLight(i); break;
                    case ChartPaletteEnum.Pastel:
                        b[i] = GetSeriesBrushesPastel(i); break;
                    case ChartPaletteEnum.SemiTransparent:
                        b[i] = GetSeriesBrushesExcel(i); break; // TODO
                                                                // 20022008 AJM GJL - New black & white printer friendly palette (NOT TO RDL SPEC BUT REQUIRED!)
                    case ChartPaletteEnum.Patterned:
                        b[i] = GetSeriesBrushesPatterned(i); break;
                    case ChartPaletteEnum.PatternedBlack:
                        b[i] = GetSeriesBrushesPatternedBlack(i); break;
                    case ChartPaletteEnum.Custom:

                        b[i] = new Draw2.SolidBrush(System.Drawing.Color.FromName(getColour(rpt, 1, i + 1))); break;
                    default:
                        b[i] = GetSeriesBrushesExcel(i); break;
                }
            }

            return b;
        }

        Draw2.Brush GetSeriesBrushesEarthTones(int i)
        {
            switch (i % 22)
            {
                case 0: return Draw2.Brushes.Maroon;
                case 1: return Draw2.Brushes.Brown;
                case 2: return Draw2.Brushes.Chocolate;
                case 3: return Draw2.Brushes.IndianRed;
                case 4: return Draw2.Brushes.Peru;
                case 5: return Draw2.Brushes.BurlyWood;
                case 6: return Draw2.Brushes.AntiqueWhite;
                case 7: return Draw2.Brushes.FloralWhite;
                case 8: return Draw2.Brushes.Ivory;
                case 9: return Draw2.Brushes.LightCoral;
                case 10: return Draw2.Brushes.DarkSalmon;
                case 11: return Draw2.Brushes.LightSalmon;
                case 12: return Draw2.Brushes.PeachPuff;
                case 13: return Draw2.Brushes.NavajoWhite;
                case 14: return Draw2.Brushes.Moccasin;
                case 15: return Draw2.Brushes.PapayaWhip;
                case 16: return Draw2.Brushes.Goldenrod;
                case 17: return Draw2.Brushes.DarkGoldenrod;
                case 18: return Draw2.Brushes.DarkKhaki;
                case 19: return Draw2.Brushes.Khaki;
                case 20: return Draw2.Brushes.Beige;
                case 21: return Draw2.Brushes.Cornsilk;
                default: return Draw2.Brushes.Brown;
            }
        }

        Draw2.Brush GetSeriesBrushesExcel(int i)
        {
            switch (i % 11)             // Just a guess at what these might actually be
            {
                //Gil's Excel 080208 - from excel 2007
                case 0: return Draw2.Brushes.Blue;
                case 1: return Draw2.Brushes.Red;
                case 2: return Draw2.Brushes.Green;
                case 3: return Draw2.Brushes.Purple;
                case 4: return Draw2.Brushes.DeepSkyBlue;
                case 5: return Draw2.Brushes.Orange;
                case 6: return Draw2.Brushes.Magenta;
                case 7: return Draw2.Brushes.Gold;
                case 8: return Draw2.Brushes.Lime;
                case 9: return Draw2.Brushes.Teal;
                case 10: return Draw2.Brushes.Pink;
                default: return Draw2.Brushes.Blue;
            }
        }
        // 20022008 AJM GJL
        Draw2.Brush GetSeriesBrushesPatterned(int i)
        {
            Drawing2D.HatchBrush PatternBrush;
            switch (i % 10)
            {
                case 0:
                    PatternBrush = new Drawing2D.HatchBrush(Drawing2D.HatchStyle.LargeConfetti, System.Drawing.Color.Blue, System.Drawing.Color.White);
                    break;
                case 1:
                    PatternBrush = new Drawing2D.HatchBrush(Drawing2D.HatchStyle.Cross, System.Drawing.Color.Red, System.Drawing.Color.White); // was weave... but I Especially didn't want to draw that in PDF - GJL
                    break;
                case 2:
                    PatternBrush = new Drawing2D.HatchBrush(Drawing2D.HatchStyle.DarkDownwardDiagonal, System.Drawing.Color.Green, System.Drawing.Color.White);
                    break;
                case 3:
                    PatternBrush = new Drawing2D.HatchBrush(Drawing2D.HatchStyle.OutlinedDiamond, System.Drawing.Color.Purple, System.Drawing.Color.White);
                    break;
                case 4:
                    PatternBrush = new Drawing2D.HatchBrush(Drawing2D.HatchStyle.DarkHorizontal, System.Drawing.Color.DeepSkyBlue, System.Drawing.Color.White);
                    break;
                case 5:
                    PatternBrush = new Drawing2D.HatchBrush(Drawing2D.HatchStyle.SmallConfetti, System.Drawing.Color.Orange, System.Drawing.Color.White);
                    break;
                case 6:
                    PatternBrush = new Drawing2D.HatchBrush(Drawing2D.HatchStyle.HorizontalBrick, System.Drawing.Color.Magenta, System.Drawing.Color.White);
                    break;
                case 7:
                    PatternBrush = new Drawing2D.HatchBrush(Drawing2D.HatchStyle.LargeCheckerBoard, System.Drawing.Color.Gold, System.Drawing.Color.White); // was wave... but I didn't want to draw that in PDF - GJL
                    break;
                case 8:
                    PatternBrush = new Drawing2D.HatchBrush(Drawing2D.HatchStyle.Vertical, System.Drawing.Color.Lime, System.Drawing.Color.White);
                    break;
                case 9:
                    PatternBrush = new Drawing2D.HatchBrush(Drawing2D.HatchStyle.SolidDiamond, System.Drawing.Color.Teal, System.Drawing.Color.White);
                    break;
                case 10:
                    PatternBrush = new Drawing2D.HatchBrush(Drawing2D.HatchStyle.DiagonalBrick, System.Drawing.Color.Pink, System.Drawing.Color.White);
                    break;
                default:
                    PatternBrush = new Drawing2D.HatchBrush(Drawing2D.HatchStyle.BackwardDiagonal, System.Drawing.Color.Blue, System.Drawing.Color.White);
                    break;

            }
            return PatternBrush;
        }


        Draw2.Brush GetSeriesBrushesPatternedBlack(int i)
        {
            Drawing2D.HatchBrush PatternBrush;
            switch (i % 10)
            {
                case 0:
                    PatternBrush = new Drawing2D.HatchBrush(Drawing2D.HatchStyle.LargeConfetti, System.Drawing.Color.Black, System.Drawing.Color.White);
                    break;
                case 1:
                    PatternBrush = new Drawing2D.HatchBrush(Drawing2D.HatchStyle.Weave, System.Drawing.Color.Black, System.Drawing.Color.White);
                    break;
                case 2:
                    PatternBrush = new Drawing2D.HatchBrush(Drawing2D.HatchStyle.DarkDownwardDiagonal, System.Drawing.Color.Black, System.Drawing.Color.White);
                    break;
                case 3:
                    PatternBrush = new Drawing2D.HatchBrush(Drawing2D.HatchStyle.OutlinedDiamond, System.Drawing.Color.Black, System.Drawing.Color.White);
                    break;
                case 4:
                    PatternBrush = new Drawing2D.HatchBrush(Drawing2D.HatchStyle.DarkHorizontal, System.Drawing.Color.Black, System.Drawing.Color.White);
                    break;
                case 5:
                    PatternBrush = new Drawing2D.HatchBrush(Drawing2D.HatchStyle.SmallConfetti, System.Drawing.Color.Black, System.Drawing.Color.White);
                    break;
                case 6:
                    PatternBrush = new Drawing2D.HatchBrush(Drawing2D.HatchStyle.HorizontalBrick, System.Drawing.Color.Black, System.Drawing.Color.White);
                    break;
                case 7:
                    PatternBrush = new Drawing2D.HatchBrush(Drawing2D.HatchStyle.Wave, System.Drawing.Color.Black, System.Drawing.Color.White);
                    break;
                case 8:
                    PatternBrush = new Drawing2D.HatchBrush(Drawing2D.HatchStyle.Vertical, System.Drawing.Color.Black, System.Drawing.Color.White);
                    break;
                case 9:
                    PatternBrush = new Drawing2D.HatchBrush(Drawing2D.HatchStyle.SolidDiamond, System.Drawing.Color.Black, System.Drawing.Color.White);
                    break;
                case 10:
                    PatternBrush = new Drawing2D.HatchBrush(Drawing2D.HatchStyle.DiagonalBrick, System.Drawing.Color.Black, System.Drawing.Color.White);
                    break;
                default:
                    PatternBrush = new Drawing2D.HatchBrush(Drawing2D.HatchStyle.BackwardDiagonal, System.Drawing.Color.Black, System.Drawing.Color.White);
                    break;

            }
            return PatternBrush;
        }

        Draw2.Brush GetSeriesBrushesGrayScale(int i)
        {
            switch (i % 10)
            {
                case 0: return Draw2.Brushes.Gray;
                case 1: return Draw2.Brushes.SlateGray;
                case 2: return Draw2.Brushes.DarkGray;
                case 3: return Draw2.Brushes.LightGray;
                case 4: return Draw2.Brushes.DarkSlateGray;
                case 5: return Draw2.Brushes.DimGray;
                case 6: return Draw2.Brushes.LightSlateGray;
                case 7: return Draw2.Brushes.Black;
                case 8: return Draw2.Brushes.White;
                case 9: return Draw2.Brushes.Gainsboro;
                default: return Draw2.Brushes.Gray;
            }
        }

        Draw2.Brush GetSeriesBrushesLight(int i)
        {
            switch (i % 13)
            {
                case 0: return Draw2.Brushes.LightBlue;
                case 1: return Draw2.Brushes.LightCoral;
                case 2: return Draw2.Brushes.LightCyan;
                case 3: return Draw2.Brushes.LightGoldenrodYellow;
                case 4: return Draw2.Brushes.LightGray;
                case 5: return Draw2.Brushes.LightGreen;
                case 6: return Draw2.Brushes.LightPink;
                case 7: return Draw2.Brushes.LightSalmon;
                case 8: return Draw2.Brushes.LightSeaGreen;
                case 9: return Draw2.Brushes.LightSkyBlue;
                case 10: return Draw2.Brushes.LightSlateGray;
                case 11: return Draw2.Brushes.LightSteelBlue;
                case 12: return Draw2.Brushes.LightYellow;
                default: return Draw2.Brushes.LightBlue;
            }
        }

        Draw2.Brush GetSeriesBrushesPastel(int i)
        {
            switch (i % 26)
            {
                case 0: return Draw2.Brushes.CadetBlue;
                case 1: return Draw2.Brushes.MediumTurquoise;
                case 2: return Draw2.Brushes.Aquamarine;
                case 3: return Draw2.Brushes.LightCyan;
                case 4: return Draw2.Brushes.Azure;
                case 5: return Draw2.Brushes.AliceBlue;
                case 6: return Draw2.Brushes.MintCream;
                case 7: return Draw2.Brushes.DarkSeaGreen;
                case 8: return Draw2.Brushes.PaleGreen;
                case 9: return Draw2.Brushes.LightGreen;
                case 10: return Draw2.Brushes.MediumPurple;
                case 11: return Draw2.Brushes.CornflowerBlue;
                case 12: return Draw2.Brushes.Lavender;
                case 13: return Draw2.Brushes.GhostWhite;
                case 14: return Draw2.Brushes.PaleGoldenrod;
                case 15: return Draw2.Brushes.LightGoldenrodYellow;
                case 16: return Draw2.Brushes.LemonChiffon;
                case 17: return Draw2.Brushes.LightYellow;
                case 18: return Draw2.Brushes.Orchid;
                case 19: return Draw2.Brushes.Plum;
                case 20: return Draw2.Brushes.LightPink;
                case 21: return Draw2.Brushes.Pink;
                case 22: return Draw2.Brushes.LavenderBlush;
                case 23: return Draw2.Brushes.Linen;
                case 24: return Draw2.Brushes.PaleTurquoise;
                case 25: return Draw2.Brushes.OldLace;
                default: return Draw2.Brushes.CadetBlue;
            }
        }

        protected ChartMarkerEnum[] GetSeriesMarkers()
        {
            ChartMarkerEnum[] m = new ChartMarkerEnum[SeriesCount];

            for (int i = 0; i < SeriesCount; i++)
            {
                m[i] = (ChartMarkerEnum)(i % (int)ChartMarkerEnum.Count);
            }

            return m;
        }

        protected async Task<(double max, double min)> GetValueMaxMin(Report rpt,  double max,  double min, int xyb, int WhichYAxis)// 20022008 AJM GJL
        {

            if ((ChartSubTypeEnum)Enum.Parse(typeof(ChartSubTypeEnum), await _ChartDefn.Subtype.EvaluateString(rpt, _row)) == ChartSubTypeEnum.PercentStacked)
            {   // Percent stacked is easy; and overrides user provided values
                max = 1;
                min = 0;
                return (max, min);
            }
            Axis ax;
            if (_ChartDefn.Type == ChartTypeEnum.Bubble ||
                _ChartDefn.Type == ChartTypeEnum.Scatter)
            {
                // Axis actually depends on the xyb parameter : 0 is X axis: 1 is Y Axis; 
                if (xyb == 0)
                    ax = _ChartDefn.CategoryAxis != null ? _ChartDefn.CategoryAxis.Axis : null;
                else if (xyb == 1)
                    ax = _ChartDefn.ValueAxis != null ? _ChartDefn.ValueAxis.Axis : null;
                else
                    ax = null;
            }
            else
            {
                ax = _ChartDefn.ValueAxis != null ? _ChartDefn.ValueAxis.Axis : null;
            }


            double vAxisMax;
            double vAxisMin;
            if (ax != null)
            {
                vAxisMax = await ax.MaxEval(rpt, _row);
                vAxisMin = await ax.MinEval(rpt, _row);
            }
            else
            {
                vAxisMax = vAxisMin = int.MinValue;
            }

            // Check for case where both min and max are provided
            if (vAxisMax != int.MinValue && !vAxisMax.Equals(double.NaN) &&
                vAxisMin != int.MinValue && !vAxisMin.Equals(double.NaN))
            {
                max = vAxisMax;
                min = vAxisMin;
                return (max, min);
            }

            // OK We have to work for it;  Calculate min/max of data
            (max, min) = await GetMaxMinDataValue(rpt, xyb, 1);  // 20022008 AJM GJL

            if (vAxisMax != int.MinValue && !vAxisMax.Equals(double.NaN))
                max = vAxisMax;
            else
            {
                //
                //int gridIncrs=10;		// assume 10 grid increments for now
                _gridIncrs = 10; //PJR 20071113 - grid incrs set & adjusted in here now

                double incr = max / _gridIncrs; // should be range between max and min?
                double log = Math.Floor(Math.Log10(Math.Abs(incr)));


                double logPow = Math.Pow(10, log) * Math.Sign(max);
                double logDig = (int)((incr / logPow) + .5);

                // promote the MSD to either 1, 2, or 5
                if (logDig > 5.0)
                    logDig = 10.0;
                else if (logDig > 2.0)
                    logDig = 5.0;
                else if (logDig > 1.0)
                    logDig = 2.0;
                //PJR 20071113 - reduce scale for large overscale options by decreasing _gridIncrs
                while (max < logDig * logPow * _gridIncrs)
                {
                    _gridIncrs--;
                }
                //_gridIncrs++;

                //PJR 20071113 - expand scale so that it is able to fit the max value by increasing _gridIncrs
                while (max > logDig * logPow * _gridIncrs)
                {
                    _gridIncrs++;
                }
                // 20022008 AJM GJL
                double tmpMax = max;
                // 04032008 AJM - Fixing Strange Number Choice with small numbers
                max = /*(int)*/ (logDig * logPow * _gridIncrs /*+ 0.5*/);

                if (tmpMax > max - ((max / _gridIncrs) * .5))
                {

                    max += (max / _gridIncrs);
                    _gridIncrs++;
                }
            }

            if (vAxisMin != int.MinValue && !vAxisMin.Equals(double.NaN))
                min = vAxisMin;
            else if (min > 0)
                min = 0;
            else
            {
                min = Math.Floor(min);
            }

            return (max, min);
        }

        protected void AdjustMargins(System.Drawing.Rectangle legendRect, Report rpt, Draw2.Graphics g)
        {
            // //110208AJM GJL Making room for second y axis        

            // if (ShowRightYAxis(rpt) && !(IsLegendRight()))
            //{              
            //     Layout.RightMargin = (int)(Layout.LeftMargin * 1.5);
            //}


            // Adjust the margins based on the legend
            if (!IsLegendInsidePlotArea())  // When inside plot area we don't adjust plot margins
            {
                if (IsLegendLeft())
                    Layout.LeftMargin += legendRect.Width;
                else if (IsLegendRight())
                    Layout.RightMargin += legendRect.Width;
                if (IsLegendTop())
                    Layout.TopMargin += legendRect.Height;
                else if (IsLegendBottom())
                    Layout.BottomMargin += legendRect.Height;
            }
            // Force some margins; if any are too small
            int min = new RSize(ChartDefn.OwnerReport, ".2 in").PixelsX;

            if (Layout.RightMargin < min + (this._LastCategoryWidth / 2))
                Layout.RightMargin = min + (this._LastCategoryWidth / 2);
            if (Layout.LeftMargin < min)
                Layout.LeftMargin = min;
            if (Layout.TopMargin < min)
                Layout.TopMargin = min;
            if (Layout.BottomMargin < min)
                Layout.BottomMargin = min;
        }

        protected bool IsLegendLeft()
        {
            Legend l = _ChartDefn.Legend;
            if (l == null || !l.Visible)
                return false;

            bool rc;
            switch (l.Position)
            {
                case LegendPositionEnum.BottomLeft:
                case LegendPositionEnum.LeftBottom:
                case LegendPositionEnum.LeftCenter:
                case LegendPositionEnum.LeftTop:
                case LegendPositionEnum.TopLeft:
                    rc = true;
                    break;
                default:
                    rc = false;
                    break;
            }

            return rc;
        }

        protected async Task<(double incr, int interval)> SetIncrementAndInterval(Report rpt, Axis a, double min, double max)
        {
            int interval = _gridIncrs; //PJR 20071113 - gridincrements set by Max value now                 // assume an interval count of 10 to start
            double incr;

            if (a.MajorInterval != null)
            {
                incr = await a.MajorInterval.EvaluateDouble(rpt, this.ChartRow);
                if (incr.CompareTo(double.MinValue) == 0)
                    incr = (max - min) / interval;
                else
                {
                    interval = (int)((int)(Math.Abs(max - min) / incr));
                }
            }
            else
                incr = (max - min) / interval;

            return (incr, interval);
        }

        protected bool IsLegendInsidePlotArea()
        {
            Legend l = _ChartDefn.Legend;
            if (l == null || !l.Visible)
                return false;               // doesn't really matter
            else
                return l.InsidePlotArea;
        }

        protected bool IsLegendRight()
        {
            Legend l = _ChartDefn.Legend;
            if (l == null || !l.Visible)
                return false;

            bool rc;
            switch (l.Position)
            {
                case LegendPositionEnum.BottomRight:
                case LegendPositionEnum.RightBottom:
                case LegendPositionEnum.RightCenter:
                case LegendPositionEnum.TopRight:
                case LegendPositionEnum.RightTop:
                    rc = true;
                    break;
                default:
                    rc = false;
                    break;
            }
            return rc;
        }

        protected bool IsLegendTop()
        {
            Legend l = _ChartDefn.Legend;
            if (l == null || !l.Visible)
                return false;

            bool rc;
            switch (l.Position)
            {
                case LegendPositionEnum.LeftTop:
                case LegendPositionEnum.TopLeft:
                case LegendPositionEnum.TopCenter:
                case LegendPositionEnum.TopRight:
                case LegendPositionEnum.RightTop:
                    rc = true;
                    break;
                default:
                    rc = false;
                    break;
            }
            return rc;
        }

        protected bool IsLegendBottom()
        {
            Legend l = _ChartDefn.Legend;
            if (l == null || !l.Visible)
                return false;

            bool rc;
            switch (l.Position)
            {
                case LegendPositionEnum.BottomCenter:
                case LegendPositionEnum.BottomLeft:
                case LegendPositionEnum.LeftBottom:
                case LegendPositionEnum.BottomRight:
                case LegendPositionEnum.RightBottom:
                    rc = true;
                    break;
                default:
                    rc = false;
                    break;
            }
            return rc;
        }

        private Row FirstChartRow(Report rpt)
        {
            Rows _Data = _ChartDefn.ChartMatrix.GetMyData(rpt);
            if (_Data != null &&
                _Data.Data.Count > 0)
                return _Data.Data[0];
            else
                return null;

        }
        #region IDisposable Members

        public void Dispose()
        {
            if (_bm != null)
                _bm.Dispose();
        }

        #endregion
    }
}
