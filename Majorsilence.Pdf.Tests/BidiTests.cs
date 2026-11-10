// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Majorsilence.Pdf.Text;
using NUnit.Framework;
using PdfPig = UglyToad.PdfPig.PdfDocument;

namespace Majorsilence.Pdf.Tests
{
    /// <summary>
    /// The Unicode Bidirectional Algorithm (UAX #9) and right-to-left text on the canvas. The
    /// algorithm was also checked against all of BidiTest.txt and BidiCharacterTest.txt; these are
    /// the cases a report meets.
    /// </summary>
    [TestFixture]
    public class BidiTests
    {
        // The text of each visual run, left to right, with the run's direction.
        private static string Visual(string text, bool rtl, bool overrideDirection = false) =>
            string.Join("|", UnicodeBidi.GetVisualRuns(text, rtl, overrideDirection)
                .Select(r => (r.IsRightToLeft ? "<" : ">") + text.Substring(r.Start, r.Length)));

        [Test]
        public void LeftToRightText_IsOneRun()
        {
            Assert.That(Visual("Hello, world", rtl: false), Is.EqualTo(">Hello, world"));
            Assert.That(UnicodeBidi.HasRightToLeftOrControls("Hello, world 123"), Is.False);
        }

        [Test]
        public void HebrewInEnglish_IsARightToLeftRunInPlace()
        {
            Assert.That(Visual("one שלום two", rtl: false), Is.EqualTo(">one |<שלום|> two"));
        }

        [Test]
        public void EnglishInHebrew_RunsAreReversed()
        {
            // The right-to-left paragraph displays its last run first (at the left).
            Assert.That(Visual("שלום Hello World", rtl: true), Is.EqualTo(">Hello World|<שלום "));
        }

        [Test]
        public void NumberInArabic_StaysLeftToRight()
        {
            var runs = UnicodeBidi.GetVisualRuns("العدد 123", rightToLeft: true);
            var digits = runs.Single(r => "العدد 123".Substring(r.Start, r.Length) == "123");
            Assert.That(digits.IsRightToLeft, Is.False, "digits are a left-to-right run (level 2)");
            Assert.That(digits.Level, Is.EqualTo(2));
            Assert.That(runs.First().Start, Is.EqualTo(digits.Start), "and display first, at the left");
        }

        [Test]
        public void PriceInHebrew_KeepsCurrencyWithItsNumber()
        {
            // W5: the dollar sign is part of the number beside it.
            var levels = UnicodeBidi.ResolveLevels(Cps("מחיר $10"), 1);
            Assert.That(levels.Skip(5).ToArray(), Is.EqualTo(new byte[] { 2, 2, 2 }));
        }

        [Test]
        public void Brackets_TakeTheDirectionOfWhatTheyEnclose()
        {
            // N0: English in brackets after English, in a right-to-left paragraph: the brackets
            // go with the English around them rather than with the paragraph.
            var levels = UnicodeBidi.ResolveLevels(Cps("a (b) c"), 1);
            Assert.That(levels[2], Is.EqualTo(2), "opening bracket");
            Assert.That(levels[4], Is.EqualTo(2), "closing bracket");
            // After a Hebrew letter they go with the paragraph instead.
            levels = UnicodeBidi.ResolveLevels(Cps("א (b) c"), 1);
            Assert.That(levels[2], Is.EqualTo(1));
            Assert.That(levels[4], Is.EqualTo(1));
        }

        [Test]
        public void Isolates_KeepTheirContentApart()
        {
            // RLI ... PDI: an isolated right-to-left word inside English is reordered on its own.
            Assert.That(Visual("a ⁧ב ג⁩ c", rtl: false), Does.Contain("<ב ג"));
        }

        [Test]
        public void TrailingSpaces_TakeTheParagraphLevel()
        {
            var levels = UnicodeBidi.ResolveLevels(Cps("abc  "), 1);
            Assert.That(levels[3], Is.EqualTo(1));
            Assert.That(levels[4], Is.EqualTo(1));
        }

        [Test]
        public void Override_IsOneRunInTheBaseDirection()
        {
            Assert.That(Visual("abc שלום", rtl: true, overrideDirection: true), Is.EqualTo("<abc שלום"));
        }

        [Test]
        public void SurrogatePairs_StayWhole()
        {
            string text = "a\U0001F600ב";
            var runs = UnicodeBidi.GetVisualRuns(text, rightToLeft: false);
            Assert.That(runs.Sum(r => r.Length), Is.EqualTo(text.Length));
            Assert.That(runs.All(r => !char.IsLowSurrogate(text[r.Start])), Is.True);
        }

