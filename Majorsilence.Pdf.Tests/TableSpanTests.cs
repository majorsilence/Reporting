// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.Linq;
using System.Text.RegularExpressions;
using Majorsilence.Pdf;
using Majorsilence.Pdf.Layout;
using NUnit.Framework;
using PdfPig = UglyToad.PdfPig.PdfDocument;

namespace Majorsilence.Pdf.Tests
{
    [TestFixture]
    public class TableSpanTests
    {
        private static PdfCanvas Canvas() => PdfDocument.Create().AddPage(PageSizes.A4);

        [Test]
        public void SpanDefaults_AreOne_AndInvalidSpansThrow()
        {
            var cell = new PdfTableCell("x");
            Assert.That(cell.ColumnSpan, Is.EqualTo(1));
            Assert.That(cell.RowSpan, Is.EqualTo(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => cell.WithColumnSpan(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => cell.WithRowSpan(0));
        }

        [Test]
        public void ColumnSpan_CellUsesTheWidthOfAllSpannedColumns()
        {
            var table = new PdfTable(100, 100, 100).WithNoBorder().WithCellPadding(2f);
            table.AddRow(new PdfTableRow(new PdfTableCell("SPANNED " + string.Join(" ", Enumerable.Repeat("word", 6))).WithColumnSpan(3)));
            var doc = PdfDocument.Create();
            doc.AddPage(PageSizes.A4).DrawTable(table, 50, 50);
            using var pdf = PdfPig.Open(doc.ToBytes());
            var words = pdf.GetPage(1).GetWords().ToList();
            // 7 words fit on one 296pt line, so they would wrap if the cell were a single 100pt column.
            Assert.That(words.Select(w => Math.Round(w.BoundingBox.Bottom)).Distinct().Count(), Is.EqualTo(1));
        }

        [Test]
        public void ColumnSpan_FollowingCellsMoveRight()
        {
            var table = new PdfTable(100, 100, 100).WithNoBorder();
            table.AddRow(new PdfTableRow(new PdfTableCell("AB").WithColumnSpan(2), new PdfTableCell("C")));
            var doc = PdfDocument.Create();
            doc.AddPage(PageSizes.A4).DrawTable(table, 0, 50);
            using var pdf = PdfPig.Open(doc.ToBytes());
            var c = pdf.GetPage(1).GetWords().Single(w => w.Text == "C");
            Assert.That(c.BoundingBox.Left, Is.EqualTo(200 + 4).Within(1)); // third column + 4pt padding
        }

        [Test]
        public void RowSpan_CellCoversRowsBelow_AndOthersShiftAroundIt()
        {
            var table = new PdfTable(100, 100).WithNoBorder();
            table.AddRow(new PdfTableRow(new PdfTableCell("TALL").WithRowSpan(2), new PdfTableCell("r1")));
            table.AddRow(new PdfTableRow(new PdfTableCell("r2"))); // lands in column 1, beside TALL
            var doc = PdfDocument.Create();
            doc.AddPage(PageSizes.A4).DrawTable(table, 0, 50);
            using var pdf = PdfPig.Open(doc.ToBytes());
            var words = pdf.GetPage(1).GetWords().ToDictionary(w => w.Text);
            Assert.That(words["r2"].BoundingBox.Left, Is.EqualTo(100 + 4).Within(1));
            Assert.That(words["r2"].BoundingBox.Bottom, Is.LessThan(words["r1"].BoundingBox.Bottom));
        }

        [Test]
        public void RowSpan_TallCell_GrowsTheLastSpannedRow()
        {
            var plain = new PdfTable(60, 100).WithNoBorder();
            plain.AddRow("a", "b");
            plain.AddRow("c", "d");
            float plainHeight = Canvas().MeasureTableHeight(plain);

            var spanned = new PdfTable(60, 100).WithNoBorder();
            spanned.AddRow(new PdfTableRow(
                new PdfTableCell(string.Join(" ", Enumerable.Repeat("long", 40))).WithRowSpan(2), new PdfTableCell("b")));
            spanned.AddRow(new PdfTableRow(new PdfTableCell("d")));
            Assert.That(Canvas().MeasureTableHeight(spanned), Is.GreaterThan(plainHeight + 50));
        }

        [Test]
        public void Spans_AreClippedToTheTable()
        {
            var table = new PdfTable(100, 100);
            table.AddRow(new PdfTableRow(new PdfTableCell("x").WithColumnSpan(9).WithRowSpan(9)));
            var doc = PdfDocument.Create();
            Assert.DoesNotThrow(() => doc.AddPage(PageSizes.A4).DrawTable(table, 0, 0));
        }

        [Test]
        public void NoSpans_RendersLikeBefore_IncludingShortRowsAndAlternation()
        {
            var table = new PdfTable(80, 80, 80).WithAlternateRowBackground(PdfColor.LightGray);
            table.AddRow("a", "b", "c");
            table.AddRow("d");               // short row: missing cells still drawn empty
            table.AddRow("e", "f", "g");
            float h = Canvas().MeasureTableHeight(table);
            Assert.That(h, Is.EqualTo(3 * (TextStyle.Default.FontSize + 8f)).Within(0.5f)); // three one-line rows with 4pt padding
        }

        [Test]
        public void Split_NeverCutsThroughARowSpan_AndNothingIsLost()
        {
            var table = new PdfTable(120, 120).WithHeaderBackground(PdfColor.LightGray);
            table.AddRow("H1", "H2");
            for (int i = 0; i < 80; i++)
            {
                // Every group is two rows tall: a spanning label plus two values.
                table.AddRow(new PdfTableRow(new PdfTableCell($"G{i:D3}").WithRowSpan(2), new PdfTableCell($"a{i:D3}")));
                table.AddRow(new PdfTableRow(new PdfTableCell($"b{i:D3}")));
            }
            var layout = PdfLayout.Begin(PdfDocument.Create(), PageSizes.A4).WithMargins(36);
            layout.Table(table);
            using var pdf = PdfPig.Open(layout.End().ToBytes());
            Assert.That(pdf.NumberOfPages, Is.GreaterThan(1));

            foreach (var page in pdf.GetPages())
            {
                var text = string.Join(" ", page.GetWords().Select(w => w.Text));
                // Each group's three cells always land on the same page.
                foreach (Match m in Regex.Matches(text, @"G(\d{3})"))
                {
                    Assert.That(text, Does.Contain("a" + m.Groups[1].Value));
                    Assert.That(text, Does.Contain("b" + m.Groups[1].Value));
                }
                Assert.That(text, Does.Contain("H1"), "header repeats");
            }
            var all = string.Join(" ", pdf.GetPages().Select(p => string.Join(" ", p.GetWords().Select(w => w.Text))));
            for (int i = 0; i < 80; i++)
                Assert.That(Regex.Matches(all, $"G{i:D3}").Count, Is.EqualTo(1));
        }

        [Test]
        public void Split_HeaderWithRowSpan_RepeatsAllHeaderRows()
        {
            var table = new PdfTable(100, 100, 100).WithHeaderBackground(PdfColor.LightGray);
            table.AddRow(new PdfTableRow(new PdfTableCell("HEADA").WithRowSpan(2), new PdfTableCell("HEADB").WithColumnSpan(2)));
            table.AddRow(new PdfTableRow(new PdfTableCell("SUB1"), new PdfTableCell("SUB2")));
            for (int i = 0; i < 120; i++) table.AddRow($"x{i:D3}", "y", "z");
            var layout = PdfLayout.Begin(PdfDocument.Create(), PageSizes.A4).WithMargins(36);
            layout.Table(table);
            using var pdf = PdfPig.Open(layout.End().ToBytes());
            Assert.That(pdf.NumberOfPages, Is.GreaterThan(1));
            foreach (var page in pdf.GetPages())
            {
                var text = string.Join(" ", page.GetWords().Select(w => w.Text));
                Assert.That(text, Does.Contain("HEADA").And.Contain("HEADB").And.Contain("SUB1").And.Contain("SUB2"));
            }
        }
    }
}
