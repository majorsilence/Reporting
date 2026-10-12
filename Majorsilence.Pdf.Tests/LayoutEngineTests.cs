// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Majorsilence.Pdf;
using Majorsilence.Pdf.Layout;
using NUnit.Framework;
using PdfPig = UglyToad.PdfPig.PdfDocument;

namespace Majorsilence.Pdf.Tests
{
    /// <summary>
    /// Tests for the <see cref="PdfLayout"/> element-tree engine: splitting tables and boxes,
    /// header/footer page-count placeholders, and a property-style check that pagination never
    /// loses, duplicates or mis-places content.
    /// </summary>
    [TestFixture]
    public class LayoutEngineTests
    {
        private const float Margin = 36f;

        private static byte[] Render(Action<PdfLayout> configure, Func<PdfLayout, PdfLayout>? setup = null)
        {
            var layout = PdfLayout.Begin(PdfDocument.Create(), PageSizes.A4).WithMargins(Margin);
            if (setup != null) layout = setup(layout);
            configure(layout);
            using var ms = new MemoryStream();
            layout.End().Save(ms);
            return ms.ToArray();
        }

        private static string[] PageTexts(byte[] bytes)
        {
            using var doc = PdfPig.Open(bytes);
            return doc.GetPages().Select(p => string.Join(" ", p.GetWords().Select(w => w.Text))).ToArray();
        }

        private static PdfTable MakeTable(int dataRows)
        {
            var table = new PdfTable(200, 200).WithHeaderBackground(PdfColor.LightGray).WithCellPadding(4f);
            table.AddRow("HEADERCOL", "HEADERVAL");
            for (int i = 0; i < dataRows; i++) table.AddRow($"row{i:D4}", "data");
            return table;
        }

        [Test]
        public void FooterTemplate_ShowsPageXOfY_OnEveryPage()
        {
            var bytes = Render(
                l => { l.Text("first"); l.PageBreak(); l.Text("second"); l.PageBreak(); l.Text("third"); },
                l => l.WithFooterText("Page {page} of {pages}"));

            var pages = PageTexts(bytes);
            Assert.That(pages, Has.Length.EqualTo(3));
            for (int i = 0; i < 3; i++)
                Assert.That(pages[i], Does.Contain($"Page {i + 1} of 3"));
        }

        [Test]
        public void Header_IsDrawnOnEveryPage_AndReservesSpace()
        {
            var bytes = Render(
                l => { l.Text("body"); l.PageBreak(); l.Text("body2"); },
                l => l.WithHeader((c, page, total) =>
                    c.DrawText($"HDR {page}/{total}", Margin, Margin + 10, TextStyle.Default), 30f));

            var pages = PageTexts(bytes);
            Assert.That(pages[0], Does.Contain("HDR 1/2"));
            Assert.That(pages[1], Does.Contain("HDR 2/2"));
        }

        [Test]
        public void LongTable_SplitsAcrossPages_RepeatsHeader_AndKeepsEveryRowOnce()
        {
            const int rows = 120;
            var bytes = Render(l => l.Table(MakeTable(rows)));

            var pages = PageTexts(bytes);
            Assert.That(pages.Length, Is.GreaterThan(1));
            foreach (var page in pages)
                Assert.That(page, Does.Contain("HEADERCOL"), "header row repeats on every page");

            var all = string.Join(" ", pages);
            for (int i = 0; i < rows; i++)
                Assert.That(Regex.Matches(all, $"row{i:D4}").Count, Is.EqualTo(1), $"row {i}");
        }

        [Test]
        public void Table_WithNoHeader_SplitsWithoutRepeatingFirstRow()
        {
            var table = new PdfTable(200);
            for (int i = 0; i < 150; i++) table.AddRow($"item{i:D4}");
            var pages = PageTexts(Render(l => l.Table(table)));

            Assert.That(pages.Length, Is.GreaterThan(1));
            var all = string.Join(" ", pages);
            Assert.That(Regex.Matches(all, "item0000").Count, Is.EqualTo(1));
            Assert.That(Regex.Matches(all, "item0149").Count, Is.EqualTo(1));
        }

        [Test]
        public void Box_WithLongText_SplitsAcrossPages()
        {
            string longText = string.Join(" ", Enumerable.Range(1, 1500).Select(i => $"w{i}x"));
            var style = LayoutBoxStyle.Default.WithPadding(8).WithBorder(PdfColor.Black, 1).WithBackground(PdfColor.LightGray);
            var pages = PageTexts(Render(l => l.Box(style, c => c.Text(longText))));

            Assert.That(pages.Length, Is.GreaterThan(1));
            var all = string.Join(" ", pages);
            Assert.That(all, Does.Contain("w1x"));
            Assert.That(all, Does.Contain("w1500x"));
        }

