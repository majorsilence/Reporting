#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// A subreport whose datasets return no rows (#345). Paged output, which PDF and the
    /// other page renderers use, ran a subreport's body only when some dataset in it
    /// returned a row, and otherwise printed its NoRows message. That is right for a
    /// subreport built around a table, list, matrix or chart, which has nothing to show
    /// without rows. A subreport with no data region - a heading, text, a parameter value -
    /// has nothing to repeat, and printed nothing at all where it should have printed its
    /// content. Asserted on the laid-out pages, since HTML takes a path that always drew
    /// the subreport.
    /// </summary>
    [TestFixture]
    public class SubreportNoRowsTests
    {
        private Uri _reportFolder;

        [SetUp]
        public void SetUp()
        {
            _reportFolder = GeneralUtils.ReportsFolder();
            RdlEngineConfig.RdlEngineConfigInit();
        }

        private async Task<List<string>> PrintedText()
        {
            Uri fileRdlUri = new Uri(_reportFolder, "SubreportNoRowsTest.rdl");
            Directory.SetCurrentDirectory(_reportFolder.LocalPath);

            // Parsed with the folder set first, because a Subreport is resolved during the
            // parse's final pass.
            var parser = new RDLParser(File.ReadAllText(fileRdlUri.LocalPath))
            {
                Folder = _reportFolder.LocalPath
            };
            Report report = await parser.Parse();
            Assert.That(report, Is.Not.Null, "Report failed to parse");
            report.Folder = _reportFolder.LocalPath;
            await report.RunGetData();

            using Pages pages = await report.BuildPages();
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(8),
                "errors: " + (report.ErrorItems == null ? "(none)" : string.Join(" | ", report.ErrorItems.OfType<object>())));
            return pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>().Select(t => t.Text).ToList();
        }

        [Test]
        public async Task ASubreportWithNoDataRegion_IsDrawn_WhenItsDatasetHasNoRows()
        {
            var printed = await PrintedText();

            Assert.Multiple(() =>
            {
                Assert.That(printed, Does.Contain("MAIN-REPORT"));
                Assert.That(printed, Does.Contain("STATIC-SUB"), "its heading");
                Assert.That(printed, Does.Contain("PARAM-VALUE"), "the parameter the parent passed, inside a Rectangle");
                Assert.That(printed, Does.Not.Contain("STATIC-NO-ROWS"), "drawn, not replaced by its NoRows message");
            });
        }

        // The table sits inside a Rectangle, so the data region is found below the top level.
        [Test]
        public async Task ASubreportBuiltAroundATable_StillShowsItsNoRowsMessage()
        {
            var printed = await PrintedText();

            Assert.Multiple(() =>
            {
                Assert.That(printed, Does.Contain("TABLE-NO-ROWS"));
                Assert.That(printed, Does.Not.Contain("TABLE-HEADER"), "the table is not drawn over no rows");
            });
        }
    }
}
#endif
