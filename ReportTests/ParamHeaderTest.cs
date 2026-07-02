#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// Issue #108: a report parameter displayed in the page header should reflect the
    /// value supplied at run time (e.g. after the user changes it and re-runs), not
    /// always the default. Body and header both bind to =Parameters!P.Value.
    /// </summary>
    [TestFixture]
    public class ParamHeaderTest
    {
        private Uri _reportFolder;

        [SetUp]
        public void SetUp()
        {
            _reportFolder = GeneralUtils.ReportsFolder();
            RdlEngineConfig.RdlEngineConfigInit();
        }

        private async Task<string> RenderHtml(IDictionary parms)
        {
            Uri fileRdlUri = new Uri(_reportFolder, "ParamHeaderTest.rdl");
            Directory.SetCurrentDirectory(_reportFolder.LocalPath);

            Report report = await RdlUtils.GetReport(fileRdlUri);
            Assert.That(report, Is.Not.Null, "Report failed to parse");
            report.Folder = _reportFolder.LocalPath;
            await report.RunGetData(parms);

            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.HTML);
            return ms.GetText();
        }

        [Test]
        public async Task Header_ReflectsRuntimeParameterValue()
        {
            var parms = new Hashtable { { "P", "CHANGEDVAL" } };
            string html = await RenderHtml(parms);

            // Body binds to the same parameter and is the control: it should show the
            // runtime value.
            Assert.That(html, Does.Contain("CHANGEDVAL"), "Runtime parameter value did not appear at all.");

            // The header must show the runtime value too, not the default.
            int changed = CountOccurrences(html, "CHANGEDVAL");
            Assert.That(changed, Is.GreaterThanOrEqualTo(2),
                $"Header did not reflect the runtime parameter value (found 'CHANGEDVAL' {changed} time(s), expected header + body). Full: header may still show default 'DEFAULTVAL'.");
            Assert.That(html, Does.Not.Contain("DEFAULTVAL"),
                "The default parameter value leaked into the output instead of the runtime value.");
        }

        [Test]
        public async Task Header_RefreshesBetweenRuns_DefaultThenChanged()
        {
            // Mirrors the viewer's Run-Report flow (a fresh parse per run): render once
            // with the default, then again with a changed value. The header must track
            // each run's value rather than sticking on the default.
            string first = await RenderHtml(new Hashtable());                       // default
            Assert.That(first, Does.Contain("DEFAULTVAL"), "First run should show the default.");

            string second = await RenderHtml(new Hashtable { { "P", "SECONDVAL" } }); // changed
            Assert.That(second, Does.Contain("SECONDVAL"), "Second run should show the changed value.");
            Assert.That(CountOccurrences(second, "SECONDVAL"), Is.GreaterThanOrEqualTo(2),
                "Header did not refresh to the changed value on the second run.");
            Assert.That(second, Does.Not.Contain("DEFAULTVAL"),
                "Header stuck on the default value after the parameter changed (#108).");
        }

        private static int CountOccurrences(string haystack, string needle)
        {
            int count = 0, i = 0;
            while ((i = haystack.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { count++; i += needle.Length; }
            return count;
        }
    }
}
#endif
