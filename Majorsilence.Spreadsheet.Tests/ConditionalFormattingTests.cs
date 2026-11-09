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
    public class ConditionalFormattingTests
    {
        private static SpreadsheetDocument WriteAndOpen(XlsxWriter writer, MemoryStream ms)
        {
            writer.Write(ms);
            ms.Position = 0;
            using (var check = SpreadsheetDocument.Open(ms, false))
            {
                var errors = new OpenXmlValidator().Validate(check).ToList();
                Assert.That(errors, Is.Empty,
                    string.Join(Environment.NewLine, errors.Select(e => $"{e.Description} ({e.Path?.XPath})")));
            }
            ms.Position = 0;
            return SpreadsheetDocument.Open(ms, false);
        }

        [Test]
        public void CellValueRule_WritesOperatorFormulaAndDxf()
        {
            var writer = new XlsxWriter("Sheet1");
            for (int r = 0; r < 5; r++) writer.SetCell(r, 0, r * 50.0);
            writer.AddConditionalFormat(0, 0, 4, 0, XlsxConditionOperator.GreaterThan, 100,
                new XlsxDifferentialFormat { Bold = true, FontColor = (192, 0, 0), BackgroundColor = (255, 235, 238) });

            using var ms = new MemoryStream();
            using var doc = WriteAndOpen(writer, ms);
            var cf = doc.WorkbookPart!.WorksheetParts.Single().Worksheet.Elements<ConditionalFormatting>().Single();
            Assert.That(cf.SequenceOfReferences!.InnerText, Is.EqualTo("A1:A5"));
            var rule = cf.Elements<ConditionalFormattingRule>().Single();
            Assert.That(rule.Type!.Value, Is.EqualTo(ConditionalFormatValues.CellIs));
            Assert.That(rule.Operator!.Value, Is.EqualTo(ConditionalFormattingOperatorValues.GreaterThan));
            Assert.That(rule.Elements<Formula>().Single().Text, Is.EqualTo("100"));

            var dxf = doc.WorkbookPart.WorkbookStylesPart!.Stylesheet.DifferentialFormats!.Elements<DifferentialFormat>()
                .ElementAt((int)rule.FormatId!.Value);
            Assert.That(dxf.Font!.GetFirstChild<Bold>(), Is.Not.Null);
            Assert.That(dxf.Font.GetFirstChild<Color>()!.Rgb!.Value, Is.EqualTo("FFC00000"));
            Assert.That(dxf.Fill!.PatternFill!.BackgroundColor!.Rgb!.Value, Is.EqualTo("FFFFEBEE"));
        }

        [Test]
        public void BetweenRule_WritesBothFormulas()
        {
            var writer = new XlsxWriter("Sheet1");
            writer.AddConditionalFormat(0, 0, 9, 1, XlsxConditionOperator.Between, 10, 20,
                new XlsxDifferentialFormat { Italic = true });

            using var ms = new MemoryStream();
            using var doc = WriteAndOpen(writer, ms);
            var rule = doc.WorkbookPart!.WorksheetParts.Single().Worksheet.Descendants<ConditionalFormattingRule>().Single();
            Assert.That(rule.Elements<Formula>().Select(f => f.Text), Is.EqualTo(new[] { "10", "20" }));
        }

        [Test]
        public void ExpressionRule_AndSharedDxf_AreWrittenOncePerFormat()
        {
            var highlight = new XlsxDifferentialFormat { BackgroundColor = (255, 255, 0) };
            var writer = new XlsxWriter("Sheet1");
            writer.AddConditionalFormatExpression(0, 0, 9, 3, "=$C1>100", highlight);
            writer.AddConditionalFormat(0, 4, 9, 4, XlsxConditionOperator.Equal, "\"Late\"", null,
                new XlsxDifferentialFormat { BackgroundColor = (255, 255, 0) });

            using var ms = new MemoryStream();
            using var doc = WriteAndOpen(writer, ms);
            var rules = doc.WorkbookPart!.WorksheetParts.Single().Worksheet.Descendants<ConditionalFormattingRule>().ToList();
            Assert.That(rules[0].Type!.Value, Is.EqualTo(ConditionalFormatValues.Expression));
            Assert.That(rules[0].Elements<Formula>().Single().Text, Is.EqualTo("$C1>100"));
            Assert.That(rules.Select(r => r.Priority!.Value), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(rules[0].FormatId!.Value, Is.EqualTo(rules[1].FormatId!.Value), "equal formats share one dxf");
            Assert.That(doc.WorkbookPart.WorkbookStylesPart!.Stylesheet.DifferentialFormats!.Count()!, Is.EqualTo(1));
        }

        [Test]
        public void DxfNumberFormat_AndRulesAlongsideOtherSheetFeatures_StayValid()
        {
            var writer = new XlsxWriter("Sheet1");
            writer.SetCell(0, 0, 0.5);
            writer.AddMerge(2, 0, 2, 1);
            writer.AddListValidation(3, 0, 3, 0, new[] { "a", "b" });
            writer.AddHyperlink(4, 0, "https://example.com");
            writer.AddConditionalFormat(0, 0, 0, 0, XlsxConditionOperator.LessThan, 1,
                new XlsxDifferentialFormat { NumberFormatCode = "0.0%" });

            using var ms = new MemoryStream();
            using var doc = WriteAndOpen(writer, ms);
            var dxf = doc.WorkbookPart!.WorkbookStylesPart!.Stylesheet.DifferentialFormats!.Elements<DifferentialFormat>().Single();
            Assert.That(dxf.NumberingFormat!.FormatCode!.Value, Is.EqualTo("0.0%"));
        }

        [Test]
        public void BetweenWithoutSecondFormula_Throws()
        {
            var writer = new XlsxWriter("Sheet1");
            Assert.Throws<ArgumentException>(() => writer.AddConditionalFormat(0, 0, 0, 0, XlsxConditionOperator.Between,
                "1", null, new XlsxDifferentialFormat()));
        }
    }
}
