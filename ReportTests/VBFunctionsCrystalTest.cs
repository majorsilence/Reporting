using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System;

namespace ReportTests
{
    /// <summary>
    /// Semantics of the VBFunctions additions that exist for Crystal Reports formulas —
    /// functions VB.NET does not have (Val, NumericText, Remainder, ...) and the
    /// multi-argument CStr/Fix/Floor/Ceiling forms, whose second argument means something
    /// different from the VB.NET equivalents. These are pure static functions, so they are
    /// exercised directly rather than through a rendered report.
    /// </summary>
    [TestFixture]
    public class VBFunctionsCrystalTest
    {
        [TestCase("123abc", 123d)]      // longest leading numeric prefix
        [TestCase("12.5", 12.5d)]
        [TestCase("-5", -5d)]
        [TestCase("1 000", 1000d)]      // embedded spaces are ignored
        [TestCase("12.5.7", 12.5d)]     // stops at the second decimal point
        [TestCase("abc", 0d)]           // no numeric prefix at all
        [TestCase("", 0d)]
        public void Val_ReadsLeadingNumericPrefix(string input, double expected)
        {
            Assert.That(VBFunctions.Val(input), Is.EqualTo(expected));
        }

        [Test]
        public void Val_OfNullOrNonString_DoesNotThrow()
        {
            Assert.That(VBFunctions.Val(null), Is.EqualTo(0d));
            Assert.That(VBFunctions.Val(DBNull.Value), Is.EqualTo(0d));
            Assert.That(VBFunctions.Val(42), Is.EqualTo(42d));
        }

        [TestCase("123", true)]
        [TestCase("-12.5", true)]
        [TestCase("12a", false)]
        [TestCase("", false)]
        [TestCase("   ", false)]
        public void NumericText_TestsWhetherAStringHoldsANumber(string input, bool expected)
        {
            Assert.That(VBFunctions.NumericText(input), Is.EqualTo(expected));
        }

        [Test]
        public void NumericText_OfNull_IsFalse()
        {
            Assert.That(VBFunctions.NumericText(null), Is.False);
            Assert.That(VBFunctions.NumericText(DBNull.Value), Is.False);
        }

        // Fix truncates toward zero; Int rounds toward negative infinity. They differ
        // only for negative values, which is the whole reason both exist.
        [Test]
        public void FixAndInt_DifferOnNegativeValues()
        {
            Assert.That(VBFunctions.Fix(2.7), Is.EqualTo(2d));
            Assert.That(VBFunctions.Int(2.7), Is.EqualTo(2d));

            Assert.That(VBFunctions.Fix(-2.5), Is.EqualTo(-2d));
            Assert.That(VBFunctions.Int(-2.5), Is.EqualTo(-3d));
        }

        [Test]
        public void Fix_WithPlaces_TruncatesToThatManyDecimals()
        {
            Assert.That(VBFunctions.Fix(102.837, 2), Is.EqualTo(102.83).Within(1e-9));
            Assert.That(VBFunctions.Fix(102.839, 0), Is.EqualTo(102d));
        }

        // Crystal's second argument is a multiple to round to, not a decimal count.
        [Test]
        public void FloorAndCeiling_WithMultiple_RoundToThatMultiple()
        {
            Assert.That(VBFunctions.Floor(1250, 100), Is.EqualTo(1200d));
            Assert.That(VBFunctions.Ceiling(102.8, 100), Is.EqualTo(200d));

            Assert.That(VBFunctions.Floor(5.7), Is.EqualTo(5d));
            Assert.That(VBFunctions.Ceiling(5.2), Is.EqualTo(6d));
        }

        [Test]
        public void FloorAndCeiling_WithZeroMultiple_ReturnTheValueUnchanged()
        {
            Assert.That(VBFunctions.Floor(5.7, 0), Is.EqualTo(5.7));
            Assert.That(VBFunctions.Ceiling(5.7, 0), Is.EqualTo(5.7));
        }

