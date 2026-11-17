using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// A group's last detail row is kept on the page with the group footer that follows it: when
    /// the footer does not fit under the row, the row starts the next page with it. A table's
    /// Custom KeepGroupFootersWithLastRow of false leaves the row where it fits, and the footer
    /// moves to the next page on its own.
    /// </summary>
    [TestFixture]
    public class TableGroupFooterWithLastRowTests
    {
        [SetUp]
        public void SetUp() => RdlEngineConfig.RdlEngineConfigInit();

        // A 200pt page with no margins: 20pt details in one group, whose footer is 60pt.
        private static string Rdl(string custom) => $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>200pt</Width>
  <PageHeight>200pt</PageHeight><PageWidth>300pt</PageWidth>
  <TopMargin>0pt</TopMargin><BottomMargin>0pt</BottomMargin>
  <DataSources><DataSource Name=""DS""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString/></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data""><Query><DataSourceName>DS</DataSourceName><CommandText>SELECT 1</CommandText></Query>
    <Fields><Field Name=""Name""><DataField>Name</DataField></Field><Field Name=""Dept""><DataField>Dept</DataField></Field></Fields></DataSet></DataSets>
  <Body><Height>100pt</Height><ReportItems>
    <Table Name=""T"">
      <DataSetName>Data</DataSetName>{custom}
      <TableColumns><TableColumn><Width>200pt</Width></TableColumn></TableColumns>
      <TableGroups><TableGroup>
        <Grouping Name=""G""><GroupExpressions><GroupExpression>=Fields!Dept.Value</GroupExpression></GroupExpressions></Grouping>
        <Footer><TableRows><TableRow><Height>60pt</Height><TableCells>
          <TableCell><ReportItems><Textbox Name=""F""><Value>TOTAL</Value></Textbox></ReportItems></TableCell>
        </TableCells></TableRow></TableRows></Footer>
      </TableGroup></TableGroups>
      <Details><TableRows><TableRow><Height>20pt</Height><TableCells>
        <TableCell><ReportItems><Textbox Name=""D""><Value>=Fields!Name.Value</Value></Textbox></ReportItems></TableCell>
      </TableCells></TableRow></TableRows></Details>
    </Table>
  </ReportItems></Body>
</Report>";

        private const string FootersOnTheirOwn = "\n      <Custom><KeepGroupFootersWithLastRow>false</KeepGroupFootersWithLastRow></Custom>";

        // Eight rows in one group: seven fill 140pt, the eighth fits (160pt) and the footer under
        // it does not (220pt of 200pt).
        private static async Task<List<List<string>>> Printed(string rdl)
        {
            var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(4));
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("Dept", typeof(string));
            for (int i = 0; i < 8; i++) dt.Rows.Add("ROW-" + i, "A");
            await report.DataSets["Data"].SetData(dt);
            await report.RunGetData(null);
            using var pages = await report.BuildPages();
            return pages.Cast<Page>().Select(p => p.Cast<PageItem>().OfType<PageText>().Select(t => t.Text).ToList()).ToList();
        }

        [Test]
        public async Task TheLastRow_MovesToTheNextPageWithItsGroupsFooter()
        {
            var pages = await Printed(Rdl(""));

            Assert.That(pages, Has.Count.EqualTo(2));
            Assert.That(pages[0], Does.Not.Contain("ROW-7"));
            Assert.That(pages[1], Is.EqualTo(new[] { "ROW-7", "TOTAL" }));
        }

        [Test]
        public async Task WithTheFootersOnTheirOwn_TheLastRowStaysWhereItFits()
        {
            var pages = await Printed(Rdl(FootersOnTheirOwn));

            Assert.That(pages, Has.Count.EqualTo(2));
            Assert.That(pages[0], Does.Contain("ROW-7"));
            Assert.That(pages[1], Is.EqualTo(new[] { "TOTAL" }));
        }
    }
}
