#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// RunningValue(expr, CountDistinct, scope) is the number of distinct non-null values of expr
    /// in the scope from its first row up to the current one.
    /// </summary>
    [TestFixture]
    public class RunningValueCountDistinctTests
    {
        private static string Rdl(string detail, string footer) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>400pt</Width>
  <DataSources><DataSource Name=""D""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>D</DataSourceName><CommandText>none</CommandText></Query>
    <Fields>
      <Field Name=""K""><DataField>K</DataField></Field>
      <Field Name=""C""><DataField>C</DataField></Field>
    </Fields>
  </DataSet></DataSets>
  <Body><Height>300pt</Height><ReportItems>
    <Table Name=""T"">
      <DataSetName>Data</DataSetName>
      <TableColumns><TableColumn><Width>300pt</Width></TableColumn></TableColumns>
      <TableGroups><TableGroup>
        <Grouping Name=""G""><GroupExpressions><GroupExpression>=Fields!K.Value</GroupExpression></GroupExpressions></Grouping>
        <Footer><TableRows><TableRow><Height>14pt</Height><TableCells><TableCell><ReportItems>
          <Textbox Name=""Foot""><Value>={footer}</Value></Textbox>
        </ReportItems></TableCell></TableCells></TableRow></TableRows></Footer>
      </TableGroup></TableGroups>
      <Details><TableRows><TableRow><Height>14pt</Height><TableCells><TableCell><ReportItems>
        <Textbox Name=""Line""><Value>={detail}</Value></Textbox>
      </ReportItems></TableCell></TableCells></TableRow></TableRows></Details>
    </Table>
  </ReportItems></Body>
  <PageHeight>800pt</PageHeight><PageWidth>500pt</PageWidth>
</Report>";

        private static async Task<string[]> Lines(string detail, string footer)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(Rdl(detail, footer)) { SkipDatabaseSchemaValidation = true }.Parse();
            var table = new DataTable();
            table.Columns.Add("K", typeof(string));
            table.Columns.Add("C", typeof(string));
            foreach (var (k, c) in new[] { ("a", "x"), ("a", "y"), ("a", "x"), ("b", "x"), ("b", "z"), ("c", null), ("c", "y") })
                table.Rows.Add(k, c);
            await report.DataSets["Data"].SetData(table);
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            return pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>()
                .OrderBy(t => t.Y).Select(t => t.Text).ToArray();
        }

        [Test]
        public async Task InADetailRow_OverTheWholeReport_ItCountsEachValueOnce()
        {
            var lines = await Lines("RunningValue(Fields!C.Value, CountDistinct, Nothing)", "\"F\"");
            Assert.That(lines, Is.EqualTo(new[] { "1", "2", "2", "F", "2", "3", "F", "3", "3", "F" }));
        }

        [Test]
        public async Task InADetailRow_WithinTheGroup_ItStartsAgainEachGroup()
        {
            var lines = await Lines("RunningValue(Fields!C.Value, CountDistinct, \"G\")", "\"F\"");
            Assert.That(lines, Is.EqualTo(new[] { "1", "2", "2", "F", "1", "2", "F", "0", "1", "F" }));
        }

        [Test]
        public async Task InAGroupFooter_WithinTheGroup_ItIsTheGroupsDistinctCount()
        {
            var lines = await Lines("RunningValue(Fields!C.Value, CountDistinct, \"G\")",
                "\"F \" &amp; CStr(RunningValue(Fields!C.Value, CountDistinct, \"G\")) &amp; \" \" &amp; CStr(CountDistinct(Fields!C.Value, \"G\"))");
            Assert.That(lines, Is.EqualTo(new[] { "1", "2", "2", "F 2 2", "1", "2", "F 2 2", "0", "1", "F 1 1" }));
        }

        // The details never read the group key: each footer counts the groups so far from the
        // report's start, once per group however many rows it has.
        [Test]
        public async Task InAGroupFooter_ReadOnlyThere_ItCountsTheGroupsSoFar()
        {
            var lines = await Lines("Fields!C.Value", "\"F \" &amp; CStr(RunningValue(Fields!K.Value, CountDistinct, Nothing))");
            Assert.That(lines, Is.EqualTo(new[] { "x", "y", "x", "F 1", "x", "z", "F 2", "", "y", "F 3" }));
        }

        [Test]
        public async Task AsAnOrdinaryAggregate_ItCountsTheScopesDistinctValues()
        {
            var lines = await Lines("Fields!C.Value",
                "\"F \" &amp; CStr(CountDistinct(Fields!C.Value, \"G\")) &amp; \" \" &amp; CStr(CountDistinct(Fields!C.Value, \"Data\"))");
            Assert.That(lines, Is.EqualTo(new[] { "x", "y", "x", "F 2 3", "x", "z", "F 2 3", "", "y", "F 1 3" }));
        }
    }
}
#endif