        [Test]
        public void Remainder_IsTheModulus_AndDegradesOnAZeroDivisor()
        {
            Assert.That(VBFunctions.Remainder(7, 3), Is.EqualTo(1d));
            Assert.That(VBFunctions.Remainder(5, 0), Is.EqualTo(0d));
        }

        [Test]
        public void CStr_WithPlacesAndSeparators_FormatsTheNumber()
        {
            Assert.That(VBFunctions.CStr(1234.5678, 2, ","), Is.EqualTo("1,234.57"));
            Assert.That(VBFunctions.CStr(1234, 0, ""), Is.EqualTo("1234"));
            Assert.That(VBFunctions.CStr(1234.5678, 4, ",", "."), Is.EqualTo("1,234.5678"));
        }

        // The separators are swapped relative to the invariant ones the number is first
        // formatted with, so a naive replace would leave "1,234,57".
        [Test]
        public void CStr_WithSwappedSeparators_DoesNotCollide()
        {
            Assert.That(VBFunctions.CStr(1234.5678, 2, ".", ","), Is.EqualTo("1.234,57"));
        }

        [Test]
        public void CStr_WithAFormatStringSecondArgument_UsesIt()
        {
            Assert.That(VBFunctions.CStr(1234.5678, "0.00", 4), Is.EqualTo("1234.57"));
        }

        [Test]
        public void ChrWAndAscW_RoundTrip()
        {
            Assert.That(VBFunctions.ChrW(65), Is.EqualTo("A"));
            Assert.That(VBFunctions.AscW("A"), Is.EqualTo(65));
            Assert.That(VBFunctions.AscW(""), Is.EqualTo(0));
        }

        [Test]
        public void DateValue_KeepsOnlyTheDatePart()
        {
            var stamp = new DateTime(2020, 5, 17, 13, 45, 30);
            Assert.That(VBFunctions.DateValue(stamp), Is.EqualTo(new DateTime(2020, 5, 17)));
            Assert.That(VBFunctions.DateValue(2020, 5, 17), Is.EqualTo(new DateTime(2020, 5, 17)));
        }

        [Test]
        public void CDateTime_KeepsTheWholeValue()
        {
            var stamp = new DateTime(2020, 5, 17, 13, 45, 30);
            Assert.That(VBFunctions.CDateTime(stamp), Is.EqualTo(stamp));
        }

        [Test]
        public void IsDateTime_TestsWhetherAValueParsesAsADate()
        {
            Assert.That(VBFunctions.IsDateTime(new DateTime(2020, 1, 1)), Is.True);
            Assert.That(VBFunctions.IsDateTime("2020-01-01"), Is.True);
            Assert.That(VBFunctions.IsDateTime("not a date"), Is.False);
            Assert.That(VBFunctions.IsDateTime(null), Is.False);
        }

        // A field whose type was never inferred arrives as Object, so these Object-typed
        // overloads are what the expression parser can actually bind to.
        [Test]
        public void ObjectTypedStringOverloads_AcceptBoxedValuesAndNulls()
        {
            object padded = "  x  ";
            Assert.That(VBFunctions.Trim(padded), Is.EqualTo("x"));
            Assert.That(VBFunctions.LTrim(padded), Is.EqualTo("x  "));
            Assert.That(VBFunctions.RTrim(padded), Is.EqualTo("  x"));
            // Cast required: an untyped null binds to the more specific Trim(string).
            Assert.That(VBFunctions.Trim((object)null), Is.EqualTo(""));

            Assert.That(VBFunctions.Mid((object)"abcdef", 2), Is.EqualTo("bcdef"));
            Assert.That(VBFunctions.Mid((object)"abcdef", 2, 3), Is.EqualTo("bcd"));
            Assert.That(VBFunctions.InStr((object)"hello", (object)"ll"), Is.EqualTo(3));
            Assert.That(VBFunctions.Replace((object)"a-b", (object)"-", (object)"+"), Is.EqualTo("a+b"));
        }
    }
}
