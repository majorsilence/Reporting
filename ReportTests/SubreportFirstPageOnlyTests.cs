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
    /// A subreport marked &lt;Custom&gt;&lt;FirstPageOnly&gt;true&lt;/FirstPageOnly&gt;&lt;/Custom&gt; prints
    /// what fits on the page it starts on and no more. It is for a subreport in a band with a
    /// fixed place on the page, which has no next page to continue on: without the mark its
    /// rows run on to a page of their own.
    /// </summary>
    [TestFixture]
    public class SubreportFirstPageOnlyTests
    {
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            RdlEngineConfig.RdlEngineConfigInit();
            _folder = Path.Combine(Path.GetTempPath(), $"rdl-subreport-first-page-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);
            File.WriteAllText(Path.Combine(_folder, "long.json"), Rows(30));
            File.WriteAllText(Path.Combine(_folder, "short.json"), Rows(3));
            File.WriteAllText(Path.Combine(_folder, "Long.rdl"), SubRdl("long.json"));
            File.WriteAllText(Path.Combine(_folder, "Short.rdl"), SubRdl("short.json"));
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_folder, recursive: true); }
            catch (IOException) { }
        }

        private static string Rows(int n) =>
            "[" + string.Join(",", Enumerable.Range(1, n).Select(i => $"{{\"Name\":\"ROW {i}\"}}")) + "]";

        // A table of 20pt rows with a TOTAL row after the last.
        private string SubRdl(string data) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>200pt</Width>
  <DataSources><DataSource Name=""Json""><ConnectionProperties><DataProvider>Json</DataProvider>
    <ConnectString>file={Path.Combine(_folder, data)}</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data""><Query><DataSourceName>Json</DataSourceName><CommandText>columns=Name</CommandText></Query>
    <Fields><Field Name=""Name""><DataField>Name</DataField></Field></Fields></DataSet></DataSets>
  <Body><Height>40pt</Height><ReportItems>
    <Table Name=""T"">
      <DataSetName>Data</DataSetName>
      <Top>0pt</Top><Left>0pt</Left>
      <TableColumns><TableColumn><Width>200pt</Width></TableColumn></TableColumns>
      <Details><TableRows><TableRow><Height>20pt</Height><TableCells>
        <TableCell><ReportItems><Textbox Name=""N""><Value>=Fields!Name.Value</Value></Textbox></ReportItems></TableCell>
      </TableCells></TableRow></TableRows></Details>
      <Footer><TableRows><TableRow><Height>20pt</Height><TableCells>
        <TableCell><ReportItems><Textbox Name=""F""><Value>TOTAL</Value></Textbox></ReportItems></TableCell>
      </TableCells></TableRow></TableRows></Footer>
    </Table>
  </ReportItems></Body>
</Report>";

        // A 400pt page: a heading, the subreport 95pt down, and a page footer.
        private static string Rdl(string subreport, bool firstPageOnly) => $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>200pt</Width>
  <PageHeight>400pt</PageHeight><PageWidth>300pt</PageWidth>
  <TopMargin>0pt</TopMargin><BottomMargin>0pt</BottomMargin>
  <Body><Height>120pt</Height><ReportItems>
    <Textbox Name=""Head""><Top>0pt</Top><Left>0pt</Left><Width>100pt</Width><Height>20pt</Height><Value>HEAD</Value></Textbox>
    <Subreport Name=""Sub""><ReportName>{subreport}</ReportName>
      <Top>95pt</Top><Left>0pt</Left><Width>200pt</Width><Height>20pt</Height>
      {(firstPageOnly ? "<Custom><FirstPageOnly>true</FirstPageOnly></Custom>" : "")}</Subreport>
  </ReportItems></Body>
  <PageFooter><Height>20pt</Height><PrintOnFirstPage>true</PrintOnFirstPage><PrintOnLastPage>true</PrintOnLastPage><ReportItems>
    <Textbox Name=""Foot""><Value>=""Page "" &amp; Globals!PageNumber &amp; "" of "" &amp; Globals!TotalPages</Value></Textbox>
  </ReportItems></PageFooter>
</Report>";

        private async Task<List<List<string>>> Printed(string rdl)
        {
            var report = await new RDLParser(rdl) { Folder = _folder }.Parse();
            report.Folder = _folder;
            await report.RunGetData(null);
            using var pages = await report.BuildPages();
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(4),
                report.ErrorItems == null ? "" : string.Join("; ", report.ErrorItems.Cast<object>()));
            return pages.Cast<Page>()
                .Select(p => p.Cast<PageItem>().OfType<PageText>().Select(t => t.Text).ToList()).ToList();
        }

        [Test]
        public async Task ALongSubreport_PrintsWhatFitsItsFirstPage()
        {
            var pages = await Printed(Rdl("Long", firstPageOnly: true));

            Assert.That(pages, Has.Count.EqualTo(1), "The subreport's rows went on to another page");
            // 380pt above the page footer, the subreport from 95pt: 14 rows of 20pt and 5pt over.
            Assert.That(pages[0].Where(t => t.StartsWith("ROW ")),
                Is.EqualTo(Enumerable.Range(1, 14).Select(i => $"ROW {i}")));
            Assert.That(pages[0], Has.No.Member("TOTAL"), "The row after the last one printed is not drawn");
            Assert.That(pages[0], Has.Member("HEAD").And.Member("Page 1 of 1"));
        }

        [Test]
        public async Task WithoutTheMark_ItRunsOn()
        {
            var pages = await Printed(Rdl("Long", firstPageOnly: false));

            Assert.That(pages, Has.Count.EqualTo(2));
            Assert.That(pages[1], Has.Member("ROW 30").And.Member("TOTAL"));
        }

        [Test]
        public async Task ASubreportThatFits_PrintsWhole()
        {
            var pages = await Printed(Rdl("Short", firstPageOnly: true));

            Assert.That(pages, Has.Count.EqualTo(1));
            Assert.That(pages[0], Has.Member("ROW 3").And.Member("TOTAL"));
        }
    }
}
