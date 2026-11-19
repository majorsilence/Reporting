using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// A table row that holds a subreport ends no higher than the subreport's table does. A table
    /// ends below the last thing it drew where its last row has room under its text; the row
    /// holding the subreport took the height of the text and images drawn in it, so the row after
    /// it started inside that room. A line the subreport draws past its table takes no room, as a
    /// line drawn past a row's bottom takes none.
    /// </summary>
    [TestFixture]
    public class TableRowHoldsSubreportEndTests
    {
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            RdlEngineConfig.RdlEngineConfigInit();
            _folder = Path.Combine(Path.GetTempPath(), $"rdl-row-holds-subreport-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);
            File.WriteAllText(Path.Combine(_folder, "total.json"), "[{\"Label\":\"TOTAL\"}]");
            File.WriteAllText(Path.Combine(_folder, "names.json"), "[{\"Name\":\"FIRST\"},{\"Name\":\"SECOND\"}]");
            File.WriteAllText(Path.Combine(_folder, "Roomy.rdl"), SubRdl("30pt", ""));
            File.WriteAllText(Path.Combine(_folder, "Tight.rdl"), SubRdl("10pt", ""));
            File.WriteAllText(Path.Combine(_folder, "Ruled.rdl"), SubRdl("10pt",
                "<Line Name=\"Rule\"><Top>40pt</Top><Left>0pt</Left><Width>200pt</Width><Height>0pt</Height>"
                + "<Style><BorderStyle><Default>Solid</Default></BorderStyle></Style></Line>"));
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_folder, recursive: true); }
            catch (IOException) { }
        }

        // A table of one row: 10pt of text at the top of a row of the given height.
        private string SubRdl(string rowHeight, string below) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>200pt</Width>
  <DataSources><DataSource Name=""Json""><ConnectionProperties><DataProvider>Json</DataProvider>
    <ConnectString>file={Path.Combine(_folder, "total.json")}</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data""><Query><DataSourceName>Json</DataSourceName><CommandText>columns=Label</CommandText></Query>
    <Fields><Field Name=""Label""><DataField>Label</DataField></Field></Fields></DataSet></DataSets>
  <Body><Height>10pt</Height><ReportItems>
    <Table Name=""T"">
      <DataSetName>Data</DataSetName>
      <Top>0pt</Top><Left>0pt</Left>
      <TableColumns><TableColumn><Width>200pt</Width></TableColumn></TableColumns>
      <Details><TableRows><TableRow><Height>{rowHeight}</Height><TableCells>
        <TableCell><ReportItems><Rectangle Name=""Band""><ReportItems>
          <Textbox Name=""L""><Top>0pt</Top><Left>0pt</Left><Width>100pt</Width><Height>10pt</Height><Value>=Fields!Label.Value</Value></Textbox>
        </ReportItems></Rectangle></ReportItems></TableCell>
      </TableCells></TableRow></TableRows></Details>
    </Table>
    {below}
  </ReportItems></Body>
</Report>";

        // Records of one 20pt row: a name, and under it a 10pt subreport 6pt down the row.
        private string Rdl(string subreport) => $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>200pt</Width>
  <PageHeight>400pt</PageHeight><PageWidth>300pt</PageWidth>
  <TopMargin>0pt</TopMargin><BottomMargin>0pt</BottomMargin>
  <DataSources><DataSource Name=""Json""><ConnectionProperties><DataProvider>Json</DataProvider>
    <ConnectString>file={Path.Combine(_folder, "names.json")}</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data""><Query><DataSourceName>Json</DataSourceName><CommandText>columns=Name</CommandText></Query>
    <Fields><Field Name=""Name""><DataField>Name</DataField></Field></Fields></DataSet></DataSets>
  <Body><Height>100pt</Height><ReportItems>
    <Table Name=""T"">
      <DataSetName>Data</DataSetName>
      <TableColumns><TableColumn><Width>200pt</Width></TableColumn></TableColumns>
      <Details><TableRows><TableRow><Height>20pt</Height><TableCells>
        <TableCell><ReportItems><Rectangle Name=""Band""><ReportItems>
          <Textbox Name=""D""><Top>0pt</Top><Left>0pt</Left><Width>50pt</Width><Height>6pt</Height><Value>=Fields!Name.Value</Value></Textbox>
          <Subreport Name=""Sub""><ReportName>{subreport}</ReportName>
            <Top>6pt</Top><Left>0pt</Left><Width>200pt</Width><Height>10pt</Height></Subreport>
        </ReportItems></Rectangle></ReportItems></TableCell>
      </TableCells></TableRow></TableRows></Details>
    </Table>
  </ReportItems></Body>
</Report>";

        private async Task<List<PageText>> Printed(string rdl)
        {
            var report = await new RDLParser(rdl) { Folder = _folder }.Parse();
            report.Folder = _folder;
            await report.RunGetData(null);
            using var pages = await report.BuildPages();
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(4),
                report.ErrorItems == null ? "" : string.Join("; ", report.ErrorItems.Cast<object>()));
            return pages.Cast<Page>().First().Cast<PageItem>().OfType<PageText>().ToList();
        }

        private static float Top(List<PageText> page, string text) => page.First(t => t.Text == text).Y;

        [Test]
        public async Task ARowWhoseSubreportsTableEndsBelowItsText_EndsWhereTheTableDoes()
        {
            // The subreport's row is 30pt with 10pt of text: the table ends 36pt down the row.
            var texts = await Printed(Rdl("Roomy"));

            Assert.That(Top(texts, "TOTAL"), Is.EqualTo(6f).Within(0.5f));
            Assert.That(Top(texts, "SECOND"), Is.EqualTo(36f).Within(0.5f),
                "The next row started inside the subreport's last row");
        }

        [Test]
        public async Task ARowItsSubreportFits_KeepsItsHeight()
        {
            // 6pt and the subreport's 10pt row fit the row's 20pt.
            var texts = await Printed(Rdl("Tight"));

            Assert.That(Top(texts, "SECOND"), Is.EqualTo(20f).Within(0.5f));
        }

        [Test]
        public async Task ALineTheSubreportDrawsPastItsTable_TakesNoRoomInTheRow()
        {
            // A rule 40pt down the subreport, past its 10pt table: drawn over the rows after.
            var texts = await Printed(Rdl("Ruled"));

            Assert.That(Top(texts, "SECOND"), Is.EqualTo(20f).Within(0.5f));
        }
    }
}
