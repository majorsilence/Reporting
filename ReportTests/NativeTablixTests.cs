#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// The engine running a table-shaped Tablix itself (RDLParser.NativeTablix, #427 phase 2)
    /// lays out every page exactly as the normaliser's rewrite into a 2005 Table does. Each
    /// report is rendered both ways and every page item compared.
    /// </summary>
    [TestFixture]
    public partial class NativeTablixTests
    {
        private const string Ns = "http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition";

        private static string Tb(string name, string value, string extra = "")
            => $@"<Textbox Name=""{name}"">{extra}<Paragraphs><Paragraph><TextRuns><TextRun><Value>{value}</Value></TextRun></TextRuns></Paragraph></Paragraphs></Textbox>";

        private static string Cell(string name, string value, string spans = "")
            => $"<TablixCell><CellContents>{Tb(name, value)}{spans}</CellContents></TablixCell>";

        private static string Row(params string[] cells)
            => $"<TablixRow><Height>0.25in</Height><TablixCells>{string.Concat(cells)}</TablixCells></TablixRow>";

        private static string Columns(int count)
            => "<TablixColumns>" + string.Concat(Enumerable.Repeat("<TablixColumn><Width>1.25in</Width></TablixColumn>", count)) + "</TablixColumns>";

        private static string Tablix(string name, int columns, string rows, string rowMembers, string columnMembers = null, string extra = "")
            => $@"<Tablix Name=""{name}"">{(extra.StartsWith("<TablixCorner>") ? extra : "")}
                <TablixBody>{Columns(columns)}<TablixRows>{rows}</TablixRows></TablixBody>
                <TablixColumnHierarchy><TablixMembers>{columnMembers ?? string.Concat(Enumerable.Repeat("<TablixMember />", columns))}</TablixMembers></TablixColumnHierarchy>
                <TablixRowHierarchy><TablixMembers>{rowMembers}</TablixMembers></TablixRowHierarchy>
                <DataSetName>Data</DataSetName>{(extra.StartsWith("<TablixCorner>") ? "" : extra)}
                <Top>0.25in</Top><Left>0.25in</Left><Height>0.75in</Height><Width>{columns * 1.25}in</Width>
              </Tablix>";

        private static string Report(string items) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""{Ns}"">
  <DataSources><DataSource Name=""Src""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data""><Query><DataSourceName>Src</DataSourceName><CommandText>none</CommandText></Query>
    <Fields>
      <Field Name=""Region""><DataField>Region</DataField></Field>
      <Field Name=""City""><DataField>City</DataField></Field>
      <Field Name=""Name""><DataField>Name</DataField></Field>
      <Field Name=""Amount""><DataField>Amount</DataField></Field>
    </Fields></DataSet></DataSets>
  <Body><Height>1.5in</Height><ReportItems>{items}</ReportItems></Body>
  <Width>7in</Width>
  <Page><PageHeight>4in</PageHeight><PageWidth>8in</PageWidth><TopMargin>0.25in</TopMargin><BottomMargin>0.25in</BottomMargin>
    <PageFooter><Height>0.25in</Height><PrintOnFirstPage>true</PrintOnFirstPage><PrintOnLastPage>true</PrintOnLastPage>
      <ReportItems>{Tb("PageNo", "=Globals!PageNumber &amp; \" of \" &amp; Globals!TotalPages", "<Top>0in</Top><Left>0in</Left><Height>0.25in</Height><Width>2in</Width>")}</ReportItems></PageFooter>
  </Page>
</Report>";

        private static DataTable Data()
        {
            var t = new DataTable();
            t.Columns.Add("Region", typeof(string));
            t.Columns.Add("City", typeof(string));
            t.Columns.Add("Name", typeof(string));
            t.Columns.Add("Amount", typeof(double));
            string[] regions = { "North", "South", "East" };
            for (int i = 0; i < 36; i++)
                t.Rows.Add(regions[i % 3], "City" + (i % 4), "Name" + i.ToString("00"), (i * 7 % 23) + 0.5);
            return t;
        }

        private static async Task<(string[] Items, Report Report)> Render(string rdl, bool native)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true, NativeTablix = native }.Parse();
            string errors = string.Join(" | ", report.ErrorItems?.Cast<object>() ?? Enumerable.Empty<object>());
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4), (native ? "native: " : "normalised: ") + errors);
            await report.DataSets["Data"].SetData(Data());
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            var items = new List<string>();
            foreach (Page page in pages)
                foreach (PageItem pi in page)
                    items.Add(string.Format(CultureInfo.InvariantCulture, "p{0} {1} {2:0.#},{3:0.#} {4:0.#}x{5:0.#} {6}",
                        page.PageNumber, pi.GetType().Name, pi.X, pi.Y, pi.W, pi.H, (pi as PageText)?.Text));
            return (items.ToArray(), report);
        }

        /// <summary>Renders both ways, checks they match, and returns the native report's items.</summary>
        /// <param name="expectedChange">Where the native layout is meant to differ: applied to each normalised item.</param>
        private static async Task<string[]> AssertSameBothWays(string rdl, string tablixName = "T", bool pivot = false,
            System.Func<string, string> expectedChange = null)
        {
            var (normalised, r1) = await Render(rdl, native: false);
            if (expectedChange != null)
                normalised = normalised.Select(expectedChange).ToArray();
            var (native, r2) = await Render(rdl, native: true);
            using (r1)
            using (r2)
            {
                var item = r2.ReportDefinition.Body.ReportItems.Items.Single(ri => ri.Name?.Nm == tablixName);
                Assert.That(item, pivot ? Is.InstanceOf<TablixPivot>() : Is.InstanceOf<Tablix>(), "the engine runs the Tablix itself");
                var rewritten = r1.ReportDefinition.Body.ReportItems.Items.Single(ri => ri.Name?.Nm == tablixName);
                Assert.That(rewritten, Is.Not.InstanceOf<Tablix>().And.Not.InstanceOf<TablixPivot>());
            }
            Assert.That(native.Length, Is.GreaterThan(0));
            Assert.That(native, Is.EqualTo(normalised), () => "first differences (normalised => native):\n" + string.Join("\n",
                Enumerable.Range(0, System.Math.Max(native.Length, normalised.Length))
                    .Where(i => i >= native.Length || i >= normalised.Length || native[i] != normalised[i]).Take(8)
                    .Select(i => (i < normalised.Length ? normalised[i] : "-") + "  =>  " + (i < native.Length ? native[i] : "-"))));
            return native;
        }

        [Test]
        public async Task HeaderDetailsAndFooter_SortedAndRepeatedOverPages()
        {
            string rows = Row(Cell("H1", "Name"), Cell("H2", "Amount"))
                + Row(Cell("D1", "=Fields!Name.Value"), Cell("D2", "=Fields!Amount.Value"))
                + Row(Cell("F1", "Total"), Cell("F2", "=Sum(Fields!Amount.Value)"));
            string members = @"<TablixMember><KeepWithGroup>After</KeepWithGroup><RepeatOnNewPage>true</RepeatOnNewPage></TablixMember>
                <TablixMember><Group Name=""Details"" /></TablixMember>
                <TablixMember><KeepWithGroup>Before</KeepWithGroup></TablixMember>";
            var items = await AssertSameBothWays(Report(Tablix("T", 2, rows, members,
                extra: "<RepeatRowHeaders>true</RepeatRowHeaders><SortExpressions><SortExpression><Value>=Fields!Amount.Value</Value><Direction>Descending</Direction></SortExpression></SortExpressions>")));

            Assert.That(items.Count(i => i.EndsWith(" Name")), Is.GreaterThan(1), "the header repeats on each page");
            Assert.That(items.Select(i => i.Split(' ')[0]).Distinct().Count(), Is.GreaterThan(1), "the table runs over pages");
        }

        [Test]
        public async Task ARowGroup_WithItsLabelColumnAndTotals_AndPageBreaksBetween()
        {
            string rows = Row(Cell("H1", "Name"), Cell("H2", "Amount"))
                + Row(Cell("D1", "=Fields!Name.Value"), Cell("D2", "=Fields!Amount.Value"))
                + Row(Cell("GF1", "Subtotal"), Cell("GF2", "=Sum(Fields!Amount.Value)"));
            string members = $@"<TablixMember><TablixHeader><Size>1in</Size><CellContents>{Tb("RH", "Region")}</CellContents></TablixHeader>
                  <KeepWithGroup>After</KeepWithGroup><RepeatOnNewPage>true</RepeatOnNewPage></TablixMember>
                <TablixMember>
                  <Group Name=""ByRegion""><GroupExpressions><GroupExpression>=Fields!Region.Value</GroupExpression></GroupExpressions>
                    <PageBreak><BreakLocation>Between</BreakLocation></PageBreak></Group>
                  <SortExpressions><SortExpression><Value>=Fields!Region.Value</Value><Direction>Descending</Direction></SortExpression></SortExpressions>
                  <TablixHeader><Size>1in</Size><CellContents>{Tb("RG", "=Fields!Region.Value")}</CellContents></TablixHeader>
                  <TablixMembers>
                    <TablixMember><Group Name=""Details"" /><SortExpressions><SortExpression><Value>=Fields!Name.Value</Value></SortExpression></SortExpressions></TablixMember>
                    <TablixMember><KeepWithGroup>Before</KeepWithGroup></TablixMember>
                  </TablixMembers>
                </TablixMember>";
            var items = await AssertSameBothWays(Report(Tablix("T", 2, rows, members)));

            Assert.That(items.Any(i => i.EndsWith(" South")), Is.True, "the group's label column is there");
            Assert.That(items.Select(i => i.Split(' ')[0]).Distinct().Count(), Is.GreaterThanOrEqualTo(3), "a page per region");
        }

        [Test]
        public async Task NestedGroups_WithHeaderRowsAndHiddenDetails()
        {
            string rows = Row(Cell("H1", "Where"), Cell("H2", "Amount"))
                + Row(Cell("R1", "=Fields!Region.Value"), Cell("R2", "=Sum(Fields!Amount.Value)"))
                + Row(Cell("C1", "=Fields!City.Value"), Cell("C2", "=Count(Fields!Name.Value)"))
                + Row(Cell("D1", "=Fields!Name.Value"), Cell("D2", "=Fields!Amount.Value"))
                + Row(Cell("RF1", "end of region"), Cell("RF2", "=Max(Fields!Amount.Value)"));
            string members = @"<TablixMember />
                <TablixMember>
                  <Group Name=""ByRegion""><GroupExpressions><GroupExpression>=Fields!Region.Value</GroupExpression></GroupExpressions></Group>
                  <SortExpressions><SortExpression><Value>=Fields!Region.Value</Value></SortExpression></SortExpressions>
                  <TablixMembers>
                    <TablixMember />
                    <TablixMember>
                      <Group Name=""ByCity""><GroupExpressions><GroupExpression>=Fields!City.Value</GroupExpression></GroupExpressions></Group>
                      <SortExpressions><SortExpression><Value>=Fields!City.Value</Value></SortExpression></SortExpressions>
                      <TablixMembers>
                        <TablixMember />
                        <TablixMember><Group Name=""Details"" /><Visibility><Hidden>true</Hidden><ToggleItem>C1</ToggleItem></Visibility></TablixMember>
                      </TablixMembers>
                    </TablixMember>
                    <TablixMember />
                  </TablixMembers>
                </TablixMember>";
            var items = await AssertSameBothWays(Report(Tablix("T", 2, rows, members)));

            Assert.That(items.Any(i => i.EndsWith(" City2")), Is.True);
            Assert.That(items.Any(i => i.EndsWith(" Name05")), Is.False, "the details start hidden");
        }

        [Test]
        public async Task OneRowPerGroup_ASummaryTable()
        {
            string rows = Row(Cell("H1", "Region"), Cell("H2", "Total"))
                + Row(Cell("G1", "=Fields!Region.Value"), Cell("G2", "=Sum(Fields!Amount.Value)"));
            string members = @"<TablixMember />
                <TablixMember><Group Name=""ByRegion""><GroupExpressions><GroupExpression>=Fields!Region.Value</GroupExpression></GroupExpressions></Group>
                  <SortExpressions><SortExpression><Value>=Sum(Fields!Amount.Value)</Value><Direction>Descending</Direction></SortExpression></SortExpressions></TablixMember>";
            var items = await AssertSameBothWays(Report(Tablix("T", 2, rows, members)));

            Assert.That(items.Count(i => i.EndsWith(" North") || i.EndsWith(" South") || i.EndsWith(" East")), Is.EqualTo(3));
        }

        [Test]
        public async Task Spans_EmptyCells_AndAHiddenColumn()
        {
            string rows = Row(Cell("H1", "Both", "<RowSpan>2</RowSpan>"), Cell("H2", "Wide", "<ColSpan>2</ColSpan>"), "<TablixCell />")
                + Row("<TablixCell />", Cell("H3", "Amount"), "<TablixCell><CellContents /></TablixCell>")
                + Row(Cell("D1", "=Fields!Name.Value"), Cell("D2", "=Fields!Amount.Value"), Cell("D3", "=Fields!City.Value"));
            string members = @"<TablixMember /><TablixMember /><TablixMember><Group Name=""Details"" /></TablixMember>";
            string columnMembers = @"<TablixMember /><TablixMember /><TablixMember><Visibility><Hidden>true</Hidden></Visibility></TablixMember>";
            var items = await AssertSameBothWays(Report(Tablix("T", 3, rows, members, columnMembers)));

            Assert.That(items.Any(i => i.EndsWith(" City1")), Is.False, "the hidden column");
            Assert.That(items.Any(i => i.EndsWith(" Wide")), Is.True);
        }

        [Test]
        public async Task ATablixInAGroupsCell_RunsNativelyToo()
        {
            string inner = Tablix("Inner", 2, Row(Cell("I1", "=Fields!Name.Value"), Cell("I2", "=Fields!City.Value")),
                @"<TablixMember><Group Name=""InnerDetails"" /></TablixMember>").Replace("<DataSetName>Data</DataSetName>", "");
            string rows = Row(Cell("G1", "=Fields!Region.Value"), $"<TablixCell><CellContents>{inner}</CellContents></TablixCell>");
            string members = @"<TablixMember><Group Name=""ByRegion""><GroupExpressions><GroupExpression>=Fields!Region.Value</GroupExpression></GroupExpressions></Group></TablixMember>";
            string outer = Tablix("T", 2, rows, members).Replace("<Width>1.25in</Width></TablixColumn></TablixColumns>", "<Width>2.5in</Width></TablixColumn></TablixColumns>");
            var items = await AssertSameBothWays(Report(outer));

            Assert.That(items.Count(i => i.Contains(" Name")), Is.EqualTo(36), "each region's rows in its own cell");

            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(Report(outer)) { SkipDatabaseSchemaValidation = true, NativeTablix = true }.Parse();
            var table = (Tablix)report.ReportDefinition.Body.ReportItems.Items.Single();
            var cell = table.TableGroups.Items[0].Header.TableRows.Items[0].TableCells.Items[1];
            Assert.That(cell.ReportItems.Items.Single(), Is.InstanceOf<Tablix>());
        }

        [Test]
        public void TheDesigner_StillEditsATable()
        {
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml(Report(Tablix("T", 2, Row(Cell("D1", "=Fields!Name.Value"), Cell("D2", "=Fields!Amount.Value")), @"<TablixMember><Group Name=""Details"" /></TablixMember>")));
            Rdl2008Exporter.NormalizeForEditing(doc);
            Assert.That(doc.GetElementsByTagName("Tablix").Count, Is.EqualTo(0));
            Assert.That(doc.GetElementsByTagName("Table").Count, Is.EqualTo(1));
        }
    }
}
#endif
