using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

using Draw2 = Majorsilence.Forms.Drawing;


namespace Majorsilence.Reporting.Rdl
{
    ///<summary>
    /// One slice of a sunburst: a node of the hierarchy, the ring it sits on (0 for the top level,
    /// the innermost ring) and its angles in degrees, clockwise from the x axis as Graphics measures them.
    ///</summary>
    internal readonly struct SunburstSlice
    {
        internal SunburstSlice(ChartHierarchyNode node, int ring, double startAngle, double sweepAngle)
        {
            Node = node;
            Ring = ring;
            StartAngle = startAngle;
            SweepAngle = sweepAngle;
        }

        internal ChartHierarchyNode Node { get; }

        internal int Ring { get; }

        internal double StartAngle { get; }

        internal double SweepAngle { get; }

        internal double EndAngle => StartAngle + SweepAngle;
    }

    ///<summary>
    /// Sunburst chart: concentric rings, one per category grouping, each slice's angle sized by its value.
    ///</summary>
    internal class ChartSunburst : ChartBase
    {
        /// <summary>Where the first top-level slice starts: twelve o'clock, as SSRS draws it.</summary>
        internal const double FirstAngle = -90;

        // The arcs of a slice are drawn as polygons with a point at least every this many degrees.
        const double ArcStep = 2;

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
        /// The slices of every ring. A top-level category's angle is its share of the total; a
        /// node's children share their parent's angle by their values, in the hierarchy's order.
        /// Rings are full turns wherever every level reaches the leaves.
        /// </summary>
        internal static List<SunburstSlice> ComputeSunburstSlices(ChartHierarchy hierarchy, double firstAngle = FirstAngle)
        {
            var slices = new List<SunburstSlice>();
            if (hierarchy == null || !(hierarchy.Total > 0))
                return slices;
            AddSlices(slices, hierarchy.Root, firstAngle, 360);
            return slices;
        }

        static void AddSlices(List<SunburstSlice> slices, ChartHierarchyNode parent, double start, double sweep)
        {
            if (!(parent.Value > 0))
                return;
            double angle = start;
            foreach (ChartHierarchyNode child in parent.Children)
            {
                double childSweep = sweep * child.Value / parent.Value;
                slices.Add(new SunburstSlice(child, child.Depth - 1, angle, childSweep));
                AddSlices(slices, child, angle, childSweep);
                angle += childSweep;
            }
        }

        /// <summary>
        /// Draws the rings into Layout.PlotArea, in its largest centred square: one ring per level
        /// around an empty centre (the hole is as wide as a ring, so one level is a doughnut).
        /// Each slice is drawn as a true ring sector, so the hole keeps the plot area's own background.
        /// </summary>
        async Task DrawPlotAreaSunburst(Report rpt, Draw2.Graphics g, ChartHierarchy hierarchy)
        {
            List<SunburstSlice> slices = ComputeSunburstSlices(hierarchy);
            int side = Math.Min(Layout.PlotArea.Width, Layout.PlotArea.Height);
            if (slices.Count == 0 || side < 8)
                return;

            float cx = Layout.PlotArea.Left + Layout.PlotArea.Width / 2f;
            float cy = Layout.PlotArea.Top + Layout.PlotArea.Height / 2f;
            float radius = side / 2f - 1;
            float ringWidth = radius / (hierarchy.Depth + 1);

            // A colour per top-level category, shared by everything inside it.
            var topIndex = new Dictionary<ChartHierarchyNode, int>();
            for (int i = 0; i < hierarchy.Root.Children.Count; i++)
                topIndex[hierarchy.Root.Children[i]] = i;
            var topBrush = new Dictionary<int, Draw2.Brush>();

            var owned = new List<IDisposable>();
            try
            {
                using (var font = new Draw2.Font("Arial", 8))
                using (var format = new Draw2.StringFormat())
                {
                    format.Alignment = Draw2.StringAlignment.Center;
                    format.LineAlignment = Draw2.StringAlignment.Center;
                    format.FormatFlags |= Draw2.StringFormatFlags.NoWrap;

                    foreach (SunburstSlice slice in slices)
                    {
                        float inner = ringWidth * (slice.Ring + 1);
                        float outer = inner + ringWidth;

                        ChartHierarchyNode top = slice.Node;
                        while (top.Parent != null && top.Parent.Parent != null)
                            top = top.Parent;
                        int index = topIndex.TryGetValue(top, out int ti) ? ti : 0;
                        if (!topBrush.TryGetValue(index, out Draw2.Brush? baseBrush))
                        {
                            baseBrush = await GetPaletteBrush(rpt, ChartRow, ChartDefn.OwnerReport, index).ConfigureAwait(false);
                            topBrush[index] = baseBrush;
                        }
                        Draw2.Brush brush = ShadeForRing(baseBrush, slice.Ring, owned);

                        DrawRingSector(g, brush, cx, cy, inner, outer, slice);
                        DrawLabel(g, font, format, brush, cx, cy, inner, outer, slice);

                        if (_showToolTips)
                            AddToolTip(g, cx, cy, inner, outer, slice);
                    }
                }
            }
            finally
            {
                foreach (IDisposable d in owned)
                    d.Dispose();
            }
        }

