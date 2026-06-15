#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// Verifies that output formats beyond PDF and Excel produce non-empty, well-formed output.
    /// Uses reports with SQLite data sources so no external database is required.
    /// </summary>
    [TestFixture]
    public class MultiFormatRenderTests
    {
        private Uri _reportFolder;
        private Uri _outputFolder;

        [SetUp]
        public void SetUp()
        {
            _reportFolder = GeneralUtils.ReportsFolder();
            _outputFolder = GeneralUtils.OutputTestsFolder();
            Directory.CreateDirectory(_outputFolder.LocalPath);
            RdlEngineConfig.RdlEngineConfigInit();
        }

        private static readonly object[] NorthwindReports =
        {
            new object[] { "ListReport.rdl" },
            new object[] { "MatrixExample.rdl" },
        };

        private static readonly object[] XmlDataReports =
        {
            new object[] { "WorldFacts.rdl" },
        };

        private static readonly object[] ChartReports =
        {
            new object[] { "ChartTypes.rdl" },
        };

        [Test, TestCaseSource(nameof(NorthwindReports))]
        public async Task CsvRender_NorthwindReport_ProducesNonEmptyOutput(string reportFile)
        {
            await RenderAndAssertNonEmpty(reportFile, OutputPresentationType.CSV, ".csv", assertText: text =>
                Assert.That(text, Is.Not.Empty, "CSV output is empty"));
        }

        [Test, TestCaseSource(nameof(NorthwindReports))]
        public async Task XmlRender_NorthwindReport_ProducesValidXml(string reportFile)
        {
            await RenderAndAssertNonEmpty(reportFile, OutputPresentationType.XML, ".xml", assertText: text =>
            {
                Assert.That(text, Is.Not.Empty, "XML output is empty");
                Assert.That(text, Does.StartWith("<?xml").Or.StartWith("<"), "Output is not XML");
            });
        }

        [Test, TestCaseSource(nameof(NorthwindReports))]
        public async Task HtmlRender_NorthwindReport_ContainsHtmlStructure(string reportFile)
        {
            await RenderAndAssertNonEmpty(reportFile, OutputPresentationType.HTML, ".html", assertText: text =>
            {
                Assert.That(text, Is.Not.Empty, "HTML output is empty");
                Assert.That(text, Does.Contain("<html").Or.Contain("<table").Or.Contain("<div"),
                    "Output does not appear to be HTML");
            });
        }

        [Test, TestCaseSource(nameof(NorthwindReports))]
        public async Task MhtmlRender_NorthwindReport_ProducesNonEmptyOutput(string reportFile)
        {
            // MHTML renderer writes via GetStream() (binary path), not GetTextWriter().
            // Verify via stream length rather than text content.
            Uri fileRdlUri = new Uri(_reportFolder, reportFile);
            System.IO.Directory.SetCurrentDirectory(_reportFolder.LocalPath);

            Report rap = await RdlUtils.GetReport(fileRdlUri);
            Assert.That(rap, Is.Not.Null, $"Report '{reportFile}' failed to parse");

            rap.Folder = _reportFolder.LocalPath;
            await rap.RunGetData();

            using var ms = new MemoryStreamGen();
            await rap.RunRender(ms, OutputPresentationType.MHTML);

            var stream = ms.GetStream();
            Assert.That(stream, Is.Not.Null, "MHTML stream is null");
            Assert.That(stream.Length, Is.GreaterThan(0), "MHTML output is empty");
        }

        [Test, TestCaseSource(nameof(NorthwindReports))]
        public async Task RtfRender_NorthwindReport_ProducesNonEmptyOutput(string reportFile)
        {
            await RenderAndAssertNonEmpty(reportFile, OutputPresentationType.RTF, ".rtf", assertText: text =>
            {
                Assert.That(text, Is.Not.Empty, "RTF output is empty");
                Assert.That(text, Does.StartWith("{\\rtf"), "Output does not appear to be RTF");
            });
        }

        [Test, TestCaseSource(nameof(XmlDataReports))]
        public async Task CsvRender_XmlDataReport_ProducesNonEmptyOutput(string reportFile)
        {
            await RenderAndAssertNonEmpty(reportFile, OutputPresentationType.CSV, ".csv", assertText: text =>
                Assert.That(text, Is.Not.Empty, "CSV output is empty"));
        }

        [Test, TestCaseSource(nameof(XmlDataReports))]
        public async Task HtmlRender_XmlDataReport_ContainsHtmlStructure(string reportFile)
        {
            await RenderAndAssertNonEmpty(reportFile, OutputPresentationType.HTML, ".html", assertText: text =>
            {
                Assert.That(text, Is.Not.Empty, "HTML output is empty");
                Assert.That(text, Does.Contain("<html").Or.Contain("<table").Or.Contain("<div"));
            });
        }

        [Test, TestCaseSource(nameof(ChartReports))]
        public async Task HtmlRender_ChartReport_ContainsHtmlStructure(string reportFile)
        {
            await RenderAndAssertNonEmpty(reportFile, OutputPresentationType.HTML, ".html", assertText: text =>
            {
                Assert.That(text, Is.Not.Empty, "HTML output is empty");
                Assert.That(text, Does.Contain("<html").Or.Contain("<table").Or.Contain("<div"),
                    "Output does not appear to be HTML");
            });
        }

        [Test, TestCaseSource(nameof(ChartReports))]
        public async Task CsvRender_ChartReport_ProducesNonEmptyOutput(string reportFile)
        {
            await RenderAndAssertNonEmpty(reportFile, OutputPresentationType.CSV, ".csv", assertText: text =>
                Assert.That(text, Is.Not.Empty, "CSV output is empty"));
        }

        private async Task RenderAndAssertNonEmpty(
            string reportFile,
            OutputPresentationType outputType,
            string extension,
            Action<string> assertText)
        {
            Uri fileRdlUri = new Uri(_reportFolder, reportFile);
            System.IO.Directory.SetCurrentDirectory(_reportFolder.LocalPath);

            Report rap = await RdlUtils.GetReport(fileRdlUri);
            Assert.That(rap, Is.Not.Null, $"Report '{reportFile}' failed to parse");
            Assert.That(rap.ErrorMaxSeverity, Is.LessThanOrEqualTo(4),
                $"Report '{reportFile}' has fatal parse errors");

            rap.Folder = _reportFolder.LocalPath;
            await rap.RunGetData();

            using var ms = new MemoryStreamGen();
            await rap.RunRender(ms, outputType);

            string text = ms.GetText();
            assertText(text);

            // Also write to disk for manual inspection during development
            string outputFile = Path.Combine(
                _outputFolder.LocalPath,
                $"{Path.GetFileNameWithoutExtension(reportFile)}_{outputType}{extension}");
            await File.WriteAllTextAsync(outputFile, text);
        }
    }
}
#endif