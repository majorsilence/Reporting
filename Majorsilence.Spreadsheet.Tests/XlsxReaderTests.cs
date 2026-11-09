using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using NUnit.Framework;

namespace Majorsilence.Spreadsheet.Tests
{
    [TestFixture]
    public class XlsxReaderTests
    {
        private static MemoryStream WriteToStream(XlsxWriter writer)
        {
            var ms = new MemoryStream();
            writer.Write(ms);
            ms.Position = 0;
            return ms;
        }

        [Test]
        public void RoundTrip_TypedCellsFromXlsxWriter()
        {
            var writer = new XlsxWriter("Data");
            writer.SetCell(0, 0, "Hello");
            writer.SetCell(0, 1, 42.5);
            writer.SetCell(0, 2, true);
            writer.SetCell(0, 3, new DateTime(2024, 3, 15));
            writer.SetCell(1, 27, new DateTime(2024, 3, 15, 13, 30, 0));

            using var ms = WriteToStream(writer);
            using var reader = XlsxReader.Open(ms);
            Assert.That(reader.SheetNames, Is.EqualTo(new[] { "Data" }));

            var sheet = reader.ReadSheet(0);
            Assert.That(sheet.GetCell(0, 0)!.Type, Is.EqualTo(XlsxCellType.String));
            Assert.That(sheet.GetCell(0, 0)!.Text, Is.EqualTo("Hello"));
            Assert.That(sheet.GetCell(0, 1)!.Type, Is.EqualTo(XlsxCellType.Number));
            Assert.That(sheet.GetCell(0, 1)!.Number, Is.EqualTo(42.5));
            Assert.That(sheet.GetCell(0, 2)!.Boolean, Is.True);

            var date = sheet.GetCell(0, 3)!;
            Assert.That(date.Type, Is.EqualTo(XlsxCellType.Date));
            Assert.That(date.DateValue, Is.EqualTo(new DateTime(2024, 3, 15)));
            Assert.That(date.NumberFormatCode, Is.EqualTo(XlsxNumberFormats.ShortDate));

            Assert.That(sheet.GetCell(1, 27)!.DateValue, Is.EqualTo(new DateTime(2024, 3, 15, 13, 30, 0)));
            Assert.That(sheet.RowCount, Is.EqualTo(2));
            Assert.That(sheet.ColumnCount, Is.EqualTo(28));
            Assert.That(sheet.GetCell(5, 5), Is.Null);
        }

        [Test]
        public void RoundTrip_SharedStringsRichTextAndFormulas()
        {
            var writer = new XlsxWriter("Sheet1") { UseSharedStrings = true };
            writer.SetCell(0, 0, "Repeated");
            writer.SetCell(1, 0, "Repeated");
            writer.SetRichText(2, 0, new[] { new XlsxTextRun("Bold ") { Bold = true }, new XlsxTextRun("plain") });
            writer.SetFormula(3, 0, "SUM(B1:B2)", 7);

            using var ms = WriteToStream(writer);
            using var reader = XlsxReader.Open(ms);
            var sheet = reader.ReadSheet("sheet1");

            Assert.That(sheet.GetCell(0, 0)!.Text, Is.EqualTo("Repeated"));
            Assert.That(sheet.GetCell(1, 0)!.Text, Is.EqualTo("Repeated"));
            Assert.That(sheet.GetCell(2, 0)!.Text, Is.EqualTo("Bold plain"));

            var formula = sheet.GetCell(3, 0)!;
            Assert.That(formula.Formula, Is.EqualTo("SUM(B1:B2)"));
            Assert.That(formula.Number, Is.EqualTo(7));
        }

        [Test]
        public void RoundTrip_MultipleSheetsAndMerges()
        {
            var writer = new XlsxWriter("First");
            int second = writer.AddSheet("Second");
            writer.SetCell(0, 0, "on first");
            writer.SetCell(0, 0, "on second", sheetIndex: second);
            writer.AddMerge(1, 1, 2, 3, sheetIndex: second);

            using var ms = WriteToStream(writer);
            using var reader = XlsxReader.Open(ms);
            Assert.That(reader.SheetNames, Is.EqualTo(new[] { "First", "Second" }));
            Assert.That(reader.ReadSheet(0).GetCell(0, 0)!.Text, Is.EqualTo("on first"));

            var sheet = reader.ReadSheet("Second");
            Assert.That(sheet.GetCell(0, 0)!.Text, Is.EqualTo("on second"));
            var merge = sheet.Merges.Single();
            Assert.That((merge.FirstRow, merge.FirstColumn, merge.LastRow, merge.LastColumn), Is.EqualTo((1, 1, 2, 3)));
        }