        /// <summary>
        /// The top-level colour, lighter by a step per ring out, so the levels stay apart while a
        /// branch keeps its colour. Hatched and other brushes are used as they are.
        /// </summary>
        static Draw2.Brush ShadeForRing(Draw2.Brush brush, int ring, List<IDisposable> owned)
        {
            if (ring == 0 || !(brush is Draw2.SolidBrush sb))
                return brush;
            double t = Math.Min(0.6, 0.2 * ring);
            System.Drawing.Color c = sb.Color;
            var shaded = new Draw2.SolidBrush(System.Drawing.Color.FromArgb(c.A,
                (int)Math.Round(c.R + (255 - c.R) * t),
                (int)Math.Round(c.G + (255 - c.G) * t),
                (int)Math.Round(c.B + (255 - c.B) * t)));
            owned.Add(shaded);
            return shaded;
        }

        static void DrawRingSector(Draw2.Graphics g, Draw2.Brush brush, float cx, float cy, float inner, float outer, SunburstSlice slice)
        {
            g.FillPolygon(brush, RingSector(cx, cy, inner, outer, slice.StartAngle, slice.SweepAngle, ArcStep));

            // Thin separators: the slice's outline, or for a whole turn its two circles (a polygon
            // round a whole ring would draw a seam across it).
            Draw2.Pen pen = Draw2.Pens.White;
            if (slice.SweepAngle >= 360 - 1e-6)
            {
                g.DrawEllipse(pen, cx - outer, cy - outer, outer * 2, outer * 2);
                g.DrawEllipse(pen, cx - inner, cy - inner, inner * 2, inner * 2);
            }
            else
                g.DrawPolygon(pen, RingSector(cx, cy, inner, outer, slice.StartAngle, slice.SweepAngle, ArcStep));
        }

        /// <summary>
        /// The outline of a ring sector: along the outer arc, then back along the inner arc.
        /// </summary>
        internal static System.Drawing.PointF[] RingSector(float cx, float cy, float inner, float outer, double startAngle, double sweepAngle, double step)
        {
            int n = Math.Max(1, (int)Math.Ceiling(Math.Abs(sweepAngle) / step));
            var points = new System.Drawing.PointF[(n + 1) * 2];
            for (int i = 0; i <= n; i++)
            {
                double a = (startAngle + sweepAngle * i / n) * Math.PI / 180;
                points[i] = new System.Drawing.PointF(cx + (float)(outer * Math.Cos(a)), cy + (float)(outer * Math.Sin(a)));
                points[(n + 1) * 2 - 1 - i] = new System.Drawing.PointF(cx + (float)(inner * Math.Cos(a)), cy + (float)(inner * Math.Sin(a)));
            }
            return points;
        }

