using System;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    [TestFixture]
    public class VBFunctionsTests
    {
        // ── Date / Time ──────────────────────────────────────────────────────────

        [TestCase(2024, 3, 15, ExpectedResult = 2024)]
        [TestCase(2000, 1, 1,  ExpectedResult = 2000)]
        public int Year_ReturnsCorrectYear(int y, int m, int d) =>
            VBFunctions.Year(new DateTime(y, m, d));

        [TestCase(2024, 3, 15, ExpectedResult = 3)]
        [TestCase(2000, 12, 31, ExpectedResult = 12)]
        public int Month_ReturnsCorrectMonth(int y, int m, int d) =>
            VBFunctions.Month(new DateTime(y, m, d));

        [TestCase(2024, 3, 15, ExpectedResult = 15)]
        [TestCase(2000, 12, 31, ExpectedResult = 31)]
        public int Day_ReturnsCorrectDay(int y, int m, int d) =>
            VBFunctions.Day(new DateTime(y, m, d));

        [Test]
        public void Hour_ReturnsCorrectHour()
        {
            Assert.That(VBFunctions.Hour(new DateTime(2024, 1, 1, 14, 30, 0)), Is.EqualTo(14));
        }

        [Test]
        public void Minute_ReturnsCorrectMinute()
        {
            Assert.That(VBFunctions.Minute(new DateTime(2024, 1, 1, 14, 30, 45)), Is.EqualTo(30));
        }

        [Test]
        public void Second_ReturnsCorrectSecond()
        {
            Assert.That(VBFunctions.Second(new DateTime(2024, 1, 1, 14, 30, 45)), Is.EqualTo(45));
        }

        // Weekday: 1=Sunday … 7=Saturday (VB convention)
        [Test]
        public void Weekday_Sunday_Returns1() =>
            Assert.That(VBFunctions.Weekday(new DateTime(2024, 3, 17)), Is.EqualTo(1)); // known Sunday

        [Test]
        public void Weekday_Monday_Returns2() =>
            Assert.That(VBFunctions.Weekday(new DateTime(2024, 3, 18)), Is.EqualTo(2));

        [Test]
        public void Weekday_Saturday_Returns7() =>
            Assert.That(VBFunctions.Weekday(new DateTime(2024, 3, 23)), Is.EqualTo(7));

        // WeekdayNonAmerican: 1=Monday … 7=Sunday (ISO convention)
        [Test]
        public void WeekdayNonAmerican_Monday_Returns1() =>
            Assert.That(VBFunctions.WeekdayNonAmerican(new DateTime(2024, 3, 18)), Is.EqualTo(1));

        [Test]
        public void WeekdayNonAmerican_Sunday_Returns7() =>
            Assert.That(VBFunctions.WeekdayNonAmerican(new DateTime(2024, 3, 17)), Is.EqualTo(7));

        [Test]
        public void WeekdayNonAmerican_StringOverload_ParsesCorrectly() =>
            Assert.That(VBFunctions.WeekdayNonAmerican("2024-03-18"), Is.EqualTo(1));

        [Test]
        public void WeekdayNonAmerican_InvalidString_ThrowsFormatException() =>
            Assert.Throws<FormatException>(() => VBFunctions.WeekdayNonAmerican("not-a-date"));

        [TestCase(1, false, "Sunday")]
        [TestCase(2, false, "Monday")]
        [TestCase(7, false, "Saturday")]
        public void WeekdayName_ReturnsCorrectName(int day, bool abbrev, string expected) =>
            Assert.That(VBFunctions.WeekdayName(day, abbrev), Is.EqualTo(expected));

        [Test]
        public void WeekdayName_Abbreviated_ReturnsShortenedName() =>
            Assert.That(VBFunctions.WeekdayName(2, true).Length, Is.LessThan(VBFunctions.WeekdayName(2, false).Length));

        // ── DateAdd ──────────────────────────────────────────────────────────────

        [Test]
        public void DateAdd_Year_AddsCorrectly()
        {
            var result = VBFunctions.DateAdd("yyyy", 2, new DateTime(2020, 6, 15));
            Assert.That(result.Year, Is.EqualTo(2022));
        }

        [Test]
        public void DateAdd_Month_AddsCorrectly()
        {
            var result = VBFunctions.DateAdd("m", 3, new DateTime(2020, 6, 15));
            Assert.That(result.Month, Is.EqualTo(9));
        }

        [Test]
        public void DateAdd_Day_AddsCorrectly()
        {
            var result = VBFunctions.DateAdd("d", 10, new DateTime(2020, 6, 15));
            Assert.That(result.Day, Is.EqualTo(25));
        }

        [Test]
        public void DateAdd_Quarter_Adds3Months()
        {
            var result = VBFunctions.DateAdd("q", 1, new DateTime(2020, 1, 1));
            Assert.That(result.Month, Is.EqualTo(4));
        }

        [Test]
        public void DateAdd_Week_Adds7Days()
        {
            var result = VBFunctions.DateAdd("ww", 2, new DateTime(2020, 6, 1));
            Assert.That(result, Is.EqualTo(new DateTime(2020, 6, 15)));
        }

        [Test]
        public void DateAdd_InvalidInterval_ThrowsArgumentException() =>
            Assert.Throws<ArgumentException>(() =>
                VBFunctions.DateAdd("xyz", 1, new DateTime(2020, 1, 1)));

        [Test]
        public void DateAdd_StringDateOverload_Works()
        {
            var result = VBFunctions.DateAdd("d", 5, "2020-01-01");
            Assert.That(result.Day, Is.EqualTo(6));
        }

        // ── Interval codes are case-insensitive ──────────────────────────────────
        //
        // VB's are, and reports are written both ways. Getting this wrong is not a wrong
        // answer but a dead report: the interval falls through to the default arm and
        // throws, so one "YYYY" somewhere in an expression takes the whole render with it.

        [TestCase("YYYY", 2022, 6, 15)]
        [TestCase("M", 2020, 8, 15)]
        [TestCase("D", 2020, 6, 17)]
        [TestCase("Q", 2020, 12, 15)]
        [TestCase("WW", 2020, 6, 29)]
        public void DateAdd_UppercaseInterval_MatchesTheLowercaseOne(
            string interval, int year, int month, int day)
        {
            var result = VBFunctions.DateAdd(interval, 2, new DateTime(2020, 6, 15));

            Assert.That(result, Is.EqualTo(new DateTime(year, month, day)));
        }

        [TestCase("YYYY", 2020)]
        [TestCase("Q", 2)]
        [TestCase("M", 6)]
        [TestCase("D", 15)]
        [TestCase("H", 13)]
        [TestCase("N", 45)]
        [TestCase("S", 30)]
        public void DatePart_UppercaseInterval_ReturnsTheSameComponent(string interval, int expected)
        {
            var when = new DateTime(2020, 6, 15, 13, 45, 30);

            Assert.That(VBFunctions.DatePart(interval, when), Is.EqualTo(expected));
            Assert.That(VBFunctions.DatePart(interval.ToLowerInvariant(), when), Is.EqualTo(expected));
        }

        /// <summary>
        /// "M" is the month and "N" the minute whichever way they are cased -- folding the
        /// case must not let one become the other.
        /// </summary>
        [Test]
        public void DatePart_MonthAndMinute_DoNotCollideWhenCaseIsFolded()
        {
            var when = new DateTime(2020, 6, 15, 13, 45, 30);

            Assert.Multiple(() => {
                Assert.That(VBFunctions.DatePart("M", when), Is.EqualTo(6), "M is the month");
                Assert.That(VBFunctions.DatePart("m", when), Is.EqualTo(6), "m is the month");
                Assert.That(VBFunctions.DatePart("N", when), Is.EqualTo(45), "N is the minute");
                Assert.That(VBFunctions.DatePart("n", when), Is.EqualTo(45), "n is the minute");
            });
        }

        [Test]
        public void DateDiff_UppercaseInterval_MatchesTheLowercaseOne()
        {
            var from = new DateTime(2020, 1, 1);
            var to = new DateTime(2021, 4, 15);

            Assert.That(VBFunctions.DateDiff("YYYY", from, to), Is.EqualTo(VBFunctions.DateDiff("yyyy", from, to)));
            Assert.That(VBFunctions.DateDiff("M", from, to), Is.EqualTo(VBFunctions.DateDiff("m", from, to)));
            Assert.That(VBFunctions.DateDiff("WW", from, to), Is.EqualTo(VBFunctions.DateDiff("ww", from, to)));
        }

        /// <summary>
        /// Folding the case must not quietly accept a code that is simply wrong: a mistyped
        /// interval is still the report author's mistake to see.
        /// </summary>
        [Test]
        public void AnInvalidInterval_StillThrowsWhateverItsCase() =>
            Assert.Throws<ArgumentException>(() =>
                VBFunctions.DatePart("XYZ", new DateTime(2020, 1, 1)));

        // ── String functions ─────────────────────────────────────────────────────

        [TestCase("Hello", 3, "Hel")]
        [TestCase("Hi", 10, "Hi")]     // count > length → return full string
        [TestCase(null, 3, null)]
        public void Left_ReturnsCorrectSubstring(string input, int count, string expected) =>
            Assert.That(VBFunctions.Left(input, count), Is.EqualTo(expected));

        [TestCase("Hello", 3, "llo")]
        [TestCase("Hi", 10, "Hi")]     // length > string → return full string
        [TestCase("Hello", 0, "")]
        [TestCase(null, 3, null)]
        public void Right_ReturnsCorrectSubstring(string input, int length, string expected) =>
            Assert.That(VBFunctions.Right(input, length), Is.EqualTo(expected));

        // Mid is 1-based
        [TestCase("Hello", 2, "ello")]
        [TestCase("Hello", 1, "Hello")]
        [TestCase("Hello", 6, "")]     // start past end
        [TestCase(null,    2, null)]
        public void Mid_NoLength_ReturnsCorrectSubstring(string input, int start, string expected) =>
            Assert.That(VBFunctions.Mid(input, start), Is.EqualTo(expected));

        [TestCase("Hello", 2, 3, "ell")]
        [TestCase("Hello", 1, 5, "Hello")]
        [TestCase("Hello", 3, 100, "llo")] // length too large → clip to end
        [TestCase("Hello", 6, 1, "")]      // start past end
        [TestCase(null,    2, 2, null)]
        public void Mid_WithLength_ReturnsCorrectSubstring(string input, int start, int length, string expected) =>
            Assert.That(VBFunctions.Mid(input, start, length), Is.EqualTo(expected));

        // InStr is 1-based
        [TestCase("Hello World", "World", 7)]
        [TestCase("Hello World", "xyz",   0)]
        [TestCase("Hello World", "",      1)]   // empty find → return start
        [TestCase(null, "x", 0)]
        public void InStr_Basic_ReturnsCorrect1BasedIndex(string str, string find, int expected) =>
            Assert.That(VBFunctions.InStr(str, find), Is.EqualTo(expected));

        [Test]
        public void InStr_CaseInsensitive_Finds()
        {
            Assert.That(VBFunctions.InStr("Hello", "HELLO", 1), Is.EqualTo(1));
            Assert.That(VBFunctions.InStr("Hello", "HELLO", 0), Is.EqualTo(0)); // case sensitive miss
        }

        [Test]
        public void InStr_WithStart_SearchesFromOffset()
        {
            // "aa" appears at 1 and 2; starting from 2 should return 2
            Assert.That(VBFunctions.InStr(2, "aaa", "aa"), Is.EqualTo(2));
        }

        [TestCase("Hello World", "World", 7)]
        [TestCase("Hello World", "xyz",   0)]
        [TestCase(null, "x", 0)]
        public void InStrRev_Basic_ReturnsCorrect1BasedIndex(string str, string find, int expected) =>
            Assert.That(VBFunctions.InStrRev(str, find), Is.EqualTo(expected));

        [Test]
        public void InStrRev_CaseInsensitive_Finds() =>
            Assert.That(VBFunctions.InStrRev("Hello", "HELLO", -1, 1), Is.EqualTo(1));

        // ── Replace ──────────────────────────────────────────────────────────────

        [Test]
        public void Replace_Basic_ReplacesAll() =>
            Assert.That(VBFunctions.Replace("aabbaa", "aa", "X"), Is.EqualTo("XbbX"));

        [Test]
        public void Replace_CaseInsensitive_ReplacesAll() =>
            Assert.That(VBFunctions.Replace("Hello hello", "hello", "Hi", 1, -1, 1),
                Is.EqualTo("Hi Hi"));

        [Test]
        public void Replace_LimitedCount_ReplacesOnlyFirst() =>
            Assert.That(VBFunctions.Replace("aaa", "a", "b", 1, 2),
                Is.EqualTo("bba"));

        [Test]
        public void Replace_NullFind_ReturnsOriginal() =>
            Assert.That(VBFunctions.Replace("hello", null, "x"), Is.EqualTo("hello"));

        // ── Len, LCase, UCase, Trim ───────────────────────────────────────────────

        [TestCase("Hello", 5)]
        [TestCase("",      0)]
        [TestCase(null,    0)]
        public void Len_ReturnsCorrectLength(string input, int expected) =>
            Assert.That(VBFunctions.Len(input), Is.EqualTo(expected));

        [Test]
        public void LCase_ReturnsLowercase() =>
            Assert.That(VBFunctions.LCase("Hello World"), Is.EqualTo("hello world"));

        [Test]
        public void UCase_ReturnsUppercase() =>
            Assert.That(VBFunctions.UCase("Hello World"), Is.EqualTo("HELLO WORLD"));

        [Test]
        public void LTrim_RemovesLeadingSpaces() =>
            Assert.That(VBFunctions.LTrim("  hello"), Is.EqualTo("hello"));

        [Test]
        public void RTrim_RemovesTrailingSpaces() =>
            Assert.That(VBFunctions.RTrim("hello  "), Is.EqualTo("hello"));

        [Test]
        public void Trim_RemovesBothEnds() =>
            Assert.That(VBFunctions.Trim("  hello  "), Is.EqualTo("hello"));

        // ── StrComp ──────────────────────────────────────────────────────────────

        [Test]
        public void StrComp_Equal_ReturnsZero() =>
            Assert.That(VBFunctions.StrComp("abc", "abc"), Is.EqualTo(0));

        [Test]
        public void StrComp_Less_ReturnsNegative() =>
            Assert.That(VBFunctions.StrComp("abc", "def"), Is.LessThan(0));

        [Test]
        public void StrComp_Greater_ReturnsPositive() =>
            Assert.That(VBFunctions.StrComp("def", "abc"), Is.GreaterThan(0));

        [Test]
        public void StrComp_CaseInsensitive_TreatsAsEqual() =>
            Assert.That(VBFunctions.StrComp("ABC", "abc", 1), Is.EqualTo(0));

        // ── StrReverse, Space, String ─────────────────────────────────────────────

        [TestCase("Hello", "olleH")]
        [TestCase("a",     "a")]
        [TestCase(null,    null)]
        public void StrReverse_ReturnsReversedString(string input, string expected) =>
            Assert.That(VBFunctions.StrReverse(input), Is.EqualTo(expected));

        [Test]
        public void Space_ReturnsCorrectNumberOfSpaces() =>
            Assert.That(VBFunctions.Space(5), Is.EqualTo("     "));

        [Test]
        public void String_CharRepeat_ReturnsCorrectString() =>
            Assert.That(VBFunctions.String(4, 'x'), Is.EqualTo("xxxx"));

        [Test]
        public void String_StringOverload_UsesFirstChar() =>
            Assert.That(VBFunctions.String(3, "ab"), Is.EqualTo("aaa"));

        // ── Round ────────────────────────────────────────────────────────────────

        [TestCase(2.5,  ExpectedResult = 2.0)]  // banker's rounding (round half to even)
        [TestCase(3.5,  ExpectedResult = 4.0)]
        [TestCase(2.75, ExpectedResult = 3.0)]
        public double Round_Double_NoDecimals(double n) => VBFunctions.Round(n);

        [Test]
        public void Round_Double_WithDecimals() =>
            Assert.That(VBFunctions.Round(3.1415, 2), Is.EqualTo(3.14));

        [Test]
        public void Round_Decimal_NoDecimals() =>
            Assert.That(VBFunctions.Round(2.5m), Is.EqualTo(2m)); // banker's rounding

        [Test]
        public void Round_Decimal_WithRoundingMode_AwayFromZero() =>
            Assert.That(VBFunctions.Round(2.5m, 0, (int)MidpointRounding.AwayFromZero), Is.EqualTo(3m));

        // ── IsNumeric ────────────────────────────────────────────────────────────

        [TestCase("42",    true)]
        [TestCase("3.14",  true)]
        [TestCase("abc",   false)]
        [TestCase(null,    false)]
        [TestCase(42,      true)]
        [TestCase(3.14,    true)]
        [TestCase(true,    true)]
        public void IsNumeric_VariousInputs(object input, bool expected) =>
            Assert.That(VBFunctions.IsNumeric(input), Is.EqualTo(expected));

        // ── Type conversions ──────────────────────────────────────────────────────

        [TestCase("True",  true)]
        [TestCase("False", false)]
        public void CBool_StringInput_ReturnsCorrectBool(string input, bool expected) =>
            Assert.That(VBFunctions.CBool(input), Is.EqualTo(expected));

        [Test]
        public void CInt_StringInput_ReturnsInt() =>
            Assert.That(VBFunctions.CInt("42"), Is.EqualTo(42));

        [Test]
        public void CDbl_StringInput_ReturnsDouble() =>
            Assert.That(VBFunctions.CDbl("3.14"), Is.EqualTo(3.14).Within(0.001));

        [Test]
        public void CStr_IntInput_ReturnsString() =>
            Assert.That(VBFunctions.CStr(42), Is.EqualTo("42"));

        [Test]
        public void CStr_Null_ReturnsEmptyString() =>
            Assert.That(VBFunctions.CStr(null), Is.EqualTo(""));

        [Test]
        public void Hex_ReturnsUppercaseHexString() =>
            Assert.That(VBFunctions.Hex(255), Is.EqualTo("FF"));

        [Test]
        public void Oct_ReturnsOctalString() =>
            Assert.That(VBFunctions.Oct(8), Is.EqualTo("10"));

        [Test]
        public void Chr_ReturnsCharacter() =>
            Assert.That(VBFunctions.Chr(65), Is.EqualTo('A'));

        [Test]
        public void Asc_ReturnsAsciiCode() =>
            Assert.That(VBFunctions.Asc("A"), Is.EqualTo(65));
    }
}
