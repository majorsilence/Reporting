using System.Collections.Specialized;
using System.Globalization;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// Regression tests for the expression-level Format() function (e.g. =Format(value, "C2")).
    /// It used to call String.Format with no IFormatProvider, so it silently formatted against
    /// CultureInfo.CurrentCulture (the ambient/thread culture) instead of the report's
    /// &lt;Language&gt; element -- unlike the Style-level &lt;Format&gt; property, which already
    /// resolved the report's language correctly via Style.EvalLanguage/FormatValue. Under the
    /// invariant culture (the .NET default in many server/container deployments that never set
    /// an explicit culture), this rendered the generic "&#164;" currency sign instead of "$",
    /// even when the report's Language was explicitly "en-US".
    /// </summary>
    [TestFixture]
    public class FormatExpressionCultureTests
    {
        // Minimal RDL: an explicit <Language>en-US</Language> and a Textbox whose Value formats
        // a report parameter (not a compile-time constant, like Fields!X.Value in real report
        // templates -- see below) via the expression-level Format(...) function.
        private const string RdlWithFormatExpression = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition""
        xmlns:rd=""http://schemas.microsoft.com/SQLServer/reporting/reportdesigner"">
  <Language>en-US</Language>
  <PageHeight>11in</PageHeight><PageWidth>8.5in</PageWidth><Width>7.5in</Width>
  <ReportParameters>
    <ReportParameter Name=""Amount"">
      <DataType>Float</DataType>
      <Nullable>false</Nullable>
    </ReportParameter>
  </ReportParameters>
  <Body>
    <Height>1in</Height>
    <ReportItems>
      <Textbox Name=""Amount""><Value>=""Total: "" &amp; Format(Parameters!Amount.Value, ""C2"")</Value><Width>2in</Width><Height>0.25in</Height></Textbox>
    </ReportItems>
  </Body>
</Report>";

        private CultureInfo _savedCulture;
        private CultureInfo _savedUiCulture;

        [SetUp]
        public void SetUp()
        {
            RdlEngineConfig.RdlEngineConfigInit();

            // Simulate a server/container process that never sets an explicit culture: in that
            // environment CultureInfo.CurrentCulture defaults to CultureInfo.InvariantCulture,
            // whose NumberFormatInfo.CurrencySymbol is the generic "&#164;" placeholder.
            _savedCulture = CultureInfo.CurrentCulture;
            _savedUiCulture = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        }

        [TearDown]
        public void TearDown()
        {
            CultureInfo.CurrentCulture = _savedCulture;
            CultureInfo.CurrentUICulture = _savedUiCulture;
        }

        [Test]
        public async Task ExpressionFormat_UnderInvariantAmbientCulture_UsesReportLanguageCurrencySymbol()
        {
            var rdlp = new RDLParser(RdlWithFormatExpression);
            using var report = await rdlp.Parse();

            var parameters = new ListDictionary { { "Amount", "1234.5" } };
            await report.RunGetData(parameters);

            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.HTML);

            string html = ms.GetText();

            Assert.That(html, Does.Contain("Total: $1,234.50"),
                "Format(value, \"C2\") should honour the report's <Language> (en-US) and render " +
                "'$', not fall back to the ambient/thread culture's currency symbol.");
            Assert.That(html, Does.Not.Contain("¤"),
                "Format() must not fall back to the invariant culture's generic currency sign.");
        }
    }
}
