#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace ReportTests
{
    /// <summary>
    /// Interactive sorting (a textbox's UserSort, issue #636): a click on a sort header, through
    /// <see cref="Report.UserSort"/>, sorts the level it names the next time the pages are built.
    /// </summary>
    [TestFixture]
    public class UserSortTests
    {
        private const string Rdl2005 = "http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition";
        private const string Rdl2010 = "http://schemas.microsoft.com/sqlserver/reporting/2010/01/reportdefinition";

        [SetUp]
        public void SetUp() => RdlEngineConfig.RdlEngineConfigInit();

        private static string Report2005(string items) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""{Rdl2005}"">
  <Width>400pt</Width>
  <DataSources><DataSource Name=""D""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>D</DataSourceName><CommandText>none</CommandText></Query>
    <Fields>
      <Field Name=""Region""><DataField>Region</DataField></Field>
      <Field Name=""Name""><DataField>Name</DataField></Field>
      <Field Name=""Amount""><DataField>Amount</DataField><TypeName>Int32</TypeName></Field>
    </Fields>
  </DataSet></DataSets>
  <Body><Height>600pt</Height><ReportItems>{items}</ReportItems></Body>
  <PageHeight>2000pt</PageHeight><PageWidth>500pt</PageWidth>
</Report>";

        private static string Cell(string name, string value, string userSort = "") =>
            $@"<TableCell><ReportItems><Textbox Name=""{name}""><Value>{value}</Value>{userSort}</Textbox></ReportItems></TableCell>";

        private static string Row(params string[] cells) =>
            $@"<TableRows><TableRow><Height>14pt</Height><TableCells>{string.Concat(cells)}</TableCells></TableRow></TableRows>";

        private static string UserSort(string expression, string scope = null, string target = null) =>
            "<UserSort><SortExpression>" + expression + "</SortExpression>"
            + (scope == null ? "" : "<SortExpressionScope>" + scope + "</SortExpressionScope>")
            + (target == null ? "" : "<SortTarget>" + target + "</SortTarget>")
            + "</UserSort>";

        // A table of the detail rows, "D" + Name + Amount, with sort headers in its header.
        private static string DetailTable(string name, string top, params string[] headerCells) => $@"
<Table Name=""{name}""><Top>{top}</Top>
  <DataSetName>Data</DataSetName>
  <TableColumns>{string.Concat(headerCells.Select(_ => "<TableColumn><Width>100pt</Width></TableColumn>"))}</TableColumns>
  <Header>{Row(headerCells)}</Header>
  <Details>{Row(headerCells.Select((_, i) => i == 0
        ? Cell(name + "Line", @"=""D "" &amp; Fields!Name.Value &amp; CStr(Fields!Amount.Value)")
        : Cell(name + "Pad" + i, "")).ToArray())}</Details>
</Table>";

        private static async Task<Report> Load(string rdl, params (string Region, string Name, int Amount)[] rows)
        {
            var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(8), string.Join(" | ", report.ErrorItems?.Cast<string>() ?? Enumerable.Empty<string>()));
            await SetData(report, rows);
            return report;
        }

        private static async Task SetData(Report report, (string Region, string Name, int Amount)[] rows)
        {
            var table = new DataTable();
            table.Columns.Add("Region", typeof(string));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Amount", typeof(int));
            foreach (var (region, name, amount) in rows)
                table.Rows.Add(region, name, amount);
            await report.DataSets["Data"].SetData(table);
            await report.RunGetData(null);
        }

        private static PageText[] Texts(Pages pages) =>
            pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>()
                .OrderBy(t => t.Y).ThenBy(t => t.X).ToArray();

        // The detail and group lines, top to bottom: "D <name><amount>" and "G <group>".
        private static async Task<string[]> Lines(Report report, string prefix = null)
        {
            var pages = await report.BuildPages();
            return Texts(pages).Select(t => t.Text)
                .Where(t => t != null && (t.StartsWith("D ") || t.StartsWith("G ")) && (prefix == null || t.StartsWith(prefix)))
                .ToArray();
        }

        private static readonly (string, string, int)[] People =
            { ("N", "c", 3), ("N", "a", 1), ("S", "b", 2) };

        [Test]
        public async Task Without_a_sort_the_rows_are_in_data_order()
        {
            var report = await Load(Report2005(DetailTable("T", "0pt", Cell("SortName", "Name", UserSort("=Fields!Name.Value")))), People);

            Assert.That(await Lines(report), Is.EqualTo(new[] { "D c3", "D a1", "D b2" }));

            report.UserSort = new UserSortState();		// an empty state is no sort either
            Assert.That(await Lines(report), Is.EqualTo(new[] { "D c3", "D a1", "D b2" }));
        }

        [Test]
        public async Task A_click_sorts_the_detail_rows_ascending_then_descending()
        {
            var report = await Load(Report2005(DetailTable("T", "0pt", Cell("SortName", "Name", UserSort("=Fields!Name.Value")))), People);

            Assert.That(report.ToggleUserSort("SortName"), Is.EqualTo(UserSortDirection.Ascending));
            Assert.That(await Lines(report), Is.EqualTo(new[] { "D a1", "D b2", "D c3" }));

            Assert.That(report.ToggleUserSort("SortName"), Is.EqualTo(UserSortDirection.Descending));
            Assert.That(await Lines(report), Is.EqualTo(new[] { "D c3", "D b2", "D a1" }));

            report.UserSort.Clear();
            Assert.That(await Lines(report), Is.EqualTo(new[] { "D c3", "D a1", "D b2" }));
        }

        [Test]
        public async Task The_sort_header_page_item_names_its_sort_and_direction()
        {
            var report = await Load(Report2005(DetailTable("T", "0pt", Cell("SortName", "Name", UserSort("=Fields!Name.Value")))), People);

            PageText Header(Pages pages) => Texts(pages).Single(t => t.Text == "Name");
            var header = Header(await report.BuildPages());
            Assert.That(header.UserSortKey, Is.EqualTo("SortName"));
            Assert.That(header.UserSortDirection, Is.EqualTo(UserSortDirection.None));
            Assert.That(Texts(await report.BuildPages()).Where(t => t.Text.StartsWith("D ")).All(t => t.UserSortKey == null));

            report.ToggleUserSort(header.UserSortKey);
            header = Header(await report.BuildPages());
            Assert.That(header.UserSortDirection, Is.EqualTo(UserSortDirection.Ascending));

            header.GetUserSortGlyphBounds(out float x, out float y, out float size);
            Assert.That(x, Is.GreaterThanOrEqualTo(header.X + header.W - PageItem.UserSortGlyphSpace));
            Assert.That(x + size, Is.LessThanOrEqualTo(header.X + header.W));
            Assert.That(y, Is.GreaterThanOrEqualTo(header.Y));
        }

        // A table grouped on Region (G), its header line "G <region>", detail lines under it.
        private static string GroupedTable(string headerCell, string innerGroup = "") => $@"
<Table Name=""T"">
  <DataSetName>Data</DataSetName>
  <TableColumns><TableColumn><Width>200pt</Width></TableColumn></TableColumns>
  <Header>{Row(headerCell)}</Header>
  <TableGroups>
    <TableGroup>
      <Grouping Name=""G""><GroupExpressions><GroupExpression>=Fields!Region.Value</GroupExpression></GroupExpressions></Grouping>
      <Header>{Row(Cell("RegionName", @"=""G "" &amp; Fields!Region.Value"))}</Header>
    </TableGroup>{innerGroup}
  </TableGroups>
  <Details>{Row(Cell("Line", @"=""D "" &amp; Fields!Name.Value &amp; CStr(Fields!Amount.Value)"))}</Details>
</Table>";

        [Test]
        public async Task Groups_sort_by_an_aggregate_at_group_scope()
        {
            var rdl = Report2005(GroupedTable(Cell("SortTotal", "Total", UserSort("=Sum(Fields!Amount.Value)", "G"))));
            // E totals 6, N 3, S 10
            var report = await Load(rdl, ("N", "a", 1), ("S", "b", 10), ("E", "c", 5), ("N", "d", 2), ("E", "e", 1));

            Assert.That(await Lines(report, "G "), Is.EqualTo(new[] { "G E", "G N", "G S" }), "by the group value");

            report.ToggleUserSort("SortTotal");
            Assert.That(await Lines(report, "G "), Is.EqualTo(new[] { "G N", "G E", "G S" }));

            report.ToggleUserSort("SortTotal");
            Assert.That(await Lines(report, "G "), Is.EqualTo(new[] { "G S", "G E", "G N" }));
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(8), string.Join(" | ", report.ErrorItems?.Cast<string>() ?? Enumerable.Empty<string>()));
        }

        [Test]
        public async Task Detail_rows_sort_within_each_group()
        {
            var rdl = Report2005(GroupedTable(Cell("SortAmount", "Amount", UserSort("=Fields!Amount.Value"))));
            var report = await Load(rdl, ("N", "a", 3), ("S", "b", 10), ("N", "c", 1), ("S", "d", 5), ("N", "e", 2));

            Assert.That(await Lines(report), Is.EqualTo(new[] { "G N", "D a3", "D c1", "D e2", "G S", "D b10", "D d5" }));

            report.ToggleUserSort("SortAmount");
            Assert.That(await Lines(report), Is.EqualTo(new[] { "G N", "D c1", "D e2", "D a3", "G S", "D d5", "D b10" }));
        }

        [Test]
        public async Task An_inner_group_sorts_within_each_instance_of_the_outer_group()
        {
            string inner = $@"
    <TableGroup>
      <Grouping Name=""GN""><GroupExpressions><GroupExpression>=Fields!Name.Value</GroupExpression></GroupExpressions></Grouping>
      <Header>{Row(Cell("NameTotal", @"=""G "" &amp; Fields!Name.Value &amp; CStr(Sum(Fields!Amount.Value))"))}</Header>
    </TableGroup>";
            var rdl = Report2005(GroupedTable(Cell("SortNames", "Total", UserSort("=Sum(Fields!Amount.Value)", "GN")), inner));
            // N: a 1+1=2, b 5, c 3.  S: a 7, z 4.
            var report = await Load(rdl, ("N", "a", 1), ("N", "b", 5), ("S", "z", 4), ("N", "c", 3), ("S", "a", 7), ("N", "a", 1));

            Assert.That(await Lines(report, "G "), Is.EqualTo(new[] { "G N", "G a2", "G b5", "G c3", "G S", "G a7", "G z4" }));

            report.ToggleUserSort("SortNames");
            Assert.That(await Lines(report, "G "), Is.EqualTo(new[] { "G N", "G a2", "G c3", "G b5", "G S", "G z4", "G a7" }));

            report.ToggleUserSort("SortNames");
            Assert.That(await Lines(report, "G "), Is.EqualTo(new[] { "G N", "G b5", "G c3", "G a2", "G S", "G a7", "G z4" }));
        }

        [Test]
        public async Task A_sort_target_in_another_region_is_sorted_too()
        {
            var rdl = Report2005(
                DetailTable("T1", "0pt", Cell("SortName", "Name", UserSort("=Fields!Name.Value", target: "T2")))
                + DetailTable("T2", "200pt", Cell("Other", "Other"))
                + DetailTable("T3", "400pt", Cell("Third", "Third")));
            var report = await Load(rdl, People);

            report.ToggleUserSort("SortName");
            var texts = Texts(await report.BuildPages());
            var lines = texts.Where(t => t.Text.StartsWith("D ")).Select(t => t.Text).ToArray();
            Assert.That(lines, Is.EqualTo(new[] { "D a1", "D b2", "D c3", "D a1", "D b2", "D c3", "D c3", "D a1", "D b2" }),
                "T1 and its target T2 are sorted; T3 is not");
        }

        [Test]
        public async Task Several_sorts_apply_in_the_order_they_were_clicked()
        {
            var rdl = Report2005(DetailTable("T", "0pt",
                Cell("SortAmount", "Amount", UserSort("=Fields!Amount.Value")),
                Cell("SortName", "Name", UserSort("=Fields!Name.Value"))));
            var report = await Load(rdl, ("", "b", 2), ("", "a", 2), ("", "c", 1), ("", "a", 1));

            report.ToggleUserSort("SortAmount");
            report.ToggleUserSort("SortName", additive: true);
            Assert.That(await Lines(report), Is.EqualTo(new[] { "D a1", "D c1", "D a2", "D b2" }), "amount, then name");
            Assert.That(report.UserSort.ToString(), Is.EqualTo("ASortAmount,ASortName"));

            report.UserSort.Clear();
            report.ToggleUserSort("SortName");
            report.ToggleUserSort("SortAmount", additive: true);
            report.ToggleUserSort("SortAmount", additive: true);		// flips in its place: still second
            Assert.That(await Lines(report), Is.EqualTo(new[] { "D a2", "D a1", "D b2", "D c1" }), "name, then amount descending");

            report.ToggleUserSort("SortName");		// a plain click replaces the others
            Assert.That(report.UserSort.Entries.Select(e => e.Key), Is.EqualTo(new[] { "SortName" }));
            Assert.That(await Lines(report), Is.EqualTo(new[] { "D c1", "D b2", "D a2", "D a1" }), "name descending; ties in data order");
        }

        [Test]
        public void The_state_reads_back_what_it_writes()
        {
            var state = new UserSortState();
            state.Set("Sort, \"odd\" name", UserSortDirection.Descending);
            state.Set("Plain", UserSortDirection.Ascending);
            var copy = UserSortState.Parse(state.ToString());
            Assert.That(copy.Entries, Is.EqualTo(state.Entries));
            Assert.That(copy.NextDirection("Plain"), Is.EqualTo(UserSortDirection.Descending));
            Assert.That(copy.NextDirection("Missing"), Is.EqualTo(UserSortDirection.Ascending));
            Assert.That(UserSortState.Parse("Xjunk,,A").IsEmpty, Is.True);
        }

        [Test]
        public async Task A_scope_or_target_that_names_nothing_is_reported()
        {
            var rdl = Report2005(DetailTable("T", "0pt", Cell("SortName", "Name", UserSort("=Fields!Name.Value", "Nowhere", "Nothing"))));
            var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            var errors = report.ErrorItems.Cast<string>().ToArray();
            Assert.That(report.ErrorMaxSeverity, Is.EqualTo(4));
            Assert.That(errors, Has.Some.Contains("SortExpressionScope 'Nowhere'"));
            Assert.That(errors, Has.Some.Contains("SortTarget 'Nothing'"));

            await SetData(report, People);
            report.ToggleUserSort("SortName");		// still sorts its own detail rows
            Assert.That(await Lines(report), Is.EqualTo(new[] { "D a1", "D b2", "D c3" }));
        }

        [Test]
        public async Task A_user_sort_without_an_expression_is_an_error()
        {
            var rdl = Report2005(DetailTable("T", "0pt", Cell("SortName", "Name", "<UserSort><SortExpressionScope>T</SortExpressionScope></UserSort>")));
            var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.EqualTo(8));
            Assert.That(report.ErrorItems.Cast<string>(), Has.Some.Contains("requires the SortExpression"));
        }

        [Test]
        public async Task A_grouped_list_sorts_its_instances()
        {
            var list = $@"
<List Name=""L"">
  <DataSetName>Data</DataSetName><Top>20pt</Top><Width>300pt</Width><Height>14pt</Height>
  <Grouping Name=""LG""><GroupExpressions><GroupExpression>=Fields!Region.Value</GroupExpression></GroupExpressions></Grouping>
  <ReportItems><Textbox Name=""LLine""><Top>0pt</Top><Width>300pt</Width><Height>14pt</Height><Value>=""G "" &amp; Fields!Region.Value</Value></Textbox></ReportItems>
</List>
<Textbox Name=""SortRegions""><Top>0pt</Top><Width>100pt</Width><Height>14pt</Height><Value>Regions</Value>{UserSort("=Sum(Fields!Amount.Value)", "LG")}</Textbox>";
            var report = await Load(Report2005(list), ("N", "a", 1), ("S", "b", 10), ("E", "c", 5));

            Assert.That(await Lines(report), Is.EqualTo(new[] { "G E", "G N", "G S" }));
            report.ToggleUserSort("SortRegions");
            report.ToggleUserSort("SortRegions");
            Assert.That(await Lines(report), Is.EqualTo(new[] { "G S", "G E", "G N" }));
        }

        // RDL 2008+: a Tablix grouped on Region, a sort header over the groups' totals and one
        // over the detail rows' names.
        private const string Tablix2010 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""" + Rdl2010 + @""">
  <DataSources><DataSource Name=""D""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>D</DataSourceName><CommandText>none</CommandText></Query>
    <Fields>
      <Field Name=""Region""><DataField>Region</DataField></Field>
      <Field Name=""Name""><DataField>Name</DataField></Field>
      <Field Name=""Amount""><DataField>Amount</DataField></Field>
    </Fields>
  </DataSet></DataSets>
  <ReportSections><ReportSection>
    <Body><Height>300pt</Height><ReportItems>
      <Tablix Name=""TX"">
        <DataSetName>Data</DataSetName>
        <TablixBody>
          <TablixColumns><TablixColumn><Width>150pt</Width></TablixColumn><TablixColumn><Width>150pt</Width></TablixColumn></TablixColumns>
          <TablixRows>
            <TablixRow><Height>14pt</Height><TablixCells>
              <TablixCell><CellContents><Textbox Name=""SortTotal""><Paragraphs><Paragraph><TextRuns><TextRun><Value>Total</Value></TextRun></TextRuns></Paragraph></Paragraphs>
                <UserSort><SortExpression>=Sum(Fields!Amount.Value)</SortExpression><SortExpressionScope>RegionGroup</SortExpressionScope></UserSort></Textbox></CellContents></TablixCell>
              <TablixCell><CellContents><Textbox Name=""SortName""><Paragraphs><Paragraph><TextRuns><TextRun><Value>Name</Value></TextRun></TextRuns></Paragraph></Paragraphs>
                <UserSort><SortExpression>=Fields!Name.Value</SortExpression></UserSort></Textbox></CellContents></TablixCell>
            </TablixCells></TablixRow>
            <TablixRow><Height>14pt</Height><TablixCells>
              <TablixCell><CellContents><Textbox Name=""RegionName""><Paragraphs><Paragraph><TextRuns><TextRun><Value>=""G "" &amp; Fields!Region.Value</Value></TextRun></TextRuns></Paragraph></Paragraphs></Textbox></CellContents></TablixCell>
              <TablixCell><CellContents><Textbox Name=""Blank""><Paragraphs><Paragraph><TextRuns><TextRun><Value></Value></TextRun></TextRuns></Paragraph></Paragraphs></Textbox></CellContents></TablixCell>
            </TablixCells></TablixRow>
            <TablixRow><Height>14pt</Height><TablixCells>
              <TablixCell><CellContents><Textbox Name=""Line""><Paragraphs><Paragraph><TextRuns><TextRun><Value>=""D "" &amp; Fields!Name.Value &amp; CStr(Fields!Amount.Value)</Value></TextRun></TextRuns></Paragraph></Paragraphs></Textbox></CellContents></TablixCell>
              <TablixCell><CellContents><Textbox Name=""Blank2""><Paragraphs><Paragraph><TextRuns><TextRun><Value></Value></TextRun></TextRuns></Paragraph></Paragraphs></Textbox></CellContents></TablixCell>
            </TablixCells></TablixRow>
          </TablixRows>
        </TablixBody>
        <TablixColumnHierarchy><TablixMembers><TablixMember /><TablixMember /></TablixMembers></TablixColumnHierarchy>
        <TablixRowHierarchy><TablixMembers>
          <TablixMember><KeepWithGroup>After</KeepWithGroup></TablixMember>
          <TablixMember>
            <Group Name=""RegionGroup""><GroupExpressions><GroupExpression>=Fields!Region.Value</GroupExpression></GroupExpressions></Group>
            <SortExpressions><SortExpression><Value>=Fields!Region.Value</Value></SortExpression></SortExpressions>
            <TablixMembers>
              <TablixMember><KeepWithGroup>After</KeepWithGroup></TablixMember>
              <TablixMember><Group Name=""Details"" /></TablixMember>
            </TablixMembers>
          </TablixMember>
        </TablixMembers></TablixRowHierarchy>
        <Top>0pt</Top><Left>0pt</Left><Height>42pt</Height><Width>300pt</Width>
      </Tablix>
    </ReportItems></Body>
    <Width>400pt</Width>
    <Page><PageHeight>2000pt</PageHeight><PageWidth>500pt</PageWidth></Page>
  </ReportSection></ReportSections>
