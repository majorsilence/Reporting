using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// The rows of a footer are kept on a page together: when they do not all fit in what is left
    /// of the page, the footer starts the next one. A table with Custom KeepFooterRowsTogether
    /// false prints each row where it fits, and the first that does not fit starts the next page.
    /// </summary>
    [TestFixture]
    public class TableFooterRowsTogetherTests
    {
        [SetUp]
        public void SetUp() => RdlEngineConfig.RdlEngineConfigInit();

        // Both tables leave the last detail row where it fits, so that only the footer's own rows
        // are in question.
        private const string Together = "<Custom><KeepGroupFootersWithLastRow>false</KeepGroupFootersWithLastRow></Custom>";
        private const string Apart = "<Custom><KeepGroupFootersWithLastRow>false</KeepGroupFootersWithLastRow>"
            + "<KeepFooterRowsTogether>false</KeepFooterRowsTogether></Custom>";

        private static string Row(string name, string text) => $@"<TableRow><Height>60pt</Height><TableCells><TableCell><ReportItems>
          <Textbox Name=""{name}""><Value>{text}</Value></Textbox>
        </ReportItems></TableCell></TableCells></TableRow>";

        // A 200pt page with no margins: five 20pt details in one group, whose footer is two 60pt
        // rows. The details leave 100pt: room for one of the footer's rows, not for both.
        private static string Rdl(string tableCustom) => $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>200pt</Width>
  <PageHeight>200pt</PageHeight><PageWidth>300pt</PageWidth>
  <TopMargin>0pt</TopMargin><BottomMargin>0pt</BottomMargin>
  <DataSources><DataSource Name=""DS""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString/></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data""><Query><DataSourceName>DS</DataSourceName><CommandText>SELECT 1</CommandText></Query>
    <Fields><Field Name=""Name""><DataField>Name</DataField></Field><Field Name=""Dept""><DataField>Dept</DataField></Field></Fields></DataSet></DataSets>
  <Body><Height>100pt</Height><ReportItems>
    <Table Name=""T"">
      {tableCustom}
      <DataSetName>Data</DataSetName>
      <TableColumns><TableColumn><Width>200pt</Width></TableColumn></TableColumns>
      <TableGroups><TableGroup>
        <Grouping Name=""G""><GroupExpressions><GroupExpression>=Fields!Dept.Value</GroupExpression></GroupExpressions></Grouping>
        <Footer><TableRows>{Row("GF1", "NOTES")}{Row("GF2", "SLIP")}</TableRows></Footer>
      </TableGroup></TableGroups>
      <Details><TableRows><TableRow><Height>20pt</Height><TableCells>
        <TableCell><ReportItems><Textbox Name=""D""><Value>=Fields!Name.Value</Value></Textbox></ReportItems></TableCell>
      </TableCells></TableRow></TableRows></Details>
    </Table>
  </ReportItems></Body>
</Report>";

        private static async Task<List<List<PageText>>> Printed(string rdl)
        {
            var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(4));
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("Dept", typeof(string));
            for (int i = 0; i < 5; i++) dt.Rows.Add($"A-{i}", "A");
            await report.DataSets["Data"].SetData(dt);
            await report.RunGetData(null);
            using var pages = await report.BuildPages();
            return pages.Cast<Page>().Select(p => p.Cast<PageItem>().OfType<PageText>().ToList()).ToList();
        }

        private static float Top(List<PageText> page, string text) => page.Single(t => t.Text == text).Y;

        [Test]
        public async Task AFooterThatDoesNotFit_StartsTheNextPageWhole()
        {
            var pages = await Printed(Rdl(Together));

            Assert.That(pages, Has.Count.EqualTo(2));
            Assert.That(pages[0].Select(t => t.Text), Does.Not.Contain("NOTES"));
            Assert.That(Top(pages[1], "NOTES"), Is.EqualTo(0f).Within(0.5f));
            Assert.That(Top(pages[1], "SLIP"), Is.EqualTo(60f).Within(0.5f));
        }

        [Test]
        public async Task WithRowsApart_EachFooterRowPrintsWhereItFits()
        {
            var pages = await Printed(Rdl(Apart));

            Assert.That(pages, Has.Count.EqualTo(2));
            Assert.That(Top(pages[0], "NOTES"), Is.EqualTo(100f).Within(0.5f));
            Assert.That(Top(pages[1], "SLIP"), Is.EqualTo(0f).Within(0.5f));
        }
    }
}
