using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// An item below a Rectangle is placed below the Rectangle's real bottom on the page its
    /// contents end on, as it is below the same items placed straight in the body (#573), with
    /// KeepTogether on or off. Once the contents ran onto a later page the follower was placed
    /// from the Rectangle's designed bottom on its first page, so it printed at its own Top on
    /// the last page, over the last rows, and more rows no longer moved it. When the contents end
    /// by breaking onto a page that holds none of them, or the Rectangle breaks the page after
    /// itself, the follower starts at the top of that page, as below a table that breaks.
    /// </summary>
    [TestFixture]
    public class RectangleSplitFollowerTests
    {
        [SetUp]
        public void SetUp() => RdlEngineConfig.RdlEngineConfigInit();

        private const string Head = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>6in</Width><PageHeight>4in</PageHeight><PageWidth>6in</PageWidth>
  <TopMargin>0.25in</TopMargin><BottomMargin>0.25in</BottomMargin><LeftMargin>0.25in</LeftMargin><RightMargin>0.25in</RightMargin>
  <DataSources><DataSource Name=""DS""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString/></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data""><Query><DataSourceName>DS</DataSourceName><CommandText>SELECT 1</CommandText></Query>
    <Fields><Field Name=""Name""><DataField>Name</DataField></Field></Fields></DataSet></DataSets>
  <Body><Height>1.5in</Height><ReportItems>";

        // The follower is 0.25in below the designed bottom of what is above it.
        private const string Tail = @"
    <Textbox Name=""Follower""><Top>1.25in</Top><Left>0in</Left><Height>0.25in</Height><Width>2in</Width><Value>FOLLOWER</Value></Textbox>
  </ReportItems></Body>
</Report>";

        private const string BreakAtEnd = "<PageBreakAtEnd>true</PageBreakAtEnd>";

        private static string Title(string top) => $@"
        <Textbox Name=""Title""><Top>{top}</Top><Left>0in</Left><Height>0.25in</Height><Width>2in</Width><Value>TITLE</Value></Textbox>";

        private static string Table(string top, bool breakAtEnd) => $@"
        <Table Name=""T"">
          <DataSetName>Data</DataSetName><Top>{top}</Top><Left>0in</Left>{(breakAtEnd ? BreakAtEnd : "")}
          <TableColumns><TableColumn><Width>4in</Width></TableColumn></TableColumns>
          <Details><TableRows><TableRow><Height>0.25in</Height><TableCells>
            <TableCell><ReportItems><Textbox Name=""D1""><Value>=Fields!Name.Value</Value></Textbox></ReportItems></TableCell>
          </TableCells></TableRow></TableRows></Details>
        </Table>";

        // The title and the table straight in the body, at the places the rectangle puts them.
        private static string Flat(bool tableBreaksAtEnd = false)
            => Head + Title("0.5in") + Table("0.75in", tableBreaksAtEnd) + Tail;

        // The same title and table in a bordered rectangle at 0.5in, designed 0.5in high.
        private static string InRectangle(bool keepTogether, bool tableBreaksAtEnd = false, bool breakAtEnd = false) => Head + $@"
    <Rectangle Name=""Box"">
      <Top>0.5in</Top><Left>0in</Left><Height>0.5in</Height><Width>4in</Width>{(keepTogether ? "<KeepTogether>true</KeepTogether>" : "")}{(breakAtEnd ? BreakAtEnd : "")}
      <Style><BorderStyle><Default>Solid</Default></BorderStyle></Style>
      <ReportItems>" + Title("0in") + Table("0.25in", tableBreaksAtEnd) + @"
      </ReportItems>
    </Rectangle>" + Tail;

        private static async Task<List<Page>> Pages(string rdl, int rows)
        {
            var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(8), string.Join(" | ", report.ErrorItems?.Cast<string>() ?? Enumerable.Empty<string>()));
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            for (int i = 0; i < rows; i++) dt.Rows.Add("ROW-" + i);
            await report.DataSets["Data"].SetData(dt);
            await report.RunGetData(null);
            return (await report.BuildPages()).Cast<Page>().ToList();
        }

        private static (int Page, PageText Text) Find(List<Page> pages, string text)
            => pages.SelectMany(p => p.Cast<PageItem>().OfType<PageText>().Where(t => t.Text == text).Select(t => (p.PageNumber, t))).Single();

        private static async Task<(int Page, float Y)> Follower(string rdl, int rows)
        {
            var (page, text) = Find(await Pages(rdl, rows), "FOLLOWER");
            return (page, text.Y);
        }

        // 10 rows fit the first page; 14, 16 and 20 run onto the second.
        [TestCase(10, false)]
        [TestCase(14, false)]
        [TestCase(16, false)]
        [TestCase(20, false)]
        [TestCase(10, true)]
        [TestCase(14, true)]
        [TestCase(20, true)]
        public async Task An_item_below_a_rectangle_follows_its_contents_as_it_does_without_the_rectangle(int rows, bool keepTogether)
        {
            var flat = await Follower(Flat(), rows);
            var boxed = await Follower(InRectangle(keepTogether), rows);

            Assert.Multiple(() =>
            {
                Assert.That(boxed.Page, Is.EqualTo(flat.Page), "the follower's page");
                Assert.That(boxed.Y, Is.EqualTo(flat.Y).Within(0.5f), "the follower's position on that page");
            });
        }

        [TestCase(16)]
        [TestCase(20)]
        public async Task An_item_below_a_rectangle_that_ran_onto_the_next_page_keeps_its_gap_below_the_last_row(int rows)
        {
            var pages = await Pages(InRectangle(false), rows);
            var lastRow = Find(pages, "ROW-" + (rows - 1));
            var follower = Find(pages, "FOLLOWER");

            Assert.That(follower.Page, Is.EqualTo(lastRow.Page));
            Assert.That(follower.Text.Y, Is.EqualTo(lastRow.Text.Y + lastRow.Text.H + 18f).Within(0.5f));
        }

        // A table that breaks the page after itself leaves its follower at the top of the next
        // page, the rectangle holding it does the same, whether its rows fit one page or not.
        [TestCase(4)]
        [TestCase(20)]
        public async Task A_rectangle_whose_contents_end_with_a_page_break_puts_the_item_below_it_at_the_top_of_the_next_page(int rows)
        {
            var flat = await Follower(Flat(tableBreaksAtEnd: true), rows);
            var pages = await Pages(InRectangle(false, tableBreaksAtEnd: true), rows);
            var follower = Find(pages, "FOLLOWER");
            var lastRow = Find(pages, "ROW-" + (rows - 1));

            Assert.Multiple(() =>
            {
                Assert.That(follower.Page, Is.EqualTo(flat.Page), "the follower's page");
                Assert.That(follower.Text.Y, Is.EqualTo(flat.Y).Within(0.5f), "the follower's position on that page");
                // the rectangle is closed below its last row, on the page before the follower's
                var box = pages.Single(p => p.PageNumber == lastRow.Page).Cast<PageItem>().OfType<PageRectangle>()
                    .Single(r => System.Math.Abs(r.W - 288f) < 1f);
                Assert.That(box.SI.BStyleBottom, Is.EqualTo(BorderStyleEnum.Solid), "the bottom border of the rectangle's last piece");
            });
        }

        [TestCase(4)]
        [TestCase(20)]
        public async Task A_rectangle_that_breaks_the_page_after_itself_puts_the_item_below_it_at_the_top_of_the_next_page(int rows)
        {
            var flat = await Follower(Flat(tableBreaksAtEnd: true), rows);
            var boxed = await Follower(InRectangle(false, breakAtEnd: true), rows);

            Assert.Multiple(() =>
            {
                Assert.That(boxed.Page, Is.EqualTo(flat.Page), "the follower's page");
                Assert.That(boxed.Y, Is.EqualTo(flat.Y).Within(0.5f), "the follower's position on that page");
            });
        }
    }
}