</Report>";

        [TestCase(false)]
        [TestCase(true)]
        public async Task A_2008_tablix_sorts_its_groups_and_details(bool nativeTablix)
        {
            var report = await new RDLParser(Tablix2010) { SkipDatabaseSchemaValidation = true, NativeTablix = nativeTablix }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(8), string.Join(" | ", report.ErrorItems?.Cast<string>() ?? Enumerable.Empty<string>()));
            // E totals 6, N 3, S 10
            await SetData(report, new[] { ("N", "b", 1), ("S", "x", 10), ("E", "c", 5), ("N", "a", 2), ("E", "d", 1) });

            Assert.That(await Lines(report), Is.EqualTo(new[] { "G E", "D c5", "D d1", "G N", "D b1", "D a2", "G S", "D x10" }));

            report.ToggleUserSort("SortTotal");
            report.ToggleUserSort("SortTotal");
            report.ToggleUserSort("SortName", additive: true);
            Assert.That(await Lines(report), Is.EqualTo(new[] { "G S", "D x10", "G E", "D c5", "D d1", "G N", "D a2", "D b1" }));
        }

        private static string Tb(string name, string value, string extra = "") =>
            $@"<Textbox Name=""{name}""><Paragraphs><Paragraph><TextRuns><TextRun><Value>{value}</Value></TextRun></TextRuns></Paragraph></Paragraphs>{extra}</Textbox>";

        // RDL 2008+ with a column group: a Matrix when normalised, a TablixPivot run natively. The
        // sort header is in the corner; the row members are the Region groups or the detail rows.
        private static string Pivot2010(string rowMember, string userSort) => Tablix2010.Substring(0, Tablix2010.IndexOf("<Tablix Name")) + $@"<Tablix Name=""PX"">
        <DataSetName>Data</DataSetName>
        <TablixCorner><TablixCornerRows><TablixCornerRow><TablixCornerCell><CellContents>{Tb("SortRows", "Sort", userSort)}</CellContents></TablixCornerCell></TablixCornerRow></TablixCornerRows></TablixCorner>
        <TablixBody>
          <TablixColumns><TablixColumn><Width>60pt</Width></TablixColumn></TablixColumns>
          <TablixRows><TablixRow><Height>14pt</Height><TablixCells><TablixCell><CellContents>{Tb("V", "=Sum(Fields!Amount.Value)")}</CellContents></TablixCell></TablixCells></TablixRow></TablixRows>
        </TablixBody>
        <TablixColumnHierarchy><TablixMembers><TablixMember>
          <Group Name=""ByCity""><GroupExpressions><GroupExpression>=Fields!Name.Value</GroupExpression></GroupExpressions></Group>
          <TablixHeader><Size>14pt</Size><CellContents>{Tb("CH", "=Fields!Name.Value")}</CellContents></TablixHeader>
        </TablixMember></TablixMembers></TablixColumnHierarchy>
        <TablixRowHierarchy><TablixMembers>{rowMember}</TablixMembers></TablixRowHierarchy>
        <Top>0pt</Top><Left>0pt</Left><Height>28pt</Height><Width>120pt</Width>
      </Tablix>
    </ReportItems></Body>
    <Width>400pt</Width>
    <Page><PageHeight>2000pt</PageHeight><PageWidth>500pt</PageWidth></Page>
  </ReportSection></ReportSections>
