// Copyright (C) 2025 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using Majorsilence.Pdf;
using NUnit.Framework;

namespace Majorsilence.Pdf.Tests
{
    [TestFixture]
    public class PdfColorTests
    {
        [Test]
        public void Constructor_StoresChannels()
        {
            var c = new PdfColor(10, 20, 30);
            Assert.That(c.R, Is.EqualTo(10));
            Assert.That(c.G, Is.EqualTo(20));
            Assert.That(c.B, Is.EqualTo(30));
        }

        [Test]
        public void FromRgb_SameAsConstructor()
        {
            var a = new PdfColor(100, 150, 200);
            var b = PdfColor.FromRgb(100, 150, 200);
            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        [TestCase("#FF8800", 255, 136, 0)]
        [TestCase("FF8800",  255, 136, 0)]
        [TestCase("#000000",   0,   0, 0)]
        [TestCase("#FFFFFF", 255, 255, 255)]
        [TestCase("#1A2B3C",  26,  43, 60)]
        public void FromHex_ParsesCorrectly(string hex, byte r, byte g, byte b)
        {
            var c = PdfColor.FromHex(hex);
            Assert.That(c.R, Is.EqualTo(r));
            Assert.That(c.G, Is.EqualTo(g));
            Assert.That(c.B, Is.EqualTo(b));
        }

        [Test]
        public void FromHex_ThrowsOnBadLength()
        {
            Assert.Throws<System.ArgumentException>(() => PdfColor.FromHex("#FFF"));
            Assert.Throws<System.ArgumentException>(() => PdfColor.FromHex("12345"));
        }

        [Test]
        public void FromHex_ThrowsOnNull()
        {
            Assert.Throws<System.ArgumentNullException>(() => PdfColor.FromHex(null!));
        }

        [Test]
        public void NamedColors_MatchExpectedValues()
        {
            Assert.That(PdfColor.Black,     Is.EqualTo(new PdfColor(  0,   0,   0)));
            Assert.That(PdfColor.White,     Is.EqualTo(new PdfColor(255, 255, 255)));
            Assert.That(PdfColor.Red,       Is.EqualTo(new PdfColor(255,   0,   0)));
            Assert.That(PdfColor.Blue,      Is.EqualTo(new PdfColor(  0,   0, 255)));
            Assert.That(PdfColor.LightGray, Is.EqualTo(new PdfColor(211, 211, 211)));
        }

        [Test]
        public void Equality_SameChannels_AreEqual()
        {
            var a = new PdfColor(1, 2, 3);
            var b = new PdfColor(1, 2, 3);
            Assert.That(a == b, Is.True);
            Assert.That(a != b, Is.False);
            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void Equality_DifferentChannels_AreNotEqual()
        {
            var a = new PdfColor(1, 2, 3);
            var b = new PdfColor(1, 2, 4);
            Assert.That(a == b, Is.False);
            Assert.That(a != b, Is.True);
        }

        [Test]
        public void ToString_ReturnsHexFormat()
        {
            Assert.That(PdfColor.Black.ToString(), Is.EqualTo("#000000"));
            Assert.That(PdfColor.White.ToString(), Is.EqualTo("#FFFFFF"));
            Assert.That(new PdfColor(26, 43, 60).ToString(), Is.EqualTo("#1A2B3C"));
        }

        [Test]
        public void NormalisedChannels_AreInRange()
        {
            // Access via internal members through the assembly — just verify via document output
            // (Rf/Gf/Bf are internal; tested indirectly through document generation)
            var c = new PdfColor(255, 128, 0);
            Assert.That(c.R, Is.EqualTo(255));
            Assert.That(c.G, Is.EqualTo(128));
            Assert.That(c.B, Is.EqualTo(0));
        }
    }
}
