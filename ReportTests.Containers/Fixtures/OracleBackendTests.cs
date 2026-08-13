using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;
using NUnit.Framework;
using Oracle.ManagedDataAccess.Client;
using ReportTests.Containers.Utils;
using Testcontainers.Oracle;

namespace ReportTests.Containers.Fixtures
{
    /// <summary>
    /// Oracle is the backend the rest of the suite can say least about, and the one issue #309
    /// is about. Note this fixture names the "Oracle.ManagedDataAccess" provider, not the
    /// legacy "Oracle" entry - that one still points at Oracle.DataAccess.dll under a
    /// hard-coded F:\ path and cannot load anywhere.
    ///
    /// Expect this fixture to be slow: the Oracle image takes a minute or more to become
    /// healthy, which is why the container is per-fixture rather than per-test.
    /// </summary>
    [TestFixture]
    [Category("Containers")]
    public class OracleBackendTests : ContainerFixtureBase
    {
        protected override string ProviderName => "Oracle.ManagedDataAccess";

        protected override IDatabaseContainer BuildContainer()
            => new OracleBuilder("gvenzl/oracle-free:23-slim-faststart").Build();

        protected override DbConnection CreateConnection(string connectionString)
            => new OracleConnection(connectionString);

        // NUMBER(9) so ODP.NET reports the column as Int32 and matches the report's
        // <rd:TypeName>System.Int32</rd:TypeName>. No trailing semicolons - Oracle rejects
        // them when the statement is sent through ExecuteNonQuery.
        protected override IReadOnlyList<string> SeedStatements => new[]
        {
            "CREATE TABLE Sales (Product VARCHAR2(50) NOT NULL, Amount NUMBER(9) NOT NULL)",
            "INSERT INTO Sales (Product, Amount) VALUES ('Widget A', 100)",
            "INSERT INTO Sales (Product, Amount) VALUES ('Widget B', 200)",
            "INSERT INTO Sales (Product, Amount) VALUES ('Widget C', 300)"
        };

        // Unquoted identifiers fold to upper case in Oracle, so alias explicitly to the names
        // the report's <DataField> elements look up.
        protected override string SelectAll
            => "SELECT Product AS \"Product\", Amount AS \"Amount\" FROM Sales ORDER BY Product";

        // Oracle placeholders are :name. The engine names the parameter "@MinAmount" and
        // ODP.NET binds positionally by default, so a single parameter lines up regardless -
        // which is exactly the limitation issue #309 is about.
        protected override string SelectFilteredByMinAmount
            => "SELECT Product AS \"Product\", Amount AS \"Amount\" FROM Sales WHERE Amount >= :MinAmount ORDER BY Product";

        [Test]
        public async Task Renders_rows_from_backend()
        {
            string html = await RenderAsync(SelectAll);
            AssertRenderedRows(html, "Widget A", "Widget B", "Widget C");
        }

        [Test]
        public async Task Renders_rows_with_query_parameter()
        {
            string html = await RenderAsync(
                SelectFilteredByMinAmount,
                new[] { new KeyValuePair<string, string>("MinAmount", "200") });

            AssertRenderedRows(html, "Widget B", "Widget C");
            Assert.That(html, Does.Not.Contain("Widget A"),
                "The query parameter was not applied - the filtered-out row still rendered");
        }
    }
}
