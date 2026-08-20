#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// Issue #159 (follow-up to the wholesale-move case): a body textbox with
    /// CanGrow=true that grows TALLER than a whole page must be split across pages
    /// instead of clipping the overflow. The report uses a deliberately short (2in)
    /// page so the grown textbox spans several pages.
    /// </summary>
    [TestFixture]
    public class CanGrowSplitTest
    {
        private Uri _reportFolder;

        [SetUp]
        public void SetUp()
        {
            _reportFolder = GeneralUtils.ReportsFolder();
            RdlEngineConfig.RdlEngineConfigInit();
        }

        [Test]
        public async Task OvertallTextbox_IsSplitAcrossPages_NoContentLost()
        {
            Uri fileRdlUri = new Uri(_reportFolder, "CanGrowSplitTest.rdl");
            Directory.SetCurrentDirectory(_reportFolder.LocalPath);
            Report report = await RdlUtils.GetReport(fileRdlUri);
            Assert.That(report, Is.Not.Null);
            report.Folder = _reportFolder.LocalPath;
            await report.RunGetData();
            Pages pgs = await report.BuildPages();

            double bottomOfPage = pgs.BottomOfPage;

            // Chunks of the GrowBox textbox: every PageText except the TopMarker on page 1.
            // A chunk may legitimately land right on a word boundary (e.g. just "ENDMARKER"
            // alone on its own page), so chunks must not be filtered by their content.
            var chunks = new List<(int page, PageText pt)>();
            int pageCount = 0;
            foreach (Page p in pgs)
            {
                pageCount++;
                foreach (PageItem pi in p)
                {
                    if (pi is PageText pt && pt.Text != null && pt.Text != "TOPMARKER")
                        chunks.Add((pageCount, pt));
                }
            }

            // The grown text no longer fits one page, so it must span several.
            Assert.That(pageCount, Is.GreaterThanOrEqualTo(3),
                $"Expected the over-tall textbox to span multiple pages, got {pageCount}.");
            Assert.That(chunks.Count, Is.GreaterThanOrEqualTo(3),
                $"Expected multiple text chunks, got {chunks.Count}.");

            // Every chunk must respect the page bottom - nothing may clip (#159).
            foreach (var (page, pt) in chunks)
            {
                Assert.That(pt.Y + pt.H, Is.LessThanOrEqualTo(bottomOfPage + 0.5),
                    $"Chunk on page {page} overflows the page (bottom={pt.Y + pt.H:F1}, page bottom={bottomOfPage:F1}).");
                Assert.That(pt.Text.Trim(), Is.Not.Empty, $"Empty chunk emitted on page {page}.");
            }

            // Chunks must appear on consecutive pages in order.
            for (int i = 1; i < chunks.Count; i++)
            {
                Assert.That(chunks[i].page, Is.EqualTo(chunks[i - 1].page + 1),
                    "Text chunks are not on consecutive pages.");
            }

            // No content lost: every original token must appear exactly once overall.
            var sb = new StringBuilder();
            foreach (var (_, pt) in chunks)
                sb.Append(pt.Text).Append(' ');
            string all = sb.ToString();

            for (int i = 1; i <= 60; i++)
            {
                string token = $"LINE{i:00}";
                int first = all.IndexOf(token, StringComparison.Ordinal);
                Assert.That(first, Is.GreaterThanOrEqualTo(0), $"{token} was lost in the split.");
                Assert.That(all.IndexOf(token, first + 1, StringComparison.Ordinal), Is.EqualTo(-1),
                    $"{token} was duplicated by the split.");
            }
            Assert.That(all, Does.Contain("ENDMARKER"), "Trailing content was clipped (#159).");
        }
    }
}
#endif
