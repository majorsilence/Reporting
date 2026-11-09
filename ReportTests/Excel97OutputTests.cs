using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NUnit.Framework;
using RdlEngine.Render.ExcelConverter;

namespace ReportTests
{
    /// <summary>
    /// The Excel97 output type writes a legacy .xls (BIFF8) workbook through the same layout as the
    /// .xlsx export; the output is read back with NPOI's independent HSSF reader.
    /// </summary>
    [TestFixture]
    public class Excel97OutputTests
    {
        private string _folder;

        // A 1x1 PNG.
        private const string Png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

        [SetUp]
        public void SetUp()
        {
            RdlEngineConfig.RdlEngineConfigInit();
            _folder = Path.Combine(Path.GetTempPath(), $"rdl-excel97-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);
            File.WriteAllText(Path.Combine(_folder, "items.json"), """
                [{"Name":"Apple","Amount":3.5},{"Name":"Pear","Amount":1234.25},{"Name":"Plum","Amount":7}]
                """);
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_folder, recursive: true); }
            catch (IOException) { }
        }

        private static string Cell(string name, string value, string style = "") =>
            $"<TableCell><ReportItems><Textbox Name=\"{name}\"><Value>{value}</Value><Style>{style}</Style></Textbox></ReportItems></TableCell>";

        private static string Row(params string[] cells) =>
            $"<TableRow><Height>0.25in</Height><TableCells>{string.Concat(cells)}</TableCells></TableRow>";

        private static string Rdl(string extraItems = "") => $"""
            <?xml version="1.0" encoding="utf-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition">
              <Name>Fruit sales</Name>
              <PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight><Width>7.5in</Width>
              <EmbeddedImages>
                <EmbeddedImage Name="Dot"><MIMEType>image/png</MIMEType><ImageData>{Png}</ImageData></EmbeddedImage>
              </EmbeddedImages>
              <DataSources><DataSource Name="Json">
                <ConnectionProperties><DataProvider>Json</DataProvider><ConnectString>file=items.json</ConnectString></ConnectionProperties>
              </DataSource></DataSources>
              <DataSets><DataSet Name="Items">
                <Query><DataSourceName>Json</DataSourceName><CommandText>columns=Name,Amount</CommandText></Query>
                <Fields><Field Name="Name"><DataField>Name</DataField></Field><Field Name="Amount"><DataField>Amount</DataField></Field></Fields>
              </DataSet></DataSets>
              <Body><Height>4in</Height><ReportItems>
                <Textbox Name="Printed"><Top>0in</Top><Left>0in</Left><Width>2in</Width><Height>0.25in</Height>
                  <Value>=CDate("2024-03-15")</Value><Style><Format>yyyy-MM-dd</Format><Language>en-US</Language></Style></Textbox>
                <Table Name="T">
                  <DataSetName>Items</DataSetName><Top>0.5in</Top><Left>0in</Left>
                  <TableColumns><TableColumn><Width>2in</Width></TableColumn><TableColumn><Width>1in</Width></TableColumn></TableColumns>
                  <Header><TableRows>{Row(Cell("H1", "Name", "<FontWeight>Bold</FontWeight><BackgroundColor>LightBlue</BackgroundColor>"), Cell("H2", "Amount", "<FontWeight>Bold</FontWeight>"))}</TableRows></Header>
                  <Details><TableRows>{Row(Cell("D1", "=Fields!Name.Value"), Cell("D2", "=CDbl(Fields!Amount.Value)", "<Format>N2</Format><Language>en-US</Language>"))}</TableRows></Details>
                </Table>
                {extraItems}
              </ReportItems></Body>
            </Report>
            """;

