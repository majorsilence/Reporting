// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Majorsilence.Pdf;
using Majorsilence.Pdf.Layout;
using Majorsilence.Pdf.Markdown;
using NUnit.Framework;
using PdfPig = UglyToad.PdfPig.PdfDocument;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Majorsilence.Pdf.Markdown.Tests
{
    /// <summary>
    /// End-to-end tests for markdown rendering, verified against real rendered PDFs with PdfPig.
    /// </summary>
    [TestFixture]
    public class MarkdownRenderingTests
    {
        private static string ExtractText(byte[] bytes)
        {
            using var doc = PdfPig.Open(bytes);
            return string.Join("\n", doc.GetPages().Select(p => ContentOrderTextExtractor.GetText(p)));
        }

        private static byte[] RenderOnePage(string markdown, List<string>? warnings = null, MarkdownStyle? style = null)
        {
            var doc = PdfDocument.Create();
            doc.AddPage(PageSizes.A4, canvas => canvas.DrawMarkdown(markdown, 36, 36, 500, style, warnings));
            using var ms = new MemoryStream();
            doc.Save(ms);
            return ms.ToArray();
        }

        [Test]
        public void DrawMarkdown_Headings_RenderWithCorrectTextAndLargerFontThanBody()
        {
            var bytes = RenderOnePage("# Heading One\n\nBody paragraph text.");
            var text = ExtractText(bytes);
            Assert.That(text, Does.Contain("Heading One"));
            Assert.That(text, Does.Contain("Body paragraph text."));

            using var doc = PdfPig.Open(bytes);
            var letters = doc.GetPage(1).Letters;
            var headingLetter = letters.First(l => l.Value == "H");
            var bodyLetter = letters.First(l => l.Value == "B");
            Assert.That(headingLetter.PointSize, Is.GreaterThan(bodyLetter.PointSize));
        }

        [Test]
        public void DrawMarkdown_WordsAcrossStyleBoundaries_KeepSpacesInExtractedText()
        {
            // Regression test: drawing each word with its trailing space intact (rather than
            // trimmed) is required for text extractors to reconstruct word boundaries -- without
            // it, adjacent words glue together ("Heading One" -> "HeadingOne").
            var bytes = RenderOnePage("A paragraph with **bold text**, *italic text*, and `inline code`.");
            var text = ExtractText(bytes);
            Assert.That(text, Does.Contain("A paragraph with"));
            Assert.That(text, Does.Contain("bold text"));
            Assert.That(text, Does.Contain("italic text"));
            Assert.That(text, Does.Contain("inline code"));
        }

        [Test]
        public void DrawMarkdown_PlainTextAfterInlineStyle_KeepsSpaceSeparation()
        {
            // Regression test: a literal text segment that starts with a space right after a
            // differently-styled run (bold/code/link) previously lost that space -- Split(' ')
            // turns a leading space into an empty first token, which got silently skipped,
            // gluing the words together ("2.0adds:", "publishAOT", "changelogfor").
            var bytes = RenderOnePage(
                "Version **2.0** adds: faster startup. Run `dotnet publish` AOT. See the [changelog](https://example.com) for details.");
            var text = ExtractText(bytes);
            Assert.That(text, Does.Contain("Version 2.0 adds:"));
            Assert.That(text, Does.Contain("publish AOT"));
            Assert.That(text, Does.Contain("changelog for details"));
        }

        [Test]
        public void DrawMarkdown_FencedCodeBlock_RendersLineContent()
        {
            var bytes = RenderOnePage("```csharp\nvar x = 1;\nvar y = 2;\n```");
            var text = ExtractText(bytes);
            Assert.That(text, Does.Contain("var x = 1;"));
            Assert.That(text, Does.Contain("var y = 2;"));
        }

        [Test]
        public void DrawMarkdown_BulletAndOrderedLists_RenderAllItems()
        {
            var bytes = RenderOnePage("- Bullet one\n- Bullet two\n\n1. First\n2. Second");
            var text = ExtractText(bytes);
            Assert.That(text, Does.Contain("Bullet one"));
            Assert.That(text, Does.Contain("Bullet two"));
            Assert.That(text, Does.Contain("First"));
            Assert.That(text, Does.Contain("Second"));
        }

        [Test]
        public void DrawMarkdown_PipeTable_RendersAsPdfTable()
        {
            var bytes = RenderOnePage("| A | B |\n|---|---|\n| 1 | 2 |");
            var text = ExtractText(bytes);
            Assert.That(text, Does.Contain("A"));
            Assert.That(text, Does.Contain("B"));
            Assert.That(text, Does.Contain("1"));
            Assert.That(text, Does.Contain("2"));
        }

        [Test]
        public void DrawMarkdown_Link_RendersTextAndAddsAnnotation()
        {
            var bytes = RenderOnePage("[a link](https://example.com)");
            var text = ExtractText(bytes);
            Assert.That(text, Does.Contain("a link"));
        }

        [Test]
        public void DrawMarkdown_Image_SkipsAndAddsWarning()
        {
            var warnings = new List<string>();
            RenderOnePage("![alt text](photo.png)", warnings);
            Assert.That(warnings, Has.Some.Contains("photo.png"));
        }

        [Test]
        public void DrawMarkdown_Blockquote_SkipsWithWarning_DoesNotThrow_AndSurroundingContentStillRenders()
        {
            var warnings = new List<string>();
            byte[] bytes = Array.Empty<byte>();
            Assert.DoesNotThrow(() => bytes = RenderOnePage("> Quoted text\n\nNormal paragraph after.", warnings));
            Assert.That(warnings, Has.Some.Contains("Blockquote"));
            Assert.That(ExtractText(bytes), Does.Contain("Normal paragraph after."));
        }

        [Test]
        public void DrawMarkdown_ReturnsPositiveHeight()
        {
            var doc = PdfDocument.Create();
            float height = 0f;
            doc.AddPage(PageSizes.A4, canvas => height = canvas.DrawMarkdown("# Title\n\nSome text.", 36, 36, 500));
            Assert.That(height, Is.GreaterThan(0f));
        }

        [Test]
        public void LayoutMarkdown_LongDocument_SpansMultiplePagesInOrder()
        {
            var sections = Enumerable.Range(1, 40).Select(i =>
                $"## Section {i}\n\nParagraph {i}. " + string.Concat(Enumerable.Repeat("Filler content. ", 8)));
            string longMarkdown = string.Join("\n\n", sections);

            var doc = PdfDocument.Create();
            var layout = PdfLayout.Begin(doc, PageSizes.A4).WithMargins(36);
            layout.Markdown(longMarkdown);
            using var ms = new MemoryStream();
            layout.End().Save(ms);
            var bytes = ms.ToArray();

            using var pdfDoc = PdfPig.Open(bytes);
            Assert.That(pdfDoc.NumberOfPages, Is.GreaterThan(1));

            var text = ExtractText(bytes);
            Assert.That(text, Does.Contain("Section 1"));
            Assert.That(text, Does.Contain("Section 40"));
            Assert.That(text.IndexOf("Section 1\n", StringComparison.Ordinal),
                Is.LessThan(text.IndexOf("Section 40", StringComparison.Ordinal)));
        }

        [Test]
        public void LayoutMarkdown_RealWorldReadme_ProducesMultiPagePdfWithExpectedSections()
        {
            // End-to-end smoke test against a real, unmodified document rather than a synthetic
            // fixture: this repo's own Majorsilence.Pdf/Readme.md, which mixes headings,
            // paragraphs, fenced C# code blocks, tables, and lists.
            string readmePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "Majorsilence.Pdf", "Readme.md");
            readmePath = Path.GetFullPath(readmePath);
            Assert.That(File.Exists(readmePath), Is.True, $"Expected to find {readmePath}");
            string markdown = File.ReadAllText(readmePath);

            var doc = PdfDocument.Create().WithTitle("Majorsilence.Pdf Readme");
            var layout = PdfLayout.Begin(doc, PageSizes.A4).WithMargins(36);
            var warnings = new List<string>();
            layout.Markdown(markdown, warnings: warnings);
            using var ms = new MemoryStream();
            layout.End().Save(ms);
            var bytes = ms.ToArray();

            using var pdfDoc = PdfPig.Open(bytes);
            Assert.That(pdfDoc.NumberOfPages, Is.GreaterThanOrEqualTo(2),
                "a real README this size should span at least 2 pages");

            var text = ExtractText(bytes);
            Assert.That(text, Does.Contain("Majorsilence.Pdf"));
            Assert.That(text, Does.Contain("Quick Start"));
            Assert.That(text, Does.Contain("PdfDocument.Create"));
        }

        [Test]
        public void MarkdownStyle_WithHeading_OverridesOnlyThatLevel()
        {
            var custom = MarkdownStyle.Default.WithHeading(1, TextStyle.Default.WithSize(40));
            var bytes = RenderOnePage("# Big Heading", style: custom);

            using var doc = PdfPig.Open(bytes);
            var letter = doc.GetPage(1).Letters.First(l => l.Value == "B");
            Assert.That(letter.PointSize, Is.EqualTo(40f).Within(0.5f));
        }

        private static string[] LayoutPages(string markdown, List<string>? warnings = null)
        {
            var doc = PdfDocument.Create();
            var layout = PdfLayout.Begin(doc, PageSizes.A4).WithMargins(36);
            layout.Markdown(markdown, warnings: warnings);
            using var pdf = PdfPig.Open(layout.End().ToBytes());
            return pdf.GetPages().Select(p => string.Join(" ", p.GetWords().Select(w => w.Text))).ToArray();
        }

        [Test]
        public void LayoutMarkdown_ParagraphTallerThanAPage_SplitsInsteadOfThrowing()
        {
            string paragraph = string.Join(" ", Enumerable.Range(1, 4000).Select(i => $"w{i}x"));
            var pages = LayoutPages(paragraph);
            Assert.That(pages.Length, Is.GreaterThan(1));
            var all = string.Join(" ", pages);
            Assert.That(System.Text.RegularExpressions.Regex.Matches(all, @"w\d+x").Count, Is.EqualTo(4000));
        }

        [Test]
        public void LayoutMarkdown_LongCodeBlock_SplitsBetweenLines()
        {
            string code = "```\n" + string.Join("\n", Enumerable.Range(1, 200).Select(i => $"code{i:D3}")) + "\n```";
            var pages = LayoutPages(code);
            Assert.That(pages.Length, Is.GreaterThan(1));
            var all = string.Join(" ", pages);
            for (int i = 1; i <= 200; i += 37)
                Assert.That(all, Does.Contain($"code{i:D3}"));
            Assert.That(pages[^1], Does.Contain("code200"));
        }

        [Test]
        public void LayoutMarkdown_LongTable_SplitsAndRepeatsTheHeader()
        {
            var sb = new System.Text.StringBuilder("| Name | Value |\n|---|---|\n");
            for (int i = 0; i < 150; i++) sb.Append($"| item{i:D3} | v{i} |\n");
            var pages = LayoutPages(sb.ToString());
            Assert.That(pages.Length, Is.GreaterThan(1));
            foreach (var page in pages) Assert.That(page, Does.Contain("Name"));
            Assert.That(string.Join(" ", pages), Does.Contain("item149"));
        }

        [Test]
        public void LayoutMarkdown_LongList_SplitsBetweenItems()
        {
            var md = string.Join("\n", Enumerable.Range(1, 150).Select(i => $"- entry{i:D3} text"));
            var pages = LayoutPages(md);
            Assert.That(pages.Length, Is.GreaterThan(1));
            var all = string.Join(" ", pages);
            Assert.That(System.Text.RegularExpressions.Regex.Matches(all, @"entry\d{3}").Count, Is.EqualTo(150));
        }

        [Test]
        public void LayoutMarkdown_Heading_IsNotStrandedAtTheFootOfAPage()
        {
            // Fill nearly the whole first page, leaving room for a heading but not the text after it.
            var filler = string.Join("\n\n", Enumerable.Range(1, 38).Select(i => $"filler{i:D2} text"));
            var pages = LayoutPages(filler + "\n\n# LATE HEADING\n\nfollow-up paragraph");
            int headingPage = Array.FindIndex(pages, p => p.Contains("LATE"));
            Assert.That(pages[headingPage], Does.Contain("follow-up"), "the heading shares a page with the text after it");
        }

        [Test]
        public void LayoutMarkdown_InsideAColumn_Renders()
        {
            var doc = PdfDocument.Create();
            var layout = PdfLayout.Begin(doc, PageSizes.A4).WithMargins(36);
            layout.Row(r =>
            {
                r.Column(0.5f, c => c.Markdown("## Left\n\nleft text", 250));
                r.Column(0.5f, c => c.Markdown("## Right\n\nright text", 250));
            });
            using var pdf = PdfPig.Open(layout.End().ToBytes());
            var text = string.Join(" ", pdf.GetPage(1).GetWords().Select(w => w.Text));
            Assert.That(text, Does.Contain("Left").And.Contain("right"));
        }

        [Test]
        public void LayoutMarkdown_Link_AddsAnnotation()
        {
            var doc = PdfDocument.Create();
            PdfLayout.Begin(doc, PageSizes.A4).Markdown("see [docs](https://example.com/x)").End();
            Assert.That(System.Text.Encoding.Latin1.GetString(doc.ToBytes()), Does.Contain("https://example.com/x"));
        }
    }
}
