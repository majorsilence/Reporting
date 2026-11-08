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
    public class FormulaCellsTests
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
        public void Formula_WithCachedValue_WritesFormulaAndValue()
        {
            using var ms = WriteToStream(w =>
            {
                w.SetCell(0, 0, 1.0);
                w.SetCell(1, 0, 2.0);
                w.SetFormula(2, 0, "SUM(A1:A2)", cachedValue: 3.0);
            });
            AssertValid(ms);

            var cell = GetCell(ms, "A3");
            Assert.That(cell.CellFormula!.Text, Is.EqualTo("SUM(A1:A2)"));
            Assert.That(cell.CellValue!.Text, Is.EqualTo("3"));
        }

        [Test]
        public void Formula_LeadingEqualsSign_IsStripped()
        {
            using var ms = WriteToStream(w => w.SetFormula(0, 0, "=A1+1"));
            AssertValid(ms);

            var cell = GetCell(ms, "A1");
            Assert.That(cell.CellFormula!.Text, Is.EqualTo("A1+1"));
        }

        [Test]
        public void Formula_WithoutCachedValue_OmitsValueElement()
        {
            using var ms = WriteToStream(w => w.SetFormula(0, 0, "NOW()"));
            AssertValid(ms);

            var cell = GetCell(ms, "A1");
            Assert.That(cell.CellFormula!.Text, Is.EqualTo("NOW()"));
            Assert.That(cell.CellValue, Is.Null);
        }
    }
}
