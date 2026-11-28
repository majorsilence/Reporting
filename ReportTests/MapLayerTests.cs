using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using SkiaSharp;
using UglyToad.PdfPig;

namespace ReportTests
{
    /// <summary>
    /// Point and line layers of the RDL 2010+ Map report item (issue #425): markers and lines
    /// placed from embedded or dataset spatial data, coloured, sized and shaped by their rules.
    /// </summary>
    [TestFixture]
    public class MapLayerTests
    {
        private const string Rdl2010 = "http://schemas.microsoft.com/sqlserver/reporting/2010/01/reportdefinition";
        private const int Size = 400;

        private const string DataRegion = @"<MapDataRegions><MapDataRegion Name=""Regions""><DataSetName>Sales</DataSetName>
            <MapMember><Group Name=""ByRegion""><GroupExpressions><GroupExpression>=Fields!Region.Value</GroupExpression></GroupExpressions></Group></MapMember>
          </MapDataRegion></MapDataRegions>";

        private const string Binding = @"<MapDataRegionName>Regions</MapDataRegionName>
            <MapBindingFieldPairs><MapBindingFieldPair><FieldName>NAME</FieldName><BindingExpression>=Fields!Region.Value</BindingExpression></MapBindingFieldPair></MapBindingFieldPairs>";

        private const string ColourRange = @"<MapColorRangeRule>
              <DataValue>=Sum(Fields!Sales.Value)</DataValue><DistributionType>EqualInterval</DistributionType><BucketCount>3</BucketCount>
              <StartColor>Green</StartColor><MiddleColor>Yellow</MiddleColor><EndColor>Red</EndColor>
            </MapColorRangeRule>";

        private static string Fields (string name, string kind = null)
            => $@"<MapFields><MapField Name=""NAME""><Value>{name}</Value></MapField>" +
               (kind == null ? "" : $@"<MapField Name=""KIND""><Value>{kind}</Value></MapField>") + "</MapFields>";

        private static string Point (string name, double x, double y, string kind = null)
            => FormattableString.Invariant ($"<MapPoint><VectorData>POINT ({x} {y})</VectorData>{Fields (name, kind)}</MapPoint>");

        // A (0,0) bottom left, B (10,0) bottom right, C (0,10) top left, D (10,10) top right.
        private static readonly string Points = Point ("A", 0, 0, "Town") + Point ("B", 10, 0, "City") + Point ("C", 0, 10, "Village") + Point ("D", 10, 10, "Town");

        private const string MarkerTemplate = @"<MapMarkerTemplate><MapMarker><MapMarkerStyle>Circle</MapMarkerStyle></MapMarker><Size>8pt</Size>
            <Style><BackgroundColor>Silver</BackgroundColor><Border><Style>None</Style></Border></Style></MapMarkerTemplate>";

        private static string PointLayer (string rules, string template = MarkerTemplate, string points = null) => $@"<MapPointLayer Name=""Towns"">
            {Binding}
            <MapPointRules>{rules}</MapPointRules>
            {template}
            <MapPoints>{points ?? Points}</MapPoints>
          </MapPointLayer>";

        private const string SizeRule = @"<MapSizeRule><DataValue>=Sum(Fields!Sales.Value)</DataValue><DistributionType>EqualInterval</DistributionType><BucketCount>3</BucketCount>
              <StartSize>6pt</StartSize><EndSize>18pt</EndSize></MapSizeRule>";

        private static string Line (string name, string wkt) => $"<MapLine><VectorData>{wkt}</VectorData>{Fields (name)}</MapLine>";

        // A along the bottom, C along the top, B down the middle.
        private static readonly string Lines = Line ("A", "LINESTRING (0 0, 10 0)") + Line ("C", "LINESTRING (0 10, 10 10)") + Line ("B", "LINESTRING (5 2, 5 8)");

