#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// Page breaks around a table row that holds a Rectangle filling its only cell: a row that
    /// is to be kept together and grows past the bottom of the page moves to the next page, the
    /// Rectangle's PageBreakAtStart and PageBreakAtEnd break the table before and after the row,
    /// and hidden rows take no room when a table footer is fitted on the page.
    /// </summary>
    [TestFixture]
    public class TableBandPaginationTests
    {
        private static string Rdl(string headerRows, string footerRows = "") => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>300pt</Width>
  <Body><Height>300pt</Height><ReportItems>
    <Table Name=""T"">
      <Width>200pt</Width>
      <TableColumns><TableColumn><Width>200pt</Width></TableColumn></TableColumns>
      <Header>
        <TableRows>{headerRows}</TableRows>
      </Header>
      {(footerRows == "" ? "" : $"<Footer><TableRows>{footerRows}</TableRows></Footer>")}
    </Table>
  </ReportItems></Body>
  <PageHeight>300pt</PageHeight><PageWidth>400pt</PageWidth>
</Report>";

        private static string TextRow(string text, float height = 20, string extra = "") =>
            $@"<TableRow><Height>{height}pt</Height>{extra}<TableCells><TableCell><ReportItems>
                 <Textbox Name=""Tb{text}""><Value>{text}</Value><Style><FontSize>10pt</FontSize></Style></Textbox>
               </ReportItems></TableCell></TableCells></TableRow>";

        private static string RectangleRow(string text, string rectangleElements, string value = null) =>
            $@"<TableRow><Height>20pt</Height><TableCells><TableCell><ReportItems>
                 <Rectangle Name=""R{text}"">{rectangleElements}<ReportItems>
                   <Textbox Name=""Tb{text}""><Top>2pt</Top><Left>0pt</Left><Width>200pt</Width><Height>12pt</Height>
                     <Value>{value ?? text}</Value><CanGrow>true</CanGrow><Style><FontSize>10pt</FontSize></Style></Textbox>
                 </ReportItems></Rectangle>
               </ReportItems></TableCell></TableCells></TableRow>";

        // Twelve lines of 10pt text: well over 100pt, which is more than is left below a 200pt row.
        private const string Lines =
            @"=""1"" &amp; Chr(10) &amp; ""2"" &amp; Chr(10) &amp; ""3"" &amp; Chr(10) &amp; ""4"" &amp; Chr(10) &amp; ""5"" &amp; Chr(10) &amp; ""6"" &amp; Chr(10) &amp; ""7"" &amp; Chr(10) &amp; ""8"" &amp; Chr(10) &amp; ""9"" &amp; Chr(10) &amp; ""10"" &amp; Chr(10) &amp; ""11"" &amp; Chr(10) &amp; ""12""";

        // The page each text was printed on, and where.
        private static async Task<(int Page, PageText Text)[]> Printed(string rdl)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            return Enumerable.Range(0, pages.Count)
                .SelectMany(i => pages[i].Cast<PageItem>().OfType<PageText>().Select(t => (i + 1, t)))
                .ToArray();
        }

        private static (int Page, PageText Text) Find((int Page, PageText Text)[] printed, string start) =>
            printed.First(p => p.Text.Text != null && p.Text.Text.StartsWith(start));

        [Test]
        public async Task ARowKeptTogetherThatGrowsPastThePage_MovesToTheNextPage()
        {
            var printed = await Printed(Rdl(
                TextRow("FIRST", 200)
                + RectangleRow("GROWN", "<KeepTogether>true</KeepTogether>", Lines)
                + TextRow("NEXT")));

            var grown = Find(printed, "1");
            var next = Find(printed, "NEXT");
            Assert.That(grown.Page, Is.EqualTo(2));
            Assert.That(grown.Text.Y, Is.LessThan(10f), "at the top of the page");
            Assert.That(printed.Count(p => p.Text.Text != null && p.Text.Text.StartsWith("1\n")), Is.EqualTo(1),
                "printed once");
            Assert.That(next.Page, Is.EqualTo(2));
            Assert.That(next.Text.Y, Is.GreaterThanOrEqualTo(grown.Text.Y + grown.Text.H - 0.01f));
        }

        // A report that does not ask for it keeps the row where it starts.
        [Test]
        public async Task ARowNotKeptTogether_StaysOnItsPage()
        {
            var printed = await Printed(Rdl(
                TextRow("FIRST", 200)
                + RectangleRow("GROWN", "", Lines)));

            Assert.That(Find(printed, "1").Page, Is.EqualTo(1));
        }

        // A row kept together that fits stays where it is.
        [Test]
        public async Task ARowKeptTogetherThatFits_StaysOnItsPage()
        {
            var printed = await Printed(Rdl(
                TextRow("FIRST", 100)
                + RectangleRow("GROWN", "<KeepTogether>true</KeepTogether>", Lines)));

            Assert.That(Find(printed, "1").Page, Is.EqualTo(1));
        }

        [Test]
        public async Task ARectangleBreakingAtStart_StartsANewPage()
        {
            var printed = await Printed(Rdl(
                TextRow("FIRST")
                + RectangleRow("SECOND", "<PageBreakAtStart>true</PageBreakAtStart>")));

            Assert.That(Find(printed, "FIRST").Page, Is.EqualTo(1));
            Assert.That(Find(printed, "SECOND").Page, Is.EqualTo(2));
        }

        [Test]
        public async Task ARectangleBreakingAtEnd_EndsThePage()
        {
            var printed = await Printed(Rdl(
                RectangleRow("FIRST", "<PageBreakAtEnd>true</PageBreakAtEnd>")
                + TextRow("SECOND")));

            Assert.That(Find(printed, "FIRST").Page, Is.EqualTo(1));
            Assert.That(Find(printed, "SECOND").Page, Is.EqualTo(2));
        }

        // A hidden Rectangle does not break the page.
        [Test]
        public async Task AHiddenRectangleBreakingAtStart_DoesNotBreak()
        {
            var printed = await Printed(Rdl(
                TextRow("FIRST")
                + RectangleRow("SECOND", "<Visibility><Hidden>true</Hidden></Visibility><PageBreakAtStart>true</PageBreakAtStart>")
                + TextRow("THIRD")));

            Assert.That(Find(printed, "THIRD").Page, Is.EqualTo(1));
        }

        // The footer's visible row fits below the header; its hidden rows would not.
        [TestCase(false)]
        [TestCase(true)]
        public async Task HiddenFooterRows_DoNotPushTheFooterToANewPage(bool growingRow)
        {
            string hidden = "<Visibility><Hidden>true</Hidden></Visibility>";
            string visible = growingRow
                ? @"<TableRow><Height>20pt</Height><TableCells><TableCell><ReportItems>
                     <Textbox Name=""TbFOOT""><Value>FOOT</Value><CanGrow>true</CanGrow><Style><FontSize>10pt</FontSize></Style></Textbox>
                   </ReportItems></TableCell></TableCells></TableRow>"
                : TextRow("FOOT");
            var printed = await Printed(Rdl(
                TextRow("FIRST", 200),
                visible + TextRow("H1", 100, hidden) + TextRow("H2", 100, hidden)));

            Assert.That(Find(printed, "FOOT").Page, Is.EqualTo(1));
        }
    }
}
#endif
