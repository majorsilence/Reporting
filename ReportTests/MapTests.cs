using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using SkiaSharp;
using UglyToad.PdfPig;

namespace ReportTests
{
    /// <summary>
    /// The RDL 2010+ Map report item (issue #425): a choropleth of polygon shapes bound to a
    /// dataset is drawn; parts the engine does not draw keep a placeholder or are named in one
    /// warning.
    /// </summary>
    [TestFixture]
    public class MapTests
    {
        private const string Rdl2010 = "http://schemas.microsoft.com/sqlserver/reporting/2010/01/reportdefinition";

        // Four 10 by 10 degree squares, A bottom left, B bottom right, C top left, D top right, as
        // Report Builder embeds them: base64 SQL Server geography values (latitude first).
        private static readonly (string Name, string Vector)[] Squares =
        {
            ("A", "5hAAAAEEBQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACRAAAAAAAAAJEAAAAAAAAAkQAAAAAAAACRAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAQAAAAIAAAAAAQAAAP////8AAAAAAw=="),
            ("B", "5hAAAAEEBQAAAAAAAAAAAAAAAAAAAAAAJEAAAAAAAAAAAAAAAAAAADRAAAAAAAAAJEAAAAAAAAA0QAAAAAAAACRAAAAAAAAAJEAAAAAAAAAAAAAAAAAAACRAAQAAAAIAAAAAAQAAAP////8AAAAAAw=="),
            ("C", "5hAAAAEEBQAAAAAAAAAAACRAAAAAAAAAAAAAAAAAAAAkQAAAAAAAACRAAAAAAAAANEAAAAAAAAAkQAAAAAAAADRAAAAAAAAAAAAAAAAAAAAkQAAAAAAAAAAAAQAAAAIAAAAAAQAAAP////8AAAAAAw=="),
            ("D", "5hAAAAEEBQAAAAAAAAAAACRAAAAAAAAAJEAAAAAAAAAkQAAAAAAAADRAAAAAAAAANEAAAAAAAAA0QAAAAAAAADRAAAAAAAAAJEAAAAAAAAAkQAAAAAAAACRAAQAAAAIAAAAAAQAAAP////8AAAAAAw=="),
        };

        private static string Polygons () => string.Concat (Squares.Select (s =>
            $@"<MapPolygon><VectorData>{s.Vector}</VectorData><MapFields><MapField Name=""NAME""><Value>{s.Name}</Value></MapField></MapFields></MapPolygon>"));

        private static string Viewport (string projection = "Equirectangular")
            => $@"<MapViewport><MapCoordinateSystem>Geographic</MapCoordinateSystem><MapProjection>{projection}</MapProjection><MapDataBoundView /></MapViewport>";

        private const string DataRegion = @"<MapDataRegions><MapDataRegion Name=""Regions""><DataSetName>Sales</DataSetName>
            <MapMember><Group Name=""ByRegion""><GroupExpressions><GroupExpression>=Fields!Region.Value</GroupExpression></GroupExpressions></Group></MapMember>
          </MapDataRegion></MapDataRegions>";

        private static string PolygonLayer (string extra = "") => $@"<MapPolygonLayer Name=""Regions"">
            <MapDataRegionName>Regions</MapDataRegionName>
            <MapBindingFieldPairs><MapBindingFieldPair><FieldName>NAME</FieldName><BindingExpression>=Fields!Region.Value</BindingExpression></MapBindingFieldPair></MapBindingFieldPairs>
            <MapFieldDefinitions><MapFieldDefinition><Name>NAME</Name><DataType>String</DataType></MapFieldDefinition></MapFieldDefinitions>
            <MapPolygonTemplate><Style><BackgroundColor>Silver</BackgroundColor><Border><Color>Black</Color><Style>Solid</Style><Width>0.5pt</Width></Border></Style></MapPolygonTemplate>
            <MapPolygonRules><MapColorRangeRule>
              <DataValue>=Sum(Fields!Sales.Value)</DataValue><DistributionType>EqualInterval</DistributionType><BucketCount>3</BucketCount>
              <StartColor>Green</StartColor><MiddleColor>Yellow</MiddleColor><EndColor>Red</EndColor>
            </MapColorRangeRule></MapPolygonRules>
            <MapPolygons>{Polygons ()}</MapPolygons>
            {extra}
          </MapPolygonLayer>";

        private static string MapItem (string layers, string extra = "", string viewport = null) =>
            $@"<Map Name=""SalesMap""><Top>0.5in</Top><Left>0in</Left><Height>2in</Height><Width>2in</Width>
          {viewport ?? Viewport ()}
          {DataRegion}
          <MapLayers>{layers}</MapLayers>
          {extra}
        </Map>";

