using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using SkiaSharp;
using UglyToad.PdfPig;

namespace ReportTests
{
    /// <summary>
    /// The RDL 2010+ Map decorations (issue #425): legends, the colour scale, titles and the
    /// distance scale, drawn over the map or beside it with the viewport shrunk to make room.
    /// </summary>
    [TestFixture]
    public class MapDecorationTests
    {
        private const string Rdl2010 = "http://schemas.microsoft.com/sqlserver/reporting/2010/01/reportdefinition";

        // Four 10 by 10 degree squares (A bottom left, B bottom right, C top left, D top right) as
        // SQL Server geography, the same as MapTests.
        private static readonly (string Name, string Vector)[] Squares =
        {
            ("A", "5hAAAAEEBQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACRAAAAAAAAAJEAAAAAAAAAkQAAAAAAAACRAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAQAAAAIAAAAAAQAAAP////8AAAAAAw=="),
            ("B", "5hAAAAEEBQAAAAAAAAAAAAAAAAAAAAAAJEAAAAAAAAAAAAAAAAAAADRAAAAAAAAAJEAAAAAAAAA0QAAAAAAAACRAAAAAAAAAJEAAAAAAAAAAAAAAAAAAACRAAQAAAAIAAAAAAQAAAP////8AAAAAAw=="),
            ("C", "5hAAAAEEBQAAAAAAAAAAACRAAAAAAAAAAAAAAAAAAAAkQAAAAAAAACRAAAAAAAAANEAAAAAAAAAkQAAAAAAAADRAAAAAAAAAAAAAAAAAAAAkQAAAAAAAAAAAAQAAAAIAAAAAAQAAAP////8AAAAAAw=="),
            ("D", "5hAAAAEEBQAAAAAAAAAAACRAAAAAAAAAJEAAAAAAAAAkQAAAAAAAADRAAAAAAAAANEAAAAAAAAA0QAAAAAAAADRAAAAAAAAAJEAAAAAAAAAkQAAAAAAAACRAAQAAAAIAAAAAAQAAAP////8AAAAAAw=="),
        };

        private static string Polygons () => string.Concat (Squares.Select (s =>
            $@"<MapPolygon><VectorData>{s.Vector}</VectorData><MapFields><MapField Name=""NAME""><Value>{s.Name}</Value></MapField></MapFields></MapPolygon>"));

        private static string Viewport (string system = "Geographic")
            => $@"<MapViewport><MapCoordinateSystem>{system}</MapCoordinateSystem><MapProjection>Equirectangular</MapProjection><MapDataBoundView /></MapViewport>";

        private const string DataRegion = @"<MapDataRegions><MapDataRegion Name=""Regions""><DataSetName>Sales</DataSetName>
            <MapMember><Group Name=""ByRegion""><GroupExpressions><GroupExpression>=Fields!Region.Value</GroupExpression></GroupExpressions></Group></MapMember>
          </MapDataRegion></MapDataRegions>";

        private static string PolygonLayer (string ruleExtra = "") => $@"<MapPolygonLayer Name=""Regions"">
            <MapDataRegionName>Regions</MapDataRegionName>
            <MapBindingFieldPairs><MapBindingFieldPair><FieldName>NAME</FieldName><BindingExpression>=Fields!Region.Value</BindingExpression></MapBindingFieldPair></MapBindingFieldPairs>
            <MapPolygonTemplate><Style><BackgroundColor>Silver</BackgroundColor><Border><Color>Black</Color><Style>Solid</Style><Width>0.5pt</Width></Border></Style></MapPolygonTemplate>
            <MapPolygonRules><MapColorRangeRule>
              <DataValue>=Sum(Fields!Sales.Value)</DataValue><DistributionType>EqualInterval</DistributionType><BucketCount>3</BucketCount>
              <StartColor>Green</StartColor><MiddleColor>Yellow</MiddleColor><EndColor>Red</EndColor>
              {ruleExtra}
            </MapColorRangeRule></MapPolygonRules>
            <MapPolygons>{Polygons ()}</MapPolygons>
          </MapPolygonLayer>";

