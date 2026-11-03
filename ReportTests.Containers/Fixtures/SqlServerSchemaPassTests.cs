using System;
using System.Data;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using Testcontainers.MsSql;

namespace ReportTests.Containers.Fixtures
{
    /// <summary>
    /// The parse-time schema pass against a real SQL Server (#540). SqlClient answers
    /// CommandBehavior.SchemaOnly with SET FMTONLY ON, which compiles the whole batch without
    /// running it, so a query that fills a temp table and then reads it fails its schema pass
    /// with "Invalid object name '#T'" although it runs. That failure is a warning: the report
    /// parses, the fields are taken as declared, and the data pass runs the query.
    ///
    /// ReportTests/SchemaPassFailureTests.cs covers the same behaviour on SQLite through
    /// StrictBehaviorProvider. These tests cover what that can't: SQL Server itself, stored
    /// procedures, and whether a connection whose FMTONLY batch failed is left clean for the
    /// data pass, whether reused directly or handed back from the pool.
    ///
    /// Off by default, like the other container fixtures: set REPORTING_CONTAINER_TESTS=1 with
    /// Docker running.
    /// </summary>
    [TestFixture]
    [Category("Containers")]
    public class SqlServerSchemaPassTests
    {
        private const string GateVariable = "REPORTING_CONTAINER_TESTS";

        private const string TempTableBatch =
            "SELECT Product, Amount INTO #T FROM Sales; SELECT Product, Amount FROM #T ORDER BY Product";

        private MsSqlContainer _container;
        private string _connectionString;

        [OneTimeSetUp]
        public async Task StartBackend()
        {
            if (Environment.GetEnvironmentVariable(GateVariable) != "1")
            {
                Assert.Ignore(
                    $"Container-backed tests are off by default. Set {GateVariable}=1 with Docker running to enable them.");
            }

            RdlEngineConfig.RdlEngineConfigInit();

            _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
            await _container.StartAsync();
            _connectionString = _container.GetConnectionString();

            using var cn = new SqlConnection(_connectionString);
            await cn.OpenAsync();
            foreach (string statement in new[]
            {
                "CREATE TABLE Sales (Product NVARCHAR(50) NOT NULL, Amount INT NOT NULL)",
                "INSERT INTO Sales (Product, Amount) VALUES ('Widget A', 100), ('Widget B', 200), ('Widget C', 300)",
                """
                CREATE PROCEDURE TempTableProc @Min INT AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT Product, Amount INTO #P FROM Sales WHERE Amount >= @Min;
                    SELECT Product, Amount FROM #P ORDER BY Product;
                END
                """,
                "CREATE PROCEDURE PlainProc AS SELECT Product, Amount FROM Sales ORDER BY Product",
            })
            {
                using var cmd = cn.CreateCommand();
                cmd.CommandText = statement;
                await cmd.ExecuteNonQueryAsync();
            }
        }

        [OneTimeTearDown]
        public async Task StopBackend()
        {
            if (_container != null)
            {
                await _container.DisposeAsync();
                _container = null;
            }
        }

        private sealed record DataSetSpec(
            string Name, string CommandText, bool StoredProcedure = false, string QueryParameters = "");

        private static string Escape(string s) => SecurityElement.Escape(s);

        // One data source, one dataset per spec, and per dataset a textbox reading
        // "<Name>: <row count>/<sum of Amount>", so a test can see both that rows came back
        // and that Amount was typed as a number.
        private static string BuildRdl(string connectionString, params DataSetSpec[] dataSets)
        {
            string sets = string.Concat(dataSets.Select(d => $"""
                    <DataSet Name="{d.Name}">
                      <Query>
                        <DataSourceName>DS1</DataSourceName>
                        {(d.StoredProcedure ? "<CommandType>StoredProcedure</CommandType>" : "")}
                        <CommandText>{Escape(d.CommandText)}</CommandText>
                        {d.QueryParameters}
                      </Query>
                      <Fields>
                        <Field Name="Product"><DataField>Product</DataField></Field>
                        <Field Name="Amount"><DataField>Amount</DataField></Field>
                      </Fields>
                    </DataSet>
                """));

            string boxes = string.Concat(dataSets.Select((d, i) => $"""
                      <Textbox Name="Box{d.Name}">
                        <Top>{i * 0.3:0.0}in</Top><Left>0in</Left><Width>6in</Width><Height>0.25in</Height>
                        <Value>="{d.Name}: " &amp; Count(Fields!Product.Value, "{d.Name}") &amp; "/" &amp; Sum(Fields!Amount.Value, "{d.Name}")</Value>
                      </Textbox>
                """));

            return $"""
                <?xml version="1.0" encoding="utf-8"?>
                <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition">
                  <DataSources>
                    <DataSource Name="DS1">
                      <ConnectionProperties>
                        <DataProvider>Microsoft.Data.SqlClient</DataProvider>
                        <ConnectString>{Escape(connectionString)}</ConnectString>
                      </ConnectionProperties>
                    </DataSource>
                  </DataSources>
                  <DataSets>{sets}</DataSets>
                  <Width>6in</Width>
                  <Body>
                    <Height>2in</Height>
                    <ReportItems>{boxes}</ReportItems>
                  </Body>
                </Report>
                """;
        }

