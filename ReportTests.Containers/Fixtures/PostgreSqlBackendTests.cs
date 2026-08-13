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

        // KNOWN FAILURE - see issue #312. This test currently throws out of RDLParser.Parse().
        // Query.FinalPass runs the schema pass with AddParameters(bValue: false), which sets
        // dp.Value = null and never sets a DbType (Query.cs:642-652). SQL Server tolerates an
        // untyped null; Npgsql cannot infer a type and throws from ResolveTypeInfo before the
        // statement is sent. The effect is that any PostgreSQL report with a <QueryParameter>
        // fails to parse at all. Left failing deliberately: it documents the defect, and the
        // Containers category is excluded from the default pipeline.
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
