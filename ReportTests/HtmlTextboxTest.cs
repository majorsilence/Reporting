#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// Issue #149: "How to parse html on a textbox?" The engine already supports this:
    /// a textbox whose Style/Format evaluates to "html" has its value treated as HTML
    /// markup instead of being escaped as literal text. This test verifies the behaviour
    /// so the feature is documented and protected against regression.
    /// </summary>
    [TestFixture]
    public class HtmlTextboxTest
    {
        private Uri _reportFolder;

        [SetUp]
        public void SetUp()
        {
            _reportFolder = GeneralUtils.ReportsFolder();
            RdlEngineConfig.RdlEngineConfigInit();
        }

        private async Task<string> RenderHtml(string reportFile)
        {
            Uri fileRdlUri = new Uri(_reportFolder, reportFile);
            Directory.SetCurrentDirectory(_reportFolder.LocalPath);

            Report report = await RdlUtils.GetReport(fileRdlUri);
            Assert.That(report, Is.Not.Null, $"Report '{reportFile}' failed to parse");
            report.Folder = _reportFolder.LocalPath;
            await report.RunGetData();

            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.HTML);
            return ms.GetText();
        }

        [Test]
        public async Task HtmlFormatTextbox_PreservesMarkup_PlainTextboxEscapesIt()
        {
            string html = await RenderHtml("HtmlTextboxTest.rdl");

            // The html-format textbox: markup is passed through and interpreted as live HTML.
            Assert.That(html, Does.Contain("<b>HtmlBold</b>"),
                "Textbox with Format=html should preserve HTML markup as live tags.");
            Assert.That(html, Does.Not.Contain("&lt;b>HtmlBold"),
                "Textbox with Format=html should not escape its HTML markup.");

            // The plain textbox with identical content: the markup is escaped (the opening
            // angle bracket becomes &lt;) so it renders as literal text, not live tags.
            Assert.That(html, Does.Contain("&lt;b>PlainBold&lt;/b>"),
                "A textbox without Format=html should escape angle brackets as literal text.");
            Assert.That(html, Does.Not.Contain("<b>PlainBold</b>"),
                "A textbox without Format=html should not emit live HTML markup.");
        }
    }
}
#endif
