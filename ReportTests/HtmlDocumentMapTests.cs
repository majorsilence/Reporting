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

        // ── Matrix row groups ─────────────────────────────────────────────────

        private const string Ns2008 = "http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition";

        private static string MatrixReport(string body, string ns = "http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition") => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""{ns}"">
  <Width>400pt</Width>
  <DataSources><DataSource Name=""D""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>D</DataSourceName><CommandText>none</CommandText></Query>
    <Fields>
      <Field Name=""K""><DataField>K</DataField></Field>
      <Field Name=""V""><DataField>V</DataField></Field>
      <Field Name=""C""><DataField>C</DataField></Field>
    </Fields>
  </DataSet></DataSets>
  <Body><Height>300pt</Height><ReportItems>{body}</ReportItems></Body>
  <PageHeight>800pt</PageHeight><PageWidth>500pt</PageWidth>
</Report>";

        private static string Matrix(string matrixLabel = "<Label>Matrix</Label>", string outerLabel = "<Label>=\"Outer \" &amp; Fields!K.Value</Label>") => $@"
    <Matrix Name=""M""><DataSetName>Data</DataSetName><Top>0pt</Top><Left>0pt</Left>{matrixLabel}
      <Corner><ReportItems><Textbox Name=""Corner""><Value>Corner</Value></Textbox></ReportItems></Corner>
      <ColumnGroupings><ColumnGrouping><Height>14pt</Height><DynamicColumns>
        <Grouping Name=""Cols""><GroupExpressions><GroupExpression>=Fields!C.Value</GroupExpression></GroupExpressions>
          <Label>=""Column "" &amp; Fields!C.Value</Label></Grouping>
        <ReportItems><Textbox Name=""ColHead""><Value>=Fields!C.Value</Value></Textbox></ReportItems>
      </DynamicColumns></ColumnGrouping></ColumnGroupings>
      <RowGroupings>
        <RowGrouping><Width>60pt</Width><DynamicRows>
          <Grouping Name=""Outer""><GroupExpressions><GroupExpression>=Fields!K.Value</GroupExpression></GroupExpressions>{outerLabel}</Grouping>
          <ReportItems><Textbox Name=""OuterHead""><Value>=Fields!K.Value</Value></Textbox></ReportItems>
        </DynamicRows></RowGrouping>
        <RowGrouping><Width>60pt</Width><DynamicRows>
          <Grouping Name=""Inner""><GroupExpressions><GroupExpression>=Fields!V.Value</GroupExpression></GroupExpressions>
            <Label>=""Inner "" &amp; Fields!V.Value</Label></Grouping>
          <ReportItems><Textbox Name=""InnerHead""><Value>=Fields!V.Value</Value></Textbox></ReportItems>
        </DynamicRows></RowGrouping>
      </RowGroupings>
      <MatrixRows><MatrixRow><Height>14pt</Height><MatrixCells><MatrixCell><ReportItems>
        <Textbox Name=""Cell""><Value>=Count(Fields!V.Value)</Value></Textbox></ReportItems></MatrixCell></MatrixCells></MatrixRow></MatrixRows>
      <MatrixColumns><MatrixColumn><Width>40pt</Width></MatrixColumn></MatrixColumns>
    </Matrix>";

        private static string Tb2008(string name, string value)
            => $@"<Textbox Name=""{name}""><Paragraphs><Paragraph><TextRuns><TextRun><Value>{value}</Value></TextRun></TextRuns></Paragraph></Paragraphs></Textbox>";

        // A 2008 Tablix with a column group, which the engine runs as a pivot (a matrix).
        private static string Pivot2008() => $@"
    <Tablix Name=""P"">
      <DocumentMapLabel>Pivot</DocumentMapLabel>
      <TablixCorner><TablixCornerRows><TablixCornerRow>
        <TablixCornerCell><CellContents>{Tb2008("C1", "")}</CellContents></TablixCornerCell>
        <TablixCornerCell><CellContents>{Tb2008("C2", "")}</CellContents></TablixCornerCell>
      </TablixCornerRow></TablixCornerRows></TablixCorner>
      <TablixBody><TablixColumns><TablixColumn><Width>40pt</Width></TablixColumn></TablixColumns>
        <TablixRows><TablixRow><Height>14pt</Height><TablixCells><TablixCell><CellContents>{Tb2008("Cell", "=Count(Fields!V.Value)")}</CellContents></TablixCell></TablixCells></TablixRow></TablixRows>
      </TablixBody>
      <TablixColumnHierarchy><TablixMembers><TablixMember>
        <Group Name=""Cols""><GroupExpressions><GroupExpression>=Fields!C.Value</GroupExpression></GroupExpressions></Group>
        <TablixHeader><Size>14pt</Size><CellContents>{Tb2008("ColHead", "=Fields!C.Value")}</CellContents></TablixHeader>
      </TablixMember></TablixMembers></TablixColumnHierarchy>
      <TablixRowHierarchy><TablixMembers><TablixMember>
        <Group Name=""Outer""><GroupExpressions><GroupExpression>=Fields!K.Value</GroupExpression></GroupExpressions>
          <DocumentMapLabel>=""Outer "" &amp; Fields!K.Value</DocumentMapLabel></Group>
        <TablixHeader><Size>60pt</Size><CellContents>{Tb2008("OuterHead", "=Fields!K.Value")}</CellContents></TablixHeader>
        <TablixMembers><TablixMember>
          <Group Name=""Inner""><GroupExpressions><GroupExpression>=Fields!V.Value</GroupExpression></GroupExpressions>
            <DocumentMapLabel>=""Inner "" &amp; Fields!V.Value</DocumentMapLabel></Group>
          <TablixHeader><Size>60pt</Size><CellContents>{Tb2008("InnerHead", "=Fields!V.Value")}</CellContents></TablixHeader>
        </TablixMember></TablixMembers>
      </TablixMember></TablixMembers></TablixRowHierarchy>
      <DataSetName>Data</DataSetName>
      <Top>0pt</Top><Left>0pt</Left><Height>28pt</Height><Width>160pt</Width>
    </Tablix>";

        private static async Task<string> MatrixHtml(string rdl, bool nativeTablix = true)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true, NativeTablix = nativeTablix }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4), "the test report must parse");
            var table = new DataTable();
            table.Columns.Add("K", typeof(string));
            table.Columns.Add("V", typeof(string));
            table.Columns.Add("C", typeof(string));
            foreach (var (k, v, c) in new[] { ("a", "1", "x"), ("a", "2", "y"), ("a", "2", "x"), ("b", "3", "y") })
                table.Rows.Add(k, v, c);
            await report.DataSets["Data"].SetData(table);
            await report.RunGetData(null);
            report.HtmlDocumentMap = true;
            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.HTML);
            return ms.GetText();
        }

        private static void AssertEveryLinkHasOneAnchor(string html)
        {
            foreach (string id in Regex.Matches(Nav(html), "href='#([^']*)'").Select(m => m.Groups[1].Value).Distinct())
                Assert.That(Regex.Matches(html, $"id='{Regex.Escape(id)}'").Count, Is.EqualTo(1), id);
        }

        [Test]
        public async Task MatrixRowGroupLabels_NestOuterAboveInner_BeneathTheMatrix()
        {
            string html = await MatrixHtml(MatrixReport(Matrix()));

            // Column groups are not listed: an instance of one runs down every row.
            Assert.That(Outline(Nav(html)), Is.EqualTo(new[]
            {
                "Matrix",
                "  Outer a",
                "    Inner 1",
                "    Inner 2",
                "  Outer b",
                "    Inner 3",
            }));
            AssertEveryLinkHasOneAnchor(html);
        }

        [Test]
        public async Task AMatrixRowGroupLabel_IsAnchoredInItsRowHeader()
        {
            string html = await MatrixHtml(MatrixReport(Matrix(matrixLabel: "")));
            var ids = Regex.Matches(Nav(html), "href='#([^']*)'").Select(m => m.Groups[1].Value).ToArray();

            // Outer b and Inner 3 start on the same row: each is anchored in its own header cell
            // of that row, ahead of the header's text.
            int outer = html.IndexOf($"id='{ids[3]}'", System.StringComparison.Ordinal);
            int inner = html.IndexOf($"id='{ids[4]}'", System.StringComparison.Ordinal);
            int rowStart = html.LastIndexOf("<tr", outer, System.StringComparison.Ordinal);
            Assert.That(inner, Is.GreaterThan(outer));
            Assert.That(html.Substring(rowStart, inner - rowStart), Does.Not.Contain("</tr>"), "both in one row");
            Assert.That(html.IndexOf(">b<", outer, System.StringComparison.Ordinal), Is.InRange(outer, inner), "the outer header's text follows its anchor");
            AssertEveryLinkHasOneAnchor(html);
        }

        [Test]
        public async Task AnUnlabelledOuterRowGroup_AddsNoLevel()
        {
            string html = await MatrixHtml(MatrixReport(Matrix(outerLabel: "")));

            Assert.That(Outline(Nav(html)), Is.EqualTo(new[] { "Matrix", "  Inner 1", "  Inner 2", "  Inner 3" }));
        }

        // A Tablix run as a pivot lists its own label; its row headers do not say which group they
        // belong to, so its row groups wait on the engine's pivot row group starts (#689).
        [Test]
        public async Task APivotTablix_ListsItsOwnLabel_AndAnchorsResolve()
        {
            string html = await MatrixHtml(MatrixReport(Pivot2008(), Ns2008));

            Assert.That(Outline(Nav(html)), Is.EqualTo(new[] { "Pivot" }));
            AssertEveryLinkHasOneAnchor(html);
        }
    }
}
#endif
