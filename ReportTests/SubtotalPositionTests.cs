using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// A matrix Subtotal's Position: After (the default) puts the total column after the
    /// group's columns and the total row after its rows; Before puts them first. Position was
    /// read but never used, so totals always came last.
    /// </summary>
    [TestFixture]
    public class SubtotalPositionTests
    {
        private const string Rdl2005 = "http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition";

        private static string Report (string columnPosition, string rowPosition) => $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""{Rdl2005}"">
  <DataSources><DataSource Name=""DS1""><ConnectionProperties><DataProvider>SQLite</DataProvider><ConnectString>Data Source=none.db</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>DS1</DataSourceName><CommandText>/* Local Query */</CommandText></Query>
    <Fields><Field Name=""Name""><DataField>Name</DataField></Field><Field Name=""Year""><DataField>Year</DataField></Field><Field Name=""Amount""><DataField>Amount</DataField><TypeName>System.Double</TypeName></Field></Fields>
  </DataSet></DataSets>
  <Body><Height>3in</Height><ReportItems>
    <Matrix Name=""Pivot""><DataSetName>Data</DataSetName><Top>0in</Top><Left>0in</Left>
      <Corner><ReportItems><Textbox Name=""Corner""><Value>Corner</Value></Textbox></ReportItems></Corner>
      <ColumnGroupings><ColumnGrouping><Height>0.25in</Height><DynamicColumns>
        <Grouping Name=""Years""><GroupExpressions><GroupExpression>=Fields!Year.Value</GroupExpression></GroupExpressions></Grouping>
        <ReportItems><Textbox Name=""YearHead""><Value>=Fields!Year.Value</Value></Textbox></ReportItems>
        <Subtotal><ReportItems><Textbox Name=""YearTotal""><Value>AllYears</Value></Textbox></ReportItems>{columnPosition}</Subtotal>
      </DynamicColumns></ColumnGrouping></ColumnGroupings>
      <RowGroupings><RowGrouping><Width>1in</Width><DynamicRows>
        <Grouping Name=""Names""><GroupExpressions><GroupExpression>=Fields!Name.Value</GroupExpression></GroupExpressions></Grouping>
        <ReportItems><Textbox Name=""NameHead""><Value>=Fields!Name.Value</Value></Textbox></ReportItems>
        <Subtotal><ReportItems><Textbox Name=""NameTotal""><Value>AllNames</Value></Textbox></ReportItems>{rowPosition}</Subtotal>
      </DynamicRows></RowGrouping></RowGroupings>
      <MatrixRows><MatrixRow><Height>0.25in</Height><MatrixCells><MatrixCell><ReportItems><Textbox Name=""Cell""><Value>=Sum(Fields!Amount.Value)</Value></Textbox></ReportItems></MatrixCell></MatrixCells></MatrixRow></MatrixRows>
      <MatrixColumns><MatrixColumn><Width>0.8in</Width></MatrixColumn></MatrixColumns>
    </Matrix>
  </ReportItems></Body>
  <Width>6in</Width>
</Report>";

        private const string Before = "<Position>Before</Position>";

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

        private static async Task<string[][]> Rows (string rdl)
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
            return Regex.Matches (memory.GetText (), "<tr[^>]*>(.*?)</tr>", RegexOptions.Singleline)
                .Cast<Match> ()
                .Select (m => Regex.Matches (m.Groups[1].Value, "<t[dh][^>]*>(.*?)</t[dh]>", RegexOptions.Singleline)
                    .Cast<Match> ()
                    .Select (c => Regex.Replace (Regex.Replace (c.Groups[1].Value, "<[^>]+>", " "), @"\s+", " ").Replace ("&nbsp;", "").Trim ())
                    .ToArray ())
                .Where (r => r.Length > 0)
                .ToArray ();
        }

        [Test]
        public async Task ByDefault_TheTotalsComeLast ()
        {
            var rows = await Rows (Report ("", ""));

            Assert.That (rows[0], Is.EqualTo (new[] { "Corner", "Y2020", "Y2021", "AllYears" }));
            Assert.That (rows.Skip (1).Select (r => r[0]), Is.EqualTo (new[] { "Gadget", "Widget", "AllNames" }));
            Assert.That (rows[3], Is.EqualTo (new[] { "AllNames", "40", "20", "60" }));
        }

        [Test]
        public async Task Before_PutsTheTotalColumnFirst_InTheHeaderAndEveryRow ()
        {
            var rows = await Rows (Report (Before, ""));

            Assert.That (rows[0], Is.EqualTo (new[] { "Corner", "AllYears", "Y2020", "Y2021" }));
            Assert.That (rows.First (r => r[0] == "Widget"), Is.EqualTo (new[] { "Widget", "30", "10", "20" }));
            Assert.That (rows.First (r => r[0] == "AllNames"), Is.EqualTo (new[] { "AllNames", "60", "40", "20" }));
        }

        [Test]
        public async Task Before_PutsTheTotalRowFirst ()
        {
            var rows = await Rows (Report ("", Before));

            Assert.That (rows.Skip (1).Select (r => r[0]), Is.EqualTo (new[] { "AllNames", "Gadget", "Widget" }));
            Assert.That (rows[1], Is.EqualTo (new[] { "AllNames", "40", "20", "60" }));
            Assert.That (rows[3], Is.EqualTo (new[] { "Widget", "10", "20", "30" }));
        }
    }
}
