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
    /// Issue #159: a CanGrow textbox that grows past the bottom of the page should be
    /// carried onto the next page instead of overflowing/clipping. This verifies the
    /// case where the grown textbox fits on a fresh page: it is moved wholesale to the
    /// next page and its full content (including the trailing text) is preserved.
    ///
    /// Known remaining limitation (not asserted here): a single textbox grown TALLER
    /// than one whole page is still not split across pages - the overflow is clipped.
    /// Fixing that requires text-splitting in the core pagination path and is tracked
    /// as follow-up work on #159.
    /// </summary>
    [TestFixture]
    public class CanGrowPageTest
    {
        private Uri _reportFolder;

        [SetUp]
        public void SetUp()
        {
            _reportFolder = GeneralUtils.ReportsFolder();
            RdlEngineConfig.RdlEngineConfigInit();
        }

        [Test]
        public async Task GrownTextbox_OverflowingPage_MovesToNextPage()
        {
            Uri fileRdlUri = new Uri(_reportFolder, "CanGrowPageTest.rdl");
            Directory.SetCurrentDirectory(_reportFolder.LocalPath);
            Report report = await RdlUtils.GetReport(fileRdlUri);
            Assert.That(report, Is.Not.Null);
            report.Folder = _reportFolder.LocalPath;
            await report.RunGetData();
            Pages pgs = await report.BuildPages();

            double bottomOfPage = pgs.BottomOfPage;

            // Locate the grown textbox (its text starts with LINE01) and the top marker.
            PageText grow = null;
            int growPage = -1, topPage = -1, pageNo = 0;
            foreach (Page p in pgs)
            {
                pageNo++;
                foreach (PageItem pi in p)
                {
                    if (pi is PageText pt && pt.Text != null)
                    {
                        if (pt.Text.StartsWith("TOPMARKER")) topPage = pageNo;
                        if (pt.Text.StartsWith("LINE01")) { grow = pt; growPage = pageNo; }
                    }
                }
            }

            Assert.That(topPage, Is.EqualTo(1), "Top marker should be on page 1.");
            Assert.That(grow, Is.Not.Null, "Grown textbox not found in output.");

            // It grew beyond its defined 0.3in height (proves CanGrow expanded it).
            Assert.That(grow.H, Is.GreaterThan(100), $"Textbox did not grow (H={grow.H}).");

            // Because it would overflow page 1, it must be carried onto page 2...
            Assert.That(growPage, Is.EqualTo(2), "Overflowing CanGrow textbox was not moved to the next page (#159).");

            // ...where it now fits without clipping, and its full content is preserved.
            Assert.That(grow.Y + grow.H, Is.LessThanOrEqualTo(bottomOfPage + 0.5),
                $"Textbox still overflows the page (bottom={grow.Y + grow.H}, page={bottomOfPage}).");
            Assert.That(grow.Text, Does.Contain("ENDMARKER"),
                "Trailing content was lost when the textbox moved pages.");
        }
    }
}
#endif
