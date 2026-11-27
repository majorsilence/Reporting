using System;
using System.Threading.Tasks;

using Draw2 = Majorsilence.Forms.Drawing;


namespace Majorsilence.Reporting.Rdl
{
    ///<summary>
    /// TreeMap chart: nested rectangles sized by value, one level of nesting per category grouping.
    ///</summary>
    internal class ChartTreeMap : ChartBase
    {
        internal ChartTreeMap(Report r, Row row, Chart c, MatrixCellEntry[,] m, Expression showTooltips, Expression showTooltipsX, Expression _ToolTipYFormat, Expression _ToolTipXFormat)
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
                await DrawPlotAreaTreeMap(rpt, g, hierarchy).ConfigureAwait(false);

                await DrawLegend(rpt, g, false, false).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Draws the tree into Layout.PlotArea.
        /// </summary>
        Task DrawPlotAreaTreeMap(Report rpt, Draw2.Graphics g, ChartHierarchy hierarchy)
        {
            // TODO #639 (TreeMap drawing follow-up): a squarified layout (Bruls, Huizing and van Wijk)
            // of hierarchy.Root.Children in Layout.PlotArea, recursing into each parent rectangle;
            // inner levels draw a header strip with their label, leaves are coloured from the
            // palette by top-level category, labels only where they fit, and the tooltip carries
            // the full path and value. Until then the plot area is left empty.
            return Task.CompletedTask;
        }
    }
}
