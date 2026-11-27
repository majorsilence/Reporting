using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
    ///<summary>
    /// One value of a hierarchical chart (TreeMap, Sunburst) with the category path it sits at,
    /// outermost level first (Region, Country, City).
    ///</summary>
    internal readonly struct ChartHierarchyPoint
    {
        internal ChartHierarchyPoint(IReadOnlyList<string>? path, double value, int row = -1, int column = -1)
        {
            Path = path ?? Array.Empty<string>();
            Value = value;
            Row = row;
            Column = column;
        }

        /// <summary>The label of each category level, outermost first.</summary>
        internal IReadOnlyList<string> Path { get; }

        internal double Value { get; }

        /// <summary>The cell of the chart's grid the value came from (-1 when it did not come from one).</summary>
        internal int Row { get; }

        internal int Column { get; }
    }

    ///<summary>
    /// A node of the value tree a TreeMap or Sunburst draws: the root holds the top-level categories,
    /// each node the categories inside it, and the leaves the innermost level.
    ///</summary>
    internal sealed class ChartHierarchyNode
    {
        // Children by label; the list keeps them in the order the grid (and so the category sorting) gave.
        readonly Dictionary<string, ChartHierarchyNode> _byLabel = new Dictionary<string, ChartHierarchyNode>(StringComparer.Ordinal);
        readonly List<ChartHierarchyNode> _children = new List<ChartHierarchyNode>();

        internal ChartHierarchyNode(ChartHierarchyNode? parent, string label, IReadOnlyList<string> path)
        {
            Parent = parent;
            Label = label;
            Path = path;
        }

        /// <summary>The category label; empty for the root.</summary>
        internal string Label { get; }

        /// <summary>The labels from the top level down to this node; empty for the root.</summary>
        internal IReadOnlyList<string> Path { get; }

        /// <summary>The sum of the values of every leaf under this node.</summary>
        internal double Value { get; private set; }

        internal ChartHierarchyNode? Parent { get; }

        internal IReadOnlyList<ChartHierarchyNode> Children => _children;

        internal bool IsLeaf => _children.Count == 0;

        /// <summary>0 for the root, 1 for a top-level category, and so on.</summary>
        internal int Depth => Path.Count;

        /// <summary>
        /// The grid cell of the first value that reached this node, for evaluating its data point
        /// (style, label, action, tooltip) with ChartBase's per-cell helpers; -1 on the root.
        /// </summary>
        internal int Row { get; private set; } = -1;

        internal int Column { get; private set; } = -1;

        internal ChartHierarchyNode GetOrAddChild(string? label)
        {
            label ??= string.Empty;
            if (_byLabel.TryGetValue(label, out ChartHierarchyNode? existing))
                return existing;

            var path = new string[Path.Count + 1];
            for (int i = 0; i < Path.Count; i++)
                path[i] = Path[i];
            path[Path.Count] = label;
            var child = new ChartHierarchyNode(this, label, path);
            _byLabel.Add(label, child);
            _children.Add(child);
            return child;
        }

        internal void Add(double value, int row, int column)
        {
            Value += value;
            if (Row < 0 && Path.Count > 0)
            {
                Row = row;
                Column = column;
            }
        }
    }

    ///<summary>
    /// The value tree of a hierarchical chart, built from the category path of each data point.
    ///</summary>
    internal sealed class ChartHierarchy
    {
        ChartHierarchy(ChartHierarchyNode root, int depth, int leftOutCount)
        {
            Root = root;
            Depth = depth;
            LeftOutCount = leftOutCount;
        }

        /// <summary>The root: no label, its value is the chart total, its children the top level.</summary>
        internal ChartHierarchyNode Root { get; }

        /// <summary>The number of levels below the root (1 for a chart with one category grouping).</summary>
        internal int Depth { get; }

        /// <summary>
        /// The number of zero, negative or non-numeric values left out. An area or an angle cannot
        /// be negative, so SSRS leaves these out of a TreeMap or Sunburst and says so once per chart.
        /// </summary>
        internal int LeftOutCount { get; }

        internal bool AnyLeftOut => LeftOutCount > 0;

        internal double Total => Root.Value;

        /// <summary>
        /// Builds the tree. Points sharing a path (several series at one category) add up into one
        /// leaf; a category whose every value is left out does not appear at all.
        /// </summary>
        internal static ChartHierarchy Build(IEnumerable<ChartHierarchyPoint>? points)
        {
            var root = new ChartHierarchyNode(null, string.Empty, Array.Empty<string>());
            int leftOut = 0;
            int depth = 0;
            if (points != null)
            {
                foreach (ChartHierarchyPoint p in points)
                {
                    if (!(p.Value > 0) || double.IsInfinity(p.Value))       // also catches NaN
                    {
                        leftOut++;
                        continue;
                    }

                    root.Add(p.Value, p.Row, p.Column);
                    ChartHierarchyNode node = root;
                    foreach (string label in p.Path)
                    {
                        node = node.GetOrAddChild(label);
                        node.Add(p.Value, p.Row, p.Column);
                    }
                    depth = Math.Max(depth, p.Path.Count);
                }
            }
            return new ChartHierarchy(root, depth, leftOut);
        }

        /// <summary>
        /// Reads the category path and value of every data point from the grid the chart's matrix
        /// builds. The grid has a header row per series grouping, then a row per innermost
        /// category; and a header column per category grouping, outermost first, then a column
        /// per series. An outer level's header sits only on the first row of its group, so the
        /// rows below it carry it on.
        /// </summary>
        /// <param name="grid">The grid, indexed [row, column].</param>
        /// <param name="headerRows">The series header rows at the top (at least 1).</param>
        /// <param name="levels">The category header columns at the left (at least 1).</param>
        /// <param name="categoryLabel">The label of the header cell at (row, level).</param>
        /// <param name="value">The value of the data cell at (row, column).</param>
        /// <param name="seriesLabel">When given, the label of the series in a column, added as the
        /// innermost level; otherwise the series at one category add up into one leaf.</param>
        internal static async Task<List<ChartHierarchyPoint>> ReadGrid(MatrixCellEntry?[,]? grid, int headerRows, int levels,
            Func<int, int, Task<string?>> categoryLabel, Func<int, int, Task<double>> value,
            Func<int, Task<string?>>? seriesLabel = null)
        {
            var points = new List<ChartHierarchyPoint>();
            if (grid == null)
                return points;
            int rows = grid.GetLength(0);
            int columns = grid.GetLength(1);
            var path = new string[levels];
            for (int i = 0; i < levels; i++)
                path[i] = string.Empty;

            var series = new string[columns];
            if (seriesLabel != null)
            {
                for (int col = levels; col < columns; col++)
                    series[col] = await seriesLabel(col).ConfigureAwait(false) ?? string.Empty;
            }

            for (int row = headerRows; row < rows; row++)
            {
                for (int level = 0; level < levels && level < columns; level++)
                {
                    if (grid[row, level] != null)
                        path[level] = await categoryLabel(row, level).ConfigureAwait(false) ?? string.Empty;
                }

                for (int col = levels; col < columns; col++)
                {
                    if (grid[row, col] == null)
                        continue;
                    string[] p;
                    if (seriesLabel == null)
                        p = (string[])path.Clone();
                    else
                    {
                        p = new string[levels + 1];
                        path.CopyTo(p, 0);
                        p[levels] = series[col];
                    }
                    points.Add(new ChartHierarchyPoint(p, await value(row, col).ConfigureAwait(false), row, col));
                }
            }
            return points;
        }
    }
}
