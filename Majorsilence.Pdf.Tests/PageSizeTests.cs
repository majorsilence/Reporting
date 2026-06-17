// Copyright (C) 2025 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using Majorsilence.Pdf;
using NUnit.Framework;

namespace Majorsilence.Pdf.Tests
{
    [TestFixture]
    public class PageSizeTests
    {
        [Test]
        public void PdfPageSize_StoresDimensions()
        {
            var s = new PdfPageSize(612, 792);
            Assert.That(s.Width,  Is.EqualTo(612f));
            Assert.That(s.Height, Is.EqualTo(792f));
        }

        [Test]
        public void Landscape_SwapsWidthAndHeight()
        {
            var portrait  = PageSizes.A4;
            var landscape = portrait.Landscape();
            Assert.That(landscape.Width,  Is.EqualTo(portrait.Height));
            Assert.That(landscape.Height, Is.EqualTo(portrait.Width));
        }

        [Test]
        public void Portrait_WhenAlreadyPortrait_Unchanged()
        {
            var p = PageSizes.A4;
            Assert.That(p.Width, Is.LessThan(p.Height), "A4 should be portrait");
            var p2 = p.Portrait();
            Assert.That(p2.Width,  Is.EqualTo(p.Width));
            Assert.That(p2.Height, Is.EqualTo(p.Height));
        }

        [Test]
        public void Portrait_WhenLandscape_CorrectsDimensions()
        {
            var landscape = PageSizes.A4.Landscape();
            Assert.That(landscape.Width, Is.GreaterThan(landscape.Height));
            var portrait = landscape.Portrait();
            Assert.That(portrait.Width, Is.LessThanOrEqualTo(portrait.Height));
        }

        [Test]
        public void Letter_HasExpectedDimensions()
        {
            Assert.That(PageSizes.Letter.Width,  Is.EqualTo(612f));
            Assert.That(PageSizes.Letter.Height, Is.EqualTo(792f));
        }

        [Test]
        public void A4_IsNarrowerThanLetter()
        {
            Assert.That(PageSizes.A4.Width, Is.LessThan(PageSizes.Letter.Width));
        }

        [Test]
        [TestCase(nameof(PageSizes.A0))]
        [TestCase(nameof(PageSizes.A1))]
        [TestCase(nameof(PageSizes.A3))]
        [TestCase(nameof(PageSizes.A4))]
        [TestCase(nameof(PageSizes.A5))]
        [TestCase(nameof(PageSizes.Letter))]
        [TestCase(nameof(PageSizes.Legal))]
        [TestCase(nameof(PageSizes.Tabloid))]
        public void AllSizes_HavePositiveDimensions(string sizeName)
        {
            var prop = typeof(PageSizes).GetProperty(sizeName);
            Assert.That(prop, Is.Not.Null, $"PageSizes.{sizeName} not found");
            var size = (PdfPageSize)prop!.GetValue(null)!;
            Assert.That(size.Width,  Is.GreaterThan(0));
            Assert.That(size.Height, Is.GreaterThan(0));
        }

        [Test]
        public void LargerSeries_BiggerThanSmaller()
        {
            Assert.That(PageSizes.A3.Width, Is.GreaterThan(PageSizes.A4.Width));
            Assert.That(PageSizes.Legal.Height, Is.GreaterThan(PageSizes.Letter.Height));
        }
    }
}
