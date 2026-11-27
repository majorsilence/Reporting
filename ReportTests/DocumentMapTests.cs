#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// The document map (issue #637): the Label / DocumentMapLabel of report items and group
    /// instances, nested by the labelled containers and groups they are in, with the page and
    /// place of the item each points at; and the PDF outline written from it.
    /// </summary>
    [TestFixture]
    public class DocumentMapTests
    {
        private const string Ns2005 = "http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition";

        private static string Report(string body, string dataSets = "", string pageHeader = "", string ns = Ns2005,
            string pageHeight = "800pt") => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""{ns}"">
  <Width>400pt</Width>
  {dataSets}
  {pageHeader}
  <Body><Height>300pt</Height><ReportItems>{body}</ReportItems></Body>
  <PageHeight>{pageHeight}</PageHeight><PageWidth>500pt</PageWidth>
</Report>";

        private const string DataSets = @"
  <DataSources><DataSource Name=""D""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>D</DataSourceName><CommandText>none</CommandText></Query>
    <Fields>
      <Field Name=""K""><DataField>K</DataField></Field>
      <Field Name=""V""><DataField>V</DataField></Field>
    </Fields>
  </DataSet></DataSets>";

        private static string Text(string name, string value, float top, string extra = "") =>
            $@"<Textbox Name=""{name}""><Top>{top}pt</Top><Left>0pt</Left><Width>200pt</Width><Height>14pt</Height>
                 <Value>{value}</Value>{extra}</Textbox>";

        private static string Table(string tableExtra = "", string groupExtra = "", string detailLabel = "",
            string groupHeaderExtra = "") => $@"
    <Table Name=""T"">
      <DataSetName>Data</DataSetName>{tableExtra}
      <TableColumns><TableColumn><Width>300pt</Width></TableColumn></TableColumns>
      <TableGroups><TableGroup>
        <Grouping Name=""G""><GroupExpressions><GroupExpression>=Fields!K.Value</GroupExpression></GroupExpressions>{groupExtra}</Grouping>
        <Header><TableRows><TableRow><Height>14pt</Height>{groupHeaderExtra}<TableCells><TableCell><ReportItems>
          <Textbox Name=""Head""><Value>=""H "" &amp; Fields!K.Value</Value></Textbox>
        </ReportItems></TableCell></TableCells></TableRow></TableRows></Header>
      </TableGroup></TableGroups>
      <Details><TableRows><TableRow><Height>14pt</Height><TableCells><TableCell><ReportItems>
        <Textbox Name=""Line""><Value>=Fields!V.Value</Value>{detailLabel}</Textbox>
      </ReportItems></TableCell></TableCells></TableRow></TableRows></Details>
    </Table>";

        private static async Task<Report> Run(string rdl, params (string K, string V)[] rows)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4),
                "the test report must parse: " + string.Join("; ", report.ErrorItems?.Cast<object>() ?? Enumerable.Empty<object>()));
            if (rows.Length > 0)
            {
                var table = new DataTable();
                table.Columns.Add("K", typeof(string));
                table.Columns.Add("V", typeof(string));
                foreach (var (k, v) in rows)
                    table.Rows.Add(k, v);
                await report.DataSets["Data"].SetData(table);
            }
            await report.RunGetData(null);
            return report;
        }

        private static async Task<DocumentMapEntry[]> Map(string rdl, params (string K, string V)[] rows)
        {
            using var report = await Run(rdl, rows);
            var pages = await report.BuildPages();
            return pages.GetDocumentMap().ToArray();
        }

        private static string[] Outline(DocumentMapEntry[] map) =>
            map.Select(e => new string(' ', e.Level * 2) + e.Label).ToArray();

        private static readonly (string, string)[] Rows = { ("a", "1"), ("a", "2"), ("b", "3") };

        [Test]
        public async Task AReportWithoutLabels_HasAnEmptyDocumentMap()
        {
            var map = await Map(Report(Text("A", "x", 0)));
            Assert.That(map, Is.Empty);
        }

        [Test]
        public async Task LabelledTextboxes_AreEntriesInLayoutOrder_WithTheirPlace()
        {
            // Written second in the report but laid out first, being higher on the page.
            var map = await Map(Report(
                Text("B", "second", 100, "<Label>Second</Label>")
                + Text("A", "first", 20, "<Label>=\"Fir\" &amp; \"st\"</Label>")
                + Text("C", "none", 200)));

            Assert.That(map.Select(e => e.Label), Is.EqualTo(new[] { "First", "Second" }));
            Assert.That(map.All(e => e.Level == 0 && e.PageIndex == 0));
            Assert.That(map[0].Y, Is.LessThan(map[1].Y));
        }

        [Test]
        public async Task GroupLabels_NestBeneathTheTable_AndDetailLabelsBeneathTheirGroup()
        {
            var map = await Map(Report(
                Table("<Label>Table</Label>", "<Label>=\"Group \" &amp; Fields!K.Value</Label>",
                    "<Label>=\"Row \" &amp; Fields!V.Value</Label>"), DataSets), Rows);

            Assert.That(Outline(map), Is.EqualTo(new[]
            {
                "Table",
                "  Group a",
                "    Row 1",
                "    Row 2",
                "  Group b",
                "    Row 3",
            }));
        }

        [Test]
        public async Task AGroupLabel_PointsAtTheGroupsFirstRow()
        {
            using var report = await Run(Report(Table(groupExtra: "<Label>=Fields!K.Value</Label>"), DataSets), Rows);
            var pages = await report.BuildPages();
            var map = pages.GetDocumentMap();
            var headB = pages[0].Cast<PageItem>().OfType<PageText>().Single(t => t.Text == "H b");

            Assert.That(map.Select(e => e.Label), Is.EqualTo(new[] { "a", "b" }));
            Assert.That(map[1].Y, Is.EqualTo(headB.Y).Within(0.01f));
        }

        [Test]
        public async Task ItemsInALabelledRectangle_NestBeneathIt()
        {
            var map = await Map(Report($@"
    <Rectangle Name=""R""><Top>0pt</Top><Left>0pt</Left><Width>300pt</Width><Height>100pt</Height>
      <Label>Box</Label>
      <ReportItems>{Text("In", "inside", 10, "<Label>Inside</Label>")}</ReportItems>
    </Rectangle>{Text("Out", "outside", 150, "<Label>Outside</Label>")}"));

            Assert.That(Outline(map), Is.EqualTo(new[] { "Box", "  Inside", "Outside" }));
        }

        [Test]
        public async Task AHiddenLabelledItem_IsNotInTheMap_NorGivesItsLabelToTheNext()
        {
            var map = await Map(Report($@"
    <Rectangle Name=""R""><Top>0pt</Top><Left>0pt</Left><Width>300pt</Width><Height>100pt</Height>
      <Label>Hidden box</Label><Visibility><Hidden>true</Hidden></Visibility>
      <ReportItems>{Text("In", "inside", 10, "<Label>Inside</Label>")}</ReportItems>
    </Rectangle>{Text("Out", "outside", 150, "<Label>Outside</Label>")}
    {Text("H", "hidden", 200, "<Label>Hidden text</Label><Visibility><Hidden>true</Hidden></Visibility>")}"));

            Assert.That(Outline(map), Is.EqualTo(new[] { "Outside" }));
        }

        [Test]
        public async Task ATableWithNoRows_GivesItsLabelToNothingElse()
        {
            var map = await Map(Report(
                Table("<Label>Table</Label>") + Text("After", "after", 200, "<Label>After</Label>"), DataSets),
                Array.Empty<(string, string)>());

            Assert.That(Outline(map), Is.EqualTo(new[] { "After" }));
        }

        [Test]
        public async Task AnEmptyLabel_IsNoEntry_AndNestsNothing()
        {
            var map = await Map(Report(
                Table("<Label>=\"\"</Label>", "<Label>=Fields!K.Value</Label>"), DataSets), Rows);

            Assert.That(Outline(map), Is.EqualTo(new[] { "a", "b" }));
        }

        [Test]
        public async Task PageHeaderLabels_AreNotInTheMap()
        {
            string header = $@"<PageHeader><Height>20pt</Height><PrintOnFirstPage>true</PrintOnFirstPage>
              <ReportItems>{Text("Hd", "header", 0, "<Label>Header</Label>")}</ReportItems></PageHeader>";
            var map = await Map(Report(Text("A", "body", 0, "<Label>Body</Label>"), pageHeader: header));

            Assert.That(map.Select(e => e.Label), Is.EqualTo(new[] { "Body" }));
        }

        [Test]
        public async Task AGroupOnALaterPage_PointsAtThatPage()
        {
            // Each group breaks onto its own page.
            var map = await Map(Report(
                Table(groupExtra: "<Label>=Fields!K.Value</Label><PageBreakAtEnd>true</PageBreakAtEnd>"), DataSets), Rows);

            Assert.That(map.Select(e => (e.Label, e.PageIndex)), Is.EqualTo(new[] { ("a", 0), ("b", 1) }));
        }

        [Test]
        public async Task AGroupKeptTogether_MovedToTheNextPage_HasItsLabelOnce_OnThatPage()
        {
            // The second group's 13 rows of 14pt do not fit below the first on a 300pt page, so it,
            // kept together, is taken off the first page and run again at the top of the next.
            var rows = new[] { ("a", "1"), ("a", "2") }
                .Concat(Enumerable.Range(0, 12).Select(i => ("b", i.ToString()))).ToArray();
            string table = Table(groupExtra: "<Label>=Fields!K.Value</Label>")
                .Replace("<TableGroup>", "<TableGroup><KeepTogether>true</KeepTogether>");
            string rdl = Report(Text("Top", "top", 0) + table.Replace("<Table Name=\"T\">", "<Table Name=\"T\"><Top>200pt</Top>"),
                DataSets, pageHeight: "300pt");
            var map = await Map(rdl, rows);

            Assert.That(map.Select(e => e.Label), Is.EqualTo(new[] { "a", "b" }));
            Assert.That(map[1].PageIndex, Is.EqualTo(1), "the group moved to the next page");
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
            var map = await Map(Report(list, DataSets), Rows);

            Assert.That(Outline(map), Is.EqualTo(new[] { "List", "  Item a", "  Item b" }));
        }

        [Test]
        public async Task Rdl2008DocumentMapLabel_IsAnEntry()
        {
            const string ns2008 = "http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition";
            var map = await Map(Report(Text("A", "x", 0, "<DocumentMapLabel>From 2008</DocumentMapLabel>"), ns: ns2008));

            Assert.That(map.Select(e => e.Label), Is.EqualTo(new[] { "From 2008" }));
        }

        [Test]
        public async Task ThePdf_HasAnOutline_NestedAsTheDocumentMap()
        {
            using var report = await Run(Report(
                Table("<Label>Table</Label>", "<Label>=\"Group \" &amp; Fields!K.Value</Label>"), DataSets), Rows);
            string pdf = await RenderPdf(report);

            Assert.That(pdf, Does.Contain("/Type /Outlines"));
            Assert.That(pdf, Does.Contain("/PageMode /UseOutlines"));
            foreach (string title in new[] { "Table", "Group a", "Group b" })
                Assert.That(pdf, Does.Contain("/Title " + PdfTitle(title)), title);
            // Table holds the two groups beneath it.
            Assert.That(pdf, Does.Contain("/Type /Outlines").And.Contain("/Count 3"));
        }

        [Test]
        public async Task ThePdf_WithoutLabels_HasNoOutline()
        {
            using var report = await Run(Report(Text("A", "x", 0)));
            string pdf = await RenderPdf(report);

            Assert.That(pdf, Does.Not.Contain("/Outlines"));
        }

        // ── RDL 2008 Tablix ────────────────────────────────────────────────────

        private const string Ns2008 = "http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition";

        private static string Tb2008(string name, string value, string extra = "")
            => $@"<Textbox Name=""{name}"">{extra}<Paragraphs><Paragraph><TextRuns><TextRun><Value>{value}</Value></TextRun></TextRuns></Paragraph></Paragraphs></Textbox>";

        private static string Row2008(string name, string value, string extra = "")
            => $@"<TablixRow><Height>14pt</Height><TablixCells><TablixCell><CellContents>{Tb2008(name, value, extra)}</CellContents></TablixCell></TablixCells></TablixRow>";

        // A Tablix with its own label, a row group labelled by its key, and labelled detail rows.
        private static string Tablix2008(string groupLabel = "<DocumentMapLabel>=\"Group \" &amp; Fields!K.Value</DocumentMapLabel>") => $@"
    <Tablix Name=""T"">
      <DocumentMapLabel>Tablix</DocumentMapLabel>
      <TablixBody><TablixColumns><TablixColumn><Width>300pt</Width></TablixColumn></TablixColumns>
        <TablixRows>{Row2008("Head", "Head")}{Row2008("Line", "=Fields!V.Value", "<DocumentMapLabel>=\"Row \" &amp; Fields!V.Value</DocumentMapLabel>")}</TablixRows>
      </TablixBody>
      <TablixColumnHierarchy><TablixMembers><TablixMember /></TablixMembers></TablixColumnHierarchy>
      <TablixRowHierarchy><TablixMembers>
        <TablixMember><KeepWithGroup>After</KeepWithGroup></TablixMember>
        <TablixMember>
          <Group Name=""G""><GroupExpressions><GroupExpression>=Fields!K.Value</GroupExpression></GroupExpressions>{groupLabel}</Group>
          <TablixMembers><TablixMember><Group Name=""Details"" /></TablixMember></TablixMembers>
        </TablixMember>
      </TablixMembers></TablixRowHierarchy>
      <DataSetName>Data</DataSetName>
      <Top>0pt</Top><Left>0pt</Left><Height>28pt</Height><Width>300pt</Width>
    </Tablix>";

        private static async Task<DocumentMapEntry[]> Map2008(string rdl, bool native, params (string K, string V)[] rows)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true, NativeTablix = native }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4),
                "the test report must parse: " + string.Join("; ", report.ErrorItems?.Cast<object>() ?? Enumerable.Empty<object>()));
            var table = new DataTable();
            table.Columns.Add("K", typeof(string));
            table.Columns.Add("V", typeof(string));
            foreach (var (k, v) in rows)
                table.Rows.Add(k, v);
            await report.DataSets["Data"].SetData(table);
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            return pages.GetDocumentMap().ToArray();
        }

        [TestCase(true, TestName = "Rdl2008TablixLabels_NestAsATable_Native")]
        [TestCase(false, TestName = "Rdl2008TablixLabels_NestAsATable_Normalised")]
        public async Task Rdl2008TablixLabels_NestAsATable(bool native)
        {
            var map = await Map2008(Report(Tablix2008(), DataSets, ns: Ns2008), native, Rows);

            Assert.That(Outline(map), Is.EqualTo(new[]
            {
                "Tablix",
                "  Group a",
                "    Row 1",
                "    Row 2",
                "  Group b",
                "    Row 3",
            }));
        }

        // A 2005-style Table written in a 2008+ file: the exporter writes its Labels as
        // DocumentMapLabel, and the Tablix it makes has the same document map.
        [TestCase(true, TestName = "ExportedTablix_KeepsItsDocumentMap_Native")]
        [TestCase(false, TestName = "ExportedTablix_KeepsItsDocumentMap_Normalised")]
        public async Task ExportedTablix_KeepsItsDocumentMap(bool native)
        {
            const string ns2010 = "http://schemas.microsoft.com/sqlserver/reporting/2010/01/reportdefinition";
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml(Report(Table("<Label>Table</Label>", "<Label>=\"Group \" &amp; Fields!K.Value</Label>",
                "<Label>=\"Row \" &amp; Fields!V.Value</Label>"), DataSets, ns: ns2010));

            Rdl2008Exporter.ConvertToTablix(doc);

            var nsm = new System.Xml.XmlNamespaceManager(doc.NameTable);
            nsm.AddNamespace("r", ns2010);
            Assert.That(doc.SelectNodes("//r:Label", nsm)!.Count, Is.EqualTo(0), "no 2005 Label is left");
            Assert.That(doc.SelectSingleNode("//r:Tablix/r:DocumentMapLabel", nsm)?.InnerText, Is.EqualTo("Table"));
            Assert.That(doc.SelectSingleNode("//r:Group[@Name='G']/r:DocumentMapLabel", nsm), Is.Not.Null);
            Assert.That(doc.SelectSingleNode("//r:Textbox[@Name='Line']/r:DocumentMapLabel", nsm), Is.Not.Null);

            var map = await Map2008(doc.OuterXml, native, Rows);
            Assert.That(Outline(map), Is.EqualTo(new[]
            {
                "Table",
                "  Group a",
                "    Row 1",
                "    Row 2",
                "  Group b",
                "    Row 3",
            }));
        }

        // ── RDL 2005 Matrix ───────────────────────────────────────────────────

        private static string Matrix(string matrixLabel = "<Label>Matrix</Label>", string columnLabel = "") => $@"
    <Matrix Name=""M""><DataSetName>Data</DataSetName><Top>0pt</Top><Left>0pt</Left>{matrixLabel}
      <Corner><ReportItems><Textbox Name=""Corner""><Value>Corner</Value></Textbox></ReportItems></Corner>
      <ColumnGroupings><ColumnGrouping><Height>14pt</Height><DynamicColumns>
        <Grouping Name=""Cols""><GroupExpressions><GroupExpression>=Fields!C.Value</GroupExpression></GroupExpressions>{columnLabel}</Grouping>
        <ReportItems><Textbox Name=""ColHead""><Value>=Fields!C.Value</Value></Textbox></ReportItems>
      </DynamicColumns></ColumnGrouping></ColumnGroupings>
      <RowGroupings>
        <RowGrouping><Width>60pt</Width><DynamicRows>
          <Grouping Name=""Outer""><GroupExpressions><GroupExpression>=Fields!K.Value</GroupExpression></GroupExpressions>
            <Label>=""Outer "" &amp; Fields!K.Value</Label></Grouping>
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

        private const string MatrixDataSets = @"
  <DataSources><DataSource Name=""D""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>D</DataSourceName><CommandText>none</CommandText></Query>
    <Fields>
      <Field Name=""K""><DataField>K</DataField></Field>
      <Field Name=""V""><DataField>V</DataField></Field>
      <Field Name=""C""><DataField>C</DataField></Field>
    </Fields>
  </DataSet></DataSets>";

        private static async Task<DocumentMapEntry[]> MatrixMap(string rdl)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4),
                "the test report must parse: " + string.Join("; ", report.ErrorItems?.Cast<object>() ?? Enumerable.Empty<object>()));
            var table = new DataTable();
            table.Columns.Add("K", typeof(string));
            table.Columns.Add("V", typeof(string));
            table.Columns.Add("C", typeof(string));
            foreach (var (k, v, c) in new[] { ("a", "1", "x"), ("a", "2", "y"), ("a", "2", "x"), ("b", "3", "y") })
                table.Rows.Add(k, v, c);
            await report.DataSets["Data"].SetData(table);
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            return pages.GetDocumentMap().ToArray();
        }

        [Test]
        public async Task MatrixRowGroupLabels_NestOuterAboveInner_BeneathTheMatrix()
        {
            var map = await MatrixMap(Report(Matrix(), MatrixDataSets));

            Assert.That(Outline(map), Is.EqualTo(new[]
            {
                "Matrix",
                "  Outer a",
                "    Inner 1",
                "    Inner 2",
                "  Outer b",
                "    Inner 3",
            }));
        }

        [Test]
        public async Task MatrixRowGroupLabels_PointAtTheirRowHeaders()
        {
            RdlEngineConfig.RdlEngineConfigInit();
            var map = await MatrixMap(Report(Matrix(matrixLabel: ""), MatrixDataSets));
            Assert.That(map.Select(e => e.Label), Is.EqualTo(new[] { "Outer a", "Inner 1", "Inner 2", "Outer b", "Inner 3" }));
            Assert.That(map[0].Y, Is.EqualTo(map[1].Y).Within(0.01f), "an outer group starts on its first inner group's row");
            Assert.That(map[2].Y, Is.GreaterThan(map[1].Y));
            Assert.That(map[3].Y, Is.GreaterThan(map[2].Y));
        }

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

        [TestCase(true, TestName = "Rdl2008PivotRowGroupLabels_Nest_Native")]
        [TestCase(false, TestName = "Rdl2008PivotRowGroupLabels_Nest_Normalised")]
        public async Task Rdl2008PivotRowGroupLabels_Nest(bool native)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(Report(Pivot2008(), MatrixDataSets, ns: Ns2008))
                { SkipDatabaseSchemaValidation = true, NativeTablix = native }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4),
                "the test report must parse: " + string.Join("; ", report.ErrorItems?.Cast<object>() ?? Enumerable.Empty<object>()));
            if (native)
                Assert.That(report.ReportDefinition.Body.ReportItems.Items.Single(ri => ri.Name?.Nm == "P"),
                    Is.InstanceOf<TablixPivot>(), "the engine runs the Tablix as a pivot");
            var table = new DataTable();
            table.Columns.Add("K", typeof(string));
            table.Columns.Add("V", typeof(string));
            table.Columns.Add("C", typeof(string));
            foreach (var (k, v, c) in new[] { ("a", "1", "x"), ("a", "2", "y"), ("a", "2", "x"), ("b", "3", "y") })
                table.Rows.Add(k, v, c);
            await report.DataSets["Data"].SetData(table);
            await report.RunGetData(null);
            var pages = await report.BuildPages();

            Assert.That(Outline(pages.GetDocumentMap().ToArray()), Is.EqualTo(new[]
            {
                "Pivot",
                "  Outer a",
                "    Inner 1",
                "    Inner 2",
                "  Outer b",
                "    Inner 3",
            }));
        }

        private static async Task<string> RenderPdf(Report report)
        {
            string file = Path.Combine(Path.GetTempPath(), $"DocumentMap_{Guid.NewGuid():N}.pdf");
            try
            {
                using (var sg = new OneFileStreamGen(file, true))
                    await report.RunRender(sg, OutputPresentationType.PDF);
                return Encoding.Latin1.GetString(File.ReadAllBytes(file));
            }
            finally
            {
                File.Delete(file);
            }
        }

        // How Majorsilence.Pdf writes an outline title: UTF-16BE with a byte order mark, in hex.
        private static string PdfTitle(string title)
        {
            var hex = new StringBuilder("<FEFF");
            foreach (char c in title)
                hex.Append(((int)c).ToString("X4"));
            return hex.Append('>').ToString();
        }
    }
}
#endif
