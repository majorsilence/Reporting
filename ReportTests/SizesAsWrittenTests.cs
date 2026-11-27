#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// A size is the size that was written. It was kept to the hundredth of a millimetre, so 12pt read back as
    /// 11.99pt and 9.25pt as 9.241pt: every row or line of that size came out a little short or long, and a page
    /// of them ended a point out of place.
    /// </summary>
    [TestFixture]
    public class SizesAsWrittenTests
    {
        [TestCase("12pt", 12f)]
        [TestCase("9.25pt", 9.25f)]
        [TestCase("0.5in", 36f)]
        [TestCase("1pc", 12f)]
        [TestCase("25.4mm", 72f)]
        [TestCase("2.54cm", 72f)]
        public void ASizeInPoints_IsTheSizeWritten(string written, float points)
        {
            Assert.That(RSize.PointSize(written), Is.EqualTo(points).Within(0.0001f));
        }

        // A width written to fit its text to a hair was read a hair wider as often as not; read as written,
        // such a line still fits, and one that is plainly too long still breaks.
        [TestCase(0.01f, 1)]
        [TestCase(0.05f, 2)]
        public void ALineOverItsWidthByAHair_Fits(float over, int lines)
        {
            // every character 5pt wide: "ONE TWO" is 35pt
            Assert.That(TextWrap.LineCount("ONE TWO", 35f - over, line => line.Length * 5f), Is.EqualTo(lines));
        }

        private const string Rdl = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>400pt</Width>
  <DataSources><DataSource Name=""D""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>D</DataSourceName><CommandText>none</CommandText></Query>
    <Fields><Field Name=""V""><DataField>V</DataField><TypeName>Int32</TypeName></Field></Fields>
  </DataSet></DataSets>
  <Body><Height>300pt</Height><ReportItems>
    <Table Name=""T"">
      <DataSetName>Data</DataSetName>
      <TableColumns><TableColumn><Width>300pt</Width></TableColumn></TableColumns>
      <Details><TableRows><TableRow><Height>12pt</Height><TableCells><TableCell><ReportItems>
        <Textbox Name=""Line""><Value>=""ROW "" &amp; CStr(Fields!V.Value)</Value></Textbox>
      </ReportItems></TableCell></TableCells></TableRow></TableRows></Details>
    </Table>
  </ReportItems></Body>
  <PageHeight>800pt</PageHeight><PageWidth>500pt</PageWidth>
  <TopMargin>0pt</TopMargin><BottomMargin>0pt</BottomMargin>
</Report>";

        [Test]
        public async Task FiftyRowsOfTwelvePoints_EndSixHundredPointsDown()
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(Rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            var table = new DataTable();
            table.Columns.Add("V", typeof(int));
            for (int i = 1; i <= 51; i++)
                table.Rows.Add(i);
            await report.DataSets["Data"].SetData(table);
            await report.RunGetData(null);
            using var pages = await report.BuildPages();

            var rows = pages.Cast<Page>().First().Cast<PageItem>().OfType<PageText>().ToList();
            float first = rows.Single(t => t.Text == "ROW 1").Y, last = rows.Single(t => t.Text == "ROW 51").Y;

            // read back as 11.99pt, the fifty rows above the last took 599.5pt
            Assert.That(last - first, Is.EqualTo(600f).Within(0.05f));
        }
    }
}
#endif
