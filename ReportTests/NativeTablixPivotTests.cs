#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// A Tablix with column groups, run by the engine itself (TablixPivot). The shapes the
    /// normaliser rewrote into a Matrix lay out exactly as before; the ones it refused or got
    /// wrong now render. The data has 3 regions, 4 cities and 36 rows (see Data()).
    /// </summary>
    public partial class NativeTablixTests
    {
        private static string H(string name, string value, string size = "1in")
            => $"<TablixHeader><Size>{size}</Size><CellContents>{Tb(name, value)}</CellContents></TablixHeader>";

        private static string G(string name, string field)
            => $@"<Group Name=""{name}""><GroupExpressions><GroupExpression>=Fields!{field}.Value</GroupExpression></GroupExpressions></Group>";

        private static string ByRegionRows => $"<TablixMember>{G("ByRegion", "Region")}{H("RH", "=Fields!Region.Value")}</TablixMember>";

        private static string Corner(string text)
            => $"<TablixCorner><TablixCornerRows><TablixCornerRow><TablixCornerCell><CellContents>{Tb("Corner", text)}</CellContents></TablixCornerCell></TablixCornerRow></TablixCornerRows></TablixCorner>";

        /// <summary>The page texts in order, without the page footer.</summary>
        private static string Texts(string[] items)
            => string.Join(",", items.Where(i => i.Contains(" PageText ") && !i.Contains(" of ") && !i.EndsWith(" ")).Select(i => i.Substring(i.LastIndexOf(' ') + 1)));

        private static async Task<string[]> RenderNative(string rdl)
        {
            var (items, report) = await Render(rdl, native: true);
            using (report)
                Assert.That(report.ReportDefinition.Body.ReportItems.Items.Single(), Is.InstanceOf<TablixPivot>());
            return items;
        }

        private static string Sum(string region, string city)
        {
            string filter = string.Join(" AND ", new[] { region == null ? null : $"Region='{region}'", city == null ? null : $"City='{city}'" }.Where(f => f != null));
            return Data().Select(filter).Sum(r => (double)r["Amount"]).ToString(CultureInfo.CurrentCulture);
        }

        private static readonly string[] Regions = { "East", "North", "South" };
        private static readonly string[] Cities = { "City0", "City1", "City2", "City3" };

        [Test]
        public async Task APivotWithTotals_LaysOutAsTheMatrixDid()
        {
            string rows = Row(Cell("C", "=Sum(Fields!Amount.Value)"), Cell("CT", "=Sum(Fields!Amount.Value)"))
                + Row(Cell("RT", "=Sum(Fields!Amount.Value)"), Cell("GT", "=Sum(Fields!Amount.Value)"));
            string columns = $"<TablixMember>{G("ByCity", "City")}{H("CH", "=Fields!City.Value", "0.25in")}</TablixMember><TablixMember>{H("CHT", "Total", "0.25in")}</TablixMember>";
            string members = ByRegionRows + $"<TablixMember>{H("RHT", "Total")}</TablixMember>";
            var items = await AssertSameBothWays(Report(Tablix("T", 2, rows, members, columns, Corner("Region"))), pivot: true);

            Assert.That(Texts(items), Does.StartWith($"Region,City0,City1,City2,City3,Total,East,{Sum("East", "City0")},"));
            Assert.That(Texts(items), Does.EndWith($",Total,{string.Join(",", Cities.Select(c => Sum(null, c)))},{Sum(null, null)}"));
        }

        [Test]
        public async Task NestedColumnGroups_AndTwoMeasuresPerGroup_LayOutAsTheMatrixDid()
        {
            string rows = Row(Cell("S", "=Sum(Fields!Amount.Value)"), Cell("N", "=Count(Fields!Name.Value)"));
            string columns = $@"<TablixMember>{G("ByCity", "City")}{H("CH", "=Fields!City.Value", "0.25in")}<TablixMembers>
                <TablixMember>{H("M1", "Sum", "0.25in")}</TablixMember><TablixMember>{H("M2", "Count", "0.25in")}</TablixMember></TablixMembers></TablixMember>";
            // The Matrix drew each city's header one column wide; it spans the city's two columns.
            var items = await AssertSameBothWays(Report(Tablix("T", 2, rows, ByRegionRows, columns)), pivot: true,
                expectedChange: i => System.Text.RegularExpressions.Regex.IsMatch(i, " 90x18 City[0-3]$") ? i.Replace(" 90x18 ", " 180x18 ") : i);

            Assert.That(Texts(items), Does.StartWith("City0,City1,City2,City3,Sum,Count,Sum,Count"));
        }

        [Test]
        public async Task ManyRows_RepeatTheColumnHeadersOnEachPage()
        {
            string rows = Row(Cell("C", "=Sum(Fields!Amount.Value)"));
            string columns = $"<TablixMember>{G("ByCity", "City")}{H("CH", "=Fields!City.Value", "0.25in")}</TablixMember>";
            string members = $"<TablixMember>{G("ByName", "Name")}{H("RH", "=Fields!Name.Value")}</TablixMember>";
            var items = await AssertSameBothWays(Report(Tablix("T", 1, rows, members, columns)), pivot: true);

            int pages = items.Select(i => i.Split(' ')[0]).Distinct().Count();
            Assert.That(pages, Is.GreaterThan(1));
            Assert.That(items.Count(i => i.EndsWith(" City3")), Is.EqualTo(pages), "the city headers on every page");
        }

        [Test]
        public async Task StaticColumnsBesideAColumnGroup_ATableWithAPivotInIt()
        {
            // Region | a column per city | Total: the normaliser refused this shape.
            string rows = Row(Cell("H1", "Region"), Cell("H2", "=Fields!City.Value"), Cell("H3", "Total"))
                + Row(Cell("D1", "=Fields!Region.Value"), Cell("D2", "=Sum(Fields!Amount.Value)"), Cell("D3", "=Sum(Fields!Amount.Value)"));
            string columns = $"<TablixMember /><TablixMember>{G("ByCity", "City")}</TablixMember><TablixMember />";
            string members = $"<TablixMember><KeepWithGroup>After</KeepWithGroup></TablixMember><TablixMember>{G("ByRegion", "Region")}</TablixMember>";
            var items = await RenderNative(Report(Tablix("T", 3, rows, members, columns)));

            string expected = "Region,City0,City1,City2,City3,Total," + string.Join(",",
                Regions.Select(r => r + "," + string.Join(",", Cities.Select(c => Sum(r, c))) + "," + Sum(r, null)));
            Assert.That(Texts(items), Is.EqualTo(expected));
        }

        [Test]
        public async Task AColumnGroupOverDetailRows()
        {
            // A row per record, its amount under its own city: the normaliser refused this shape.
            string rows = Row(Cell("D1", "=Fields!Name.Value"), Cell("D2", "=Sum(Fields!Amount.Value)"));
            string columns = $"<TablixMember>{H("NH", "Name", "0.25in")}</TablixMember><TablixMember>{G("ByCity", "City")}{H("CH", "=Fields!City.Value", "0.25in")}</TablixMember>";
            string members = @"<TablixMember><Group Name=""Details"" /></TablixMember>";
            var items = await RenderNative(Report(Tablix("T", 2, rows, members, columns)));

            var row0 = Data().Rows[0];	// Name00, in City0
            var row1 = Data().Rows[1];	// Name01, in City1
            Assert.That(Texts(items), Does.StartWith($"Name,City0,City1,City2,City3,Name00,{row0["Amount"]},Name01,{row1["Amount"]},"));
            Assert.That(items.Count(i => i.Contains(" PageText ") && i.EndsWith(" ")), Is.EqualTo(36 * 3), "each record is blank under the other cities");
        }

        [Test]
        public async Task AHeaderRowAboveTheRowGroup_StaysOnTop()
        {
            // The normaliser moved this row below the groups, as a total.
            string rows = Row(Cell("YH", "=Fields!City.Value")) + Row(Cell("V", "=Sum(Fields!Amount.Value)"));
            string columns = $"<TablixMember>{G("ByCity", "City")}</TablixMember>";
            string members = $"<TablixMember>{H("RH0", "Region")}</TablixMember>" + ByRegionRows;
            var items = await RenderNative(Report(Tablix("T", 1, rows, members, columns)));

            Assert.That(Texts(items), Is.EqualTo("Region,City0,City1,City2,City3," + string.Join(",",
                Regions.Select(r => r + "," + string.Join(",", Cities.Select(c => Sum(r, c)))))));
        }

        [Test]
        public async Task GroupScopedAggregates_AndInScope_SeeTheirOwnGroup()
        {
            // Each cell's share of its city; InScope tells the cells from the total column.
            string share = "=Format(Sum(Fields!Amount.Value) / Sum(Fields!Amount.Value, \"ByCity\"), \"0.00\") &amp; IIf(InScope(\"ByCity\"), \"\", \"T\")";
            string rows = Row(Cell("P", share), Cell("PT", share.Replace("\"ByCity\"), \"0.00\")", "\"ByRegion\"), \"0.00\")")));
            string columns = $"<TablixMember>{G("ByCity", "City")}{H("CH", "=Fields!City.Value", "0.25in")}</TablixMember><TablixMember>{H("CHT", "Total", "0.25in")}</TablixMember>";
            var texts = Texts(await RenderNative(Report(Tablix("T", 2, rows, ByRegionRows, columns)))).Split(',');

            var data = Data();
            double Amount(string filter) => data.Select(filter).Sum(r => (double)r["Amount"]);
            Assert.That(texts, Does.Contain((Amount("Region='East' AND City='City0'") / Amount("City='City0'")).ToString("0.00")));
            Assert.That(texts.Where(t => t.EndsWith("T")), Is.EqualTo(new[] { "1.00T", "1.00T", "1.00T" }), "the total column: out of the city group, all of its region");
        }

        [Test]
        public async Task HiddenColumnMembers_LeaveTheirColumnsOut()
        {
            string rows = Row(Cell("C", "=Sum(Fields!Amount.Value)"), Cell("CT", "=Sum(Fields!Amount.Value)"));
            string columns = $"<TablixMember>{G("ByCity", "City")}{H("CH", "=Fields!City.Value", "0.25in")}<Visibility><Hidden>=Fields!City.Value = \"City2\"</Hidden></Visibility></TablixMember>"
                + $"<TablixMember>{H("CHT", "Total", "0.25in")}<Visibility><Hidden>true</Hidden></Visibility></TablixMember>";
            var items = await RenderNative(Report(Tablix("T", 2, rows, ByRegionRows, columns)));

            Assert.That(Texts(items), Does.StartWith($"City0,City1,City3,East,{Sum("East", "City0")},{Sum("East", "City1")},{Sum("East", "City3")},North"));
        }

        [Test]
        public async Task ATablixTooBrokenToLayOut_IsStillRefusedAndDropped([Values(false, true)] bool native)
        {
            string rows = Row(Cell("A1", "=Fields!Name.Value"), Cell("A2", "a")) + Row(Cell("B1", "=Fields!City.Value"), Cell("B2", "b"));
            string rdl = Report(Tablix("T", 2, rows, @"<TablixMember><Group Name=""Details"" /></TablixMember>"));   // two rows, one member
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true, NativeTablix = native }.Parse();
            string errors = string.Join(" | ", report.ErrorItems?.Cast<object>() ?? Enumerable.Empty<object>());
            Assert.That(errors, Does.Contain("has 2 row(s) but 1 row hierarchy leaf member(s)"), "the normaliser says why");
            Assert.That(errors, Does.Contain("Unknown ReportItems element 'Tablix'"));
        }

        [Test]
        public async Task AShapeTheNormaliserRefuses_RunsNativelyWhenItsHierarchiesMatchItsBody()
        {
            // Two row groups side by side: neither a Table nor a Matrix.
            string rows = Row(Cell("A", "=Fields!Region.Value"), Cell("AS", "=Sum(Fields!Amount.Value)"))
                + Row(Cell("B", "=Fields!City.Value"), Cell("BS", "=Sum(Fields!Amount.Value)"));
            string members = $"<TablixMember>{G("ByRegion", "Region")}</TablixMember><TablixMember>{G("ByCity", "City")}</TablixMember>";
            var (normalised, r1) = await Render(Report(Tablix("T", 2, rows, members)), native: false);
            r1.Dispose();
            var items = await RenderNative(Report(Tablix("T", 2, rows, members)));

            Assert.That(Texts(items), Is.EqualTo(string.Join(",", Regions.Select(r => r + "," + Sum(r, null)))
                + "," + string.Join(",", Cities.Select(c => c + "," + Sum(null, c)))));
        }
    }
}
#endif
