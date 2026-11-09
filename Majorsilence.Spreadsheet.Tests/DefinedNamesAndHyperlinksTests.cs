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
    public class DefinedNamesAndHyperlinksTests
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
        public void DefinedName_SingleCell_WritesAbsoluteReference()
        {
            var writer = new XlsxWriter("Data");
            writer.SetCell(4, 2, 42.0); // C5
            writer.AddDefinedName("Total", sheetIndex: 0, row: 4, col: 2);

            using var ms = new MemoryStream();
            writer.Write(ms);
            AssertValid(ms);

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var dn = doc.WorkbookPart!.Workbook.DefinedNames!.Elements<DefinedName>().Single();
            Assert.That(dn.Name!.Value, Is.EqualTo("Total"));
            Assert.That(dn.Text, Is.EqualTo("'Data'!$C$5"));
        }

        [Test]
        public void DefinedName_Range_WritesAbsoluteRangeReference()
        {
            var writer = new XlsxWriter("Sheet1");
            writer.AddDefinedName("Table", sheetIndex: 0, r1: 0, c1: 0, r2: 9, c2: 2);

            using var ms = new MemoryStream();
            writer.Write(ms);
            AssertValid(ms);

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var dn = doc.WorkbookPart!.Workbook.DefinedNames!.Elements<DefinedName>().Single();
            Assert.That(dn.Text, Is.EqualTo("'Sheet1'!$A$1:$C$10"));
        }

        [Test]
        public void ExternalHyperlink_WritesRelationshipAndReference()
        {
            var writer = new XlsxWriter("Sheet1");
            writer.SetCell(0, 0, "link");
            writer.AddHyperlink(0, 0, "https://example.com/");

            using var ms = new MemoryStream();
            writer.Write(ms);
            AssertValid(ms);

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var worksheetPart = doc.WorkbookPart!.WorksheetParts.Single();
            var link = worksheetPart.Worksheet.Descendants<Hyperlink>().Single();
            Assert.That(link.Reference!.Value, Is.EqualTo("A1"));
            var rel = worksheetPart.HyperlinkRelationships.Single();
            Assert.That(rel.Uri.ToString(), Is.EqualTo("https://example.com/"));
        }

        [Test]
        public void InternalHyperlink_WritesLocationWithoutRelationship()
        {
            var writer = new XlsxWriter("Sheet1");
            int sheet2 = writer.AddSheet("Sheet2");
            writer.SetCell(0, 0, "jump");
            writer.AddInternalHyperlink(0, 0, "Sheet2!A1");

            using var ms = new MemoryStream();
            writer.Write(ms);
            AssertValid(ms);

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var worksheetPart = doc.WorkbookPart!.WorksheetParts.First();
            var link = worksheetPart.Worksheet.Descendants<Hyperlink>().Single();
            Assert.That(link.Location!.Value, Is.EqualTo("Sheet2!A1"));
            Assert.That(link.Id, Is.Null);
            Assert.That(worksheetPart.HyperlinkRelationships.Any(), Is.False);
        }

        [Test]
        public void ExternalHyperlink_CoexistsWithImageOnSameSheet()
        {
            var writer = new XlsxWriter("Sheet1");
            byte[] fakeJpeg = { 0xFF, 0xD8, 0xFF, 0xD9 };
            writer.AddImage(fakeJpeg, 0, 0, 1000, 1000);
            writer.AddHyperlink(0, 0, "https://example.com/");

            using var ms = new MemoryStream();
            writer.Write(ms);
            AssertValid(ms);

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var worksheetPart = doc.WorkbookPart!.WorksheetParts.Single();
            Assert.That(worksheetPart.DrawingsPart, Is.Not.Null);
            Assert.That(worksheetPart.HyperlinkRelationships.Single().Uri.ToString(), Is.EqualTo("https://example.com/"));
        }
    }
}
