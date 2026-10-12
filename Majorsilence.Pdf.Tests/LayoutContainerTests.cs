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
    [TestFixture]
    public class LayoutContainerTests
    {
        private const float Margin = 36f;
        private static float ContentHeight => PageSizes.A4.Height - 2 * Margin;

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

        private static string Lines(int n) => string.Join("\n", Enumerable.Range(1, n).Select(i => $"line{i:D3}"));

        // ── spans ────────────────────────────────────────────────────────────

        [Test]
        public void Spans_RenderMixedStylesAndWrap()
        {
            var pages = PageTexts(Render(l => l.Spans(s => s
                .Span("Plain ")
                .Span("bold ", TextStyle.Default.WithBold())
                .Span("red ", TextStyle.Default.WithColor(PdfColor.Red))
                .Span(string.Join(" ", Enumerable.Repeat("filler", 200))))));
            Assert.That(pages[0], Does.Contain("Plain").And.Contain("bold").And.Contain("red"));
        }

        [Test]
        public void Spans_LinkProducesALinkAnnotation()
        {
            var bytes = Render(l => l.Spans(s => s.Span("see ").Link("the docs", "https://example.com/docs")));
            string raw = System.Text.Encoding.Latin1.GetString(bytes);
            Assert.That(raw, Does.Contain("https://example.com/docs"));
        }

        [Test]
        public void Spans_GoToLinksToDestinationOnLaterPage()
        {
            var bytes = Render(l =>
            {
                l.Spans(s => s.GoTo("jump", "end"));
                l.PageBreak();
                l.Destination("end").Text("destination page");
            });
            string raw = System.Text.Encoding.Latin1.GetString(bytes);
            Assert.That(raw, Does.Match(@"/Subtype /Link[^>]*/Dest \["));
        }

        [Test]
        public void Spans_PageNumberAndTotalPages_AreFilledInPerPage()
        {
            var pages = PageTexts(Render(
                l => { l.Text("a"); l.PageBreak(); l.Text("b"); l.PageBreak(); l.Text("c"); },
                l => l.WithFooterContent(c => c.Spans(s => s.Span("Pg ").PageNumber().Span(" of ").TotalPages()))));
            Assert.That(pages, Has.Length.EqualTo(3));
            for (int i = 0; i < 3; i++) Assert.That(pages[i], Does.Contain($"Pg {i + 1} of 3"));
        }

        [Test]
        public void Spans_ExplicitNewlineStartsANewLine()
        {
            using var pdf = PdfPig.Open(Render(l => l.Spans(s => s.Span("first\nsecond"))));
            var words = pdf.GetPage(1).GetWords().ToList();
            Assert.That(words.Single(w => w.Text == "first").BoundingBox.Bottom,
                Is.GreaterThan(words.Single(w => w.Text == "second").BoundingBox.Bottom + 5f));
        }

        [Test]
        public void Spans_LongParagraph_SplitsAcrossPages_WithoutLosingWords()
        {
            string text = string.Join(" ", Enumerable.Range(1, 3000).Select(i => $"w{i}x"));
            var all = string.Join(" ", PageTexts(Render(l => l.Spans(s => s.Span(text)))));
            Assert.That(PageTexts(Render(l => l.Spans(s => s.Span(text)))).Length, Is.GreaterThan(1));
            Assert.That(Regex.Matches(all, @"w\d+x").Count, Is.EqualTo(3000));
        }

        // ── orphan / widow control ───────────────────────────────────────────

        [Test]
        public void Text_NeverLeavesASingleLineAtTheBottomOrTop()
        {
            // Fill the page so exactly one line of the paragraph would fit on page 1.
            float lh = TextStyle.Default.FontSize * 1.2f;
            int linesOnPage = (int)((ContentHeight - TextStyle.Default.FontSize) / lh) + 1;
            var pages = PageTexts(Render(l =>
            {
                l.Text(Lines(linesOnPage - 1));
                l.Text(Lines(6).Replace("line", "para"));
            }));
            Assert.That(pages, Has.Length.EqualTo(2));
            int onFirst = Regex.Matches(pages[0], "para").Count;
            Assert.That(onFirst, Is.EqualTo(0).Or.GreaterThanOrEqualTo(2), "no orphan");
            Assert.That(Regex.Matches(pages[1], "para").Count, Is.Not.EqualTo(1), "no widow");
        }

        [Test]
        public void Text_Widow_PullsOneLineBackFromTheFirstPage()
        {
            float lh = TextStyle.Default.FontSize * 1.2f;
            int linesOnPage = (int)((ContentHeight - TextStyle.Default.FontSize) / lh) + 1;
            // N lines where exactly N-1 fit on page 1 would leave a lone last line.
            var pages = PageTexts(Render(l => l.Text(Lines(linesOnPage + 1))));
            Assert.That(pages, Has.Length.EqualTo(2));
            Assert.That(Regex.Matches(pages[1], "line").Count, Is.EqualTo(2));
            Assert.That(Regex.Matches(pages[0], "line").Count, Is.EqualTo(linesOnPage - 1));
        }

        [Test]
        public void Text_OnAPageThatFitsOneLine_StillRenders()
        {
            var layout = PdfLayout.Begin(PdfDocument.Create(), new PdfPageSize(300, 60)).WithMargins(10);
            layout.Text("one\ntwo\nthree\nfour");
            Assert.DoesNotThrow(() => layout.End().ToBytes());
        }

        // ── decoration, show once, skip once ─────────────────────────────────

        [Test]
        public void Decoration_RepeatsHeaderAndFooterOnEveryPageTheContentSpans()
        {
            var pages = PageTexts(Render(l => l.Decoration(
                h => h.Text("HEADER"), c => c.Text(Lines(150)), f => f.Text("FOOTER"))));
            Assert.That(pages.Length, Is.GreaterThan(1));
            foreach (var p in pages) Assert.That(p, Does.Contain("HEADER").And.Contain("FOOTER"));
            Assert.That(Regex.Matches(string.Join(" ", pages), @"line\d+").Count, Is.EqualTo(150));
        }

        [Test]
        public void ShowOnce_AppearsOnlyOnTheFirstFragment()
        {
            var pages = PageTexts(Render(l => l.Decoration(
                h => h.ShowOnce(x => x.Text("TITLE")).Text("running"), c => c.Text(Lines(150)))));
            Assert.That(pages[0], Does.Contain("TITLE"));
            for (int i = 1; i < pages.Length; i++)
            {
                Assert.That(pages[i], Does.Not.Contain("TITLE"));
                Assert.That(pages[i], Does.Contain("running"));
            }
        }

        [Test]
        public void SkipOnce_AppearsOnlyAfterTheFirstFragment()
        {
            var pages = PageTexts(Render(l => l.Decoration(
                h => h.SkipOnce(x => x.Text("CONTINUED")), c => c.Text(Lines(150)))));
            Assert.That(pages[0], Does.Not.Contain("CONTINUED"));
            Assert.That(pages[1], Does.Contain("CONTINUED"));
        }

        [Test]
        public void Decoration_WithHeaderTallerThanPage_Throws()
        {
            var layout = PdfLayout.Begin(PdfDocument.Create(), PageSizes.A4).WithMargins(Margin);
            Assert.Throws<InvalidOperationException>(() =>
                layout.Decoration(h => h.Spacer(ContentHeight + 10), c => c.Text("x")));
        }

        // ── ensure space ─────────────────────────────────────────────────────

        [Test]
        public void EnsureSpace_BreaksPageWhenTooLittleRemains()
        {
            var pages = PageTexts(Render(l =>
            {
                l.Spacer(ContentHeight - 40);
                l.EnsureSpace(100).Text("HEADING");
            }));
            Assert.That(pages, Has.Length.EqualTo(2));
            Assert.That(pages[1], Does.Contain("HEADING"));
        }

        [Test]
        public void EnsureSpace_DoesNothingWhenThereIsRoom()
        {
            var pages = PageTexts(Render(l => l.Text("a").EnsureSpace(100).Text("b")));
            Assert.That(pages, Has.Length.EqualTo(1));
        }

        // ── layers ───────────────────────────────────────────────────────────

        [Test]
        public void Layers_DrawBehindAndInFrontOfThePrimaryLayer_AndSizeFromPrimary()
        {
            var bytes = Render(l =>
            {
                l.Layers(x => x
                    .Behind(c => c.Text("BEHIND", TextStyle.Default.WithAlignment(TextAlignment.Center)))
                    .Primary(c => c.Text(Lines(5)))
                    .InFront(c => c.Text("FRONT", TextStyle.Default.WithAlignment(TextAlignment.Right))));
                l.Text("AFTER");
            });
            using var pdf = PdfPig.Open(bytes);
            var words = pdf.GetPage(1).GetWords().ToList();
            Assert.That(words.Select(w => w.Text), Does.Contain("BEHIND").And.Contain("FRONT").And.Contain("AFTER"));
            // BEHIND and FRONT share the primary layer's top edge.
            Assert.That(words.Single(w => w.Text == "BEHIND").BoundingBox.Bottom,
                Is.EqualTo(words.Single(w => w.Text == "FRONT").BoundingBox.Bottom).Within(0.5));
            // AFTER sits below all five primary lines, not just one.
            Assert.That(words.Single(w => w.Text == "BEHIND").BoundingBox.Bottom
                      - words.Single(w => w.Text == "AFTER").BoundingBox.Bottom, Is.GreaterThan(4 * 12f));
        }

        [Test]
        public void Layers_WithoutPrimary_Throws()
        {
            var layout = PdfLayout.Begin(PdfDocument.Create(), PageSizes.A4);
            Assert.Throws<InvalidOperationException>(() => layout.Layers(x => x.Behind(c => c.Text("x"))));
        }

        [Test]
        public void Layers_SplitWithThePrimaryLayer()
        {
            var pages = PageTexts(Render(l => l.Layers(x => x
                .Primary(c => c.Text(Lines(150)))
                .InFront(c => c.Text("STAMP", TextStyle.Default.WithAlignment(TextAlignment.Right))))));
            Assert.That(pages.Length, Is.GreaterThan(1));
            Assert.That(pages[0], Does.Contain("STAMP"));
        }

        // ── inlined ──────────────────────────────────────────────────────────

        [Test]
        public void Inlined_WrapsItemsOntoNewLines()
        {
            var bytes = Render(l => l.Inlined(i =>
            {
                for (int n = 0; n < 40; n++)
                {
                    int id = n;
                    i.Item(c => c.Text($"TAG{id:D2}"));
                }
            }));
            using var pdf = PdfPig.Open(bytes);
            var words = pdf.GetPage(1).GetWords().Where(w => w.Text.StartsWith("TAG")).ToList();
            Assert.That(words, Has.Count.EqualTo(40));
            Assert.That(words.Select(w => Math.Round(w.BoundingBox.Bottom)).Distinct().Count(), Is.GreaterThan(1));
            Assert.That(words.All(w => w.BoundingBox.Right <= PageSizes.A4.Width - Margin + 1), Is.True);
        }

        [Test]
        public void Inlined_ManyItems_SplitsAcrossPagesWithoutLoss()
        {
            var text = string.Join(" ", PageTexts(Render(l => l.Inlined(i =>
            {
                for (int n = 0; n < 4000; n++)
                {
                    int id = n;
                    i.Item(c => c.Box(LayoutBoxStyle.Default.WithPadding(4).WithBorder(PdfColor.Black), b => b.Text($"T{id:D4}")));
                }
            }))));
            Assert.That(Regex.Matches(text, @"T\d{4}").Count, Is.EqualTo(4000));
        }

        // ── bullets, components, size constraints ────────────────────────────

        [Test]
        public void Bullet_DrawsTheMarkerOnlyOnTheFirstFragment()
        {
            var pages = PageTexts(Render(l => l.Bullet("•", 20, c => c.Text(Lines(150)))));
            Assert.That(pages.Length, Is.GreaterThan(1));
            Assert.That(pages[0], Does.Contain("•"));
            Assert.That(pages[1], Does.Not.Contain("•"));
        }

        private sealed class InvoiceHeader : ILayoutComponent
        {
            public void Compose(LayoutColumn column) => column.Text("INVOICE-HEADER").Line();
        }

        [Test]
        public void Component_CanBeReusedAcrossTheDocument()
        {
            var text = string.Join(" ", PageTexts(Render(l =>
            {
                l.Component(new InvoiceHeader());
                l.PageBreak();
                l.Component(new InvoiceHeader());
            })));
            Assert.That(Regex.Matches(text, "INVOICE-HEADER").Count, Is.EqualTo(2));
        }

        [Test]
        public void Box_Shrink_SizesToContent_AndAlignsRight()
        {
            var style = LayoutBoxStyle.Default.WithShrink().WithAlignment(TextAlignment.Right);
            using var pdf = PdfPig.Open(Render(l => l.Box(style, c => c.Text("TINY"))));
            var w = pdf.GetPage(1).GetWords().Single(x => x.Text == "TINY");
            Assert.That(w.BoundingBox.Right, Is.EqualTo(PageSizes.A4.Width - Margin).Within(1.0));
        }

        [Test]
        public void Box_MaxWidth_WrapsTextEarly()
        {
            var style = LayoutBoxStyle.Default.WithMaxWidth(120);
            using var pdf = PdfPig.Open(Render(l => l.Box(style, c => c.Text(string.Join(" ", Enumerable.Repeat("word", 40))))));
            Assert.That(pdf.GetPage(1).GetWords().Max(w => w.BoundingBox.Right), Is.LessThanOrEqualTo(Margin + 121));
        }

        [Test]
        public void Box_MaxHeight_ContinuesTheContentOnTheNextPage()
        {
            var style = LayoutBoxStyle.Default.WithMaxHeight(100);
            var pages = PageTexts(Render(l => l.Box(style, c => c.Text(Lines(40)))));
            Assert.That(pages.Length, Is.GreaterThan(1));
            Assert.That(Regex.Matches(string.Join(" ", pages), @"line\d+").Count, Is.EqualTo(40));
        }

        [Test]
        public void Box_FixedHeight_ReservesExactlyThatHeight()
        {
            var style = LayoutBoxStyle.Default.WithHeight(200);
            using var pdf = PdfPig.Open(Render(l => { l.Box(style, c => c.Text("TOP")); l.Text("NEXT"); }));
            var words = pdf.GetPage(1).GetWords().ToList();
            double gap = words.Single(w => w.Text == "TOP").BoundingBox.Bottom - words.Single(w => w.Text == "NEXT").BoundingBox.Bottom;
            Assert.That(gap, Is.EqualTo(200).Within(2));
        }
    }
}
