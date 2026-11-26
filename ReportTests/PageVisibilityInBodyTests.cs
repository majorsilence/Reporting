#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// A body item whose Hidden reads Globals!PageNumber or Globals!TotalPages - totals shown on
    /// the last page only, a note on every page but the first - is hidden by the page it is drawn
    /// on. Neither is known while the body is laid out: the count read 1 there, so "hide unless
    /// this is the last page" showed the item on the first page and hid it on every other.
    /// </summary>
    [TestFixture]
    public class PageVisibilityInBodyTests
    {
        private static string Rdl(string hidden, string region = null) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>400pt</Width>
  <DataSources><DataSource Name=""D""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>D</DataSourceName><CommandText>none</CommandText></Query>
    <Fields><Field Name=""V""><DataField>V</DataField><TypeName>Int32</TypeName></Field></Fields>
  </DataSet></DataSets>
  <Body><Height>300pt</Height><ReportItems>
    {region ?? $@"<Table Name=""T"">
      <DataSetName>Data</DataSetName>
      <TableColumns><TableColumn><Width>200pt</Width></TableColumn><TableColumn><Width>100pt</Width></TableColumn></TableColumns>
      <Details><TableRows><TableRow><Height>14pt</Height><TableCells>
        <TableCell><ReportItems><Textbox Name=""Line""><Value>=""D "" &amp; CStr(Fields!V.Value)</Value></Textbox></ReportItems></TableCell>
        <TableCell><ReportItems><Textbox Name=""Mark""><Value>=""Mark "" &amp; CStr(Fields!V.Value)</Value>{hidden}</Textbox></ReportItems></TableCell>
      </TableCells></TableRow></TableRows></Details>
    </Table>"}
  </ReportItems></Body>
  <PageHeight>200pt</PageHeight><PageWidth>500pt</PageWidth>
  <TopMargin>10pt</TopMargin><BottomMargin>10pt</BottomMargin>
</Report>";

        private static string Hidden(string expression) => $"<Visibility><Hidden>{expression}</Hidden></Visibility>";

        private static async Task<Pages> Run(string rdl)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            var table = new DataTable();
            table.Columns.Add("V", typeof(int));
            for (int i = 1; i <= 40; i++)
                table.Rows.Add(i);
            await report.DataSets["Data"].SetData(table);
            await report.RunGetData(null);
            return await report.BuildPages();
        }

        private static int[] Count(Pages pages, string prefix) =>
            pages.Cast<Page>().Select(p => p.Cast<PageItem>().OfType<PageText>().Count(t => t.Text.StartsWith(prefix))).ToArray();

        [Test]
        public async Task ABoxShownOnTheLastPageOnly_PrintsThereAlone()
        {
            var pages = await Run(Rdl(Hidden("=Globals!TotalPages &lt;&gt; Globals!PageNumber")));

            Assert.That(pages.Count, Is.GreaterThan(2), "40 rows of 14pt on 180pt pages");
            int[] rows = Count(pages, "D "), marks = Count(pages, "Mark ");
            Assert.That(marks.Take(pages.Count - 1), Is.All.EqualTo(0));
            Assert.That(marks.Last(), Is.EqualTo(rows.Last()).And.GreaterThan(0));
        }

        [Test]
        public async Task ABoxHiddenOnTheFirstPage_PrintsOnEveryOther()
        {
            var pages = await Run(Rdl(Hidden("=Globals!PageNumber = 1")));

            int[] rows = Count(pages, "D "), marks = Count(pages, "Mark ");
            Assert.That(marks[0], Is.EqualTo(0));
            Assert.That(marks.Skip(1), Is.EqualTo(rows.Skip(1)));
        }

        // The item keeps its room whether or not it prints: the rows fall on the same pages.
        [Test]
        public async Task TheRowsStayWhereTheyWere()
        {
            var plain = await Run(Rdl(""));
            var pages = await Run(Rdl(Hidden("=Globals!TotalPages &lt;&gt; Globals!PageNumber")));

            Assert.That(Count(pages, "D "), Is.EqualTo(Count(plain, "D ")));
        }

        [Test]
        public async Task ALineShownOnTheLastPageOnly_IsDrawnThereAlone()
        {
            var pages = await Run(Rdl("", $@"<List Name=""L"">
      <DataSetName>Data</DataSetName><Height>14pt</Height><Width>300pt</Width>
      <ReportItems>
        <Textbox Name=""Line""><Height>12pt</Height><Width>200pt</Width><Value>=""D "" &amp; CStr(Fields!V.Value)</Value></Textbox>
        <Line Name=""Rule""><Top>13pt</Top><Left>0pt</Left><Height>0pt</Height><Width>300pt</Width>
          <Style><BorderStyle><Default>Solid</Default></BorderStyle></Style>{Hidden("=Globals!TotalPages &lt;&gt; Globals!PageNumber")}</Line>
      </ReportItems>
    </List>"));

            Assert.That(pages.Count, Is.GreaterThan(2));
            int[] rows = Count(pages, "D ");
            int[] rules = pages.Cast<Page>().Select(p => p.Cast<PageItem>().OfType<PageLine>().Count()).ToArray();
            Assert.That(rules.Take(pages.Count - 1), Is.All.EqualTo(0));
            Assert.That(rules.Last(), Is.EqualTo(rows.Last()).And.GreaterThan(0));
        }

        // In the page footer the page values were always known; an item hidden by them there is as before.
        [Test]
        public async Task APageFooterItemHiddenByPage_IsAsBefore()
        {
            var pages = await Run(Rdl("").Replace("<PageHeight>", $@"<PageFooter><Height>14pt</Height><PrintOnFirstPage>true</PrintOnFirstPage><PrintOnLastPage>true</PrintOnLastPage><ReportItems>
    <Textbox Name=""Foot""><Value>Foot</Value>{Hidden("=Globals!TotalPages &lt;&gt; Globals!PageNumber")}</Textbox>
  </ReportItems></PageFooter><PageHeight>"));

            int[] feet = Count(pages, "Foot");
            Assert.That(feet.Take(pages.Count - 1), Is.All.EqualTo(0));
            Assert.That(feet.Last(), Is.EqualTo(1));
        }
    }
}
#endif