        [Test]
        public void ReadsFileProducedByOpenXmlSdk_WithOtherCellTypesAnd1904Dates()
        {
            using var ms = new MemoryStream();
            using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
            {
                var wbPart = doc.AddWorkbookPart();
                wbPart.Workbook = new Workbook(new WorkbookProperties { Date1904 = true }, new Sheets());

                var styles = wbPart.AddNewPart<WorkbookStylesPart>();
                styles.Stylesheet = new Stylesheet(
                    new NumberingFormats(new NumberingFormat { NumberFormatId = 164, FormatCode = "\"Day\" dd/mm/yyyy" },
                        new NumberingFormat { NumberFormatId = 165, FormatCode = "[Red]0.00;\"neg\"" }),
                    new Fonts(new Font()) { Count = 1 },
                    new Fills(new Fill()) { Count = 1 },
                    new Borders(new Border()) { Count = 1 },
                    new CellFormats(new CellFormat(), new CellFormat { NumberFormatId = 164 },
                        new CellFormat { NumberFormatId = 165 }, new CellFormat { NumberFormatId = 46 }) { Count = 4 });

                var wsPart = wbPart.AddNewPart<WorksheetPart>();
                wsPart.Worksheet = new Worksheet(new SheetData(
                    new Row(
                        // no r attributes: positions are implied by order
                        new Cell { DataType = CellValues.String, CellFormula = new CellFormula("\"a\"&\"b\""), CellValue = new CellValue("ab") },
                        new Cell { DataType = CellValues.Error, CellValue = new CellValue("#DIV/0!") },
                        new Cell { StyleIndex = 1, CellValue = new CellValue("0") },
                        new Cell { StyleIndex = 2, CellValue = new CellValue("1.5") },
                        new Cell { StyleIndex = 3, CellValue = new CellValue("1.25") },
                        new Cell { DataType = CellValues.Date, CellValue = new CellValue("2020-02-29T06:00:00") },
                        new Cell { StyleIndex = 1 })));
                wbPart.Workbook.Sheets!.Append(new Sheet { Id = wbPart.GetIdOfPart(wsPart), SheetId = 1, Name = "SDK" });
                wbPart.Workbook.Save();
            }

            ms.Position = 0;
            using var reader = XlsxReader.Open(ms);
            var cells = reader.ReadSheet(0).Cells;
            Assert.That(cells.Select(c => (c.Row, c.Column)), Is.EqualTo(Enumerable.Range(0, 7).Select(i => (0, i))));

            Assert.That(cells[0].Type, Is.EqualTo(XlsxCellType.String));
            Assert.That(cells[0].Text, Is.EqualTo("ab"));
            Assert.That(cells[0].Formula, Is.EqualTo("\"a\"&\"b\""));

            Assert.That(cells[1].Type, Is.EqualTo(XlsxCellType.Error));
            Assert.That(cells[1].Text, Is.EqualTo("#DIV/0!"));

            Assert.That(cells[2].Type, Is.EqualTo(XlsxCellType.Date));
            Assert.That(cells[2].DateValue, Is.EqualTo(new DateTime(1904, 1, 1)));

            Assert.That(cells[3].Type, Is.EqualTo(XlsxCellType.Number)); // quoted text and [Red] are not date tokens
            Assert.That(cells[3].NumberFormatCode, Is.EqualTo("[Red]0.00;\"neg\""));

            Assert.That(cells[4].Type, Is.EqualTo(XlsxCellType.Date)); // [h]:mm:ss elapsed time
            Assert.That(cells[4].DateValue, Is.EqualTo(new DateTime(1904, 1, 2, 6, 0, 0)));

            Assert.That(cells[5].Type, Is.EqualTo(XlsxCellType.Date));
            Assert.That(cells[5].DateValue, Is.EqualTo(new DateTime(2020, 2, 29, 6, 0, 0)));

            Assert.That(cells[6].Type, Is.EqualTo(XlsxCellType.Blank));
        }

        [Test]
        public void ReadSheet_UnknownNameOrIndex_Throws()
        {
            using var ms = WriteToStream(new XlsxWriter("Only"));
            using var reader = XlsxReader.Open(ms);
            Assert.Throws<ArgumentException>(() => reader.ReadSheet("Missing"));
            Assert.Throws<ArgumentOutOfRangeException>(() => reader.ReadSheet(1));
        }

        [Test]
        public void Open_NonPackageStream_Throws()
        {
            using var ms = new MemoryStream(new byte[] { 1, 2, 3, 4 });
            Assert.Throws<InvalidDataException>(() => XlsxReader.Open(ms));
        }

        [Test]
        public void Open_FromPath_ReadsAndReleasesFile()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".xlsx");
            try
            {
                var writer = new XlsxWriter("Sheet1");
                writer.SetCell(0, 0, "from disk");
                using (var fs = File.Create(path))
                    writer.Write(fs);

                using (var reader = XlsxReader.Open(path))
                    Assert.That(reader.ReadSheet(0).GetCell(0, 0)!.Text, Is.EqualTo("from disk"));

                File.Delete(path); // throws on Windows if the reader still held the file
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
