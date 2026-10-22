using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// An aggregate's quoted scope may name a grouping, as RDL allows: in a city's row,
    /// Sum(Fields!Amount.Value, "Country") is the total of the country the city is in. Only a
    /// DataSet name was taken; a grouping was rejected as "Scope 'Country' does not reference a
    /// known DataSet", so a city's share of its country -- a Crystal Reports "Percentage of"
    /// an outer group -- had no expression.
    /// </summary>
    [TestFixture]
    public class GroupScopeAggregateTests
    {
        private const string Rdl2005 = "http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition";

        private static string Report (string share) => $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""{Rdl2005}"">
  <DataSources><DataSource Name=""DS1""><ConnectionProperties><DataProvider>SQLite</DataProvider><ConnectString>Data Source=none.db</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>DS1</DataSourceName><CommandText>/* Local Query */</CommandText></Query>
    <Fields><Field Name=""Country""><DataField>Country</DataField></Field><Field Name=""City""><DataField>City</DataField></Field><Field Name=""Amount""><DataField>Amount</DataField><TypeName>System.Double</TypeName></Field></Fields>
  </DataSet></DataSets>
  <Body><Height>3in</Height><ReportItems>
    <Table Name=""Cities""><DataSetName>Data</DataSetName><Top>0in</Top><Left>0in</Left>
      <TableColumns><TableColumn><Width>1in</Width></TableColumn><TableColumn><Width>1in</Width></TableColumn><TableColumn><Width>1in</Width></TableColumn></TableColumns>
      <TableGroups>
        <TableGroup>
          <Grouping Name=""Country""><GroupExpressions><GroupExpression>=Fields!Country.Value</GroupExpression></GroupExpressions></Grouping>
        </TableGroup>
        <TableGroup>
          <Grouping Name=""City""><GroupExpressions><GroupExpression>=Fields!City.Value</GroupExpression></GroupExpressions></Grouping>
          <Footer><TableRows><TableRow><Height>0.25in</Height><TableCells>
            <TableCell><ReportItems><Textbox Name=""CityName""><Value>=Fields!City.Value</Value></Textbox></ReportItems></TableCell>
            <TableCell><ReportItems><Textbox Name=""Share""><Value>{share}</Value></Textbox></ReportItems></TableCell>
            <TableCell><ReportItems><Textbox Name=""OfAll""><Value>=Format(Sum(Fields!Amount.Value) / Sum(Fields!Amount.Value, ""Data"") * 100, ""0.0"")</Value></Textbox></ReportItems></TableCell>
          </TableCells></TableRow></TableRows></Footer>
        </TableGroup>
      </TableGroups>
    </Table>
  </ReportItems></Body>
  <Width>6in</Width>
</Report>";

        private static DataTable Data ()
        {
            var table = new DataTable ();
            table.Columns.Add ("Country", typeof (string));
            table.Columns.Add ("City", typeof (string));
            table.Columns.Add ("Amount", typeof (double));
            table.Rows.Add ("A", "Alpha", 4.0);
            table.Rows.Add ("A", "Alpha", 6.0);
            table.Rows.Add ("A", "Beta", 30.0);
            table.Rows.Add ("B", "Gamma", 40.0);
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
                .Select (m => Regex.Matches (m.Groups[1].Value, "<td[^>]*>(.*?)</td>", RegexOptions.Singleline)
                    .Cast<Match> ()
                    .Select (c => Regex.Replace (Regex.Replace (c.Groups[1].Value, "<[^>]+>", " "), @"\s+", " ").Trim ())
                    .ToArray ())
                .Where (r => r.Length == 3)
                .ToArray ();
        }

        [Test]
        public async Task AGroupingScope_AggregatesOverThatGroupsInstance ()
        {
            var rows = await Rows (Report ("=Format(Sum(Fields!Amount.Value) / Sum(Fields!Amount.Value, \"Country\") * 100, \"0.0\")"));

            // A is 40 (Alpha 10, Beta 30) and B is 40 (Gamma); the whole set is 80.
            Assert.That (rows, Is.EqualTo (new[]
            {
                new[] { "Alpha", "25.0", "12.5" },
                new[] { "Beta", "75.0", "37.5" },
                new[] { "Gamma", "100.0", "50.0" },
            }));
        }

        [Test]
        public void AScopeThatNamesNothing_IsStillAnError ()
        {
            Assert.ThrowsAsync<AssertionException> (async () =>
                await Rows (Report ("=Sum(Fields!Amount.Value, \"NoSuchScope\")")));
        }
    }
}
