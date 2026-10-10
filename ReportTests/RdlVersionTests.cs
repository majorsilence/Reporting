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
        [TestCase ("Sparkline")]
        [TestCase ("DataBar")]
        [TestCase ("Indicator")]
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
        public async Task SeveralUnsupportedItemsOfOneKind_GiveOneWarning ()
        {
            RdlEngineConfig.RdlEngineConfigInit ();
            var rdl = ReportWithItem (string.Concat (Enumerable.Range (1, 3).Select (i =>
                $@"<Sparkline Name=""S{i}""><Top>{i * 0.3}in</Top><Left>0in</Left><Height>0.25in</Height><Width>1in</Width></Sparkline>")));

            var parser = new RDLParser (rdl) { SkipDatabaseSchemaValidation = true };
            using var report = await parser.Parse ();

            var sparklineWarnings = report.ErrorItems.Cast<string> ().Count (e => e.Contains ("Sparkline"));
            Assert.That (sparklineWarnings, Is.EqualTo (1));
            Assert.That (report.ErrorItems.Cast<string> ().First (e => e.Contains ("Sparkline")), Does.Contain ("3"));
        }
    }
}
