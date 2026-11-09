using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Spreadsheet;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using RdlEngine.Render.ExcelConverter;

namespace ReportTests
{
    /// <summary>
    /// Excel output writes numbers, dates and booleans as typed cells, with the textbox's .NET Format
    /// translated to an Excel number format, so the values can be summed and sorted in Excel. A format
    /// Excel can't express keeps the cell as the formatted text.
    /// </summary>
    [TestFixture]
    public class ExcelTypedCellsTests
    {
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            RdlEngineConfig.RdlEngineConfigInit();
            _folder = Path.Combine(Path.GetTempPath(), $"rdl-excel-typed-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_folder, recursive: true); }
            catch (IOException) { }
        }

        private static string Box(string name, double top, string value, string format = null) =>
            $"""
            <Textbox Name="{name}"><Top>{top.ToString(CultureInfo.InvariantCulture)}in</Top><Left>0in</Left><Width>2in</Width><Height>0.25in</Height>
              <Value>{System.Security.SecurityElement.Escape(value)}</Value>
              <Style>{(format == null ? "" : $"<Format>{System.Security.SecurityElement.Escape(format)}</Format>")}<Language>en-US</Language></Style>
            </Textbox>
            """;

        private static string Rdl(params string[] items) => $"""
            <?xml version="1.0" encoding="utf-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition">
              <PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight>
              <Width>7.5in</Width>
              <Body><Height>4in</Height><ReportItems>{string.Concat(items)}</ReportItems></Body>
            </Report>
            """;

