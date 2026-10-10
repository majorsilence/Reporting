using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// Charts changed model between RDL 2005 (what the engine renders) and 2008+. These cover the
    /// conversion in both directions: the designer saving a 2005 chart into an RDLC report, and a
    /// 2008+ chart being rendered. Issue #409.
    /// </summary>
    [TestFixture]
    public class Rdl2008ChartTests
    {
        private const string Rdl2008 = "http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition";

        private static string ReportWith (string ns, string item) => $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""{ns}"">
  <DataSources><DataSource Name=""DS1""><ConnectionProperties><DataProvider>SQLite</DataProvider><ConnectString>Data Source=none.db</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>DS1</DataSourceName><CommandText>/* Local Query */</CommandText></Query>
    <Fields><Field Name=""Name""><DataField>Name</DataField></Field><Field Name=""Region""><DataField>Region</DataField></Field><Field Name=""Amount""><DataField>Amount</DataField></Field></Fields>
  </DataSet></DataSets>
  <Body><Height>3in</Height><ReportItems>{item}</ReportItems></Body>
  <Width>6in</Width>
</Report>";

        internal const string Chart2005 = @"
<Chart Name=""Revenue""><Top>0in</Top><Left>0in</Left><Height>3in</Height><Width>5in</Width><DataSetName>Data</DataSetName>
  <Type>Column</Type><Subtype>Stacked</Subtype>
  <CategoryGroupings><CategoryGrouping><DynamicCategories>
    <Grouping Name=""ByName""><GroupExpressions><GroupExpression>=Fields!Name.Value</GroupExpression></GroupExpressions></Grouping>
    <Sorting><SortBy><SortExpression>=Fields!Name.Value</SortExpression><Direction>Descending</Direction></SortBy></Sorting>
  </DynamicCategories></CategoryGrouping></CategoryGroupings>
  <CategoryAxis><Axis><Visible>true</Visible><Title><Caption>Name</Caption></Title></Axis></CategoryAxis>
  <SeriesGroupings><SeriesGrouping><DynamicSeries>
    <Grouping Name=""ByRegion""><GroupExpressions><GroupExpression>=Fields!Region.Value</GroupExpression></GroupExpressions></Grouping>
    <Label>=Fields!Region.Value</Label>
  </DynamicSeries></SeriesGrouping></SeriesGroupings>
  <ChartData><ChartSeries><DataPoints><DataPoint><DataValues><DataValue><Value>=Sum(Fields!Amount.Value)</Value></DataValue></DataValues><DataLabel><Visible>true</Visible></DataLabel></DataPoint></DataPoints></ChartSeries></ChartData>
  <ValueAxis><Axis><Visible>true</Visible><Title><Caption>Amount</Caption></Title><MajorGridLines><ShowGridLines>true</ShowGridLines></MajorGridLines></Axis></ValueAxis>
  <Legend><Visible>true</Visible><Position>BottomCenter</Position></Legend>
  <Title><Caption>Revenue by region</Caption></Title>
  <Palette>Pastel</Palette>
</Chart>";

        /// <summary>A column chart as Report Builder writes it: category and series groups, axes incl. an unused Secondary.</summary>
        private const string Chart2008 = @"
<Chart Name=""Revenue"">
  <ChartCategoryHierarchy><ChartMembers><ChartMember>
    <Group Name=""Revenue_CategoryGroup""><GroupExpressions><GroupExpression>=Fields!Name.Value</GroupExpression></GroupExpressions></Group>
    <SortExpressions><SortExpression><Value>=Fields!Name.Value</Value></SortExpression></SortExpressions>
    <Label>=Fields!Name.Value</Label>
  </ChartMember></ChartMembers></ChartCategoryHierarchy>
  <ChartSeriesHierarchy><ChartMembers><ChartMember>
    <Group Name=""Revenue_SeriesGroup""><GroupExpressions><GroupExpression>=Fields!Region.Value</GroupExpression></GroupExpressions></Group>
    <Label>=Fields!Region.Value</Label>
  </ChartMember></ChartMembers></ChartSeriesHierarchy>
  <ChartData><ChartSeriesCollection><ChartSeries Name=""Amount"">
    <ChartDataPoints><ChartDataPoint><ChartDataPointValues><Y>=Sum(Fields!Amount.Value)</Y></ChartDataPointValues><ChartDataLabel><Style /></ChartDataLabel><Style /><ChartMarker><Style /></ChartMarker></ChartDataPoint></ChartDataPoints>
    <Type>Column</Type><Subtype>Plain</Subtype><Style /><ValueAxisName>Primary</ValueAxisName><CategoryAxisName>Primary</CategoryAxisName>
  </ChartSeries></ChartSeriesCollection></ChartData>
  <ChartAreas><ChartArea Name=""Default"">
    <ChartCategoryAxes>
      <ChartAxis Name=""Primary""><Style /><ChartAxisTitle><Caption>Name</Caption><Style /></ChartAxisTitle><Visible>True</Visible></ChartAxis>
      <ChartAxis Name=""Secondary""><Style /><Visible>False</Visible></ChartAxis>
    </ChartCategoryAxes>
    <ChartValueAxes>
      <ChartAxis Name=""Primary""><Style /><ChartAxisTitle><Caption>Amount</Caption><Style /></ChartAxisTitle><ChartMajorGridLines><Enabled>True</Enabled><Style /></ChartMajorGridLines><Visible>True</Visible></ChartAxis>
      <ChartAxis Name=""Secondary""><Style /><Visible>False</Visible></ChartAxis>
    </ChartValueAxes>
    <Style />
  </ChartArea></ChartAreas>
  <ChartLegends><ChartLegend Name=""Default""><Style /><Position>BottomCenter</Position></ChartLegend></ChartLegends>
  <ChartTitles><ChartTitle Name=""Default""><Caption>Revenue by region</Caption><Style /></ChartTitle></ChartTitles>
  <Palette>Pastel</Palette>
  <DataSetName>Data</DataSetName><Top>0in</Top><Left>0in</Left><Height>3in</Height><Width>5in</Width>
</Chart>";

        private static XmlDocument Load (string xml)
        {
            var doc = new XmlDocument ();
            doc.LoadXml (xml);
            return doc;
        }

        private static XmlElement First (XmlDocument doc, string name)
            => doc.GetElementsByTagName (name).Cast<XmlElement> ().FirstOrDefault ();

        private static DataTable Data ()
        {
            var table = new DataTable ();
            table.Columns.Add ("Name", typeof (string));
            table.Columns.Add ("Region", typeof (string));
            table.Columns.Add ("Amount", typeof (double));
            table.Rows.Add ("Widget", "North", 10.0);
            table.Rows.Add ("Widget", "South", 5.0);
            table.Rows.Add ("Gadget", "North", 7.0);
            return table;
        }

        private static System.Collections.Generic.IEnumerable<string> Messages (Report report)
            => report.ErrorItems == null ? Enumerable.Empty<string> () : report.ErrorItems.Cast<string> ();

        [SetUp]
        public void SetUp () => RdlEngineConfig.RdlEngineConfigInit ();

        [Test]
        public void Chart2005_IsWrittenInTheRichModel ()
        {
            var doc = Load (ReportWith (Rdl2008, Chart2005));

            Assert.That (Rdl2008Exporter.NeedsConversion (doc), Is.True);
            Rdl2008Exporter.ConvertToTablix (doc);

            var chart = First (doc, "Chart");
            Assert.That (chart["Type", Rdl2008], Is.Null, "type moves onto each series");
            var series = chart["ChartData", Rdl2008]["ChartSeriesCollection", Rdl2008]["ChartSeries", Rdl2008];
            Assert.That (series["Type", Rdl2008].InnerText, Is.EqualTo ("Column"));
            Assert.That (series["Subtype", Rdl2008].InnerText, Is.EqualTo ("Stacked"));
            Assert.That (series["ChartDataPoints", Rdl2008]["ChartDataPoint", Rdl2008]["ChartDataPointValues", Rdl2008]["Y", Rdl2008].InnerText,
                Is.EqualTo ("=Sum(Fields!Amount.Value)"));
            var category = chart["ChartCategoryHierarchy", Rdl2008]["ChartMembers", Rdl2008]["ChartMember", Rdl2008];
            Assert.That (category["Group", Rdl2008].GetAttribute ("Name"), Is.EqualTo ("ByName"));
            Assert.That (category["SortExpressions", Rdl2008]["SortExpression", Rdl2008]["Direction", Rdl2008].InnerText, Is.EqualTo ("Descending"));
            Assert.That (chart["ChartSeriesHierarchy", Rdl2008]["ChartMembers", Rdl2008]["ChartMember", Rdl2008]["Label", Rdl2008].InnerText,
                Is.EqualTo ("=Fields!Region.Value"));
            Assert.That (chart["ChartTitles", Rdl2008]["ChartTitle", Rdl2008]["Caption", Rdl2008].InnerText, Is.EqualTo ("Revenue by region"));
            Assert.That (chart["ChartLegends", Rdl2008]["ChartLegend", Rdl2008]["Position", Rdl2008].InnerText, Is.EqualTo ("BottomCenter"));
            Assert.That (chart["Palette", Rdl2008].InnerText, Is.EqualTo ("Pastel"));
            Assert.That (Rdl2008Exporter.NeedsConversion (doc), Is.False, "an exported chart is not converted twice");
        }

        [Test]
        public void RoundTrip_Chart2005KeepsItsSettings ()
        {
            var doc = Load (ReportWith (Rdl2008, Chart2005));
            Rdl2008Exporter.ConvertToTablix (doc);

            Rdl2008Normalizer.Normalize (doc, null);

            var chart = First (doc, "Chart");
            Assert.That (chart["Type", Rdl2008].InnerText, Is.EqualTo ("Column"));
            Assert.That (chart["Subtype", Rdl2008].InnerText, Is.EqualTo ("Stacked"));
            var category = chart["CategoryGroupings", Rdl2008]["CategoryGrouping", Rdl2008]["DynamicCategories", Rdl2008];
            Assert.That (category["Grouping", Rdl2008].GetAttribute ("Name"), Is.EqualTo ("ByName"));
            Assert.That (category["Sorting", Rdl2008]["SortBy", Rdl2008]["Direction", Rdl2008].InnerText, Is.EqualTo ("Descending"));
            var series = chart["SeriesGroupings", Rdl2008]["SeriesGrouping", Rdl2008]["DynamicSeries", Rdl2008];
            Assert.That (series["Label", Rdl2008].InnerText, Is.EqualTo ("=Fields!Region.Value"));
            Assert.That (chart["ChartData", Rdl2008]["ChartSeries", Rdl2008]["DataPoints", Rdl2008]["DataPoint", Rdl2008]["DataValues", Rdl2008]["DataValue", Rdl2008]["Value", Rdl2008].InnerText,
                Is.EqualTo ("=Sum(Fields!Amount.Value)"));
            Assert.That (chart["CategoryAxis", Rdl2008]["Axis", Rdl2008]["Title", Rdl2008]["Caption", Rdl2008].InnerText, Is.EqualTo ("Name"));
            Assert.That (chart["ValueAxis", Rdl2008]["Axis", Rdl2008]["MajorGridLines", Rdl2008]["ShowGridLines", Rdl2008].InnerText, Is.EqualTo ("true"));
            Assert.That (chart["Legend", Rdl2008]["Position", Rdl2008].InnerText, Is.EqualTo ("BottomCenter"));
            Assert.That (chart["Title", Rdl2008]["Caption", Rdl2008].InnerText, Is.EqualTo ("Revenue by region"));
        }

        [TestCase ("Pie", "Plain", "Shape", "Pie")]
        [TestCase ("Pie", "Exploded", "Shape", "ExplodedPie")]
        [TestCase ("Doughnut", "Plain", "Shape", "Doughnut")]
        [TestCase ("Line", "Smooth", "Line", "Smooth")]
        [TestCase ("Bar", "PercentStacked", "Bar", "PercentStacked")]
        public void ChartTypes_MapBothWays (string type, string subtype, string type2008, string subtype2008)
        {
            var xml = Chart2005.Replace ("<Type>Column</Type><Subtype>Stacked</Subtype>", $"<Type>{type}</Type><Subtype>{subtype}</Subtype>");
            var doc = Load (ReportWith (Rdl2008, xml));

            Rdl2008Exporter.ConvertToTablix (doc);
            var series = First (doc, "ChartSeries");
            Assert.That (series["Type", Rdl2008].InnerText, Is.EqualTo (type2008));
            Assert.That (series["Subtype", Rdl2008].InnerText, Is.EqualTo (subtype2008));

            Rdl2008Normalizer.Normalize (doc, null);
            Assert.That (First (doc, "Chart")["Type", Rdl2008].InnerText, Is.EqualTo (type));
            Assert.That (First (doc, "Chart")["Subtype", Rdl2008].InnerText, Is.EqualTo (subtype));
        }

        [Test]
        public void StaticSeries_RoundTripAsSeparateSeries ()
        {
            var xml = @"<Chart Name=""Two""><Type>Line</Type><Subtype>Plain</Subtype>
  <CategoryGroupings><CategoryGrouping><DynamicCategories><Grouping Name=""C""><GroupExpressions><GroupExpression>=Fields!Name.Value</GroupExpression></GroupExpressions></Grouping></DynamicCategories></CategoryGrouping></CategoryGroupings>
  <SeriesGroupings><SeriesGrouping><StaticSeries><StaticMember><Label>Revenue</Label></StaticMember><StaticMember><Label>Cost</Label></StaticMember></StaticSeries></SeriesGrouping></SeriesGroupings>
  <ChartData>
    <ChartSeries><DataPoints><DataPoint><DataValues><DataValue><Value>=Sum(Fields!Amount.Value)</Value></DataValue></DataValues></DataPoint></DataPoints></ChartSeries>
    <ChartSeries><DataPoints><DataPoint><DataValues><DataValue><Value>=Count(Fields!Amount.Value)</Value></DataValue></DataValues></DataPoint></DataPoints></ChartSeries>
  </ChartData></Chart>";
            var doc = Load (ReportWith (Rdl2008, xml));

            Rdl2008Exporter.ConvertToTablix (doc);
            var collection = First (doc, "ChartSeriesCollection");
            Assert.That (collection.ChildNodes, Has.Count.EqualTo (2));
            Assert.That (collection.ChildNodes[1]["LegendText", Rdl2008].InnerText, Is.EqualTo ("Cost"));
            Assert.That (doc.GetElementsByTagName ("ChartSeriesHierarchy"), Has.Count.EqualTo (0).Or.Count.EqualTo (1));

            Rdl2008Normalizer.Normalize (doc, null);
            var chart = First (doc, "Chart");
            Assert.That (chart["ChartData", Rdl2008].ChildNodes, Has.Count.EqualTo (2));
            var labels = chart["SeriesGroupings", Rdl2008].SelectNodes (".//*[local-name()='Label']").Cast<XmlNode> ().Select (n => n.InnerText).ToArray ();
            Assert.That (labels, Is.EqualTo (new[] { "Revenue", "Cost" }));
        }

        [Test]
        public void ScatterAndBubbleKeepTheirValueOrder ()
        {
            var xml = @"<Chart Name=""S""><Type>Bubble</Type><Subtype>Plain</Subtype>
  <CategoryGroupings><CategoryGrouping><DynamicCategories><Grouping Name=""C""><GroupExpressions><GroupExpression>=Fields!Name.Value</GroupExpression></GroupExpressions></Grouping></DynamicCategories></CategoryGrouping></CategoryGroupings>
  <ChartData><ChartSeries><DataPoints><DataPoint><DataValues>
    <DataValue><Value>=1</Value></DataValue><DataValue><Value>=2</Value></DataValue><DataValue><Value>=3</Value></DataValue>
  </DataValues></DataPoint></DataPoints></ChartSeries></ChartData></Chart>";
            var doc = Load (ReportWith (Rdl2008, xml));

            Rdl2008Exporter.ConvertToTablix (doc);
            var values = First (doc, "ChartDataPointValues");
            Assert.That (values.ChildNodes.Cast<XmlNode> ().Select (n => n.LocalName + n.InnerText).ToArray (),
                Is.EqualTo (new[] { "X=1", "Y=2", "Size=3" }));

            Rdl2008Normalizer.Normalize (doc, null);
            var back = First (doc, "DataValues").ChildNodes.Cast<XmlNode> ().Select (n => n.InnerText).ToArray ();
            Assert.That (back, Is.EqualTo (new[] { "=1", "=2", "=3" }));
        }

        [Test]
        public async Task Chart2008_RendersThroughTheEngine ()
        {
            var parser = new RDLParser (ReportWith (Rdl2008, Chart2008)) { SkipDatabaseSchemaValidation = true };
            using var report = await parser.Parse ();

            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4), string.Join (" | ", Messages (report)));
            Assert.That (Messages (report).Any (e => e.Contains ("secondary")), Is.False,
                "the always-present, unused Secondary axis must not warn");

            await report.DataSets["Data"].SetData (Data ());
            await report.RunGetData (null);
            using var memory = new MemoryStreamGen ();
            await report.RunRender (memory, OutputPresentationType.HTML);
            Assert.That (memory.GetText (), Does.Contain ("<img"), "the chart should render as an image");
        }

        [Test]
        public async Task Chart2008_PieRendersToo ()
        {
            var pie = Chart2008.Replace ("<Type>Column</Type><Subtype>Plain</Subtype>", "<Type>Shape</Type><Subtype>Pie</Subtype>");
            var parser = new RDLParser (ReportWith (Rdl2008, pie)) { SkipDatabaseSchemaValidation = true };
            using var report = await parser.Parse ();

            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4), string.Join (" | ", Messages (report)));
            await report.DataSets["Data"].SetData (Data ());
            await report.RunGetData (null);
            using var memory = new MemoryStreamGen ();
            await report.RunRender (memory, OutputPresentationType.HTML);
            Assert.That (memory.GetText (), Does.Contain ("<img"));
        }

        [TestCase ("<Type>Polar</Type>", "Polar")]
        [TestCase ("<Palette>BrightPastel</Palette>", "BrightPastel")]
        public async Task UnsupportedChartFeatures_GiveOneWarningAndStillRender (string replacement, string mentioned)
        {
            var xml = replacement.StartsWith ("<Type>")
                ? Chart2008.Replace ("<Type>Column</Type>", replacement)
                : Chart2008.Replace ("<Palette>Pastel</Palette>", replacement);
            var parser = new RDLParser (ReportWith (Rdl2008, xml)) { SkipDatabaseSchemaValidation = true };
            using var report = await parser.Parse ();

            var warnings = Messages (report).Where (e => e.Contains ("Chart 'Revenue'")).ToList ();
            Assert.That (warnings, Has.Count.EqualTo (1));
            Assert.That (warnings[0], Does.Contain (mentioned));
            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4));
        }

        [Test]
        public async Task SecondaryAxisUse_IsReported ()
        {
            var xml = Chart2008.Replace ("<ValueAxisName>Primary</ValueAxisName>", "<ValueAxisName>Secondary</ValueAxisName>");
            var parser = new RDLParser (ReportWith (Rdl2008, xml)) { SkipDatabaseSchemaValidation = true };
            using var report = await parser.Parse ();

            Assert.That (Messages (report).Count (e => e.Contains ("secondary axes")), Is.EqualTo (1));
        }

        /// <summary>
        /// Report Builder stores a sparkline as a Chart with its axes hidden, sitting in a Tablix cell.
        /// It is converted like any other chart and lands in the table cell. (Whether the engine
        /// draws a chart nested in a table cell is a separate matter: it currently renders blank.)
        /// </summary>
        [Test]
        public async Task SparklineInATablixCell_ConvertsToAChartInTheTableCell ()
        {
            var sparkline = Chart2008
                .Replace ("<Type>Column</Type>", "<Type>Line</Type>")
                .Replace ("<Visible>True</Visible>", "<Visible>False</Visible>")
                .Replace ("<DataSetName>Data</DataSetName>", "")   // a sparkline takes the table's scope
                .Replace ("<Top>0in</Top><Left>0in</Left><Height>3in</Height><Width>5in</Width>", "<Height>0.4in</Height><Width>1.5in</Width>");
            var tablix = $@"<Tablix Name=""Grid""><DataSetName>Data</DataSetName>
  <TablixBody>
    <TablixColumns><TablixColumn><Width>1.5in</Width></TablixColumn></TablixColumns>
    <TablixRows>
      <TablixRow><Height>0.25in</Height><TablixCells><TablixCell><CellContents><Textbox Name=""H""><Value>Trend</Value></Textbox></CellContents></TablixCell></TablixCells></TablixRow>
      <TablixRow><Height>0.4in</Height><TablixCells><TablixCell><CellContents>{sparkline}</CellContents></TablixCell></TablixCells></TablixRow>
    </TablixRows>
  </TablixBody>
  <TablixColumnHierarchy><TablixMembers><TablixMember /></TablixMembers></TablixColumnHierarchy>
  <TablixRowHierarchy><TablixMembers><TablixMember /><TablixMember><Group Name=""Details"" /></TablixMember></TablixMembers></TablixRowHierarchy>
</Tablix>";
            var parser = new RDLParser (ReportWith (Rdl2008, tablix)) { SkipDatabaseSchemaValidation = true };
            using var report = await parser.Parse ();

            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4), string.Join (" | ", Messages (report)));
            Assert.That (Messages (report), Is.Empty.Or.All.Not.Contain ("not supported"));
            var doc = Load (ReportWith (Rdl2008, tablix));
            Rdl2008Normalizer.Normalize (doc, null);
            var chart = doc.GetElementsByTagName ("TableCell").Cast<XmlElement> ().SelectMany (x => x.GetElementsByTagName ("Chart").Cast<XmlElement> ()).FirstOrDefault ();
            Assert.That (chart, Is.Not.Null, "the sparkline chart sits in a table cell");
            Assert.That (chart["Type", Rdl2008].InnerText, Is.EqualTo ("Line"));
            Assert.That (chart["CategoryAxis", Rdl2008]["Axis", Rdl2008]["Visible", Rdl2008].InnerText, Is.EqualTo ("false"), "its axes stay hidden");
        }
    }
}
