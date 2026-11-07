using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ReportTests
{
    /// <summary>
    /// Issue #573: a Textbox below a Rectangle stopped being pushed down once the Rectangle's
    /// contents ran onto a following page. <c>Rectangle.RunPage</c> recorded the bottom the
    /// following siblings are placed from as <c>pr.Y + pr.H</c> whenever the contents stayed on
    /// the Rectangle's own page, but used the same design-height value even when they split
    /// across pages, so the follower froze at its design Top plus the page's top offset no
    /// matter how much the content grew. Fixed by <c>Rectangle.SplitAcrossPages</c> (see
    /// <see cref="RectangleAcrossPagesTests"/>), which this test exercises with the reporter's
    /// own repro shape: a Tablix (not a Table) inside the Rectangle, grouped so it normalizes to
    /// a List region, which is the shape that still froze after the Table-only fix.
    /// </summary>
    [TestFixture]
    public class RectangleSplitFollowerGrowthTests
    {
        [SetUp]
        public void SetUp() => RdlEngineConfig.RdlEngineConfigInit();

        private static string BuildRdl(string variant)
        {
            var inner = new StringBuilder();
            inner.Append(Textbox("Inner", "INNER", variant == "flat" ? "20mm" : "0mm", "10mm"));
            inner.Append(Tablix(variant == "flat" ? "27mm" : "7mm", "10mm"));

            var body = variant == "flat"
                ? inner.ToString()
                : "<Rectangle Name=\"R1\"><ReportItems>" + inner + "</ReportItems>" +
                  (variant == "keep" ? "<KeepTogether>true</KeepTogether>" : string.Empty) +
                  "<Top>20mm</Top><Left>10mm</Left><Height>20mm</Height><Width>80mm</Width><Style /></Rectangle>";

            body += Textbox("Follower", "FOLLOWER", "45mm", "10mm");

            return
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<Report xmlns=\"http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition\"" +
                " xmlns:rd=\"http://schemas.microsoft.com/SQLServer/reporting/reportdesigner\">" +
                "<DataSources><DataSource Name=\"Src\"><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>" +
                "<DataSets><DataSet Name=\"Data\"><Fields><Field Name=\"C1\"><DataField>C1</DataField></Field></Fields>" +
                "<Query><DataSourceName>Src</DataSourceName><CommandText /></Query></DataSet></DataSets>" +
                "<Body><ReportItems>" + body + "</ReportItems><Height>250mm</Height></Body>" +
                "<Width>190mm</Width>" +
                "<Page><PageHeight>297mm</PageHeight><PageWidth>210mm</PageWidth>" +
                "<LeftMargin>10mm</LeftMargin><RightMargin>10mm</RightMargin><TopMargin>10mm</TopMargin><BottomMargin>10mm</BottomMargin></Page>" +
                "</Report>";
        }

        private static string Textbox(string name, string value, string top, string left) =>
            $"<Textbox Name=\"{name}\"><CanGrow>true</CanGrow><Paragraphs><Paragraph><TextRuns><TextRun>" +
            $"<Value>{value}</Value><Style><FontFamily>Arial</FontFamily><FontSize>9pt</FontSize></Style>" +
            "</TextRun></TextRuns><Style /></Paragraph></Paragraphs>" +
            $"<Top>{top}</Top><Left>{left}</Left><Height>6mm</Height><Width>60mm</Width><Style /></Textbox>";

        // A single-row, single-column Tablix grouped on rows: Rdl2008Normalizer turns this into a
        // List region (not a Table), which is how the reporter's real Tablix was shaped.
        private static string Tablix(string top, string left) =>
            "<Tablix Name=\"T1\"><TablixBody>" +
            "<TablixColumns><TablixColumn><Width>60mm</Width></TablixColumn></TablixColumns>" +
            "<TablixRows><TablixRow><Height>6mm</Height><TablixCells><TablixCell><CellContents>" +
            "<Textbox Name=\"cell\"><CanGrow>true</CanGrow><Paragraphs><Paragraph><TextRuns><TextRun>" +
            "<Value>=Fields!C1.Value</Value><Style><FontFamily>Arial</FontFamily><FontSize>9pt</FontSize></Style>" +
            "</TextRun></TextRuns><Style /></Paragraph></Paragraphs><Height>6mm</Height><Width>60mm</Width><Style /></Textbox>" +
            "</CellContents></TablixCell></TablixCells></TablixRow></TablixRows></TablixBody>" +
            "<TablixColumnHierarchy><TablixMembers><TablixMember /></TablixMembers></TablixColumnHierarchy>" +
            "<TablixRowHierarchy><TablixMembers><TablixMember><Group Name=\"Details\" /></TablixMember></TablixMembers></TablixRowHierarchy>" +
            "<DataSetName>Data</DataSetName>" +
            $"<Top>{top}</Top><Left>{left}</Left><Height>6mm</Height><Width>60mm</Width><Style /></Tablix>";

        // The Y of the FOLLOWER textbox, on whichever page it lands on, in points.
        private static async Task<float> FollowerY(string variant, int rows)
        {
            var document = new XmlDocument();
            document.LoadXml(BuildRdl(variant));
            var parser = new RDLParser(document) { SkipDatabaseSchemaValidation = true };
            var report = await parser.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(8),
                string.Join(" | ", report.ErrorItems?.Cast<string>() ?? Enumerable.Empty<string>()));

            var table = new DataTable("Data");
            table.Columns.Add("C1", typeof(string));
            for (var r = 1; r <= rows; r++)
                table.Rows.Add($"RIGA{r:00}");

            await report.DataSets["Data"].SetData(table);
            await report.RunGetData(null);
            var pages = await report.BuildPages();

            var follower = pages.Cast<Page>()
                .SelectMany(p => p.Cast<PageItem>())
                .OfType<PageText>()
                .Single(t => t.Text == "FOLLOWER");
            return follower.Y;
        }

        // Once the Rectangle's Tablix overflows onto a second page (at and beyond 45 rows here),
        // the follower must keep moving down as more rows are added - not freeze at one value.
        [TestCase("keep")]
        [TestCase("nokeep")]
        public async Task Follower_keeps_moving_down_as_the_split_rectangle_grows(string variant)
        {
            float y45 = await FollowerY(variant, 45);
            float y50 = await FollowerY(variant, 50);
            float y60 = await FollowerY(variant, 60);

            Assert.That(y50, Is.GreaterThan(y45 + 10f), $"{variant}: follower froze between 45 and 50 rows");
            Assert.That(y60, Is.GreaterThan(y50 + 10f), $"{variant}: follower froze between 50 and 60 rows");
        }

        // The follower's growth should track a flat control with the same content at absolute
        // body coordinates (no Rectangle at all): the two must grow by the same amount as rows
        // are added, even though they land at different absolute Y (the "gap" above the follower
        // is computed from a different, closer item's design geometry in each shape).
        [TestCase("keep")]
        [TestCase("nokeep")]
        public async Task Follower_growth_matches_the_flat_control(string variant)
        {
            float flat45 = await FollowerY("flat", 45);
            float flat60 = await FollowerY("flat", 60);
            float rect45 = await FollowerY(variant, 45);
            float rect60 = await FollowerY(variant, 60);

            float flatGrowth = flat60 - flat45;
            float rectGrowth = rect60 - rect45;
            Assert.That(rectGrowth, Is.EqualTo(flatGrowth).Within(1f),
                $"{variant}: follower did not grow by the same amount as the flat control");
        }
    }
}