        private async Task<XlsxReadSheet> Render(string rdl, OutputPresentationType type = OutputPresentationType.Excel2007)
        {
            using var report = await new RDLParser(rdl) { Folder = _folder }.Parse();
            report.Folder = _folder;
            await report.RunGetData(null);
            string path = Path.Combine(_folder, "out.xlsx");
            using (var sg = new OneFileStreamGen(path, true))
                await report.RunRender(sg, type);

            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4),
                report.ErrorItems == null ? "" : string.Join("; ", report.ErrorItems.Cast<object>()));
            using var reader = XlsxReader.Open(path);
            return reader.ReadSheet(0);
        }

        // The nth non-blank cell from the top; the layout may put spacer rows between textboxes.
        private static XlsxReadCell CellInRow(XlsxReadSheet sheet, int n) =>
            sheet.Cells.Where(c => c.Type != XlsxCellType.Blank).OrderBy(c => c.Row).ElementAt(n);

        [TestCase(OutputPresentationType.Excel2007)]
        [TestCase(OutputPresentationType.Excel2007DataOnly)]
        public async Task NumbersAndDates_AreTypedWithTranslatedFormats(OutputPresentationType type)
        {
            var sheet = await Render(Rdl(
                Box("Amount", 0, "=1234.5", "N2"),
                Box("Plain", 0.5, "=42"),
                Box("Day", 1, "=CDate(\"2024-03-15\")", "yyyy-MM-dd"),
                Box("Ratio", 1.5, "=0.25", "P0")), type);

            var amount = CellInRow(sheet, 0);
            Assert.That(amount.Type, Is.EqualTo(XlsxCellType.Number));
            Assert.That(amount.Number, Is.EqualTo(1234.5));
            Assert.That(amount.NumberFormatCode, Is.EqualTo("#,##0.00"));

            var plain = CellInRow(sheet, 1);
            Assert.That(plain.Number, Is.EqualTo(42));
            Assert.That(plain.NumberFormatCode, Is.Null.Or.EqualTo("General"));

            var day = CellInRow(sheet, 2);
            Assert.That(day.Type, Is.EqualTo(XlsxCellType.Date));
            Assert.That(day.DateValue, Is.EqualTo(new DateTime(2024, 3, 15)));
            Assert.That(day.NumberFormatCode, Is.EqualTo("yyyy-mm-dd"));

            var ratio = CellInRow(sheet, 3);
            Assert.That(ratio.Number, Is.EqualTo(0.25));
            Assert.That(ratio.NumberFormatCode, Is.EqualTo("0%"));
        }

        [Test]
        public async Task FormatsExcelCannotExpress_StayAsFormattedText()
        {
            var sheet = await Render(Rdl(
                Box("TwelveHourNoMarker", 0, "=CDate(\"2024-03-15 13:05\")", "h:mm"),
                Box("Text", 0.5, "Hello"),
                Box("HexNumber", 1, "=255", "X")));

            var time = CellInRow(sheet, 0);
            Assert.That(time.Type, Is.EqualTo(XlsxCellType.String));
            Assert.That(time.Text, Is.EqualTo("1:05"));
            Assert.That(CellInRow(sheet, 1).Text, Is.EqualTo("Hello"));
            Assert.That(CellInRow(sheet, 2).Type, Is.EqualTo(XlsxCellType.String));
        }

        [Test]
        public async Task CustomFormatWithLiteralText_KeepsTheTextInTheNumberFormat()
        {
            var sheet = await Render(Rdl(Box("Total", 0, "=99.5", "'Total: '0.00")));

            var total = CellInRow(sheet, 0);
            Assert.That(total.Type, Is.EqualTo(XlsxCellType.Number));
            Assert.That(total.NumberFormatCode, Is.EqualTo("\"Total: \"0.00"));
        }

        [TestCase("N0", "#,##0")]
        [TestCase("F3", "0.000")]
        [TestCase("D5", "00000")]
        [TestCase("E2", "0.00E+000")]
        [TestCase("#,##0.00;(#,##0.00);'-'", "#,##0.00;(#,##0.00);\"-\"")]
        [TestCase("0.0%", "0.0%")]
        public void NumberFormats_Translate(string netFormat, string excel)
        {
            Assert.That(ExcelValueConverter.TryNumberFormat(netFormat, CultureInfo.GetCultureInfo("en-US"), true, out string code), Is.True);
            Assert.That(code, Is.EqualTo(excel));
        }

        [TestCase("en-US", "\"$\"#,##0.00;-\"$\"#,##0.00")]
        [TestCase("de-DE", "#,##0.00\" \"\"€\";-#,##0.00\" \"\"€\"")]
        public void CurrencyFormat_UsesTheCulturesSymbolAndPattern(string culture, string excel)
        {
            Assert.That(ExcelValueConverter.TryNumberFormat("C", CultureInfo.GetCultureInfo(culture), false, out string code), Is.True);
            Assert.That(code, Is.EqualTo(excel));
        }

        [TestCase("d", "en-US", "m/d/yyyy")]
        [TestCase("d", "de-DE", "dd.mm.yyyy")]
        [TestCase("dd MMM yyyy HH:mm:ss", "en-US", "dd mmm yyyy hh:mm:ss")]
        [TestCase("hh:mm tt", "en-US", "hh:mm AM/PM")]
        [TestCase("HH:mm:ss.fff", "en-US", "hh:mm:ss.000")]
        public void DateFormats_Translate(string netFormat, string culture, string excel)
        {
            Assert.That(ExcelValueConverter.TryDateFormat(netFormat, CultureInfo.GetCultureInfo(culture), out string code), Is.True);
            Assert.That(code, Is.EqualTo(excel));
        }

        [TestCase("h:mm")]     // 12-hour without AM/PM
        [TestCase("mm")]       // minutes alone would be read as a month
        [TestCase("o")]        // round-trip with offset
        [TestCase("yyyy-MM-ddTHH:mm:sszzz")]
        public void DateFormats_ExcelCannotExpress_AreRejected(string netFormat)
        {
            Assert.That(ExcelValueConverter.TryDateFormat(netFormat, CultureInfo.GetCultureInfo("en-US"), out _), Is.False);
        }
    }
}
