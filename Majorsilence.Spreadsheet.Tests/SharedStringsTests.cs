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
    public class SharedStringsTests
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
        public void Default_TextCellsStayInline_NoSharedStringsPart()
        {
            var writer = new XlsxWriter("Sheet1"); // UseSharedStrings defaults to false
            writer.SetCell(0, 0, "Hello");

            using var ms = new MemoryStream();
            writer.Write(ms);
            AssertValid(ms);

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            Assert.That(doc.WorkbookPart!.SharedStringTablePart, Is.Null);
            var cell = doc.WorkbookPart!.WorksheetParts.Single().Worksheet.Descendants<Cell>().Single();
            Assert.That(cell.DataType!.Value, Is.EqualTo(CellValues.InlineString));
        }

        [Test]
        public void UseSharedStrings_RepeatedText_DedupesIntoOneEntry()
        {
            var writer = new XlsxWriter("Sheet1") { UseSharedStrings = true };
            writer.SetCell(0, 0, "Repeated");
            writer.SetCell(1, 0, "Repeated");
            writer.SetCell(2, 0, "Unique");

            using var ms = new MemoryStream();
            writer.Write(ms);
            AssertValid(ms);

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var sst = doc.WorkbookPart!.SharedStringTablePart!.SharedStringTable;
            Assert.That(sst.Elements<SharedStringItem>().Count(), Is.EqualTo(2));
            Assert.That(sst.Count!.Value, Is.EqualTo(3u)); // 3 total references, 2 unique

            var cells = doc.WorkbookPart!.WorksheetParts.Single().Worksheet.Descendants<Cell>()
                .OrderBy(c => c.CellReference!.Value).ToList();
            Assert.That(cells.All(c => c.DataType!.Value == CellValues.SharedString), Is.True);
            Assert.That(cells[0].CellValue!.Text, Is.EqualTo(cells[1].CellValue!.Text)); // same shared index
        }

        [Test]
        public void UseSharedStrings_ResolvesCorrectTextThroughTheSharedStringTable()
        {
            var writer = new XlsxWriter("Sheet1") { UseSharedStrings = true };
            writer.SetCell(0, 0, "First");
            writer.SetCell(0, 1, "Second");

            using var ms = new MemoryStream();
            writer.Write(ms);
            AssertValid(ms);

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var sst = doc.WorkbookPart!.SharedStringTablePart!.SharedStringTable.Elements<SharedStringItem>().ToList();
            var cells = doc.WorkbookPart!.WorksheetParts.Single().Worksheet.Descendants<Cell>()
                .OrderBy(c => c.CellReference!.Value).ToList();

            string TextOf(Cell c) => sst[int.Parse(c.CellValue!.Text)].InnerText;
            Assert.That(TextOf(cells[0]), Is.EqualTo("First"));
            Assert.That(TextOf(cells[1]), Is.EqualTo("Second"));
        }
    }
}
