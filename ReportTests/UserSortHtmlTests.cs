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
    /// Interactive sorting in HTML (a textbox's UserSort, issue #636): a sort header has a link
    /// that shows the current direction and asks for the next one, carrying the sorts already
    /// applied; the host renders the report again with <see cref="UserSortState.FromQuery"/>.
    /// The client-side sort from a table's Custom element stays for tables with no UserSort.
    /// </summary>
    [TestFixture]
    public class UserSortHtmlTests
    {
        private const string Rdl2005 = "http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition";

        [SetUp]
        public void SetUp() => RdlEngineConfig.RdlEngineConfigInit();

        // A table of "D <name><amount>" detail rows; the Name and Amount headers sort when
        // userSort is set; custom is the table's Custom element (the old client-side sort).
        private static string Report(bool userSort, string custom = "") => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""{Rdl2005}"">
  <Width>400pt</Width>
  <DataSources><DataSource Name=""D""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>D</DataSourceName><CommandText>none</CommandText></Query>
    <Fields>
      <Field Name=""Name""><DataField>Name</DataField></Field>
      <Field Name=""Amount""><DataField>Amount</DataField><TypeName>System.Int32</TypeName></Field>
    </Fields>
  </DataSet></DataSets>
  <Body><Height>600pt</Height><ReportItems>
<Table Name=""T""><DataSetName>Data</DataSetName>{custom}
  <TableColumns><TableColumn><Width>100pt</Width></TableColumn><TableColumn><Width>100pt</Width></TableColumn></TableColumns>
  <Header><TableRows><TableRow><Height>14pt</Height><TableCells>
    <TableCell><ReportItems><Textbox Name=""SortName""><Value>Name</Value>{(userSort ? "<UserSort><SortExpression>=Fields!Name.Value</SortExpression></UserSort>" : "")}</Textbox></ReportItems></TableCell>
    <TableCell><ReportItems><Textbox Name=""SortAmount""><Value>Amount</Value>{(userSort ? "<UserSort><SortExpression>=Fields!Amount.Value</SortExpression></UserSort>" : "")}</Textbox></ReportItems></TableCell>
  </TableCells></TableRow></TableRows></Header>
  <Details><TableRows><TableRow><Height>14pt</Height><TableCells>
    <TableCell><ReportItems><Textbox Name=""Line""><Value>=""D "" &amp; Fields!Name.Value &amp; CStr(Fields!Amount.Value)</Value></Textbox></ReportItems></TableCell>
    <TableCell><ReportItems><Textbox Name=""Amt""><Value>=Fields!Amount.Value</Value></Textbox></ReportItems></TableCell>
  </TableCells></TableRow></TableRows></Details>
</Table>
  </ReportItems></Body>
  <PageHeight>2000pt</PageHeight><PageWidth>500pt</PageWidth>
</Report>";

        private const string CustomSort = "<Custom><HTML>true</HTML></Custom>";

        private static async Task<Report> Load(string rdl)
        {
            var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(8), string.Join(" | ", report.ErrorItems?.Cast<string>() ?? Enumerable.Empty<string>()));
            var table = new DataTable();
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Amount", typeof(int));
            table.Rows.Add("c", 3);
            table.Rows.Add("a", 1);
            table.Rows.Add("b", 2);
            await report.DataSets["Data"].SetData(table);
            await report.RunGetData(null);
            return report;
        }

        private static async Task<string> Html(Report report)
        {
            using var sg = new MemoryStreamGen();
            await report.RunRender(sg, OutputPresentationType.HTML);
            return sg.GetText();
        }

        // The detail lines, top to bottom.
        private static string[] Lines(string html) =>
            Regex.Matches(html, @"D [a-z]\d").Select(m => m.Value).ToArray();

        // The sort link after the header textbox's text.
        private static Match SortLink(string html, string headerText)
        {
            var m = Regex.Match(html, Regex.Escape(headerText) + @"<a class=""rdl-usersort"" data-sort-direction=""(?<dir>[a-z]+)"" href=""(?<href>[^""]*)"" title=""(?<title>[^""]*)"" aria-label=""(?<label>[^""]*)""[^>]*>(?<glyph>[^<]*)</a>");
            Assert.That(m.Success, Is.True, "No sort link after '" + headerText + "' in: " + html);
            return m;
        }

        [Test]
        public async Task A_sort_header_links_to_the_ascending_sort()
        {
            var report = await Load(Report(userSort: true));
            string html = await Html(report);

            var link = SortLink(html, "Name");
            Assert.That(link.Groups["dir"].Value, Is.EqualTo("none"));
            Assert.That(link.Groups["href"].Value, Is.EqualTo("?rs:SortId=SortName&amp;rs:SortDirection=Ascending"));
            Assert.That(link.Groups["glyph"].Value, Is.EqualTo("&#x21C5;"));
            Assert.That(link.Groups["title"].Value, Is.EqualTo("Sort ascending"));
            Assert.That(link.Groups["label"].Value, Is.EqualTo("Sort ascending"));
            Assert.That(Lines(html), Is.EqualTo(new[] { "D c3", "D a1", "D b2" }));
        }

        [Test]
        public async Task A_sorted_header_shows_its_direction_and_links_to_the_other_one_with_the_state()
        {
            var report = await Load(Report(userSort: true));
            report.UserSort = UserSortState.FromQuery(null, "SortName", "Ascending");
            string html = await Html(report);

            Assert.That(Lines(html), Is.EqualTo(new[] { "D a1", "D b2", "D c3" }));
            var name = SortLink(html, "Name");
            Assert.That(name.Groups["dir"].Value, Is.EqualTo("ascending"));
            Assert.That(name.Groups["glyph"].Value, Is.EqualTo("&#x25B2;"));
            Assert.That(name.Groups["href"].Value,
                Is.EqualTo("?rs:SortId=SortName&amp;rs:SortDirection=Descending&amp;rs:SortState=ASortName"));
            Assert.That(name.Groups["title"].Value, Does.Contain("Sorted ascending"));

            // the other header is not sorted, but its link carries the Name sort
            var amount = SortLink(html, "Amount");
            Assert.That(amount.Groups["dir"].Value, Is.EqualTo("none"));
            Assert.That(amount.Groups["href"].Value,
                Is.EqualTo("?rs:SortId=SortAmount&amp;rs:SortDirection=Ascending&amp;rs:SortState=ASortName"));

            // following the Name link
            report.UserSort = UserSortState.FromQuery("ASortName", "SortName", "Descending");
            html = await Html(report);
            Assert.That(Lines(html), Is.EqualTo(new[] { "D c3", "D b2", "D a1" }));
            name = SortLink(html, "Name");
            Assert.That(name.Groups["dir"].Value, Is.EqualTo("descending"));
            Assert.That(name.Groups["glyph"].Value, Is.EqualTo("&#x25BC;"));
            Assert.That(name.Groups["href"].Value,
                Is.EqualTo("?rs:SortId=SortName&amp;rs:SortDirection=Ascending&amp;rs:SortState=DSortName"));
        }

        [Test]
        public async Task The_link_goes_to_the_hosts_url()
        {
            var report = await Load(Report(userSort: true));
            report.UserSortUrl = "/msr/RdlReport/ShowFile/R/html?Who=a b";
            Assert.That(SortLink(await Html(report), "Name").Groups["href"].Value,
                Is.EqualTo("/msr/RdlReport/ShowFile/R/html?Who=a b&amp;rs:SortId=SortName&amp;rs:SortDirection=Ascending"));

            report.UserSortUrl = "/report";
            Assert.That(SortLink(await Html(report), "Name").Groups["href"].Value,
                Is.EqualTo("/report?rs:SortId=SortName&amp;rs:SortDirection=Ascending"));
        }

        [Test]
        public void The_query_values_replace_the_sort_or_add_to_it()
        {
            Assert.That(UserSortState.FromQuery(null, null, null), Is.Null);
            Assert.That(UserSortState.FromQuery("", "SortName", "Sideways"), Is.Null, "an unknown direction is no click");
            Assert.That(UserSortState.FromQuery("ASortName", null, null).ToString(), Is.EqualTo("ASortName"));

            // a plain click replaces the sort
            Assert.That(UserSortState.FromQuery("ASortName", "SortAmount", "Descending").ToString(), Is.EqualTo("DSortAmount"));
            // a shift-click adds to it, or flips a sort in its place
            Assert.That(UserSortState.FromQuery("ASortName", "SortAmount", "descending", "true").ToString(), Is.EqualTo("ASortName,DSortAmount"));
            Assert.That(UserSortState.FromQuery("ASortName,DSortAmount", "SortName", "Descending", "true").ToString(), Is.EqualTo("DSortName,DSortAmount"));
            // a key with characters that need escaping survives the round trip
            var state = UserSortState.FromQuery(null, "Sub/Sort Name,1", "Ascending");
            Assert.That(UserSortState.Parse(state.ToString()).GetDirection("Sub/Sort Name,1"), Is.EqualTo(UserSortDirection.Ascending));
        }

        [Test]
        public async Task Without_a_user_sort_there_is_no_sort_link()
        {
            var report = await Load(Report(userSort: false));
            string html = await Html(report);
            Assert.That(html, Does.Not.Contain("rdl-usersort"));
            Assert.That(html, Does.Not.Contain("rs:SortId"));
            Assert.That(html, Does.Not.Contain("sort_table"));
        }

        [Test]
        public async Task The_custom_client_side_sort_still_works_without_a_user_sort()
        {
            var report = await Load(Report(userSort: false, custom: CustomSort));
            string html = await Html(report);
            Assert.That(html, Does.Contain("function sort_table"));
            Assert.That(html, Does.Contain("onclick=\"sort_table(this,sort_cmp_string,1,0);return false;\""));
            Assert.That(html, Does.Contain("onclick=\"sort_table(this,sort_cmp_number,1,0);return false;\""));
            Assert.That(html, Does.Not.Contain("rdl-usersort"));
        }

        [Test]
        public async Task A_user_sort_replaces_the_custom_client_side_sort()
        {
            var report = await Load(Report(userSort: true, custom: CustomSort));
            string html = await Html(report);
            Assert.That(html, Does.Not.Contain("sort_table"));
            Assert.That(html, Does.Not.Contain("sortarrow"));
            Assert.That(SortLink(html, "Name").Groups["dir"].Value, Is.EqualTo("none"));
            Assert.That(SortLink(html, "Amount").Groups["dir"].Value, Is.EqualTo("none"));
        }

        [Test]
        public async Task The_query_helper_sorts_the_rendered_rows_by_a_number()
        {
            var report = await Load(Report(userSort: true));
            report.UserSort = UserSortState.FromQuery(null, "SortAmount", "Descending");
            Assert.That(Lines(await Html(report)), Is.EqualTo(new[] { "D c3", "D b2", "D a1" }));

            report.UserSort = UserSortState.FromQuery("DSortAmount", "SortAmount", "Ascending");
            Assert.That(Lines(await Html(report)), Is.EqualTo(new[] { "D a1", "D b2", "D c3" }));
        }
    }
}
#endif
