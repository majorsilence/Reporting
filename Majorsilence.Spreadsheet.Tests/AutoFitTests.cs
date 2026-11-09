using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using NUnit.Framework;

namespace Majorsilence.Spreadsheet.Tests
{
    [TestFixture]
    public class AutoFitTests
    {
        private static double[] Widths(XlsxWriter writer, int count)
        {
            using var ms = new MemoryStream();
            writer.Write(ms);
            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var cols = doc.WorkbookPart!.WorksheetParts.Single().Worksheet.Descendants<Column>().ToList();
            return Enumerable.Range(1, count)
                .Select(i => cols.SingleOrDefault(c => c.Min!.Value == i)?.Width?.Value ?? 0)
                .ToArray();
        }

        [Test]
        public void LongerText_GetsAWiderColumn()
        {
            var writer = new XlsxWriter("Sheet1");
            writer.SetCell(0, 0, "Id");
            writer.SetCell(0, 1, "A considerably longer description");
            writer.AutoFitColumns();

            var w = Widths(writer, 2);
            Assert.That(w[1], Is.GreaterThan(w[0] * 4));
            Assert.That(w[1], Is.InRange(24, 32), "Excel fits this Calibri 11 text in about 26 characters");
        }

        [Test]
        public void Numbers_AreMeasuredAsDisplayedThroughTheirFormat()
        {
            var writer = new XlsxWriter("Sheet1");
            writer.SetCell(0, 0, 1234567.891);
            writer.SetCell(0, 1, 1234567.891, new XlsxCellFormat { NumberFormatCode = "#,##0.00 \"USD\"" });
            writer.SetCell(0, 2, new DateTime(2024, 3, 15));
            writer.AutoFitColumns();

            var w = Widths(writer, 3);
            Assert.That(w[1], Is.GreaterThan(w[0]), "\"1,234,567.89 USD\" is wider than \"1234567.891\"");
            Assert.That(w[2], Is.InRange(9, 12), "\"3/15/2024\"");
        }

        [Test]
        public void BiggerAndBolderFonts_AndMultiLineText_AreAccountedFor()
        {
            var writer = new XlsxWriter("Sheet1");
            writer.SetCell(0, 0, "Heading");
            writer.SetCell(0, 1, "Heading", new XlsxCellFormat { FontSizePt = 22, Bold = true });
            writer.SetCell(0, 2, "short\nmuch longer line\nmid");
            writer.SetCell(0, 3, "much longer line");
            writer.AutoFitColumns();

            var w = Widths(writer, 4);
            Assert.That(w[1], Is.GreaterThan(w[0] * 1.8));
            Assert.That(w[2], Is.EqualTo(w[3]), "the longest line decides");
        }

        [Test]
        public void MergedAcrossColumns_AndLimits_AreRespected()
        {
            var writer = new XlsxWriter("Sheet1");
            writer.SetCell(0, 0, new string('x', 80));
            writer.AddMerge(0, 0, 0, 2);
            writer.SetCell(1, 0, "ab");
            writer.SetCell(0, 4, new string('x', 400));
            writer.AutoFitColumns(minWidth: 8, maxWidth: 60);

            var w = Widths(writer, 5);
            Assert.That(w[0], Is.EqualTo(8), "the merged title doesn't widen column A; \"ab\" is under the minimum");
            Assert.That(w[4], Is.EqualTo(60));
        }

        [Test]
        public void ColumnsWithoutCells_KeepTheirWidth()
        {
            var writer = new XlsxWriter("Sheet1");
            writer.SetColumnWidth(3, 42);
            writer.SetCell(0, 0, "a");
            writer.AutoFitColumns();

            Assert.That(Widths(writer, 4)[3], Is.EqualTo(42));
        }
    }
}
