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
    public class XlsxStreamWriterTests
    {
        private static void AssertValid(MemoryStream ms)
        {
            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var errors = new OpenXmlValidator().Validate(doc).ToList();
            Assert.That(errors, Is.Empty,
                string.Join(Environment.NewLine, errors.Select(e => $"{e.Description} ({e.Path?.XPath})")));
            ms.Position = 0;
        }

        [Test]
        public void TypedCellsStylesAndSheetSettings_RoundTrip()
        {
            var bold = new XlsxCellFormat { Bold = true };
            var money = new XlsxCellFormat { NumberFormatCode = XlsxNumberFormats.ThousandsTwoDecimal };
            using var ms = new MemoryStream();
            using (var w = new XlsxStreamWriter(ms, "Data"))
            {
                w.SetColumnWidth(0, 20);
                w.FreezePanes(1, 0);
                w.StartRow(0);
                w.WriteCell(0, "Name", bold);
                w.WriteCell(1, "Amount", bold);
                w.WriteCell(2, "When", bold);
                w.WriteCell(3, "Paid", bold);
                w.StartRow(1, heightInPoints: 18);
                w.WriteCell(0, "Apple & <Pear>");
                w.WriteCell(1, 1234.5, money);
                w.WriteCell(2, new DateTime(2024, 3, 15));
                w.WriteCell(3, true);
                w.StartRow(3);
                w.WriteFormula(1, "=SUM(B2:B2)", 1234.5, money);
                w.AddMerge(3, 2, 3, 3);
                w.SetAutoFilter(0, 0, 1, 3);
                w.SetPrintSetup(0.5, 0.5, 0.5, 0.5, landscape: true);
            }
            AssertValid(ms);

            using var reader = XlsxReader.Open(ms);
            var sheet = reader.ReadSheet("Data");
            Assert.That(sheet.GetCell(1, 0)!.Text, Is.EqualTo("Apple & <Pear>"));
            Assert.That(sheet.GetCell(1, 1)!.DisplayText, Is.EqualTo("1,234.50"));
            Assert.That(sheet.GetCell(1, 2)!.DateValue, Is.EqualTo(new DateTime(2024, 3, 15)));
            Assert.That(sheet.GetCell(1, 3)!.Boolean, Is.True);
            Assert.That(sheet.GetCell(3, 1)!.Formula, Is.EqualTo("SUM(B2:B2)"));
            Assert.That(sheet.Merges.Single().FirstColumn, Is.EqualTo(2));

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var ws = doc.WorkbookPart!.WorksheetParts.Single().Worksheet;
            Assert.That(ws.Descendants<Pane>().Single().VerticalSplit!.Value, Is.EqualTo(1));
            Assert.That(ws.Descendants<Column>().Single().Width!.Value, Is.EqualTo(20));
            Assert.That(ws.Descendants<AutoFilter>().Single().Reference!.Value, Is.EqualTo("A1:D2"));
            var headerStyles = ws.Descendants<Row>().First().Elements<Cell>().Select(c => c.StyleIndex!.Value).Distinct();
            Assert.That(headerStyles.Count(), Is.EqualTo(1), "one format instance resolves to one style");
        }

        [Test]
        public void MultipleSheets_AndAnEmptySheet_AreValid()
        {
            using var ms = new MemoryStream();
            using (var w = new XlsxStreamWriter(ms, "First"))
            {
                w.StartRow(0);
                w.WriteCell(0, 1.0);
                Assert.That(w.StartSheet("Empty"), Is.EqualTo(1));
                w.StartSheet("Third");
                w.SetColumnWidth(1, 9);
                w.StartRow(4);
                w.WriteCell(1, "x");
            }
            AssertValid(ms);

            using var reader = XlsxReader.Open(ms);
            Assert.That(reader.SheetNames, Is.EqualTo(new[] { "First", "Empty", "Third" }));
            Assert.That(reader.ReadSheet(1).Cells, Is.Empty);
            Assert.That(reader.ReadSheet(2).GetCell(4, 1)!.Text, Is.EqualTo("x"));
        }

        [Test]
        public void OutOfOrderRowsCellsAndLateSheetSettings_Throw()
        {
            using var ms = new MemoryStream();
            using var w = new XlsxStreamWriter(ms, "Sheet1");
            Assert.Throws<InvalidOperationException>(() => w.WriteCell(0, "no row"));
            w.StartRow(2);
            w.WriteCell(1, "b");
            Assert.Throws<InvalidOperationException>(() => w.WriteCell(1, "same column"));
            Assert.Throws<InvalidOperationException>(() => w.StartRow(2));
            Assert.Throws<InvalidOperationException>(() => w.SetColumnWidth(0, 10));
            Assert.Throws<InvalidOperationException>(() => w.FreezePanes(1, 0));
        }

        [Test]
        public void Rows_AreWrittenToTheOutputAsTheyAreAdded()
        {
            using var ms = new MemoryStream();
            var w = new XlsxStreamWriter(ms, "Big");
            for (int r = 0; r < 50000; r++)
            {
                w.StartRow(r);
                w.WriteCell(0, "row " + r);
                w.WriteCell(1, r * 1.5);
            }
            long beforeFinish = ms.Length;
            w.Dispose();

            Assert.That(beforeFinish, Is.GreaterThan(ms.Length / 2), "most of the file was written before Finish");
            AssertValid(ms);
            using var reader = XlsxReader.Open(ms);
            Assert.That(reader.ReadSheet(0).RowCount, Is.EqualTo(50000));
        }

        [Test]
        public void UsingAfterFinish_Throws()
        {
            using var ms = new MemoryStream();
            var w = new XlsxStreamWriter(ms, "Sheet1");
            w.Finish();
            Assert.Throws<ObjectDisposedException>(() => w.StartRow(0));
            Assert.DoesNotThrow(() => w.Dispose());
        }
    }
}
