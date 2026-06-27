using System;
using System.Globalization;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    [TestFixture]
    public class StyleFormatTests
    {
        // ── C0 (currency, 0 decimals) ─────────────────────────────────────────────

        [Test]
        public void FormatValue_C0_EnUs_ProducesDollarWithNoDecimals()
        {
            string result = Style.FormatValue(1234.56m, TypeCode.Decimal, "C0", "en-US");
            Assert.That(result, Does.Contain("$"));
            Assert.That(result, Does.Contain("1,235"));
        }

        [Test]
        public void FormatValue_C0_EnCa_ProducesDollarSymbol()
        {
            string result = Style.FormatValue(1234.56m, TypeCode.Decimal, "C0", "en-CA");
            Assert.That(result, Is.Not.Null.And.Not.Empty);
            Assert.That(result, Does.Contain("$"));
        }

        [Test]
        public void FormatValue_C0_FrFr_ProducesEuroSymbol()
        {
            string result = Style.FormatValue(1234.56m, TypeCode.Decimal, "C0", "fr-FR");
            Assert.That(result, Does.Contain("€"));
        }

        [Test]
        public void FormatValue_C0_DeDe_ProducesEuroSymbol()
        {
            string result = Style.FormatValue(1234.56m, TypeCode.Decimal, "C0", "de-DE");
            Assert.That(result, Does.Contain("€"));
        }

        [Test]
        public void FormatValue_C0_EnGb_ProducesPoundSymbol()
        {
            string result = Style.FormatValue(1234.56m, TypeCode.Decimal, "C0", "en-GB");
            Assert.That(result, Does.Contain("£"));
        }

        // Different locales must produce different C0 strings
        [Test]
        public void FormatValue_C0_EnUsVsFrFr_ProduceDifferentOutput()
        {
            string enUs = Style.FormatValue(1234.56m, TypeCode.Decimal, "C0", "en-US");
            string frFr = Style.FormatValue(1234.56m, TypeCode.Decimal, "C0", "fr-FR");
            Assert.That(enUs, Is.Not.EqualTo(frFr));
        }

        // ── C2 (currency, 2 decimals) ─────────────────────────────────────────────

        [Test]
        public void FormatValue_C2_EnUs_ProducesTwoDecimalPlaces()
        {
            string result = Style.FormatValue(1234.5m, TypeCode.Decimal, "C2", "en-US");
            Assert.That(result, Is.EqualTo("$1,234.50"));
        }

        // ── N (number) ────────────────────────────────────────────────────────────

        [Test]
        public void FormatValue_N0_Int32_EnUs_UsesThousandsSeparator()
        {
            string result = Style.FormatValue(1234567, TypeCode.Int32, "N0", "en-US");
            Assert.That(result, Is.EqualTo("1,234,567"));
        }

        [Test]
        public void FormatValue_N2_Decimal_DeDe_UsesCommaAsDecimalSeparator()
        {
            string result = Style.FormatValue(1234.56m, TypeCode.Decimal, "N2", "de-DE");
            // German uses comma as decimal separator and period as thousands separator
            Assert.That(result, Does.Contain(",56"));
        }

        [Test]
        public void FormatValue_N2_Decimal_FrFr_UsesCommaAsDecimalSeparator()
        {
            string result = Style.FormatValue(1234.56m, TypeCode.Decimal, "N2", "fr-FR");
            Assert.That(result, Does.Contain(",56"));
        }

        // ── P (percent) ───────────────────────────────────────────────────────────

        [Test]
        public void FormatValue_P0_Decimal_EnUs_FormatsAsPercent()
        {
            string result = Style.FormatValue(0.75m, TypeCode.Decimal, "P0", "en-US");
            Assert.That(result, Does.Contain("75"));
            Assert.That(result, Does.Contain("%"));
        }

        [Test]
        public void FormatValue_P2_Double_EnUs_FormatsAsPercentWithDecimals()
        {
            string result = Style.FormatValue(0.1234, TypeCode.Double, "P2", "en-US");
            Assert.That(result, Does.Contain("12.34"));
            Assert.That(result, Does.Contain("%"));
        }

        // ── Integer types ────────────────────────────────────────────────────────

        [Test]
        public void FormatValue_C0_Int64_EnUs_ProducesCurrencyFormat()
        {
            string result = Style.FormatValue(9876543L, TypeCode.Int64, "C0", "en-US");
            Assert.That(result, Does.Contain("$"));
            Assert.That(result, Does.Contain("9,876,543"));
        }

        [Test]
        public void FormatValue_N0_Int16_EnUs_FormatsCorrectly()
        {
            string result = Style.FormatValue((short)12345, TypeCode.Int16, "N0", "en-US");
            Assert.That(result, Is.EqualTo("12,345"));
        }

        // ── Date formatting ───────────────────────────────────────────────────────

        [Test]
        public void FormatValue_ShortDate_EnUs_UsesSlashSeparator()
        {
            var date = new DateTime(2024, 6, 15);
            string result = Style.FormatValue(date, TypeCode.DateTime, "yyyy-MM-dd", "en-US");
            Assert.That(result, Is.EqualTo("2024-06-15"));
        }

        [Test]
        public void FormatValue_LongMonthName_FrFr_UsesFrenchMonthName()
        {
            var date = new DateTime(2024, 1, 15);
            string result = Style.FormatValue(date, TypeCode.DateTime, "MMMM", "fr-FR");
            Assert.That(result.ToLowerInvariant(), Does.Contain("janvier"));
        }

        [Test]
        public void FormatValue_LongMonthName_DeDe_UsesGermanMonthName()
        {
            var date = new DateTime(2024, 1, 15);
            string result = Style.FormatValue(date, TypeCode.DateTime, "MMMM", "de-DE");
            Assert.That(result.ToLowerInvariant(), Does.Contain("januar"));
        }

        // ── Edge cases ────────────────────────────────────────────────────────────

        [Test]
        public void FormatValue_NullLanguage_DoesNotThrow()
        {
            string result = Style.FormatValue(1234m, TypeCode.Decimal, "C0", null);
            Assert.That(result, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void FormatValue_EmptyLanguage_DoesNotThrow()
        {
            string result = Style.FormatValue(1234m, TypeCode.Decimal, "C0", "");
            Assert.That(result, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void FormatValue_UnknownLanguageTag_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => Style.FormatValue(1234m, TypeCode.Decimal, "C0", "xx-INVALID"));
        }

        [Test]
        public void FormatValue_StringType_IgnoresFormat()
        {
            string result = Style.FormatValue("hello", TypeCode.String, "C0", "en-US");
            Assert.That(result, Is.EqualTo("hello"));
        }

        [Test]
        public void FormatValue_EmptyFormat_ReturnsToString()
        {
            string result = Style.FormatValue(1234.56m, TypeCode.Decimal, "", "en-US");
            Assert.That(result, Is.EqualTo("1234.56"));
        }

        // ── Locale correctness: same number, different locale, different output ───

        [TestCase("en-US", "$")]
        [TestCase("en-GB", "£")]
        [TestCase("fr-FR", "€")]
        [TestCase("de-DE", "€")]
        public void FormatValue_C0_VariousLocales_ContainExpectedCurrencySymbol(string locale, string expectedSymbol)
        {
            string result = Style.FormatValue(1000m, TypeCode.Decimal, "C0", locale);
            Assert.That(result, Does.Contain(expectedSymbol),
                $"C0 format with locale {locale} should contain '{expectedSymbol}' but was '{result}'");
        }

        [TestCase("en-US", ".")]
        [TestCase("de-DE", ",")]
        [TestCase("fr-FR", ",")]
        public void FormatValue_N2_VariousLocales_UseExpectedDecimalSeparator(string locale, string decimalSep)
        {
            string result = Style.FormatValue(1.5m, TypeCode.Decimal, "N2", locale);
            Assert.That(result, Does.Contain($"1{decimalSep}50"),
                $"N2 format with locale {locale} should use '{decimalSep}' as decimal separator but was '{result}'");
        }
    }
}
