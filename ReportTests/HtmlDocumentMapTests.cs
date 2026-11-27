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
    /// The HTML table of contents (Report.HtmlDocumentMap, issue #637): a nested list of links,
    /// built from the Label / DocumentMapLabel of report items and group instances, to anchors
    /// in the page.
    /// </summary>
    [TestFixture]
    public class HtmlDocumentMapTests
    {
        private static string Report(string body, string pageHeader = "") => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>400pt</Width>
  <DataSources><DataSource Name=""D""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>D</DataSourceName><CommandText>none</CommandText></Query>
    <Fields>
      <Field Name=""K""><DataField>K</DataField></Field>
      <Field Name=""V""><DataField>V</DataField></Field>
    </Fields>
  </DataSet></DataSets>
  {pageHeader}
  <Body><Height>300pt</Height><ReportItems>{body}</ReportItems></Body>
  <PageHeight>800pt</PageHeight><PageWidth>500pt</PageWidth>
</Report>";

        private static string Text(string name, string value, float top, string extra = "") =>
            $@"<Textbox Name=""{name}""><Top>{top}pt</Top><Left>0pt</Left><Width>200pt</Width><Height>14pt</Height>
                 <Value>{value}</Value>{extra}</Textbox>";

        private static string Table(string tableExtra = "", string groupExtra = "", string detailLabel = "") => $@"
    <Table Name=""T""><Top>50pt</Top>
      <DataSetName>Data</DataSetName>{tableExtra}
      <TableColumns><TableColumn><Width>300pt</Width></TableColumn></TableColumns>
      <TableGroups><TableGroup>
        <Grouping Name=""G""><GroupExpressions><GroupExpression>=Fields!K.Value</GroupExpression></GroupExpressions>{groupExtra}</Grouping>
        <Header><TableRows><TableRow><Height>14pt</Height><TableCells><TableCell><ReportItems>
          <Textbox Name=""Head""><Value>=""H "" &amp; Fields!K.Value</Value></Textbox>
        </ReportItems></TableCell></TableCells></TableRow></TableRows></Header>
      </TableGroup></TableGroups>
      <Details><TableRows><TableRow><Height>14pt</Height><TableCells><TableCell><ReportItems>
        <Textbox Name=""Line""><Value>=Fields!V.Value</Value>{detailLabel}</Textbox>
      </ReportItems></TableCell></TableCells></TableRow></TableRows></Details>
    </Table>";

        private static async Task<string> Html(string rdl, bool documentMap = true)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4), "the test report must parse");
            var table = new DataTable();
            table.Columns.Add("K", typeof(string));
            table.Columns.Add("V", typeof(string));
            foreach (var (k, v) in new[] { ("a", "1"), ("a", "2"), ("b", "3") })
                table.Rows.Add(k, v);
            await report.DataSets["Data"].SetData(table);
            await report.RunGetData(null);
            report.HtmlDocumentMap = documentMap;
            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.HTML);
            return ms.GetText();
        }

        private static string Nav(string html)
        {
            var m = Regex.Match(html, "<nav class='documentmap'[^>]*>(.*?)</nav>", RegexOptions.Singleline);
            return m.Success ? m.Groups[1].Value.Trim() : null;
        }

        // The entries as an indented outline, from the nested lists.
        private static string[] Outline(string nav)
        {
            var lines = new System.Collections.Generic.List<string>();
            int depth = 0;
            foreach (Match m in Regex.Matches(nav, "<ul>|</ul>|<a href='#[^']*'>([^<]*)</a>"))
            {
                if (m.Value == "<ul>") depth++;
                else if (m.Value == "</ul>") depth--;
                else lines.Add(new string(' ', (depth - 1) * 2) + m.Groups[1].Value);
            }
            return lines.ToArray();
        }

        [Test]
        public async Task ByDefault_ThereIsNoTableOfContents()
        {
            string html = await Html(Report(Table("<Label>Table</Label>", "<Label>=Fields!K.Value</Label>")), documentMap: false);

            Assert.That(html, Does.Not.Contain("documentmap"));
            Assert.That(html, Does.Not.Contain("id='dm"));
        }

        [Test]
        public async Task AReportWithoutLabels_HasNoTableOfContents()
        {
            string html = await Html(Report(Table()));

            Assert.That(Nav(html), Is.Null);
        }

        [Test]
        public async Task GroupLabels_AreNestedLinks_ToAnchorsInThePage()
        {
            string html = await Html(Report(
                Text("Title", "Title", 0, "<Label>Title</Label>")
                + Table("<Label>Table</Label>", "<Label>=\"Group \" &amp; Fields!K.Value</Label>",
                    "<Label>=\"Row \" &amp; Fields!V.Value</Label>")));
            string nav = Nav(html);

            Assert.That(nav, Is.Not.Null);
            Assert.That(Outline(nav), Is.EqualTo(new[]
            {
                "Title",
                "Table",
                "  Group a",
                "    Row 1",
                "    Row 2",
                "  Group b",
                "    Row 3",
            }));

            // Every link goes to an anchor that is in the page, once.
            var hrefs = Regex.Matches(nav, "href='#([^']*)'").Select(m => m.Groups[1].Value).ToArray();
            foreach (string id in hrefs.Distinct())
                Assert.That(Regex.Matches(html, $"id='{id}'").Count, Is.EqualTo(1), id);
            // The table and its first group point at the same place, the first thing written.
            Assert.That(hrefs[1], Is.EqualTo(hrefs[2]));
        }

        [Test]
        public async Task TheGroupAnchor_IsInTheGroupsHeaderRow()
        {
            string html = await Html(Report(Table(groupExtra: "<Label>=Fields!K.Value</Label>")));
            string id = Regex.Matches(Nav(html), "href='#([^']*)'")[1].Groups[1].Value;   // group b

            int anchor = html.IndexOf($"id='{id}'");
            Assert.That(anchor, Is.GreaterThan(html.IndexOf("H a")));
            Assert.That(html.IndexOf("H b", anchor), Is.GreaterThan(anchor).And.LessThan(anchor + 40),
                "the anchor is just before the header text of group b");
        }

        [Test]
        public async Task HiddenItems_AreNotInTheTableOfContents()
        {
            string html = await Html(Report(
                Text("Shown", "shown", 0, "<Label>Shown</Label>")
                + Text("Hidden", "hidden", 20, "<Label>Hidden</Label><Visibility><Hidden>true</Hidden></Visibility>")
                + Table("<Label>Hidden table</Label><Visibility><Hidden>true</Hidden></Visibility>",
                    "<Label>=Fields!K.Value</Label>")));

            Assert.That(Outline(Nav(html)), Is.EqualTo(new[] { "Shown" }));
        }

        [Test]
        public async Task PageHeaderLabels_AreNotInTheTableOfContents()
        {
            string header = $@"<PageHeader><Height>20pt</Height><PrintOnFirstPage>true</PrintOnFirstPage>
              <ReportItems>{Text("Hd", "header", 0, "<Label>Header</Label>")}</ReportItems></PageHeader>";
            string html = await Html(Report(Text("Body", "body", 0, "<Label>Body</Label>"), header));

            Assert.That(Outline(Nav(html)), Is.EqualTo(new[] { "Body" }));
        }

        [Test]
        public async Task ListGroupLabels_AreEntries()
        {
            string list = @"
    <List Name=""L""><DataSetName>Data</DataSetName><Top>0pt</Top><Left>0pt</Left><Width>300pt</Width><Height>20pt</Height>
      <Label>List</Label>
      <Grouping Name=""LG""><GroupExpressions><GroupExpression>=Fields!K.Value</GroupExpression></GroupExpressions>
        <Label>=""Item "" &amp; Fields!K.Value</Label></Grouping>
      <ReportItems><Textbox Name=""LT""><Top>0pt</Top><Left>0pt</Left><Width>200pt</Width><Height>14pt</Height>
        <Value>=Fields!K.Value</Value></Textbox></ReportItems>
    </List>";
            string html = await Html(Report(list));

            Assert.That(Outline(Nav(html)), Is.EqualTo(new[] { "List", "  Item a", "  Item b" }));
        }

        [Test]
        public async Task LabelsAreEscaped()
        {
            string html = await Html(Report(Text("A", "x", 0, "<Label>a &lt;b&gt; &amp; c</Label>")));

            Assert.That(Nav(html), Does.Contain("a &lt;b").And.Contain("&amp; c").And.Not.Contain("<b"));
        }
    }
}
#endif
