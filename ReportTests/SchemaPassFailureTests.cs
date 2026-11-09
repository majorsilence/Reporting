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
    /// A query whose schema can't be read without running it, such as a batch that fills a temp table
    /// and then reads it, still parses and runs (#540). The parse-time schema pass failed and its
    /// exception escaped Parse(), so the report couldn't be loaded; the only way past it,
    /// SkipDatabaseSchemaValidation, also keeps the data pass from connecting. The schema pass's failure
    /// is now a warning and the fields are taken as declared.
    ///
    /// SQL Server is not needed: StrictBehaviorProvider compiles the whole batch for SchemaOnly before
    /// running any of it, as SQL Server's SET FMTONLY ON does, so the batch fails there the same way.
    /// </summary>
    [TestFixture]
    public class SchemaPassFailureTests
    {
        private string _folder;
        private string _dbPath;

        [SetUp]
        public void SetUp()
        {
            RdlEngineConfig.RdlEngineConfigInit();
            StrictBehaviorProvider.Register();
            StrictBehaviorProvider.Reset();

            _folder = Path.Combine(Path.GetTempPath(), $"rdl-schema-pass-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);
            _dbPath = Path.Combine(_folder, "numbers.db");
            File.WriteAllText(Path.Combine(_folder, "Pushed.rdl"), PushedSubreportRdl);
        }

        [TearDown]
        public void TearDown()
        {
            SqliteConnection.ClearAllPools();
            try { Directory.Delete(_folder, recursive: true); }
            catch (IOException) { }
        }

        private static string[] Messages(Report report)
            => report.ErrorItems?.Cast<string>().ToArray() ?? Array.Empty<string>();

        private string QueryRdl(string commandText) => $"""
            <?xml version="1.0" encoding="utf-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition">
              <DataSources>
                <DataSource Name="Db">
                  <ConnectionProperties>
                    <DataProvider>{StrictBehaviorProvider.ProviderName}</DataProvider>
                    <ConnectString>Data Source={_dbPath};Pooling=False</ConnectString>
                  </ConnectionProperties>
                </DataSource>
              </DataSources>
              <DataSets>
                <DataSet Name="Numbers">
                  <Query>
                    <DataSourceName>Db</DataSourceName>
                    <CommandText>{commandText}</CommandText>
                  </Query>
                  <Fields><Field Name="N"><DataField>N</DataField></Field></Fields>
                </DataSet>
              </DataSets>
              <Width>6in</Width>
              <Body>
                <Height>1in</Height>
                <ReportItems>
                  <Textbox Name="Rows">
                    <Top>0in</Top><Left>0in</Left><Width>6in</Width><Height>0.25in</Height>
                    <Value>="Rows: " &amp; Count(Fields!N.Value, "Numbers")</Value>
                  </Textbox>
                </ReportItems>
              </Body>
            </Report>
            """;

        [Test]
        public async Task TempTableQuery_ParsesAndItsDataPassRuns()
        {
            string rdl = QueryRdl("CREATE TEMP TABLE T AS SELECT 1 AS N UNION ALL SELECT 2; SELECT N FROM T;");
            using var report = await new RDLParser(rdl) { Folder = _folder }.Parse();

            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4), string.Join("; ", Messages(report)));
            Assert.That(Messages(report), Has.Some.Contains("SQL Exception during report compilation of DataSet 'Numbers'"),
                "The schema pass's failure should be logged as a warning naming its DataSet");

            await report.RunGetData(null);
            using var sg = new MemoryStreamGen();
            await report.RunRender(sg, OutputPresentationType.HTML);

            Assert.That(sg.GetText(), Does.Contain("Rows: 2"));
            Assert.That(Messages(report), Has.None.Contains("not returned by query"));
        }

        // With the schema pass unable to describe the batch, a DataField the query doesn't return
        // used to read as null silently. The data pass now warns, naming the field and DataSet (#549).
        [Test]
        public async Task TempTableQuery_MissingDataField_WarnsInTheDataPass()
        {
            string rdl = QueryRdl("CREATE TEMP TABLE T AS SELECT 1 AS M UNION ALL SELECT 2; SELECT M FROM T;");
            using var report = await new RDLParser(rdl) { Folder = _folder }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4), string.Join("; ", Messages(report)));
            Assert.That(Messages(report), Has.None.Contains("not returned by query"));

            await report.RunGetData(null);

            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4), string.Join("; ", Messages(report)));
            Assert.That(Messages(report), Has.Exactly(1).Contains("DataField 'N' not returned by query of DataSet 'Numbers'."));
        }

        [Test]
        public async Task QueryThatFailsToRun_StillFailsInTheDataPass()
        {
            using var report = await new RDLParser(QueryRdl("SELECT N FROM NoSuchTable")) { Folder = _folder }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4), string.Join("; ", Messages(report)));

            Assert.That(async () => await report.RunGetData(null), Throws.InstanceOf<SqliteException>());
            Assert.That(report.ErrorMaxSeverity, Is.GreaterThan(4));
        }

        // A report whose data is all pushed with SetData: provider SQL with no connection string, parsed
        // with SkipDatabaseSchemaValidation, and a subreport filled in SubreportDataRetrieval. The flag still
        // means neither pass connects: there is nothing to connect to.
        private const string PushedRdl = """
            <?xml version="1.0" encoding="utf-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition">
              <DataSources><DataSource Name="Pushed"><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
              <DataSets>
                <DataSet Name="Data">
                  <Query><DataSourceName>Pushed</DataSourceName><CommandText>SELECT N FROM #T</CommandText></Query>
                  <Fields><Field Name="N"><DataField>N</DataField></Field></Fields>
                </DataSet>
              </DataSets>
              <Width>6in</Width>
              <Body>
                <Height>1in</Height>
                <ReportItems>
                  <Textbox Name="Master">
                    <Top>0in</Top><Left>0in</Left><Width>6in</Width><Height>0.25in</Height>
                    <Value>="Master: " &amp; Sum(Fields!N.Value, "Data")</Value>
                  </Textbox>
                  <Subreport Name="Child">
                    <ReportName>Pushed</ReportName>
                    <Top>0.5in</Top><Left>0in</Left><Width>6in</Width><Height>0.25in</Height>
                  </Subreport>
                </ReportItems>
              </Body>
            </Report>
            """;

        private const string PushedSubreportRdl = """
            <?xml version="1.0" encoding="utf-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition">
              <DataSources><DataSource Name="Pushed"><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
              <DataSets>
                <DataSet Name="Data">
                  <Query><DataSourceName>Pushed</DataSourceName><CommandText>SELECT N FROM #T</CommandText></Query>
                  <Fields><Field Name="N"><DataField>N</DataField></Field></Fields>
                </DataSet>
              </DataSets>
              <Width>6in</Width>
              <Body>
                <Height>0.25in</Height>
                <ReportItems>
                  <Textbox Name="Child">
                    <Top>0in</Top><Left>0in</Left><Width>6in</Width><Height>0.25in</Height>
                    <Value>="Child: " &amp; Sum(Fields!N.Value, "Data")</Value>
                  </Textbox>
                </ReportItems>
              </Body>
            </Report>
            """;

        private static DataTable Numbers(params int[] values)
        {
            var table = new DataTable();
            table.Columns.Add("N", typeof(int));
            foreach (int n in values)
                table.Rows.Add(n);
            return table;
        }

        [Test]
        public async Task SkipDatabaseSchemaValidation_PushedData_RendersWithoutConnecting()
        {
            using var report = await new RDLParser(PushedRdl) { Folder = _folder, SkipDatabaseSchemaValidation = true }.Parse();
            report.Folder = _folder;
            await report.DataSets["Data"].SetData(Numbers(1, 2, 3));
            report.SubreportDataRetrieval += (_, e) => e.Report.DataSets["Data"].SetData(Numbers(10, 20)).GetAwaiter().GetResult();

            await report.RunGetData(null);
            var pages = await report.BuildPages();

            var texts = pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>()
                .OrderBy(t => t.Y).Select(t => t.Text).ToList();
            Assert.That(texts, Is.EqualTo(new[] { "Master: 6", "Child: 30" }));
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4), string.Join("; ", Messages(report)));
            Assert.That(Messages(report), Has.None.Contains("connect").IgnoreCase);
            Assert.That(Messages(report), Has.None.Contains("SQL Exception"));
            Assert.That(Messages(report), Has.None.Contains("no data was supplied with SetData"));
        }

        // The flag keeps the data pass from connecting, so a query whose data isn't pushed is never
        // run. The DataSet is empty, as before, but a warning now names it and says why (#540).
        [Test]
        public async Task SkipDatabaseSchemaValidation_QueryWithoutPushedData_WarnsThatItWasNotRun()
        {
            using (var cn = new SqliteConnection($"Data Source={_dbPath};Pooling=False"))
            {
                cn.Open();
                using var cmd = cn.CreateCommand();
                cmd.CommandText = "CREATE TABLE Numbers (N INTEGER); INSERT INTO Numbers VALUES (1), (2);";
                cmd.ExecuteNonQuery();
            }

            using var report = await new RDLParser(QueryRdl("SELECT N FROM Numbers")) { Folder = _folder, SkipDatabaseSchemaValidation = true }.Parse();
            await report.RunGetData(null);
            using var sg = new MemoryStreamGen();
            await report.RunRender(sg, OutputPresentationType.HTML);

            Assert.That(sg.GetText(), Does.Contain("Rows: 0"));
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4), string.Join("; ", Messages(report)));
            Assert.That(Messages(report), Has.Exactly(1).Contains("DataSet 'Numbers' has a query but no data was supplied with SetData"));
        }
    }
}
