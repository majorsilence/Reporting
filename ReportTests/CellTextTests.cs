#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// The text of a table cell: the value, its Format and Language, nulls, NaN. Plain fields and constant formats
    /// take a path with no Tasks; calculated values and non-constant formats take the async one. Both give this text.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class CellTextTests
    {
        private const string Rdl = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"" xmlns:rd=""http://schemas.microsoft.com/SQLServer/reporting/reportdesigner"">
  <Width>6in</Width><PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight>
  <DataSources><DataSource Name=""DS1""><ConnectionProperties><DataProvider>xxx</DataProvider><ConnectString>none</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets>
    <DataSet Name=""Data"">
      <Query><DataSourceName>DS1</DataSourceName><CommandText></CommandText></Query>
      <Fields>
        <Field Name=""Amount""><DataField>Amount</DataField><rd:TypeName>System.Double</rd:TypeName></Field>
        <Field Name=""Name""><DataField>Name</DataField><rd:TypeName>System.String</rd:TypeName></Field>
        <Field Name=""When""><DataField>When</DataField><rd:TypeName>System.DateTime</rd:TypeName></Field>
      </Fields>
      <fyi:Rows xmlns:fyi=""http://www.fyireporting.com/schemas""><Row><Amount>1234.5</Amount><Name>first</Name><When>2024-05-06</When></Row><Row><Amount>0.125</Amount><Name></Name><When>2024-12-31</When></Row></fyi:Rows>
    </DataSet>
  </DataSets>
  <Body>
    <Height>1in</Height>
    <ReportItems>
      <Table Name=""Table1"">
        <DataSetName>Data</DataSetName>
        <Top>0in</Top><Left>0in</Left><Width>6in</Width><Height>0.2in</Height>
        <TableColumns><TableColumn><Width>1.5in</Width></TableColumn><TableColumn><Width>1.5in</Width></TableColumn><TableColumn><Width>1.5in</Width></TableColumn><TableColumn><Width>1.5in</Width></TableColumn></TableColumns>
        <Details><TableRows><TableRow><Height>0.2in</Height><TableCells>
          <TableCell><ReportItems><Textbox Name=""A""><Value>=Fields!Amount.Value</Value><Style><Format>#,##0.00</Format></Style></Textbox></ReportItems></TableCell>
          <TableCell><ReportItems><Textbox Name=""B""><Value>=Fields!Name.Value</Value></Textbox></ReportItems></TableCell>
          <TableCell><ReportItems><Textbox Name=""C""><Value>=Fields!When.Value</Value><Style><Format>yyyy-MM-dd</Format></Style></Textbox></ReportItems></TableCell>
          <TableCell><ReportItems><Textbox Name=""D""><Value>=Fields!Amount.Value * 2</Value><Style><Format>0.0</Format></Style></Textbox></ReportItems></TableCell>
          </TableCells></TableRow></TableRows></Details>
      </Table>
    </ReportItems>
  </Body>
</Report>";

        [OneTimeSetUp]
        public void Init() => RdlEngineConfig.RdlEngineConfigInit();

        private static async Task<string> Html(string cultureName)
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo(cultureName);
                using var report = await new RDLParser(Rdl).Parse();
                await report.RunGetData();
                using var ms = new MemoryStreamGen();
                await report.RunRender(ms, OutputPresentationType.HTML);
                return ms.GetText();
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Test]
        public async Task FormattedValues_AreFormatted_InvariantCulture()
        {
            string html = await Html("en-US");
            Assert.That(html, Does.Contain("1,234.50"));
            Assert.That(html, Does.Contain("0.13"));      // 0.125 with #,##0.00
            Assert.That(html, Does.Contain("2024-05-06"));
            Assert.That(html, Does.Contain("2024-12-31"));
            Assert.That(html, Does.Contain("2469.0"));               // calculated value: 1234.5 * 2 with 0.0
            Assert.That(html, Does.Contain("first"));
        }

        [Test]
        public async Task FormattedValues_FollowTheCurrentCulture()
        {
            string html = await Html("de-DE");
            Assert.That(html, Does.Contain("1.234,50"));
            Assert.That(html, Does.Contain("2469,0"));
        }
    }
}
#endif
