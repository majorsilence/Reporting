using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using ReportTests.Utils;

namespace ReportTests
{
    /// <summary>
    /// Regression tests for issue #308. Both bugs were silent: the report rendered, no error
    /// was raised above severity 4, and the output was simply missing content. The existing
    /// suite only covered the static &lt;Rows&gt; and SetData() paths, neither of which reaches
    /// Query.GetData's data reader, so nothing caught them.
    /// </summary>
    [TestFixture]
    public class SqlDataSetRegressionTests
    {
        private string _dbPath;
        private string _workFolder;

        [SetUp]
        public void SetUp()
        {
            RdlEngineConfig.RdlEngineConfigInit();
            StrictBehaviorProvider.Register();
            StrictBehaviorProvider.Reset();

            _workFolder = Path.Combine(Path.GetTempPath(), "rdlTestResults", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_workFolder);
            _dbPath = Path.Combine(_workFolder, "issue308.db");

            using var cn = new SqliteConnection($"Data Source={_dbPath}");
            cn.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText =
                "CREATE TABLE Sales (Product TEXT NOT NULL, Amount INTEGER NOT NULL);" +
                "INSERT INTO Sales (Product, Amount) VALUES ('Widget A', 100);" +
                "INSERT INTO Sales (Product, Amount) VALUES ('Widget B', 200);" +
                "INSERT INTO Sales (Product, Amount) VALUES ('Widget C', 300);";
            cmd.ExecuteNonQuery();
            SqliteConnection.ClearAllPools();
        }

        [TearDown]
        public void TearDown()
        {
            SqliteConnection.ClearAllPools();
            try { Directory.Delete(_workFolder, true); }
            catch (IOException) { /* the db file may still be held on some platforms */ }
        }

        private string BuildRdl(bool includeTableFooter)
        {
            string footer = includeTableFooter
                ? @"<Footer>
              <TableRows>
                <TableRow>
                  <Height>12pt</Height>
                  <TableCells>
                    <TableCell><ReportItems><Textbox Name=""FtrLabel""><Value>Grand Total</Value></Textbox></ReportItems></TableCell>
                    <TableCell><ReportItems><Textbox Name=""FtrTotal""><Value>=Sum(Fields!Amount.Value)</Value></Textbox></ReportItems></TableCell>
                  </TableCells>
                </TableRow>
              </TableRows>
            </Footer>"
                : string.Empty;

            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
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
        <DataProvider>{StrictBehaviorProvider.ProviderName}</DataProvider>
        <ConnectString>Data Source={_dbPath}</ConnectString>
        <IntegratedSecurity>false</IntegratedSecurity>
      </ConnectionProperties>
    </DataSource>
  </DataSources>
  <DataSets>
    <DataSet Name=""Sales"">
      <Query>
        <DataSourceName>DS1</DataSourceName>
        <CommandText>SELECT Product, Amount FROM Sales ORDER BY Product</CommandText>
      </Query>
      <Fields>
        <Field Name=""Product"">
          <DataField>Product</DataField>
          <rd:TypeName>System.String</rd:TypeName>
        </Field>
        <Field Name=""Amount"">
          <DataField>Amount</DataField>
          <rd:TypeName>System.Int32</rd:TypeName>
        </Field>
      </Fields>
    </DataSet>
  </DataSets>
  <Body>
    <Height>2in</Height>
    <ReportItems>
      <Table Name=""SalesTable"">
        <DataSetName>Sales</DataSetName>
        <NoRows>NO_ROWS_MESSAGE</NoRows>
        <Width>7in</Width>
        <TableColumns>
          <TableColumn><Width>4in</Width></TableColumn>
          <TableColumn><Width>3in</Width></TableColumn>
        </TableColumns>
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
        {footer}
      </Table>
    </ReportItems>
  </Body>
</Report>";
        }

        private static async Task<string> RenderHtml(Report report)
        {
            await report.RunGetData(null);
            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.HTML);
            return ms.GetText();
        }

        [Test]
        public async Task SqlBackedDataSet_ReturnsRows()
        {
            // Bug: Query.CreateDataReader hard-coded CommandBehavior.SchemaOnly, which is
            // correct for the FinalPass schema pass but returns column metadata and no rows.
            // GetData shared the helper, so the first Read() returned false and every
            // SQL-backed dataset rendered its <NoRows> message with no error at any severity.
            var rdlp = new RDLParser(BuildRdl(includeTableFooter: false));
            using var report = await rdlp.Parse();

            Assert.That(report, Is.Not.Null, "Report failed to parse");
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4),
                "Unexpected fatal parse errors: " + string.Join("; ", report.ErrorItems));

            StrictBehaviorProvider.Reset();     // discard the parse-time schema pass
            string html = await RenderHtml(report);

            Assert.Multiple(() =>
            {
                Assert.That(StrictBehaviorProvider.ObservedBehaviors, Is.Not.Empty,
                    "The data pass never opened a reader");
                Assert.That(StrictBehaviorProvider.ObservedBehaviors,
                    Is.All.Matches<CommandBehavior>(b => (b & CommandBehavior.SchemaOnly) == 0),
                    "The data pass asked for CommandBehavior.SchemaOnly, which returns no rows");

                Assert.That(html, Does.Not.Contain("NO_ROWS_MESSAGE"),
                    "The dataset returned zero rows, so the table rendered its <NoRows> message");
                Assert.That(html, Does.Contain("Widget A"));
                Assert.That(html, Does.Contain("Widget B"));
                Assert.That(html, Does.Contain("Widget C"));
            });
        }

        [Test]
        public async Task SqlBackedDataSet_SchemaPassStillResolvesColumnTypes()
        {
            // The schema pass must keep using SchemaOnly: it has to learn the column names
            // and types without executing the query at compile time. Parsing with no <Fields>
            // type annotations only succeeds if the schema pass resolved them from the reader.
            string rdl = BuildRdl(includeTableFooter: false)
                .Replace("<rd:TypeName>System.String</rd:TypeName>", string.Empty)
                .Replace("<rd:TypeName>System.Int32</rd:TypeName>", string.Empty);

            var rdlp = new RDLParser(rdl);
            using var report = await rdlp.Parse();

            Assert.That(report, Is.Not.Null);
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4),
                "Unexpected fatal parse errors: " + string.Join("; ", report.ErrorItems));

            Assert.That(StrictBehaviorProvider.ObservedBehaviors,
                Is.All.Matches<CommandBehavior>(b => (b & CommandBehavior.SchemaOnly) != 0),
                "The schema pass must not execute the query at compile time");

            string html = await RenderHtml(report);
            Assert.That(html, Does.Contain("Widget A"));
        }

        [Test]
        public async Task TableLevelFooter_ParsesAndRenders()
        {
            // Bug: Table.cs matched the element name "gooter" instead of "footer", so a
            // table-level <Footer> fell through to the default branch, logged
            // "Unknown Table element 'Footer' ignored." at severity 4 and left _Footer null.
            var rdlp = new RDLParser(BuildRdl(includeTableFooter: true));
            using var report = await rdlp.Parse();

            Assert.That(report, Is.Not.Null, "Report failed to parse");
            Assert.That(string.Join("; ", report.ErrorItems), Does.Not.Contain("Unknown Table element 'Footer'"),
                "The table-level <Footer> was not recognised by the Table element parser");

            string html = await RenderHtml(report);

            Assert.Multiple(() =>
            {
                Assert.That(html, Does.Contain("Grand Total"), "Table footer row did not render");
                Assert.That(html, Does.Contain("600"), "Table footer aggregate did not render");
            });
        }
    }
}
