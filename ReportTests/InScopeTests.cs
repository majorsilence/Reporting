using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// InScope("Group") is true for a matrix cell inside one instance of that group and false for a
    /// subtotal cell that totals over all of them. Reports use it to format totals differently
    /// from detail (issue #411).
    /// </summary>
    [TestFixture]
    public class InScopeTests
    {
        private const string Rdl2005 = "http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition";

        private static string Report (string cellValue, string extraItems = "") => $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""{Rdl2005}"">
  <DataSources><DataSource Name=""DS1""><ConnectionProperties><DataProvider>SQLite</DataProvider><ConnectString>Data Source=none.db</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>DS1</DataSourceName><CommandText>/* Local Query */</CommandText></Query>
    <Fields><Field Name=""Name""><DataField>Name</DataField></Field><Field Name=""Year""><DataField>Year</DataField></Field><Field Name=""Amount""><DataField>Amount</DataField></Field></Fields>
  </DataSet></DataSets>
  <Body><Height>3in</Height><ReportItems>
    <Matrix Name=""Pivot""><DataSetName>Data</DataSetName><Top>0in</Top><Left>0in</Left>
      <Corner><ReportItems><Textbox Name=""Corner""><Value>Corner</Value></Textbox></ReportItems></Corner>
      <ColumnGroupings><ColumnGrouping><Height>0.25in</Height><DynamicColumns>
        <Grouping Name=""Years""><GroupExpressions><GroupExpression>=Fields!Year.Value</GroupExpression></GroupExpressions></Grouping>
        <ReportItems><Textbox Name=""YearHead""><Value>=Fields!Year.Value</Value></Textbox></ReportItems>
        <Subtotal><ReportItems><Textbox Name=""YearTotal""><Value>AllYears</Value></Textbox></ReportItems></Subtotal>
      </DynamicColumns></ColumnGrouping></ColumnGroupings>
      <RowGroupings><RowGrouping><Width>1in</Width><DynamicRows>
        <Grouping Name=""Names""><GroupExpressions><GroupExpression>=Fields!Name.Value</GroupExpression></GroupExpressions></Grouping>
        <ReportItems><Textbox Name=""NameHead""><Value>=Fields!Name.Value</Value></Textbox></ReportItems>
        <Subtotal><ReportItems><Textbox Name=""NameTotal""><Value>AllNames</Value></Textbox></ReportItems></Subtotal>
      </DynamicRows></RowGrouping></RowGroupings>
      <MatrixRows><MatrixRow><Height>0.25in</Height><MatrixCells><MatrixCell><ReportItems><Textbox Name=""Cell""><Value>{cellValue}</Value></Textbox></ReportItems></MatrixCell></MatrixCells></MatrixRow></MatrixRows>
      <MatrixColumns><MatrixColumn><Width>0.8in</Width></MatrixColumn></MatrixColumns>
    </Matrix>
    {extraItems}
  </ReportItems></Body>
  <Width>6in</Width>
</Report>";

        private static DataTable Data ()
        {
            var table = new DataTable ();
            table.Columns.Add ("Name", typeof (string));
            table.Columns.Add ("Year", typeof (string));
            table.Columns.Add ("Amount", typeof (double));
            table.Rows.Add ("Widget", "Y2020", 10.0);
            table.Rows.Add ("Widget", "Y2021", 20.0);
            table.Rows.Add ("Gadget", "Y2020", 30.0);
            return table;
        }

        private static async Task<string> Render (string rdl)
        {
            RdlEngineConfig.RdlEngineConfigInit ();
            var parser = new RDLParser (rdl) { SkipDatabaseSchemaValidation = true };
            using var report = await parser.Parse ();
            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4),
                string.Join (" | ", report.ErrorItems?.Cast<string> () ?? Enumerable.Empty<string> ()));
            await report.DataSets["Data"].SetData (Data ());
            await report.RunGetData (null);
            using var memory = new MemoryStreamGen ();
            await report.RunRender (memory, OutputPresentationType.HTML);
            return memory.GetText ();
        }

        private static string[][] Cells (string html)
            => Regex.Matches (html, "<tr[^>]*>(.*?)</tr>", RegexOptions.Singleline)
                .Cast<Match> ()
                .Select (m => Regex.Matches (m.Groups[1].Value, "<t[dh][^>]*>(.*?)</t[dh]>", RegexOptions.Singleline)
                    .Cast<Match> ()
                    .Select (c => Regex.Replace (Regex.Replace (c.Groups[1].Value, "<[^>]+>", " "), @"\s+", " ").Trim ())
                    .ToArray ())
                .Where (r => r.Length > 0)
                .ToArray ();

        [Test]
        public async Task InScope_IsFalseOnlyInTheSubtotalCells ()
        {
            var html = await Render (Report ("=IIf(InScope(&quot;Years&quot;), IIf(InScope(&quot;Names&quot;), &quot;dd&quot;, &quot;dt&quot;), IIf(InScope(&quot;Names&quot;), &quot;td&quot;, &quot;tt&quot;))"));
            var rows = Cells (html);

            // header: corner, Y2020, Y2021, AllYears
            Assert.That (rows[0], Is.EqualTo (new[] { "Corner", "Y2020", "Y2021", "AllYears" }));
            // in a Name row: inside a year -> "dd"; the AllYears column totals over years -> "td"
            Assert.That (rows.First (r => r[0] == "Gadget"), Is.EqualTo (new[] { "Gadget", "dd", "dd", "td" }));
            Assert.That (rows.First (r => r[0] == "Widget"), Is.EqualTo (new[] { "Widget", "dd", "dd", "td" }));
            // the AllNames row totals over names: "dt" under the years, "tt" in the corner cell of the totals
            Assert.That (rows.First (r => r[0] == "AllNames"), Is.EqualTo (new[] { "AllNames", "dt", "dt", "tt" }));
        }

        [Test]
        public async Task InScope_DrivesFormattingOfTotals ()
        {
            // The usual use: bold totals. FontWeight is an expression on the cell's own style.
            var rdl = Report ("=Sum(Fields!Amount.Value)").Replace (
                "<Textbox Name=\"Cell\"><Value>=Sum(Fields!Amount.Value)</Value></Textbox>",
                "<Textbox Name=\"Cell\"><Value>=Sum(Fields!Amount.Value)</Value><Style><FontWeight>=IIf(InScope(\"Years\") And InScope(\"Names\"), \"Normal\", \"Bold\")</FontWeight></Style></Textbox>");
            Assert.That (rdl, Does.Contain ("InScope"), "the fixture must carry the expression");

            var html = await Render (rdl);

            // Detail cells share one style class and the five total cells another, and only the latter is bold.
            var cells = Regex.Matches (html, "<td id='(css\\d+)'[^>]*>(\\d+)\\s*<").Cast<Match> ().Select (m => (Class: m.Groups[1].Value, Text: m.Groups[2].Value)).ToList ();
            Assert.That (cells.Select (c => c.Class).Distinct ().Count (), Is.EqualTo (2), "two styles among the data cells");
            var detail = cells.GroupBy (c => c.Class).OrderBy (g => g.Count ()).First ();   // 4 detail cells
            var totals = cells.GroupBy (c => c.Class).OrderBy (g => g.Count ()).Last ();    // 5 total cells
            Assert.That (new[] { detail.Count (), totals.Count () }, Is.EqualTo (new[] { 4, 5 }));
            Assert.That (Regex.IsMatch (html, @"#" + totals.Key + @"\s*\{[^}]*font-weight:\s*bold", RegexOptions.IgnoreCase)
                || Regex.IsMatch (html, @"\." + totals.Key + @"\s*\{[^}]*font-weight:\s*bold", RegexOptions.IgnoreCase), Is.True, "the totals' class is bold");
            Assert.That (Regex.IsMatch (html, @"[#.]" + detail.Key + @"\s*\{[^}]*font-weight:\s*bold", RegexOptions.IgnoreCase), Is.False, "the detail class is not");
        }

        [Test]
        public async Task InScope_OutsideAMatrix_IsAlwaysTrue ()
        {
            var textbox = @"<Textbox Name=""Outside""><Top>2in</Top><Left>0in</Left><Height>0.25in</Height><Width>2in</Width><Value>=IIf(InScope(&quot;Names&quot;), &quot;in&quot;, &quot;out&quot;)</Value></Textbox>";
            var html = await Render (Report ("=Sum(Fields!Amount.Value)", textbox));

            Assert.That (html, Does.Contain (">in<").Or.Contain ("in</"), "no matrix cell is being rendered, so the item is not outside the group");
            Assert.That (html, Does.Not.Contain (">out<"));
        }

        [Test]
        public void InScope_WithAnUnknownGroup_IsAParseError ()
        {
            RdlEngineConfig.RdlEngineConfigInit ();
            var rdl = Report ("=InScope(&quot;NoSuchGroup&quot;)");

            var parser = new RDLParser (rdl) { SkipDatabaseSchemaValidation = true };
            using var report = parser.Parse ().GetAwaiter ().GetResult ();

            Assert.That (report.ErrorMaxSeverity, Is.GreaterThanOrEqualTo (8), "a misspelt scope should be reported, not silently true");
        }

        // --- tables: the answer depends on where the expression sits ---------------------------------------

        private static string TableRow (string name)
            => $@"<TableRows><TableRow><Height>0.25in</Height><TableCells><TableCell><ReportItems><Textbox Name=""{name}""><Value>=IIf(InScope(&quot;Names&quot;), &quot;N&quot;, &quot;-&quot;) &amp; IIf(InScope(&quot;Years&quot;), &quot;Y&quot;, &quot;-&quot;)</Value></Textbox></ReportItems></TableCell></TableCells></TableRow></TableRows>";

        private static string GroupedTable () => $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""{Rdl2005}"">
  <DataSources><DataSource Name=""DS1""><ConnectionProperties><DataProvider>SQLite</DataProvider><ConnectString>Data Source=none.db</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>DS1</DataSourceName><CommandText>/* Local Query */</CommandText></Query>
    <Fields><Field Name=""Name""><DataField>Name</DataField></Field><Field Name=""Year""><DataField>Year</DataField></Field><Field Name=""Amount""><DataField>Amount</DataField></Field></Fields>
  </DataSet></DataSets>
  <Body><Height>3in</Height><ReportItems>
    <Table Name=""T""><DataSetName>Data</DataSetName>
      <TableColumns><TableColumn><Width>2in</Width></TableColumn></TableColumns>
      <Header>{TableRow ("TableHeader")}</Header>
      <TableGroups>
        <TableGroup><Grouping Name=""Names""><GroupExpressions><GroupExpression>=Fields!Name.Value</GroupExpression></GroupExpressions></Grouping>
          <Header>{TableRow ("NameHeader")}</Header><Footer>{TableRow ("NameFooter")}</Footer></TableGroup>
        <TableGroup><Grouping Name=""Years""><GroupExpressions><GroupExpression>=Fields!Year.Value</GroupExpression></GroupExpressions></Grouping>
          <Header>{TableRow ("YearHeader")}</Header><Footer>{TableRow ("YearFooter")}</Footer></TableGroup>
      </TableGroups>
      <Details>{TableRow ("Detail")}</Details>
      <Footer>{TableRow ("TableFooter")}</Footer>
    </Table>
  </ReportItems></Body>
  <Width>6in</Width>
