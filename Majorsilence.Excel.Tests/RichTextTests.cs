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
    public class RichTextTests
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
        public void MixedRuns_WriteSeparateRunsWithTheirOwnFormatting()
        {
            var writer = new XlsxWriter("Sheet1");
            writer.SetRichText(0, 0, new[]
            {
                new XlsxTextRun("Bold ") { Bold = true },
                new XlsxTextRun("plain"),
            });

            using var ms = new MemoryStream();
            writer.Write(ms);
            AssertValid(ms);

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var cell = doc.WorkbookPart!.WorksheetParts.Single().Worksheet.Descendants<Cell>().Single();
            Assert.That(cell.DataType!.Value, Is.EqualTo(CellValues.InlineString));

            var runs = cell.InlineString!.Elements<Run>().ToList();
            Assert.That(runs, Has.Count.EqualTo(2));
            Assert.That(runs[0].Text!.Text, Is.EqualTo("Bold "));
            Assert.That(runs[0].RunProperties!.GetFirstChild<Bold>(), Is.Not.Null);
            Assert.That(runs[1].Text!.Text, Is.EqualTo("plain"));
            Assert.That(runs[1].RunProperties, Is.Null);
        }

        [Test]
        public void RunWithFontAndColor_WritesInCorrectSchemaOrder()
        {
            var writer = new XlsxWriter("Sheet1");
            writer.SetRichText(0, 0, new[]
            {
                new XlsxTextRun("colored")
                {
                    FontName = "Arial",
                    FontSizePt = 14,
                    Color = (255, 0, 0),
                    Italic = true,
                    Underline = true,
                },
            });

            using var ms = new MemoryStream();
            writer.Write(ms);
            AssertValid(ms); // the validator checks rPr child element ordering

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var run = doc.WorkbookPart!.WorksheetParts.Single().Worksheet.Descendants<Run>().Single();
            Assert.That(run.RunProperties!.GetFirstChild<RunFont>()!.Val!.Value, Is.EqualTo("Arial"));
            Assert.That(run.RunProperties!.GetFirstChild<FontSize>()!.Val!.Value, Is.EqualTo(14));
            Assert.That(run.RunProperties!.GetFirstChild<Italic>(), Is.Not.Null);
            Assert.That(run.RunProperties!.GetFirstChild<Underline>(), Is.Not.Null);
        }

        [Test]
        public void RichTextCell_CanStillHaveACellLevelFormat()
        {
            var format = new XlsxCellFormat { HasBackground = true, BackgroundColor = (255, 255, 0) };
            var writer = new XlsxWriter("Sheet1");
            writer.SetRichText(0, 0, new[] { new XlsxTextRun("text") }, format);

            using var ms = new MemoryStream();
            writer.Write(ms);
            AssertValid(ms);

            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var cell = doc.WorkbookPart!.WorksheetParts.Single().Worksheet.Descendants<Cell>().Single();
            Assert.That(cell.StyleIndex!.Value, Is.GreaterThan(0u));
        }
    }
}
