using System.Collections.Specialized;
using System.Globalization;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// VB's integer division operator, lhs \ rhs. Each operand is rounded to a whole number
    /// the way CLng does (halves to even) before dividing, the remainder is dropped, and \
    /// ranks below * and / but above + and -. The expected values are VB.NET's own results.
    /// </summary>
    [TestFixture]
    public class IntegerDivideTests
    {
        private const string Rdl = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Language>en-US</Language>
  <PageHeight>11in</PageHeight><PageWidth>8.5in</PageWidth><Width>7.5in</Width>
  <ReportParameters>
    <ReportParameter Name=""X""><DataType>Float</DataType><Nullable>false</Nullable></ReportParameter>
    <ReportParameter Name=""Y""><DataType>Float</DataType><Nullable>false</Nullable></ReportParameter>
  </ReportParameters>
  <Body>
    <Height>1in</Height>
    <ReportItems>
      <Textbox Name=""Result""><Value>{0}</Value><Width>2in</Width><Height>0.25in</Height>{1}</Textbox>
    </ReportItems>
  </Body>
</Report>";

        [SetUp]
        public void SetUp()
        {
            RdlEngineConfig.RdlEngineConfigInit();
        }

        private static async Task<(Report Report, string Html)> Render(string value, string style, double x, double y)
        {
            string rdl = string.Format(Rdl, System.Security.SecurityElement.Escape(value), style);
            var report = await new RDLParser(rdl).Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4), "The expression failed to parse.");

            await report.RunGetData(new ListDictionary
            {
                { "X", x.ToString(CultureInfo.InvariantCulture) },
                { "Y", y.ToString(CultureInfo.InvariantCulture) },
            });
            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.HTML);
            return (report, ms.GetText());
        }

        [TestCase(7, 2, "3")]
        [TestCase(-7, 2, "-3")]
        [TestCase(7.5, 2, "4")]      // 8 \ 2
        [TestCase(-7.5, 2, "-4")]
        [TestCase(7.6, 1.9, "4")]    // 8 \ 2
        [TestCase(2.5, 1, "2")]      // halves go to the even number
        [TestCase(3.5, 1, "4")]
        [TestCase(7.5, 1, "8")]      // dividing by 1 still rounds
        public async Task RoundsEachOperandThenDropsTheRemainder(double x, double y, string expected)
        {
            var (_, html) = await Render(@"=""M["" & CStr(Parameters!X.Value \ Parameters!Y.Value) & ""]""", "", x, y);

            Assert.That(html, Does.Contain($"M[{expected}]"));
        }

        [TestCase(10, 0)]
        [TestCase(10, 0.5)]           // 0.5 rounds to 0
        [TestCase(0, 0.4)]            // 0 \ x still divides by x
        public async Task DivisorThatRoundsToZeroIsAnError(double x, double y)
        {
            var (report, html) = await Render(@"=""M["" & CStr(Parameters!X.Value \ Parameters!Y.Value) & ""]""", "", x, y);

            Assert.That(html, Does.Not.Contain("M["), "A division by zero has no value.");
            Assert.That(report.ErrorMaxSeverity, Is.GreaterThan(0), "The division by zero should be reported.");
        }

        [TestCase(@"7 \ 2 * 2", "1")]     // 7 \ 4
        [TestCase(@"2 * 7 \ 2", "7")]     // 14 \ 2
        [TestCase(@"7 \ 2 \ 2", "1")]     // left to right
        [TestCase(@"1 + 7 \ 2", "4")]
        [TestCase(@"7 \ 2 + 1", "4")]
        [TestCase(@"2 ^ 3 \ 3", "2")]
        public async Task RanksBelowMultiplyAndDivide(string expression, string expected)
        {
            var (_, html) = await Render($@"=""M["" & CStr({expression}) & ""]""", "", 0, 1);

            Assert.That(html, Does.Contain($"M[{expected}]"));
        }

        [Test]
        public async Task ResultTakesANumericFormat()
        {
            var (report, html) = await Render(@"=Parameters!X.Value \ Parameters!Y.Value",
                "<Style><Format>0.00</Format></Style>", 7, 2);

            Assert.That(html, Does.Contain("3.00"));
            Assert.That(report.ErrorMaxSeverity, Is.EqualTo(0), "Formatting the result should not log an error.");
        }
    }
}
