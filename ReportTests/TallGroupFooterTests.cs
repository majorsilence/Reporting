using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// A group footer taller than a page is paged row by row: each row that does not fit what is
    /// left of the page starts the next one. The footer was moved to a new page as a whole and its
    /// rows then drawn with no fit check, so the rows past the page's bottom were lost, and room
    /// for all of it was kept below the group's last detail row, which moved that to a new page.
    /// </summary>
    [TestFixture]
    public class TallGroupFooterTests
    {
        private static string FooterRow(string name) => $"""
            <TableRow><Height>4in</Height><TableCells><TableCell><ReportItems>
              <Textbox Name="{name}"><Value>{name}</Value></Textbox>
            </ReportItems></TableCell></TableCells></TableRow>
            """;

        private static readonly string Rdl = """
            <?xml version="1.0" encoding="UTF-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition">
              <PageHeight>11in</PageHeight><PageWidth>8.5in</PageWidth>
              <TopMargin>.5in</TopMargin><BottomMargin>.5in</BottomMargin>
              <Width>6in</Width>
              <DataSources><DataSource Name="Pushed"><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
              <DataSets><DataSet Name="Data"><Query><DataSourceName>Pushed</DataSourceName><CommandText>none</CommandText></Query>
                <Fields><Field Name="Bill"><DataField>Bill</DataField></Field></Fields>
              </DataSet></DataSets>
              <Body>
                <Height>1in</Height>
                <ReportItems>
                  <Table Name="Bills">
                    <DataSetName>Data</DataSetName>
                    <TableColumns><TableColumn><Width>6in</Width></TableColumn></TableColumns>
                    <TableGroups><TableGroup>
                      <Grouping Name="ByBill"><GroupExpressions><GroupExpression>=Fields!Bill.Value</GroupExpression></GroupExpressions></Grouping>
                      <Footer><TableRows>
            """ + FooterRow("Front") + FooterRow("Middle") + FooterRow("Back") + """
                      </TableRows></Footer>
                    </TableGroup></TableGroups>
                    <Details><TableRows><TableRow><Height>.25in</Height><TableCells><TableCell><ReportItems>
                      <Textbox Name="BillNo"><Value>=Fields!Bill.Value</Value></Textbox>
                    </ReportItems></TableCell></TableCells></TableRow></TableRows></Details>
                  </Table>
                </ReportItems>
              </Body>
            </Report>
            """;

        [Test]
        public async Task AFooterTallerThanAPage_PrintsEachRowWithinAPage()
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(Rdl).Parse();
            var table = new DataTable();
            table.Columns.Add("Bill", typeof(string));
            table.Rows.Add("B-1");
            await report.DataSets["Data"].SetData(table);

            await report.RunGetData(null);
            var pages = await report.BuildPages();

            var placed = pages.Cast<Page>()
                .SelectMany((p, i) => p.Cast<PageItem>().OfType<PageText>().Select(t => (Page: i, Text: t)))
                .ToList();
            float bottom = pages.PageHeight;
            Assert.That(placed.Select(x => x.Text.Text), Is.EqualTo(new[] { "B-1", "Front", "Middle", "Back" }));
            Assert.That(placed.Where(x => x.Text.Y + x.Text.H > bottom), Is.Empty);
            // The page holds 10in: the bill number and two 4in rows fit on the first, the third does not.
            Assert.That(placed.Select(x => x.Page), Is.EqualTo(new[] { 0, 0, 0, 1 }));
        }
    }
}
