


using System;
using System.Collections;
using System.Collections.Generic;

#if DRAWINGCOMPAT
using Draw2 = Majorsilence.Forms.Drawing;
using Imaging = Majorsilence.Forms.Drawing.Imaging;
#else
using Draw2 = System.Drawing;
using Imaging = System.Drawing.Imaging;
#endif
using System.Text;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;


namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Column chart definition and processing
	///</summary>
	internal class ChartMap: ChartBase
	{

        internal ChartMap(Report r, Row row, Chart c, MatrixCellEntry[,] m, Expression showTooltips, Expression showTooltipsX, Expression _ToolTipYFormat, Expression _ToolTipXFormat)
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
					_mf = new Imaging.Metafile(_aStream, HDC, new System.Drawing.RectangleF(0, 0, _bm.Width, _bm.Height),
						Imaging.MetafileFrameUnit.Pixel);
					g1.ReleaseHdc(HDC);
				}
			}

			using(Draw2.Graphics g = Draw2.Graphics.FromImage(_mf != null ? _mf : _bm))
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

				double max=0,min=0; // Get the max and min values
                                    //		GetValueMaxMin(rpt, ref max, ref min,0, 1);

                await DrawChartStyle(rpt, g);

                // Draw title; routine determines if necessary
                await DrawTitle(rpt, g, ChartDefn.Title, new System.Drawing.Rectangle(0, 0, _bm.Width, Layout.TopMargin));

				Layout.LeftMargin = 0;
                Layout.RightMargin = 0;

				// Draw legend
				System.Drawing.Rectangle lRect = await DrawLegend(rpt, g, false, true);

				Layout.BottomMargin = 0;

				AdjustMargins(lRect,rpt, g);        // Adjust margins based on legend.

                // Draw Plot area
                await DrawPlotAreaStyle(rpt, g, lRect);

                string subtype = await _ChartDefn.Subtype.EvaluateString(rpt, _row);

                await DrawMap(rpt, g, subtype, max, min);

                await DrawLegend(rpt, g, false, false);

			}
		}

        private async Task DrawMap(Report rpt, Draw2.Graphics g, string mapfile, double max, double min)
        {
            string file = XmlUtil.XmlFileExists(mapfile);

            MapData mp;
            if (file != null)
                mp = MapData.Create(file);
            else
            {
                rpt.rl.LogError(4, string.Format("Map Subtype file {0} not found.", mapfile));                
                mp = new MapData();         // we'll at least put up something; but it won't be right
            }
            float scale = mp.GetScale(Layout.PlotArea.Width, Layout.PlotArea.Height);

            for (int iRow = 1; iRow <= CategoryCount; iRow++)
            {
                for (int iCol = 1; iCol <= SeriesCount; iCol++)
                {
                    string sv = await GetSeriesValue(rpt, iCol);

                    string c = await this.GetDataValueString(rpt, iRow, iCol);
                    List<MapPolygon> pl = mp.GetPolygon(sv);
                    if (pl == null)
                        continue;
                    Draw2.Brush br = new Draw2.SolidBrush(XmlUtil.ColorFromHtml(c, System.Drawing.Color.Transparent));
                    foreach (MapPolygon mpoly in pl)
                    {
	                    System.Drawing.PointF[] polygon = mpoly.Polygon;
	                    System.Drawing.PointF[] drawpoly = new System.Drawing.PointF[polygon.Length];
                        // make points relative to plotarea --- need to scale this as well
                        for (int ip = 0; ip < drawpoly.Length; ip++)
                        {
                            drawpoly[ip] = new System.Drawing.PointF(Layout.PlotArea.X + (polygon[ip].X * scale), Layout.PlotArea.Y + (polygon[ip].Y * scale));
                        }
                        g.FillPolygon(br, drawpoly);
                        if (_showToolTips)
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append("PolyToolTip:");
                            sb.Append(sv.Replace('|', '/'));        // we treat '|' as a separator character; don't allow in string
                            sb.Append(' ');
                            sb.Append(c.Replace('|', '/'));
                            foreach (System.Drawing.PointF pf in drawpoly)
                                sb.AppendFormat(NumberFormatInfo.InvariantInfo, "|{0}|{1}", pf.X, pf.Y);
                            g.AddMetafileComment(new System.Text.ASCIIEncoding().GetBytes(sb.ToString()));
                        }
                    }
                    br.Dispose();
                }
            }
            // draw the outline of the map
            foreach (MapObject mo in mp.MapObjects)
            {
                mo.Draw(g, scale, Layout.PlotArea.X, Layout.PlotArea.Y);
            }

        }
    }
}
