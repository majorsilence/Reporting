using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests.Containers.Utils
{
    /// <summary>
    /// Shared lifecycle for the container-backed backend tests: start a real database, seed a
    /// small table, and render a report against it through the ordinary engine entry points.
    ///
    /// These exist because the rest of the suite runs on SQLite, which does not reproduce the
    /// behaviour of the databases people actually point the engine at. Issue #308 - every
    /// SQL-backed dataset silently returning zero rows - went unnoticed precisely because
    /// Microsoft.Data.Sqlite ignores CommandBehavior.SchemaOnly and returns rows anyway.
    ///
    /// Off by default. Set REPORTING_CONTAINER_TESTS=1 with a working Docker daemon to run
    /// them; otherwise every fixture reports Ignored rather than failing on a machine with no
    /// container runtime.
    /// </summary>
    public abstract class ContainerFixtureBase
    {
        private const string GateVariable = "REPORTING_CONTAINER_TESTS";

        private IDatabaseContainer _container;

        /// <summary>The &lt;DataProvider&gt; name the report will name, as registered in RdlEngineConfig.</summary>
        protected abstract string ProviderName { get; }

        /// <summary>The Testcontainers container for this backend. Not started yet.</summary>
        protected abstract IDatabaseContainer BuildContainer();

        /// <summary>An ADO.NET connection for seeding, using this backend's own client.</summary>
        protected abstract DbConnection CreateConnection(string connectionString);

        /// <summary>DDL and inserts, executed in order. One statement per entry - not every
        /// client accepts a batch, and Oracle rejects a trailing semicolon outright.</summary>
        protected abstract IReadOnlyList<string> SeedStatements { get; }

        /// <summary>SELECT returning columns aliased exactly "Product" and "Amount".</summary>
        protected abstract string SelectAll { get; }

        /// <summary>Same SELECT filtered by a single query parameter named MinAmount, written
        /// with this backend's placeholder syntax.</summary>
        protected abstract string SelectFilteredByMinAmount { get; }

        protected string ConnectionString { get; private set; }

        [OneTimeSetUp]
        public async Task StartBackend()
        {
            if (Environment.GetEnvironmentVariable(GateVariable) != "1")
            {
                Assert.Ignore(
                    $"Container-backed tests are off by default. Set {GateVariable}=1 with Docker running to enable them.");
            }

            RdlEngineConfig.RdlEngineConfigInit();

            _container = BuildContainer();
            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();

            using var cn = CreateConnection(ConnectionString);
            await cn.OpenAsync();
            foreach (string statement in SeedStatements)
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

        /// <summary>
        /// Parses and renders a report against this backend, asserting the definition itself is
        /// sound so that a later content assertion failing means the data pass, not the RDL.
        /// </summary>
        protected async Task<string> RenderAsync(
            string commandText,
            IEnumerable<KeyValuePair<string, string>> queryParameters = null)
        {
            string rdl = BackendRdl.Build(ProviderName, ConnectionString, commandText, queryParameters);

            var parser = new RDLParser(rdl);
            using Report report = await parser.Parse();

            Assert.That(report, Is.Not.Null, "Report failed to parse");
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4),
                "Unexpected fatal parse errors: " + string.Join("; ", report.ErrorItems));

            await report.RunGetData(null);
            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.HTML);
            return ms.GetText();
        }

        protected static void AssertRenderedRows(string html, params string[] expected)
        {
            Assert.Multiple(() =>
            {
                Assert.That(html, Does.Not.Contain(BackendRdl.NoRowsSentinel),
                    "The dataset returned zero rows, so the table rendered its <NoRows> message");
                foreach (string value in expected)
                    Assert.That(html, Does.Contain(value));
            });
        }
    }
}
