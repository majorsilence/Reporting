using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;

namespace ReportTests
{
    /// <summary>
    /// Verifies that RDLParser.SkipDatabaseSchemaValidation = true allows Parse() and rendering
    /// to succeed with a bogus/nonexistent connection string when all data is supplied via SetData().
    /// </summary>
    [TestFixture]
    public class SkipDatabaseSchemaValidationTests
    {
        // Minimal RDL with a SQLite connection string that points to a nonexistent file.
        // Fields carry explicit rd:TypeName annotations (recommended when DB schema is unavailable).
        private const string RdlXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition""
        xmlns:rd=""http://schemas.microsoft.com/SQLServer/reporting/reportdesigner"">
  <PageHeight>11in</PageHeight>
  <PageWidth>8.5in</PageWidth>
  <Width>7.5in</Width>
  <TopMargin>.25in</TopMargin>
  <LeftMargin>.25in</LeftMargin>
  <RightMargin>.25in</RightMargin>
  <BottomMargin>.25in</BottomMargin>
  <DataSources>
    <DataSource Name=""DS1"">
      <ConnectionProperties>
        <DataProvider>SQLite</DataProvider>
        <ConnectString>Data Source=this-file-does-not-exist.db</ConnectString>
      </ConnectionProperties>
    </DataSource>
  </DataSources>
  <DataSets>
    <DataSet Name=""Sales"">
      <Query>
        <DataSourceName>DS1</DataSourceName>
        <CommandText>SELECT Product, Amount FROM Sales</CommandText>
      </Query>
      <Fields>
        <Field Name=""Product"">
          <DataField>Product</DataField>
          <rd:TypeName>System.String</rd:TypeName>
        </Field>
        <Field Name=""Amount"">
          <DataField>Amount</DataField>
          <rd:TypeName>System.Decimal</rd:TypeName>
        </Field>
      </Fields>
    </DataSet>
  </DataSets>
  <Body>
    <Height>1in</Height>
    <ReportItems>
      <Table Name=""SalesTable"">
        <DataSetName>Sales</DataSetName>
        <Width>7in</Width>
        <TableColumns>
          <TableColumn><Width>4in</Width></TableColumn>
          <TableColumn><Width>3in</Width></TableColumn>
        </TableColumns>
        <Header>
          <TableRows>
            <TableRow>
              <Height>14pt</Height>
              <TableCells>
                <TableCell><ReportItems><Textbox Name=""HdrProduct""><Value>Product</Value><Style><FontWeight>Bold</FontWeight></Style></Textbox></ReportItems></TableCell>
                <TableCell><ReportItems><Textbox Name=""HdrAmount""><Value>Amount</Value><Style><FontWeight>Bold</FontWeight></Style></Textbox></ReportItems></TableCell>
              </TableCells>
            </TableRow>
          </TableRows>
          <RepeatOnNewPage>true</RepeatOnNewPage>
        </Header>
        <Details>
          <TableRows>
            <TableRow>
              <Height>12pt</Height>
              <TableCells>
                <TableCell><ReportItems><Textbox Name=""CellProduct""><Value>=Fields!Product.Value</Value></Textbox></ReportItems></TableCell>
                <TableCell><ReportItems><Textbox Name=""CellAmount""><Value>=Fields!Amount.Value</Value></Textbox></ReportItems></TableCell>
              </TableCells>
            </TableRow>
          </TableRows>
        </Details>
      </Table>
    </ReportItems>
  </Body>
</Report>";

        private Uri _outputFolder;

        [SetUp]
        public void SetUp()
        {
            _outputFolder = GeneralUtils.OutputTestsFolder();
            Directory.CreateDirectory(_outputFolder.LocalPath);
            RdlEngineConfig.RdlEngineConfigInit();
        }

        [Test]
        public async Task Parse_WithSkipFlag_SucceedsWithNonexistentDatabase()
        {
            var rdlp = new RDLParser(RdlXml) { SkipDatabaseSchemaValidation = true };
            using var report = await rdlp.Parse();

            Assert.That(report, Is.Not.Null, "Parse should return a report");
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4),
                "No fatal parse errors expected when skipping DB validation");
        }

        [Test]
        public async Task Render_WithSkipFlagAndSetData_ProducesValidPdf()
        {
            var rdlp = new RDLParser(RdlXml) { SkipDatabaseSchemaValidation = true };
            using var report = await rdlp.Parse();

            Assert.That(report, Is.Not.Null);
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4));

            var salesData = new List<SaleRow>
            {
                new("Widget A", 199.99m),
                new("Widget B", 49.50m),
                new("Widget C", 320.00m),
            };
            await report.DataSets["Sales"].SetData(salesData);
            await report.RunGetData(null);

            string outFile = Path.Combine(_outputFolder.LocalPath, "skip-db-test.pdf");
            using var sg = new OneFileStreamGen(outFile, true);
            await report.RunRender(sg, OutputPresentationType.PDF);

            Assert.That(File.Exists(outFile), "PDF output file was not created");
            byte[] bytes = File.ReadAllBytes(outFile);
            Assert.That(bytes.Length, Is.GreaterThan(100), "PDF is suspiciously small");
            Assert.That(bytes[0], Is.EqualTo((byte)'%'));
            Assert.That(bytes[1], Is.EqualTo((byte)'P'));
            Assert.That(bytes[2], Is.EqualTo((byte)'D'));
            Assert.That(bytes[3], Is.EqualTo((byte)'F'));
        }

        [Test]
        public async Task Render_WithSkipFlagAndSetData_ReportContainsExpectedRowCount()
        {
            var rdlp = new RDLParser(RdlXml) { SkipDatabaseSchemaValidation = true };
            using var report = await rdlp.Parse();

            var salesData = new List<SaleRow>
            {
                new("Alpha",  100m),
                new("Beta",   200m),
                new("Gamma",  300m),
                new("Delta",  400m),
                new("Epsilon",500m),
            };
            await report.DataSets["Sales"].SetData(salesData);
            await report.RunGetData(null);

            // Verify data was injected: DataSets["Sales"] row count matches input
            Assert.That(report.DataSets["Sales"], Is.Not.Null);

            string outFile = Path.Combine(_outputFolder.LocalPath, "skip-db-rowcount-test.pdf");
            using var sg = new OneFileStreamGen(outFile, true);
            await report.RunRender(sg, OutputPresentationType.PDF);

            Assert.That(File.Exists(outFile));
            Assert.That(new FileInfo(outFile).Length, Is.GreaterThan(0));
        }

        private record SaleRow(string Product, decimal Amount);
    }
}
