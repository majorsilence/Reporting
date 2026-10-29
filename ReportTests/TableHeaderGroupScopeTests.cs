#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// A table's header repeated on each page, showing an aggregate scoped to a group, has the
    /// total of the group the page starts in. On the first page, before any group had started,
    /// the group was unset, so the total was blank.
    /// </summary>
    [TestFixture]
    public class TableHeaderGroupScopeTests
    {
        private static string Rdl(string header, string groups, string breakAt = "PageBreakAtStart") => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>400pt</Width>
  <DataSources><DataSource Name=""D""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>D</DataSourceName><CommandText>none</CommandText></Query>
    <Fields>
      <Field Name=""K""><DataField>K</DataField></Field>
      <Field Name=""J""><DataField>J</DataField></Field>
      <Field Name=""V""><DataField>V</DataField><TypeName>Decimal</TypeName></Field>
      <Field Name=""GT""><Value>=Sum(Fields!V.Value, ""G"")</Value></Field>
    </Fields>
  </DataSet></DataSets>
  <Body><Height>300pt</Height><ReportItems>
    <Table Name=""T"">
      <DataSetName>Data</DataSetName>
      <TableColumns><TableColumn><Width>300pt</Width></TableColumn></TableColumns>
      <Header><RepeatOnNewPage>true</RepeatOnNewPage><TableRows><TableRow><Height>14pt</Height><TableCells><TableCell><ReportItems>
        <Textbox Name=""Head""><Value>{header}</Value></Textbox>
      </ReportItems></TableCell></TableCells></TableRow></TableRows></Header>
      <TableGroups>{groups.Replace("BREAK", breakAt)}</TableGroups>
      <Details><TableRows><TableRow><Height>14pt</Height><TableCells><TableCell><ReportItems>
        <Textbox Name=""Line""><Value>=""D "" &amp; CStr(Fields!V.Value)</Value></Textbox>
      </ReportItems></TableCell></TableCells></TableRow></TableRows></Details>
    </Table>
  </ReportItems></Body>
  <PageHeight>800pt</PageHeight><PageWidth>500pt</PageWidth>
</Report>";

        private const string OneGroup = @"<TableGroup>
        <Grouping Name=""G""><GroupExpressions><GroupExpression>=Fields!K.Value</GroupExpression></GroupExpressions><BREAK>true</BREAK></Grouping>
      </TableGroup>";

        // Groups listed last first, so the first group printed is not the one the first row is in.
        private const string OneGroupSortedDown = @"<TableGroup>
        <Grouping Name=""G""><GroupExpressions><GroupExpression>=Fields!K.Value</GroupExpression></GroupExpressions><BREAK>true</BREAK></Grouping>
        <Sorting><SortBy><SortExpression>=Fields!K.Value</SortExpression><Direction>Descending</Direction></SortBy></Sorting>
      </TableGroup>";

        private const string TwoGroups = @"<TableGroup>
        <Grouping Name=""G""><GroupExpressions><GroupExpression>=Fields!K.Value</GroupExpression></GroupExpressions></Grouping>
      </TableGroup>
      <TableGroup>
        <Grouping Name=""H""><GroupExpressions><GroupExpression>=Fields!J.Value</GroupExpression></GroupExpressions><BREAK>true</BREAK></Grouping>
      </TableGroup>";

        private static readonly (string K, string J, decimal V)[] Rows =
        {
            ("a", "x", 1m), ("a", "y", 2m), ("b", "x", 10m), ("b", "x", 20m), ("b", "y", 40m),
        };

        private static async Task<Report> Parse(string rdl, bool withRows)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4),
                string.Join(" | ", report.ErrorItems?.Cast<string>() ?? Enumerable.Empty<string>()));
            var table = new DataTable();
            table.Columns.Add("K", typeof(string));
            table.Columns.Add("J", typeof(string));
            table.Columns.Add("V", typeof(decimal));
            if (withRows)
                foreach (var (k, j, v) in Rows)
                    table.Rows.Add(k, j, v);
            await report.DataSets["Data"].SetData(table);
            await report.RunGetData(null);
            return report;
        }

        // The header line on each page.
        private static async Task<string[]> Headers(string rdl, bool withRows = true)
        {
            using var report = await Parse(rdl, withRows);
            var pages = await report.BuildPages();
            return pages.Cast<Page>()
                .Select(p => p.Cast<PageItem>().OfType<PageText>().OrderBy(t => t.Y).Select(t => t.Text).FirstOrDefault())
                .ToArray();
        }

        [Test]
        public async Task OnEachPage_TheHeaderHasTheTotalOfTheGroupThePageStartsIn()
        {
            var direct = await Headers(Rdl("=\"H \" &amp; CStr(Sum(Fields!V.Value, \"G\"))", OneGroup));
            var calculated = await Headers(Rdl("=\"H \" &amp; CStr(Fields!GT.Value)", OneGroup));

            Assert.That(direct, Is.EqualTo(new[] { "H 3", "H 70" }));
            Assert.That(calculated, Is.EqualTo(new[] { "H 3", "H 70" }));
        }

        [Test]
        public async Task AfterABreakAtTheGroupsEnd_TheHeaderHasTheNextGroupsTotal()
        {
            var headers = await Headers(Rdl("=Fields!K.Value &amp; \" \" &amp; CStr(Sum(Fields!V.Value, \"G\"))", OneGroup, "PageBreakAtEnd"));

            Assert.That(headers, Is.EqualTo(new[] { "a 3", "b 70" }));
        }

        [Test]
        public async Task WithNestedGroups_TheHeaderHasTheTotalsOfTheFirstGroupAtEachLevel()
        {
            var headers = await Headers(Rdl(
                "=\"H \" &amp; CStr(Sum(Fields!V.Value, \"G\")) &amp; \" \" &amp; CStr(Sum(Fields!V.Value, \"H\"))", TwoGroups));

            // a/x, a/y, b/x, b/y: a page for each inner group.
            Assert.That(headers, Is.EqualTo(new[] { "H 3 1", "H 3 2", "H 70 30", "H 70 40" }));
        }

        [Test]
        public async Task WithSortedGroups_TheHeaderIsTheFirstGroupPrinted()
        {
            var headers = await Headers(Rdl(
                "=Fields!K.Value &amp; \" \" &amp; CStr(Sum(Fields!V.Value, \"G\"))", OneGroupSortedDown));

            Assert.That(headers, Is.EqualTo(new[] { "b 70", "a 3" }));
        }

        // With no rows there is no group; the header still prints, over no values.
        [Test]
        public async Task WithNoRows_TheHeaderCountsNoValues()
        {
            var headers = await Headers(Rdl(
                "=\"H \" &amp; CStr(Count(Fields!V.Value, \"G\"))", OneGroup), withRows: false);

            Assert.That(headers, Is.EqualTo(new[] { "H 0" }));
        }

        [Test]
        public async Task InHtml_TheHeaderHasTheFirstGroupsTotal()
        {
            using var report = await Parse(Rdl("=\"H \" &amp; CStr(Fields!GT.Value)", OneGroup), true);
            using var memory = new MemoryStreamGen();
            await report.RunRender(memory, OutputPresentationType.HTML);
            var text = Regex.Replace(Regex.Replace(memory.GetText(), "<[^>]+>", " "), @"\s+", " ");

            Assert.That(text, Does.Contain(" H 3 "));
        }
    }
}
#endif
