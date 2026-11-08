using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Validation;
using NUnit.Framework;

namespace Majorsilence.Excel.Tests
{
    [TestFixture]
    public class FreezePanesAndAutoFilterTests
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

        [Test]
        public void FreezePanes_TopRowOnly_WritesFrozenPaneWithBottomLeftActivePane()
        {
            using var ms = WriteToStream(w =>
            {
                w.SetCell(0, 0, "header");
                w.FreezePanes(frozenRows: 1, frozenCols: 0);
            });
            AssertValid(ms);

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var pane = doc.WorkbookPart!.WorksheetParts.Single().Worksheet.Descendants<Pane>().Single();
            Assert.That(pane.VerticalSplit!.Value, Is.EqualTo(1));
            Assert.That(pane.HorizontalSplit, Is.Null);
            Assert.That(pane.TopLeftCell!.Value, Is.EqualTo("A2"));
            Assert.That(pane.ActivePane!.Value, Is.EqualTo(PaneValues.BottomLeft));
        }

        [Test]
        public void FreezePanes_RowAndColumn_WritesBottomRightActivePane()
        {
            using var ms = WriteToStream(w => w.FreezePanes(frozenRows: 2, frozenCols: 1));
            AssertValid(ms);

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var pane = doc.WorkbookPart!.WorksheetParts.Single().Worksheet.Descendants<Pane>().Single();
            Assert.That(pane.HorizontalSplit!.Value, Is.EqualTo(1));
            Assert.That(pane.VerticalSplit!.Value, Is.EqualTo(2));
            Assert.That(pane.TopLeftCell!.Value, Is.EqualTo("B3"));
            Assert.That(pane.ActivePane!.Value, Is.EqualTo(PaneValues.BottomRight));
        }

        [Test]
        public void NoFreeze_NoSheetViewsElement()
        {
            using var ms = WriteToStream(w => w.SetCell(0, 0, "plain"));
            AssertValid(ms);

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            Assert.That(doc.WorkbookPart!.WorksheetParts.Single().Worksheet.Elements<SheetViews>().Any(), Is.False);
        }

        [Test]
        public void AutoFilter_WritesRangeReference()
        {
            using var ms = WriteToStream(w =>
            {
                w.SetCell(0, 0, "a");
                w.SetCell(0, 1, "b");
                w.SetAutoFilter(0, 0, 10, 1);
            });
            AssertValid(ms);

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var autoFilter = doc.WorkbookPart!.WorksheetParts.Single().Worksheet.Elements<AutoFilter>().Single();
            Assert.That(autoFilter.Reference!.Value, Is.EqualTo("A1:B11"));
        }
    }
}
