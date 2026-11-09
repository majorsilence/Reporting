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
    public class TypedCellsTests
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
            var errors = new OpenXmlValidator().Validate(doc).ToList();
            Assert.That(errors, Is.Empty,
                string.Join(Environment.NewLine, errors.Select(e => $"{e.Description} ({e.Path?.XPath})")));
        }

        private static Cell GetCell(Stream xlsxStream, string reference)
        {
            xlsxStream.Position = 0;
            using var doc = SpreadsheetDocument.Open(xlsxStream, false);
            var sheetData = doc.WorkbookPart!.WorksheetParts.Single().Worksheet.Elements<SheetData>().Single();
            return sheetData.Descendants<Cell>().Single(c => c.CellReference == reference);
        }

        [Test]
        public void NumberCell_HasNoTypeAttributeAndNumericValue()
        {
            using var ms = WriteToStream(w => w.SetCell(0, 0, 42.5));
            AssertValid(ms);

            var cell = GetCell(ms, "A1");
            Assert.That(cell.DataType, Is.Null);
            Assert.That(double.Parse(cell.CellValue!.Text, System.Globalization.CultureInfo.InvariantCulture), Is.EqualTo(42.5));
        }

        [Test]
        public void BooleanCell_IsWrittenAsBooleanType()
        {
            using var ms = WriteToStream(w =>
            {
                w.SetCell(0, 0, true);
                w.SetCell(0, 1, false);
            });
            AssertValid(ms);

            Assert.That(GetCell(ms, "A1").DataType!.Value, Is.EqualTo(CellValues.Boolean));
            Assert.That(GetCell(ms, "A1").CellValue!.Text, Is.EqualTo("1"));
            Assert.That(GetCell(ms, "B1").CellValue!.Text, Is.EqualTo("0"));
        }

        [Test]
        public void DateCell_IsWrittenAsSerialNumberWithDefaultDateFormat()
        {
            var date = new DateTime(2026, 1, 15);
            using var ms = WriteToStream(w => w.SetCell(0, 0, date));
            AssertValid(ms);

            var cell = GetCell(ms, "A1");
            Assert.That(cell.DataType, Is.Null);
            // Excel epoch is 1899-12-30; 2026-01-15 is a known serial value.
            double serial = double.Parse(cell.CellValue!.Text, System.Globalization.CultureInfo.InvariantCulture);
            Assert.That(serial, Is.EqualTo((date - new DateTime(1899, 12, 30)).TotalDays).Within(0.0001));

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var stylesPart = doc.WorkbookPart!.WorkbookStylesPart!.Stylesheet;
            var xf = stylesPart.CellFormats!.Elements<CellFormat>().ElementAt((int)cell.StyleIndex!.Value);
            var numFmt = stylesPart.NumberingFormats!.Elements<NumberingFormat>()
                .Single(nf => nf.NumberFormatId!.Value == xf.NumberFormatId!.Value);
            Assert.That(numFmt.FormatCode!.Value, Is.EqualTo(XlsxNumberFormats.ShortDate));
        }

        [Test]
        public void CustomNumberFormat_RoundTripsThroughStyles()
        {
            var format = new XlsxCellFormat { NumberFormatCode = XlsxNumberFormats.Currency };
            using var ms = WriteToStream(w => w.SetCell(0, 0, 1234.5, format));
            AssertValid(ms);

            var cell = GetCell(ms, "A1");
            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var stylesPart = doc.WorkbookPart!.WorkbookStylesPart!.Stylesheet;
            var xf = stylesPart.CellFormats!.Elements<CellFormat>().ElementAt((int)cell.StyleIndex!.Value);
            var numFmt = stylesPart.NumberingFormats!.Elements<NumberingFormat>()
                .Single(nf => nf.NumberFormatId!.Value == xf.NumberFormatId!.Value);
            Assert.That(numFmt.FormatCode!.Value, Is.EqualTo(XlsxNumberFormats.Currency));
            Assert.That(numFmt.NumberFormatId!.Value, Is.GreaterThanOrEqualTo(164u));
        }

        [Test]
        public void TextCell_StillWritesInlineStringUnchanged()
        {
            using var ms = WriteToStream(w => w.SetCell(0, 0, "Hello"));
            AssertValid(ms);

            var cell = GetCell(ms, "A1");
            Assert.That(cell.DataType!.Value, Is.EqualTo(CellValues.InlineString));
            Assert.That(cell.InlineString!.Text!.Text, Is.EqualTo("Hello"));
        }
    }
}
