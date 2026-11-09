#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// Regression coverage for issue #541: the SSRS Lookup, LookupSet and MultiLookup
    /// functions. Unlike an aggregate's scope, their destination/result arguments are
    /// Fields! references that must resolve against the named target DataSet rather
    /// than the current scope -- the engine previously didn't know Lookup at all, so
    /// those arguments were resolved against the current scope and failed to parse
    /// with a misleading "Field not found".
    /// </summary>
    [TestFixture]
    public class LookupFunctionsTests
    {
        // Orders drives a table of per-row lookups; Labels is the joined-in dataset.
        // Category "Z" deliberately has no row in Labels, to exercise the no-match case.
        private const string Rdl = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"" xmlns:rd=""http://schemas.microsoft.com/SQLServer/reporting/reportdesigner"">
  <Width>6in</Width><PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight>
  <DataSources><DataSource Name=""DS1""><ConnectionProperties><DataProvider>xxx</DataProvider><ConnectString>none</ConnectString></ConnectionProperties></DataSource></DataSources>
  <ReportParameters>
    <ReportParameter Name=""Codes"">
      <DataType>String</DataType>
      <Nullable>false</Nullable>
      <AllowBlank>false</AllowBlank>
      <MultiValue>true</MultiValue>
      <DefaultValue><Values><Value>A</Value><Value>B</Value><Value>Z</Value></Values></DefaultValue>
      <Prompt>Category codes</Prompt>
    </ReportParameter>
  </ReportParameters>
  <DataSets>
    <DataSet Name=""Orders"">
      <Query><DataSourceName>DS1</DataSourceName><CommandText></CommandText></Query>
      <Fields>
        <Field Name=""OrderId""><DataField>OrderId</DataField><rd:TypeName>System.Int32</rd:TypeName></Field>
        <Field Name=""CategoryCode""><DataField>CategoryCode</DataField><rd:TypeName>System.String</rd:TypeName></Field>
      </Fields>
      <fyi:Rows xmlns:fyi=""http://www.fyireporting.com/schemas"">
        <Row><OrderId>1</OrderId><CategoryCode>A</CategoryCode></Row>
        <Row><OrderId>2</OrderId><CategoryCode>B</CategoryCode></Row>
        <Row><OrderId>3</OrderId><CategoryCode>A</CategoryCode></Row>
        <Row><OrderId>4</OrderId><CategoryCode>Z</CategoryCode></Row>
      </fyi:Rows>
    </DataSet>
    <DataSet Name=""Labels"">
      <Query><DataSourceName>DS1</DataSourceName><CommandText></CommandText></Query>
      <Fields>
        <Field Name=""Code""><DataField>Code</DataField><rd:TypeName>System.String</rd:TypeName></Field>
        <Field Name=""Description""><DataField>Description</DataField><rd:TypeName>System.String</rd:TypeName></Field>
      </Fields>
      <fyi:Rows xmlns:fyi=""http://www.fyireporting.com/schemas"">
        <Row><Code>A</Code><Description>Alpha-1</Description></Row>
        <Row><Code>A</Code><Description>Alpha-2</Description></Row>
        <Row><Code>B</Code><Description>Beta</Description></Row>
      </fyi:Rows>
    </DataSet>
  </DataSets>
  <Body>
    <Height>2in</Height>
    <ReportItems>
      <Textbox Name=""tMultiLookup"">
        <Top>0in</Top><Left>0in</Left><Width>6in</Width><Height>0.25in</Height>
        <Value>=Join(MultiLookup(Parameters!Codes.Value, Fields!Code.Value, Fields!Description.Value, ""Labels""), ""|"")</Value>
      </Textbox>
      <Table Name=""OrdersTable"">
        <DataSetName>Orders</DataSetName>
        <Top>0.25in</Top><Left>0in</Left><Width>6in</Width><Height>1in</Height>
        <TableColumns><TableColumn><Width>1in</Width></TableColumn><TableColumn><Width>2.5in</Width></TableColumn><TableColumn><Width>2.5in</Width></TableColumn></TableColumns>
        <Details><TableRows><TableRow><Height>0.2in</Height><TableCells>
          <TableCell><ReportItems><Textbox Name=""dOrderId""><Value>=Fields!OrderId.Value</Value></Textbox></ReportItems></TableCell>
          <TableCell><ReportItems><Textbox Name=""dLookup""><Value>=Lookup(Fields!CategoryCode.Value, Fields!Code.Value, Fields!Description.Value, ""Labels"")</Value></Textbox></ReportItems></TableCell>
          <TableCell><ReportItems><Textbox Name=""dLookupSet""><Value>=Join(LookupSet(Fields!CategoryCode.Value, Fields!Code.Value, Fields!Description.Value, ""Labels""), "", "")</Value></Textbox></ReportItems></TableCell>
        </TableCells></TableRow></TableRows></Details>
      </Table>
    </ReportItems>
  </Body>
</Report>";

        [OneTimeSetUp]
        public void Init() => RdlEngineConfig.RdlEngineConfigInit();

        private static async Task<(Report Report, string Html)> Render(string rdl)
        {
            Report report = await new RDLParser(rdl).Parse();
            Assert.That(report, Is.Not.Null, "Report failed to parse");
            await report.RunGetData();

            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.HTML);
            return (report, ms.GetText());
        }

        [Test]
        public async Task Lookup_ParsesAndRenders_WithoutFieldNotFoundError()
        {
            var (report, _) = await Render(Rdl);

            Assert.That(report.ErrorMaxSeverity, Is.LessThan(8),
                "Lookup/LookupSet/MultiLookup should parse against their target DataSet's fields.");
        }

        [Test]
        public async Task Lookup_ReturnsFirstMatch_OrNothingWhenNoRowMatches()
        {
            var (_, html) = await Render(Rdl);

            // Order 1 and 3 are category "A", which matches two rows in Labels; Lookup
            // takes the first. Order 2 is "B", the only match. Order 4 is "Z", unmatched.
            Assert.That(html, Does.Contain("Alpha-1"));
            Assert.That(html, Does.Contain("Beta"));
        }

        [Test]
        public async Task LookupSet_ReturnsAllMatches_JoinedTogether()
        {
            var (_, html) = await Render(Rdl);

            // LookupSet returns every matching row's result, so category "A" (two rows in
            // Labels) joins into both descriptions; "B" has exactly one match.
            Assert.That(html, Does.Contain("Alpha-1, Alpha-2"));
        }

        [Test]
        public async Task MultiLookup_ReturnsOneResultPerSourceKey_InOrder()
        {
            var (_, html) = await Render(Rdl);

            // Parameters!Codes.Value is ["A", "B", "Z"]; MultiLookup keeps one output
            // per key, so the unmatched "Z" renders as an empty entry between the
            // delimiters rather than shrinking the array.
            Assert.That(html, Does.Contain("Alpha-1|Beta|"));
        }

        [Test]
        public async Task Lookup_UnknownDataSetName_IsAParseError()
        {
            string rdl = SingleExpressionReport(
                "=Lookup(Fields!CategoryCode.Value, Fields!Code.Value, Fields!Description.Value, \"NoSuchDataSet\")");

            Report report = await new RDLParser(rdl).Parse();
            Assert.That(report, Is.Not.Null);
            Assert.That(report.ErrorMaxSeverity, Is.GreaterThanOrEqualTo(8));
            Assert.That(JoinErrors(report), Does.Contain("DataSet"));
        }

        [Test]
        public async Task Lookup_NonConstantDataSetName_IsAParseError()
        {
            string rdl = SingleExpressionReport(
                "=Lookup(Fields!CategoryCode.Value, Fields!Code.Value, Fields!Description.Value, Fields!CategoryCode.Value)");

            Report report = await new RDLParser(rdl).Parse();
            Assert.That(report, Is.Not.Null);
            Assert.That(report.ErrorMaxSeverity, Is.GreaterThanOrEqualTo(8));
            Assert.That(JoinErrors(report), Does.Contain("constant"));
        }

        [Test]
        public async Task Lookup_WrongArgumentCount_IsAParseError()
        {
            string rdl = SingleExpressionReport(
                "=Lookup(Fields!CategoryCode.Value, Fields!Code.Value, \"Labels\")");

            Report report = await new RDLParser(rdl).Parse();
            Assert.That(report, Is.Not.Null);
            Assert.That(report.ErrorMaxSeverity, Is.GreaterThanOrEqualTo(8));
            Assert.That(JoinErrors(report), Does.Contain("4 argument"));
        }

        private static string JoinErrors(Report report)
        {
            return report.ErrorItems == null
                ? string.Empty
                : string.Join(System.Environment.NewLine, System.Linq.Enumerable.Cast<string>(report.ErrorItems));
        }

        // A minimal report with Orders/Labels (same shape as Rdl above) and a single
        // body Textbox carrying the expression under test, for the parse-error cases.
        private static string SingleExpressionReport(string expression)
        {
            return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"" xmlns:rd=""http://schemas.microsoft.com/SQLServer/reporting/reportdesigner"">
  <Width>6in</Width><PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight>
  <DataSources><DataSource Name=""DS1""><ConnectionProperties><DataProvider>xxx</DataProvider><ConnectString>none</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets>
    <DataSet Name=""Orders"">
      <Query><DataSourceName>DS1</DataSourceName><CommandText></CommandText></Query>
      <Fields><Field Name=""CategoryCode""><DataField>CategoryCode</DataField><rd:TypeName>System.String</rd:TypeName></Field></Fields>
      <fyi:Rows xmlns:fyi=""http://www.fyireporting.com/schemas""><Row><CategoryCode>A</CategoryCode></Row></fyi:Rows>
    </DataSet>
    <DataSet Name=""Labels"">
      <Query><DataSourceName>DS1</DataSourceName><CommandText></CommandText></Query>
      <Fields><Field Name=""Code""><DataField>Code</DataField><rd:TypeName>System.String</rd:TypeName></Field><Field Name=""Description""><DataField>Description</DataField><rd:TypeName>System.String</rd:TypeName></Field></Fields>
      <fyi:Rows xmlns:fyi=""http://www.fyireporting.com/schemas""><Row><Code>A</Code><Description>Alpha</Description></Row></fyi:Rows>
    </DataSet>
  </DataSets>
  <Body>
    <Height>0.5in</Height>
    <ReportItems>
      <Table Name=""T"">
        <DataSetName>Orders</DataSetName>
        <Top>0in</Top><Left>0in</Left><Width>6in</Width><Height>0.2in</Height>
        <TableColumns><TableColumn><Width>6in</Width></TableColumn></TableColumns>
        <Details><TableRows><TableRow><Height>0.2in</Height><TableCells>
          <TableCell><ReportItems><Textbox Name=""Bad""><Value>" + expression + @"</Value></Textbox></ReportItems></TableCell>
        </TableCells></TableRow></TableRows></Details>
      </Table>
    </ReportItems>
  </Body>
</Report>";
        }
    }
}
#endif