        private static string LineLayer (string lines = null) => $@"<MapLineLayer Name=""Roads"">
            {Binding}
            <MapLineRules>
              <MapSizeRule><DataValue>=Sum(Fields!Sales.Value)</DataValue><DistributionType>EqualInterval</DistributionType><BucketCount>2</BucketCount><StartSize>2pt</StartSize><EndSize>12pt</EndSize></MapSizeRule>
              <MapCustomColorRule><DataValue>=Sum(Fields!Sales.Value)</DataValue><DistributionType>EqualInterval</DistributionType><BucketCount>2</BucketCount>
                <MapCustomColors><MapCustomColor>Blue</MapCustomColor><MapCustomColor>Magenta</MapCustomColor></MapCustomColors></MapCustomColorRule>
            </MapLineRules>
            <MapLineTemplate><Width>3pt</Width><Style><BackgroundColor>Black</BackgroundColor></Style></MapLineTemplate>
            <MapLines>{lines ?? Lines}</MapLines>
          </MapLineLayer>";

        private static string Square (string name, double x0, double y0, double x1, double y1)
            => FormattableString.Invariant ($"<MapPolygon><VectorData>POLYGON (({x0} {y0}, {x1} {y0}, {x1} {y1}, {x0} {y1}, {x0} {y0}))</VectorData>{Fields (name)}</MapPolygon>");

        private static string PolygonLayer () => $@"<MapPolygonLayer Name=""Regions"">
            {Binding}
            <MapPolygonTemplate><Style><BackgroundColor>Silver</BackgroundColor><Border><Color>Gray</Color><Style>Solid</Style><Width>0.5pt</Width></Border></Style></MapPolygonTemplate>
            <MapPolygonRules><MapColorRangeRule><DataValue>=Sum(Fields!Sales.Value)</DataValue><DistributionType>EqualInterval</DistributionType><BucketCount>3</BucketCount>
              <StartColor>#C8E6C9</StartColor><MiddleColor>#FFF59D</MiddleColor><EndColor>#FFCDD2</EndColor></MapColorRangeRule></MapPolygonRules>
            <MapPolygons>{Square ("A", -2, -2, 5, 5)}{Square ("B", 5, -2, 12, 5)}{Square ("C", -2, 5, 5, 12)}{Square ("D", 5, 5, 12, 12)}</MapPolygons>
          </MapPolygonLayer>";

        private static string MapItem (string layers, string coordinates = "Planar", string decorations = "") =>
            $@"<Map Name=""TownMap""><Top>0.5in</Top><Left>0in</Left><Height>2in</Height><Width>2in</Width>
          <MapViewport><MapCoordinateSystem>{coordinates}</MapCoordinateSystem><MapProjection>Equirectangular</MapProjection><MapDataBoundView /></MapViewport>
          {DataRegion}
          <MapLayers>{layers}</MapLayers>
          {decorations}
        </Map>";

        private const string LegendRight = @"<MapLegends><MapLegend Name=""Legend1""><Position>RightTop</Position><DockOutsideViewport>true</DockOutsideViewport><Layout>Column</Layout></MapLegend></MapLegends>";

        private static string ReportWith (string item) => $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""{Rdl2010}"">
  <DataSources><DataSource Name=""DS1""><ConnectionProperties><DataProvider>SQLite</DataProvider><ConnectString>Data Source=none.db</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets>
    <DataSet Name=""Sales"">
      <Query><DataSourceName>DS1</DataSourceName><CommandText>/* Local Query */</CommandText></Query>
      <Fields><Field Name=""Region""><DataField>Region</DataField></Field><Field Name=""Sales""><DataField>Sales</DataField></Field></Fields>
    </DataSet>
    <DataSet Name=""Places"">
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
            table.Rows.Add ("B", 40.0);
            table.Rows.Add ("C", 15.0);
            table.Rows.Add ("A", 50.0);
            return table;
        }

        /// <summary>A SQL Server geography point as a report carries it: SRID 4326, latitude before longitude.</summary>
        private static string GeographyPoint (double latitude, double longitude)
        {
            var bytes = new byte[22];
            BitConverter.GetBytes (4326).CopyTo (bytes, 0);
            bytes[4] = 1;           // version
            bytes[5] = 0x0C;        // valid, a single point
            BitConverter.GetBytes (latitude).CopyTo (bytes, 6);
            BitConverter.GetBytes (longitude).CopyTo (bytes, 14);
            return Convert.ToBase64String (bytes);
        }

