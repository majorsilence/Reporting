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
    public class DataValidationTests
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
        public void ListValidation_FromOptions_WritesQuotedInlineList()
        {
            var writer = new XlsxWriter("Sheet1");
            writer.AddListValidation(0, 0, 9, 0, new[] { "Yes", "No", "Maybe" });

            using var ms = new MemoryStream();
            writer.Write(ms);
            AssertValid(ms);

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var dv = doc.WorkbookPart!.WorksheetParts.Single().Worksheet.Descendants<DataValidation>().Single();
            Assert.That(dv.Type!.Value, Is.EqualTo(DataValidationValues.List));
            Assert.That(dv.SequenceOfReferences!.InnerText, Is.EqualTo("A1:A10"));
            Assert.That(dv.Formula1!.Text, Is.EqualTo("\"Yes,No,Maybe\""));
        }

        [Test]
        public void ListValidation_FromRangeFormula_IsWrittenVerbatim()
        {
            var writer = new XlsxWriter("Sheet1");
            int lookup = writer.AddSheet("Lookup");
            writer.SetCell(0, 0, "a", sheetIndex: lookup);
            writer.AddListValidation(0, 0, 0, 0, "Lookup!$A$1:$A$5");

            using var ms = new MemoryStream();
            writer.Write(ms);
            AssertValid(ms);

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var firstSheetPart = doc.WorkbookPart!.WorksheetParts.First();
            var dv = firstSheetPart.Worksheet.Descendants<DataValidation>().Single();
            Assert.That(dv.Formula1!.Text, Is.EqualTo("Lookup!$A$1:$A$5"));
        }
    }
}
