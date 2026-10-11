#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// A textbox's default data element output (the XML renderer) depends on whether its value is a constant:
    /// a constant is left out, a computed value is written.
    /// </summary>
    [TestFixture]
    public class DataElementOutputTests
    {
        private const string Rdl = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"" xmlns:rd=""http://schemas.microsoft.com/SQLServer/reporting/reportdesigner"">
  <Width>6in</Width><PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight>
  <DataSources><DataSource Name=""DS1""><ConnectionProperties><DataProvider>xxx</DataProvider><ConnectString>none</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets>
    <DataSet Name=""Data"">
      <Query><DataSourceName>DS1</DataSourceName><CommandText></CommandText></Query>
      <Fields><Field Name=""Id""><DataField>Id</DataField><rd:TypeName>System.Int32</rd:TypeName></Field></Fields>
      <fyi:Rows xmlns:fyi=""http://www.fyireporting.com/schemas""><Row><Id>7</Id></Row><Row><Id>8</Id></Row></fyi:Rows>
    </DataSet>
  </DataSets>
  <Body>
    <Height>1in</Height>
    <ReportItems>
      <Table Name=""Table1"">
        <DataSetName>Data</DataSetName>
        <Top>0in</Top><Left>0in</Left><Width>4in</Width><Height>0.2in</Height>
        <TableColumns><TableColumn><Width>2in</Width></TableColumn><TableColumn><Width>2in</Width></TableColumn></TableColumns>
        <Details><TableRows><TableRow><Height>0.2in</Height><TableCells>
          <TableCell><ReportItems><Textbox Name=""ConstCell""><DataElementName>ConstCell</DataElementName><Value>hello</Value></Textbox></ReportItems></TableCell>
          <TableCell><ReportItems><Textbox Name=""FieldCell""><DataElementName>FieldCell</DataElementName><Value>=Fields!Id.Value</Value></Textbox></ReportItems></TableCell>
        </TableCells></TableRow></TableRows></Details>
      </Table>
    </ReportItems>
  </Body>
</Report>";

        [OneTimeSetUp]
        public void Init() => RdlEngineConfig.RdlEngineConfigInit();

        [Test]
        public async Task Xml_OmitsConstantTextboxes_AndWritesComputedOnes()
        {
            using var report = await new RDLParser(Rdl).Parse();
            await report.RunGetData();
            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.XML);
            string xml = ms.GetText();

            Assert.That(xml, Does.Contain("FieldCell='7'"));
            Assert.That(xml, Does.Contain("FieldCell='8'"));
            Assert.That(xml, Does.Not.Contain("ConstCell"));
            Assert.That(xml, Does.Not.Contain("hello"));
        }
    }
}
#endif