        private const string LegendText = "<LegendText>#FROMVALUE{N0} - #TOVALUE{N0}</LegendText>";

        private static string Legend (string extra = "", string name = "Legend1")
            => $@"<MapLegends><MapLegend Name=""{name}""><Position>RightTop</Position><DockOutsideViewport>true</DockOutsideViewport><Layout>Column</Layout>{extra}</MapLegend></MapLegends>";

        private static string Title (string extra = "")
            => $@"<MapTitles><MapTitle Name=""Title1""><Text>Sales by region</Text><Position>TopCenter</Position><DockOutsideViewport>true</DockOutsideViewport>{extra}</MapTitle></MapTitles>";

        private static string ColorScale (string extra = "")
            => $@"<MapColorScale><Position>BottomCenter</Position><DockOutsideViewport>true</DockOutsideViewport><MapColorScaleTitle><Caption>Sales</Caption></MapColorScaleTitle>{extra}</MapColorScale>";

        private static string DistanceScale (string extra = "")
            => $@"<MapDistanceScale><Position>BottomLeft</Position>{extra}</MapDistanceScale>";

        private static string MapItem (string decorations, string ruleExtra = LegendText, string viewport = null) =>
            $@"<Map Name=""SalesMap""><Top>0.5in</Top><Left>0in</Left><Height>3in</Height><Width>4in</Width>
          {viewport ?? Viewport ()}
          {DataRegion}
          <MapLayers>{PolygonLayer (ruleExtra)}</MapLayers>
          {decorations}
        </Map>";

        private static string ReportWith (string item) => $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""{Rdl2010}"">
  <DataSources><DataSource Name=""DS1""><ConnectionProperties><DataProvider>SQLite</DataProvider><ConnectString>Data Source=none.db</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets>
    <DataSet Name=""Sales"">
      <Query><DataSourceName>DS1</DataSourceName><CommandText>/* Local Query */</CommandText></Query>
      <Fields><Field Name=""Region""><DataField>Region</DataField></Field><Field Name=""Sales""><DataField>Sales</DataField></Field></Fields>
    </DataSet>
  </DataSets>
  <ReportSections><ReportSection>
    <Body><Height>4in</Height><ReportItems>
      <Textbox Name=""Before""><Top>0in</Top><Left>0in</Left><Height>0.25in</Height><Width>2in</Width><Paragraphs><Paragraph><TextRuns><TextRun><Value>Before map</Value><Style /></TextRun></TextRuns><Style /></Paragraph></Paragraphs></Textbox>
      {item}
    </ReportItems></Body>
    <Width>5in</Width>
  </ReportSection></ReportSections>
</Report>";

        /// <summary>A sums to 60, B is 40, C is 15, D has no data: equal intervals over 15..60 make 15-30, 30-45, 45-60.</summary>
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

        [SetUp]
        public void SetUp () => RdlEngineConfig.RdlEngineConfigInit ();

        private static async Task<Report> Parse (string item)
        {
            var parser = new RDLParser (ReportWith (item)) { SkipDatabaseSchemaValidation = true };
            var report = await parser.Parse ();
            await report.DataSets["Sales"].SetData (Sales ());
            await report.RunGetData (null);
            return report;
        }

        private static string[] Messages (Report report)
            => report.ErrorItems == null ? Array.Empty<string> () : report.ErrorItems.Cast<string> ().ToArray ();

        private static async Task<(Map Map, MapScene Scene)> Scene (Report report)
        {
            var map = report.ReportDefinition.Body.ReportItems.Items.OfType<Map> ().Single ();
            await report.BuildPages ();     // runs the data
            return (map, await map.BuildScene (report, null));
        }

        // The map item is 4in by 3in, 288 by 216 points; drawn at 2 pixels a point.
        private const float PixelsPerPoint = 2;

        private static SKBitmap Draw (MapScene scene)
            => SKBitmap.Decode (scene.ToPng ((int)(scene.WidthPoints * PixelsPerPoint), (int)(scene.HeightPoints * PixelsPerPoint)));