        [Test]
        public void Classes_FromTheCharacterDatabase()
        {
            Assert.That(UnicodeBidi.GetClass('a'), Is.EqualTo(BidiClass.L));
            Assert.That(UnicodeBidi.GetClass(0x05D0), Is.EqualTo(BidiClass.R));
            Assert.That(UnicodeBidi.GetClass(0x0627), Is.EqualTo(BidiClass.AL));
            Assert.That(UnicodeBidi.GetClass(0x0661), Is.EqualTo(BidiClass.AN));
            Assert.That(UnicodeBidi.GetClass('5'), Is.EqualTo(BidiClass.EN));
            Assert.That(UnicodeBidi.GetClass(0x0300), Is.EqualTo(BidiClass.NSM));
            Assert.That(UnicodeBidi.GetClass(0x2067), Is.EqualTo(BidiClass.RLI));
            Assert.That(UnicodeBidi.GetClass(0x1E900), Is.EqualTo(BidiClass.R), "Adlam, outside the BMP");
        }

        private static List<int> Cps(string s)
        {
            var list = new List<int>();
            for (int i = 0; i < s.Length; i++)
            {
                if (char.IsHighSurrogate(s[i])) { list.Add(char.ConvertToUtf32(s[i], s[i + 1])); i++; }
                else list.Add(s[i]);
            }
            return list;
        }

        // ── the canvas, without a shaper ──────────────────────────────────────

        private static string BundledFontsDir => Majorsilence.Forms.Drawing.FontResourceLoader.GetFontDirectory();

        private static IReadOnlyList<UglyToad.PdfPig.Content.Letter> Draw(string text, TextStyle style)
        {
            var reg = new FontRegistry().AddDirectory(BundledFontsDir).AddFallback("LiberationSans").AddFallback("NotoSansHebrew");
            byte[] pdf = PdfDocument.Create().WithFontRegistry(reg)
                .AddPage(PageSizes.A4, c => c.DrawText(text, 300, 100, style))
                .ToBytes();
            using var doc = PdfPig.Open(pdf);
            return doc.GetPage(1).Letters.Where(l => !string.IsNullOrWhiteSpace(l.Value)).ToList();
        }

        [Test]
        public void Canvas_HebrewWithDigits_IsInDisplayOrder()
        {
            Assume.That(Directory.Exists(BundledFontsDir));
            var letters = Draw("שלום 2026", TextStyle.Default.WithFamily("LiberationSans").WithSize(14).WithRightToLeft());

            var ordered = letters.OrderBy(l => l.StartBaseLine.X).Select(l => l.Value).ToArray();
            Assert.That(string.Concat(ordered), Is.EqualTo("2026" + "םולש"), "digits left to right at the left, then the Hebrew from its last letter");
        }

        [Test]
        public void Canvas_RightToLeftWithRightAlignment_EndsAtX()
        {
            Assume.That(Directory.Exists(BundledFontsDir));
            var letters = Draw("שלום", TextStyle.Default.WithFamily("NotoSansHebrew").WithSize(14)
                .WithRightToLeft().WithAlignment(TextAlignment.Right));
            Assert.That((float)letters.Max(l => l.EndBaseLine.X), Is.EqualTo(300f).Within(0.5f));
        }

        [Test]
        public void Canvas_LatinWithRightToLeftFlag_IsNoLongerReversed()
        {
            // The flag is the paragraph direction: a Latin word in it still reads left to right.
            Assume.That(Directory.Exists(BundledFontsDir));
            var letters = Draw("abc", TextStyle.Default.WithFamily("LiberationSans").WithSize(14).WithRightToLeft());
            Assert.That(string.Concat(letters.OrderBy(l => l.StartBaseLine.X).Select(l => l.Value)), Is.EqualTo("abc"));
        }

        [Test]
        public void Canvas_MirrorsBracketsInRightToLeftText()
        {
            Assume.That(Directory.Exists(BundledFontsDir));
            // "(שלום)" in a right-to-left paragraph: the opening bracket is at the right, drawn as ')'... mirrored to '('.
            var letters = Draw("(שלום)", TextStyle.Default.WithFamily("NotoSansHebrew").WithSize(14).WithRightToLeft());
            var ordered = letters.OrderBy(l => l.StartBaseLine.X).Select(l => l.Value).ToArray();
            Assert.That(ordered.First(), Is.EqualTo("("), "the closing bracket, drawn at the left, is mirrored");
            Assert.That(ordered.Last(), Is.EqualTo(")"));
        }

        [Test]
        public void Canvas_WordSpacing_WidensEachSpace()
        {
            Assume.That(Directory.Exists(BundledFontsDir));
            var reg = new FontRegistry().AddDirectory(BundledFontsDir);
            var style = TextStyle.Default.WithFamily("LiberationSans").WithSize(12);
            float plain = 0, spaced = 0;
            PdfDocument.Create().WithFontRegistry(reg).AddPage(PageSizes.A4, c =>
            {
                plain = c.MeasureTextWidth("a b c", style);
                spaced = c.MeasureTextWidth("a b c", style.WithWordSpacing(5));
            });
            Assert.That(spaced - plain, Is.EqualTo(10f).Within(0.01f));
        }
    }
}
