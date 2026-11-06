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
    /// Regression tests for issue #542.
    ///
    /// Query.AddParameters used to bind an unsupplied (Nothing) parameter value as a CLR null.
    /// ADO.NET providers treat a parameter whose Value is a CLR null as not set at all -
    /// SqlClient omits it from the call and SQL Server throws "expects the parameter '@X', which
    /// was not supplied". The fix binds DBNull.Value instead, which every provider accepts as an
    /// explicit SQL NULL.
    /// </summary>
    [TestFixture]
    public class QueryParameterNullValueTests
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
            _dbPath = Path.Combine(_workFolder, "issue542.db");

            using var cn = new SqliteConnection($"Data Source={_dbPath}");
            cn.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "CREATE TABLE Greetings (G TEXT NULL);";
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

        private string BuildRdl()
        {
            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
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
    <DataSet Name=""Echo"">
      <Query>
        <DataSourceName>DS1</DataSourceName>
        <CommandText>SELECT @Greeting AS G</CommandText>
        <QueryParameters>
          <QueryParameter Name=""@Greeting""><Value>=Parameters!Greeting.Value</Value></QueryParameter>
        </QueryParameters>
      </Query>
      <Fields>
        <Field Name=""G""><DataField>G</DataField></Field>
      </Fields>
    </DataSet>
  </DataSets>
  <ReportParameters>
    <ReportParameter Name=""Greeting"">
      <DataType>String</DataType>
      <Nullable>true</Nullable>
      <Prompt>Greeting</Prompt>
    </ReportParameter>
  </ReportParameters>
  <Width>6in</Width>
  <Body>
    <Height>1in</Height>
    <ReportItems>
      <Textbox Name=""G"">
        <Top>0in</Top><Left>0in</Left><Width>6in</Width><Height>0.25in</Height>
        <Value>=""["" &amp; First(Fields!G.Value, ""Echo"") &amp; ""]""</Value>
      </Textbox>
    </ReportItems>
  </Body>
</Report>";
        }

        [Test]
        public async Task DataPass_BindsUnsuppliedParameterAsDbNull_NotClrNull()
        {
            var rdlp = new RDLParser(BuildRdl());
            using var report = await rdlp.Parse();

            Assert.That(report, Is.Not.Null, "Report failed to parse");

            StrictBehaviorProvider.Reset();     // discard the parse-time schema pass

            // The Greeting report parameter is never supplied, so it evaluates to Nothing.
            await report.RunGetData(new System.Collections.Hashtable());

            var observed = StrictBehaviorProvider.ObservedParameters;
            Assert.That(observed, Is.Not.Empty, "The data pass bound no parameters");
            Assert.That(observed.First().Value, Is.EqualTo(DBNull.Value),
                "An unsupplied query parameter must bind as DBNull.Value, not a CLR null " +
                "(issue #542) - a CLR null leaves the parameter out of the call entirely on " +
                "providers such as Microsoft.Data.SqlClient.");
        }
    }
}
