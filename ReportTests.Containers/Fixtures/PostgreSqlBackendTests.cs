using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;
using Npgsql;
using NUnit.Framework;
using ReportTests.Containers.Utils;
using Testcontainers.PostgreSql;

namespace ReportTests.Containers.Fixtures
{
    [TestFixture]
    [Category("Containers")]
    public class PostgreSqlBackendTests : ContainerFixtureBase
    {
        protected override string ProviderName => "PostgreSQL";

        protected override IDatabaseContainer BuildContainer()
            => new PostgreSqlBuilder("postgres:16-alpine").Build();

        protected override DbConnection CreateConnection(string connectionString)
            => new NpgsqlConnection(connectionString);

        protected override IReadOnlyList<string> SeedStatements => new[]
        {
            "CREATE TABLE sales (product TEXT NOT NULL, amount INTEGER NOT NULL)",
            "INSERT INTO sales (product, amount) VALUES ('Widget A', 100), ('Widget B', 200), ('Widget C', 300)"
        };

        // Unquoted identifiers fold to lower case in PostgreSQL, so alias explicitly to the
        // names the report's <DataField> elements look up.
        protected override string SelectAll
            => "SELECT product AS \"Product\", amount AS \"Amount\" FROM sales ORDER BY product";

        protected override string SelectFilteredByMinAmount
            => "SELECT product AS \"Product\", amount AS \"Amount\" FROM sales WHERE amount >= @MinAmount ORDER BY product";

        [Test]
        public async Task Renders_rows_from_backend()
        {
            string html = await RenderAsync(SelectAll);
            AssertRenderedRows(html, "Widget A", "Widget B", "Widget C");
        }

        // Regression test for issue #312. Query.FinalPass runs the schema pass with
        // AddParameters(bValue: false), which supplied a null value and no DbType. SQL Server
        // infers a type from that; Npgsql cannot, and threw from ResolveTypeInfo before the
        // statement was sent - so every PostgreSQL report with a <QueryParameter> failed to
        // parse at all, never reaching the render.
        [Test]
        public async Task Renders_rows_with_query_parameter()
        {
            string html = await RenderAsync(
                SelectFilteredByMinAmount,
                new[] { new KeyValuePair<string, string>("MinAmount", "=200") });

            AssertRenderedRows(html, "Widget B", "Widget C");
            Assert.That(html, Does.Not.Contain("Widget A"),
                "The query parameter was not applied - the filtered-out row still rendered");
        }
    }
}
