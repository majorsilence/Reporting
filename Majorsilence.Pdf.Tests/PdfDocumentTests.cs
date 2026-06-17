// Copyright (C) 2025 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Majorsilence.Pdf;
using NUnit.Framework;
using PdfPig = UglyToad.PdfPig.PdfDocument;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Majorsilence.Pdf.Tests
{
    /// <summary>
    /// End-to-end tests that generate real PDFs and verify them with PdfPig.
    /// </summary>
    [TestFixture]
    public class PdfDocumentTests
    {
        private static string? FindSystemFont() => FindFont(
            @"C:\Windows\Fonts\arial.ttf",
            @"C:\Windows\Fonts\times.ttf",
            "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
            "/Library/Fonts/Arial.ttf");

        private static string? FindFont(params string[] candidates)
        {
            foreach (var c in candidates) if (File.Exists(c)) return c;
            return null;
        }

        // ── helpers ───────────────────────────────────────────────────────────

        private static byte[] GenerateBytes(Action<PdfDocument> configure)
        {
            var doc = PdfDocument.Create();
            configure(doc);
            return doc.ToBytes();
        }

        private static void AssertValidPdf(byte[] bytes)
        {
            Assert.That(bytes.Length, Is.GreaterThan(100));
            var header = Encoding.ASCII.GetString(bytes, 0, 5);
            Assert.That(header, Is.EqualTo("%PDF-"), $"Expected PDF header, got: {header}");
        }

        private static IReadOnlyList<Word> ExtractWords(byte[] pdfBytes)
        {
            using var doc = PdfPig.Open(pdfBytes);
            return doc.GetPages().SelectMany(p => p.GetWords()).ToList();
        }

        private static string ExtractAllText(byte[] pdfBytes)
        {
            using var doc = PdfPig.Open(pdfBytes);
            return string.Join(" ", doc.GetPages().Select(p => ContentOrderTextExtractor.GetText(p)));
        }

        // ── basic document structure ──────────────────────────────────────────

        [Test]
        public void Create_ReturnsNonNull()
        {
            Assert.That(PdfDocument.Create(), Is.Not.Null);
        }

        [Test]
        public void EmptyDocument_ProducesValidPdf()
        {
            var bytes = GenerateBytes(doc => doc.AddPage(PageSizes.A4, _ => { }));
            AssertValidPdf(bytes);
        }

        [Test]
        public void ToBytes_ProducesValidPdf()
        {
            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.Letter, canvas =>
                    canvas.DrawText("Hello", 72, 72))
                .ToBytes();

            AssertValidPdf(bytes);
        }

        [Test]
        public void Save_ToStream_ProducesValidPdf()
        {
            using var ms = new MemoryStream();
            PdfDocument.Create()
                .AddPage(PageSizes.A4, canvas => canvas.DrawText("Test", 50, 50))
                .Save(ms);

            AssertValidPdf(ms.ToArray());
        }

        [Test]
        public void Save_ToFile_ProducesValidPdf()
        {
            string path = Path.GetTempFileName();
            try
            {
                PdfDocument.Create()
                    .AddPage(PageSizes.A4, canvas => canvas.DrawText("File save test", 50, 50))
                    .Save(path);

                Assert.That(File.Exists(path), Is.True);
                AssertValidPdf(File.ReadAllBytes(path));
            }
            finally { File.Delete(path); }
        }

        // ── metadata ──────────────────────────────────────────────────────────

        [Test]
        public void Metadata_IsStoredInDocument()
        {
            var bytes = PdfDocument.Create()
                .WithTitle("My Report")
                .WithAuthor("Test Author")
                .WithSubject("Testing")
                .WithCreator("Majorsilence.Pdf Tests")
                .AddPage(PageSizes.A4, _ => { })
                .ToBytes();

            AssertValidPdf(bytes);
            using var pig = PdfPig.Open(bytes);
            var info = pig.Information;
            Assert.That(info.Title,   Is.EqualTo("My Report"));
            Assert.That(info.Author,  Is.EqualTo("Test Author"));
        }

        // ── page count ────────────────────────────────────────────────────────

        [Test]
        public void SinglePage_CountIsOne()
        {
            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.A4, _ => { })
                .ToBytes();

            using var pig = PdfPig.Open(bytes);
            Assert.That(pig.NumberOfPages, Is.EqualTo(1));
        }

        [Test]
        public void MultiPage_CorrectPageCount()
        {
            const int count = 5;
            var doc = PdfDocument.Create();
            for (int i = 0; i < count; i++)
                doc.AddPage(PageSizes.A4, canvas => canvas.DrawText($"Page", 50, 50));

            using var pig = PdfPig.Open(doc.ToBytes());
            Assert.That(pig.NumberOfPages, Is.EqualTo(count));
        }

        [Test]
        public void IncrementalStyle_ProducesCorrectPageCount()
        {
            var doc = PdfDocument.Create();
            var page1 = doc.AddPage(PageSizes.A4);
            page1.DrawText("Page 1", 50, 50);
            var page2 = doc.AddPage(PageSizes.Letter);
            page2.DrawText("Page 2", 50, 50);

            using var pig = PdfPig.Open(doc.ToBytes());
            Assert.That(pig.NumberOfPages, Is.EqualTo(2));
        }

        // ── page dimensions ───────────────────────────────────────────────────

        [Test]
        public void PageSize_A4_CorrectDimensions()
        {
            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.A4, _ => { })
                .ToBytes();

            using var pig = PdfPig.Open(bytes);
            var page = pig.GetPage(1);
            Assert.That(page.Width,  Is.EqualTo(PageSizes.A4.Width).Within(1f));
            Assert.That(page.Height, Is.EqualTo(PageSizes.A4.Height).Within(1f));
        }

        [Test]
        public void PageSize_Letter_CorrectDimensions()
        {
            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.Letter, _ => { })
                .ToBytes();

            using var pig = PdfPig.Open(bytes);
            var page = pig.GetPage(1);
            Assert.That(page.Width,  Is.EqualTo(PageSizes.Letter.Width).Within(1f));
            Assert.That(page.Height, Is.EqualTo(PageSizes.Letter.Height).Within(1f));
        }

        [Test]
        public void MixedPageSizes_EachPageHasCorrectSize()
        {
            var doc = PdfDocument.Create();
            doc.AddPage(PageSizes.A4,     _ => { });
            doc.AddPage(PageSizes.Letter, _ => { });

            using var pig = PdfPig.Open(doc.ToBytes());
            Assert.That(pig.GetPage(1).Width,  Is.EqualTo(PageSizes.A4.Width).Within(1f));
            Assert.That(pig.GetPage(2).Width,  Is.EqualTo(PageSizes.Letter.Width).Within(1f));
        }

        // ── standard-font text ────────────────────────────────────────────────

        [Test]
        public void DrawText_StandardFont_Helvetica_ProducesWords()
        {
            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.A4, canvas =>
                    canvas.DrawText("Hello World",  72, 100, TextStyle.Default))
                .ToBytes();

            AssertValidPdf(bytes);
            var words = ExtractWords(bytes);
            var allText = string.Join(" ", words.Select(w => w.Text));
            Assert.That(allText, Does.Contain("Hello").Or.Contain("World"),
                "Expected extracted text to contain rendered content");
        }

        // ── WinAnsi encoding tests ────────────────────────────────────────────
        // EscapeWinAnsi is internal; InternalsVisibleTo makes it accessible here.

        [Test]
        public void EscapeWinAnsi_EmDash_MapsToByte0x97()
        {
            string result = PdfCanvas.EscapeWinAnsi("—");
            Assert.That(result, Has.Length.EqualTo(1));
            Assert.That((int)result[0], Is.EqualTo(0x97), "Em dash must map to WinAnsi byte 0x97");
        }

        [Test]
        public void EscapeWinAnsi_EnDash_MapsToByte0x96()
        {
            string result = PdfCanvas.EscapeWinAnsi("–");
            Assert.That((int)result[0], Is.EqualTo(0x96));
        }

        [Test]
        [TestCase('…', 0x85)]  // …
        [TestCase('•', 0x95)]  // •
        [TestCase('‘', 0x91)]  // ' (left single quotation mark)
        [TestCase('’', 0x92)]  // ' (right single quotation mark)
        [TestCase('“', 0x93)]  // " (left double quotation mark)
        [TestCase('”', 0x94)]  // " (right double quotation mark)
        [TestCase('™', 0x99)]  // ™
        [TestCase('€', 0x80)]  // €
        public void EscapeWinAnsi_CommonSpecialChars_MapCorrectly(char input, int expectedByte)
        {
            string result = PdfCanvas.EscapeWinAnsi(input.ToString());
            Assert.That(result, Has.Length.EqualTo(1));
            Assert.That((int)result[0], Is.EqualTo(expectedByte),
                $"U+{(int)input:X4} ('{input}') should map to WinAnsi byte 0x{expectedByte:X2}");
        }

        [Test]
        public void EscapeWinAnsi_RegularAscii_PassesThrough()
        {
            const string ascii = "Hello, World! 123";
            Assert.That(PdfCanvas.EscapeWinAnsi(ascii), Is.EqualTo(ascii));
        }

        [Test]
        public void EscapeWinAnsi_Latin1Extended_PassesThrough()
        {
            // Characters 0xA0–0xFF are the same in both Latin-1 and Windows-1252
            const string latin1 = "Ä Ö Ü ä ö ü ß é à";
            string result = PdfCanvas.EscapeWinAnsi(latin1);
            Assert.That(result, Is.EqualTo(latin1));
        }

        [Test]
        public void EscapeWinAnsi_TruelyUnmappableChar_ReplacedWithQuestionMarkInSilentOverload()
        {
            // The silent overload (used by MeasureTextWidth) still substitutes '?'
            string result = PdfCanvas.EscapeWinAnsi("中");
            Assert.That(result, Is.EqualTo("?"));
        }

        [Test]
        public void DrawText_StandardFont_UnmappableChar_Throws()
        {
            // U+4E2D (中) is not in WinAnsiEncoding — DrawText must throw rather than silently corrupt
            Assert.Throws<InvalidOperationException>(() =>
            {
                PdfDocument.Create()
                    .AddPage(PageSizes.A4, canvas =>
                        canvas.DrawText("Hello 中 World", 72, 100, TextStyle.Default.WithSize(12)))
                    .ToBytes();
            });
        }

        [Test]
        public void DrawText_StandardFont_UnmappableChar_ErrorMessageMentionsWithFontFile()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                PdfDocument.Create()
                    .AddPage(PageSizes.A4, canvas =>
                        canvas.DrawText("中", 72, 100, TextStyle.Default.WithSize(12)))
                    .ToBytes();
            });
            Assert.That(ex!.Message, Does.Contain("WithFontFile"));
            Assert.That(ex.Message, Does.Contain("4E2D"));
        }

        [Test]
        public void Metadata_UnicodeTitle_RoundTrips()
        {
            // The Info dict now uses UTF-16BE hex strings — verify PdfPig can read them back.
            const string title = "Título: café résumé 中文";
            var bytes = PdfDocument.Create()
                .WithTitle(title)
                .AddPage(PageSizes.A4, canvas =>
                    canvas.DrawText("Unicode metadata test", 72, 100, TextStyle.Default.WithSize(12)))
                .ToBytes();

            AssertValidPdf(bytes);
            using var doc = PdfPig.Open(bytes);
            Assert.That(doc.Information.Title, Is.EqualTo(title));
        }

        [Test]
        public void DrawText_StandardFont_WinAnsiSpecialChars_ProducesValidPdf()
        {
            // Smoke test: ensure the document renders without error when WinAnsi
            // extension characters are used with a standard Type1 font.
            const string sample = "Display — em dash – en dash … • bullet";
            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.A4, canvas =>
                    canvas.DrawText(sample, 50, 50, TextStyle.Default.WithSize(14)))
                .ToBytes();

            AssertValidPdf(bytes);
        }

        [Test]
        public void DrawText_TimesRoman_Bold_ProducesOutput()
        {
            var style = TextStyle.Default.WithFamily("Times-Roman").WithSize(14).WithBold();
            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.A4, canvas =>
                    canvas.DrawText("Bold Times Text", 50, 50, style))
                .ToBytes();

            AssertValidPdf(bytes);
        }

        [Test]
        public void DrawText_Courier_ProducesOutput()
        {
            var style = TextStyle.Default.WithFamily("Courier").WithSize(10);
            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.A4, canvas =>
                    canvas.DrawText("Monospace text here", 50, 50, style))
                .ToBytes();

            AssertValidPdf(bytes);
        }

        [Test]
        public void DrawText_ColoredText_ProducesOutput()
        {
            var style = TextStyle.Default.WithColor(PdfColor.Red).WithSize(16);
            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.A4, canvas =>
                    canvas.DrawText("Red text", 50, 50, style))
                .ToBytes();

            AssertValidPdf(bytes);
        }

        [Test]
        public void DrawText_EmptyString_DoesNotCrash()
        {
            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.A4, canvas =>
                {
                    canvas.DrawText("",   50, 50);
                    canvas.DrawText(null!, 50, 70);
                })
                .ToBytes();

            AssertValidPdf(bytes);
        }

        // ── embedded TTF font text ────────────────────────────────────────────

        [Test]
        public void DrawText_EmbeddedTtf_ProducesOutput()
        {
            string? fontPath = FindSystemFont();
            Assume.That(fontPath, Is.Not.Null, "No system TTF font available");

            var style = TextStyle.Default.WithFontFile(fontPath!).WithSize(12);
            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.A4, canvas =>
                    canvas.DrawText("Embedded TTF font text", 72, 100, style))
                .ToBytes();

            AssertValidPdf(bytes);
        }

        [Test]
        public void DrawText_SameTtfReused_BothPagesRender()
        {
            string? fontPath = FindSystemFont();
            Assume.That(fontPath, Is.Not.Null);

            var style = TextStyle.Default.WithFontFile(fontPath!).WithSize(11);
            var doc = PdfDocument.Create();
            doc.AddPage(PageSizes.A4, c => c.DrawText("Page 1 - TTF font", 50, 50, style));
            doc.AddPage(PageSizes.A4, c => c.DrawText("Page 2 - same TTF", 50, 50, style));
            var bytes = doc.ToBytes();

            AssertValidPdf(bytes);
            using var pig = PdfPig.Open(bytes);
            Assert.That(pig.NumberOfPages, Is.EqualTo(2));
        }

        // ── shapes ────────────────────────────────────────────────────────────

        [Test]
        public void DrawLine_ProducesValidPdf()
        {
            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.A4, canvas =>
                    canvas.DrawLine(50, 50, 200, 50))
                .ToBytes();

            AssertValidPdf(bytes);
        }

        [Test]
        public void DrawLine_AllLineStyles_ProducesValidPdf()
        {
            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.A4, canvas =>
                {
                    canvas.DrawLine(50, 50,  300, 50,  StrokeStyle.Default);
                    canvas.DrawLine(50, 80,  300, 80,  StrokeStyle.Default.Dashed());
                    canvas.DrawLine(50, 110, 300, 110, StrokeStyle.Default.Dotted());
                })
                .ToBytes();

            AssertValidPdf(bytes);
        }

        [Test]
        public void DrawRectangle_FilledAndStroked_ProducesValidPdf()
        {
            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.A4, canvas =>
                {
                    canvas.DrawRectangle(50, 50, 200, 100, ShapeStyle.Filled(PdfColor.LightGray));
                    canvas.DrawRectangle(50, 180, 200, 100, ShapeStyle.Stroked(PdfColor.Black, 2f));
                    canvas.DrawRectangle(50, 310, 200, 100,
                        ShapeStyle.Filled(PdfColor.Yellow).WithStroke(PdfColor.DarkGray, 1.5f));
                })
                .ToBytes();

            AssertValidPdf(bytes);
        }

        [Test]
        public void DrawEllipse_ProducesValidPdf()
        {
            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.A4, canvas =>
                {
                    canvas.DrawEllipse(50, 50, 200, 80, ShapeStyle.Filled(PdfColor.Blue));
                    canvas.DrawEllipse(50, 160, 80, 80, ShapeStyle.Stroked(PdfColor.Red, 2f));
                })
                .ToBytes();

            AssertValidPdf(bytes);
        }

        [Test]
        public void DrawPolygon_Triangle_ProducesValidPdf()
        {
            var points = new List<(float x, float y)>
            {
                (150, 50), (250, 150), (50, 150)
            };
            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.A4, canvas =>
                    canvas.DrawPolygon(points, ShapeStyle.Filled(PdfColor.Green)))
                .ToBytes();

            AssertValidPdf(bytes);
        }

        [Test]
        public void DrawPolygon_TooFewPoints_DoesNotCrash()
        {
            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.A4, canvas =>
                {
                    canvas.DrawPolygon(null!, ShapeStyle.Filled(PdfColor.Red));
                    canvas.DrawPolygon(new List<(float, float)> { (10, 10) }, null);
                })
                .ToBytes();

            AssertValidPdf(bytes);
        }

        [Test]
        public void DrawCurve_SmoothCurve_ProducesValidPdf()
        {
            var points = new List<(float x, float y)>
            {
                (50, 100), (150, 50), (250, 100), (350, 50), (450, 100)
            };
            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.A4, canvas =>
                    canvas.DrawCurve(points, StrokeStyle.Default.WithWidth(2)))
                .ToBytes();

            AssertValidPdf(bytes);
        }

        [Test]
        public void DrawCurve_TwoPoints_RendersLine()
        {
            var points = new List<(float x, float y)> { (50, 50), (200, 50) };
            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.A4, canvas =>
                    canvas.DrawCurve(points))
                .ToBytes();

            AssertValidPdf(bytes);
        }

        // ── images ────────────────────────────────────────────────────────────

        [Test]
        public void DrawImage_RawRgb_ProducesValidPdf()
        {
            // Create a 4x4 red image (raw RGB bytes)
            int w = 4, h = 4;
            var rgb = new byte[w * h * 3];
            for (int i = 0; i < rgb.Length; i += 3) { rgb[i] = 255; }

            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.A4, canvas =>
                    canvas.DrawImage(rgb, w, h, isJpeg: false, x: 50, y: 50, width: 100, height: 100))
                .ToBytes();

            AssertValidPdf(bytes);
        }

        [Test]
        public void DrawImage_MinimalJpeg_ProducesValidPdf()
        {
            // Smallest valid JPEG: 1x1 pixel white
            byte[] jpeg = Convert.FromBase64String(
                "/9j/4AAQSkZJRgABAQEASABIAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8U" +
                "HRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgN" +
                "DRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIy" +
                "MjL/wAARCAABAAEDASIAAhEBAxEB/8QAFAABAAAAAAAAAAAAAAAAAAAACf/EABQQAQAAAAAA" +
                "AAAAAAAAAAAAAAD/xAAUAQEAAAAAAAAAAAAAAAAAAAAA/8QAFBEBAAAAAAAAAAAAAAAAAAAAAP/" +
                "aAAwDAQACEQMRAD8AJQAB/9k=");

            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.A4, canvas =>
                    canvas.DrawImage(jpeg, 1, 1, isJpeg: true, x: 50, y: 50, width: 72, height: 72))
                .ToBytes();

            AssertValidPdf(bytes);
        }

        // ── links and tooltips ────────────────────────────────────────────────

        [Test]
        public void AddLink_ProducesAnnotation()
        {
            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.A4, canvas =>
                {
                    canvas.DrawText("Click me!", 50, 50);
                    canvas.AddLink(50, 40, 150, 20, "https://example.com");
                })
                .ToBytes();

            AssertValidPdf(bytes);
        }

        [Test]
        public void AddTooltip_ProducesAnnotation()
        {
            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.A4, canvas =>
                {
                    canvas.DrawText("Hover over me", 50, 50);
                    canvas.AddTooltip(50, 40, 200, 20, "This is a tooltip");
                })
                .ToBytes();

            AssertValidPdf(bytes);
        }

        // ── measurement ───────────────────────────────────────────────────────

        [Test]
        public void MeasureTextWidth_StandardFont_ReturnsPositive()
        {
            var doc    = PdfDocument.Create();
            var canvas = doc.AddPage(PageSizes.A4);
            float w = canvas.MeasureTextWidth("Hello World", TextStyle.Default.WithSize(12));
            Assert.That(w, Is.GreaterThan(0));
        }

        [Test]
        public void MeasureTextWidth_LongerText_IsWider()
        {
            var doc    = PdfDocument.Create();
            var canvas = doc.AddPage(PageSizes.A4);
            var style  = TextStyle.Default.WithSize(12);
            float wShort = canvas.MeasureTextWidth("Hi",          style);
            float wLong  = canvas.MeasureTextWidth("Hello World", style);
            Assert.That(wLong, Is.GreaterThan(wShort));
        }

        [Test]
        public void MeasureTextWidth_EmbeddedFont_ReturnsTtfBasedWidth()
        {
            string? fontPath = FindSystemFont();
            Assume.That(fontPath, Is.Not.Null);

            var doc    = PdfDocument.Create();
            var canvas = doc.AddPage(PageSizes.A4);
            var style  = TextStyle.Default.WithFontFile(fontPath!).WithSize(12);
            float w = canvas.MeasureTextWidth("Hello", style);
            Assert.That(w, Is.GreaterThan(0));
        }

        [Test]
        public void MeasureTextWidth_Empty_IsZero()
        {
            var doc    = PdfDocument.Create();
            var canvas = doc.AddPage(PageSizes.A4);
            Assert.That(canvas.MeasureTextWidth("", null), Is.EqualTo(0f));
        }

        // ── canvas chaining ───────────────────────────────────────────────────

        [Test]
        public void Canvas_FluentChain_AllMethodsReturnThis()
        {
            var doc    = PdfDocument.Create();
            var canvas = doc.AddPage(PageSizes.A4);

            var points = new List<(float x, float y)> { (10, 10), (20, 20), (30, 10) };

            var returned = canvas
                .DrawText("Chained text", 50, 50)
                .DrawLine(10, 10, 100, 10)
                .DrawRectangle(10, 20, 80, 40)
                .DrawEllipse(10, 80, 80, 40)
                .DrawPolygon(points)
                .DrawCurve(points)
                .AddLink(10, 10, 50, 20, "https://example.com")
                .AddTooltip(10, 40, 50, 20, "tip");

            Assert.That(returned, Is.SameAs(canvas));
        }

        // ── document chaining ─────────────────────────────────────────────────

        [Test]
        public void Document_FluentChain_MetadataReturnsThis()
        {
            var doc = PdfDocument.Create();
            var returned = doc
                .WithTitle("T")
                .WithAuthor("A")
                .WithSubject("S")
                .WithCreator("C");
            Assert.That(returned, Is.SameAs(doc));
        }

        [Test]
        public void Document_CallbackAddPage_ReturnsDocument()
        {
            var doc = PdfDocument.Create();
            var returned = doc.AddPage(PageSizes.A4, _ => { });
            Assert.That(returned, Is.SameAs(doc));
        }

        // ── combined content ──────────────────────────────────────────────────

        [Test]
        public void ComplexPage_AllPrimitivesTogetherOnOnePage()
        {
            string? fontPath = FindSystemFont();

            var bytes = PdfDocument.Create()
                .WithTitle("Complex test")
                .AddPage(PageSizes.A4, canvas =>
                {
                    // Background rect
                    canvas.DrawRectangle(0, 0, PageSizes.A4.Width, PageSizes.A4.Height,
                        ShapeStyle.Filled(PdfColor.White));

                    // Text
                    canvas.DrawText("Title", 72, 60,
                        TextStyle.Default.WithSize(20).WithBold());
                    canvas.DrawText("Body paragraph text here.", 72, 90,
                        TextStyle.Default.WithSize(11));

                    // TTF text if available
                    if (fontPath != null)
                        canvas.DrawText("TTF: " + fontPath, 72, 120,
                            TextStyle.Default.WithFontFile(fontPath).WithSize(10));

                    // Shapes
                    canvas.DrawLine(72, 140, 400, 140, StrokeStyle.Default.WithWidth(0.5f));
                    canvas.DrawRectangle(72, 160, 100, 60,
                        ShapeStyle.Filled(PdfColor.LightGray).WithStroke(PdfColor.Gray));
                    canvas.DrawEllipse(200, 160, 100, 60,
                        ShapeStyle.Filled(PdfColor.Blue).WithNoFill().WithStroke(PdfColor.Blue, 2f));

                    // Polygon
                    canvas.DrawPolygon(
                        new List<(float, float)> { (320, 160), (380, 160), (350, 220) },
                        ShapeStyle.Filled(PdfColor.Yellow).WithStroke(PdfColor.Orange));

                    // Curve
                    canvas.DrawCurve(
                        new List<(float, float)> { (72, 260), (150, 230), (230, 270), (310, 240) },
                        StrokeStyle.Default.WithWidth(1.5f).WithColor(PdfColor.Red).Dashed());

                    // Dashed rect
                    canvas.DrawRectangle(72, 290, 300, 80,
                        ShapeStyle.Stroked(PdfColor.DarkGray, 1f).Dashed());

                    // Link over text
                    canvas.DrawText("Visit us", 72, 400);
                    canvas.AddLink(72, 390, 120, 16, "https://majorsilence.com");
                })
                .ToBytes();

            AssertValidPdf(bytes);
            using var pig = PdfPig.Open(bytes);
            Assert.That(pig.NumberOfPages, Is.EqualTo(1));
        }

        // ── guard clauses ─────────────────────────────────────────────────────

        [Test]
        public void Save_NullStream_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                PdfDocument.Create().AddPage(PageSizes.A4, _ => { }).Save((Stream)null!));
        }

        [Test]
        public void Save_NullPath_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                PdfDocument.Create().AddPage(PageSizes.A4, _ => { }).Save((string)null!));
        }

        [Test]
        public void AddPage_NullCallback_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                PdfDocument.Create().AddPage(PageSizes.A4, (Action<PdfCanvas>)null!));
        }
    }
}
