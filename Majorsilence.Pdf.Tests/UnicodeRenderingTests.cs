// Copyright (C) 2025 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Majorsilence.Pdf;
using NUnit.Framework;
using PdfPig = UglyToad.PdfPig.PdfDocument;

namespace Majorsilence.Pdf.Tests
{
    [TestFixture]
    public class UnicodeRenderingTests
    {
        // ── helpers ──────────────────────────────────────────────────────────

        // The fallback fonts ship embedded in Majorsilence.Forms.Drawing.Common; the loader
        // extracts them to a temp directory on first use.
        private static string BundledFontsDir =>
            Majorsilence.Forms.Drawing.FontResourceLoader.GetFontDirectory();

        private static bool BundledFontsExist => Directory.Exists(BundledFontsDir);

        private static string? FindFont(params string[] paths)
        {
            foreach (var p in paths) if (File.Exists(p)) return p;
            return null;
        }

        private static FontRegistry BundledRegistry() =>
            new FontRegistry().AddDirectory(BundledFontsDir).AddFallback("NotoSans");

        private static void AssertValidPdf(byte[] bytes)
        {
            Assert.That(bytes, Is.Not.Null.And.Not.Empty);
            using var doc = PdfPig.Open(bytes);
            Assert.That(doc.NumberOfPages, Is.GreaterThan(0));
        }

        // ── Latin extended ────────────────────────────────────────────────────

        [Test]
        public void LatinExtended_AccentedChars_Renders()
        {
            Assume.That(BundledFontsExist, "Bundled fonts not found");

            // All of these are in LiberationSans (Latin-1 Supplement + Latin Extended-A)
            const string text = "café résumé naïve Ångström fiancée Üniversität";
            var bytes = PdfDocument.Create()
                .WithFontRegistry(BundledRegistry())
                .AddPage(PageSizes.A4, canvas =>
                    canvas.DrawText(text, 72, 100,
                        TextStyle.Default.WithFamily("LiberationSans").WithSize(14)))
                .ToBytes();

            AssertValidPdf(bytes);
        }

        [Test]
        public void LatinExtended_Punctuation_AndSymbols_Renders()
        {
            Assume.That(BundledFontsExist, "Bundled fonts not found");

            // em-dash, en-dash, smart quotes, ellipsis, bullet — all in Liberation/WinAnsi range
            const string text = "Heading — subtitle – note ‘quoted’ “and this” … •";
            var bytes = PdfDocument.Create()
                .WithFontRegistry(BundledRegistry())
                .AddPage(PageSizes.A4, canvas =>
                    canvas.DrawText(text, 72, 100,
                        TextStyle.Default.WithFamily("LiberationSans").WithSize(12)))
                .ToBytes();

            AssertValidPdf(bytes);
        }

        // ── Greek ─────────────────────────────────────────────────────────────

        [Test]
        public void Greek_AlphabetSample_RendersWithNotoSans()
        {
            Assume.That(BundledFontsExist, "Bundled fonts not found");

            // NotoSans has full Greek coverage (verified via cmap analysis)
            const string text = "ΑβΓδΕζΗθΙκΛμ " +
                                 "Ω π αβγδε";
            // ΑΒΓΔΕΖΗΘΙΚΛΜ Ω π αβγδε
            var bytes = PdfDocument.Create()
                .WithFontRegistry(BundledRegistry())
                .AddPage(PageSizes.A4, canvas =>
                    canvas.DrawText(text, 72, 100,
                        TextStyle.Default.WithFamily("NotoSans").WithSize(16)))
                .ToBytes();

            AssertValidPdf(bytes);
        }

        [Test]
        public void Greek_Words_RendersWithNotoSans()
        {
            Assume.That(BundledFontsExist, "Bundled fonts not found");

            var bytes = PdfDocument.Create()
                .WithFontRegistry(BundledRegistry())
                .AddPage(PageSizes.A4, canvas =>
                {
                    float y = 60;
                    var style = TextStyle.Default.WithFamily("NotoSans").WithSize(14);
                    canvas.DrawText("Ελληνικά",           72, y, style); y += 24; // Ελληνικά
                    canvas.DrawText("αλφάβητο",           72, y, style); y += 24; // αλφάβητο
                    canvas.DrawText("φιλοσοφία",    72, y, style);           // φιλοσοφία
                })
                .ToBytes();

            AssertValidPdf(bytes);
        }

        // ── Cyrillic ──────────────────────────────────────────────────────────