        /// <summary>
        /// The category label, centred in the slice, drawn only when its box lies wholly inside the
        /// slice: within the ring's width and the slice's angle.
        /// </summary>
        static void DrawLabel(Draw2.Graphics g, Draw2.Font font, Draw2.StringFormat format, Draw2.Brush fill,
            float cx, float cy, float inner, float outer, SunburstSlice slice)
        {
            string text = slice.Node.Label;
            if (string.IsNullOrEmpty(text))
                return;

            System.Drawing.SizeF size = g.MeasureString(text, font);
            double mid = (slice.StartAngle + slice.SweepAngle / 2) * Math.PI / 180;
            float midRadius = (inner + outer) / 2;
            float x = cx + (float)(midRadius * Math.Cos(mid));
            float y = cy + (float)(midRadius * Math.Sin(mid));
            var box = new System.Drawing.RectangleF(x - size.Width / 2, y - size.Height / 2, size.Width, size.Height);
            if (!LabelFits(box, cx, cy, inner, outer, slice.StartAngle, slice.SweepAngle))
                return;

            System.Drawing.Color textColor = System.Drawing.Color.Black;
            if (fill is Draw2.SolidBrush sb)
            {
                System.Drawing.Color c = sb.Color;
                double luminance = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255;
                if (luminance < 0.55)
                    textColor = System.Drawing.Color.White;
            }
            using (var textBrush = new Draw2.SolidBrush(textColor))
                g.DrawString(text, font, textBrush, box, format);
        }

        /// <summary>
        /// Whether every corner of the box (with a pixel's margin) lies inside the ring sector.
        /// </summary>
        internal static bool LabelFits(System.Drawing.RectangleF box, float cx, float cy, float inner, float outer, double startAngle, double sweepAngle)
        {
            const float margin = 1;
            System.Drawing.RectangleF b = System.Drawing.RectangleF.Inflate(box, margin, margin);
            System.Drawing.PointF[] corners = {
                new System.Drawing.PointF(b.Left, b.Top), new System.Drawing.PointF(b.Right, b.Top),
                new System.Drawing.PointF(b.Left, b.Bottom), new System.Drawing.PointF(b.Right, b.Bottom),
            };
            foreach (System.Drawing.PointF p in corners)
            {
                double dx = p.X - cx, dy = p.Y - cy;
                double r = Math.Sqrt(dx * dx + dy * dy);
                if (r < inner || r > outer)
                    return false;
                if (sweepAngle >= 360)
                    continue;
                double a = Math.Atan2(dy, dx) * 180 / Math.PI;
                double offset = ((a - startAngle) % 360 + 360) % 360;
                if (offset > sweepAngle)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// A tooltip over the slice with its full path and value, as the map chart gives its polygons.
        /// </summary>
        void AddToolTip(Draw2.Graphics g, float cx, float cy, float inner, float outer, SunburstSlice slice)
        {
            string text = ToolTipText(slice.Node, _tooltipYFormat);
            var sb = new StringBuilder("PolyToolTip:");
            sb.Append(text);
            foreach (System.Drawing.PointF p in RingSector(cx, cy, inner, outer, slice.StartAngle, slice.SweepAngle, 10))
                sb.AppendFormat(NumberFormatInfo.InvariantInfo, "|{0}|{1}", p.X, p.Y);
            g.AddMetafileComment(new ASCIIEncoding().GetBytes(sb.ToString()));
        }

        /// <summary>
        /// "Europe / France / Paris = 1.5". The tooltip comment separates its fields with '|' and its
        /// tag with ':', so neither can appear in the text.
        /// </summary>
        internal static string ToolTipText(ChartHierarchyNode node, string? format)
        {
            string value;
            try
            {
                value = string.IsNullOrEmpty(format) ? node.Value.ToString(CultureInfo.CurrentCulture) : node.Value.ToString(format, CultureInfo.CurrentCulture);
            }
            catch (FormatException)
            {
                value = node.Value.ToString(CultureInfo.CurrentCulture);
            }
            string text = string.Join(" / ", node.Path) + " = " + value;
            return text.Replace('|', '/').Replace(':', ' ');
        }
    }
}
