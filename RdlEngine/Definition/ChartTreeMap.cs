using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Draw2 = Majorsilence.Forms.Drawing;
using RectangleF = System.Drawing.RectangleF;


namespace Majorsilence.Reporting.Rdl
{
    ///<summary>
    /// TreeMap chart: nested rectangles sized by value, one level of nesting per category grouping.
    ///</summary>
    internal class ChartTreeMap : ChartBase
    {
        /// <summary>Space around a label, inside its cell or header strip.</summary>
        const int LabelPadding = 2;

        /// <summary>The frame of the parent's colour left around its children on the left, right and bottom.</summary>
        const int ParentInset = 2;

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
        /// Draws the tree into Layout.PlotArea: a squarified layout of the top level, then of each
        /// node's children inside its rectangle. An inner node draws a header strip with its label
        /// above its children; a leaf is filled with its top-level category's palette colour.
        /// </summary>
        async Task DrawPlotAreaTreeMap(Report rpt, Draw2.Graphics g, ChartHierarchy hierarchy)
        {
            if (hierarchy.Root.Children.Count == 0)
                return;     // everything was left out: the plot stays empty

            System.Drawing.Rectangle plot = Layout.PlotArea;
            if (plot.Width <= 0 || plot.Height <= 0)
                return;

            Row row = ChartRow;         // the row the palette is evaluated on, as for the series brushes
            using (Draw2.Font font = new Draw2.Font("Arial", 8))
            using (Draw2.StringFormat format = new Draw2.StringFormat())
            using (Draw2.Pen border = new Draw2.Pen(System.Drawing.Color.White, 1))
            {
                format.Alignment = Draw2.StringAlignment.Near;
                format.LineAlignment = Draw2.StringAlignment.Near;
                format.FormatFlags |= Draw2.StringFormatFlags.NoWrap;

                var ctx = new DrawContext(g, font, format, border,
                    (int)Math.Ceiling(g.MeasureString("Ag", font).Height) + LabelPadding);

                // One palette colour per top-level category, in the grid's order.
                IReadOnlyList<ChartHierarchyNode> top = hierarchy.Root.Children;
                var colours = new Draw2.Brush[top.Count];
                for (int i = 0; i < top.Count; i++)
                    colours[i] = await GetPaletteBrush(rpt, row, ChartDefn.OwnerReport, i).ConfigureAwait(false);

                RectangleF[] cells = ComputeSquarified(Values(top), new RectangleF(plot.X, plot.Y, plot.Width, plot.Height));
                for (int i = 0; i < top.Count; i++)
                    await DrawNode(rpt, ctx, top[i], cells[i], colours[i]).ConfigureAwait(false);
            }
        }

        sealed class DrawContext
        {
            internal DrawContext(Draw2.Graphics g, Draw2.Font font, Draw2.StringFormat format, Draw2.Pen border, int headerHeight)
            {
                G = g; Font = font; Format = format; Border = border; HeaderHeight = headerHeight;
            }
            internal readonly Draw2.Graphics G;
            internal readonly Draw2.Font Font;
            internal readonly Draw2.StringFormat Format;
            internal readonly Draw2.Pen Border;
            internal readonly int HeaderHeight;
        }

        async Task DrawNode(Report rpt, DrawContext ctx, ChartHierarchyNode node, RectangleF cell, Draw2.Brush category)
        {
            // Whole pixels, so neighbouring cells share an edge instead of blending into each other.
            var rect = System.Drawing.Rectangle.FromLTRB((int)Math.Round(cell.Left), (int)Math.Round(cell.Top),
                (int)Math.Round(cell.Right), (int)Math.Round(cell.Bottom));
            if (rect.Width <= 0 || rect.Height <= 0)
                return;     // too small to show at this size

            Draw2.Graphics g = ctx.G;
            if (node.IsLeaf)
            {
                Draw2.Brush? own = await GetDataPointBrush(rpt, node).ConfigureAwait(false);
                Draw2.Brush fill = own ?? Shade(category, node.Depth, leaf: true);
                g.FillRectangle(fill, rect);
                DrawCellBorder(ctx, rect);
                DrawLabel(ctx, node.Label, rect, TextColour(fill));
                AddToolTip(g, node, rect);
                return;
            }

            // An inner node: its colour, a header strip with its label, and its children below it.
            Draw2.Brush header = Shade(category, node.Depth, leaf: false);
            g.FillRectangle(header, rect);
            DrawCellBorder(ctx, rect);

            var inner = System.Drawing.Rectangle.FromLTRB(rect.Left + ParentInset, rect.Top + ctx.HeaderHeight,
                rect.Right - ParentInset, rect.Bottom - ParentInset);
            if (inner.Width < 2 || inner.Height < 2)
            {
                // No room for a header: the children take the whole rectangle.
                inner = rect;
            }
            else
            {
                var strip = new System.Drawing.Rectangle(rect.Left, rect.Top, rect.Width, ctx.HeaderHeight);
                DrawLabel(ctx, node.Label, strip, TextColour(header));
                AddToolTip(g, node, strip);
            }

            RectangleF[] cells = ComputeSquarified(Values(node.Children), new RectangleF(inner.X, inner.Y, inner.Width, inner.Height));
            for (int i = 0; i < node.Children.Count; i++)
                await DrawNode(rpt, ctx, node.Children[i], cells[i], category).ConfigureAwait(false);
        }

