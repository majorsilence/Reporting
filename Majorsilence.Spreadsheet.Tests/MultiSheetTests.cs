using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Validation;
using NUnit.Framework;

namespace Majorsilence.Spreadsheet.Tests
{
    [TestFixture]
    public class MultiSheetTests
    {
        private static void AssertValid(Stream xlsxStream)
        {
            xlsxStream.Position = 0;
            using var doc = SpreadsheetDocument.Open(xlsxStream, false);
            var errors = new OpenXmlValidator().Validate(doc).ToList();
            Assert.That(errors, Is.Empty,
                string.Join(Environment.NewLine, errors.Select(e => $"{e.Description} ({e.Path?.XPath})")));
        }

        [Test]
        public void AddSheet_ReturnsSequentialIndexesStartingAtOne()
        {
            var writer = new XlsxWriter("First");
            int second = writer.AddSheet("Second");
            int third = writer.AddSheet("Third");

            Assert.That(second, Is.EqualTo(1));
            Assert.That(third, Is.EqualTo(2));
        }

        [Test]
        public void MultipleSheets_EachKeepsItsOwnCellsAndName()
        {
            var writer = new XlsxWriter("First");
            int second = writer.AddSheet("Second");

            writer.SetCell(0, 0, "on sheet 1"); // sheetIndex defaults to 0
            writer.SetCell(0, 0, "on sheet 2", sheetIndex: second);

            using var ms = new MemoryStream();
            writer.Write(ms);
            ms.Position = 0;
            AssertValid(ms);

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var sheets = doc.WorkbookPart!.Workbook.Sheets!.Elements<Sheet>().ToList();
            Assert.That(sheets.Select(s => s.Name!.Value), Is.EqualTo(new[] { "First", "Second" }));

            var worksheetParts = doc.WorkbookPart!.WorksheetParts.ToList();
            Assert.That(worksheetParts, Has.Count.EqualTo(2));

            string TextOf(WorksheetPart part) => part.Worksheet.Descendants<InlineString>().Single().Text!.Text;
            var firstPart = (WorksheetPart)doc.WorkbookPart!.GetPartById(((Sheet)sheets[0]).Id!.Value!);
            var secondPart = (WorksheetPart)doc.WorkbookPart!.GetPartById(((Sheet)sheets[1]).Id!.Value!);
            Assert.That(TextOf(firstPart), Is.EqualTo("on sheet 1"));
            Assert.That(TextOf(secondPart), Is.EqualTo("on sheet 2"));
        }

        [Test]
        public void MultipleSheets_ImagesOnDifferentSheetsGetGloballyUniqueMediaNames()
        {
            var writer = new XlsxWriter("First");
            int second = writer.AddSheet("Second");
            byte[] fakeJpeg = { 0xFF, 0xD8, 0xFF, 0xD9 };

            writer.AddImage(fakeJpeg, 0, 0, 1000, 1000);
            writer.AddImage(fakeJpeg, 0, 0, 1000, 1000, sheetIndex: second);
            writer.AddImage(fakeJpeg, 0, 0, 1000, 1000, sheetIndex: second);

            using var ms = new MemoryStream();
            writer.Write(ms);
            ms.Position = 0;
            AssertValid(ms);

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var allImageParts = doc.WorkbookPart!.WorksheetParts
                .Select(wp => wp.DrawingsPart)
                .Where(dp => dp != null)
                .SelectMany(dp => dp!.ImageParts)
                .ToList();
            Assert.That(allImageParts, Has.Count.EqualTo(3));
        }

        [Test]
        public void StylesAreSharedAndDedupedAcrossSheets()
        {
            var writer = new XlsxWriter("First");
            int second = writer.AddSheet("Second");
            var format = new XlsxCellFormat { Bold = true };

            writer.SetCell(0, 0, "a", format);
            writer.SetCell(0, 0, "b", format, sheetIndex: second);

            using var ms = new MemoryStream();
            writer.Write(ms);
            ms.Position = 0;
            AssertValid(ms);

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var cells = doc.WorkbookPart!.WorksheetParts.SelectMany(wp => wp.Worksheet.Descendants<Cell>()).ToList();
            Assert.That(cells[0].StyleIndex!.Value, Is.EqualTo(cells[1].StyleIndex!.Value));
        }
    }
}