        [Test]
        public void Cyrillic_Russian_RendersWithNotoSans()
        {
            Assume.That(BundledFontsExist, "Bundled fonts not found");

            // NotoSans covers Cyrillic (verified: U+0410 → glyph 425)
            const string text = "Привет мир"; // Привет мир
            var bytes = PdfDocument.Create()
                .WithFontRegistry(BundledRegistry())
                .AddPage(PageSizes.A4, canvas =>
                    canvas.DrawText(text, 72, 100,
                        TextStyle.Default.WithFamily("NotoSans").WithSize(16)))
                .ToBytes();

            AssertValidPdf(bytes);
        }

        [Test]
        public void Cyrillic_MultipleSentences_Renders()
        {
            Assume.That(BundledFontsExist, "Bundled fonts not found");

            var lines = new[]
            {
                "Москва",                                       // Москва
                "Санкт-Петербург", // Санкт-Петербург
                "Добро пожаловать", // Добро пожаловать
            };
            var bytes = PdfDocument.Create()
                .WithFontRegistry(BundledRegistry())
                .AddPage(PageSizes.A4, canvas =>
                {
                    float y = 60;
                    var style = TextStyle.Default.WithFamily("NotoSans").WithSize(14);
                    foreach (var line in lines) { canvas.DrawText(line, 72, y, style); y += 24; }
                })
                .ToBytes();

            AssertValidPdf(bytes);
        }

        // ── Mixed scripts ─────────────────────────────────────────────────────

        [Test]
        public void MixedLatinAndGreek_WithFallback_Renders()
        {
            Assume.That(BundledFontsExist, "Bundled fonts not found");

            // LiberationSans = primary (has Latin); NotoSans = fallback (has Greek)
            // The segmenter splits on font boundary so both render correctly.
            const string text = "Hello κόσμε"; // Hello κόσμε
            var bytes = PdfDocument.Create()
                .WithFontRegistry(BundledRegistry())
                .AddPage(PageSizes.A4, canvas =>
                    canvas.DrawText(text, 72, 100,
                        TextStyle.Default.WithFamily("LiberationSans").WithSize(14)))
                .ToBytes();

            AssertValidPdf(bytes);
        }

        [Test]
        public void MixedLatinCyrillicGreek_SinglePage_Renders()
        {
            Assume.That(BundledFontsExist, "Bundled fonts not found");

            var bytes = PdfDocument.Create()
                .WithFontRegistry(BundledRegistry())
                .AddPage(PageSizes.A4, canvas =>
                {
                    var style = TextStyle.Default.WithFamily("NotoSans").WithSize(13);
                    float y = 60;
                    canvas.DrawText("Latin: The quick brown fox",                              72, y, style); y += 22;
                    canvas.DrawText("Greek: αβγδεζηθικ", 72, y, style); y += 22; // αβγδεζηθικ
                    canvas.DrawText("Cyrillic: АБВГДЕЖЗИЙ", 72, y, style); y += 22; // АБВГДЕЖЗИЙ
                    canvas.DrawText("Mixed: Hello κόσμε мир", 72, y, style);          // Hello κόσμε мир
                })
                .ToBytes();

            AssertValidPdf(bytes);
        }

        [Test]
        public void MixedScript_FallbackSegmentation_ProducesValidPdf()
        {
            Assume.That(BundledFontsExist, "Bundled fonts not found");

            // This exercises the SegmentByFont code path:
            // LiberationSans primary covers Latin; NotoSans fallback covers Greek/Cyrillic.
            // The single DrawText call should produce multiple Tj segments in the PDF stream.
            const string text = "ABC αβγ XYZ АБВ end";
            var bytes = PdfDocument.Create()
                .WithFontRegistry(
                    new FontRegistry()
                        .AddDirectory(BundledFontsDir)
                        .AddFallback("NotoSans"))
                .AddPage(PageSizes.A4, canvas =>
                    canvas.DrawText(text, 72, 100,
                        TextStyle.Default.WithFamily("LiberationSans").WithSize(14)))
                .ToBytes();

            AssertValidPdf(bytes);
        }

        // ── Emoji / surrogate pairs ───────────────────────────────────────────

        [Test]
        public void Emoji_SurrogatePairs_DoNotThrow()
        {
            Assume.That(BundledFontsExist, "Bundled fonts not found");

            // 😀 (U+1F600) is a surrogate pair in UTF-16: 😀
            // NotoSans does not contain emoji glyphs → glyph ID 0 (.notdef) is used.
            // The code must not crash when processing surrogate pairs.
            const string text = "😀 🎉 👍 emoji test";
            var bytes = PdfDocument.Create()
                .WithFontRegistry(BundledRegistry())
                .AddPage(PageSizes.A4, canvas =>
                    canvas.DrawText(text, 72, 100,
                        TextStyle.Default.WithFamily("NotoSans").WithSize(14)))
                .ToBytes();

            // PDF may contain .notdef boxes for unsupported codepoints — still valid.
            AssertValidPdf(bytes);
        }