        static void DrawCellBorder(DrawContext ctx, System.Drawing.Rectangle rect)
        {
            if (rect.Width > 2 && rect.Height > 2)
                ctx.G.DrawRectangle(ctx.Border, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
        }

        /// <summary>
        /// Draws a label in the top left of a rectangle when it fits; a label too long for it is cut
        /// short with an ellipsis, and one with no room for even a letter is left out.
        /// </summary>
        static void DrawLabel(DrawContext ctx, string label, System.Drawing.Rectangle rect, System.Drawing.Color colour)
        {
            if (string.IsNullOrEmpty(label))
                return;
            float room = rect.Width - 2 * LabelPadding;
            float height = ctx.G.MeasureString(label, ctx.Font).Height;
            if (room <= 0 || height + LabelPadding > rect.Height)
                return;

            string text = FitLabel(label, room, s => ctx.G.MeasureString(s, ctx.Font).Width);
            if (text.Length == 0)
                return;

            using (var brush = new Draw2.SolidBrush(colour))
                ctx.G.DrawString(text, ctx.Font, brush,
                    new RectangleF(rect.X + LabelPadding, rect.Y + LabelPadding / 2f, room, height), ctx.Format);
        }

        /// <summary>
        /// The label as it fits in <paramref name="room"/>: whole, cut short with an ellipsis, or
        /// empty when not even one letter and the ellipsis fit.
        /// </summary>
        internal static string FitLabel(string label, float room, Func<string, float> width)
        {
            if (string.IsNullOrEmpty(label) || room <= 0)
                return string.Empty;
            if (width(label) <= room)
                return label;

            const string ellipsis = "…";
            // The longest start of the label that fits with the ellipsis (binary search on its length).
            int lo = 0, hi = label.Length - 1;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (width(label.Substring(0, mid) + ellipsis) <= room)
                    lo = mid;
                else
                    hi = mid - 1;
            }
            return lo == 0 ? string.Empty : label.Substring(0, lo).TrimEnd() + ellipsis;
        }

        /// <summary>
        /// The tooltip, through the metafile comment the other chart types use: the full category
        /// path and the value.
        /// </summary>
        void AddToolTip(Draw2.Graphics g, ChartHierarchyNode node, System.Drawing.Rectangle rect)
        {
            if (!_showToolTips)
                return;
            string val = "ToolTip:" + ToolTipText(node, _tooltipYFormat) +
                "|X:" + rect.X + "|Y:" + rect.Y + "|W:" + rect.Width + "|H:" + rect.Height;
            g.AddMetafileComment(new System.Text.ASCIIEncoding().GetBytes(val));
        }

        /// <summary>
        /// "Europe / France / Paris = 1.5". The comment's reader splits on '|' and ':', so those are
        /// replaced in the text.
        /// </summary>
        internal static string ToolTipText(ChartHierarchyNode node, string? format)
        {
            string text = string.Join(" / ", node.Path) + " = " +
                (string.IsNullOrEmpty(format) ? node.Value.ToString() : node.Value.ToString(format));
            return text.Replace('|', '/').Replace(':', '.');
        }

        /// <summary>The data point's own BackgroundColor for a leaf, when its style sets one.</summary>
        async Task<Draw2.Brush?> GetDataPointBrush(Report rpt, ChartHierarchyNode node)
        {
            if (node.Row < 0 || node.Column < 0)
                return null;
            MatrixCellEntry mce = DataDefn[node.Row, node.Column];
            if (mce == null)
                return null;
            ChartExpression ce = (ChartExpression)mce.DisplayItem;
            if (ce.DP == null || ce.DP.Style == null || ce.DP.Style.BackgroundColor == null)
                return null;

            ChartDefn.ChartMatrix.SetMyData(rpt, mce.Data);      // Must set this for evaluation
            Row lrow = mce.Data.Data.Count > 0 ? mce.Data.Data[0] : null!;
            string sc = await ce.DP.Style.BackgroundColor.EvaluateString(rpt, lrow).ConfigureAwait(false);
            System.Drawing.Color c = XmlUtil.ColorFromHtml(sc, System.Drawing.Color.Empty, rpt);
            return c == System.Drawing.Color.Empty ? null : new Draw2.SolidBrush(c);
        }

        /// <summary>
        /// The colour of a node of a top-level category: the headers of the outer levels darker, the
        /// leaves lighter the deeper they sit. A patterned palette's pattern fills the leaves, and
        /// its colour the headers.
        /// </summary>
        static Draw2.Brush Shade(Draw2.Brush category, int depth, bool leaf)
        {
            System.Drawing.Color c;
            if (category is Draw2.SolidBrush sb)
                c = sb.Color;
            else if (category is Draw2.Drawing2D.HatchBrush hb)
            {
                if (leaf)
                    return category;
                c = hb.ForegroundColor;
            }
            else
                return category;

            if (leaf)
                return depth <= 1 ? category : new Draw2.SolidBrush(Blend(c, System.Drawing.Color.White, Math.Min(0.45, 0.15 * (depth - 1))));
            return new Draw2.SolidBrush(Blend(c, System.Drawing.Color.Black, 0.3 / depth));
        }