        private async Task<(HSSFWorkbook Workbook, byte[] Bytes, Report Report)> Render(string rdl)
        {
            var report = await new RDLParser(rdl) { Folder = _folder }.Parse();
            report.Folder = _folder;
            await report.RunGetData(null);
            string path = Path.Combine(_folder, "out.xls");
            using (var sg = new OneFileStreamGen(path, true))
                await report.RunRender(sg, OutputPresentationType.Excel97);
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4),
                report.ErrorItems == null ? "" : string.Join("; ", report.ErrorItems.Cast<object>()));

            byte[] bytes = File.ReadAllBytes(path);
            return (new HSSFWorkbook(new MemoryStream(bytes)), bytes, report);
        }

        private static ICell Find(ISheet sheet, Func<ICell, bool> match)
        {
            for (int r = sheet.FirstRowNum; r <= sheet.LastRowNum; r++)
            {
                var row = sheet.GetRow(r);
                if (row == null) continue;
                foreach (var c in row.Cells)
                    if (match(c)) return c;
            }
            throw new AssertionException("cell not found");
        }

        private static ICell Text(ISheet sheet, string text) =>
            Find(sheet, c => c.CellType == CellType.String && c.StringCellValue == text);

        [Test]
        public async Task Report_RendersAsBiff8WorkbookWithTypedStyledCells()
        {
            var (wb, bytes, report) = await Render(Rdl());
            using var _ = report;
            using var __ = wb;

            Assert.That(bytes.Take(8), Is.EqualTo(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }), "an OLE2 compound file");
            var sheet = wb.GetSheetAt(0);
            Assert.That(wb.GetSheetName(0), Is.EqualTo("Fruit sales"));

            var header = Text(sheet, "Name");
            Assert.That(((HSSFCellStyle)header.CellStyle).GetFont(wb).IsBold, Is.True);
            Assert.That(header.CellStyle.FillPattern, Is.EqualTo(FillPattern.SolidForeground));

            var amount = Find(sheet, c => c.CellType == CellType.Numeric && c.NumericCellValue == 1234.25);
            Assert.That(amount.CellStyle.GetDataFormatString(), Is.EqualTo("#,##0.00"));

            var printed = Find(sheet, c => c.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(c));
            Assert.That(DateUtil.GetJavaDate(printed.NumericCellValue), Is.EqualTo(new DateTime(2024, 3, 15)));
            Assert.That(printed.CellStyle.GetDataFormatString(), Is.EqualTo("yyyy-mm-dd"));

            Assert.That(sheet.PaneInformation?.IsFreezePane(), Is.True, "the table header is frozen, as in .xlsx");
            Assert.That(sheet.PaneInformation!.HorizontalSplitTopRow, Is.EqualTo(header.RowIndex + 1));
        }

        [Test]
        public async Task ImagesAndLines_AreLeftOutWithAWarning()
        {
            var (wb, _, report) = await Render(Rdl("""
                <Image Name="Logo"><Source>Embedded</Source><Value>Dot</Value><Top>3in</Top><Left>0in</Left><Width>0.5in</Width><Height>0.5in</Height></Image>
                """));
            using var _r = report;
            using var _w = wb;
            var messages = report.ErrorItems?.Cast<object>().Select(o => o.ToString()).ToList() ?? new List<string>();
            Assert.That(messages, Has.Some.Contains("not included in Excel 97-2003"));
            Assert.That(Text(wb.GetSheetAt(0), "Apple"), Is.Not.Null);
        }

        [Test]
        public void CellsBeyondBiff8Limits_AreDroppedWithOneWarning()
        {
            var warnings = new List<string>();
            var workbook = new XlsWorkbook("Sheet1", warnings.Add);
            workbook.SetCell(0, 0, "kept", null);
            workbook.SetCell(70000, 0, "too far down", null);
            workbook.SetCell(0, 300, 1.0, null);
            workbook.AddMerge(65530, 0, 65540, 1);

            var ms = new MemoryStream();
            workbook.Write(ms);
            ms.Position = 0;
            using var wb = new HSSFWorkbook(ms);
            Assert.That(wb.GetSheetAt(0).GetRow(0).GetCell(0).StringCellValue, Is.EqualTo("kept"));
            Assert.That(wb.GetSheetAt(0).NumMergedRegions, Is.EqualTo(0));
            Assert.That(warnings, Has.Count.EqualTo(1));
            Assert.That(warnings[0], Does.Contain("65,536 rows"));
        }

        [Test]
        public async Task RenderFormatName_IsExcel()
        {
            var (wb, _, report) = await Render(Rdl("""
                <Textbox Name="Format"><Top>3in</Top><Left>0in</Left><Width>2in</Width><Height>0.25in</Height><Value>=Globals!RenderFormat.Name</Value></Textbox>
                """));
            using var _r = report;
            using var _w = wb;
            Assert.That(Text(wb.GetSheetAt(0), "EXCEL"), Is.Not.Null, "SSRS calls its .xls renderer EXCEL");
        }
    }
}
