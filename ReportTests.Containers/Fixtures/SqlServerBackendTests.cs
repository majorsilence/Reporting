using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using ReportTests.Containers.Utils;
using Testcontainers.MsSql;

namespace ReportTests.Containers.Fixtures
{
    [TestFixture]
    [Category("Containers")]
    public class SqlServerBackendTests : ContainerFixtureBase
    {
        protected override string ProviderName => "Microsoft.Data.SqlClient";

        protected override IDatabaseContainer BuildContainer()
            => new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

        protected override DbConnection CreateConnection(string connectionString)
            => new SqlConnection(connectionString);

        protected override IReadOnlyList<string> SeedStatements => new[]
        {
            "CREATE TABLE Sales (Product NVARCHAR(50) NOT NULL, Amount INT NOT NULL)",
            "INSERT INTO Sales (Product, Amount) VALUES ('Widget A', 100), ('Widget B', 200), ('Widget C', 300)"
        };

        protected override string SelectAll
            => "SELECT Product, Amount FROM Sales ORDER BY Product";

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
                new[] { new KeyValuePair<string, string>("MinAmount", "200") });

            AssertRenderedRows(html, "Widget B", "Widget C");
            Assert.That(html, Does.Not.Contain("Widget A"),
                "The query parameter was not applied - the filtered-out row still rendered");
        }
    }
}
