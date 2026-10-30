using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// The engine models RDL 2005 and should read every published schema version. 2008 and later
    /// go through Rdl2008Normalizer (see Rdl2008NormalizerTests); these cover the 2003 and 2005
    /// namespaces, which need no rewriting.
    /// </summary>
    [TestFixture]
    public class RdlVersionTests
    {
        [TestCase ("http://schemas.microsoft.com/sqlserver/reporting/2003/10/reportdefinition")]
        [TestCase ("http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition")]
        public async Task TableReport_RendersInEarlierNamespaces (string ns)
        {
            RdlEngineConfig.RdlEngineConfigInit ();
            var rdl = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""{ns}"" xmlns:rd=""http://schemas.microsoft.com/SQLServer/reporting/reportdesigner"">
  <DataSources><DataSource Name=""DS1""><ConnectionProperties><DataProvider>SQLite</DataProvider><ConnectString>Data Source=none.db</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>DS1</DataSourceName><CommandText>/* Local Query */</CommandText></Query>
    <Fields><Field Name=""Name""><DataField>Name</DataField></Field></Fields>
  </DataSet></DataSets>
  <Body><Height>2in</Height><ReportItems>
    <Table Name=""T""><DataSetName>Data</DataSetName><Top>0in</Top><Left>0in</Left><Width>2in</Width>
      <TableColumns><TableColumn><Width>2in</Width></TableColumn></TableColumns>
      <Header><TableRows><TableRow><Height>0.25in</Height><TableCells><TableCell><ReportItems><Textbox Name=""H""><Value>Heading</Value></Textbox></ReportItems></TableCell></TableCells></TableRow></TableRows></Header>
      <Details><TableRows><TableRow><Height>0.25in</Height><TableCells><TableCell><ReportItems><Textbox Name=""D""><Value>=Fields!Name.Value</Value></Textbox></ReportItems></TableCell></TableCells></TableRow></TableRows></Details>
    </Table>
  </ReportItems></Body>
  <Width>3in</Width>
</Report>";

            var parser = new RDLParser (rdl) { SkipDatabaseSchemaValidation = true };
            using var report = await parser.Parse ();
            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4),
                string.Join (" | ", report.ErrorItems ?? new System.Collections.ArrayList ()));

            var data = new DataTable ();
            data.Columns.Add ("Name", typeof (string));
            data.Rows.Add ("Widget");
            await report.DataSets["Data"].SetData (data);
            await report.RunGetData (null);
            using var memory = new MemoryStreamGen ();
            await report.RunRender (memory, OutputPresentationType.HTML);
            var html = memory.GetText ();

            Assert.That (html, Does.Contain ("Heading"));
            Assert.That (html, Does.Contain ("Widget"));
        }

        private static string ReportWithItem (string item) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2010/01/reportdefinition"">
  <DataSources><DataSource Name=""DS1""><ConnectionProperties><DataProvider>SQLite</DataProvider><ConnectString>Data Source=none.db</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>DS1</DataSourceName><CommandText>/* Local Query */</CommandText></Query>
    <Fields><Field Name=""Name""><DataField>Name</DataField></Field></Fields>
  </DataSet></DataSets>
  <ReportSections><ReportSection>
    <Body><Height>2in</Height><ReportItems>
      <Textbox Name=""Before""><Top>0in</Top><Left>0in</Left><Height>0.25in</Height><Width>2in</Width><Paragraphs><Paragraph><TextRuns><TextRun><Value>Before item</Value><Style /></TextRun></TextRuns><Style /></Paragraph></Paragraphs></Textbox>
      {item}
    </ReportItems></Body>
    <Width>4in</Width>
  </ReportSection></ReportSections>