        [Test]
        public void KeepTogether_MovesWholeBlockToNextPage()
        {
            var pages = PageTexts(Render(l =>
            {
                l.Spacer(PageSizes.A4.Height - 2 * Margin - 30);
                l.KeepTogether(c => c.Text("KEEP-A").Spacer(20).Text("KEEP-B"));
            }));

            Assert.That(pages, Has.Length.EqualTo(2));
            Assert.That(pages[0], Does.Not.Contain("KEEP-A"));
            Assert.That(pages[1], Does.Contain("KEEP-A").And.Contain("KEEP-B"));
        }

        [Test]
        public void KeepTogether_TallerThanPage_Throws()
        {
            var layout = PdfLayout.Begin(PdfDocument.Create(), PageSizes.A4).WithMargins(Margin);
            Assert.Throws<InvalidOperationException>(() =>
                layout.KeepTogether(c => c.Spacer(500).Spacer(500)));
        }

        [Test]
        public void RowConfigureCallback_RunsExactlyOnce()
        {
            int calls = 0;
            Render(l => l.Row(r => r.Column(1f, col => { calls++; col.Text("x"); })));
            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void ColumnsCanHoldBoxes()
        {
            var style = LayoutBoxStyle.Default.WithPadding(4).WithBorder(PdfColor.Black);
            var text = string.Join(" ", PageTexts(Render(l => l.Row(r =>
            {
                r.Column(0.5f, c => c.Box(style, b => b.Text("LEFTBOX")));
                r.Column(0.5f, c => c.Box(style, b => b.Text("RIGHTBOX")));
            }))));
            Assert.That(text, Does.Contain("LEFTBOX").And.Contain("RIGHTBOX"));
        }

        [Test]
        public void End_CalledTwice_DoesNotAddPages()
        {
            var layout = PdfLayout.Begin(PdfDocument.Create(), PageSizes.A4);
            layout.Text("hello");
            var doc = layout.End();
            Assert.That(layout.End(), Is.SameAs(doc));
        }

        [Test]
        public void Pagination_RandomContent_NeverLosesDuplicatesOrOverflowsContent([Range(1, 12)] int seed)
        {
            var rng = new Random(seed);
            var expected = new System.Collections.Generic.List<string>();
            int id = 0;
            string Marker() { var m = $"MK{seed:D2}{id++:D4}Z"; expected.Add(m); return m; }
            string Filler(int words) => string.Join(" ", Enumerable.Range(0, words).Select(_ => "lorem"));

            var bytes = Render(l =>
            {
                int count = rng.Next(5, 30);
                for (int i = 0; i < count; i++)
                {
                    switch (rng.Next(6))
                    {
                        case 0: l.Text(Marker() + " " + Filler(rng.Next(1, 900))); break;
                        case 1: l.Spacer(rng.Next(1, 200)); break;
                        case 2:
                            var t = new PdfTable(150, 150).WithHeaderBackground(PdfColor.LightGray);
                            t.AddRow("hdr", "h"); // repeats on continuation pages, so untracked
                            int n = rng.Next(1, 80);
                            for (int r = 0; r < n; r++) t.AddRow(Marker(), "v");
                            l.Table(t);
                            break;
                        case 3:
                            l.Box(LayoutBoxStyle.Default.WithPadding(rng.Next(0, 12)).WithBorder(PdfColor.Black, 1),
                                c => c.Text(Marker() + " " + Filler(rng.Next(1, 600))).Spacer(rng.Next(0, 40)));
                            break;
                        case 4:
                            l.KeepTogether(c => c.Text(Marker()).Spacer(rng.Next(1, 300)));
                            break;
                        default:
                            l.Row(r =>
                            {
                                r.Column(0.5f, c => c.Text(Marker() + " " + Filler(rng.Next(1, 60))));
                                r.Column(0.5f, c => c.Text(Marker() + " " + Filler(rng.Next(1, 60))));
                            });
                            break;
                    }
                }
            });

            using var pdf = PdfPig.Open(bytes);
            var all = string.Join(" ", pdf.GetPages().Select(p => string.Join(" ", p.GetWords().Select(w => w.Text))));
            foreach (var m in expected)
                Assert.That(Regex.Matches(all, m).Count, Is.EqualTo(1), $"marker {m}");

            foreach (var page in pdf.GetPages())
                foreach (var letter in page.Letters)
                {
                    // PdfPig y is measured from the bottom of the page.
                    Assert.That(letter.GlyphRectangle.Bottom, Is.GreaterThanOrEqualTo(Margin - 2f), "below the bottom margin");
                    Assert.That(letter.GlyphRectangle.Top, Is.LessThanOrEqualTo(page.Height - Margin + 2f), "above the top margin");
                }
        }
    }
}
