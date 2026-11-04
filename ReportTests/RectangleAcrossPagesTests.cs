using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// A rectangle whose items run onto later pages is split across them, as in SSRS: the first
    /// page has its top and side borders down to the last item there, a middle page its sides,
    /// and the last page its sides and bottom border below the last item. The engine grew the
    /// rectangle only when its items ended on its own page, so a table that broke onto a second
    /// page left the rectangle at its design height and its bottom border was drawn through the
    /// table's first rows (Aged Debtors).
    /// </summary>
    [TestFixture]
    public class RectangleAcrossPagesTests
    {
        [SetUp]
        public void SetUp() => RdlEngineConfig.RdlEngineConfigInit();

        private const string Rdl = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>6in</Width><PageHeight>4in</PageHeight><PageWidth>6in</PageWidth>
  <TopMargin>0.25in</TopMargin><BottomMargin>0.25in</BottomMargin><LeftMargin>0.25in</LeftMargin><RightMargin>0.25in</RightMargin>
  <DataSources><DataSource Name=""DS""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString/></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data""><Query><DataSourceName>DS</DataSourceName><CommandText>SELECT 1</CommandText></Query>
    <Fields><Field Name=""Name""><DataField>Name</DataField></Field></Fields></DataSet></DataSets>
  <Body><Height>2in</Height><ReportItems>
    <Rectangle Name=""Box"">
      <Top>0in</Top><Left>0in</Left><Height>1in</Height><Width>4in</Width>
      <Style><BorderStyle><Default>Solid</Default></BorderStyle></Style>
      <ReportItems>
        <Table Name=""T"">
          <DataSetName>Data</DataSetName><Top>0.25in</Top><Left>0in</Left>
          <TableColumns><TableColumn><Width>4in</Width></TableColumn></TableColumns>
          <Header><TableRows><TableRow><Height>0.25in</Height><TableCells>
            <TableCell><ReportItems><Textbox Name=""H1""><Value>HEADER</Value></Textbox></ReportItems></TableCell>
          </TableCells></TableRow></TableRows></Header>
          <Details><TableRows><TableRow><Height>0.25in</Height><TableCells>
            <TableCell><ReportItems><Textbox Name=""D1""><Value>=Fields!Name.Value</Value></Textbox></ReportItems></TableCell>
          </TableCells></TableRow></TableRows></Details>
        </Table>
      </ReportItems>
    </Rectangle>
    <Textbox Name=""After""><Top>1.25in</Top><Left>0in</Left><Height>0.25in</Height><Width>2in</Width><Value>AFTER</Value></Textbox>
  </ReportItems></Body>
</Report>";

        private static async Task<List<Page>> Pages(int rows)
        {
            var report = await new RDLParser(Rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(8), string.Join(" | ", report.ErrorItems?.Cast<string>() ?? Enumerable.Empty<string>()));
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            for (int i = 0; i < rows; i++) dt.Rows.Add("ROW-" + i);
            await report.DataSets["Data"].SetData(dt);
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            pages.SortPageItems();
            return pages.Cast<Page>().ToList();
        }

        // The box's pieces: rectangles four inches wide (the table's cells are text, not rectangles).
        private static List<PageRectangle> Box(Page p)
            => p.Cast<PageItem>().OfType<PageRectangle>().Where(r => System.Math.Abs(r.W - 288f) < 1f).ToList();

        private static List<PageText> Rows(Page p)
            => p.Cast<PageItem>().OfType<PageText>().Where(t => t.Text != null && (t.Text.StartsWith("ROW-") || t.Text == "HEADER")).ToList();

        [Test]
        public async Task A_rectangle_whose_table_runs_onto_later_pages_is_split_like_SSRS()
        {
            var pages = await Pages(30);
            var withRows = pages.Where(p => Rows(p).Count > 0).ToList();
            Assert.That(withRows.Count, Is.EqualTo(3), "the table spans three pages");

            for (int i = 0; i < withRows.Count; i++)
            {
                var page = withRows[i];
                var box = Box(page);
                Assert.That(box.Count, Is.EqualTo(1), $"one piece of the box on page {page.PageNumber}");
                var piece = box[0];
                var rows = Rows(page);
                bool first = i == 0, last = i == withRows.Count - 1;

                Assert.That(piece.SI.BStyleTop, Is.EqualTo(first ? BorderStyleEnum.Solid : BorderStyleEnum.None), $"top border on page {page.PageNumber}");
                Assert.That(piece.SI.BStyleBottom, Is.EqualTo(last ? BorderStyleEnum.Solid : BorderStyleEnum.None), $"bottom border on page {page.PageNumber}");
                Assert.That(piece.SI.BStyleLeft, Is.EqualTo(BorderStyleEnum.Solid));
                Assert.That(piece.SI.BStyleRight, Is.EqualTo(BorderStyleEnum.Solid));
                Assert.That(piece.Y, Is.LessThanOrEqualTo(rows.Min(r => r.Y) + 0.5f), $"the piece starts above the rows on page {page.PageNumber}");
                Assert.That(piece.Y + piece.H, Is.GreaterThanOrEqualTo(rows.Max(r => r.Y + r.H) - 0.5f), $"the piece reaches below the rows on page {page.PageNumber}");

                var items = page.Cast<PageItem>().ToList();
                Assert.That(items.IndexOf(piece), Is.LessThan(rows.Min(r => items.IndexOf(r))), $"the piece is drawn behind the rows on page {page.PageNumber}");
            }

            var lastPage = withRows.Last();
            var after = pages.SelectMany(p => p.Cast<PageItem>().OfType<PageText>().Select(t => (Page: p, Text: t))).Single(x => x.Text.Text == "AFTER");
            Assert.That(after.Page.PageNumber, Is.GreaterThanOrEqualTo(lastPage.PageNumber), "the item below the box follows it");
            if (after.Page == lastPage)
                Assert.That(after.Text.Y, Is.GreaterThanOrEqualTo(Box(lastPage)[0].Y + Box(lastPage)[0].H - 0.5f), "the item below the box starts below its last piece");
        }

        [Test]
        public async Task A_rectangle_whose_items_fit_its_page_still_grows_to_them()
        {
            var pages = await Pages(6);
            var page = pages.Single(p => Rows(p).Count > 0);
            var box = Box(page).Single();

            Assert.That(box.SI.BStyleTop, Is.EqualTo(BorderStyleEnum.Solid));
            Assert.That(box.SI.BStyleBottom, Is.EqualTo(BorderStyleEnum.Solid));
            Assert.That(box.Y + box.H, Is.GreaterThanOrEqualTo(Rows(page).Max(r => r.Y + r.H) - 0.5f));
        }
    }
}
