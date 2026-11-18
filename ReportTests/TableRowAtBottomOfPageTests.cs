using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// A table row whose only cell holds a Rectangle with Custom PrintAtBottomOfPage prints at the
    /// foot of the page it falls on: it is moved down so that it and the rows after it in its
    /// footer end at the bottom of the page. Without the element a row prints straight under the
    /// row before it.
    /// </summary>
    [TestFixture]
    public class TableRowAtBottomOfPageTests
    {
        [SetUp]
        public void SetUp() => RdlEngineConfig.RdlEngineConfigInit();

        private const string AtBottom = "<Custom><PrintAtBottomOfPage>true</PrintAtBottomOfPage></Custom>";

        private static string Band(string name, string text, string custom) => $@"<TableCell><ReportItems>
          <Rectangle Name=""{name}"">{custom}<ReportItems>
            <Textbox Name=""{name}T""><Top>0pt</Top><Left>0pt</Left><Width>100pt</Width><Height>12pt</Height><Value>{text}</Value></Textbox>
          </ReportItems></Rectangle>
        </ReportItems></TableCell>";

        // A 200pt page with no margins: 20pt details in one group. The group's footer is a 30pt
        // row and a 20pt row; the table's footer is a 20pt row.
        private static string Rdl(string groupFooterCustom, string tableFooterCustom) => $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>200pt</Width>
  <PageHeight>200pt</PageHeight><PageWidth>300pt</PageWidth>
  <TopMargin>0pt</TopMargin><BottomMargin>0pt</BottomMargin>
  <DataSources><DataSource Name=""DS""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString/></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data""><Query><DataSourceName>DS</DataSourceName><CommandText>SELECT 1</CommandText></Query>
    <Fields><Field Name=""Name""><DataField>Name</DataField></Field><Field Name=""Dept""><DataField>Dept</DataField></Field></Fields></DataSet></DataSets>
  <Body><Height>100pt</Height><ReportItems>
    <Table Name=""T"">
      <DataSetName>Data</DataSetName>
      <TableColumns><TableColumn><Width>200pt</Width></TableColumn></TableColumns>
      <TableGroups><TableGroup>
        <Grouping Name=""G""><GroupExpressions><GroupExpression>=Fields!Dept.Value</GroupExpression></GroupExpressions></Grouping>
        <Footer><TableRows>
          <TableRow><Height>30pt</Height><TableCells>{Band("GF1", "TOTAL", groupFooterCustom)}</TableCells></TableRow>
          <TableRow><Height>20pt</Height><TableCells>{Band("GF2", "REMIT", "")}</TableCells></TableRow>
        </TableRows></Footer>
      </TableGroup></TableGroups>
      <Details><TableRows><TableRow><Height>20pt</Height><TableCells>
        <TableCell><ReportItems><Textbox Name=""D""><Value>=Fields!Name.Value</Value></Textbox></ReportItems></TableCell>
      </TableCells></TableRow></TableRows></Details>
      <Footer><TableRows>
        <TableRow><Height>20pt</Height><TableCells>{Band("TF", "END", tableFooterCustom)}</TableCells></TableRow>
      </TableRows></Footer>
    </Table>
  </ReportItems></Body>
</Report>";

        private static async Task<List<List<PageText>>> Printed(string rdl, params string[] depts)
        {
            var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(4));
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("Dept", typeof(string));
            foreach (string dept in depts)
                for (int i = 0; i < 2; i++) dt.Rows.Add($"{dept}-{i}", dept);
            await report.DataSets["Data"].SetData(dt);
            await report.RunGetData(null);
            using var pages = await report.BuildPages();
            return pages.Cast<Page>().Select(p => p.Cast<PageItem>().OfType<PageText>().ToList()).ToList();
        }

        private static float Top(List<PageText> page, string text) => page.Single(t => t.Text == text).Y;

        [Test]
        public async Task WithoutTheElement_AFooterPrintsUnderTheLastRow()
        {
            var pages = await Printed(Rdl("", ""), "A");

            Assert.That(pages, Has.Count.EqualTo(1));
            Assert.That(Top(pages[0], "TOTAL"), Is.EqualTo(40f).Within(0.5f));
            Assert.That(Top(pages[0], "REMIT"), Is.EqualTo(70f).Within(0.5f));
            Assert.That(Top(pages[0], "END"), Is.EqualTo(90f).Within(0.5f));
        }

        [Test]
        public async Task AGroupFooterRow_PrintsAtTheFootOfThePage_WithTheRowsAfterIt()
        {
            var pages = await Printed(Rdl(AtBottom, ""), "A", "B");

            // The first group's footer ends the first page, so the second group starts the next.
            Assert.That(pages, Has.Count.EqualTo(3));
            Assert.That(Top(pages[0], "TOTAL"), Is.EqualTo(150f).Within(0.5f));
            Assert.That(Top(pages[0], "REMIT"), Is.EqualTo(180f).Within(0.5f));
            Assert.That(pages[1].Select(t => t.Text), Does.Contain("B-0"));
            Assert.That(Top(pages[1], "TOTAL"), Is.EqualTo(150f).Within(0.5f));
            // The table's footer has no room left under it, and prints at the top of the last page.
            Assert.That(Top(pages[2], "END"), Is.EqualTo(0f).Within(0.5f));
        }

        [Test]
        public async Task ATableFooterRow_PrintsAtTheFootOfThePage()
        {
            var pages = await Printed(Rdl("", AtBottom), "A");

            Assert.That(pages, Has.Count.EqualTo(1));
            Assert.That(Top(pages[0], "TOTAL"), Is.EqualTo(40f).Within(0.5f));
            Assert.That(Top(pages[0], "END"), Is.EqualTo(180f).Within(0.5f));
        }
    }
}
