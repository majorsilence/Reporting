// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using NUnit.Framework;

namespace Majorsilence.Reporting.RdlDesign.Tests
{
    [TestFixture]
    public class MeasureUnitsTests
    {
        [TestCase("mm", "mm")]
        [TestCase("MM", "mm")]
        [TestCase(" mm ", "mm")]
        [TestCase("cm", "cm")]
        [TestCase("inches", "inches")]
        [TestCase("", "inches")]
        [TestCase(null, "inches")]
        [TestCase("bogus", "inches")]
        public void Normalize(string input, string expected)
        {
            Assert.That(MeasureUnits.Normalize(input), Is.EqualTo(expected));
        }

        [TestCase("mm", true)]
        [TestCase("cm", true)]
        [TestCase("inches", false)]
        [TestCase(null, false)]
        public void IsMetric(string units, bool expected)
        {
            Assert.That(MeasureUnits.IsMetric(units), Is.EqualTo(expected));
        }

        [TestCase("inches", 1.0, 1.0)]
        [TestCase("cm", 1.0, 2.54)]
        [TestCase("mm", 1.0, 25.4)]
        [TestCase("mm", 0.5, 12.7)]
        public void FromInches(string units, double inches, double expected)
        {
            Assert.That(MeasureUnits.FromInches(inches, units), Is.EqualTo(expected).Within(1e-9));
        }

        [Test]
        public void FromPoints_OneInchIs72Points()
        {
            Assert.That(MeasureUnits.FromPoints(72, "mm"), Is.EqualTo(25.4).Within(1e-9));
            Assert.That(MeasureUnits.FromPoints(72, "cm"), Is.EqualTo(2.54).Within(1e-9));
            Assert.That(MeasureUnits.FromPoints(72, "inches"), Is.EqualTo(1.0).Within(1e-9));
        }
    }
}