        static System.Drawing.Color Blend(System.Drawing.Color from, System.Drawing.Color to, double t)
        {
            return System.Drawing.Color.FromArgb(from.A,
                (int)Math.Round(from.R + (to.R - from.R) * t),
                (int)Math.Round(from.G + (to.G - from.G) * t),
                (int)Math.Round(from.B + (to.B - from.B) * t));
        }

        /// <summary>Black on light fills, white on dark ones.</summary>
        static System.Drawing.Color TextColour(Draw2.Brush fill)
        {
            if (!(fill is Draw2.SolidBrush sb))
                return System.Drawing.Color.Black;     // a pattern on white
            System.Drawing.Color c = sb.Color;
            double luminance = 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;
            return luminance < 140 ? System.Drawing.Color.White : System.Drawing.Color.Black;
        }

        static double[] Values(IReadOnlyList<ChartHierarchyNode> nodes)
        {
            var values = new double[nodes.Count];
            for (int i = 0; i < values.Length; i++)
                values[i] = nodes[i].Value;
            return values;
        }

        /// <summary>
        /// The squarified treemap layout (Bruls, Huizing and van Wijk, 2000): the rectangles, in the
        /// order of <paramref name="values"/>, that tile <paramref name="bounds"/> with areas in
        /// proportion to the values. The values are placed largest first, in rows along the shorter
        /// side of the space left, and a row takes another value only while that does not make its
        /// worst aspect ratio worse. Zero, negative and non-finite values get an empty rectangle.
        /// </summary>
        internal static RectangleF[] ComputeSquarified(IReadOnlyList<double>? values, RectangleF bounds)
        {
            int n = values?.Count ?? 0;
            var result = new RectangleF[n];
            if (values == null || n == 0)
                return result;
            for (int i = 0; i < n; i++)
                result[i] = new RectangleF(bounds.X, bounds.Y, 0, 0);

            double total = 0;
            var order = new List<int>(n);
            for (int i = 0; i < n; i++)
            {
                double v = values[i];
                if (v > 0 && !double.IsInfinity(v))
                {
                    total += v;
                    order.Add(i);
                }
            }
            if (order.Count == 0 || !(bounds.Width > 0) || !(bounds.Height > 0))
                return result;

            // Largest first; equal values keep their order.
            order.Sort((a, b) =>
            {
                int c = values[b].CompareTo(values[a]);
                return c != 0 ? c : a.CompareTo(b);
            });

            double x = bounds.X, y = bounds.Y, w = bounds.Width, h = bounds.Height;
            double scale = w * h / total;
            var areas = new double[n];
            foreach (int i in order)
                areas[i] = values[i] * scale;

            int start = 0;
            while (start < order.Count)
            {
                double side = Math.Min(w, h);
                double rowArea = areas[order[start]];
                double rowMin = rowArea, rowMax = rowArea;
                double worst = Worst(rowArea, rowMin, rowMax, side);
                int end = start + 1;
                while (end < order.Count)
                {
                    double a = areas[order[end]];
                    double next = Worst(rowArea + a, Math.Min(rowMin, a), Math.Max(rowMax, a), side);
                    if (next > worst)
                        break;
                    rowArea += a;
                    rowMin = Math.Min(rowMin, a);
                    rowMax = Math.Max(rowMax, a);
                    worst = next;
                    end++;
                }

                bool last = end == order.Count;
                if (w >= h)
                {
                    // A column at the left, as tall as the space; the last one takes what is left exactly.
                    double thickness = last ? w : Math.Min(w, rowArea / h);
                    double cy = y;
                    for (int k = start; k < end; k++)
                    {
                        double ch = k == end - 1 ? y + h - cy : areas[order[k]] / thickness;
                        result[order[k]] = new RectangleF((float)x, (float)cy, (float)thickness, (float)ch);
                        cy += ch;
                    }
                    x += thickness;
                    w -= thickness;
                }
                else
                {
                    // A row along the top, as wide as the space.
                    double thickness = last ? h : Math.Min(h, rowArea / w);
                    double cx = x;
                    for (int k = start; k < end; k++)
                    {
                        double cw = k == end - 1 ? x + w - cx : areas[order[k]] / thickness;
                        result[order[k]] = new RectangleF((float)cx, (float)y, (float)cw, (float)thickness);
                        cx += cw;
                    }
                    y += thickness;
                    h -= thickness;
                }
                start = end;
            }
            return result;
        }

        /// <summary>
        /// The worst (largest) aspect ratio of a row of areas laid along a side:
        /// max(side² · max / sum², sum² / (side² · min)).
        /// </summary>
        static double Worst(double sum, double min, double max, double side)
        {
            double s2 = side * side;
            double sum2 = sum * sum;
            return Math.Max(s2 * max / sum2, sum2 / (s2 * min));
        }
    }
}