        private static SKColor At (SKBitmap bitmap, float xPoints, float yPoints)
            => bitmap.GetPixel ((int)(xPoints * PixelsPerPoint), (int)(yPoints * PixelsPerPoint));

        private static void AssertColour (SKColor c, SKColor expected, string what)
            => Assert.That (Math.Abs (c.Red - expected.Red) + Math.Abs (c.Green - expected.Green) + Math.Abs (c.Blue - expected.Blue), Is.LessThan (12),
                $"{what}: expected {expected}, drawn {c}");

        private static int DarkPixels (SKBitmap bitmap, SKRect points)
        {
            var count = 0;
            for (var y = (int)(points.Top * PixelsPerPoint); y < (int)(points.Bottom * PixelsPerPoint); y++) {
                for (var x = (int)(points.Left * PixelsPerPoint); x < (int)(points.Right * PixelsPerPoint); x++) {
                    var c = bitmap.GetPixel (x, y);
                    if (c.Alpha > 128 && c.Red + c.Green + c.Blue < 200)
                        count++;
                }
            }
            return count;
        }

        private static readonly SKColor Green = new SKColor (0, 128, 0);
        private static readonly SKColor Yellow = new SKColor (255, 255, 0);
        private static readonly SKColor Red = new SKColor (255, 0, 0);

        [Test]
        public async Task Legend_HasAnEntryPerBucket_WithItsColourAndLegendText ()
        {
            using var report = await Parse (MapItem (Legend ()));
            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4), string.Join (" | ", Messages (report)));
            var (_, scene) = await Scene (report);

            var legend = scene.Overlays.OfType<MapLegendOverlay> ().Single ();
            Assert.That (legend.Name, Is.EqualTo ("Legend1"));
            Assert.That (legend.Entries.Select (e => e.Text), Is.EqualTo (new[] { "15 - 30", "30 - 45", "45 - 60" }));
            Assert.That (legend.Entries.Select (e => ((MapColorSwatch)e.Swatch).Color.Name), Is.EqualTo (new[] { "ff008000", "ffffff00", "ffff0000" }));
            Assert.That (legend.Rows, Is.False, "a Column layout lists the entries top to bottom");
            Assert.That (legend.Entries.Select (e => e.At.Top), Is.Ordered.Ascending);

            // Docked outside on the right: the viewport gives up that room, and the legend is right of it.
            Assert.That (scene.ViewWidth, Is.LessThan (100));
            Assert.That (legend.Bounds.Left, Is.GreaterThanOrEqualTo (scene.WidthPoints * (float)(scene.ViewWidth / 100)));
            Assert.That (legend.Bounds.Right, Is.LessThanOrEqualTo (scene.WidthPoints + 0.01f));

