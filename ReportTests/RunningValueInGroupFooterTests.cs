#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// A running value read in a group's footer, directly or through a calculated field, is its
    /// value at the group's last row.
    /// </summary>
    [TestFixture]
    public class RunningValueInGroupFooterTests
    {
        private static string Rdl(string scope, string function = "Sum", string detail = "=\"D \" &amp; CStr(Fields!RT.Value)") => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>400pt</Width>
  <DataSources><DataSource Name=""D""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>D</DataSourceName><CommandText>none</CommandText></Query>
    <Fields>
      <Field Name=""K""><DataField>K</DataField></Field>
      <Field Name=""V""><DataField>V</DataField><TypeName>Decimal</TypeName></Field>
      <Field Name=""RT""><Value>=RunningValue(Fields!V.Value, {function}, {scope})</Value></Field>
    </Fields>
  </DataSet></DataSets>
  <Body><Height>300pt</Height><ReportItems>
    <Table Name=""T"">
      <DataSetName>Data</DataSetName>
      <TableColumns><TableColumn><Width>300pt</Width></TableColumn></TableColumns>
      <TableGroups><TableGroup>
        <Grouping Name=""G""><GroupExpressions><GroupExpression>=Fields!K.Value</GroupExpression></GroupExpressions></Grouping>
        <Footer><TableRows><TableRow><Height>14pt</Height><TableCells><TableCell><ReportItems>
          <Textbox Name=""Foot""><Value>=""F "" &amp; CStr(Fields!RT.Value) &amp; "" "" &amp; CStr(Sum(Fields!V.Value) - Fields!RT.Value)</Value></Textbox>
        </ReportItems></TableCell></TableCells></TableRow></TableRows></Footer>
      </TableGroup></TableGroups>
      <Details><TableRows><TableRow><Height>14pt</Height><TableCells><TableCell><ReportItems>
        <Textbox Name=""Line""><Value>{detail}</Value></Textbox>
      </ReportItems></TableCell></TableCells></TableRow></TableRows></Details>
    </Table>
  </ReportItems></Body>
  <PageHeight>800pt</PageHeight><PageWidth>500pt</PageWidth>
</Report>";

        private static async Task<string[]> Lines(string scope, string function = "Sum", string detail = null)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(detail == null ? Rdl(scope, function) : Rdl(scope, function, detail)) { SkipDatabaseSchemaValidation = true }.Parse();
            var table = new DataTable();
            table.Columns.Add("K", typeof(string));
            table.Columns.Add("V", typeof(decimal));
            foreach (var (k, v) in new[] { ("a", 1m), ("a", 2m), ("b", 10m), ("b", 20m), ("b", 30m) })
                table.Rows.Add(k, v);
            await report.DataSets["Data"].SetData(table);
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            return pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>()
                .OrderBy(t => t.Y).Select(t => t.Text).ToArray();
        }

        [Test]
        public async Task WithinTheGroup_TheFooterHasTheGroupsTotal()
        {
            var lines = await Lines("\"G\"");
            Assert.That(lines, Is.EqualTo(new[] { "D 1", "D 3", "F 3 0", "D 10", "D 30", "D 60", "F 60 0" }));
        }

        [Test]
        public async Task OverTheWholeReport_TheFooterHasTheTotalSoFar()
        {
            var lines = await Lines("Nothing");
            Assert.That(lines, Is.EqualTo(new[] { "D 1", "D 3", "F 3 0", "D 13", "D 33", "D 63", "F 63 -3" }));
        }

        // Detail rows that never read the value: the footer works it out from the group's start.
        [Test]
        public async Task ReadOnlyInTheFooter_ItIsTheGroupsTotal()
        {
            var lines = await Lines("\"G\"", detail: "=Fields!K.Value");
            Assert.That(lines, Is.EqualTo(new[] { "a", "a", "F 3 0", "b", "b", "b", "F 60 0" }));
        }

        [Test]
        public async Task ARunningCount_IsTheGroupsCountInItsFooter()
        {
            var lines = await Lines("\"G\"", "Count");
            Assert.That(lines, Is.EqualTo(new[] { "D 1", "D 2", "F 2 1", "D 1", "D 2", "D 3", "F 3 57" }));
        }
    }
}
#endif