        private static string ReportWith (string item) => $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""{Rdl2010}"">
  <DataSources><DataSource Name=""DS1""><ConnectionProperties><DataProvider>SQLite</DataProvider><ConnectString>Data Source=none.db</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets>
    <DataSet Name=""Sales"">
      <Query><DataSourceName>DS1</DataSourceName><CommandText>/* Local Query */</CommandText></Query>
      <Fields><Field Name=""Region""><DataField>Region</DataField></Field><Field Name=""Sales""><DataField>Sales</DataField></Field></Fields>
    </DataSet>
    <DataSet Name=""Shapes"">
      <Query><DataSourceName>DS1</DataSourceName><CommandText>/* Local Query */</CommandText></Query>
      <Fields><Field Name=""Name""><DataField>Name</DataField></Field><Field Name=""Geo""><DataField>Geo</DataField></Field></Fields>
    </DataSet>
  </DataSets>
  <ReportSections><ReportSection>
    <Body><Height>3in</Height><ReportItems>
      <Textbox Name=""Before""><Top>0in</Top><Left>0in</Left><Height>0.25in</Height><Width>2in</Width><Paragraphs><Paragraph><TextRuns><TextRun><Value>Before map</Value><Style /></TextRun></TextRuns><Style /></Paragraph></Paragraphs></Textbox>
      {item}
    </ReportItems></Body>
    <Width>5in</Width>
  </ReportSection></ReportSections>
</Report>";

        /// <summary>A sums to 60 over two rows, B is 40, C is 15, D has no data.</summary>
        private static DataTable Sales ()
        {
            var table = new DataTable ();
            table.Columns.Add ("Region", typeof (string));
            table.Columns.Add ("Sales", typeof (double));
            table.Rows.Add ("A", 10.0);
            table.Rows.Add ("b ", 40.0);     // binding ignores case and surrounding spaces
            table.Rows.Add ("C", 15.0);
            table.Rows.Add ("A", 50.0);
            table.Rows.Add ("Z", 99.0);      // no shape
            return table;
        }

        private static string Wkt (double x0, double y0, double x1, double y1)
            => FormattableString.Invariant ($"POLYGON (({x0} {y0}, {x1} {y0}, {x1} {y1}, {x0} {y1}, {x0} {y0}))");

        private static DataTable Shapes ()
        {
            var table = new DataTable ();
            table.Columns.Add ("Name", typeof (string));
            table.Columns.Add ("Geo", typeof (string));
            table.Rows.Add ("A", Wkt (0, 0, 10, 10));
            table.Rows.Add ("B", Wkt (10, 0, 20, 10));
            table.Rows.Add ("C", Wkt (0, 10, 10, 20));
            table.Rows.Add ("D", Wkt (10, 10, 20, 20));
            return table;
        }

        [SetUp]
        public void SetUp () => RdlEngineConfig.RdlEngineConfigInit ();

        private static async Task<Report> Parse (string item)
        {
            var parser = new RDLParser (ReportWith (item)) { SkipDatabaseSchemaValidation = true };
            var report = await parser.Parse ();
            await report.DataSets["Sales"].SetData (Sales ());
            await report.DataSets["Shapes"].SetData (Shapes ());
            await report.RunGetData (null);
            return report;
        }

        private static string[] Messages (Report report)
            => report.ErrorItems == null ? Array.Empty<string> () : report.ErrorItems.Cast<string> ().ToArray ();

        private static async Task<SKBitmap> DrawnMap (Report report, int size = 400)
        {
            var pages = await report.BuildPages ();
            var image = pages[0].OfType<PageImage> ().Single ();
            return SKBitmap.Decode (image.GetImageData (size, size));
        }

        private static void AssertColour (SKBitmap bitmap, int x, int y, SKColor expected, string what)
        {
            var c = bitmap.GetPixel (x, y);
            Assert.That (Math.Abs (c.Red - expected.Red) + Math.Abs (c.Green - expected.Green) + Math.Abs (c.Blue - expected.Blue), Is.LessThan (12),
                $"{what}: expected {expected}, drawn {c}");
        }

        private static readonly SKColor Green = new SKColor (0, 128, 0);
        private static readonly SKColor Yellow = new SKColor (255, 255, 0);
        private static readonly SKColor Red = new SKColor (255, 0, 0);
        private static readonly SKColor Silver = new SKColor (192, 192, 192);