            // Pixel smoke check: each swatch is drawn in its colour.
            using var bitmap = Draw (scene);
            var expected = new[] { Green, Yellow, Red };
            for (var i = 0; i < legend.Entries.Count; i++) {
                var e = legend.Entries[i];
                var size = e.Swatch.Size (legend.TextStyle.Size);
                var x = legend.Bounds.Left + legend.Padding + e.At.Left + size.Width / 2;
                var y = legend.Bounds.Top + legend.Padding + e.At.MidY;
                AssertColour (At (bitmap, x, y), expected[i], legend.Entries[i].Text);
            }
            Assert.That (DarkPixels (bitmap, legend.Bounds), Is.GreaterThan (20), "the labels are drawn");
        }

        [Test]
        public async Task Legend_RowLayout_ListsEntriesLeftToRight_AndTitleIsKept ()
        {
            var legend = @"<MapLegends><MapLegend Name=""Legend1""><Position>BottomCenter</Position><Layout>Row</Layout>
                <MapLegendTitle><Caption>Sales</Caption><TitleSeparator>Line</TitleSeparator></MapLegendTitle></MapLegend></MapLegends>";
            using var report = await Parse (MapItem (legend));
            var (_, scene) = await Scene (report);

            var overlay = scene.Overlays.OfType<MapLegendOverlay> ().Single ();
            Assert.That (overlay.Rows, Is.True);
            Assert.That (overlay.Title, Is.EqualTo ("Sales"));
            Assert.That (overlay.Entries.Select (e => e.At.Left), Is.Ordered.Ascending);
            Assert.That (overlay.Entries.Select (e => e.At.Top).Distinct ().Count (), Is.EqualTo (1), "one row");
            Assert.That (scene.ViewHeight, Is.EqualTo (100), "inside the viewport: the map keeps its room");
            Assert.That (overlay.Bounds.Bottom, Is.LessThanOrEqualTo (scene.HeightPoints));
        }

        [Test]
        public async Task Legend_WithoutLegendText_UsesTheBucketRange ()
        {
            using var report = await Parse (MapItem (Legend (), ruleExtra: ""));
            var (_, scene) = await Scene (report);

            Assert.That (scene.Overlays.OfType<MapLegendOverlay> ().Single ().Entries.Select (e => e.Text),
                Is.EqualTo (new[] { "15 - 30", "30 - 45", "45 - 60" }));
        }

        [Test]
        public async Task Rule_GoesToTheLegendItNames_OrTheFirst ()
        {
            var legends = @"<MapLegends><MapLegend Name=""First"" /><MapLegend Name=""Second"" /></MapLegends>";
            using var report = await Parse (MapItem (legends, LegendText + "<LegendName>Second</LegendName>"));
            var (map, scene) = await Scene (report);

            var overlay = scene.Overlays.OfType<MapLegendOverlay> ().Single ();
            Assert.That (overlay.Name, Is.EqualTo ("Second"), "the first legend has no entries and is not drawn");

            var rule = scene.Rules.Single ().Rule;
            Assert.That (map.Decorations.LegendFor (rule).Name, Is.EqualTo ("Second"));
            rule.LegendName = null;
            Assert.That (map.Decorations.LegendFor (rule).Name, Is.EqualTo ("First"));
            rule.LegendName = "Missing";
            Assert.That (map.Decorations.LegendFor (rule), Is.Null);
        }

        [Test]
        public void LegendText_FormatsTheBucketsValues ()
        {
            var range = new MapBucket { From = 1234.56, To = 2000 };
            Assert.That (MapLegendText.Format ("#FROMVALUE{N0} - #TOVALUE{N0}", range), Is.EqualTo ("1,235 - 2,000"));
            Assert.That (MapLegendText.Format ("#FROMVALUE{N2} to #TOVALUE", range), Is.EqualTo ("1,234.56 to 2000"));
            Assert.That (MapLegendText.Format ("from #fromvalue", range), Is.EqualTo ("from 1234.56"), "keywords ignore case");
            Assert.That (MapLegendText.Format ("#VALUE", new MapBucket { Category = "North" }), Is.EqualTo ("North"));
        }

        [Test]
        public async Task ColorScale_ShowsTheRuleThatAsksForIt ()
        {
            using var report = await Parse (MapItem (ColorScale (), LegendText + "<ShowInColorScale>true</ShowInColorScale>"));
            var (_, scene) = await Scene (report);

            var scale = scene.Overlays.OfType<MapColorScaleOverlay> ().Single ();
            Assert.That (scale.Title, Is.EqualTo ("Sales"));
            Assert.That (scale.Colors.Select (c => c.Name), Is.EqualTo (new[] { "ff008000", "ffffff00", "ffff0000" }));
            Assert.That (scale.Labels, Is.EqualTo (new[] { "15", "30", "45", "60" }));
            Assert.That (scene.ViewHeight, Is.LessThan (100), "docked outside at the bottom");
            Assert.That (scale.Bounds.Top, Is.GreaterThanOrEqualTo (scene.HeightPoints * (float)((scene.ViewTop + scene.ViewHeight) / 100)));

            using var bitmap = Draw (scene);
            var counts = new Dictionary<string, int> { ["green"] = 0, ["yellow"] = 0, ["red"] = 0 };
            for (var y = (int)(scale.Bounds.Top * PixelsPerPoint); y < (int)(scale.Bounds.Bottom * PixelsPerPoint); y++) {
                for (var x = (int)(scale.Bounds.Left * PixelsPerPoint); x < (int)(scale.Bounds.Right * PixelsPerPoint); x++) {
                    var c = bitmap.GetPixel (x, y);
                    if (c == Green) counts["green"]++;
                    else if (c == Yellow) counts["yellow"]++;
                    else if (c == Red) counts["red"]++;
                }
            }
            Assert.That (counts.Values, Has.All.GreaterThan (50), "each colour is a cell of the bar: " + string.Join (", ", counts));
        }

        [Test]
        public async Task ColorScale_WithoutARuleToShow_IsNotDrawn ()
        {
            using var report = await Parse (MapItem (ColorScale ()));
            var (_, scene) = await Scene (report);

            Assert.That (scene.Overlays, Is.Empty);
            Assert.That (scene.ViewHeight, Is.EqualTo (100));
        }

        [Test]
        public async Task ColorScale_HidesEndLabelsAndFormats ()
        {
            using var report = await Parse (MapItem (ColorScale ("<LabelFormat>0.0</LabelFormat><HideEndLabels>true</HideEndLabels>"),
                "<ShowInColorScale>true</ShowInColorScale>"));
            var (_, scene) = await Scene (report);

            Assert.That (scene.Overlays.OfType<MapColorScaleOverlay> ().Single ().Labels, Is.EqualTo (new[] { null, "30.0", "45.0", null }));
        }

        [Test]
        public async Task Title_IsDrawnAboveTheMap ()
        {
            using var report = await Parse (MapItem (Title ("<Style><FontSize>14pt</FontSize></Style>")));
            var (_, scene) = await Scene (report);

            var title = scene.Overlays.OfType<MapTitleOverlay> ().Single ();
            Assert.That (title.Text, Is.EqualTo ("Sales by region"));
            Assert.That (title.TextStyle.Size, Is.EqualTo (14));
            Assert.That (scene.ViewTop, Is.GreaterThan (0), "the viewport starts below the title");
            Assert.That (title.Bounds.Top, Is.EqualTo (0).Within (0.01));
            Assert.That (title.Bounds.MidX, Is.EqualTo (scene.WidthPoints / 2).Within (0.5), "centred");

            using var bitmap = Draw (scene);
            Assert.That (DarkPixels (bitmap, title.Bounds), Is.GreaterThan (100), "the text is drawn");
        }

        [Test]
        public async Task Title_TextCanBeAnExpression ()
        {
            var titles = @"<MapTitles><MapTitle Name=""T""><Text>=""Total "" &amp; Sum(Fields!Sales.Value, ""Sales"")</Text></MapTitle></MapTitles>";
            using var report = await Parse (MapItem (titles));
            var (_, scene) = await Scene (report);

            Assert.That (scene.Overlays.OfType<MapTitleOverlay> ().Single ().Text, Is.EqualTo ("Total 115"));
        }

        [Test]
        public async Task HiddenDecorations_AreNotDrawn ()
        {
            const string hidden = "<Hidden>true</Hidden>";
            using var report = await Parse (MapItem (Legend (hidden) + Title (hidden) + ColorScale (hidden) + DistanceScale (hidden),
                LegendText + "<ShowInColorScale>true</ShowInColorScale>"));
            var (_, scene) = await Scene (report);

            Assert.That (scene.Overlays, Is.Empty);
            Assert.That (new[] { scene.ViewLeft, scene.ViewTop, scene.ViewWidth, scene.ViewHeight }, Is.EqualTo (new[] { 0.0, 0, 100, 100 }),
                "a hidden decoration takes no room");
        }

        [Test]
        public async Task DistanceScale_IsMeasuredAtTheMapCentre ()
        {
            using var report = await Parse (MapItem (DistanceScale ()));
            var (_, scene) = await Scene (report);
            var scale = scene.Overlays.OfType<MapDistanceScaleOverlay> ().Single ();

            using var bitmap = Draw (scene);       // the bars are worked out when drawn

            // 20 degrees of longitude fill the 216 point high viewport less margins (the map is
            // limited by its height): about 10 points a degree; at latitude 10, 109.5 km a degree.
            var frame = scene.Frame ((int)(scene.WidthPoints * PixelsPerPoint), (int)(scene.HeightPoints * PixelsPerPoint));
            var pointsPerDegree = frame.Scale / PixelsPerPoint;
            var metresPerPoint = MapDistance.KilometresPerDegree (10) * 1000 / pointsPerDegree;
            Assert.That (scale.Metric.Unit, Is.EqualTo ("km"));
            Assert.That (scale.Imperial.Unit, Is.EqualTo ("mi"));
            Assert.That (scale.Metric.Length, Is.EqualTo (scale.Metric.Distance * 1000 / metresPerPoint).Within (0.01));
            Assert.That (scale.Imperial.Length, Is.EqualTo (scale.Imperial.Distance * MapDistance.MetresPerMile / metresPerPoint).Within (0.01));
            var room = scale.Bounds.Width - 2 * scale.Padding;
            Assert.That (scale.Metric.Length, Is.LessThanOrEqualTo (room).And.GreaterThan (room / 5), "a round distance that fits");
            Assert.That (DarkPixels (bitmap, scale.Bounds), Is.GreaterThan (20), "the labels are drawn");
        }

        [Test]
        public void DistanceScale_Arithmetic ()
        {
            Assert.That (MapDistance.KilometresPerDegree (0), Is.EqualTo (111.195).Within (0.001));
            Assert.That (MapDistance.KilometresPerDegree (60), Is.EqualTo (55.597).Within (0.001));

            Assert.That (MapDistance.Nice (7.3), Is.EqualTo (5));
            Assert.That (MapDistance.Nice (19), Is.EqualTo (10));
            Assert.That (MapDistance.Nice (20), Is.EqualTo (20));
            Assert.That (MapDistance.Nice (0.25), Is.EqualTo (0.2).Within (1e-12));
            Assert.That (MapDistance.Nice (0), Is.EqualTo (0));

            // The inverse of the viewport's Mercator.
            var mercator45 = Math.Log (Math.Tan (Math.PI / 4 + 45 * Math.PI / 360)) * 180 / Math.PI;     // as MapViewport.Project
            Assert.That (MapDistance.Latitude (mercator45, true), Is.EqualTo (45).Within (1e-9));
            Assert.That (MapDistance.Latitude (45, false), Is.EqualTo (45));

            // 10 points a degree at the equator, 100 points of room: 1,112 km fit, so 1,000 km; 691 miles, so 500.
            var km = MapDistance.Bar (111195, 10, 100, true);
            Assert.That ((km.Distance, km.Unit, km.Label), Is.EqualTo ((1000.0, "km", "1,000 km")));
            Assert.That (km.Length, Is.EqualTo (89.93).Within (0.01));
            var miles = MapDistance.Bar (111195, 10, 100, false);
            Assert.That ((miles.Distance, miles.Unit), Is.EqualTo ((500.0, "mi")));
            Assert.That (miles.Length, Is.EqualTo (72.37).Within (0.01));

            // Zoomed in: metres and feet.
            var metres = MapDistance.Bar (111195, 100000, 100, true);
            Assert.That ((metres.Distance, metres.Unit), Is.EqualTo ((100.0, "m")));
            var feet = MapDistance.Bar (111195, 100000, 100, false);
            Assert.That ((feet.Distance, feet.Unit), Is.EqualTo ((200.0, "ft")), "111 m is 365 ft");
        }

        [Test]
        public async Task DistanceScale_OnPlanarCoordinates_IsLeftOutAndNamed ()
        {
            var item = MapItem (DistanceScale (), viewport: Viewport ("Planar"));
            using var report = await Parse (item);
            var (_, scene) = await Scene (report);
            Assert.That (scene.Overlays, Is.Empty);

            var doc = new XmlDocument ();
            doc.LoadXml (ReportWith (item));
            var log = new ReportLog ();
            Rdl2008Normalizer.Normalize (doc, log);
            Assert.That (log.ErrorItems.Cast<string> ().Single (), Does.Contain ("distance scale on planar coordinates"));
        }

        [Test]
        public void Warning_NoLongerNamesTheDrawnDecorations ()
        {
            var doc = new XmlDocument ();
            doc.LoadXml (ReportWith (MapItem (Legend () + Title () + ColorScale () + DistanceScale ())));
            var log = new ReportLog ();

            Rdl2008Normalizer.Normalize (doc, log);

            Assert.That (doc.GetElementsByTagName ("Map"), Has.Count.EqualTo (1));
            Assert.That (log.ErrorItems?.Cast<string> ().ToArray () ?? Array.Empty<string> (), Is.Empty);
        }

        [Test]
        public async Task Placement_FixedLocationAndSize ()
        {
            var titles = @"<MapTitles><MapTitle Name=""T""><Text>Here</Text><DockOutsideViewport>true</DockOutsideViewport>
                <MapLocation><Left>10</Left><Top>50</Top></MapLocation><MapSize><Width>1</Width><Height>0.5</Height><Unit>Inch</Unit></MapSize></MapTitle></MapTitles>";
            using var report = await Parse (MapItem (titles));
            var (_, scene) = await Scene (report);

            var title = scene.Overlays.OfType<MapTitleOverlay> ().Single ();
            Assert.That (title.Bounds.Left, Is.EqualTo (28.8).Within (0.01), "10% of 288 points");
            Assert.That (title.Bounds.Top, Is.EqualTo (108).Within (0.01), "50% of 216 points");
            Assert.That ((title.Bounds.Width, title.Bounds.Height), Is.EqualTo ((72f, 36f)));
            Assert.That (scene.ViewHeight, Is.EqualTo (100), "a placed decoration takes no room from the viewport");
        }

        [Test]
        public async Task InsideDecorations_AtOnePosition_AreStacked ()
        {
            var titles = @"<MapTitles><MapTitle Name=""One""><Text>One</Text><Position>TopLeft</Position></MapTitle>
                <MapTitle Name=""Two""><Text>Two</Text><Position>TopLeft</Position></MapTitle></MapTitles>";
            using var report = await Parse (MapItem (titles));
            var (_, scene) = await Scene (report);

            var t = scene.Overlays.OfType<MapTitleOverlay> ().ToArray ();
            Assert.That (t[1].Bounds.Top, Is.GreaterThanOrEqualTo (t[0].Bounds.Bottom));
            Assert.That (t[0].Bounds.Left, Is.EqualTo (t[1].Bounds.Left));
        }

        [TestCase (OutputPresentationType.PDF)]
        [TestCase (OutputPresentationType.HTML)]
        [TestCase (OutputPresentationType.TIF)]
        public async Task DecoratedMap_RendersInEachOutput (OutputPresentationType type)
        {
            using var report = await Parse (MapItem (Legend () + Title () + ColorScale () + DistanceScale (),
                LegendText + "<ShowInColorScale>true</ShowInColorScale>"));
            using var memory = new MemoryStreamGen ();

            await report.RunRender (memory, type);

            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4), string.Join (" | ", Messages (report)));
            Assert.That (Messages (report).Where (m => m.Contains ("Map")), Is.Empty, string.Join (" | ", Messages (report)));
            if (type == OutputPresentationType.HTML) {
                Assert.That (memory.GetText (), Does.Contain ("<img"), "the map is an image");
                return;
            }
            var bytes = ((MemoryStream)memory.GetStream ()).ToArray ();
            Assert.That (bytes.Length, Is.GreaterThan (1000));
            if (type == OutputPresentationType.PDF) {
                using var pdf = PdfDocument.Open (bytes);
                Assert.That (pdf.GetPage (1).GetImages ().Count (), Is.EqualTo (1), "the map and its decorations are one image");
            }
        }
    }
}
