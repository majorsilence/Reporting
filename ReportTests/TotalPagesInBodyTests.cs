#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// Globals!TotalPages read in the body, such as a "Page N of M" box in a table header repeated
    /// on every page, prints the report's page count. Only the page header and footer were given
    /// the count, so in the body it printed 1.
    /// </summary>
    [TestFixture]
    public class TotalPagesInBodyTests
    {
        private static string Rdl(string header, string pageFooter = "") => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>400pt</Width>
  <DataSources><DataSource Name=""D""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>D</DataSourceName><CommandText>none</CommandText></Query>
    <Fields><Field Name=""V""><DataField>V</DataField><TypeName>Int32</TypeName></Field></Fields>
  </DataSet></DataSets>
  <Body><Height>300pt</Height><ReportItems>
    <Table Name=""T"">
      <DataSetName>Data</DataSetName>
      <TableColumns><TableColumn><Width>300pt</Width></TableColumn></TableColumns>
      <Header><RepeatOnNewPage>true</RepeatOnNewPage><TableRows><TableRow><Height>14pt</Height><TableCells><TableCell><ReportItems>
        <Textbox Name=""Head""><Value>{header}</Value></Textbox>
      </ReportItems></TableCell></TableCells></TableRow></TableRows></Header>
      <Details><TableRows><TableRow><Height>14pt</Height><TableCells><TableCell><ReportItems>
        <Textbox Name=""Line""><Value>=""D "" &amp; CStr(Fields!V.Value)</Value></Textbox>
      </ReportItems></TableCell></TableCells></TableRow></TableRows></Details>
    </Table>
  </ReportItems></Body>{pageFooter}
  <PageHeight>200pt</PageHeight><PageWidth>500pt</PageWidth>
  <TopMargin>10pt</TopMargin><BottomMargin>10pt</BottomMargin>
</Report>";

        private static async Task<Pages> Run(string header, string pageFooter = "")
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(Rdl(header, pageFooter)) { SkipDatabaseSchemaValidation = true }.Parse();
            var table = new DataTable();
            table.Columns.Add("V", typeof(int));
            for (int i = 1; i <= 40; i++)
                table.Rows.Add(i);
            await report.DataSets["Data"].SetData(table);
            await report.RunGetData(null);
            return await report.BuildPages();
        }

        private static string[] Heads(Pages pages, string prefix) =>
            pages.Cast<Page>().Select(p => p.Cast<PageItem>().OfType<PageText>()
                .Where(t => t.Text.StartsWith(prefix)).Select(t => t.Text).Single()).ToArray();

        [Test]
        public async Task ATableHeaderOnEveryPage_PrintsPageNOfM()
        {
            var pages = await Run(@"=""Page "" &amp; Globals!PageNumber &amp; "" of "" &amp; Globals!TotalPages");

            Assert.That(pages.Count, Is.GreaterThan(2), "40 rows of 14pt on 180pt pages");
            Assert.That(Heads(pages, "Page"),
                Is.EqualTo(Enumerable.Range(1, pages.Count).Select(n => $"Page {n} of {pages.Count}").ToArray()));
        }

        // The value is worked out again with the count, so a format or other text around it still applies.
        [Test]
        public async Task TheCountTakesPartInTheExpression()
        {
            var pages = await Run(@"=""N="" &amp; Format(Globals!TotalPages * 10, ""0.0"")");

            Assert.That(Heads(pages, "N="), Is.All.EqualTo($"N={pages.Count * 10}.0"));
        }

        // A group kept together that moves to the next page is run again there: the box it left
        // behind is gone, and the one on the next page has that page's number and the count.
        [Test]
        public async Task AGroupKeptTogetherThatMoves_PrintsItsNewPage()
        {
            RdlEngineConfig.RdlEngineConfigInit();
            string rdl = Rdl("x").Replace(
                @"<Details>",
                @"<TableGroups><TableGroup>
        <Grouping Name=""G""><GroupExpressions><GroupExpression>=Fields!K.Value</GroupExpression></GroupExpressions></Grouping>
        <KeepTogether>true</KeepTogether>
        <Header><TableRows><TableRow><Height>14pt</Height><TableCells><TableCell><ReportItems>
          <Textbox Name=""GroupHead""><Value>=""G "" &amp; Fields!K.Value &amp; "" "" &amp; Globals!PageNumber &amp; ""/"" &amp; Globals!TotalPages</Value></Textbox>
        </ReportItems></TableCell></TableCells></TableRow></TableRows></Header>
      </TableGroup></TableGroups>
      <Details>").Replace(@"<Field Name=""V"">", @"<Field Name=""K""><DataField>K</DataField></Field><Field Name=""V"">");
            using var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            var table = new DataTable();
            table.Columns.Add("K", typeof(string));
            table.Columns.Add("V", typeof(int));
            for (int i = 1; i <= 21; i++)
                table.Rows.Add(((char)('a' + (i - 1) / 7)).ToString(), i);   // three groups of seven rows, 112pt each
            await report.DataSets["Data"].SetData(table);
            await report.RunGetData(null);
            var pages = await report.BuildPages();

            var heads = pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>().OfType<PageText>()
                .Where(t => t.Text.StartsWith("G ")).Select(t => (p.PageNumber, t.Text))).ToArray();
            Assert.That(pages.Count, Is.EqualTo(3), "each group starts its own page");
            Assert.That(heads, Is.EqualTo(new[] { (1, "G a 1/3"), (2, "G b 2/3"), (3, "G c 3/3") }));
        }

        // A page footer reading the count prints it as before.
        [Test]
        public async Task APageFooterStillPrintsTheCount()
        {
            var pages = await Run("Head", @"
  <PageFooter><Height>14pt</Height><PrintOnFirstPage>true</PrintOnFirstPage><PrintOnLastPage>true</PrintOnLastPage><ReportItems>
    <Textbox Name=""Foot""><Value>=""Of "" &amp; Globals!TotalPages</Value></Textbox>
  </ReportItems></PageFooter>");

            Assert.That(Heads(pages, "Of "), Is.All.EqualTo($"Of {pages.Count}"));
        }
    }
}
#endif
