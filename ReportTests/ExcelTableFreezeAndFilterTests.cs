using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Majorsilence.Spreadsheet;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// When a report's sheet holds exactly one table, Excel output freezes the panes below its header
    /// and puts an AutoFilter over the header and detail rows (not the footer, which holds totals).
    /// </summary>
    [TestFixture]
    public class ExcelTableFreezeAndFilterTests
    {
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            RdlEngineConfig.RdlEngineConfigInit();
            _folder = Path.Combine(Path.GetTempPath(), $"rdl-excel-freeze-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);
            File.WriteAllText(Path.Combine(_folder, "items.json"), """
                [{"Name":"Apple","Amount":3},{"Name":"Pear","Amount":5},{"Name":"Plum","Amount":7}]
                """);
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_folder, recursive: true); }
            catch (IOException) { }
        }

        private static string Cell(string name, string value) =>
            $"<TableCell><ReportItems><Textbox Name=\"{name}\"><Value>{value}</Value></Textbox></ReportItems></TableCell>";

        private static string Row(params string[] cells) =>
            $"<TableRow><Height>0.25in</Height><TableCells>{string.Concat(cells)}</TableCells></TableRow>";

        private static string Table(string name, string top, bool header = true, bool footer = true) => $"""
            <Table Name="{name}">
              <DataSetName>Items</DataSetName><Top>{top}</Top><Left>0in</Left>
              <TableColumns><TableColumn><Width>2in</Width></TableColumn><TableColumn><Width>1in</Width></TableColumn></TableColumns>
              {(header ? $"<Header><TableRows>{Row(Cell(name + "H1", "Name"), Cell(name + "H2", "Amount"))}</TableRows></Header>" : "")}
              <Details><TableRows>{Row(Cell(name + "D1", "=Fields!Name.Value"), Cell(name + "D2", "=Fields!Amount.Value"))}</TableRows></Details>
              {(footer ? $"<Footer><TableRows>{Row(Cell(name + "F1", "Total"), Cell(name + "F2", "=Sum(Fields!Amount.Value)"))}</TableRows></Footer>" : "")}
            </Table>
            """;

        private static string Rdl(string items) => $"""
            <?xml version="1.0" encoding="utf-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition">
              <PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight><Width>7.5in</Width>
              <DataSources><DataSource Name="Json">
                <ConnectionProperties><DataProvider>Json</DataProvider><ConnectString>file=items.json</ConnectString></ConnectionProperties>
              </DataSource></DataSources>
              <DataSets><DataSet Name="Items">
                <Query><DataSourceName>Json</DataSourceName><CommandText>columns=Name,Amount</CommandText></Query>
                <Fields><Field Name="Name"><DataField>Name</DataField></Field><Field Name="Amount"><DataField>Amount</DataField></Field></Fields>
              </DataSet></DataSets>
              <Body><Height>4in</Height><ReportItems>{items}</ReportItems></Body>
            </Report>
            """;

        private async Task<(string SheetXml, XlsxReadSheet Sheet)> Render(string rdl, OutputPresentationType type = OutputPresentationType.Excel2007)
        {
            using var report = await new RDLParser(rdl) { Folder = _folder }.Parse();
            report.Folder = _folder;
            await report.RunGetData(null);
            string path = Path.Combine(_folder, "out.xlsx");
            using (var sg = new OneFileStreamGen(path, true))
                await report.RunRender(sg, type);
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4),
                report.ErrorItems == null ? "" : string.Join("; ", report.ErrorItems.Cast<object>()));

            string xml;
            using (var zip = ZipFile.OpenRead(path))
            using (var reader = new StreamReader(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open()))
                xml = reader.ReadToEnd();
            using var xlsx = XlsxReader.Open(path);
            return (xml, xlsx.ReadSheet(0));
        }

        private static XlsxReadCell Find(XlsxReadSheet sheet, string text) => sheet.Cells.Single(c => c.Text == text);

        [TestCase(OutputPresentationType.Excel2007)]
        [TestCase(OutputPresentationType.Excel2007DataOnly)]
        public async Task SingleTable_FreezesBelowHeader_AndFiltersHeaderAndDetails(OutputPresentationType type)
        {
            var (xml, sheet) = await Render(Rdl(Table("T", "0.5in")), type);
            var nameHeader = Find(sheet, "Name");
            var amountHeader = Find(sheet, "Amount");
            var lastDetail = Find(sheet, "Plum");

            Assert.That(xml, Does.Contain($"ySplit=\"{nameHeader.Row + 1}\""));
            Assert.That(xml, Does.Contain("state=\"frozen\""));

            string expected = $"{Col(nameHeader.Column)}{nameHeader.Row + 1}:{Col(amountHeader.Column)}{lastDetail.Row + 1}";
            Assert.That(Regex.Match(xml, "<autoFilter ref=\"([^\"]+)\"").Groups[1].Value, Is.EqualTo(expected),
                "The filter covers the header and detail rows but not the footer's total");
        }

        [Test]
        public async Task TwoTables_GetNeitherFreezeNorFilter()
        {
            var (xml, _) = await Render(Rdl(Table("A", "0in") + Table("B", "2.5in")));
            Assert.That(xml, Does.Not.Contain("<pane"));
            Assert.That(xml, Does.Not.Contain("<autoFilter"));
        }

        [Test]
        public async Task TableWithoutHeader_GetsNeitherFreezeNorFilter()
        {
            var (xml, _) = await Render(Rdl(Table("T", "0in", header: false)));
            Assert.That(xml, Does.Not.Contain("<pane"));
            Assert.That(xml, Does.Not.Contain("<autoFilter"));
        }

        [Test]
        public async Task TableFarDownTheSheet_IsFilteredButNotFrozen()
        {
            var (xml, _) = await Render(Rdl(Table("T", "3.5in")));
            Assert.That(xml, Does.Not.Contain("<pane"));
            Assert.That(xml, Does.Contain("<autoFilter"));
        }

        private static string Col(int index)
        {
            string s = "";
            for (int n = index + 1; n > 0; n = (n - 1) / 26)
                s = (char)('A' + (n - 1) % 26) + s;
            return s;
        }
    }
}
