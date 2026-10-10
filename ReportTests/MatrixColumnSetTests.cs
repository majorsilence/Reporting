using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// RDL 2008+ allows sibling column groups in a pivot (for example Year columns followed by Region
    /// columns, both over the same rows). The engine's Matrix has one column hierarchy, so extra
    /// sets are carried as fyi:ColumnSet elements and laid out beside the first (issue #411).
    /// </summary>
    [TestFixture]
    public class MatrixColumnSetTests
    {
        private static string Cell (string name, string expression)
            => $@"<MatrixCell><ReportItems><Textbox Name=""{name}""><Value>{expression}</Value></Textbox></ReportItems></MatrixCell>";

        private static string Head (string name, string expression)
            => $@"<ReportItems><Textbox Name=""{name}""><Value>{expression}</Value></Textbox></ReportItems>";

        private static string ColumnGrouping (string group, string field, string headerName, string height = "0.25in")
            => $@"<ColumnGrouping><Height>{height}</Height><DynamicColumns><Grouping Name=""{group}""><GroupExpressions><GroupExpression>=Fields!{field}.Value</GroupExpression></GroupExpressions></Grouping>{Head (headerName, "=Fields!" + field + ".Value")}</DynamicColumns></ColumnGrouping>";

        private static string MatrixRows (string cellName)
            => $@"<MatrixRows><MatrixRow><Height>0.25in</Height><MatrixCells>{Cell (cellName, "=Sum(Fields!Amount.Value)")}</MatrixCells></MatrixRow></MatrixRows><MatrixColumns><MatrixColumn><Width>0.8in</Width></MatrixColumn></MatrixColumns>";

        private static string Report (string firstColumnGroupings, string setColumnGroupings) => $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"" xmlns:fyi=""http://www.fyireporting.com/schemas"">
  <DataSources><DataSource Name=""DS1""><ConnectionProperties><DataProvider>SQLite</DataProvider><ConnectString>Data Source=none.db</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>DS1</DataSourceName><CommandText>/* Local Query */</CommandText></Query>
    <Fields><Field Name=""Name""><DataField>Name</DataField></Field><Field Name=""Year""><DataField>Year</DataField></Field><Field Name=""Quarter""><DataField>Quarter</DataField></Field><Field Name=""Region""><DataField>Region</DataField></Field><Field Name=""Amount""><DataField>Amount</DataField></Field></Fields>
  </DataSet></DataSets>
  <Body><Height>3in</Height><ReportItems>
    <Matrix Name=""Pivot""><DataSetName>Data</DataSetName><Top>0in</Top><Left>0in</Left>
      <Corner>{Head ("Corner", "Name")}</Corner>
      <ColumnGroupings>{firstColumnGroupings}</ColumnGroupings>
      <RowGroupings><RowGrouping><Width>1in</Width><DynamicRows><Grouping Name=""ByName""><GroupExpressions><GroupExpression>=Fields!Name.Value</GroupExpression></GroupExpressions></Grouping>{Head ("RowHead", "=Fields!Name.Value")}</DynamicRows></RowGrouping></RowGroupings>
      {MatrixRows ("ByYear")}
      <fyi:ColumnSet>
        <ColumnGroupings>{setColumnGroupings}</ColumnGroupings>
        {MatrixRows ("ByRegion")}
      </fyi:ColumnSet>
    </Matrix>
  </ReportItems></Body>
  <Width>8in</Width>
</Report>";

        private static DataTable Data ()
        {
            var table = new DataTable ();
            table.Columns.Add ("Name", typeof (string));
            table.Columns.Add ("Year", typeof (string));
            table.Columns.Add ("Quarter", typeof (string));
            table.Columns.Add ("Region", typeof (string));
            table.Columns.Add ("Amount", typeof (double));
            table.Rows.Add ("Widget", "Y2020", "Q1", "North", 10.0);
            table.Rows.Add ("Widget", "Y2021", "Q2", "South", 20.0);
            table.Rows.Add ("Gadget", "Y2020", "Q1", "South", 30.0);
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

        /// <summary>The visible text of each table row, in order.</summary>
        private static string[] Rows (string html)
            => Regex.Matches (html, "<tr[^>]*>(.*?)</tr>", RegexOptions.Singleline)
                .Cast<Match> ()
                .Select (m => Regex.Replace (Regex.Replace (m.Groups[1].Value, "<[^>]+>", " "), @"\s+", " ").Trim ())
                .Where (r => r.Length > 0)
                .ToArray ();

        [Test]
        public async Task SecondColumnSet_SitsBesideTheFirst ()
        {
            var html = await Render (Report (ColumnGrouping ("Years", "Year", "YearHead"), ColumnGrouping ("Regions", "Region", "RegionHead")));
            var rows = Rows (html);

            // header row: corner, the years, then the regions -- the row headers appear once
            var header = rows.First (r => r.Contains ("Y2020"));
            var order = new[] { "Y2020", "Y2021", "North", "South" }.Select (t => header.IndexOf (t)).ToArray ();
            Assert.That (order.All (i => i >= 0), Is.True, header);
            Assert.That (order, Is.Ordered, "years first, then regions: " + header);
            Assert.That (Regex.Matches (html, "Widget").Count, Is.EqualTo (1), "the row headers are not repeated for the second set");

            // data row for Widget: Y2020=10, Y2021=20 | North=10, South=20
            var widget = rows.First (r => r.StartsWith ("Widget"));
            Assert.That (Regex.Matches (widget, @"\d+(\.\d+)?").Select (m => double.Parse (m.Value)).ToArray (),
                Is.EqualTo (new[] { 10.0, 20.0, 10.0, 20.0 }), widget);
            var gadget = rows.First (r => r.StartsWith ("Gadget"));
            Assert.That (Regex.Matches (gadget, @"\d+(\.\d+)?").Select (m => double.Parse (m.Value)).ToArray (),
                Is.EqualTo (new[] { 30.0, 0.0, 0.0, 30.0 }), gadget);
        }

        [Test]
        public async Task SetsWithDifferentHeaderDepth_AlignAtTheData ()
        {
            // The first set has two column levels (Year, Quarter); the second only one (Region).
            var first = ColumnGrouping ("Years", "Year", "YearHead") + ColumnGrouping ("Quarters", "Quarter", "QuarterHead");
            var html = await Render (Report (first, ColumnGrouping ("Regions", "Region", "RegionHead")));
            var rows = Rows (html);

            var quarterRow = rows.First (r => r.Contains ("Q1"));
            Assert.That (quarterRow, Does.Contain ("North"), "the second set's single header sits on the lower header row, beside the quarters");
            Assert.That (rows.First (r => r.Contains ("Y2020")), Does.Not.Contain ("North"));
            Assert.That (rows.First (r => r.StartsWith ("Widget")), Does.Contain ("10"));
        }

        [Test]
        public async Task AMatrixWithoutExtraSets_RendersAsBefore ()
        {
            var rdl = Report (ColumnGrouping ("Years", "Year", "YearHead"), ColumnGrouping ("Regions", "Region", "RegionHead"));
            rdl = Regex.Replace (rdl, "<fyi:ColumnSet>.*?</fyi:ColumnSet>", "", RegexOptions.Singleline);

            var html = await Render (rdl);

            Assert.That (html, Does.Contain ("Y2020"));
            Assert.That (html, Does.Not.Contain ("North"));
        }
    }
}