        [Test]
        public async Task EmbeddedPolygonMap_ParsesWithoutErrorsOrWarnings ()
        {
            using var report = await Parse (MapItem (PolygonLayer ()));

            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4), string.Join (" | ", Messages (report)));
            Assert.That (Messages (report).Where (m => m.Contains ("Map")), Is.Empty, "a map drawn in full needs no warning");
        }

        [Test]
        public void SupportedMap_StaysAMap_ForTheEngine ()
        {
            var doc = new XmlDocument ();
            doc.LoadXml (ReportWith (MapItem (PolygonLayer ())));
            var log = new ReportLog ();

            Rdl2008Normalizer.Normalize (doc, log);

            Assert.That (doc.GetElementsByTagName ("Map"), Has.Count.EqualTo (1));
            Assert.That (log.ErrorItems?.Cast<string> ().ToArray () ?? Array.Empty<string> (), Is.Empty);
        }

        [Test]
        public async Task ColourRule_ColoursEachShapeByItsGroupsSum ()
        {
            using var report = await Parse (MapItem (PolygonLayer ()));
            using var bitmap = await DrawnMap (report);

            // Equal intervals over 15..60: C (15) is the first bucket, B (40) the second, A (10 + 50) the last.
            // A summed over its own group's rows is what puts it in the red bucket.
            AssertColour (bitmap, 100, 300, Red, "A, bottom left");
            AssertColour (bitmap, 300, 300, Yellow, "B, bottom right");
            AssertColour (bitmap, 100, 100, Green, "C, top left");
            AssertColour (bitmap, 300, 100, Silver, "D, with no data, keeps the template colour");
        }

        [Test]
        public async Task ColourRule_ResultIsKeptForALegend ()
        {
            using var report = await Parse (MapItem (PolygonLayer ()));
            var map = report.ReportDefinition.Body.ReportItems.Items.OfType<Map> ().Single ();
            await report.BuildPages ();     // runs the data

            var scene = await map.BuildScene (report, null);

            var rule = scene.Rules.Single ();
            Assert.That (rule.LayerName, Is.EqualTo ("Regions"));
            Assert.That (rule.Buckets.Select (b => b.From), Is.EqualTo (new[] { 15.0, 30.0, 45.0 }));
            Assert.That (rule.Buckets.Last ().To, Is.EqualTo (60.0));
            Assert.That (rule.Buckets.Select (b => b.Color.Name), Is.EqualTo (new[] { "ff008000", "ffffff00", "ffff0000" }));
            Assert.That (scene.Drawables, Has.Count.EqualTo (4));
        }

        [TestCase ("EqualDistribution")]
        [TestCase ("Optimal")]
        public async Task OtherDistributions_PutEachValueInItsOwnBucket (string distribution)
        {
            using var report = await Parse (MapItem (PolygonLayer ().Replace ("EqualInterval", distribution)));
            using var bitmap = await DrawnMap (report);

            AssertColour (bitmap, 100, 300, Red, "A, the largest");
            AssertColour (bitmap, 300, 300, Yellow, "B, the middle");
            AssertColour (bitmap, 100, 100, Green, "C, the smallest");
        }

        [Test]
        public async Task CustomColours_ColourTheBuckets ()
        {
            var layer = PolygonLayer ().Replace ("<MapColorRangeRule>", "<MapCustomColorRule>").Replace ("</MapColorRangeRule>", "</MapCustomColorRule>")
                .Replace ("<StartColor>Green</StartColor><MiddleColor>Yellow</MiddleColor><EndColor>Red</EndColor>",
                    "<MapCustomColors><MapCustomColor>#0000FF</MapCustomColor><MapCustomColor>#00FFFF</MapCustomColor><MapCustomColor>#FF00FF</MapCustomColor></MapCustomColors>");
            using var report = await Parse (MapItem (layer));
            using var bitmap = await DrawnMap (report);

            AssertColour (bitmap, 100, 100, new SKColor (0, 0, 255), "C, first colour");
            AssertColour (bitmap, 300, 300, new SKColor (0, 255, 255), "B, second colour");
            AssertColour (bitmap, 100, 300, new SKColor (255, 0, 255), "A, third colour");
        }

        [Test]
        public async Task Mercator_StretchesTowardsThePoles ()
        {
            using var report = await Parse (MapItem (PolygonLayer (), viewport: Viewport ("Mercator")));
            var map = report.ReportDefinition.Body.ReportItems.Items.OfType<Map> ().Single ();
            await report.BuildPages ();

            var scene = await map.BuildScene (report, null);

            // Projected heights: 0..10 degrees is 10.05 high, 10..20 degrees 10.37; widths stay 10.
            var heights = scene.Drawables.Select (d => d.Bounds ().Value).Where (b => b.MinX < 5)
                .OrderBy (b => b.MinY).Select (b => b.MaxY - b.MinY).ToArray ();
            Assert.That (heights[0], Is.EqualTo (10.05).Within (0.01), "A");
            Assert.That (heights[1], Is.EqualTo (10.37).Within (0.01), "C, further from the equator, is taller");
        }

        [Test]
        public async Task SpatialDataSet_ShapesComeFromAFieldOfWellKnownText ()
        {
            var layer = PolygonLayer ()
                .Replace ($"<MapPolygons>{Polygons ()}</MapPolygons>",
                    "<MapSpatialDataSet><DataSetName>Shapes</DataSetName><SpatialField>Geo</SpatialField><MapFieldNames><MapFieldName>Name</MapFieldName></MapFieldNames></MapSpatialDataSet>")
                .Replace ("<FieldName>NAME</FieldName>", "<FieldName>Name</FieldName>");
            using var report = await Parse (MapItem (layer));
            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4), string.Join (" | ", Messages (report)));
            using var bitmap = await DrawnMap (report);

            AssertColour (bitmap, 100, 300, Red, "A");
            AssertColour (bitmap, 100, 100, Green, "C");
            AssertColour (bitmap, 300, 100, Silver, "D");
        }

        [Test]
        public async Task SpatialDataRegion_EachGroupBringsItsShape ()
        {
            // The shapes come with the analytical data: the Sales rows carry their square's text.
            var layer = $@"<MapPolygonLayer Name=""Own"">
                <MapDataRegionName>Regions</MapDataRegionName>
                <MapSpatialDataRegion><VectorData>=Switch(Fields!Region.Value = ""A"", ""{Wkt (0, 0, 10, 10)}"", Fields!Region.Value = ""C"", ""{Wkt (0, 10, 10, 20)}"", True, ""{Wkt (10, 0, 20, 10)}"")</VectorData></MapSpatialDataRegion>
                <MapPolygonRules><MapColorRangeRule><DataValue>=Sum(Fields!Sales.Value)</DataValue><DistributionType>EqualInterval</DistributionType><BucketCount>2</BucketCount><StartColor>Blue</StartColor><MiddleColor /><EndColor>Red</EndColor></MapColorRangeRule></MapPolygonRules>
              </MapPolygonLayer>";
            using var report = await Parse (MapItem (layer));
            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4), string.Join (" | ", Messages (report)));
            var map = report.ReportDefinition.Body.ReportItems.Items.OfType<Map> ().Single ();
            await report.BuildPages ();

            var scene = await map.BuildScene (report, null);

            // A, b and C have a square of their own; Z falls through to B's square.
            Assert.That (scene.Drawables, Has.Count.EqualTo (4));
            Assert.That (scene.Rules.Single ().BucketOfValue.Count (b => b == 1), Is.EqualTo (2), "A (60) and Z (99) are in the top half of 15..99");
        }

        [TestCase (OutputPresentationType.PDF)]
        [TestCase (OutputPresentationType.HTML)]
        [TestCase (OutputPresentationType.TIF)]
        public async Task Map_RendersInEachOutput (OutputPresentationType type)
        {
            using var report = await Parse (MapItem (PolygonLayer ()));
            using var memory = new MemoryStreamGen ();

            await report.RunRender (memory, type);

            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4), string.Join (" | ", Messages (report)));
            if (type == OutputPresentationType.HTML) {
                var html = memory.GetText ();
                Assert.That (html, Does.Contain ("Before map"));
                Assert.That (html, Does.Contain ("<img"), "the map is an image");
                return;
            }
            var bytes = ((MemoryStream)memory.GetStream ()).ToArray ();
            Assert.That (bytes.Length, Is.GreaterThan (1000));
            if (type == OutputPresentationType.PDF) {
                using var pdf = PdfDocument.Open (bytes);
                Assert.That (pdf.GetPage (1).GetImages ().Count (), Is.EqualTo (1), "the map is an image on the page");
            }
        }

        [Test]
        public async Task TileLayerOnly_KeepsThePlaceholderAndNamesIt ()
        {
            using var report = await Parse (MapItem (@"<MapTileLayer Name=""Bing""><ServiceUrl>http://example.invalid</ServiceUrl><TileStyle>Road</TileStyle></MapTileLayer>"));

            var mapMessages = Messages (report).Where (m => m.Contains ("Map")).ToArray ();
            Assert.That (mapMessages, Has.Length.EqualTo (1), string.Join (" | ", Messages (report)));
            Assert.That (mapMessages[0], Does.Contain ("not supported").And.Contain ("tile layer"));
            Assert.That (report.ReportDefinition.Body.ReportItems.Items.OfType<Map> (), Is.Empty, "replaced by a placeholder");
            var pages = await report.BuildPages ();
            Assert.That (pages[0].OfType<PageImage> (), Is.Empty);
        }

        [Test]
        public async Task PolygonAndTileLayer_DrawsThePolygonsAndNamesTheRest ()
        {
            var layers = @"<MapTileLayer Name=""Bing""><TileStyle>Road</TileStyle></MapTileLayer>" + PolygonLayer () +
                         @"<MapPointLayer Name=""Points""><MapPoints /></MapPointLayer>";
            var extra = @"<MapLegends><MapLegend Name=""Legend1"" /></MapLegends><MapColorScale><Hidden>true</Hidden></MapColorScale>";
            using var report = await Parse (MapItem (layers, extra));

            var mapMessages = Messages (report).Where (m => m.Contains ("Map")).ToArray ();
            Assert.That (mapMessages, Has.Length.EqualTo (1), string.Join (" | ", Messages (report)));
            Assert.That (mapMessages[0], Does.Contain ("drawn without"));
            Assert.That (mapMessages[0], Does.Contain ("tile layer").And.Contain ("point layer").And.Contain ("legend"));
            Assert.That (mapMessages[0], Does.Not.Contain ("colour scale"), "a hidden colour scale is not missed");
            using var bitmap = await DrawnMap (report);
            AssertColour (bitmap, 100, 300, Red, "the polygon layer is still drawn");
        }

        [Test]
        public void UnsupportedProjection_IsNamed ()
        {
            var doc = new XmlDocument ();
            doc.LoadXml (ReportWith (MapItem (PolygonLayer (), viewport: Viewport ("Robinson"))));
            var log = new ReportLog ();

            Rdl2008Normalizer.Normalize (doc, log);

            Assert.That (doc.GetElementsByTagName ("Map"), Has.Count.EqualTo (1));
            Assert.That (log.ErrorItems.Cast<string> ().Single (), Does.Contain ("Robinson projection"));
        }

        [Test]
        public void DesignerKeepsTheOriginalMap ()
        {
            var xml = ReportWith (MapItem (PolygonLayer (), @"<MapLegends><MapLegend Name=""Legend1""><Style><Border><Style>Solid</Style></Border></Style></MapLegend></MapLegends>"));
            var original = new XmlDocument ();
            original.LoadXml (xml);
            var doc = new XmlDocument ();
            doc.LoadXml (xml);

            Rdl2008Exporter.NormalizeForEditing (doc);
            Rdl2008Exporter.ConvertToTablix (doc);

            var before = original.GetElementsByTagName ("Map").Cast<XmlElement> ().Single ().OuterXml;
            var after = doc.GetElementsByTagName ("Map").Cast<XmlElement> ().Single ().OuterXml;
            Assert.That (after, Is.EqualTo (before), "opening and saving writes the map back unchanged");
        }

        [Test]
        public void Geometry_ReadsEachSpatialForm ()
        {
            // SQL Server geography: latitude first, so the square spans longitude 0..10 and latitude 0..10 either way;
            // check with an asymmetric one.
            var wkt = MapGeometry.Read ("MULTIPOLYGON (((0 0, 4 0, 4 4, 0 4, 0 0), (1 1, 2 1, 2 2, 1 1)), ((10 10, 11 10, 11 11, 10 10)))", true);
            Assert.That (wkt.Rings, Has.Count.EqualTo (3), "two outer rings and a hole");

            var wkb = new byte[1 + 4 + 16];
            wkb[0] = 1;
            BitConverter.GetBytes (1u).CopyTo (wkb, 1);
            BitConverter.GetBytes (30.0).CopyTo (wkb, 5);
            BitConverter.GetBytes (-5.0).CopyTo (wkb, 13);
            var point = MapGeometry.Read (wkb, true);
            Assert.That (point.Points.Single ().X, Is.EqualTo (30.0));
            Assert.That (point.Points.Single ().Y, Is.EqualTo (-5.0));

            var sql = MapGeometry.Read (Squares[1].Vector, true);     // B: longitude 10..20, latitude 0..10
            Assert.That (sql.Rings.Single ().Max (p => p.X), Is.EqualTo (20.0));
            Assert.That (sql.Rings.Single ().Max (p => p.Y), Is.EqualTo (10.0));

            Assert.That (MapGeometry.Read ("not spatial data", true), Is.Null);
        }
    }
}