</Report>";

        [TestCase ("Map")]
        [TestCase ("GaugePanel")]
        public async Task UnsupportedItem_ParsesAndKeepsItsPlace (string kind)
        {
            RdlEngineConfig.RdlEngineConfigInit ();
            var rdl = ReportWithItem ($@"<{kind} Name=""Unsupported1""><Top>0.5in</Top><Left>0in</Left><Height>1in</Height><Width>2in</Width><SomeInternalSetting>x</SomeInternalSetting></{kind}>");

            var parser = new RDLParser (rdl) { SkipDatabaseSchemaValidation = true };
            using var report = await parser.Parse ();

            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4), string.Join (" | ", report.ErrorItems ?? new System.Collections.ArrayList ()));
            var warnings = string.Join (" | ", report.ErrorItems.Cast<string> ());
            Assert.That (warnings, Does.Contain (kind), "one warning should name the item");
            Assert.That (warnings, Does.Contain ("not supported"));

            using var memory = new MemoryStreamGen ();
            await report.RunGetData (null);
            await report.RunRender (memory, OutputPresentationType.HTML);
            Assert.That (memory.GetText (), Does.Contain ("Before item"), "the rest of the report still renders");
        }

        [Test]
        public async Task Map_WarningNamesItsLayersAndRules ()
        {
            RdlEngineConfig.RdlEngineConfigInit ();
            var rdl = ReportWithItem (@"<Map Name=""Map1""><Top>0.5in</Top><Left>0in</Left><Height>1in</Height><Width>2in</Width>
                <MapLayers>
                  <MapPolygonLayer Name=""Layer1"">
                    <MapSpatialDataSet><DataSetName>Data</DataSetName><SpatialField>Shape</SpatialField></MapSpatialDataSet>
                    <MapPolygonRules><MapColorRangeRule><DataValue>=Sum(Fields!Name.Value)</DataValue></MapColorRangeRule></MapPolygonRules>
                  </MapPolygonLayer>
                </MapLayers>
              </Map>");

            var parser = new RDLParser (rdl) { SkipDatabaseSchemaValidation = true };
            using var report = await parser.Parse ();

            var warning = report.ErrorItems.Cast<string> ().First (e => e.Contains ("Map"));
            Assert.That (warning, Does.Contain ("polygon layer"));
            Assert.That (warning, Does.Contain ("dataset's spatial field"));
            Assert.That (warning, Does.Contain ("colour-range rule"));
        }

        [Test]
        public async Task SeveralUnsupportedItemsOfOneKind_GiveOneWarning ()
        {
            RdlEngineConfig.RdlEngineConfigInit ();
            var rdl = ReportWithItem (string.Concat (Enumerable.Range (1, 3).Select (i =>
                $@"<GaugePanel Name=""G{i}""><Top>{i * 0.3}in</Top><Left>0in</Left><Height>0.25in</Height><Width>1in</Width></GaugePanel>")));

            var parser = new RDLParser (rdl) { SkipDatabaseSchemaValidation = true };
            using var report = await parser.Parse ();

            var sparklineWarnings = report.ErrorItems.Cast<string> ().Count (e => e.Contains ("GaugePanel"));
            Assert.That (sparklineWarnings, Is.EqualTo (1));
            Assert.That (report.ErrorItems.Cast<string> ().First (e => e.Contains ("GaugePanel")), Does.Contain ("3"));
        }

        private static string TwoSectionReport (string firstHeight = "1in", string secondHeight = "2in") => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition"">
  <ReportSections>
    <ReportSection>
      <Body><Height>{firstHeight}</Height><ReportItems>
        <Textbox Name=""A""><Top>0in</Top><Left>0in</Left><Height>0.25in</Height><Width>2in</Width><Paragraphs><Paragraph><TextRuns><TextRun><Value>First section text</Value><Style /></TextRun></TextRuns><Style /></Paragraph></Paragraphs></Textbox>
      </ReportItems></Body>
      <Width>5in</Width>
      <Page><PageHeight>11in</PageHeight><PageWidth>8.5in</PageWidth></Page>
    </ReportSection>
    <ReportSection>
      <Body><Height>{secondHeight}</Height><ReportItems>
        <Textbox Name=""B""><Top>0.5in</Top><Left>0in</Left><Height>0.25in</Height><Width>2in</Width><Paragraphs><Paragraph><TextRuns><TextRun><Value>Second section text</Value><Style /></TextRun></TextRuns><Style /></Paragraph></Paragraphs></Textbox>
      </ReportItems></Body>
      <Width>5in</Width>
    </ReportSection>
  </ReportSections>
</Report>";

        [Test]
        public async Task LaterReportSections_AreStackedBelowTheFirstOnNewPages ()
        {
            RdlEngineConfig.RdlEngineConfigInit ();
            var parser = new RDLParser (TwoSectionReport ()) { SkipDatabaseSchemaValidation = true };
            using var report = await parser.Parse ();

            var messages = report.ErrorItems == null ? new string[0] : report.ErrorItems.Cast<string> ().ToArray ();
            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4), string.Join (" | ", messages));
            Assert.That (messages.Count (m => m.Contains ("ReportSections")), Is.EqualTo (1));

            await report.RunGetData (null);
            using var memory = new MemoryStreamGen ();
            await report.RunRender (memory, OutputPresentationType.HTML);
            var html = memory.GetText ();

            Assert.That (html, Does.Contain ("First section text"));
            Assert.That (html, Does.Contain ("Second section text"), "the second section must not be dropped");
        }

        [Test]
        public void LaterReportSections_SitBelowTheFirstBody ()
        {
            var doc = new System.Xml.XmlDocument ();
            doc.LoadXml (TwoSectionReport ());

            Rdl2008Normalizer.Normalize (doc, null);

            var rectangle = doc.GetElementsByTagName ("Rectangle").Cast<System.Xml.XmlElement> ().Single (r => r.GetAttribute ("Name") == "RdlSection2");
            var ns = rectangle.NamespaceURI;
            Assert.That (rectangle["Top", ns].InnerText, Is.EqualTo ("1in"), "below the first body");
            Assert.That (rectangle["PageBreakAtStart", ns].InnerText, Is.EqualTo ("true"));
            Assert.That (doc.DocumentElement["Body", ns]["Height", ns].InnerText, Is.EqualTo ("3in"), "the body grows to hold both");
        }

        [Test]
        public async Task ReportSectionsWithUnreadableHeights_KeepOnlyTheFirstAndSaySo ()
        {
            RdlEngineConfig.RdlEngineConfigInit ();
            var parser = new RDLParser (TwoSectionReport (secondHeight: "=Code.Height()")) { SkipDatabaseSchemaValidation = true };
            using var report = await parser.Parse ();

            var messages = report.ErrorItems.Cast<string> ().ToArray ();
            Assert.That (messages.Single (m => m.Contains ("ReportSections")), Does.Contain ("could not be placed"));
        }
    }
}