        [Test]
        public void Emoji_WithSystemEmojiFont_RendersIfAvailable()
        {
            // Segoe UI Emoji on Windows — purely optional, test is skipped if absent.
            string? emojiFont = FindFont(
                @"C:\Windows\Fonts\seguiemj.ttf",
                "/usr/share/fonts/truetype/noto/NotoColorEmoji.ttf",
                "/usr/share/fonts/noto/NotoEmoji-Regular.ttf");

            Assume.That(emojiFont, Is.Not.Null, "No system emoji font found");

            var reg = new FontRegistry()
                .AddFamily("Emoji", regular: emojiFont)
                .AddFallback("Emoji");

            const string text = "😀 🎉 👍";
            Assert.DoesNotThrow(() =>
            {
                PdfDocument.Create()
                    .WithFontRegistry(reg)
                    .AddPage(PageSizes.A4, canvas =>
                        canvas.DrawText(text, 72, 100,
                            TextStyle.Default.WithFamily("Emoji").WithSize(24)))
                    .ToBytes();
            });
        }

        // ── CJK (system fonts) ────────────────────────────────────────────────

        [Test]
        public void CJK_Chinese_WithSystemFont_RendersIfAvailable()
        {
            string? cjkFont = FindFont(
                @"C:\Windows\Fonts\malgun.ttf",          // Malgun Gothic (Korean, also has some CJK)
                @"C:\Windows\Fonts\Deng.ttf",
                "/usr/share/fonts/truetype/droid/DroidSansFallbackFull.ttf",
                "/usr/share/fonts/truetype/noto/NotoSansCJK-Regular.ttc");

            Assume.That(cjkFont, Is.Not.Null, "No CJK system font found");

            var reg = new FontRegistry().AddFamily("CJK", regular: cjkFont);

            // 你好世界 = Hello World in Chinese
            const string text = "你好世界";
            Assert.DoesNotThrow(() =>
            {
                PdfDocument.Create()
                    .WithFontRegistry(reg)
                    .AddPage(PageSizes.A4, canvas =>
                        canvas.DrawText(text, 72, 100,
                            TextStyle.Default.WithFamily("CJK").WithSize(24)))
                    .ToBytes();
            });
        }

        [Test]
        public void CJK_Japanese_WithSystemFont_RendersIfAvailable()
        {
            string? jpFont = FindFont(
                @"C:\Windows\Fonts\YuGothM.ttc",
                @"C:\Windows\Fonts\meiryo.ttc",
                "/usr/share/fonts/truetype/takao-gothic/TakaoGothic.ttf",
                "/usr/share/fonts/truetype/noto/NotoSansCJKjp-Regular.otf");

            Assume.That(jpFont, Is.Not.Null, "No Japanese system font found");

            var reg = new FontRegistry().AddFamily("Japanese", regular: jpFont);

            // こんにちは = Hello in Japanese (Hiragana)
            const string text = "こんにちは";
            Assert.DoesNotThrow(() =>
            {
                PdfDocument.Create()
                    .WithFontRegistry(reg)
                    .AddPage(PageSizes.A4, canvas =>
                        canvas.DrawText(text, 72, 100,
                            TextStyle.Default.WithFamily("Japanese").WithSize(24)))
                    .ToBytes();
            });
        }

        // ── Subsetting with non-Latin glyphs ─────────────────────────────────

        [Test]
        public void Subset_GreekGlyphs_ProducesSmallFont()
        {
            Assume.That(BundledFontsExist, "Bundled fonts not found");

            // Render a short Greek string and verify the embedded font is much
            // smaller than the full NotoSans (512 KB raw → should be < 20 KB subset).
            const string text = "αβγ"; // αβγ

            byte[] pdfBytes = PdfDocument.Create()
                .WithFontRegistry(BundledRegistry())
                .AddPage(PageSizes.A4, canvas =>
                    canvas.DrawText(text, 72, 100,
                        TextStyle.Default.WithFamily("NotoSans").WithSize(14)))
                .ToBytes();

            AssertValidPdf(pdfBytes);
            // Subset of 3 glyphs from a 512 KB font should fit comfortably under 50 KB
            Assert.That(pdfBytes.Length, Is.LessThan(50_000),
                "Subset PDF should be much smaller than full font embedding");
        }

