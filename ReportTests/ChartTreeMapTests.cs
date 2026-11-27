using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// The TreeMap chart (#639): the squarified layout, and the chart drawn from a one-, two- and
    /// three-level category hierarchy.
    /// </summary>
    [TestFixture]
    public class ChartTreeMapTests
    {
        private const double Tolerance = 1e-3;

        #region The squarified layout

        private static double Area (RectangleF r) => (double) r.Width * r.Height;

        private static double Aspect (RectangleF r) => Math.Max (r.Width, r.Height) / Math.Min (r.Width, r.Height);

        /// <summary>Each value gets an area in proportion to it, and the rectangles tile the bounds.</summary>
        private static void AssertTiles (IReadOnlyList<double> values, RectangleF bounds, RectangleF[] cells)
        {
            Assert.That (cells, Has.Length.EqualTo (values.Count));
            double total = values.Where (v => v > 0).Sum ();
            double boundsArea = Area (bounds);
            const float eps = 0.01f;

            for (int i = 0; i < values.Count; i++) {
                double expected = values[i] > 0 ? values[i] / total * boundsArea : 0;
                Assert.That (Area (cells[i]), Is.EqualTo (expected).Within (boundsArea * Tolerance), $"the area of value {i} ({values[i]})");
                if (values[i] <= 0)
                    continue;
                Assert.That (cells[i].Left, Is.GreaterThanOrEqualTo (bounds.Left - eps), $"cell {i} inside");
                Assert.That (cells[i].Top, Is.GreaterThanOrEqualTo (bounds.Top - eps), $"cell {i} inside");
                Assert.That (cells[i].Right, Is.LessThanOrEqualTo (bounds.Right + eps), $"cell {i} inside");
                Assert.That (cells[i].Bottom, Is.LessThanOrEqualTo (bounds.Bottom + eps), $"cell {i} inside");
            }

            for (int i = 0; i < cells.Length; i++)
                for (int j = i + 1; j < cells.Length; j++) {
                    var overlap = RectangleF.Intersect (cells[i], cells[j]);
                    Assert.That (overlap.IsEmpty ? 0 : Area (overlap), Is.LessThan (boundsArea * Tolerance), $"cells {i} and {j} overlap");
                }

            Assert.That (cells.Sum (Area), Is.EqualTo (boundsArea).Within (boundsArea * Tolerance), "nothing is left uncovered");
        }

        [Test]
        public void TheExampleFromThePaper_TilesWithProportionalAreas ()
        {
            // Bruls, Huizing and van Wijk's worked example: 6, 6, 4, 3, 2, 2, 1 in a 6 x 4 rectangle.
            double[] values = { 6, 6, 4, 3, 2, 2, 1 };
            var bounds = new RectangleF (0, 0, 6, 4);

            var cells = ChartTreeMap.ComputeSquarified (values, bounds);

            AssertTiles (values, bounds, cells);
            // The two 6s go first, as a column of two 3 x 2 cells on the left.
            Assert.That (cells[0].X, Is.EqualTo (0).Within (Tolerance));
            Assert.That (cells[0].Width, Is.EqualTo (3).Within (Tolerance));
            Assert.That (cells[0].Height, Is.EqualTo (2).Within (Tolerance));
            Assert.That (cells[1].Y, Is.EqualTo (2).Within (Tolerance));
            Assert.That (cells.Max (Aspect), Is.LessThan (3), "the paper's layout has no cell worse than 3:1 (the 1 is 2.25:1)");
        }

        [TestCase (1)]
        [TestCase (2)]
        [TestCase (7)]
        [TestCase (30)]
        [TestCase (200)]
        public void RandomValues_TileTheBounds (int seed)
        {
            var random = new Random (seed);
            int n = 1 + random.Next (40);
            double[] values = Enumerable.Range (0, n).Select (_ => Math.Pow (random.NextDouble (), 2) * 1000 + 0.01).ToArray ();
            var bounds = new RectangleF (12.5f, 30, 410, 237);

            AssertTiles (values, bounds, ChartTreeMap.ComputeSquarified (values, bounds));
        }

        [Test]
        public void TheCells_AreFarSquarerThanSlices ()
        {
            double[] values = Enumerable.Range (1, 20).Select (i => (double) i).ToArray ();
            var bounds = new RectangleF (0, 0, 400, 300);

            var cells = ChartTreeMap.ComputeSquarified (values, bounds);

            // Slicing the width in proportion to the values, as a slice-and-dice layout's first level does.
            double total = values.Sum ();
            double sliceWorst = values.Max (v => Aspect (new RectangleF (0, 0, (float) (v / total * 400), 300)));
            double sliceMean = values.Average (v => Aspect (new RectangleF (0, 0, (float) (v / total * 400), 300)));
            double worst = cells.Max (Aspect);
            double mean = cells.Average (Aspect);

            Assert.That (worst, Is.LessThan (sliceWorst / 10), $"worst aspect {worst:F2} vs {sliceWorst:F2} sliced");
            Assert.That (mean, Is.LessThan (2), $"mean aspect {mean:F2} (sliced {sliceMean:F2})");
        }

        [Test]
        public void OneValue_FillsTheBounds ()
        {
            var bounds = new RectangleF (5, 6, 70, 80);

            var cells = ChartTreeMap.ComputeSquarified (new[] { 42.0 }, bounds);

            Assert.That (cells.Single (), Is.EqualTo (bounds));
        }

        [Test]
        public void NoValues_AreNoCells ()
        {
            Assert.That (ChartTreeMap.ComputeSquarified (Array.Empty<double> (), new RectangleF (0, 0, 10, 10)), Is.Empty);
            Assert.That (ChartTreeMap.ComputeSquarified (null, new RectangleF (0, 0, 10, 10)), Is.Empty);
        }

        [Test]
        public void ZeroNegativeAndNonFiniteValues_GetEmptyCells_AndTheRestStillTile ()
        {
            double[] values = { 5, 0, -3, double.NaN, double.PositiveInfinity, 3, 2 };
            var bounds = new RectangleF (0, 0, 100, 50);

            var cells = ChartTreeMap.ComputeSquarified (values, bounds);

            Assert.That (new[] { 1, 2, 3, 4 }.Select (i => Area (cells[i])), Is.All.EqualTo (0));
            double[] kept = { 5, 3, 2 };
            AssertTiles (kept, bounds, new[] { cells[0], cells[5], cells[6] });
        }

        [Test]
        public void EmptyBounds_GiveEmptyCells ()
        {
            var cells = ChartTreeMap.ComputeSquarified (new[] { 1.0, 2.0 }, new RectangleF (3, 4, 0, 20));

            Assert.That (cells.Select (Area), Is.All.EqualTo (0));
        }

        [Test]
        public void EqualValues_InASquare_AreEqualCells ()
        {
            var cells = ChartTreeMap.ComputeSquarified (new[] { 1.0, 1.0, 1.0, 1.0 }, new RectangleF (0, 0, 100, 100));

            Assert.That (cells.Select (c => Math.Round (c.Width, 3)), Is.All.EqualTo (50));
            Assert.That (cells.Select (c => Math.Round (c.Height, 3)), Is.All.EqualTo (50));
        }

        #endregion

        #region Labels and tooltips

        // One unit of width per character.
        private static float Chars (string s) => s.Length;

        [Test]
        public void ALabelThatFits_IsKeptWhole ()
        {
            Assert.That (ChartTreeMap.FitLabel ("Europe", 6, Chars), Is.EqualTo ("Europe"));
        }

        [Test]
        public void ALongLabel_IsCutShortWithAnEllipsis ()
        {
            Assert.That (ChartTreeMap.FitLabel ("Europe", 4, Chars), Is.EqualTo ("Eur…"));
        }

        [Test]
        public void ALabelWithNoRoom_IsLeftOut ()
        {
            Assert.That (ChartTreeMap.FitLabel ("Europe", 1.5f, Chars), Is.Empty);
            Assert.That (ChartTreeMap.FitLabel ("Europe", 0, Chars), Is.Empty);
            Assert.That (ChartTreeMap.FitLabel ("", 10, Chars), Is.Empty);
        }

        [Test]
        public void TheToolTip_CarriesThePathAndValue_WithoutTheCommentsSeparators ()
        {
            var h = ChartHierarchy.Build (new[] {
                new ChartHierarchyPoint (new[] { "Europe", "France", "Paris|Centre: 1" }, 1234.5),
            });
            var leaf = h.Root.Children[0].Children[0].Children[0];

            string text = ChartTreeMap.ToolTipText (leaf, "N1");

            Assert.That (text, Does.StartWith ("Europe / France / Paris/Centre. 1 = "));
            Assert.That (text, Does.EndWith (1234.5.ToString ("N1")));
            Assert.That (text, Does.Not.Contain ("|").And.Not.Contain (":"));
            Assert.That (ChartTreeMap.ToolTipText (h.Root.Children[0], null), Is.EqualTo ("Europe = " + 1234.5.ToString ()));
        }

        #endregion

        #region Through a report

        private static string Report (int levels, string palette = "Default", string width = "5in", string height = "3.5in")
        {
            string[] fields = { "Region", "Country", "City" };
            var groupings = string.Concat (fields.Take (levels).Select (f => $@"
        <CategoryGrouping><DynamicCategories><Grouping Name=""By{f}""><GroupExpressions><GroupExpression>=Fields!{f}.Value</GroupExpression></GroupExpressions></Grouping><Label>=Fields!{f}.Value</Label></DynamicCategories></CategoryGrouping>"));
            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <DataSources><DataSource Name=""DS1""><ConnectionProperties><DataProvider>SQLite</DataProvider><ConnectString>Data Source=none.db</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>DS1</DataSourceName><CommandText>/* Local Query */</CommandText></Query>
    <Fields><Field Name=""Region""><DataField>Region</DataField></Field><Field Name=""Country""><DataField>Country</DataField></Field><Field Name=""City""><DataField>City</DataField></Field><Field Name=""Amount""><DataField>Amount</DataField><TypeName>System.Double</TypeName></Field></Fields>
  </DataSet></DataSets>
  <Body><Height>4in</Height><ReportItems>
    <Chart Name=""Chart1""><DataSetName>Data</DataSetName><Top>0in</Top><Left>0in</Left><Width>{width}</Width><Height>{height}</Height>
      <Type>TreeMap</Type><Subtype>Plain</Subtype><Palette>{palette}</Palette>
      <CategoryGroupings>{groupings}
      </CategoryGroupings>
      <ChartData><ChartSeries><DataPoints><DataPoint><DataValues><DataValue><Value>=Sum(Fields!Amount.Value)</Value></DataValue></DataValues></DataPoint></DataPoints></ChartSeries></ChartData>
      <Title><Caption>Sales</Caption></Title>
    </Chart>
  </ReportItems></Body>
  <Width>6in</Width>
</Report>";
        }

        private static DataTable Data (bool withNonPositive = false, bool onlyNonPositive = false)
        {
            var table = new DataTable ();
            table.Columns.Add ("Region", typeof (string));
            table.Columns.Add ("Country", typeof (string));
            table.Columns.Add ("City", typeof (string));
            table.Columns.Add ("Amount", typeof (double));
            if (!onlyNonPositive) {
                table.Rows.Add ("Asia", "Japan", "Tokyo", 30.0);
                table.Rows.Add ("Asia", "Japan", "Osaka", 12.0);
                table.Rows.Add ("Asia", "China", "Shanghai", 25.0);
                table.Rows.Add ("Asia", "China", "Beijing", 18.0);
                table.Rows.Add ("Asia", "India", "Mumbai", 15.0);
                table.Rows.Add ("Europe", "France", "Paris", 22.0);
                table.Rows.Add ("Europe", "France", "Lyon", 7.0);
                table.Rows.Add ("Europe", "Spain", "Madrid", 14.0);
                table.Rows.Add ("Europe", "Spain", "Barcelona", 11.0);
                table.Rows.Add ("Europe", "Germany", "Berlin", 16.0);
                table.Rows.Add ("Europe", "Germany", "Munich", 9.0);
                table.Rows.Add ("Americas", "Canada", "Toronto", 13.0);
                table.Rows.Add ("Americas", "Canada", "St. John's", 4.0);
                table.Rows.Add ("Americas", "USA", "New York", 24.0);
                table.Rows.Add ("Americas", "Brazil", "São Paulo", 10.0);
            }
            if (withNonPositive || onlyNonPositive) {
                table.Rows.Add ("Asia", "Korea", "Seoul", -3.0);
                table.Rows.Add ("Africa", "Kenya", "Nairobi", 0.0);
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

        private static void AssertDrewCleanly (Report report)
        {
            string all = string.Join (" | ", Messages (report));
            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4), all);
            Assert.That (Messages (report).Any (m => m.Contains ("Exception") || m.Contains ("stopped early") || m.Contains ("not currently supported")),
                Is.False, all);
        }

        private static IEnumerable<TestCaseData> LevelsAndOutputs ()
        {
            foreach (int levels in new[] { 1, 2, 3 })
                foreach (var output in new[] { OutputPresentationType.PDF, OutputPresentationType.HTML, OutputPresentationType.SVG, OutputPresentationType.TIF })
                    yield return new TestCaseData (levels, output);
        }

        [TestCaseSource (nameof (LevelsAndOutputs))]
        public async Task OneTwoAndThreeLevels_Render (int levels, OutputPresentationType output)
        {
            using var report = await Parse (Report (levels), Data ());

            using var memory = new MemoryStreamGen ();
            await report.RunRender (memory, output);

            AssertDrewCleanly (report);
            Assert.That (memory.GetStream ().Length, Is.GreaterThan (0));
        }

        [TestCase ("Default")]
        [TestCase ("Pastel")]
        [TestCase ("GrayScale")]
        [TestCase ("Patterned")]
        [TestCase ("Custom")]
        public async Task EveryPalette_Renders (string palette)
        {
            using var report = await Parse (Report (3, palette), Data ());

            var pages = await report.BuildPages ();

            AssertDrewCleanly (report);
            Assert.That (pages[0].OfType<PageImage> ().Single ().GetImageData (), Is.Not.Empty);
        }

        [Test]
        public async Task ZeroAndNegativeValues_AreLeftOut_AndLoggedOnce ()
        {
            using var report = await Parse (Report (3), Data (withNonPositive: true));

            using var memory = new MemoryStreamGen ();
            await report.RunRender (memory, OutputPresentationType.PDF);

            AssertDrewCleanly (report);
            Assert.That (Messages (report).Count (m => m.Contains ("zero or negative")), Is.EqualTo (1));
        }

        [Test]
        public async Task NothingLeftIn_IsAnEmptyPlot ()
        {
            using var report = await Parse (Report (2), Data (onlyNonPositive: true));

            var pages = await report.BuildPages ();

            AssertDrewCleanly (report);
            using var bitmap = Decode (pages);
            Assert.That (Coloured (bitmap), Is.EqualTo (0), "no cell is drawn");
        }

        [Test]
        public async Task ATinyChart_DrawsWithoutLabelsOrErrors ()
        {
            using var report = await Parse (Report (3, width: "0.6in", height: "0.6in"), Data ());

            var pages = await report.BuildPages ();

            AssertDrewCleanly (report);
        }

        private static SkiaSharp.SKBitmap Decode (Pages pages)
            => SkiaSharp.SKBitmap.Decode (pages[0].OfType<PageImage> ().Single ().GetImageData ());

        /// <summary>The pixels of a strong colour (not white, grey or black), as the cells are filled.</summary>
        private static int Coloured (SkiaSharp.SKBitmap bitmap, Func<SkiaSharp.SKColor, bool>? which = null)
        {
            int count = 0;
            for (int y = 0; y < bitmap.Height; y++)
                for (int x = 0; x < bitmap.Width; x++) {
                    var c = bitmap.GetPixel (x, y);
                    int max = Math.Max (c.Red, Math.Max (c.Green, c.Blue)), min = Math.Min (c.Red, Math.Min (c.Green, c.Blue));
                    if (max - min > 80 && (which == null || which (c)))
                        count++;
                }
            return count;
        }

        [Test]
        public async Task TheCells_FillThePlot_InEachTopLevelCategorysColour ()
        {
            using var report = await Parse (Report (3), Data ());

            var pages = await report.BuildPages ();

            AssertDrewCleanly (report);
            using var bitmap = Decode (pages);
            int total = bitmap.Width * bitmap.Height;

            // The default (Excel) palette's first three colours, blue, red and green, go to the regions
            // in their grouping's order: Americas, Asia, Europe.
            int blue = Coloured (bitmap, c => c.Blue > c.Red + 60 && c.Blue > c.Green + 60);
            int red = Coloured (bitmap, c => c.Red > c.Green + 60 && c.Red > c.Blue + 60);
            int green = Coloured (bitmap, c => c.Green > c.Red + 60 && c.Green > c.Blue + 60);

            Assert.That ((double) (blue + red + green) / total, Is.GreaterThan (0.5), "the cells cover most of the chart");
            foreach (var (name, count) in new[] { ("blue", blue), ("red", red), ("green", green) })
                Assert.That ((double) count / total, Is.GreaterThan (0.1), $"{name} covers {(double) count / total:P0}");
            // Asia 100, Europe 79, Americas 51: the areas follow the values, less the headers and borders.
            Assert.That (red, Is.GreaterThan (green), "Asia (100) is larger than Europe (79)");
            Assert.That (green, Is.GreaterThan (blue), "Europe (79) is larger than the Americas (51)");
        }

        #endregion
    }
}
