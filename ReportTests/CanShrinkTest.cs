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
    /// Issue #10: CanShrink for table detail rows. A detail row whose textboxes
    /// have CanShrink=true should collapse to the height of its content instead of
    /// always occupying the defined row height. Measured against the engine's
    /// page model so the result is independent of any particular renderer.
    /// </summary>
    [TestFixture]
    public class CanShrinkTest
    {
        private Uri _reportFolder;

        [SetUp]
        public void SetUp()
        {
            _reportFolder = GeneralUtils.ReportsFolder();
            RdlEngineConfig.RdlEngineConfigInit();
        }

        // Vertical extent (max bottom edge) of the report content across all pages.
        private async Task<(double extent, int items)> ContentExtent(string reportFile)
        {
            Uri fileRdlUri = new Uri(_reportFolder, reportFile);
            System.IO.Directory.SetCurrentDirectory(_reportFolder.LocalPath);

            Report report = await RdlUtils.GetReport(fileRdlUri);
            Assert.That(report, Is.Not.Null, $"Report '{reportFile}' failed to parse");
            report.Folder = _reportFolder.LocalPath;
            await report.RunGetData();

            Pages pgs = await report.BuildPages();

            double extent = 0;
            int items = 0;
            foreach (Page p in pgs)
            {
                foreach (PageItem pi in p)
                {
                    items++;
                    extent = Math.Max(extent, pi.Y + pi.H);
                }
            }
            return (extent, items);
        }

        [Test]
        public async Task CanShrink_CollapsesRowsBelowDefinedHeight()
        {
            var shrink = await ContentExtent("CanShrinkTest.rdl");
            var fixedRows = await ContentExtent("CanShrinkTestOff.rdl");

            Assert.That(fixedRows.items, Is.GreaterThan(0), "control report produced no page items");
            Assert.That(shrink.items, Is.EqualTo(fixedRows.items),
                "shrink and control reports should produce the same number of items");

            // 6 detail rows at a defined height of 72pt -> ~432pt when fixed.
            Assert.That(fixedRows.extent, Is.GreaterThan(400),
                $"control rows did not occupy their defined height (extent={fixedRows.extent}pt)");
            Assert.That(shrink.extent, Is.LessThan(fixedRows.extent * 0.75),
                $"CanShrink did not collapse rows (shrink={shrink.extent}pt, fixed={fixedRows.extent}pt)");
        }
    }
}
#endif
