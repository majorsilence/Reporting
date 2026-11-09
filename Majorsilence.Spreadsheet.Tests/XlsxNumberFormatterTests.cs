using System;
using System.IO;
using NUnit.Framework;

namespace Majorsilence.Spreadsheet.Tests
{
    [TestFixture]
    public class XlsxNumberFormatterTests
    {
        [TestCase(1234.5, "1234.5")]
        [TestCase(0.30000000000000004, "0.3")]
        [TestCase(123456789012.0, "1.23457E+11")]
        [TestCase(1.0 / 3, "0.333333333")]
        [TestCase(0.0, "0")]
        [TestCase(-5.0, "-5")]
        [TestCase(12345.6789012345, "12345.6789")]
        public void General(double value, string expected)
        {
            Assert.That(XlsxNumberFormatter.Format(value, null), Is.EqualTo(expected));
            Assert.That(XlsxNumberFormatter.Format(value, "General"), Is.EqualTo(expected));
        }

        [TestCase(2.675, "0.00", "2.68")]
        [TestCase(-1.5, "0.00", "-1.50")]
        [TestCase(1234567.8, "#,##0", "1,234,568")]
        [TestCase(0.0, "#,##0", "0")]
        [TestCase(-1234.5, "#,##0.00", "-1,234.50")]
        [TestCase(-1234.5, "#,##0.00;(#,##0.00)", "(1,234.50)")]
        [TestCase(1234.5, "#,##0.00;(#,##0.00)", "1,234.50")]
        [TestCase(0.256, "0%", "26%")]
        [TestCase(0.0525, "0.00%", "5.25%")]
        [TestCase(12345.0, "0.00E+00", "1.23E+04")]
        [TestCase(0.000123, "0.00E+00", "1.23E-04")]
        [TestCase(9.999, "0.00E+00", "1.00E+01")]
        [TestCase(12345.0, "##0.0E+0", "12.3E+3")]
        [TestCase(1234567.0, "#,##0,", "1,235")]
        [TestCase(1234567.0, "0.0,,", "1.2")]
        [TestCase(123456789.0, "000-00-0000", "123-45-6789")]
        [TestCase(1234.5, "#,##0.00 \"USD\"", "1,234.50 USD")]
        [TestCase(5.0, "\"$\"#,##0.00", "$5.00")]
        [TestCase(1234.5, "[$€-407] #,##0.00", "€ 1,234.50")]
        [TestCase(1.5, "[Red]0.00", "1.50")]
        [TestCase(0.0, "0;-0;\"zero\"", "zero")]
        [TestCase(-3.0, "0;-0;\"zero\"", "-3")]
        [TestCase(150.0, "[>=100]0\" big\";0\" small\"", "150 big")]
        [TestCase(5.0, "[>=100]0\" big\";0\" small\"", "5 small")]
        [TestCase(0.5, "#.##", ".5")]
        [TestCase(2.0, "0.##", "2.")]
        [TestCase(1.5, ".00", "1.50")]
        [TestCase(12.0, "00000", "00012")]
        [TestCase(1.0, "0_);(0)", "1 ")]
        [TestCase(-1.0, "0_);(0)", "(1)")]
        public void Numbers(double value, string code, string expected)
        {
            Assert.That(XlsxNumberFormatter.Format(value, code), Is.EqualTo(expected));
        }

        [TestCase(0.5, "?/?", "1/2")]
        [TestCase(1.25, "# ?/?", "1 1/4")]
        [TestCase(1.25, "# ?/4", "1 1/4")]
        [TestCase(3.14159, "# ??/??", "3 14/99")]
        [TestCase(2.0, "# ?/?", "2")]
        public void Fractions(double value, string code, string expected)
        {
            Assert.That(XlsxNumberFormatter.Format(value, code), Is.EqualTo(expected));
        }

        // 45366 is 2024-03-15 (a Friday); .5625 is 13:30.
        [TestCase(45366.0, "yyyy-mm-dd", "2024-03-15")]
        [TestCase(45366.0, "m/d/yyyy", "3/15/2024")]
        [TestCase(45366.0, "dd mmm yyyy", "15 Mar 2024")]
        [TestCase(45366.0, "dddd, mmmm d", "Friday, March 15")]
        [TestCase(45366.0, "mmm-yy", "Mar-24")]
        [TestCase(45366.0, "d-mmm", "15-Mar")]
        [TestCase(45366.0, "mmmmm", "M")]
        [TestCase(45366.5625, "h:mm AM/PM", "1:30 PM")]
        [TestCase(45366.5625, "h:mm a/p", "1:30 p")]
        [TestCase(45366.5625, "hh:mm:ss", "13:30:00")]
        [TestCase(45366.5625, "m/d/yyyy h:mm", "3/15/2024 13:30")]
        [TestCase(1.5, "[h]:mm", "36:00")]
        [TestCase(1.25 / 86400, "mm:ss.0", "00:01.3")]
        [TestCase(1.0, "m/d/yyyy", "1/1/1900")]
        public void Dates(double serial, string code, string expected)
        {
            Assert.That(XlsxNumberFormatter.Format(serial, code), Is.EqualTo(expected));
        }

        [Test]
        public void Dates_1904System()
        {
            Assert.That(XlsxNumberFormatter.Format(0, "m/d/yyyy", date1904: true), Is.EqualTo("1/1/1904"));
        }

        [TestCase("5", "@\" units\"", "5 units")]
        [TestCase("Bob", "0;-0;0;\"Name: \"@", "Name: Bob")]
        [TestCase("Bob", "0.00", "Bob")]
        [TestCase("Bob", null, "Bob")]
        public void Text(string text, string code, string expected)
        {
            Assert.That(XlsxNumberFormatter.FormatText(text, code), Is.EqualTo(expected));
        }

        [Test]
        public void Reader_DisplayText_AppliesTheCellsFormat()
        {
            var writer = new XlsxWriter("Sheet1");
            writer.SetCell(0, 0, 1234.5, new XlsxCellFormat { NumberFormatCode = XlsxNumberFormats.ThousandsTwoDecimal });
            writer.SetCell(0, 1, new DateTime(2024, 3, 15));
            writer.SetCell(0, 2, true);
            writer.SetCell(0, 3, "text");
            writer.SetCell(0, 4, 0.125, new XlsxCellFormat { NumberFormatCode = XlsxNumberFormats.Percentage });

            using var ms = new MemoryStream();
            writer.Write(ms);
            ms.Position = 0;
            using var reader = XlsxReader.Open(ms);
            var sheet = reader.ReadSheet(0);

            Assert.That(sheet.GetCell(0, 0)!.DisplayText, Is.EqualTo("1,234.50"));
            Assert.That(sheet.GetCell(0, 1)!.DisplayText, Is.EqualTo("3/15/2024"));
            Assert.That(sheet.GetCell(0, 2)!.DisplayText, Is.EqualTo("TRUE"));
            Assert.That(sheet.GetCell(0, 3)!.DisplayText, Is.EqualTo("text"));
            Assert.That(sheet.GetCell(0, 4)!.DisplayText, Is.EqualTo("12.50%"));
        }
    }
}
