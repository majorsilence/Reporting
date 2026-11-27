using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// The Sunburst chart's drawing (#639): the angle of every slice, and the rings through a report.
    /// </summary>
    [TestFixture]
    public class ChartSunburstTests
    {
        private const double Tolerance = 1e-9;

        private static ChartHierarchyPoint P (double value, params string[] path) => new ChartHierarchyPoint (path, value);

        private static ChartHierarchy ThreeLevels () => ChartHierarchy.Build (new[] {
            P (1, "Europe", "France", "Paris"), P (2, "Europe", "France", "Lyon"),
            P (4, "Europe", "Spain", "Madrid"), P (8, "Asia", "Japan", "Tokyo"),
            P (3, "Asia", "China", "Beijing"), P (2, "Asia", "China", "Shanghai"),
        });

        #region The angles

        [Test]
        public void EveryRing_IsAWholeTurn ()
        {
            var slices = ChartSunburst.ComputeSunburstSlices (ThreeLevels ());

            Assert.That (slices.Select (s => s.Ring).Distinct ().OrderBy (r => r), Is.EqualTo (new[] { 0, 1, 2 }));
            for (int ring = 0; ring < 3; ring++) {
                var onRing = slices.Where (s => s.Ring == ring).ToList ();
                Assert.That (onRing.Sum (s => s.SweepAngle), Is.EqualTo (360).Within (Tolerance), $"ring {ring}");
                Assert.That (onRing[0].StartAngle, Is.EqualTo (ChartSunburst.FirstAngle).Within (Tolerance), "each ring starts at twelve o'clock");
                for (int i = 1; i < onRing.Count; i++)
                    Assert.That (onRing[i].StartAngle, Is.EqualTo (onRing[i - 1].EndAngle).Within (Tolerance), "slices follow one another without gaps");
            }
        }

        [Test]
        public void SliceAngles_AreProportionalToTheirValues ()
        {
            var h = ThreeLevels ();
            foreach (var s in ChartSunburst.ComputeSunburstSlices (h))
                Assert.That (s.SweepAngle, Is.EqualTo (360 * s.Node.Value / h.Total).Within (Tolerance), string.Join ("/", s.Node.Path));
        }

        [Test]
        public void Children_SitInsideTheirParent_InOrder ()
        {
            var slices = ChartSunburst.ComputeSunburstSlices (ThreeLevels ());
            var byNode = slices.ToDictionary (s => s.Node);

            foreach (var s in slices.Where (s => s.Ring > 0)) {
                var parent = byNode[s.Node.Parent!];
                Assert.That (s.Ring, Is.EqualTo (parent.Ring + 1));
                Assert.That (s.StartAngle, Is.GreaterThanOrEqualTo (parent.StartAngle - Tolerance));
                Assert.That (s.EndAngle, Is.LessThanOrEqualTo (parent.EndAngle + Tolerance));
            }

            var europe = byNode.Keys.Single (n => n.Label == "Europe");
            var children = europe.Children.Select (c => byNode[c]).ToList ();
            Assert.That (children[0].StartAngle, Is.EqualTo (byNode[europe].StartAngle).Within (Tolerance), "the first child starts with its parent");
            Assert.That (children[^1].EndAngle, Is.EqualTo (byNode[europe].EndAngle).Within (Tolerance), "the last child ends with its parent");
            Assert.That (children.Select (c => c.Node.Label), Is.EqualTo (new[] { "France", "Spain" }), "the hierarchy's order is kept");
        }

        [Test]
        public void KnownAngles ()
        {
            // Europe 7 of 20 → 126°, Asia 13 of 20 → 234°; France 3 of 20 → 54°.
            var slices = ChartSunburst.ComputeSunburstSlices (ThreeLevels (), firstAngle: 0);
            var byLabel = slices.ToDictionary (s => string.Join ("/", s.Node.Path));

            Assert.That (byLabel["Europe"].SweepAngle, Is.EqualTo (126).Within (Tolerance));
            Assert.That (byLabel["Asia"].StartAngle, Is.EqualTo (126).Within (Tolerance));
            Assert.That (byLabel["Asia"].SweepAngle, Is.EqualTo (234).Within (Tolerance));
            Assert.That (byLabel["Europe/France"].SweepAngle, Is.EqualTo (54).Within (Tolerance));
            Assert.That (byLabel["Europe/Spain"].StartAngle, Is.EqualTo (54).Within (Tolerance));
            Assert.That (byLabel["Europe/France/Lyon"].StartAngle, Is.EqualTo (18).Within (Tolerance));
        }

        [Test]
        public void OneLevel_IsOneRing_LikeADoughnut ()
        {
            var slices = ChartSunburst.ComputeSunburstSlices (ChartHierarchy.Build (new[] { P (1, "A"), P (3, "B") }));

            Assert.That (slices.Select (s => s.Ring), Is.All.EqualTo (0));
            Assert.That (slices.Select (s => s.SweepAngle), Is.EqualTo (new[] { 90.0, 270.0 }).Within (Tolerance));
        }

        [Test]
        public void OneCategory_IsAWholeRing ()
        {
            var slices = ChartSunburst.ComputeSunburstSlices (ChartHierarchy.Build (new[] { P (5, "Only") }));

            Assert.That (slices.Single ().SweepAngle, Is.EqualTo (360).Within (Tolerance));
        }

        [Test]
        public void ZeroAndNegativeValues_GetNoSlice ()
        {
            var h = ChartHierarchy.Build (new[] { P (2, "A", "x"), P (0, "A", "y"), P (-5, "B", "z"), P (2, "C", "w") });
            var slices = ChartSunburst.ComputeSunburstSlices (h);

            Assert.That (slices.Select (s => string.Join ("/", s.Node.Path)), Is.EquivalentTo (new[] { "A", "A/x", "C", "C/w" }));
            Assert.That (slices.Where (s => s.Ring == 0).Select (s => s.SweepAngle), Is.EqualTo (new[] { 180.0, 180.0 }).Within (Tolerance));
        }

        [Test]
        public void AnEmptyHierarchy_HasNoSlices ()
        {
            Assert.That (ChartSunburst.ComputeSunburstSlices (ChartHierarchy.Build (Array.Empty<ChartHierarchyPoint> ())), Is.Empty);
            Assert.That (ChartSunburst.ComputeSunburstSlices (ChartHierarchy.Build (new[] { P (-1, "A") })), Is.Empty);
            Assert.That (ChartSunburst.ComputeSunburstSlices (null!), Is.Empty);
        }

        #endregion

        #region Ring sectors, labels and tooltips

        [Test]
        public void RingSector_RunsAlongTheOuterArcAndBackAlongTheInner ()
        {
            var pts = ChartSunburst.RingSector (0, 0, 10, 20, 0, 90, 45);

            Assert.That (pts.Length, Is.EqualTo (6));
            Assert.That (pts[0].X, Is.EqualTo (20).Within (1e-4)); Assert.That (pts[0].Y, Is.EqualTo (0).Within (1e-4));
            Assert.That (pts[2].X, Is.EqualTo (0).Within (1e-4)); Assert.That (pts[2].Y, Is.EqualTo (20).Within (1e-4));
            Assert.That (pts[3].X, Is.EqualTo (0).Within (1e-4)); Assert.That (pts[3].Y, Is.EqualTo (10).Within (1e-4));
            Assert.That (pts[5].X, Is.EqualTo (10).Within (1e-4)); Assert.That (pts[5].Y, Is.EqualTo (0).Within (1e-4));
        }

        [Test]
        public void ALabel_FitsOnlyInsideItsSlice ()
        {
            // A 10x6 box centred at radius 30, straight below the centre (90°).
            var box = new System.Drawing.RectangleF (-5, 27, 10, 6);

            Assert.That (ChartSunburst.LabelFits (box, 0, 0, 20, 40, 45, 90), Is.True);
            Assert.That (ChartSunburst.LabelFits (box, 0, 0, 28, 40, 45, 90), Is.False, "the ring is too thin");
            Assert.That (ChartSunburst.LabelFits (box, 0, 0, 20, 40, 88, 4), Is.False, "the slice is too narrow");
            Assert.That (ChartSunburst.LabelFits (box, 0, 0, 20, 40, -90, 360), Is.True, "a whole ring only needs the width");
        }

        [Test]
        public void TheToolTip_CarriesTheFullPathAndValue_WithoutTheCommentSeparators ()
        {
            var h = ChartHierarchy.Build (new[] { P (1.5, "Europe", "France", "Paris|Nord"), P (2, "Asia", "Japan:East", "Tokyo") });
            var paris = h.Root.Children[0].Children[0].Children[0];
            var japan = h.Root.Children[1].Children[0];

            Assert.That (ChartSunburst.ToolTipText (paris, "0.00"), Is.EqualTo ("Europe / France / Paris/Nord = " + 1.5.ToString ("0.00")));
            Assert.That (ChartSunburst.ToolTipText (japan, ""), Does.Not.Contain (":").And.StartWith ("Asia / Japan East"));
        }

        #endregion

        #region Through a report

        private static string Report (int levels, bool title = true, bool tooltips = false)
        {
            string[] fields = { "Region", "Country", "City" };
            var groupings = string.Concat (fields.Take (levels).Select (f => $@"
        <CategoryGrouping><DynamicCategories><Grouping Name=""By{f}""><GroupExpressions><GroupExpression>=Fields!{f}.Value</GroupExpression></GroupExpressions></Grouping></DynamicCategories></CategoryGrouping>"));
            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"" xmlns:fyi=""http://www.fyireporting.com/schemas"">
  <DataSources><DataSource Name=""DS1""><ConnectionProperties><DataProvider>SQLite</DataProvider><ConnectString>Data Source=none.db</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>DS1</DataSourceName><CommandText>/* Local Query */</CommandText></Query>
    <Fields><Field Name=""Region""><DataField>Region</DataField></Field><Field Name=""Country""><DataField>Country</DataField></Field><Field Name=""City""><DataField>City</DataField></Field><Field Name=""Amount""><DataField>Amount</DataField><TypeName>System.Double</TypeName></Field></Fields>
  </DataSet></DataSets>
  <Body><Height>4.5in</Height><ReportItems>
    <Chart Name=""Chart1""><DataSetName>Data</DataSetName><Top>0in</Top><Left>0in</Left><Width>4in</Width><Height>4in</Height>
      <Type>Sunburst</Type><Subtype>Plain</Subtype>{(tooltips ? "<fyi:Tooltip>true</fyi:Tooltip>" : "")}
      <CategoryGroupings>{groupings}
      </CategoryGroupings>
      <ChartData><ChartSeries><DataPoints><DataPoint><DataValues><DataValue><Value>=Sum(Fields!Amount.Value)</Value></DataValue></DataValues></DataPoint></DataPoints></ChartSeries></ChartData>
      {(title ? "<Title><Caption>Sales</Caption></Title>" : "")}
    </Chart>
  </ReportItems></Body>
  <Width>6in</Width>
</Report>";
        }

        private static DataTable Data (bool withNonPositive = false, bool empty = false)
        {
            var table = new DataTable ();
            table.Columns.Add ("Region", typeof (string));
            table.Columns.Add ("Country", typeof (string));
            table.Columns.Add ("City", typeof (string));
            table.Columns.Add ("Amount", typeof (double));
            if (empty)
                return table;
            table.Rows.Add ("Asia", "Japan", "Tokyo", 8.0);
            table.Rows.Add ("Asia", "Japan", "Osaka", 3.0);
            table.Rows.Add ("Asia", "China", "Beijing", 5.0);
            table.Rows.Add ("Europe", "France", "Paris", 4.0);
            table.Rows.Add ("Europe", "France", "Lyon", 2.0);
            table.Rows.Add ("Europe", "Spain", "Madrid", 4.0);
            table.Rows.Add ("Africa", "Kenya", "Nairobi", 3.0);
            if (withNonPositive) {
                table.Rows.Add ("Asia", "Korea", "Seoul", -3.0);
                table.Rows.Add ("Africa", "Egypt", "Cairo", 0.0);
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

        private static void AssertNoDrawingErrors (Report report)
        {
            var messages = Messages (report);
            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4), string.Join (" | ", messages));
            Assert.That (messages.Any (m => m.Contains ("Exception") || m.Contains ("stopped early") || m.Contains ("not currently supported")), Is.False,
                string.Join (" | ", messages));
        }

        private static IEnumerable<TestCaseData> Renders ()
        {
            foreach (int levels in new[] { 1, 2, 3 })
                foreach (var output in new[] { OutputPresentationType.PDF, OutputPresentationType.HTML, OutputPresentationType.SVG, OutputPresentationType.TIF })
                    yield return new TestCaseData (levels, output);
        }

        [TestCaseSource (nameof (Renders))]
        public async Task Renders_WithoutErrors (int levels, OutputPresentationType output)
        {
            using var report = await Parse (Report (levels, tooltips: levels == 3), Data ());

            using var memory = new MemoryStreamGen ();
            await report.RunRender (memory, output);

            AssertNoDrawingErrors (report);
            Assert.That (memory.GetStream ().Length, Is.GreaterThan (0));
        }

        [TestCase (OutputPresentationType.PDF)]
        [TestCase (OutputPresentationType.HTML)]
        public async Task ZeroAndNegativeValues_AreLeftOut_AndLoggedOnce (OutputPresentationType output)
        {
            using var report = await Parse (Report (3), Data (withNonPositive: true));

            using var memory = new MemoryStreamGen ();
            await report.RunRender (memory, output);

            AssertNoDrawingErrors (report);
            Assert.That (Messages (report).Count (m => m.Contains ("zero or negative")), Is.EqualTo (1));
        }

        [Test]
        public async Task NoData_DrawsAnEmptyPlot ()
        {
            using var report = await Parse (Report (2), Data (empty: true));

            var pages = await report.BuildPages ();

            AssertNoDrawingErrors (report);
            Assert.That (pages.Count, Is.GreaterThan (0));
        }

        private static async Task<SkiaSharp.SKBitmap> ChartBitmap (int levels)
        {
            using var report = await Parse (Report (levels, title: false), Data ());
            var pages = await report.BuildPages ();
            AssertNoDrawingErrors (report);
            var image = pages[0].OfType<PageImage> ().Single ();
            return SkiaSharp.SKBitmap.Decode (image.GetImageData ());
        }

        private static bool IsWhite (SkiaSharp.SKColor c) => c.Red > 245 && c.Green > 245 && c.Blue > 245;

        private static int Brightness (SkiaSharp.SKColor c) => c.Red + c.Green + c.Blue;

        [Test]
        public async Task ThreeLevels_DrawRingsAroundAnEmptyCentre ()
        {
            using var bitmap = await ChartBitmap (3);
            int cx = bitmap.Width / 2, cy = bitmap.Height / 2;
            float radius = Math.Min (bitmap.Width, bitmap.Height) / 2f;
            float ring = radius / 4;        // a hole and three rings, each as wide

            Assert.That (IsWhite (bitmap.GetPixel (cx, cy)), Is.True, "the centre is a hole");

            // Just right of twelve o'clock is the first top-level category, its country and city.
            double a = (ChartSunburst.FirstAngle + 3) * Math.PI / 180;
            SkiaSharp.SKColor At (float r) => bitmap.GetPixel (cx + (int) (r * Math.Cos (a)), cy + (int) (r * Math.Sin (a)));
            var inner = At (ring * 1.5f);
            var middle = At (ring * 2.5f);
            var outer = At (ring * 3.5f);

            Assert.That (IsWhite (inner) || IsWhite (middle) || IsWhite (outer), Is.False,
                $"every ring is filled: {inner} {middle} {outer}");
            Assert.That (Brightness (inner), Is.LessThan (Brightness (middle)), "rings further out are lighter shades");
            Assert.That (Brightness (middle), Is.LessThan (Brightness (outer)));

            int inked = 0;
            for (int y = 0; y < bitmap.Height; y++)
                for (int x = 0; x < bitmap.Width; x++)
                    if (!IsWhite (bitmap.GetPixel (x, y)))
                        inked++;
            double share = (double) inked / (bitmap.Width * bitmap.Height);
            Assert.That (share, Is.GreaterThan (0.4), $"the sunburst covers {share:P0} of the chart");
        }

        [Test]
        public async Task TopLevelCategories_HaveDifferentColours ()
        {
            using var bitmap = await ChartBitmap (1);
            int cx = bitmap.Width / 2, cy = bitmap.Height / 2;
            float r = Math.Min (bitmap.Width, bitmap.Height) / 2f * 0.75f;      // the middle of the single ring

            // The categories come sorted: Africa (3 of 29) from twelve o'clock, then Asia (16 of 29)
            // round the lower right, then Europe (10 of 29) on the left.
            var asia = bitmap.GetPixel (cx + (int) (r * 0.707), cy + (int) (r * 0.707));
            var europe = bitmap.GetPixel (cx - (int) r, cy);

            Assert.That (IsWhite (asia) || IsWhite (europe), Is.False, $"{asia} {europe}");
            Assert.That (asia, Is.Not.EqualTo (europe));
        }

        #endregion
    }
}
