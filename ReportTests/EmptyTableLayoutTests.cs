using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// A table with no rows. The RDL specification describes NoRows as a message to display
    /// "instead of the region layout", so with no NoRows message the layout prints: the header
    /// and footer bands, over no detail rows. The engine drew nothing at all, so a report given
    /// no data lost its column headings and footer totals, and a subreport converted from a
    /// Crystal report printed no report footer. With a NoRows message, the message alone prints,
    /// as before.
    /// </summary>
    [TestFixture]
    public class EmptyTableLayoutTests
    {
        [SetUp]
        public void SetUp() => RdlEngineConfig.RdlEngineConfigInit();

        private static string Rdl(string noRows) => $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>6in</Width>
  <DataSources><DataSource Name=""DS""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString/></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data""><Query><DataSourceName>DS</DataSourceName><CommandText>SELECT 1</CommandText></Query>
    <Fields><Field Name=""Name""><DataField>Name</DataField></Field><Field Name=""Amount""><DataField>Amount</DataField><TypeName>Double</TypeName></Field></Fields></DataSet></DataSets>
  <Body><Height>2in</Height><ReportItems>
    <Table Name=""T"">
      <DataSetName>Data</DataSetName>{noRows}
      <TableColumns><TableColumn><Width>2in</Width></TableColumn><TableColumn><Width>2in</Width></TableColumn></TableColumns>
      <Header><TableRows><TableRow><Height>0.25in</Height><TableCells>
        <TableCell><ReportItems><Textbox Name=""H1""><Value>HEADER-NAME</Value></Textbox></ReportItems></TableCell>
        <TableCell><ReportItems><Textbox Name=""H2""><Value>HEADER-AMOUNT</Value></Textbox></ReportItems></TableCell>
      </TableCells></TableRow></TableRows></Header>
      <Details><TableRows><TableRow><Height>0.25in</Height><TableCells>
        <TableCell><ReportItems><Textbox Name=""D1""><Value>=Fields!Name.Value</Value></Textbox></ReportItems></TableCell>
        <TableCell><ReportItems><Textbox Name=""D2""><Value>=Fields!Amount.Value</Value></Textbox></ReportItems></TableCell>
      </TableCells></TableRow></TableRows></Details>
      <Footer><TableRows><TableRow><Height>0.25in</Height><TableCells>
        <TableCell><ReportItems><Textbox Name=""F1""><Value>FOOTER-TOTAL</Value></Textbox></ReportItems></TableCell>
        <TableCell><ReportItems><Textbox Name=""F2""><Value>=Sum(Fields!Amount.Value)</Value></Textbox></ReportItems></TableCell>
      </TableCells></TableRow></TableRows></Footer>
    </Table>
  </ReportItems></Body>
</Report>";

        private static async Task<List<string>> Printed(string rdl, DataTable data)
        {
            var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(8));
            await report.DataSets["Data"].SetData(data);
            await report.RunGetData(null);
            using var pages = await report.BuildPages();
            return pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>().Select(t => t.Text).ToList();
        }

        private static DataTable Rows(int count)
        {
            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("Amount", typeof(double));
            for (int i = 0; i < count; i++) dt.Rows.Add("ROW-" + i, 10.0 * (i + 1));
            return dt;
        }

        [Test]
        public async Task NoRows_NoMessage_PrintsTheHeaderAndFooter()
        {
            var printed = await Printed(Rdl(""), Rows(0));

            Assert.That(printed, Does.Contain("HEADER-NAME"));
            Assert.That(printed, Does.Contain("HEADER-AMOUNT"));
            Assert.That(printed, Does.Contain("FOOTER-TOTAL"));
            Assert.That(printed.Any(t => t.StartsWith("ROW-")), Is.False);
            Assert.That(printed.IndexOf("HEADER-NAME"), Is.LessThan(printed.IndexOf("FOOTER-TOTAL")));
        }

        [Test]
        public async Task NoRows_WithAMessage_PrintsTheMessageAlone()
        {
            var printed = await Printed(Rdl("\n      <NoRows>NOTHING-TO-SHOW</NoRows>"), Rows(0));

            Assert.That(printed, Does.Contain("NOTHING-TO-SHOW"));
            Assert.That(printed, Does.Not.Contain("HEADER-NAME"));
            Assert.That(printed, Does.Not.Contain("FOOTER-TOTAL"));
        }

        [Test]
        public async Task WithRows_Unchanged()
        {
            var printed = await Printed(Rdl(""), Rows(2));

            Assert.That(printed, Does.Contain("HEADER-NAME"));
            Assert.That(printed, Does.Contain("ROW-0"));
            Assert.That(printed, Does.Contain("ROW-1"));
            Assert.That(printed, Does.Contain("FOOTER-TOTAL"));
            Assert.That(printed, Does.Contain("30"));
        }
    }
}
