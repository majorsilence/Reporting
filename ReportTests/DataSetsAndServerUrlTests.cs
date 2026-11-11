#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// DataSets!Name.CommandText / DataSets("Name").CommandText (SSMS custom reports show their
    /// own SQL with it) and Globals!ReportServerUrl, which real SSRS reports use and the engine
    /// failed to parse.
    /// </summary>
    [TestFixture]
    public class DataSetsAndServerUrlTests
    {
        private static string Report(string value) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition"">
  <DataSources><DataSource Name=""Src""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
  <DataSets>
    <DataSet Name=""Sessions""><Query><DataSourceName>Src</DataSourceName><CommandText>select * from sys.dm_exec_sessions</CommandText></Query>
      <Fields><Field Name=""Id""><DataField>Id</DataField></Field></Fields></DataSet>
    <DataSet Name=""Computed""><Query><DataSourceName>Src</DataSourceName><CommandText>=""select "" &amp; Parameters!Top.Value &amp; "" from t""</CommandText></Query>
      <Fields><Field Name=""Id""><DataField>Id</DataField></Field></Fields></DataSet>
  </DataSets>
  <ReportParameters><ReportParameter Name=""Top""><DataType>String</DataType><DefaultValue><Values><Value>top 5 *</Value></Values></DefaultValue><Prompt>Top</Prompt></ReportParameter></ReportParameters>
  <ReportSections><ReportSection>
    <Body><Height>1in</Height><ReportItems>
      <Textbox Name=""T""><Paragraphs><Paragraph><TextRuns><TextRun><Value>{value}</Value></TextRun></TextRuns></Paragraph></Paragraphs>
        <Top>0in</Top><Left>0in</Left><Height>0.25in</Height><Width>5in</Width></Textbox>
    </ReportItems></Body>
    <Width>6in</Width><Page><PageHeight>11in</PageHeight><PageWidth>8.5in</PageWidth></Page>
  </ReportSection></ReportSections>
</Report>";

        private static async Task<string> Text(string value, string serverUrl = null)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(Report(value)) { SkipDatabaseSchemaValidation = true }.Parse();
            string errors = string.Join(" | ", report.ErrorItems?.Cast<object>() ?? Enumerable.Empty<object>());
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4), errors);
            if (serverUrl != null)
                report.ReportServerUrl = serverUrl;
            var empty = new DataTable();
            empty.Columns.Add("Id", typeof(int));
            await report.DataSets["Sessions"].SetData(empty);
            await report.DataSets["Computed"].SetData(empty);
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            return pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>().Single().Text;
        }

        [TestCase("=DataSets!Sessions.CommandText")]
        [TestCase("=DataSets(\"Sessions\").CommandText")]
        [TestCase("=DataSets(\"Sessions\").RewrittenCommandText")]
        public async Task CommandText_IsTheDataSetsQuery(string value)
        {
            Assert.That(await Text(value), Is.EqualTo("select * from sys.dm_exec_sessions"));
        }

        [Test]
        public async Task CommandText_OfAnExpressionQuery_IsEvaluated()
        {
            Assert.That(await Text("=DataSets(\"Computed\").CommandText &amp; \";\""), Is.EqualTo("select top 5 * from t;"));
        }

        [Test]
        public async Task AnUnknownDataSetOrProperty_IsAParseError()
        {
            RdlEngineConfig.RdlEngineConfigInit();
            foreach (var (value, message) in new[] { ("=DataSets!Nope.CommandText", "DataSet 'Nope' not found"), ("=DataSets!Sessions.Fields", "supports only CommandText") })
            {
                using var report = await new RDLParser(Report(value)) { SkipDatabaseSchemaValidation = true }.Parse();
                Assert.That(report.ErrorMaxSeverity, Is.GreaterThanOrEqualTo(8));
                Assert.That(string.Join(" | ", report.ErrorItems.Cast<object>()), Does.Contain(message));
            }
        }

        [Test]
        public async Task ReportServerUrl_IsEmptyUnlessTheHostSetsIt()
        {
            Assert.That(await Text("=\"[\" &amp; Globals!ReportServerUrl &amp; \"]\""), Is.EqualTo("[]"));
            Assert.That(await Text("=Replace(Globals!ReportServerUrl, \"/ReportServer\", \"\")", "http://reports.example/ReportServer"), Is.EqualTo("http://reports.example"));
            Assert.That(await Text("=Globals(\"ReportServerUrl\")", "http://x/ReportServer"), Is.EqualTo("http://x/ReportServer"));
        }
    }
}
#endif
