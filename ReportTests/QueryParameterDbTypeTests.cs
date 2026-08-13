using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using ReportTests.Utils;

namespace ReportTests
{
    /// <summary>
    /// Regression tests for issue #312.
    ///
    /// Query.FinalPass runs the compile-time schema pass with AddParameters(bValue: false),
    /// which deliberately supplies no value. It also used to supply no type, leaving the
    /// parameter a null of unknown kind. SQL Server and SQLite infer something and carry on;
    /// Npgsql cannot, because it has to choose a wire format before sending, and threw out of
    /// RDLParser.Parse(). Every PostgreSQL report with a &lt;QueryParameter&gt; was unusable.
    ///
    /// The provider-specific half is covered by ReportTests.Containers against a real
    /// PostgreSQL instance. These tests cover the engine half - that a type is set at all -
    /// without needing a container, so the fix stays covered in the default pipeline.
    /// </summary>
    [TestFixture]
    public class QueryParameterDbTypeTests
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
            _dbPath = Path.Combine(_workFolder, "issue312.db");

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

        private string BuildRdl(string parameterValue, string typeName)
        {
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
        <CommandText>SELECT Product, Amount FROM Sales WHERE Amount &gt;= @MinAmount ORDER BY Product</CommandText>
        <QueryParameters>
          <QueryParameter Name=""MinAmount""><Value>{parameterValue}</Value></QueryParameter>
        </QueryParameters>
      </Query>
      <Fields>
        <Field Name=""Product"">
          <DataField>Product</DataField>
          <rd:TypeName>System.String</rd:TypeName>
        </Field>
        <Field Name=""Amount"">
          <DataField>Amount</DataField>
          <rd:TypeName>{typeName}</rd:TypeName>
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
                <TableCell><ReportItems><Textbox Name=""Product""><Value>=Fields!Product.Value</Value></Textbox></ReportItems></TableCell>
                <TableCell><ReportItems><Textbox Name=""Amount""><Value>=Fields!Amount.Value</Value></Textbox></ReportItems></TableCell>
              </TableCells>
            </TableRow>
          </TableRows>
        </Details>
      </Table>
    </ReportItems>
  </Body>
</Report>";
        }

        [Test]
        public async Task SchemaPass_SetsDbTypeFromTheDeclaredParameterType()
        {
            // The schema pass runs during Parse(). Before the fix the parameter reached the
            // provider with no DbType at all, so a provider that cannot infer one from a null
            // value had nothing to bind with.
            // "=200" is an expression and types as Int32. A bare "200" would be an RDL string
            // literal - that case is covered separately below.
            var rdlp = new RDLParser(BuildRdl("=200", "System.Int32"));
            using var report = await rdlp.Parse();

            Assert.That(report, Is.Not.Null, "Report failed to parse");
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4),
                "Unexpected fatal parse errors: " + string.Join("; ", report.ErrorItems));

            var observed = StrictBehaviorProvider.ObservedParameters;
            Assert.That(observed, Is.Not.Empty, "The schema pass bound no parameters");

            var p = observed.First();
            Assert.Multiple(() =>
            {
                Assert.That(p.Name, Is.EqualTo("@MinAmount"));
                Assert.That(p.DbType, Is.EqualTo(DbType.Int32),
                    "The schema pass must type the parameter from its declared type");
                // The value is a stand-in, never used: the reader is opened with SchemaOnly.
                // What matters is that it is bindable, which an untyped null was not.
                Assert.That(p.Value, Is.EqualTo(0),
                    "The schema pass must bind a placeholder rather than an untyped null (issue #312)");
            });
        }

        [Test]
        public async Task SchemaPass_SetsDbTypeForStringParameters()
        {
            // A bare value is an RDL string literal, not an expression.
            var rdlp = new RDLParser(BuildRdl("200", "System.Int32"));
            using var report = await rdlp.Parse();

            Assert.That(report, Is.Not.Null, "Report failed to parse");

            var observed = StrictBehaviorProvider.ObservedParameters;
            Assert.That(observed, Is.Not.Empty, "The schema pass bound no parameters");
            Assert.Multiple(() =>
            {
                Assert.That(observed.First().DbType, Is.EqualTo(DbType.String));
                Assert.That(observed.First().Value, Is.EqualTo(string.Empty));
            });
        }

        [Test]
        public async Task DataPass_StillBindsTheEvaluatedValue()
        {
            // Typing the schema pass must not disturb the data pass, which supplies a real
            // value and relies on the provider inferring from it as before.
            var rdlp = new RDLParser(BuildRdl("=200", "System.Int32"));
            using var report = await rdlp.Parse();

            StrictBehaviorProvider.Reset();     // discard the parse-time schema pass

            await report.RunGetData(null);
            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.HTML);
            string html = ms.GetText();

            var observed = StrictBehaviorProvider.ObservedParameters;
            Assert.That(observed, Is.Not.Empty, "The data pass bound no parameters");
            Assert.That(observed.First().Value, Is.EqualTo(200),
                "The data pass must bind the evaluated parameter value");

            Assert.Multiple(() =>
            {
                Assert.That(html, Does.Not.Contain("NO_ROWS_MESSAGE"));
                Assert.That(html, Does.Contain("Widget B"));
                Assert.That(html, Does.Contain("Widget C"));
                Assert.That(html, Does.Not.Contain("Widget A"),
                    "The query parameter was not applied - the filtered-out row still rendered");
            });
        }
    }
}
