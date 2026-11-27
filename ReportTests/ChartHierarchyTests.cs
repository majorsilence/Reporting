using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// The shared part of the TreeMap and Sunburst charts (#639): every data point's full category
    /// path read from the chart's grid, and the value tree built from those paths.
    /// </summary>
    [TestFixture]
    public class ChartHierarchyTests
    {
        private static ChartHierarchyPoint P (double value, params string[] path) => new ChartHierarchyPoint (path, value);

        private static string[] Labels (ChartHierarchyNode node) => node.Children.Select (c => c.Label).ToArray ();

        #region Building the tree

        [Test]
        public void OneLevel_IsAListOfLeaves ()
        {
            var h = ChartHierarchy.Build (new[] { P (3, "A"), P (5, "B"), P (2, "C") });

            Assert.That (h.Depth, Is.EqualTo (1));
            Assert.That (Labels (h.Root), Is.EqualTo (new[] { "A", "B", "C" }), "the grid's order is kept");
            Assert.That (h.Root.Children.All (c => c.IsLeaf), Is.True);
            Assert.That (h.Total, Is.EqualTo (10));
            Assert.That (h.AnyLeftOut, Is.False);
        }

        [Test]
        public void TwoLevels_ParentsSumTheirLeaves ()
        {
            var h = ChartHierarchy.Build (new[] {
                P (10, "Europe", "France"), P (20, "Europe", "Spain"), P (5, "Asia", "Japan"),
            });

            Assert.That (h.Depth, Is.EqualTo (2));
            Assert.That (Labels (h.Root), Is.EqualTo (new[] { "Europe", "Asia" }));
            var europe = h.Root.Children[0];
            Assert.That (europe.Value, Is.EqualTo (30));
            Assert.That (europe.Depth, Is.EqualTo (1));
            Assert.That (Labels (europe), Is.EqualTo (new[] { "France", "Spain" }));
            Assert.That (europe.Children[1].Path, Is.EqualTo (new[] { "Europe", "Spain" }));
            Assert.That (europe.Children[1].Parent, Is.SameAs (europe));
            Assert.That (h.Total, Is.EqualTo (35));
        }

        [Test]
        public void ThreeLevels_NestAllTheWayDown ()
        {
            var h = ChartHierarchy.Build (new[] {
                P (1, "Europe", "France", "Paris"), P (2, "Europe", "France", "Lyon"),
                P (4, "Europe", "Spain", "Madrid"), P (8, "Asia", "Japan", "Tokyo"),
            });

            Assert.That (h.Depth, Is.EqualTo (3));
            var france = h.Root.Children[0].Children[0];
            Assert.That (france.Value, Is.EqualTo (3));
            Assert.That (Labels (france), Is.EqualTo (new[] { "Paris", "Lyon" }));
            Assert.That (france.Children[1].Path, Is.EqualTo (new[] { "Europe", "France", "Lyon" }));
            Assert.That (france.Children[1].Depth, Is.EqualTo (3));
            Assert.That (h.Root.Children[0].Value, Is.EqualTo (7));
            Assert.That (h.Root.Children[1].Value, Is.EqualTo (8));
        }

        [Test]
        public void ZeroNegativeAndNonNumericValues_AreLeftOutAndCounted ()
        {
            var h = ChartHierarchy.Build (new[] {
                P (10, "Europe", "France"), P (0, "Europe", "Spain"), P (-4, "Asia", "Japan"),
                P (double.NaN, "Asia", "China"), P (double.PositiveInfinity, "Asia", "Korea"), P (6, "Africa", "Kenya"),
            });

            Assert.That (h.LeftOutCount, Is.EqualTo (4));
            Assert.That (h.AnyLeftOut, Is.True);
            Assert.That (Labels (h.Root), Is.EqualTo (new[] { "Europe", "Africa" }), "a category with nothing left in it is not drawn");
            Assert.That (Labels (h.Root.Children[0]), Is.EqualTo (new[] { "France" }));
            Assert.That (h.Total, Is.EqualTo (16));
        }

        [Test]
        public void PointsSharingAPath_AddUpIntoOneLeaf ()
        {
            var h = ChartHierarchy.Build (new[] {
                new ChartHierarchyPoint (new[] { "A", "x" }, 2, 3, 4),
                new ChartHierarchyPoint (new[] { "A", "x" }, 5, 3, 5),
            });

            var leaf = h.Root.Children[0].Children.Single ();
            Assert.That (leaf.Value, Is.EqualTo (7));
            Assert.That ((leaf.Row, leaf.Column), Is.EqualTo ((3, 4)), "the leaf keeps the first cell it came from");
        }

        [Test]
        public void NoPoints_IsAnEmptyRoot ()
        {
            var h = ChartHierarchy.Build (Array.Empty<ChartHierarchyPoint> ());

            Assert.That (h.Root.Children, Is.Empty);
            Assert.That (h.Total, Is.EqualTo (0));
            Assert.That (h.Depth, Is.EqualTo (0));
        }

        #endregion

        #region Reading the grid

        /// <summary>
        /// A grid as the chart's matrix lays it out: header rows on top, a header column per
        /// category level on the left, and an outer level's header only on its group's first row.
        /// </summary>
        private sealed class Grid
        {
            public readonly MatrixCellEntry[,] Cells;
            public readonly Dictionary<(int, int), string> Labels = new ();
            public readonly Dictionary<(int, int), double> Values = new ();

            public Grid (int rows, int columns) => Cells = new MatrixCellEntry[rows, columns];

            public void Header (int row, int column, string label)
            {
                Cells[row, column] = new MatrixCellEntry (null, null);
                Labels[(row, column)] = label;
            }

            public void Value (int row, int column, double value)
            {
                Cells[row, column] = new MatrixCellEntry (null, null);
                Values[(row, column)] = value;
            }

            public Task<List<ChartHierarchyPoint>> Read (int headerRows, int levels, bool series = false)
                => ChartHierarchy.ReadGrid (Cells, headerRows, levels,
                    (r, c) => Task.FromResult (Labels[(r, c)]),
                    (r, c) => Task.FromResult (Values[(r, c)]),
                    series ? c => Task.FromResult (Labels[(headerRows - 1, c)]) : null);
        }

        private static string[] Paths (IEnumerable<ChartHierarchyPoint> points)
            => points.Select (p => string.Join ("/", p.Path) + "=" + p.Value.ToString (CultureInfo.InvariantCulture)).ToArray ();

        [Test]
        public async Task ReadGrid_OneLevel ()
        {
            var g = new Grid (3, 2);
            g.Header (1, 0, "A"); g.Value (1, 1, 3);
            g.Header (2, 0, "B"); g.Value (2, 1, 4);

            Assert.That (Paths (await g.Read (1, 1)), Is.EqualTo (new[] { "A=3", "B=4" }));
        }

        [Test]
        public async Task ReadGrid_TwoLevels_CarryTheOuterHeaderDown ()
        {
            var g = new Grid (5, 3);
            g.Header (1, 0, "Europe"); g.Header (1, 1, "France"); g.Value (1, 2, 1);
            g.Header (2, 1, "Spain"); g.Value (2, 2, 2);
            g.Header (3, 0, "Asia"); g.Header (3, 1, "Japan"); g.Value (3, 2, 3);
            g.Header (4, 1, "China"); g.Value (4, 2, 4);

            var points = await g.Read (1, 2);

            Assert.That (Paths (points), Is.EqualTo (new[] { "Europe/France=1", "Europe/Spain=2", "Asia/Japan=3", "Asia/China=4" }));
            Assert.That ((points[1].Row, points[1].Column), Is.EqualTo ((2, 2)));
        }

        [Test]
        public async Task ReadGrid_ThreeLevels ()
        {
            var g = new Grid (5, 4);
            g.Header (1, 0, "Europe"); g.Header (1, 1, "France"); g.Header (1, 2, "Paris"); g.Value (1, 3, 1);
            g.Header (2, 2, "Lyon"); g.Value (2, 3, 2);
            g.Header (3, 1, "Spain"); g.Header (3, 2, "Madrid"); g.Value (3, 3, 3);
            g.Header (4, 0, "Asia"); g.Header (4, 1, "Japan"); g.Header (4, 2, "Tokyo"); g.Value (4, 3, 4);

            Assert.That (Paths (await g.Read (1, 3)), Is.EqualTo (new[] {
                "Europe/France/Paris=1", "Europe/France/Lyon=2", "Europe/Spain/Madrid=3", "Asia/Japan/Tokyo=4" }));
        }

        [Test]
        public async Task ReadGrid_SeveralSeries_EachCellIsAPoint_AndMissingCellsAreSkipped ()
        {
            // Two series header rows (two series groupings), two series columns; B has no value in the second.
            var g = new Grid (4, 3);
            g.Header (0, 1, "2024"); g.Header (0, 2, "2025");
            g.Header (1, 1, "Q1"); g.Header (1, 2, "Q2");
            g.Header (2, 0, "A"); g.Value (2, 1, 1); g.Value (2, 2, 2);
            g.Header (3, 0, "B"); g.Value (3, 1, 3);

            Assert.That (Paths (await g.Read (2, 1)), Is.EqualTo (new[] { "A=1", "A=2", "B=3" }));
            Assert.That (Paths (await g.Read (2, 1, series: true)), Is.EqualTo (new[] { "A/Q1=1", "A/Q2=2", "B/Q1=3" }),
                "the innermost series header names the series");
        }

        #endregion

        #region Through a report

        private static string Report (string type, int levels)
        {
            string[] fields = { "Region", "Country", "City" };
            var groupings = string.Concat (fields.Take (levels).Select (f => $@"
        <CategoryGrouping><DynamicCategories><Grouping Name=""By{f}""><GroupExpressions><GroupExpression>=Fields!{f}.Value</GroupExpression></GroupExpressions></Grouping></DynamicCategories></CategoryGrouping>"));
            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <DataSources><DataSource Name=""DS1""><ConnectionProperties><DataProvider>SQLite</DataProvider><ConnectString>Data Source=none.db</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>DS1</DataSourceName><CommandText>/* Local Query */</CommandText></Query>
    <Fields><Field Name=""Region""><DataField>Region</DataField></Field><Field Name=""Country""><DataField>Country</DataField></Field><Field Name=""City""><DataField>City</DataField></Field><Field Name=""Amount""><DataField>Amount</DataField><TypeName>System.Double</TypeName></Field></Fields>
  </DataSet></DataSets>
  <Body><Height>3in</Height><ReportItems>
    <Chart Name=""Chart1""><DataSetName>Data</DataSetName><Top>0in</Top><Left>0in</Left><Width>4in</Width><Height>2.5in</Height>
      <Type>{type}</Type><Subtype>Plain</Subtype>
      <CategoryGroupings>{groupings}
      </CategoryGroupings>
      <ChartData><ChartSeries><DataPoints><DataPoint><DataValues><DataValue><Value>=Sum(Fields!Amount.Value)</Value></DataValue></DataValues></DataPoint></DataPoints></ChartSeries></ChartData>
      <Title><Caption>Sales</Caption></Title>
    </Chart>
  </ReportItems></Body>
  <Width>6in</Width>
</Report>";
        }

        private static DataTable Data (bool withNonPositive = false)
        {
            var table = new DataTable ();
            table.Columns.Add ("Region", typeof (string));
            table.Columns.Add ("Country", typeof (string));
            table.Columns.Add ("City", typeof (string));
            table.Columns.Add ("Amount", typeof (double));
            table.Rows.Add ("Asia", "Japan", "Tokyo", 8.0);
            table.Rows.Add ("Europe", "France", "Paris", 1.0);
            table.Rows.Add ("Europe", "France", "Lyon", 2.0);
            table.Rows.Add ("Europe", "Spain", "Madrid", 4.0);
            table.Rows.Add ("Europe", "France", "Paris", 0.5);
            if (withNonPositive) {
                table.Rows.Add ("Asia", "China", "Beijing", -3.0);
                table.Rows.Add ("Asia", "Korea", "Seoul", 0.0);
            }
            return table;
        }

        private static async Task<Report> Parse (string rdl, DataTable data)
        {
            RdlEngineConfig.RdlEngineConfigInit ();
            var parser = new RDLParser (rdl) { SkipDatabaseSchemaValidation = true };
            var report = await parser.Parse ();
            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4), string.Join (" | ", Messages (report)));
            await report.DataSets["Data"].SetData (data);
            await report.RunGetData (null);
            return report;
        }

        private static string[] Messages (Report report) => report.ErrorItems?.Cast<string> ().ToArray () ?? Array.Empty<string> ();

        /// <summary>Builds the chart as rendering does, and reads its hierarchy.</summary>
        private static async Task<ChartHierarchy> Hierarchy (Report report)
        {
            var chart = (Chart) report.ReportDefinition.Body.ReportItems.Items[0];
            chart.ChartMatrix.RunReset (report);
            chart.SetMyData (report, await chart.GetFilteredData (report, null));
            using var cb = await chart.RunChartBuild (report, null);
            return await cb.GetHierarchyAsync (report);
        }

        private static string[] Leaves (ChartHierarchyNode node)
            => node.IsLeaf ? new[] { string.Join ("/", node.Path) + "=" + node.Value.ToString (CultureInfo.InvariantCulture) }
                : node.Children.SelectMany (Leaves).ToArray ();

        [TestCase ("TreeMap")]
        [TestCase ("Sunburst")]
        public async Task TheChartGrid_GivesEachPointItsFullPath (string type)
        {
            using var report = await Parse (Report (type, 3), Data ());

            var h = await Hierarchy (report);

            Assert.That (h.Depth, Is.EqualTo (3));
            Assert.That (Leaves (h.Root), Is.EquivalentTo (new[] {
                "Asia/Japan/Tokyo=8", "Europe/France/Paris=1.5", "Europe/France/Lyon=2", "Europe/Spain/Madrid=4" }));
            Assert.That (h.Root.Children.Single (c => c.Label == "Europe").Value, Is.EqualTo (7.5));
        }

        [Test]
        public async Task TwoLevels_ThroughAReport ()
        {
            using var report = await Parse (Report ("TreeMap", 2), Data ());

            var h = await Hierarchy (report);

            Assert.That (h.Depth, Is.EqualTo (2));
            Assert.That (Leaves (h.Root), Is.EquivalentTo (new[] { "Asia/Japan=8", "Europe/France=3.5", "Europe/Spain=4" }));
        }

        [Test]
        public async Task OneLevel_ThroughAReport ()
        {
            using var report = await Parse (Report ("Sunburst", 1), Data ());

            var h = await Hierarchy (report);

            Assert.That (Leaves (h.Root), Is.EquivalentTo (new[] { "Asia=8", "Europe=7.5" }));
        }

        [Test]
        public async Task NonPositiveValues_AreLeftOutAndLoggedOnce ()
        {
            using var report = await Parse (Report ("TreeMap", 3), Data (withNonPositive: true));

            var h = await Hierarchy (report);

            Assert.That (h.LeftOutCount, Is.EqualTo (2));
            Assert.That (Leaves (h.Root), Has.None.Contains ("China").And.None.Contains ("Korea"));
            Assert.That (Messages (report).Count (m => m.Contains ("zero or negative")), Is.EqualTo (1));
        }

        #endregion

        #region Rendering the placeholder

        [TestCase ("TreeMap", OutputPresentationType.HTML)]
        [TestCase ("TreeMap", OutputPresentationType.PDF)]
        [TestCase ("TreeMap", OutputPresentationType.SVG)]
        [TestCase ("Sunburst", OutputPresentationType.HTML)]
        [TestCase ("Sunburst", OutputPresentationType.PDF)]
        [TestCase ("Sunburst", OutputPresentationType.SVG)]
        public async Task TheNewTypes_RenderWithoutErrors (string type, OutputPresentationType output)
        {
            using var report = await Parse (Report (type, 2), Data ());

            using var memory = new MemoryStreamGen ();
            await report.RunRender (memory, output);

            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4), string.Join (" | ", Messages (report)));
            Assert.That (Messages (report).Any (m => m.Contains ("Exception") || m.Contains ("not currently supported")), Is.False,
                string.Join (" | ", Messages (report)));
            Assert.That (memory.GetStream ().Length, Is.GreaterThan (0));
        }

        [TestCase ("TreeMap")]
        [TestCase ("Sunburst")]
        public void TheTypeNames_Parse (string type)
        {
            Assert.That (ChartType.GetStyle (type).ToString (), Is.EqualTo (type));
        }

        #endregion
    }
}
