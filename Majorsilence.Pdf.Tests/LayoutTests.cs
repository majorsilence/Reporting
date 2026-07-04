// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using System.Linq;
using Majorsilence.Pdf;
using Majorsilence.Pdf.Layout;
using NUnit.Framework;
using PdfPig = UglyToad.PdfPig.PdfDocument;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Majorsilence.Pdf.Tests
{
    /// <summary>
    /// End-to-end tests for the <see cref="PdfLayout"/> flow-layout engine, verified against
    /// real rendered PDFs with PdfPig (page count, text presence, and reading order).
    /// </summary>
    [TestFixture]
    public class LayoutTests
    {
        private static byte[] Render(Action<PdfLayout> configure)
        {
            var doc = PdfDocument.Create();
            var layout = PdfLayout.Begin(doc, PageSizes.A4).WithMargins(36);
            configure(layout);
            using var ms = new MemoryStream();
            layout.End().Save(ms);
            return ms.ToArray();
        }

        private static int PageCount(byte[] bytes)
        {
            using var doc = PdfPig.Open(bytes);
            return doc.NumberOfPages;
        }

        private static string ExtractAllText(byte[] bytes)
        {
            using var doc = PdfPig.Open(bytes);
            return string.Join("\n", doc.GetPages().Select(p => ContentOrderTextExtractor.GetText(p)));
        }

        [Test]
        public void Row_HeightIsMaxOfChildColumns_BothColumnsRenderOnOnePage()
        {
            var bytes = Render(layout =>
            {
                layout.Row(row =>
                {
                    row.Column(0.5f, col => col.Text("Short"));
                    row.Column(0.5f, col => col.Text(
                        "This is a much longer line of text that should wrap across " +
                        "multiple lines within its narrower column width, making this " +
                        "column noticeably taller than the short one next to it."));
                });
                layout.Text("AFTER-ROW-MARKER");
            });

            Assert.That(PageCount(bytes), Is.EqualTo(1));
            var text = ExtractAllText(bytes);
            Assert.That(text, Does.Contain("Short"));
            Assert.That(text, Does.Contain("noticeably"));
            Assert.That(text, Does.Contain("AFTER-ROW-MARKER"),
                "content drawn after the row should still land on the same page, proving the " +
                "row's measured height (not an overestimate) was used to advance the cursor");
        }

        [Test]
        public void Text_LongerThanOnePage_SpillsAcrossPagesInOrder()
        {
            string longText = string.Join(" ", Enumerable.Range(1, 2000).Select(i => $"word{i}"));
            var bytes = Render(layout => layout.Text(longText, TextStyle.Default.WithSize(11)));

            Assert.That(PageCount(bytes), Is.GreaterThanOrEqualTo(2));
            var text = ExtractAllText(bytes);
            Assert.That(text, Does.Contain("word1 "));
            Assert.That(text, Does.Contain("word2000"));
            Assert.That(text.IndexOf("word1 ", StringComparison.Ordinal),
                Is.LessThan(text.IndexOf("word2000", StringComparison.Ordinal)),
                "earlier words must render before later words across the page split");
        }

        [Test]
        public void Row_ThatDoesNotFitOnCurrentPage_MovesEntirelyToNextPage()
        {
            var bytes = Render(layout =>
            {
                float contentHeight = PageSizes.A4.Height - 72; // 36pt margins top+bottom
                layout.Spacer(contentHeight - 5); // leave less than one line of room
                layout.Row(row => row.Column(1.0f, col => col.Text("ROWMARKER content")));
            });

            Assert.That(PageCount(bytes), Is.EqualTo(2));
            Assert.That(ExtractAllText(bytes), Does.Contain("ROWMARKER content"));
        }

        [Test]
        public void Row_NestedInsideColumn_RendersAllContent()
        {
            var bytes = Render(layout =>
            {
                layout.Row(row =>
                {
                    row.Column(1.0f, col =>
                    {
                        col.Text("Outer column header");
                        col.Row(nested =>
                        {
                            nested.Column(0.5f, c => c.Text("Nested A"));
                            nested.Column(0.5f, c => c.Text("Nested B"));
                        });
                    });
                });
            });

            var text = ExtractAllText(bytes);
            Assert.That(text, Does.Contain("Outer column header"));
            Assert.That(text, Does.Contain("Nested A"));
            Assert.That(text, Does.Contain("Nested B"));
        }

        [Test]
        public void Footer_IsDrawnOnceOnEveryPage()
        {
            int footerCalls = 0;
            var doc = PdfDocument.Create();
            var layout = PdfLayout.Begin(doc, PageSizes.A4)
                .WithMargins(36)
                .WithFooter((canvas, page) =>
                {
                    footerCalls++;
                    canvas.DrawText($"FOOTERMARK{page}", 36, 800, TextStyle.Default.WithSize(9));
                });

            layout.Text("Page 1 content");
            layout.PageBreak();
            layout.Text("Page 2 content");
            layout.PageBreak();
            layout.Text("Page 3 content");

            using var ms = new MemoryStream();
            layout.End().Save(ms);
            var bytes = ms.ToArray();

            Assert.That(footerCalls, Is.EqualTo(3));
            Assert.That(PageCount(bytes), Is.EqualTo(3));
            var text = ExtractAllText(bytes);
            Assert.That(text, Does.Contain("FOOTERMARK1"));
            Assert.That(text, Does.Contain("FOOTERMARK2"));
            Assert.That(text, Does.Contain("FOOTERMARK3"));
        }

        [Test]
        public void Row_TallerThanFullPageContentArea_ThrowsInvalidOperationException()
        {
            var doc = PdfDocument.Create();
            var layout = PdfLayout.Begin(doc, PageSizes.A4).WithMargins(36);

            Assert.Throws<InvalidOperationException>(() =>
                layout.Row(row => row.Column(1.0f, col => col.Spacer(2000f))));
        }

        [Test]
        public void Table_ThatDoesNotFit_MovesToNextPage()
        {
            var bytes = Render(layout =>
            {
                float contentHeight = PageSizes.A4.Height - 72;
                layout.Spacer(contentHeight - 5);

                var table = new PdfTable(new float[] { 200, 200 })
                    .WithCellPadding(4f);
                table.AddRow("Col1", "Col2");
                table.AddRow("TABLEMARKER", "data");

                layout.Table(table);
            });

            Assert.That(PageCount(bytes), Is.EqualTo(2));
            Assert.That(ExtractAllText(bytes), Does.Contain("TABLEMARKER"));
        }

        [Test]
        public void Spacer_And_Line_AdvanceCursorWithoutThrowing()
        {
            Assert.DoesNotThrow(() => Render(layout =>
            {
                layout.Text("Before");
                layout.Spacer(20f);
                layout.Line();
                layout.Text("After");
            }));
        }

        [Test]
        public void Canvas_EscapeHatch_InvokesDrawCallbackWithReservedRect()
        {
            LayoutRect? captured = null;
            var bytes = Render(layout =>
            {
                layout.Canvas((canvas, rect) =>
                {
                    captured = rect;
                    canvas.DrawText("CANVASMARKER", rect.X, rect.Y, TextStyle.Default);
                }, height: 40f);
            });

            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Value.Height, Is.EqualTo(40f));
            Assert.That(ExtractAllText(bytes), Does.Contain("CANVASMARKER"));
        }

        [Test]
        public void WithMargins_AfterFirstDrawCall_Throws()
        {
            var doc = PdfDocument.Create();
            var layout = PdfLayout.Begin(doc, PageSizes.A4);
            layout.Text("start");

            Assert.Throws<InvalidOperationException>(() => layout.WithMargins(10));
        }
    }
}
