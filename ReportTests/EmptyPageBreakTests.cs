using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// A table row that does not fit below the header repeated at the top of a page does not
    /// fit any other page either. The table kept the page, as a new one would hold no more, but
    /// still printed the header on it a second time. And a chart in such a row, which ran past
    /// the page's bottom, moved to the next page by itself, leaving its row behind.
    /// </summary>
    [TestFixture]
    public class EmptyPageBreakTests
    {
        [SetUp]
        public void Prepare() => RdlEngineConfig.RdlEngineConfigInit();

        // A 300pt page with 10pt margins: 280pt of body. The table header is 100pt and repeats
        // on each page; each group's footer is 250pt, so header and footer never fit together.
        private static string Rdl(string footerItem) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <PageWidth>400pt</PageWidth><PageHeight>300pt</PageHeight>
  <LeftMargin>10pt</LeftMargin><RightMargin>10pt</RightMargin>
  <TopMargin>10pt</TopMargin><BottomMargin>10pt</BottomMargin>
  <Width>380pt</Width>
  <DataSources><DataSource Name=""DS""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString></ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>DS</DataSourceName><CommandText>select</CommandText></Query>
    <Fields>
      <Field Name=""G""><DataField>G</DataField><TypeName>System.String</TypeName></Field>
      <Field Name=""V""><DataField>V</DataField><TypeName>System.Double</TypeName></Field>
    </Fields>
  </DataSet></DataSets>
  <Body><Height>360pt</Height><ReportItems>
    <Table Name=""T"">
      <DataSetName>Data</DataSetName>
      <TableColumns><TableColumn><Width>380pt</Width></TableColumn></TableColumns>
      <Header><RepeatOnNewPage>true</RepeatOnNewPage><TableRows><TableRow><Height>100pt</Height>
        <TableCells><TableCell><ReportItems><Textbox Name=""Head""><Value>HEADER</Value></Textbox></ReportItems></TableCell></TableCells>
      </TableRow></TableRows></Header>
      <TableGroups><TableGroup>
        <Grouping Name=""ByG""><GroupExpressions><GroupExpression>=Fields!G.Value</GroupExpression></GroupExpressions><PageBreakAtEnd>true</PageBreakAtEnd></Grouping>
        <Footer><TableRows><TableRow><Height>250pt</Height>
          <TableCells><TableCell><ReportItems>
            <Rectangle Name=""Band""><ReportItems>{footerItem}</ReportItems></Rectangle>
          </ReportItems></TableCell></TableCells>
        </TableRow></TableRows></Footer>
      </TableGroup></TableGroups>
      <Details><TableRows><TableRow><Height>1pt</Height><Visibility><Hidden>true</Hidden></Visibility>
        <TableCells><TableCell><ReportItems><Textbox Name=""Detail""><Value>=Fields!G.Value</Value></Textbox></ReportItems></TableCell></TableCells>
      </TableRow></TableRows></Details>
    </Table>
  </ReportItems></Body>
</Report>";

        private static async Task<Pages> Build(string footerItem)
        {
            var report = await new RDLParser(Rdl(footerItem)).Parse();
            var data = new DataTable();
            data.Columns.Add("G", typeof(string));
            data.Columns.Add("V", typeof(double));
            // More than one row to a group: the header is not run again for the row it last
            // ran for, which hid the fault with one.
            foreach (var g in new[] { "A", "A", "A", "B", "B" })
                data.Rows.Add(g, 5.0);
            await report.DataSets["Data"].SetData(data);
            await report.RunGetData(null);
            return await report.BuildPages();
        }

        private static List<string> Texts(Page page) =>
            page.OfType<PageText>().Select(t => t.Text).ToList();

        [Test]
        public async Task FooterTallerThanRoomBelowHeader_PrintsBelowOneHeader()
        {
            var pages = await Build(@"<Textbox Name=""Foot""><Top>0pt</Top><Height>12pt</Height><Value>=""FOOTER "" &amp; Fields!G.Value</Value></Textbox>");

            Assert.That(pages.Count, Is.EqualTo(2), "one page for each group");
            for (int i = 0; i < pages.Count; i++)
            {
                var texts = Texts(pages[i]);
                Assert.That(texts.Count(t => t == "HEADER"), Is.EqualTo(1), $"page {i + 1}: {string.Join(", ", texts)}");
                Assert.That(texts, Does.Contain("FOOTER " + (i == 0 ? "A" : "B")));
            }
        }

        [Test]
        public async Task ChartInARowRunningPastThePage_StaysWithTheRow()
        {
            var pages = await Build(@"<Chart Name=""C""><Top>0pt</Top><Left>0pt</Left><Width>300pt</Width><Height>250pt</Height>
              <DataSetName>Data</DataSetName><Type>Column</Type>
              <SeriesGroupings><SeriesGrouping><DynamicSeries>
                <Grouping Name=""Series""><GroupExpressions><GroupExpression>=Fields!G.Value</GroupExpression></GroupExpressions></Grouping>
                <Label>=Fields!G.Value</Label>
              </DynamicSeries></SeriesGrouping></SeriesGroupings>
              <ChartData><ChartSeries><DataPoints><DataPoint><DataValues><DataValue><Value>=Sum(Fields!V.Value)</Value></DataValue></DataValues></DataPoint></DataPoints></ChartSeries></ChartData>
            </Chart>");

            Assert.That(pages.Count, Is.EqualTo(2), "one page for each group");
            for (int i = 0; i < pages.Count; i++)
            {
                Assert.That(pages[i].OfType<PageImage>().Count(), Is.EqualTo(1), $"page {i + 1} holds its group's chart");
                Assert.That(Texts(pages[i]).Count(t => t == "HEADER"), Is.EqualTo(1));
            }
        }
    }
}
