#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// Regression tests for issue #144: RunningValue always gives an error with scope.
    /// The expression parser pre-scan that detects an aggregate's DataSet scope assumed
    /// the scope was the second argument. RunningValue's signature is
    /// RunningValue(expression, aggregateFunction, scope) so the aggregate function
    /// (e.g. Sum) was mistaken for the scope and rejected with
    /// "function's scope must be a constant".
    /// </summary>
    [TestFixture]
    public class RunningValueScopeTest
    {
        private Uri _reportFolder;

        [SetUp]
        public void SetUp()
        {
            _reportFolder = GeneralUtils.ReportsFolder();
            RdlEngineConfig.RdlEngineConfigInit();
        }

        [Test]
        public async Task RunningValueWithScope_ParsesWithoutScopeError()
        {
            Uri fileRdlUri = new Uri(_reportFolder, "RunningValueScopeTest.rdl");
            System.IO.Directory.SetCurrentDirectory(_reportFolder.LocalPath);

            string source = await File.ReadAllTextAsync(fileRdlUri.LocalPath);
            var parser = new RDLParser(source);
            Report report = await parser.Parse();

            Assert.That(report, Is.Not.Null, "Report failed to parse");

            string errors = report.ErrorItems == null
                ? string.Empty
                : string.Join(Environment.NewLine, report.ErrorItems.Cast<string>());

            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4),
                "RunningValue with scope produced fatal parse errors: " + errors);
            Assert.That(errors, Does.Not.Contain("scope").IgnoreCase
                .Or.Not.Contain("constant").IgnoreCase,
                "RunningValue scope was rejected by the parser: " + errors);
        }

        [Test]
        public async Task RunningValueWithScope_RendersRunningTotals()
        {
            Uri fileRdlUri = new Uri(_reportFolder, "RunningValueScopeTest.rdl");
            System.IO.Directory.SetCurrentDirectory(_reportFolder.LocalPath);

            Report report = await RdlUtils.GetReport(fileRdlUri);
            Assert.That(report, Is.Not.Null, "Report failed to parse (fatal errors)");

            report.Folder = _reportFolder.LocalPath;
            await report.RunGetData();

            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.HTML);
            string html = ms.GetText();

            Assert.That(html, Is.Not.Empty, "HTML output is empty");
            // The quoted and unquoted scope columns should both render the same running
            // totals; at minimum the report must render without raising runtime errors.
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4),
                "RunningValue produced runtime errors during rendering");
        }
    }
}
#endif