        private static DataTable Places ()
        {
            var table = new DataTable ();
            table.Columns.Add ("Name", typeof (string));
            table.Columns.Add ("Geo", typeof (string));
            table.Rows.Add ("A", "POINT (30 -5)");                 // well-known text: longitude 30, latitude -5
            table.Rows.Add ("B", GeographyPoint (20, -10));        // longitude -10, latitude 20
            return table;
        }

        [SetUp]
        public void SetUp () => RdlEngineConfig.RdlEngineConfigInit ();

        private static async Task<Report> Parse (string item)
        {
            var parser = new RDLParser (ReportWith (item)) { SkipDatabaseSchemaValidation = true };
            var report = await parser.Parse ();
            await report.DataSets["Sales"].SetData (Sales ());
            await report.DataSets["Places"].SetData (Places ());
            await report.RunGetData (null);
            return report;
        }

        private static string[] Messages (Report report)
            => report.ErrorItems == null ? Array.Empty<string> () : report.ErrorItems.Cast<string> ().ToArray ();

        private static void AssertNoMapWarnings (Report report)
        {
            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4), string.Join (" | ", Messages (report)));
            Assert.That (Messages (report).Where (m => m.Contains ("Map")), Is.Empty, string.Join (" | ", Messages (report)));
        }

        private static async Task<MapScene> Scene (Report report)
        {
            var map = report.ReportDefinition.Body.ReportItems.Items.OfType<Map> ().Single ();
            await report.BuildPages ();     // runs the data
            return await map.BuildScene (report, null);
        }

        private static async Task<SKBitmap> DrawnMap (Report report)
        {
            var pages = await report.BuildPages ();
            var image = pages[0].OfType<PageImage> ().Single ();
            return SKBitmap.Decode (image.GetImageData (Size, Size));
        }

        /// <summary>Where a coordinate is drawn on the bitmap.</summary>
        private static SKPoint Pixel (MapScene scene, double x, double y)
            => scene.Frame (Size, Size).ToPixel (scene.Viewport.Project (new MapPoint (x, y)));

        private static void AssertColour (SKBitmap bitmap, SKPoint at, float dx, float dy, SKColor expected, string what)
        {
            var c = bitmap.GetPixel ((int)Math.Round (at.X + dx), (int)Math.Round (at.Y + dy));
            Assert.That (c.Alpha, Is.GreaterThan (200), $"{what}: nothing drawn at {at} + ({dx}, {dy})");
            Assert.That (Math.Abs (c.Red - expected.Red) + Math.Abs (c.Green - expected.Green) + Math.Abs (c.Blue - expected.Blue), Is.LessThan (12),
                $"{what}: expected {expected}, drawn {c}");
        }

        private static void AssertEmpty (SKBitmap bitmap, SKPoint at, float dx, float dy, string what)
        {
            var c = bitmap.GetPixel ((int)Math.Round (at.X + dx), (int)Math.Round (at.Y + dy));
            Assert.That (c.Alpha, Is.LessThan (20), $"{what}: expected nothing at {at} + ({dx}, {dy}), drawn {c}");
        }

        private static float Px (float points) => points * Size / 144f;     // the map is 2in (144pt) across

        private static readonly SKColor Green = new SKColor (0, 128, 0);
        private static readonly SKColor Yellow = new SKColor (255, 255, 0);
        private static readonly SKColor Red = new SKColor (255, 0, 0);
        private static readonly SKColor Silver = new SKColor (192, 192, 192);
        private static readonly SKColor Blue = new SKColor (0, 0, 255);
        private static readonly SKColor Magenta = new SKColor (255, 0, 255);

        [Test]
        public async Task PointLayer_ColourAndSizeRules_DrawEachMarkerByItsGroup ()
        {
            using var report = await Parse (MapItem (PointLayer (SizeRule + ColourRange)));
            AssertNoMapWarnings (report);
            var scene = await Scene (report);
            using var bitmap = await DrawnMap (report);

            // Equal intervals over 15..60: C (15) the first bucket, B (40) the second, A (10 + 50) the last.
            var a = Pixel (scene, 0, 0);
            var b = Pixel (scene, 10, 0);
            var c = Pixel (scene, 0, 10);
            var d = Pixel (scene, 10, 10);
            AssertColour (bitmap, a, 0, 0, Red, "A");
            AssertColour (bitmap, b, 0, 0, Yellow, "B");
            AssertColour (bitmap, c, 0, 0, Green, "C");
            AssertColour (bitmap, d, 0, 0, Silver, "D, with no data, keeps the template colour");

            // Sizes 18pt, 12pt, 6pt by bucket; D the template's 8pt.
            AssertColour (bitmap, a, Px (8), 0, Red, "A is 18pt across");
            AssertEmpty (bitmap, a, Px (10), 0, "A is no wider than 18pt");
            AssertColour (bitmap, b, -Px (5), 0, Yellow, "B is 12pt across");
            AssertEmpty (bitmap, b, -Px (7), 0, "B is no wider than 12pt");
            AssertColour (bitmap, c, Px (2), 0, Green, "C is 6pt across");
            AssertEmpty (bitmap, c, Px (4), 0, "C is no wider than 6pt");
            AssertEmpty (bitmap, d, -Px (5), 0, "D is the template's 8pt");

            // Both rules leave their buckets for a legend, as the polygon colour rule does.
            Assert.That (scene.Rules, Has.Count.EqualTo (2));
            Assert.That (scene.Rules.All (r => r.LayerName == "Towns"));
            var size = scene.Rules.Single (r => r.Rule is MapSizeRule);
            Assert.That (size.Buckets.Select (k => k.SizePoints), Is.EqualTo (new[] { 6f, 12f, 18f }));
            var colour = scene.Rules.Single (r => r.Rule is MapColorRule);
            Assert.That (colour.Buckets.Select (k => k.Color.Name), Is.EqualTo (new[] { "ff008000", "ffffff00", "ffff0000" }));
        }

        [Test]
        public async Task MarkerRule_GivesEachCategoryItsMarker ()
        {
            var rule = @"<MapMarkerRule><DataValue>#KIND</DataValue>
                <MapMarkers><MapMarker><MapMarkerStyle>Square</MapMarkerStyle></MapMarker><MapMarker><MapMarkerStyle>Triangle</MapMarkerStyle></MapMarker><MapMarker><MapMarkerStyle>Star</MapMarkerStyle></MapMarker></MapMarkers>
              </MapMarkerRule>";
            var template = MarkerTemplate.Replace ("<Size>8pt</Size>", "<Size>20pt</Size>");
            using var report = await Parse (MapItem (PointLayer (rule, template)));
            AssertNoMapWarnings (report);
            var scene = await Scene (report);
            using var bitmap = await DrawnMap (report);

            // Categories in the order they first occur: Town (A, D), City (B), Village (C).
            var markers = scene.Drawables.OfType<MapMarkerShape> ().ToDictionary (m => (m.Center.X, m.Center.Y), m => m.Marker);
            Assert.That (markers[(0, 0)], Is.EqualTo (MapMarkerStyle.Rectangle));
            Assert.That (markers[(10, 10)], Is.EqualTo (MapMarkerStyle.Rectangle));
            Assert.That (markers[(10, 0)], Is.EqualTo (MapMarkerStyle.Triangle));
            Assert.That (markers[(0, 10)], Is.EqualTo (MapMarkerStyle.Star));
            var result = scene.Rules.Single ();
            Assert.That (result.Buckets.Select (k => (k.Category, k.Marker)),
                Is.EqualTo (new (string, MapMarkerStyle?)[] { ("Town", MapMarkerStyle.Rectangle), ("City", MapMarkerStyle.Triangle), ("Village", MapMarkerStyle.Star) }));

            // A square fills its corners; a triangle, point up, leaves its top corners empty.
            var corner = Px (8);
            AssertColour (bitmap, Pixel (scene, 0, 0), -corner, -corner, Silver, "the square's top left corner");
            AssertEmpty (bitmap, Pixel (scene, 10, 0), -corner, -corner, "the triangle's top left corner");
            AssertColour (bitmap, Pixel (scene, 10, 0), 0, corner, Silver, "the triangle's base");
        }

        [Test]
        public async Task LineLayer_WidthAndColourRules_DrawEachLineByItsGroup ()
        {
            using var report = await Parse (MapItem (LineLayer ()));
            AssertNoMapWarnings (report);
            var scene = await Scene (report);
            using var bitmap = await DrawnMap (report);

            // Two equal intervals over 15..60: A (60) and B (40) the second (12pt, magenta), C (15) the first (2pt, blue).
            var a = Pixel (scene, 5, 0);
            var c = Pixel (scene, 5, 10);
            var b = Pixel (scene, 5, 5);
            AssertColour (bitmap, a, 0, 0, Magenta, "A");
            AssertColour (bitmap, a, 0, -Px (5), Magenta, "A is 12pt wide");
            AssertEmpty (bitmap, a, 0, -Px (7.5f), "A is no wider than 12pt");
            AssertColour (bitmap, c, 0, 0, Blue, "C");
            AssertEmpty (bitmap, c, 0, Px (3), "C is 2pt wide");
            AssertColour (bitmap, b, Px (5), 0, Magenta, "B, down the middle");

            var widths = scene.Drawables.OfType<MapLineShape> ().ToDictionary (l => (l.Parts[0][0].X, l.Parts[0][0].Y), l => l.WidthPoints);
            Assert.That (widths[(0, 0)], Is.EqualTo (12f), "A");
            Assert.That (widths[(0, 10)], Is.EqualTo (2f), "C");
            Assert.That (widths[(5, 2)], Is.EqualTo (12f), "B");
            Assert.That (scene.Rules.Select (r => r.Rule.GetType ().Name), Is.EquivalentTo (new[] { "MapSizeRule", "MapCustomColorRule" }));
        }

        [Test]
        public async Task LineTemplate_GivesWidthColourAndDashWithoutRules ()
        {
            var layer = $@"<MapLineLayer Name=""Plain"">
                <MapLineTemplate><Width>6pt</Width><Style><Border><Color>Blue</Color><Style>Dashed</Style></Border></Style></MapLineTemplate>
                <MapLines>{Line ("A", "LINESTRING (0 0, 10 0)")}{Line ("C", "LINESTRING (0 10, 10 10)")}</MapLines>
              </MapLineLayer>";
            using var report = await Parse (MapItem (layer));
            AssertNoMapWarnings (report);
            var scene = await Scene (report);

            var lines = scene.Drawables.OfType<MapLineShape> ().ToArray ();
            Assert.That (lines, Has.Length.EqualTo (2));
            Assert.That (lines.Select (l => (l.WidthPoints, l.Color.ToArgb (), l.Dash)),
                Is.All.EqualTo ((6f, System.Drawing.Color.Blue.ToArgb (), BorderStyleEnum.Dashed)));
            Assert.That (scene.Rules, Is.Empty);
        }

        [Test]
        public async Task PointLayer_FromASpatialDataSet_ReadsTextAndGeographyPoints ()
        {
            var layer = $@"<MapPointLayer Name=""Places"">
                <MapSpatialDataSet><DataSetName>Places</DataSetName><SpatialField>Geo</SpatialField><MapFieldNames><MapFieldName>Name</MapFieldName></MapFieldNames></MapSpatialDataSet>
                <MapDataRegionName>Regions</MapDataRegionName>
                <MapBindingFieldPairs><MapBindingFieldPair><FieldName>Name</FieldName><BindingExpression>=Fields!Region.Value</BindingExpression></MapBindingFieldPair></MapBindingFieldPairs>
                <MapPointRules>{ColourRange.Replace ("<BucketCount>3</BucketCount>", "<BucketCount>2</BucketCount>").Replace ("<MiddleColor>Yellow</MiddleColor>", "<MiddleColor />")}</MapPointRules>
                {MarkerTemplate.Replace ("<Size>8pt</Size>", "<Size>12pt</Size>")}
              </MapPointLayer>";
            using var report = await Parse (MapItem (layer, "Geographic"));
            AssertNoMapWarnings (report);
            var scene = await Scene (report);
            using var bitmap = await DrawnMap (report);

            var centres = scene.Drawables.OfType<MapMarkerShape> ().Select (m => (m.Center.X, m.Center.Y)).ToArray ();
            Assert.That (centres, Is.EquivalentTo (new[] { (30.0, -5.0), (-10.0, 20.0) }), "longitude across, latitude up, from either form");

            // A (60) the upper bucket, B (40) the lower.
            var a = Pixel (scene, 30, -5);
            var b = Pixel (scene, -10, 20);
            Assert.That (a.X, Is.GreaterThan (b.X), "A is east of B");
            Assert.That (a.Y, Is.GreaterThan (b.Y), "A is south of B");
            AssertColour (bitmap, a, 0, 0, Red, "A");
            AssertColour (bitmap, b, 0, 0, Green, "B");
        }

        [Test]
        public async Task PointLayerOnly_IsDrawnWithoutAPlaceholderOrWarning ()
        {
            using var report = await Parse (MapItem (PointLayer (ColourRange)));

            AssertNoMapWarnings (report);
            Assert.That (Messages (report).Where (m => m.Contains ("point layer")), Is.Empty);
            Assert.That (report.ReportDefinition.Body.ReportItems.Items.OfType<Map> ().Count (), Is.EqualTo (1), "not replaced by a placeholder");
            using var bitmap = await DrawnMap (report);
            var scene = await Scene (report);
            AssertColour (bitmap, Pixel (scene, 0, 0), 0, 0, Red, "A");
        }

        [Test]
        public async Task PointLayerLabels_AreNamedInTheWarning ()
        {
            var template = MarkerTemplate.Replace ("<Size>8pt</Size>", "<Size>8pt</Size><Label>=Fields!Region.Value</Label>");
            using var report = await Parse (MapItem (PointLayer (ColourRange, template)));

            var mapMessages = Messages (report).Where (m => m.Contains ("Map")).ToArray ();
            Assert.That (mapMessages, Has.Length.EqualTo (1), string.Join (" | ", Messages (report)));
            Assert.That (mapMessages[0], Does.Contain ("point labels").And.Not.Contain ("a point layer"));
        }

        [Test]
        public async Task PolygonLineAndPointLayers_AreDrawnInLayerOrder ()
        {
            using var report = await Parse (MapItem (PolygonLayer () + LineLayer () + PointLayer (SizeRule + ColourRange)));
            AssertNoMapWarnings (report);
            var scene = await Scene (report);
            using var bitmap = await DrawnMap (report);

            var kinds = scene.Drawables.Select (d => d.GetType ().Name).ToArray ();
            Assert.That (kinds, Is.EqualTo (Enumerable.Repeat ("MapPolygonShape", 4).Concat (Enumerable.Repeat ("MapLineShape", 3)).Concat (Enumerable.Repeat ("MapMarkerShape", 4))));

            AssertColour (bitmap, Pixel (scene, 2.5, 7.5), 0, 0, new SKColor (0xC8, 0xE6, 0xC9), "the polygon C, where nothing is over it");
            AssertColour (bitmap, Pixel (scene, 2.5, 0), 0, 0, Magenta, "the line A, over the polygons");
            AssertColour (bitmap, Pixel (scene, 0, 0), 0, 0, Red, "the point A, over the line");
            AssertColour (bitmap, Pixel (scene, 10, 10), 0, 0, Silver, "the point D, over the line C");

            var output = Environment.GetEnvironmentVariable ("MAP_LAYERS_PNG");
            if (!string.IsNullOrEmpty (output))
                File.WriteAllBytes (output, scene.ToPng (Size, Size));
        }

        [TestCase (OutputPresentationType.PDF)]
        [TestCase (OutputPresentationType.HTML)]
        [TestCase (OutputPresentationType.TIF)]
        public async Task PointAndLineMap_RendersInEachOutput (OutputPresentationType type)
        {
            using var report = await Parse (MapItem (LineLayer () + PointLayer (SizeRule + ColourRange)));
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
        public async Task Legend_SizeRuleOnPoints_ShowsAMarkerOfEachSize ()
        {
            using var report = await Parse (MapItem (PointLayer (SizeRule), decorations: LegendRight));
            AssertNoMapWarnings (report);
            var scene = await Scene (report);
            var swatches = scene.Overlays.OfType<MapLegendOverlay> ().Single ().Entries.Select (e => e.Swatch).ToArray ();

            Assert.That (swatches, Has.Length.EqualTo (3).And.All.InstanceOf<MapMarkerSwatch> ());
            var markers = swatches.Cast<MapMarkerSwatch> ().ToArray ();
            Assert.That (markers.Select (m => m.Marker), Is.All.EqualTo (MapMarkerStyle.Circle));
            Assert.That (markers.Select (m => m.SizePoints), Is.EqualTo (new[] { 6f, 12f, 18f }), "StartSize to EndSize across the buckets");
            Assert.That (markers[2].Size (8).Height, Is.GreaterThan (markers[0].Size (8).Height), "a bigger bucket, a bigger swatch");
        }

        [Test]
        public async Task Legend_MarkerRule_ShowsEachBucketsMarker ()
        {
            var rule = @"<MapMarkerRule><DataValue>#KIND</DataValue>
                <MapMarkers><MapMarker><MapMarkerStyle>Square</MapMarkerStyle></MapMarker><MapMarker><MapMarkerStyle>Triangle</MapMarkerStyle></MapMarker><MapMarker><MapMarkerStyle>Star</MapMarkerStyle></MapMarker></MapMarkers>
              </MapMarkerRule>";
            using var report = await Parse (MapItem (PointLayer (rule), decorations: LegendRight));
            AssertNoMapWarnings (report);
            var scene = await Scene (report);
            var swatches = scene.Overlays.OfType<MapLegendOverlay> ().Single ().Entries.Select (e => e.Swatch).ToArray ();

            Assert.That (swatches, Is.All.InstanceOf<MapMarkerSwatch> ());
            Assert.That (swatches.Cast<MapMarkerSwatch> ().Select (m => m.Marker),
                Is.EqualTo (new[] { MapMarkerStyle.Rectangle, MapMarkerStyle.Triangle, MapMarkerStyle.Star }));

            // The markers are drawn: grey inside each swatch.
            using var bitmap = new SKBitmap (100, 30);
            using (var canvas = new SKCanvas (bitmap)) {
                canvas.Clear (SKColors.White);
                for (var i = 0; i < swatches.Length; i++)
                    swatches[i].Draw (canvas, SKRect.Create (5 + i * 30, 5, 20, 20));
            }
            for (var i = 0; i < swatches.Length; i++)
                Assert.That (bitmap.GetPixel (15 + i * 30, 17), Is.Not.EqualTo (SKColors.White), swatches[i] + " is drawn");
        }

        [Test]
        public async Task Legend_SizeRuleOnLines_ShowsALineOfEachWidth ()
        {
            using var report = await Parse (MapItem (LineLayer (), decorations: LegendRight.Replace ("</MapLegend>", "</MapLegend><MapLegend Name=\"Unused\"><Hidden>true</Hidden></MapLegend>")));
            AssertNoMapWarnings (report);
            var scene = await Scene (report);
            var swatches = scene.Overlays.OfType<MapLegendOverlay> ().Single ().Entries.Select (e => e.Swatch).ToArray ();

            // The size rule's two widths, then the colour rule's two colours.
            var lines = swatches.OfType<MapLineSwatch> ().ToArray ();
            Assert.That (lines.Select (l => l.WidthPoints), Is.EqualTo (new[] { 2f, 12f }));
            Assert.That (swatches.OfType<MapColorSwatch> ().Count (), Is.EqualTo (2));
            Assert.That (swatches.OfType<MapMarkerSwatch> (), Is.Empty, "a line layer's size rule is not drawn as markers");
        }

        [Test]
        public void MarkerSwatch_IsCappedSoALegendStaysReadable ()
        {
            var huge = new MapMarkerSwatch (MapMarkerStyle.Circle, 200, System.Drawing.Color.Empty);
            Assert.That (huge.Size (8).Height, Is.EqualTo (MapMarkerSwatch.MaxPoints));
            Assert.That (huge.Fill, Is.EqualTo (MapMarkerSwatch.DefaultFill), "a size rule has no colour of its own");
            Assert.That (new MapLineSwatch (float.NaN, System.Drawing.Color.Empty).LinePoints, Is.EqualTo (1f));
        }
}
}
