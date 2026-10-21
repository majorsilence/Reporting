using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// RowNumber is the row's position in its scope, however many times, or how few, an
    /// expression evaluates it. It was a running count of evaluations, so a RowNumber behind
    /// a condition fell behind: in Switch(RowNumber(Nothing) Mod 2 = 0, "even",
    /// RowNumber(Nothing) Mod 2 = 1, "odd") the second is evaluated only where the first is
    /// false, and every fourth row matched neither.
    /// </summary>
    [TestFixture]
    public class RowNumberTests
    {
        private const string Rdl2005 = "http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition";

        private static string Report (string sortDirection) => $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""{Rdl2005}"">
  <DataSources><DataSource Name=""DS1""><ConnectionProperties><DataProvider>SQLite</DataProvider><ConnectString>Data Source=none.db</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>DS1</DataSourceName><CommandText>/* Local Query */</CommandText></Query>
    <Fields><Field Name=""Name""><DataField>Name</DataField></Field></Fields>
  </DataSet></DataSets>
  <Body><Height>3in</Height><ReportItems>
    <Table Name=""T""><DataSetName>Data</DataSetName>
      <TableColumns><TableColumn><Width>1in</Width></TableColumn><TableColumn><Width>1in</Width></TableColumn><TableColumn><Width>1in</Width></TableColumn></TableColumns>
      <Details>
        <TableRows><TableRow><Height>0.25in</Height><TableCells>
          <TableCell><ReportItems><Textbox Name=""N""><Value>=Fields!Name.Value</Value></Textbox></ReportItems></TableCell>
          <TableCell><ReportItems><Textbox Name=""R""><Value>=RowNumber(Nothing)</Value></Textbox></ReportItems></TableCell>
          <TableCell><ReportItems><Textbox Name=""P""><Value>=Switch(RowNumber(Nothing) Mod 2 = 0, ""even"", RowNumber(Nothing) Mod 2 = 1, ""odd"")</Value></Textbox></ReportItems></TableCell>
        </TableCells></TableRow></TableRows>
        <Sorting><SortBy><SortExpression>=Fields!Name.Value</SortExpression><Direction>{sortDirection}</Direction></SortBy></Sorting>
      </Details>
    </Table>
  </ReportItems></Body>
  <Width>6in</Width>
</Report>";

        private static async Task<string[][]> Rows (string sortDirection)
        {
            RdlEngineConfig.RdlEngineConfigInit ();
            var parser = new RDLParser (Report (sortDirection)) { SkipDatabaseSchemaValidation = true };
            using var report = await parser.Parse ();
            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4),
                string.Join (" | ", report.ErrorItems?.Cast<string> () ?? Enumerable.Empty<string> ()));
            var table = new DataTable ();
            table.Columns.Add ("Name", typeof (string));
            foreach (var n in new[] { "c", "a", "f", "b", "e", "d" })
                table.Rows.Add (n);
            await report.DataSets["Data"].SetData (table);
            await report.RunGetData (null);
            using var memory = new MemoryStreamGen ();
            await report.RunRender (memory, OutputPresentationType.HTML);
            return Regex.Matches (memory.GetText (), "<tr[^>]*>(.*?)</tr>", RegexOptions.Singleline)
                .Cast<Match> ()
                .Select (m => Regex.Matches (m.Groups[1].Value, "<t[dh][^>]*>(.*?)</t[dh]>", RegexOptions.Singleline)
                    .Cast<Match> ()
                    .Select (c => Regex.Replace (Regex.Replace (c.Groups[1].Value, "<[^>]+>", " "), @"\s+", " ").Trim ())
                    .ToArray ())
                .Where (r => r.Length == 3)
                .ToArray ();
        }

        [TestCase ("Ascending", new[] { "a", "b", "c", "d", "e", "f" })]
        [TestCase ("Descending", new[] { "f", "e", "d", "c", "b", "a" })]
        public async Task RowNumber_IsTheRowsPosition_InPrintedOrder (string direction, string[] names)
        {
            var rows = await Rows (direction);

            Assert.That (rows.Select (r => r[0]), Is.EqualTo (names));
            Assert.That (rows.Select (r => r[1]), Is.EqualTo (new[] { "1", "2", "3", "4", "5", "6" }));
        }

        [Test]
        public async Task RowNumber_BehindACondition_StillCountsEveryRow ()
        {
            var rows = await Rows ("Ascending");

            Assert.That (rows.Select (r => r[2]), Is.EqualTo (new[] { "odd", "even", "odd", "even", "odd", "even" }));
        }
    }
}