        private static string[] Messages(Report report)
            => report.ErrorItems?.Cast<string>().ToArray() ?? Array.Empty<string>();

        private sealed record RenderResult(string Html, string[] ParseMessages);

        private static async Task<RenderResult> ParseAndRenderAsync(string rdl)
        {
            using Report report = await new RDLParser(rdl).Parse();
            string[] parseMessages = Messages(report);
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4), string.Join("; ", parseMessages));

            await report.RunGetData(null);
            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.HTML);
            return new RenderResult(ms.GetText(), parseMessages);
        }

        [Test]
        public async Task TempTableBatch_ParsesWithWarning_AndItsDataPassReturnsRows()
        {
            var result = await ParseAndRenderAsync(BuildRdl(_connectionString, new DataSetSpec("A", TempTableBatch)));

            Assert.That(result.ParseMessages, Has.Some.Contains("Invalid object name '#T'"),
                "The schema pass's failure should be logged as a warning");
            Assert.That(result.Html, Does.Contain("A: 3/600"));
        }

        [Test]
        public async Task TempTableStoredProcedure_WithQueryParameter_ReturnsFilteredRows()
        {
            const string qp = """<QueryParameters><QueryParameter Name="@Min"><Value>=200</Value></QueryParameter></QueryParameters>""";
            var result = await ParseAndRenderAsync(BuildRdl(_connectionString,
                new DataSetSpec("A", "TempTableProc", StoredProcedure: true, QueryParameters: qp)));

            Assert.That(result.ParseMessages, Has.Some.Contains("Invalid object name '#P'"));
            Assert.That(result.Html, Does.Contain("A: 2/500"));
        }

        // The datasets of one data source share the parse-time connection, so the ones after a
        // failed FMTONLY batch run their schema pass on the connection it failed on.
        [Test]
        public async Task DataSetsAfterAFailedSchemaPass_OnTheSameDataSource_AreUnaffected()
        {
            var result = await ParseAndRenderAsync(BuildRdl(_connectionString,
                new DataSetSpec("A", TempTableBatch),
                new DataSetSpec("B", "SELECT Product, Amount FROM Sales WHERE Amount > 100 ORDER BY Product"),
                new DataSetSpec("C", "PlainProc", StoredProcedure: true)));

            Assert.Multiple(() =>
            {
                Assert.That(result.Html, Does.Contain("A: 3/600"));
                Assert.That(result.Html, Does.Contain("B: 2/500"));
                Assert.That(result.Html, Does.Contain("C: 3/600"));
            });
        }

        // Max Pool Size=1 makes the data pass, and then a second report, reuse the physical
        // connection the failed FMTONLY batch ran on. Had FMTONLY survived the pool, both would
        // return no rows.
        [Test]
        public async Task PooledConnection_AfterAFailedSchemaPass_StillReturnsRows()
        {
            string connectionString = _connectionString + ";Pooling=true;Max Pool Size=1";
            SqlConnection.ClearAllPools();

            var first = await ParseAndRenderAsync(BuildRdl(connectionString, new DataSetSpec("A", TempTableBatch)));
            var second = await ParseAndRenderAsync(BuildRdl(connectionString,
                new DataSetSpec("B", "SELECT Product, Amount FROM Sales")));

            Assert.That(first.Html, Does.Contain("A: 3/600"));
            Assert.That(second.Html, Does.Contain("B: 3/600"));
        }

        // The ADO.NET behaviour the engine relies on: SqlClient doesn't leave FMTONLY on for a
        // connection whose SchemaOnly batch failed.
        [Test]
        public async Task SqlClient_FailedSchemaOnlyBatch_LeavesTheConnectionClean()
        {
            using var cn = new SqlConnection(_connectionString + ";Pooling=false");
            await cn.OpenAsync();

            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = TempTableBatch;
                Assert.That(async () =>
                {
                    using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SchemaOnly);
                }, Throws.InstanceOf<SqlException>().With.Message.Contains("#T"));
            }

            using (var cmd = cn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM Sales";
                Assert.That(await cmd.ExecuteScalarAsync(), Is.EqualTo(3),
                    "FMTONLY was left on for the connection after its batch failed");
            }
        }

        [Test]
        public async Task QueryThatFailsToRun_StillFailsInTheDataPass()
        {
            using Report report = await new RDLParser(BuildRdl(_connectionString,
                new DataSetSpec("A", "SELECT Product, Amount FROM NoSuchTable"))).Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4), string.Join("; ", Messages(report)));

            Assert.That(async () => await report.RunGetData(null), Throws.InstanceOf<SqlException>());
            Assert.That(report.ErrorMaxSeverity, Is.GreaterThan(4));
        }

        [Test]
        public async Task QueryWhoseSchemaPassSucceeds_ParsesWithoutWarning()
        {
            var result = await ParseAndRenderAsync(BuildRdl(_connectionString,
                new DataSetSpec("A", "SELECT Product, Amount FROM Sales")));

            Assert.That(result.ParseMessages, Has.None.Contains("SQL Exception"));
            Assert.That(result.Html, Does.Contain("A: 3/600"));
        }
    }
}
