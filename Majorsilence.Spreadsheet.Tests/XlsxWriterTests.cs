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
    public class XlsxWriterTests
    {
        private static MemoryStream WriteToStream(Action<XlsxWriter> build)
        {
            var writer = new XlsxWriter("Sheet1");
            build(writer);
            var ms = new MemoryStream();
            writer.Write(ms);
            ms.Position = 0;
            return ms;
        }

        private static void AssertValid(Stream xlsxStream)
        {
            xlsxStream.Position = 0;
            using var doc = SpreadsheetDocument.Open(xlsxStream, false);
            var validator = new OpenXmlValidator();
            var errors = validator.Validate(doc).ToList();
            Assert.That(errors, Is.Empty,
                string.Join(Environment.NewLine, errors.Select(e => $"{e.Description} ({e.Path?.XPath})")));
        }

        [Test]
        public void SimpleCells_ProduceValidWorkbook()
        {
            using var ms = WriteToStream(w =>
            {
                w.SetCell(0, 0, "Hello");
                w.SetCell(0, 1, "World");
            });

            AssertValid(ms);

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var sheetData = doc.WorkbookPart!.WorksheetParts.Single().Worksheet.Elements<SheetData>().Single();
            var cells = sheetData.Elements<Row>().Single().Elements<Cell>().ToList();
            Assert.That(cells[0].InnerText, Is.EqualTo("Hello"));
            Assert.That(cells[1].InnerText, Is.EqualTo("World"));
        }

        [Test]
        public void MergedCellsAndColumnWidths_RoundTrip()
        {
            using var ms = WriteToStream(w =>
            {
                w.SetColumnWidth(0, 20);
                w.SetCell(0, 0, "Merged");
                w.AddMerge(0, 0, 0, 2);
            });

            AssertValid(ms);

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var worksheet = doc.WorkbookPart!.WorksheetParts.Single().Worksheet;
            var merge = worksheet.Elements<MergeCells>().Single().Elements<MergeCell>().Single();
            Assert.That(merge.Reference!.Value, Is.EqualTo("A1:C1"));

            var col = worksheet.Elements<Columns>().Single().Elements<Column>().Single();
            Assert.That(col.Width!.Value, Is.EqualTo(20).Within(0.001));
        }

        [Test]
        public void CellFormat_AppliesFontAndBorder()
        {
            var format = new XlsxCellFormat
            {
                Bold = true,
                FontColor = (255, 0, 0),
                BorderBottom = new XlsxBorderSide { Style = "thin", Color = (0, 0, 0) },
            };

            using var ms = WriteToStream(w => w.SetCell(0, 0, "Styled", format));

            AssertValid(ms);

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var stylesPart = doc.WorkbookPart!.WorkbookStylesPart!.Stylesheet;
            Assert.That(stylesPart.Fonts!.Count(f => f is Font font && font.Bold != null), Is.GreaterThan(0));
            Assert.That(stylesPart.Borders!.Elements<Border>().Any(b => b.BottomBorder?.Style?.Value == BorderStyleValues.Thin), Is.True);
        }

        [Test]
        public void Image_IsEmbeddedAsDrawing()
        {
            byte[] fakeJpeg = { 0xFF, 0xD8, 0xFF, 0xD9 };

            using var ms = WriteToStream(w =>
            {
                w.SetCell(0, 0, "With image");
                w.AddImage(fakeJpeg, 0, 0, 10 * XlsxUnits.EmuPerPoint, 10 * XlsxUnits.EmuPerPoint);
            });

            AssertValid(ms);

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var worksheetPart = doc.WorkbookPart!.WorksheetParts.Single();
            Assert.That(worksheetPart.DrawingsPart, Is.Not.Null);
            Assert.That(worksheetPart.DrawingsPart!.ImageParts.Count(), Is.EqualTo(1));
        }

        [Test]
        public void PrintSetup_LandscapeIsPreserved()
        {
            using var ms = WriteToStream(w => w.SetPrintSetup(0.5, 0.5, 0.5, 0.5, landscape: true));

            AssertValid(ms);

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var pageSetup = doc.WorkbookPart!.WorksheetParts.Single().Worksheet.Elements<PageSetup>().Single();
            Assert.That(pageSetup.Orientation!.Value, Is.EqualTo(OrientationValues.Landscape));
        }
    }
}
