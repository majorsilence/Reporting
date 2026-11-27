using System;
using System.Threading.Tasks;

using Draw2 = Majorsilence.Forms.Drawing;


namespace Majorsilence.Reporting.Rdl
{
    ///<summary>
    /// Sunburst chart: concentric rings, one per category grouping, each slice's angle sized by its value.
    ///</summary>
    internal class ChartSunburst : ChartBase
    {
        internal ChartSunburst(Report r, Row row, Chart c, MatrixCellEntry[,] m, Expression showTooltips, Expression showTooltipsX, Expression _ToolTipYFormat, Expression _ToolTipXFormat)
            : base(r, row, c, m, showTooltips, showTooltipsX, _ToolTipYFormat, _ToolTipXFormat)
        {
        }

        override internal async Task Draw(Report rpt)
        {
            CreateSizedBitmap();

            using (Draw2.Graphics g = Draw2.Graphics.FromImage(_bm))
            {
                await DrawFrameWithoutAxes(rpt, g).ConfigureAwait(false);

                ChartHierarchy hierarchy = await GetHierarchyAsync(rpt).ConfigureAwait(false);
                await DrawPlotAreaSunburst(rpt, g, hierarchy).ConfigureAwait(false);

                await DrawLegend(rpt, g, false, false).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Draws the rings into Layout.PlotArea.
        /// </summary>
        Task DrawPlotAreaSunburst(Report rpt, Draw2.Graphics g, ChartHierarchy hierarchy)
        {
            // TODO #639 (Sunburst drawing follow-up): one ring per level (hierarchy.Depth rings), each
            // slice's angle proportional to its node's Value and every child inside its parent's
            // angle. Drawing from the outermost ring in, each with DrawPieSlice on a smaller square,
            // then filling the centre (empty or showing hierarchy.Total) gives the rings, as
            // ChartPie's doughnut does. Until then the plot area is left empty.
            return Task.CompletedTask;
        }
    }
}
