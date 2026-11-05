using System.Collections;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// A DataSet's filters apply to data given with SetData when the report gets its data, with
    /// the parameters RunGetData is given. They were applied as the data was given, before any
    /// parameter had a value, so a filter that compared a field with a parameter kept the wrong
    /// rows.
    /// </summary>
    [TestFixture]
    public class PushedDataFilterTests
    {
        [SetUp]
        public void Prepare() => RdlEngineConfig.RdlEngineConfigInit();

        private const string Rdl = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight>
  <Width>7.5in</Width>
  <DataSources><DataSource Name=""DS""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString></ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>DS</DataSourceName><CommandText>select</CommandText></Query>
    <Fields><Field Name=""G""><DataField>G</DataField><TypeName>System.String</TypeName></Field></Fields>
    <Filters><Filter>
      <FilterExpression>=Fields!G.Value</FilterExpression><Operator>Equal</Operator>
      <FilterValues><FilterValue>=Parameters!Pick.Value</FilterValue></FilterValues>
    </Filter></Filters>
  </DataSet></DataSets>
  <ReportParameters><ReportParameter Name=""Pick""><DataType>String</DataType><Nullable>true</Nullable><AllowBlank>true</AllowBlank><Prompt>Pick</Prompt></ReportParameter></ReportParameters>
  <Body><Height>1in</Height><ReportItems>
    <Table Name=""T"">
      <DataSetName>Data</DataSetName>
      <TableColumns><TableColumn><Width>2in</Width></TableColumn></TableColumns>
      <Details><TableRows><TableRow><Height>12pt</Height>
        <TableCells><TableCell><ReportItems><Textbox Name=""Row""><Value>=""ROW "" &amp; Fields!G.Value</Value></Textbox></ReportItems></TableCell></TableCells>
      </TableRow></TableRows></Details>
    </Table>
  </ReportItems></Body>
</Report>";

        private static async Task<string[]> Rows(Report report, string pick)
        {
            await report.RunGetData(new Hashtable { ["Pick"] = pick });
            var pages = await report.BuildPages();
            return pages[0].OfType<PageText>().Select(t => t.Text).Where(t => t.StartsWith("ROW")).ToArray();
        }

        [Test]
        public async Task FilterReadingAParameter_KeepsTheRowsItSelects()
        {
            var report = await new RDLParser(Rdl).Parse();
            var data = new DataTable();
            data.Columns.Add("G", typeof(string));
            foreach (var g in new[] { "A", "B", "A", "C" })
                data.Rows.Add(g);
            await report.DataSets["Data"].SetData(data);

            Assert.That(await Rows(report, "A"), Is.EqualTo(new[] { "ROW A", "ROW A" }));
        }
    }
}
