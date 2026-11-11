#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// A DataSet whose query is in a shared dataset (SharedDataSet/SharedDataSetReference, RDL
    /// 2008+) used to fail to parse ("Query element must be specified in a DataSet"). The engine
    /// does not load .rsd files, so the dataset is filled by the application with SetData, and the
    /// report says so in a warning.
    /// </summary>
    [TestFixture]
    public class SharedDataSetTests
    {
        private const string Rdl = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition"">
  <DataSets><DataSet Name=""Servers""><SharedDataSet><SharedDataSetReference>Servers</SharedDataSetReference></SharedDataSet>
    <Fields><Field Name=""ServerName""><DataField>ServerName</DataField></Field></Fields></DataSet></DataSets>
  <ReportSections><ReportSection>
    <Body><Height>1in</Height><ReportItems>
      <Textbox Name=""Count""><Paragraphs><Paragraph><TextRuns><TextRun><Value>=CountRows(""Servers"") &amp; "": "" &amp; First(Fields!ServerName.Value, ""Servers"")</Value></TextRun></TextRuns></Paragraph></Paragraphs>
        <Top>0in</Top><Left>0in</Left><Height>0.25in</Height><Width>4in</Width></Textbox>
    </ReportItems></Body>
    <Width>6in</Width><Page><PageHeight>11in</PageHeight><PageWidth>8.5in</PageWidth></Page>
  </ReportSection></ReportSections>
</Report>";

        private static async Task<(string Text, string Errors)> Run(bool supplyRows)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(Rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            string errors = string.Join(" | ", report.ErrorItems?.Cast<object>() ?? Enumerable.Empty<object>());
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4), errors);
            if (supplyRows)
            {
                var t = new DataTable();
                t.Columns.Add("ServerName", typeof(string));
                t.Rows.Add("alpha");
                t.Rows.Add("beta");
                await report.DataSets["Servers"].SetData(t);
            }
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            return (pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>().Single().Text, errors);
        }

        [Test]
        public async Task ItsRows_AreTheOnesTheApplicationSupplies()
        {
            var (text, errors) = await Run(supplyRows: true);
            Assert.That(text, Is.EqualTo("2: alpha"));
            Assert.That(errors, Does.Contain("uses the shared dataset 'Servers', which is not loaded"));
        }

        [Test]
        public async Task WithNoneSupplied_ItIsEmpty()
        {
            var (text, _) = await Run(supplyRows: false);
            Assert.That(text, Does.StartWith("0"));
        }
    }
}
#endif
