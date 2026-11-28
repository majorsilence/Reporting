using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// A map's MapDataRegion: the analytical data a vector layer binds to its shapes, a dataset
    /// grouped by MapMember/Group.
    /// </summary>
    /// <remarks>
    /// The expressions of a layer bound to the region (its binding expressions, rule values and
    /// template styles) are evaluated once per group instance, on the group's first row, and an
    /// aggregate in them covers that group's rows: Expression.FinalPass gives them the region's
    /// grouping as their scope, and the grouping is treated as a matrix grouping, whose current
    /// rows <see cref="ForEachGroup"/> sets. Only the first level of MapMember is read; group
    /// filters and sorting are not applied. A region without a group binds each row on its own.
    /// </remarks>
    internal sealed class MapDataRegion : DataRegion
    {
        internal Grouping Grouping { get; }

        internal MapDataRegion (ReportDefn r, ReportLink p, XmlNode xNode) : base (r, p, xNode)
        {
            foreach (XmlNode node in xNode.ChildNodes) {
                if (node.NodeType != XmlNodeType.Element)
                    continue;
                if (node.LocalName == "MapMember") {
                    var group = MapEmbeddedElement.Child (node, "Group");
                    if (group != null)
                        Grouping = new Grouping (r, this, group);
                    continue;
                }
                if (!DataRegionElement (node))
                    OwnerReport.rl.LogError (4, "Unknown MapDataRegion element '" + node.Name + "' ignored.");
            }
        }

        internal override async Task FinalPass ()
        {
            await base.FinalPass ().ConfigureAwait (false);
            if (Grouping != null)
                await Grouping.FinalPass ().ConfigureAwait (false);
        }

        /// <summary>
        /// Runs <paramref name="action"/> once per group instance with the group's first row, while
        /// the group's rows are the ones its aggregates read.
        /// </summary>
        internal async Task ForEachGroup (Report rpt, Row row, Func<Row, Task> action)
        {
            if (DataSetDefn == null)
                return;
            var data = await GetFilteredData (rpt, row).ConfigureAwait (false);
            if (data?.Data == null || data.Data.Count == 0)
                return;

            var groups = new List<List<Row>> ();
            if (Grouping?.GroupExpressions == null) {
                foreach (Row r in data.Data)
                    groups.Add (new List<Row> { r });
            } else {
                var index = new Dictionary<string, List<Row>> (StringComparer.Ordinal);
                foreach (Row r in data.Data) {
                    var key = new System.Text.StringBuilder ();
                    foreach (GroupExpression ge in Grouping.GroupExpressions.Items) {
                        var value = await ge.Expression.Evaluate (rpt, r).ConfigureAwait (false);
                        key.Append (Convert.ToString (value, CultureInfo.InvariantCulture)).Append ('\u001F');
                    }
                    if (!index.TryGetValue (key.ToString (), out var members)) {
                        members = new List<Row> ();
                        index[key.ToString ()] = members;
                        groups.Add (members);
                    }
                    members.Add (r);
                }
            }

            try {
                foreach (var members in groups) {
                    var rows = new Rows (rpt);
                    var copies = new List<Row> (members.Count);
                    foreach (var member in members)
                        copies.Add (new Row (rows, member) { RowNumber = member.RowNumber });
                    rows.Data = copies;
                    Grouping?.SetRows (rpt, rows);
                    await action (copies[0]).ConfigureAwait (false);
                }
            } finally {
                Grouping?.SetRows (rpt, null);
            }
        }
    }
}