</Report>";

        [TestCase(false)]
        [TestCase(true)]
        public async Task A_2008_pivot_sorts_its_row_groups(bool nativeTablix)
        {
            string member = $@"<TablixMember>
            <Group Name=""ByRegion""><GroupExpressions><GroupExpression>=Fields!Region.Value</GroupExpression></GroupExpressions></Group>
            <TablixHeader><Size>60pt</Size><CellContents>{Tb("RH", @"=""G "" &amp; Fields!Region.Value")}</CellContents></TablixHeader>
          </TablixMember>";
            var report = await new RDLParser(Pivot2010(member, UserSort("=Sum(Fields!Amount.Value)", "ByRegion")))
                { SkipDatabaseSchemaValidation = true, NativeTablix = nativeTablix }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(8), string.Join(" | ", report.ErrorItems?.Cast<string>() ?? Enumerable.Empty<string>()));
            Assert.That(report.ReportDefinition.Body.ReportItems.Items.Single(), nativeTablix ? Is.InstanceOf<TablixPivot>() : Is.InstanceOf<Matrix>());
            // E totals 6, N 3, S 10
            await SetData(report, new[] { ("N", "x", 1), ("S", "y", 10), ("E", "x", 5), ("N", "y", 2), ("E", "y", 1) });

            Assert.That(await Lines(report), Is.EqualTo(new[] { "G E", "G N", "G S" }));
            report.ToggleUserSort("SortRows");
            Assert.That(await Lines(report), Is.EqualTo(new[] { "G N", "G E", "G S" }));
            report.ToggleUserSort("SortRows");
            Assert.That(await Lines(report), Is.EqualTo(new[] { "G S", "G E", "G N" }));
        }

        [Test]
        public async Task A_native_pivot_sorts_its_detail_rows()
        {
            string member = $@"<TablixMember>
            <Group Name=""Details"" />
            <TablixHeader><Size>60pt</Size><CellContents>{Tb("RH", @"=""D "" &amp; Fields!Region.Value &amp; CStr(Fields!Amount.Value)")}</CellContents></TablixHeader>
          </TablixMember>";
            var report = await new RDLParser(Pivot2010(member, UserSort("=Fields!Amount.Value")))
                { SkipDatabaseSchemaValidation = true, NativeTablix = true }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(8), string.Join(" | ", report.ErrorItems?.Cast<string>() ?? Enumerable.Empty<string>()));
            Assert.That(report.ReportDefinition.Body.ReportItems.Items.Single(), Is.InstanceOf<TablixPivot>());
            await SetData(report, new[] { ("N", "x", 3), ("S", "y", 10), ("E", "x", 1) });

            Assert.That(await Lines(report), Is.EqualTo(new[] { "D N3", "D S10", "D E1" }));
            report.ToggleUserSort("SortRows");
            report.ToggleUserSort("SortRows");
            Assert.That(await Lines(report), Is.EqualTo(new[] { "D S10", "D N3", "D E1" }));
        }

        [Test]
        public void A_2008_user_sort_survives_the_designer_round_trip()
        {
            var doc = new XmlDocument();
            doc.LoadXml(Tablix2010);

            Assert.That(Rdl2008Exporter.NormalizeForEditing(doc), Is.True);
            Assert.That(doc.GetElementsByTagName("Tablix"), Is.Empty, "the designer edits the 2005 shape");
            AssertUserSorts(doc);

            Rdl2008Exporter.ConvertToTablix(doc);
            Assert.That(doc.GetElementsByTagName("Tablix"), Is.Not.Empty);
            Assert.That(doc.GetElementsByTagName("Table"), Is.Empty);
            AssertUserSorts(doc);
            foreach (XmlElement userSort in doc.GetElementsByTagName("UserSort"))
                Assert.That(((XmlElement)userSort.ParentNode)["Paragraphs"], Is.Not.Null, "on a 2008 Textbox");
        }

        private static void AssertUserSorts(XmlDocument doc)
        {
            var sorts = doc.GetElementsByTagName("UserSort").Cast<XmlElement>()
                .ToDictionary(u => ((XmlElement)u.ParentNode).GetAttribute("Name"));
            Assert.That(sorts.Keys, Is.EquivalentTo(new[] { "SortTotal", "SortName" }));
            Assert.That(sorts["SortTotal"]["SortExpression"].InnerText, Is.EqualTo("=Sum(Fields!Amount.Value)"));
            Assert.That(sorts["SortTotal"]["SortExpressionScope"].InnerText, Is.EqualTo("RegionGroup"));
            Assert.That(sorts["SortName"]["SortExpression"].InnerText, Is.EqualTo("=Fields!Name.Value"));
            Assert.That(sorts["SortName"]["SortExpressionScope"], Is.Null);
            Assert.That(doc.GetElementsByTagName("Group").Cast<XmlElement>().Concat(doc.GetElementsByTagName("Grouping").Cast<XmlElement>())
                .Any(g => g.GetAttribute("Name") == "RegionGroup"), "the scope it names is still there");
        }
    }
}
#endif
