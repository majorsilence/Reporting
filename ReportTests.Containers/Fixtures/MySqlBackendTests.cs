using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using ReportTests.Containers.Utils;
using Testcontainers.MySql;

namespace ReportTests.Containers.Fixtures
{
    [TestFixture]
    [Category("Containers")]
    public class MySqlBackendTests : ContainerFixtureBase
    {
        protected override string ProviderName => "MySQL.NET";

        protected override IDatabaseContainer BuildContainer()
            => new MySqlBuilder("mysql:8.4").Build();

        protected override DbConnection CreateConnection(string connectionString)
            => new MySqlConnection(connectionString);

        protected override IReadOnlyList<string> SeedStatements => new[]
        {
            "CREATE TABLE Sales (Product VARCHAR(50) NOT NULL, Amount INT NOT NULL)",
            "INSERT INTO Sales (Product, Amount) VALUES ('Widget A', 100), ('Widget B', 200), ('Widget C', 300)"
        };

        protected override string SelectAll
            => "SELECT Product, Amount FROM Sales ORDER BY Product";

        // NOTE: MySQL.NET carries <ReplaceParameters>true</ReplaceParameters> in
        // RdlEngineConfig.xml, so this parameter is NOT bound. Query.AddParameters returns
        // early and Query.AddParametersAsLiterals rewrites "@MinAmount" into the SQL text as a
        // literal before the command is ever created. That is a different engine path from the
        // other three backends here - the assertion below is deliberately identical, but what
        // it covers is not. Do not "fix" this to use a bound parameter.
        protected override string SelectFilteredByMinAmount
            => "SELECT Product, Amount FROM Sales WHERE Amount >= @MinAmount ORDER BY Product";

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
                new[] { new KeyValuePair<string, string>("MinAmount", "=200") });

            AssertRenderedRows(html, "Widget B", "Widget C");
            Assert.That(html, Does.Not.Contain("Widget A"),
                "The query parameter was not applied - the filtered-out row still rendered");
        }
    }
}
