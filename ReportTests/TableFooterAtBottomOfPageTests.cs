using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// A table's Custom FooterRowsAtBottomOfPage of n makes the last n rows of its Footer a footer
    /// for every page the table is on: they print at the foot of each page, and the table's other
    /// rows stop above them. The Footer's rows above those still print once, after the last row.
    /// Without it the Footer prints once, straight under the last row, as before.
    /// </summary>
    [TestFixture]
    public class TableFooterAtBottomOfPageTests
    {
        [SetUp]
        public void SetUp() => RdlEngineConfig.RdlEngineConfigInit();

        // A 400pt page with no margins: a 20pt header, 20pt details, and a Footer of a 20pt row
        // and a 40pt row.
        private static string Rdl(string custom) => $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>200pt</Width>
  <PageHeight>400pt</PageHeight><PageWidth>300pt</PageWidth>
  <TopMargin>0pt</TopMargin><BottomMargin>0pt</BottomMargin>
  <DataSources><DataSource Name=""DS""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString/></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data""><Query><DataSourceName>DS</DataSourceName><CommandText>SELECT 1</CommandText></Query>
    <Fields><Field Name=""Name""><DataField>Name</DataField></Field></Fields></DataSet></DataSets>
  <Body><Height>100pt</Height><ReportItems>
    <Table Name=""T"">
      <DataSetName>Data</DataSetName>{custom}
      <TableColumns><TableColumn><Width>200pt</Width></TableColumn></TableColumns>
      <Header><TableRows><TableRow><Height>20pt</Height><TableCells>
        <TableCell><ReportItems><Textbox Name=""H""><Value>HEADER</Value></Textbox></ReportItems></TableCell>
      </TableCells></TableRow></TableRows></Header>
      <Details><TableRows><TableRow><Height>20pt</Height><TableCells>
        <TableCell><ReportItems><Textbox Name=""D""><Value>=Fields!Name.Value</Value></Textbox></ReportItems></TableCell>
      </TableCells></TableRow></TableRows></Details>
      <Footer><TableRows>
        <TableRow><Height>20pt</Height><TableCells>
          <TableCell><ReportItems><Textbox Name=""Once""><Value>ONCE</Value></Textbox></ReportItems></TableCell>
        </TableCells></TableRow>
        <TableRow><Height>40pt</Height><TableCells>
          <TableCell><ReportItems><Textbox Name=""PageFoot""><Value>PAGEFOOT</Value></Textbox></ReportItems></TableCell>
        </TableCells></TableRow>
      </TableRows></Footer>
    </Table>
  </ReportItems></Body>
</Report>";

        private const string OneRowAtBottom = "\n      <Custom><FooterRowsAtBottomOfPage>1</FooterRowsAtBottomOfPage></Custom>";

        private static async Task<List<List<PageText>>> Printed(string rdl, int rows)
        {
            var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(4));
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            for (int i = 0; i < rows; i++) dt.Rows.Add("ROW-" + i);
            await report.DataSets["Data"].SetData(dt);
            await report.RunGetData(null);
            using var pages = await report.BuildPages();
            return pages.Cast<Page>().Select(p => p.Cast<PageItem>().OfType<PageText>().ToList()).ToList();
        }

        [Test]
        public async Task BottomRows_PrintAtTheFootOfThePage()
        {
            var pages = await Printed(Rdl(OneRowAtBottom), 3);

            Assert.That(pages, Has.Count.EqualTo(1));
            Assert.That(pages[0].Single(t => t.Text == "PAGEFOOT").Y, Is.EqualTo(360f).Within(0.5f));
            // The row above it still prints straight under the last row.
            Assert.That(pages[0].Single(t => t.Text == "ONCE").Y, Is.EqualTo(80f).Within(0.5f));
        }

        [Test]
        public async Task BottomRows_PrintOnEveryPage_AndTheRowsStopAboveThem()
        {
            var pages = await Printed(Rdl(OneRowAtBottom), 30);

            Assert.That(pages.Count, Is.GreaterThan(1));
            foreach (var page in pages)
            {
                var foot = page.Where(t => t.Text == "PAGEFOOT").ToList();
                Assert.That(foot, Has.Count.EqualTo(1));
                Assert.That(foot[0].Y, Is.EqualTo(360f).Within(0.5f));
                foreach (var row in page.Where(t => t.Text.StartsWith("ROW-") || t.Text == "ONCE"))
                    Assert.That(row.Y + row.H, Is.LessThanOrEqualTo(360f + 0.5f), row.Text);
            }
            Assert.That(pages.SelectMany(p => p).Count(t => t.Text == "ONCE"), Is.EqualTo(1));
            Assert.That(pages.Last().Any(t => t.Text == "ONCE"), Is.True);
            Assert.That(pages.SelectMany(p => p).Count(t => t.Text.StartsWith("ROW-")), Is.EqualTo(30));
        }

        [Test]
        public async Task BottomRows_PrintAtTheFootOfThePage_WithNoRows()
        {
            var pages = await Printed(Rdl(OneRowAtBottom), 0);

            Assert.That(pages, Has.Count.EqualTo(1));
            Assert.That(pages[0].Single(t => t.Text == "PAGEFOOT").Y, Is.EqualTo(360f).Within(0.5f));
            Assert.That(pages[0].Single(t => t.Text == "ONCE").Y, Is.EqualTo(20f).Within(0.5f));
        }

        [Test]
        public async Task WithoutIt_TheFooterPrintsOnceUnderTheLastRow()
        {
            var pages = await Printed(Rdl(""), 30);

            var all = pages.SelectMany(p => p).ToList();
            Assert.That(all.Count(t => t.Text == "PAGEFOOT"), Is.EqualTo(1));
            var last = pages.Last();
            Assert.That(last.Single(t => t.Text == "PAGEFOOT").Y,
                Is.EqualTo(last.Single(t => t.Text == "ONCE").Y + 20f).Within(0.5f));
        }
    }
}
