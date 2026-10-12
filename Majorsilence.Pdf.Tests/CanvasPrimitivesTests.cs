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
    public class CanvasPrimitivesTests
    {
        [Test]
        public void Bookmarks_AreWrittenWithNestingAndPageDestinations()
        {
            var doc = PdfDocument.Create();
            var p1 = doc.AddPage(PageSizes.A4);
            p1.AddBookmark("Chapter 1").AddBookmark("Section 1.1", 200, level: 1).AddBookmark("Section 1.2", 300, level: 1);
            var p2 = doc.AddPage(PageSizes.A4);
            p2.AddBookmark("Chapter 2", 50);

            using var pdf = PdfPig.Open(doc.ToBytes());
            Assert.That(pdf.TryGetBookmarks(out var marks), Is.True);
            Assert.That(marks.Roots.Select(r => r.Title), Is.EqualTo(new[] { "Chapter 1", "Chapter 2" }));
            var ch1 = marks.Roots[0];
            Assert.That(ch1.Children.Select(c => c.Title), Is.EqualTo(new[] { "Section 1.1", "Section 1.2" }));
            Assert.That(((UglyToad.PdfPig.Outline.DocumentBookmarkNode)marks.Roots[1]).PageNumber, Is.EqualTo(2));
        }

        [Test]
        public void BookmarkLevelJump_IsClampedToOneBelowPrevious()
        {
            var doc = PdfDocument.Create();
            doc.AddPage(PageSizes.A4).AddBookmark("Top").AddBookmark("Deep", 10, level: 5);
            using var pdf = PdfPig.Open(doc.ToBytes());
            pdf.TryGetBookmarks(out var marks);
            Assert.That(marks.Roots, Has.Count.EqualTo(1));
            Assert.That(marks.Roots[0].Children.Single().Title, Is.EqualTo("Deep"));
        }

        [Test]
        public void InternalLink_ResolvesToDestinationOnAnotherPage()
        {
            var doc = PdfDocument.Create();
            var p1 = doc.AddPage(PageSizes.A4);
            p1.AddInternalLink(50, 50, 100, 20, "target");
            var p2 = doc.AddPage(PageSizes.A4);
            p2.AddDestination("target", 72, 144);

            string raw = System.Text.Encoding.Latin1.GetString(doc.ToBytes());
            Assert.That(raw, Does.Match(@"/Subtype /Link[^>]*/Dest \[\d+ 0 R /XYZ 72 \d+(\.\d+)? null\]"));
            Assert.That(raw, Does.Contain("/Dests"));

            using var pdf = PdfPig.Open(doc.ToBytes());
            Assert.That(pdf.NumberOfPages, Is.EqualTo(2));
        }

        [Test]
        public void InternalLink_ToMissingDestination_ThrowsOnSave()
        {
            var doc = PdfDocument.Create();
            doc.AddPage(PageSizes.A4).AddInternalLink(0, 0, 10, 10, "nowhere");
            Assert.Throws<InvalidOperationException>(() => doc.ToBytes());
        }

        [Test]
        public void DestinationNames_WithSpecialCharacters_AreEscaped()
        {
            var doc = PdfDocument.Create();
            var p = doc.AddPage(PageSizes.A4);
            p.AddDestination("sec 1/é", 0, 0).AddInternalLink(0, 0, 5, 5, "sec 1/é");
            string raw = System.Text.Encoding.Latin1.GetString(doc.ToBytes());
            Assert.That(raw, Does.Contain("/sec#201#2F#C3#A9 ["));
            Assert.DoesNotThrow(() => PdfPig.Open(doc.ToBytes()).Dispose());
        }

        [Test]
        public void ClipRectangle_HidesTextOutsideTheClip_AndRestoreStateReleasesIt()
        {
            var doc = PdfDocument.Create();
            var c = doc.AddPage(PageSizes.A4);
            c.SaveState().ClipRectangle(0, 0, 100, 100);
            c.DrawText("CLIPPED", 20, 50, TextStyle.Default);
            c.RestoreState();
            c.DrawText("FREE", 20, 200, TextStyle.Default);

            string content = ContentStream(doc.ToBytes());
            Assert.That(content, Does.Match(@"q\s+0 [\d.]+ 100 100 re W n"));
            Assert.That(content.IndexOf("W n", StringComparison.Ordinal),
                Is.LessThan(content.IndexOf("Q", StringComparison.Ordinal)));
            using var pdf = PdfPig.Open(doc.ToBytes());
            Assert.That(pdf.GetPage(1).Text, Does.Contain("CLIPPED").And.Contain("FREE"));
        }

        [Test]
        public void RestoreState_WithoutSave_Throws()
        {
            var c = PdfDocument.Create().AddPage(PageSizes.A4);
            Assert.Throws<InvalidOperationException>(() => c.RestoreState());
        }

        [Test]
        public void Rotate90_MovesTextAlongTheRotatedAxis()
        {
            var doc = PdfDocument.Create();
            var c = doc.AddPage(PageSizes.A4);
            c.SaveState().Rotate(90, 300, 300);
            c.DrawText("ROT", 300, 300, TextStyle.Default.WithSize(20));
            c.RestoreState();
            using var pdf = PdfPig.Open(doc.ToBytes());
            var letters = pdf.GetPage(1).Letters;
            // Rotated 90° clockwise about its start point, the text runs downward: x stays, y falls.
            Assert.That(letters.Max(l => l.StartBaseLine.X) - letters.Min(l => l.StartBaseLine.X), Is.LessThan(1f));
            Assert.That(letters.First().StartBaseLine.Y - letters.Last().StartBaseLine.Y, Is.GreaterThan(5f));
        }

        [Test]
        public void Translate_And_Scale_AreAppliedToDrawing()
        {
            var doc = PdfDocument.Create();
            var c = doc.AddPage(PageSizes.A4);
            c.SaveState().Translate(100, 50).Scale(2, 2);
            c.DrawText("MOVED", 0, 20, TextStyle.Default.WithSize(10));
            c.RestoreState();
            using var pdf = PdfPig.Open(doc.ToBytes());
            var first = pdf.GetPage(1).Letters.First();
            Assert.That(first.StartBaseLine.X, Is.EqualTo(100f).Within(0.5f));
            Assert.That(first.PointSize, Is.EqualTo(20f).Within(0.5f));
        }

        [Test]
        public void ClipRoundedRectangle_ProducesAClosedClipPath()
        {
            var doc = PdfDocument.Create();
            var c = doc.AddPage(PageSizes.A4);
            c.SaveState().ClipRoundedRectangle(10, 10, 100, 50, 8);
            c.DrawRectangle(0, 0, 200, 200, ShapeStyle.Filled(PdfColor.Red));
            c.RestoreState();
            string content = ContentStream(doc.ToBytes());
            Assert.That(content, Does.Contain("h W n"));
            Assert.That(Regex.Matches(content, @" c\n").Count, Is.EqualTo(4));
        }

        [Test]
        public void BreakLines_MatchesWrappingAndReportsIndexesAndWidths()
        {
            var c = PdfDocument.Create().AddPage(PageSizes.A4);
            var style = TextStyle.Default.WithSize(12);
            string text = "alpha beta gamma delta epsilon\nzeta eta theta";
            var lines = c.BreakLines(text, 80, style);

            Assert.That(lines.Count, Is.GreaterThan(2));
            Assert.That(lines.All(l => l.Width <= 80.5f), Is.True);
            Assert.That(string.Concat(lines.Select(l => text.Substring(l.StartIndex, l.Length))), Is.EqualTo(text));
            // The same text must measure to the same number of lines as the height measurement.
            float expected = style.FontSize + (lines.Count - 1) * c.GetLineHeight(style);
            Assert.That(c.MeasureTextBoxHeight(text, 80, style), Is.EqualTo(expected).Within(0.01f));
        }

        [Test]
        public void Layout_BookmarkAndDestination_LandOnTheirPages()
        {
            var doc = PdfDocument.Create();
            var layout = PdfLayout.Begin(doc, PageSizes.A4).WithMargins(36);
            layout.Bookmark("Intro").Destination("intro").Text("hello")
                  .PageBreak()
                  .Bookmark("Details").Destination("details").Text("world");
            layout.Canvas((c, r) => c.AddInternalLink(r.X, r.Y, 50, 10, "intro"), 12);
            layout.End();

            using var pdf = PdfPig.Open(doc.ToBytes());
            pdf.TryGetBookmarks(out var marks);
            Assert.That(marks.Roots.Select(m => m.Title), Is.EqualTo(new[] { "Intro", "Details" }));
            Assert.That(((UglyToad.PdfPig.Outline.DocumentBookmarkNode)marks.Roots[0]).PageNumber, Is.EqualTo(1));
            Assert.That(((UglyToad.PdfPig.Outline.DocumentBookmarkNode)marks.Roots[1]).PageNumber, Is.EqualTo(2));
        }

        private static string ContentStream(byte[] pdfBytes) => RawContent(pdfBytes);

        // Inflate every FlateDecode stream in the file and return the concatenated text.
        private static string RawContent(byte[] bytes)
        {
            var sb = new System.Text.StringBuilder();
            string latin = System.Text.Encoding.Latin1.GetString(bytes);
            foreach (Match m in Regex.Matches(latin, @"/Filter /FlateDecode /Length (\d+) >>\nstream\n"))
            {
                int start = m.Index + m.Length;
                int len = int.Parse(m.Groups[1].Value);
                using var ms = new System.IO.MemoryStream(bytes, start, len);
                using var z = new System.IO.Compression.ZLibStream(ms, System.IO.Compression.CompressionMode.Decompress);
                using var r = new System.IO.StreamReader(z, System.Text.Encoding.Latin1);
                sb.Append(r.ReadToEnd()).Append('\n');
            }
            return sb.ToString();
        }
    }
}
