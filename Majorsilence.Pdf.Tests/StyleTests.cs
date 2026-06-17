// Copyright (C) 2025 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using Majorsilence.Pdf;
using NUnit.Framework;

namespace Majorsilence.Pdf.Tests
{
    [TestFixture]
    public class TextStyleTests
    {
        [Test]
        public void Default_HasExpectedValues()
        {
            var s = TextStyle.Default;
            Assert.That(s.FontFamily,   Is.EqualTo("Helvetica"));
            Assert.That(s.FontFilePath, Is.Null);
            Assert.That(s.FontSize,     Is.EqualTo(12f));
            Assert.That(s.Color,        Is.EqualTo(PdfColor.Black));
            Assert.That(s.IsBold,       Is.False);
            Assert.That(s.IsItalic,     Is.False);
            Assert.That(s.Alignment,    Is.EqualTo(TextAlignment.Left));
            Assert.That(s.Decoration,   Is.EqualTo(TextDecoration.None));
            Assert.That(s.IsVertical,   Is.False);
        }

        [Test]
        public void WithFamily_ChangesFamily_OriginalUnchanged()
        {
            var orig = TextStyle.Default;
            var changed = orig.WithFamily("Times-Roman");
            Assert.That(changed.FontFamily, Is.EqualTo("Times-Roman"));
            Assert.That(orig.FontFamily,    Is.EqualTo("Helvetica"), "original must not change");
        }

        [Test]
        public void WithFamily_NullFallsBackToHelvetica()
        {
            var s = TextStyle.Default.WithFamily(null!);
            Assert.That(s.FontFamily, Is.EqualTo("Helvetica"));
        }

        [Test]
        public void WithFontFile_SetsFontFileAndClearsNothing()
        {
            const string path = "/fonts/test.ttf";
            var s = TextStyle.Default.WithFontFile(path);
            Assert.That(s.FontFilePath, Is.EqualTo(path));
        }

        [Test]
        public void WithFamily_ClearsFontFilePath()
        {
            var s = TextStyle.Default.WithFontFile("/tmp/x.ttf").WithFamily("Courier");
            Assert.That(s.FontFilePath, Is.Null);
            Assert.That(s.FontFamily,   Is.EqualTo("Courier"));
        }

        [Test]
        public void WithSize_ChangesSize()
        {
            var s = TextStyle.Default.WithSize(24f);
            Assert.That(s.FontSize, Is.EqualTo(24f));
        }

        [Test]
        public void WithColor_ChangesColor()
        {
            var s = TextStyle.Default.WithColor(PdfColor.Red);
            Assert.That(s.Color, Is.EqualTo(PdfColor.Red));
        }

        [Test]
        public void WithBold_SetsAndClearsFlag()
        {
            var bold = TextStyle.Default.WithBold();
            Assert.That(bold.IsBold, Is.True);
            var notBold = bold.WithBold(false);
            Assert.That(notBold.IsBold, Is.False);
        }

        [Test]
        public void WithItalic_SetsAndClearsFlag()
        {
            var italic = TextStyle.Default.WithItalic();
            Assert.That(italic.IsItalic, Is.True);
            var notItalic = italic.WithItalic(false);
            Assert.That(notItalic.IsItalic, Is.False);
        }

        [Test]
        public void WithAlignment_AllValues()
        {
            foreach (TextAlignment a in System.Enum.GetValues(typeof(TextAlignment)))
            {
                var s = TextStyle.Default.WithAlignment(a);
                Assert.That(s.Alignment, Is.EqualTo(a));
            }
        }

        [Test]
        public void Decoration_Fluent()
        {
            Assert.That(TextStyle.Default.WithUnderline().Decoration,    Is.EqualTo(TextDecoration.Underline));
            Assert.That(TextStyle.Default.WithStrikethrough().Decoration, Is.EqualTo(TextDecoration.Strikethrough));
            Assert.That(TextStyle.Default.WithOverline().Decoration,     Is.EqualTo(TextDecoration.Overline));
            Assert.That(TextStyle.Default.WithUnderline().WithNoDecoration().Decoration, Is.EqualTo(TextDecoration.None));
        }

        [Test]
        public void WithVertical_SetsFlag()
        {
            var v = TextStyle.Default.WithVertical();
            Assert.That(v.IsVertical, Is.True);
            Assert.That(v.WithVertical(false).IsVertical, Is.False);
        }

        [Test]
        public void FluentChain_CombinesProperties()
        {
            var s = TextStyle.Default
                .WithFamily("Times-Roman")
                .WithSize(18)
                .WithBold()
                .WithItalic()
                .WithColor(PdfColor.Blue)
                .WithAlignment(TextAlignment.Center)
                .WithUnderline();

            Assert.That(s.FontFamily,  Is.EqualTo("Times-Roman"));
            Assert.That(s.FontSize,    Is.EqualTo(18f));
            Assert.That(s.IsBold,      Is.True);
            Assert.That(s.IsItalic,    Is.True);
            Assert.That(s.Color,       Is.EqualTo(PdfColor.Blue));
            Assert.That(s.Alignment,   Is.EqualTo(TextAlignment.Center));
            Assert.That(s.Decoration,  Is.EqualTo(TextDecoration.Underline));
        }
    }