</Report>";

        [Test]
        public async Task InScope_InATable_FollowsTheSectionTheExpressionIsIn ()
        {
            var html = await Render (GroupedTable ());
            var rows = Cells (html).Select (r => r[0]).ToArray ();

            // Gadget has one year, Widget two. Each section reports which groups it is inside: the table's
            // own header and footer neither, a name header or footer only the name group, and the year
            // header, detail and year footer rows both.
            var expected = new[] {
                "--",
                "N-", "NY", "NY", "NY", "N-",                       // Gadget: one year (groups come out sorted)
                "N-", "NY", "NY", "NY", "NY", "NY", "NY", "N-",     // Widget: two years
                "--",
            };
            Assert.That (rows, Is.EqualTo (expected), string.Join (" ", rows));
        }

        [Test]
        public async Task InScope_SeesTheGroupsOfEveryRowSet ()
        {
            // Sibling row groups (Names rows, then Regions rows) are separate row sets: a cell is inside
            // the group of its own set and not inside the other set's.
            var rdl = Report ("=IIf(InScope(&quot;Names&quot;), &quot;n&quot;, &quot;-&quot;) &amp; IIf(InScope(&quot;Regions&quot;), &quot;r&quot;, &quot;-&quot;)")
                .Replace ("</Matrix>", @"<fyi:RowSet xmlns:fyi=""http://www.fyireporting.com/schemas"">
        <RowGroupings><RowGrouping><Width>1in</Width><DynamicRows>
          <Grouping Name=""Regions""><GroupExpressions><GroupExpression>=Fields!Year.Value</GroupExpression></GroupExpressions></Grouping>
          <ReportItems><Textbox Name=""RegionHead""><Value>=Fields!Year.Value</Value></Textbox></ReportItems>
        </DynamicRows></RowGrouping></RowGroupings>
        <MatrixRows><MatrixRow><Height>0.25in</Height><MatrixCells><MatrixCell><ReportItems><Textbox Name=""RegionCell""><Value>=IIf(InScope(&quot;Names&quot;), &quot;n&quot;, &quot;-&quot;) &amp; IIf(InScope(&quot;Regions&quot;), &quot;r&quot;, &quot;-&quot;)</Value></Textbox></ReportItems></MatrixCell></MatrixCells></MatrixRow></MatrixRows>
      </fyi:RowSet></Matrix>")
                .Replace ("<Report xmlns=\"" + Rdl2005 + "\">", "<Report xmlns=\"" + Rdl2005 + "\" xmlns:fyi=\"http://www.fyireporting.com/schemas\">");
            // the subtotals of the first set would add rows of their own; keep this about the two sets
            rdl = Regex.Replace (rdl, "<Subtotal>.*?</Subtotal>", "", RegexOptions.Singleline);

            var html = await Render (rdl);
            var rows = Cells (html);

            // Name rows: inside Names, not Regions. Region rows: inside Regions, not Names.
            Assert.That (rows.Where (r => r[0] == "Widget" || r[0] == "Gadget").SelectMany (r => r.Skip (1)).Distinct ().ToArray (), Is.EqualTo (new[] { "n-" }),
                string.Join (" || ", rows.Select (r => string.Join (" ", r))));
            Assert.That (rows.Where (r => r[0] == "Y2020" || r[0] == "Y2021").SelectMany (r => r.Skip (1)).Distinct ().ToArray (), Is.EqualTo (new[] { "-r" }),
                string.Join (" || ", rows.Select (r => string.Join (" ", r))));
        }
    }
}
