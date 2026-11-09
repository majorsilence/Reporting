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
    public class OutlineAndHiddenTests
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
        public void HiddenRowWithoutAnyCell_StillGetsARowElement()
        {
            var writer = new XlsxWriter("Sheet1");
            writer.SetCell(0, 0, "visible");
            writer.SetRowHidden(1, true); // row 2 has no cells at all

            using var ms = new MemoryStream();
            writer.Write(ms);
            AssertValid(ms);

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var rows = doc.WorkbookPart!.WorksheetParts.Single().Worksheet.Descendants<Row>().ToList();
            var hiddenRow = rows.Single(r => r.RowIndex!.Value == 2);
            Assert.That(hiddenRow.Hidden!.Value, Is.True);
            Assert.That(hiddenRow.Elements<Cell>().Any(), Is.False);
        }

        [Test]
        public void RowOutlineLevel_IsWritten()
        {
            var writer = new XlsxWriter("Sheet1");
            writer.SetCell(0, 0, "a");
            writer.SetRowOutlineLevel(0, 2);

            using var ms = new MemoryStream();
            writer.Write(ms);
            AssertValid(ms);

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var row = doc.WorkbookPart!.WorksheetParts.Single().Worksheet.Descendants<Row>().Single();
            Assert.That(row.OutlineLevel!.Value, Is.EqualTo(2));
        }

        [Test]
        public void ColumnOutlineLevelAndHidden_WithoutExplicitWidth_StillGetsAColElement()
        {
            var writer = new XlsxWriter("Sheet1");
            writer.SetColumnOutlineLevel(3, 1);
            writer.SetColumnHidden(3, true);

            using var ms = new MemoryStream();
            writer.Write(ms);
            AssertValid(ms);

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var col = doc.WorkbookPart!.WorksheetParts.Single().Worksheet.Descendants<Column>().Single();
            Assert.That(col.Min!.Value, Is.EqualTo(4)); // 0-based index 3 -> 1-based column 4
            Assert.That(col.OutlineLevel!.Value, Is.EqualTo((byte)1));
            Assert.That(col.Hidden!.Value, Is.True);
            Assert.That(col.CustomWidth, Is.Null);
        }

        [Test]
        public void SetColumnHidden_False_UnhidesAColumnThatWasHidden()
        {
            var writer = new XlsxWriter("Sheet1");
            writer.SetColumnHidden(0, true);
            writer.SetColumnHidden(0, false);
            writer.SetColumnOutlineLevel(0, 1); // keep the <col> entry around to inspect

            using var ms = new MemoryStream();
            writer.Write(ms);
            AssertValid(ms);

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var col = doc.WorkbookPart!.WorksheetParts.Single().Worksheet.Descendants<Column>().Single();
            Assert.That(col.Hidden, Is.Null);
        }
    }
}