    [TestFixture]
    public class StrokeStyleTests
    {
        [Test]
        public void Default_HasExpectedValues()
        {
            var s = StrokeStyle.Default;
            Assert.That(s.Width,     Is.EqualTo(1f));
            Assert.That(s.Color,     Is.EqualTo(PdfColor.Black));
            Assert.That(s.LineStyle, Is.EqualTo(LineStyle.Solid));
        }

        [Test]
        public void WithWidth_ChangesWidth_OriginalUnchanged()
        {
            var orig    = StrokeStyle.Default;
            var changed = orig.WithWidth(3f);
            Assert.That(changed.Width, Is.EqualTo(3f));
            Assert.That(orig.Width,    Is.EqualTo(1f));
        }

        [Test]
        public void WithColor_ChangesColor()
        {
            var s = StrokeStyle.Default.WithColor(PdfColor.Red);
            Assert.That(s.Color, Is.EqualTo(PdfColor.Red));
        }

        [Test]
        public void Dashed_SetsDashedStyle()
        {
            var s = StrokeStyle.Default.Dashed();
            Assert.That(s.LineStyle, Is.EqualTo(LineStyle.Dashed));
        }

        [Test]
        public void Dotted_SetsDottedStyle()
        {
            var s = StrokeStyle.Default.Dotted();
            Assert.That(s.LineStyle, Is.EqualTo(LineStyle.Dotted));
        }

        [Test]
        public void Solid_ResetsToDashedStyle()
        {
            var s = StrokeStyle.Default.Dashed().Solid();
            Assert.That(s.LineStyle, Is.EqualTo(LineStyle.Solid));
        }

        [Test]
        public void Chain_CombinesAll()
        {
            var s = StrokeStyle.Default.WithWidth(2.5f).WithColor(PdfColor.Green).Dashed();
            Assert.That(s.Width,     Is.EqualTo(2.5f));
            Assert.That(s.Color,     Is.EqualTo(PdfColor.Green));
            Assert.That(s.LineStyle, Is.EqualTo(LineStyle.Dashed));
        }
    }

    [TestFixture]
    public class ShapeStyleTests
    {
        [Test]
        public void Empty_HasNoFillAndNoStroke()
        {
            var s = ShapeStyle.Empty;
            Assert.That(s.FillColor,   Is.Null);
            Assert.That(s.StrokeColor, Is.Null);
            Assert.That(s.HasFill,     Is.False);
            Assert.That(s.HasStroke,   Is.False);
        }

        [Test]
        public void Filled_HasFillNoStroke()
        {
            var s = ShapeStyle.Filled(PdfColor.Blue);
            Assert.That(s.FillColor,   Is.EqualTo(PdfColor.Blue));
            Assert.That(s.StrokeColor, Is.Null);
            Assert.That(s.HasFill,     Is.True);
            Assert.That(s.HasStroke,   Is.False);
        }

        [Test]
        public void Stroked_HasStrokeNoFill()
        {
            var s = ShapeStyle.Stroked(PdfColor.Red, 2f);
            Assert.That(s.StrokeColor, Is.EqualTo(PdfColor.Red));
            Assert.That(s.StrokeWidth, Is.EqualTo(2f));
            Assert.That(s.FillColor,   Is.Null);
            Assert.That(s.HasFill,     Is.False);
            Assert.That(s.HasStroke,   Is.True);
        }

        [Test]
        public void WithFill_AddsFill()
        {
            var s = ShapeStyle.Empty.WithFill(PdfColor.Yellow);
            Assert.That(s.FillColor, Is.EqualTo(PdfColor.Yellow));
            Assert.That(s.HasFill,   Is.True);
        }

        [Test]
        public void WithNoFill_RemovesFill()
        {
            var s = ShapeStyle.Filled(PdfColor.Red).WithNoFill();
            Assert.That(s.HasFill, Is.False);
        }

        [Test]
        public void WithStroke_AddsStroke()
        {
            var s = ShapeStyle.Empty.WithStroke(PdfColor.Black, 1.5f);
            Assert.That(s.StrokeColor, Is.EqualTo(PdfColor.Black));
            Assert.That(s.StrokeWidth, Is.EqualTo(1.5f));
            Assert.That(s.HasStroke,   Is.True);
        }

        [Test]
        public void WithNoStroke_RemovesStroke()
        {
            var s = ShapeStyle.Stroked(PdfColor.Black).WithNoStroke();
            Assert.That(s.HasStroke, Is.False);
        }

        [Test]
        public void ZeroStrokeWidth_MeansNoStroke()
        {
            var s = ShapeStyle.Empty.WithStroke(PdfColor.Black, 0f);
            Assert.That(s.HasStroke, Is.False);
        }

        [Test]
        public void Dashed_SetsLineStyle()
        {
            var s = ShapeStyle.Stroked(PdfColor.Black).Dashed();
            Assert.That(s.LineStyle, Is.EqualTo(LineStyle.Dashed));
        }

        [Test]
        public void FilledAndStroked_BothActive()
        {
            var s = ShapeStyle.Filled(PdfColor.LightGray).WithStroke(PdfColor.DarkGray, 1f);
            Assert.That(s.HasFill,   Is.True);
            Assert.That(s.HasStroke, Is.True);
        }

        [Test]
        public void WithFill_OriginalUnchanged()
        {
            var orig    = ShapeStyle.Empty;
            var changed = orig.WithFill(PdfColor.Red);
            Assert.That(orig.HasFill, Is.False, "original must not change");
            Assert.That(changed.HasFill, Is.True);
        }
    }
}