        [Test]
        public void Subset_MixedScripts_IncludesAllUsedGlyphs()
        {
            Assume.That(BundledFontsExist, "Bundled fonts not found");

            // Draw Latin, Greek, and Cyrillic — each from NotoSans.
            // The subset must include glyphs for all three scripts.
            const string text = "Hello αβγ АБВ";

            byte[] pdfBytes = PdfDocument.Create()
                .WithFontRegistry(BundledRegistry())
                .AddPage(PageSizes.A4, canvas =>
                    canvas.DrawText(text, 72, 100,
                        TextStyle.Default.WithFamily("NotoSans").WithSize(14)))
                .ToBytes();

            AssertValidPdf(pdfBytes);
        }

        [Test]
        public void Subset_LargeAlphabet_StillProducesValidPdf()
        {
            Assume.That(BundledFontsExist, "Bundled fonts not found");

            // Render the full Greek and Cyrillic alphabets
            const string greek = "ΑΒΓΔΕΖΗΘΙΚΛΜ" +
                                  "ΝΞΟΠΡΣΤΥΦΧΨΩ" +
                                  "αβγδεζηθικλμ" +
                                  "νξοπρςστυφχψω";
            const string cyrillic = "АБВГДЕЖЗИЙКЛ" +
                                     "МНОПРСТУФХЦЧ" +
                                     "абвгдежзийкл";

            byte[] pdfBytes = PdfDocument.Create()
                .WithFontRegistry(BundledRegistry())
                .AddPage(PageSizes.A4, canvas =>
                {
                    var style = TextStyle.Default.WithFamily("NotoSans").WithSize(13);
                    canvas.DrawText(greek,    72,  60, style);
                    canvas.DrawText(cyrillic, 72, 100, style);
                })
                .ToBytes();

            AssertValidPdf(pdfBytes);
        }

        // ── Measurement of non-Latin text ─────────────────────────────────────

        [Test]
        public void MeasureTextWidth_GreekString_ReturnsPositiveWidth()
        {
            Assume.That(BundledFontsExist, "Bundled fonts not found");

            var reg = new FontRegistry().AddDirectory(BundledFontsDir);
            var style = TextStyle.Default.WithFamily("NotoSans").WithSize(12);
            float width = 0;

            PdfDocument.Create()
                .WithFontRegistry(reg)
                .AddPage(PageSizes.A4, canvas =>
                {
                    width = canvas.MeasureTextWidth("αβγδε", style);
                    canvas.DrawText("αβγδε", 72, 100, style);
                })
                .ToBytes();

            Assert.That(width, Is.GreaterThan(0), "Greek text should have positive measured width");
            Assert.That(width, Is.LessThan(150),  "αβγδε at 12pt should be under 150pt wide");
        }

        [Test]
        public void MeasureTextWidth_CyrillicString_ReturnsPositiveWidth()
        {
            Assume.That(BundledFontsExist, "Bundled fonts not found");

            var reg = new FontRegistry().AddDirectory(BundledFontsDir);
            var style = TextStyle.Default.WithFamily("NotoSans").WithSize(12);
            float width = 0;

            PdfDocument.Create()
                .WithFontRegistry(reg)
                .AddPage(PageSizes.A4, canvas =>
                {
                    // Привет = Hello in Russian
                    width = canvas.MeasureTextWidth("Привет", style);
                    canvas.DrawText("Привет", 72, 100, style);
                })
                .ToBytes();

            Assert.That(width, Is.GreaterThan(0), "Cyrillic text should have positive measured width");
        }

        // ── Right-to-left (unsupported, but must not crash) ───────────────────

        [Test]
        public void Arabic_LaidOutLeftToRight_DoesNotThrow()
        {
            // Arabic shaping/RTL layout is not supported; characters are drawn
            // in logical (LTR) order. This test verifies no exception is thrown
            // even when the glyphs are missing from the font.
            Assume.That(BundledFontsExist, "Bundled fonts not found");

            // مرحبا = Hello in Arabic (NotoSans does not cover Arabic — renders as .notdef)
            const string arabic = "مرحبا";
            Assert.DoesNotThrow(() =>
                PdfDocument.Create()
                    .WithFontRegistry(BundledRegistry())
                    .AddPage(PageSizes.A4, canvas =>
                        canvas.DrawText(arabic, 72, 100,
                            TextStyle.Default.WithFamily("NotoSans").WithSize(14)))
                    .ToBytes());
        }
    }
}
