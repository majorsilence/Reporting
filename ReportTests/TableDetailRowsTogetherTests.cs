using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// The Details rows of one record are kept on a page together: when they do not all fit in
    /// what is left of the page, the record starts the next one. A table's Custom
    /// KeepDetailRowsTogether of false prints each of the record's rows where it fits, and the
    /// first that does not fit starts the next page.
    /// </summary>
    [TestFixture]
    public class TableDetailRowsTogetherTests
    {
        [SetUp]
        public void SetUp() => RdlEngineConfig.RdlEngineConfigInit();

        // A 190pt page with no margins; each record is two 20pt rows.
        private static string Rdl(string custom) => $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>200pt</Width>
  <PageHeight>190pt</PageHeight><PageWidth>300pt</PageWidth>
  <TopMargin>0pt</TopMargin><BottomMargin>0pt</BottomMargin>
  <DataSources><DataSource Name=""DS""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString/></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data""><Query><DataSourceName>DS</DataSourceName><CommandText>SELECT 1</CommandText></Query>
    <Fields><Field Name=""Name""><DataField>Name</DataField></Field></Fields></DataSet></DataSets>
  <Body><Height>100pt</Height><ReportItems>
    <Table Name=""T"">
      <DataSetName>Data</DataSetName>{custom}
      <TableColumns><TableColumn><Width>200pt</Width></TableColumn></TableColumns>
      <Details><TableRows>
        <TableRow><Height>20pt</Height><TableCells>
          <TableCell><ReportItems><Textbox Name=""A""><Value>=""A-"" &amp; Fields!Name.Value</Value></Textbox></ReportItems></TableCell>
        </TableCells></TableRow>
        <TableRow><Height>20pt</Height><TableCells>
          <TableCell><ReportItems><Textbox Name=""B""><Value>=""B-"" &amp; Fields!Name.Value</Value></Textbox></ReportItems></TableCell>
        </TableCells></TableRow>
      </TableRows></Details>
    </Table>
  </ReportItems></Body>
</Report>";

        private const string RowsApart = "\n      <Custom><KeepDetailRowsTogether>false</KeepDetailRowsTogether></Custom>";

        // Five records: four fill 160pt, the fifth's first row fits (180pt) and its second does
        // not (200pt of 190pt).
        private static async Task<List<List<string>>> Printed(string rdl)
        {
            var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(4));
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            for (int i = 0; i < 5; i++) dt.Rows.Add(i.ToString());
            await report.DataSets["Data"].SetData(dt);
            await report.RunGetData(null);
            using var pages = await report.BuildPages();
            return pages.Cast<Page>().Select(p => p.Cast<PageItem>().OfType<PageText>().Select(t => t.Text).ToList()).ToList();
        }

        [Test]
        public async Task ARecordsRows_MoveToTheNextPageTogether()
        {
            var pages = await Printed(Rdl(""));

            Assert.That(pages, Has.Count.EqualTo(2));
            Assert.That(pages[0].Last(), Is.EqualTo("B-3"));
            Assert.That(pages[1], Is.EqualTo(new[] { "A-4", "B-4" }));
        }

        [Test]
        public async Task WithTheRowsApart_EachPrintsWhereItFits()
        {
            var pages = await Printed(Rdl(RowsApart));

            Assert.That(pages, Has.Count.EqualTo(2));
            Assert.That(pages[0].Last(), Is.EqualTo("A-4"));
            Assert.That(pages[1], Is.EqualTo(new[] { "B-4" }));
        }
    }
}
