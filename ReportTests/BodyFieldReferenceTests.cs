using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// In a report with one dataset, a textbox in the body outside any data region may refer to a
    /// field directly (=Fields!Title.Value): SSRS reads it as the dataset's first row's, as
    /// First(Fields!Title.Value, "DataSet") does. It used to fail to parse ("Field not found").
    /// </summary>
    [TestFixture]
    public class BodyFieldReferenceTests
    {
        private static string Report(string value, string secondDataSet = "") => $@"<?xml version=""1.0""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <DataSources><DataSource Name=""S""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""D""><Query><DataSourceName>S</DataSourceName><CommandText>x</CommandText></Query>
    <Fields><Field Name=""Title""><DataField>Title</DataField></Field><Field Name=""N""><DataField>N</DataField></Field></Fields></DataSet>{secondDataSet}</DataSets>
  <Body><Height>1in</Height><ReportItems>
    <Rectangle Name=""R""><Top>0in</Top><Left>0in</Left><Height>0.5in</Height><Width>4in</Width><ReportItems>
      <Textbox Name=""T""><Value>{value}</Value><Top>0in</Top><Left>0in</Left><Height>0.25in</Height><Width>4in</Width></Textbox>
    </ReportItems></Rectangle>
  </ReportItems></Body><Width>5in</Width></Report>";

        private static async Task<Report> Parse(string rdl)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            return await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
        }

        [Test]
        public async Task ABareField_IsTheFirstRowsValue()
        {
            using var report = await Parse(Report("=Fields!Title.Value &amp; \" / \" &amp; Sum(Fields!N.Value)"));
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4), string.Join(" | ", report.ErrorItems?.Cast<object>() ?? Enumerable.Empty<object>()));
            var t = new DataTable();
            t.Columns.Add("Title", typeof(string));
            t.Columns.Add("N", typeof(int));
            t.Rows.Add("first", 2);
            t.Rows.Add("second", 3);
            await report.DataSets["D"].SetData(t);
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            Assert.That(pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>().Single().Text, Is.EqualTo("first / 5"));
        }

        [Test]
        public async Task WithTwoDataSets_ItIsStillAnError()
        {
            // Which dataset's field it is would be a guess.
            string second = @"<DataSet Name=""E""><Query><DataSourceName>S</DataSourceName><CommandText>y</CommandText></Query><Fields><Field Name=""Title""><DataField>Title</DataField></Field></Fields></DataSet>";
            using var report = await Parse(Report("=Fields!Title.Value", second));
            Assert.That(report.ErrorMaxSeverity, Is.GreaterThanOrEqualTo(8));
        }
    }
}
