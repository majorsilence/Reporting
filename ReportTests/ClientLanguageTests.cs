using System.Globalization;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    [TestFixture]
    public class ClientLanguageTests
    {
        // Minimal RDL with a <Language> literal — no data source needed.
        private const string RdlWithLanguage = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition""
        xmlns:rd=""http://schemas.microsoft.com/SQLServer/reporting/reportdesigner"">
  <Language>en-US</Language>
  <PageHeight>11in</PageHeight><PageWidth>8.5in</PageWidth><Width>7.5in</Width>
  <Body>
    <Height>1in</Height>
    <ReportItems>
      <Textbox Name=""Label""><Value>Hello</Value><Width>2in</Width><Height>0.25in</Height></Textbox>
    </ReportItems>
  </Body>
</Report>";

        // Same report without any <Language> element.
        private const string RdlWithoutLanguage = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition""
        xmlns:rd=""http://schemas.microsoft.com/SQLServer/reporting/reportdesigner"">
  <PageHeight>11in</PageHeight><PageWidth>8.5in</PageWidth><Width>7.5in</Width>
  <Body>
    <Height>1in</Height>
    <ReportItems>
      <Textbox Name=""Label""><Value>Hello</Value><Width>2in</Width><Height>0.25in</Height></Textbox>
    </ReportItems>
  </Body>
</Report>";

        [SetUp]
        public void SetUp()
        {
            RdlEngineConfig.RdlEngineConfigInit();
        }

        [Test]
        public async Task ClientLanguage_NoRdlLanguage_NotSet_ReturnsCurrentCulture()
        {
            var rdlp = new RDLParser(RdlWithoutLanguage);
            using var report = await rdlp.Parse();

            Assert.That(report.ClientLanguage,
                Is.EqualTo(CultureInfo.CurrentCulture.ThreeLetterISOLanguageName));
        }

        [Test]
        public async Task ClientLanguage_NoRdlLanguage_WhenSet_ReturnsSetValue()
        {
            var rdlp = new RDLParser(RdlWithoutLanguage);
            using var report = await rdlp.Parse();

            report.ClientLanguage = "fr-FR";

            Assert.That(report.ClientLanguage, Is.EqualTo("fr-FR"));
        }

        [Test]
        public async Task ClientLanguage_WithRdlLanguage_NotSet_ReturnsRdlLanguage()
        {
            var rdlp = new RDLParser(RdlWithLanguage);
            using var report = await rdlp.Parse();

            Assert.That(report.ClientLanguage, Is.EqualTo("en-US"));
        }

        /// <summary>
        /// Regression test: ClientLanguage assignment was silently discarded when the RDL
        /// defined a &lt;Language&gt; element, because the getter evaluated the RDL expression
        /// before checking the backing field.
        /// </summary>
        [Test]
        public async Task ClientLanguage_WithRdlLanguage_WhenSet_OverridesRdlLanguage()
        {
            var rdlp = new RDLParser(RdlWithLanguage);
            using var report = await rdlp.Parse();

            report.ClientLanguage = "fr-FR";

            Assert.That(report.ClientLanguage, Is.EqualTo("fr-FR"),
                "Explicitly set ClientLanguage should take precedence over the RDL <Language> element");
        }
    }
}
