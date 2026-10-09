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
    /// A Rectangle grows with what it holds (#374). It was drawn at its designed height
    /// however far its contents printed, so a bordered Rectangle around a Subreport that
    /// grew drew its border as a strip at the top, with the subreport's content below it,
    /// and the next item under the Rectangle was placed from its designed bottom.
    /// </summary>
    [TestFixture]
    public class RectangleGrowsTests
    {
        private Uri _reportFolder;

        [SetUp]
        public void SetUp()
        {
            _reportFolder = GeneralUtils.ReportsFolder();
            RdlEngineConfig.RdlEngineConfigInit();
        }

        private async Task<List<PageItem>> FirstPage()
        {
            Uri fileRdlUri = new Uri(_reportFolder, "RectangleGrowsTest.rdl");
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
            return pages.Cast<Page>().First().Cast<PageItem>().ToList();
        }

        private static PageText Text(List<PageItem> items, string text) =>
            items.OfType<PageText>().Single(t => t.Text == text);

        [Test]
        public async Task ARectangle_GrowsToTheBottomOfASubreportThatGrowsInsideIt()
        {
            var items = await FirstPage();
            var subTop = Text(items, "SUB-TOP");
            var subBottom = Text(items, "SUB-BOTTOM");
            var frame = items.OfType<PageRectangle>().Single(r => Math.Abs(r.X - subTop.X) < 1f);

            Assert.Multiple(() =>
            {
                Assert.That(frame.Y, Is.EqualTo(subTop.Y).Within(0.5f), "the frame starts where the subreport does");
                Assert.That(frame.Y + frame.H, Is.EqualTo(subBottom.Y + subBottom.H).Within(0.5f),
                    "the frame ends at the subreport's last item, not 18pt down");
            });
        }

        // "Below" is placed 0.15in under the Rectangle's designed bottom, so it keeps that gap
        // under the grown one instead of printing over the subreport.
        [Test]
        public async Task AnItemBelowAGrownRectangle_PrintsBelowWhatItHolds()
        {
            var items = await FirstPage();
            var subBottom = Text(items, "SUB-BOTTOM");
            var below = Text(items, "BELOW-FRAME");

            Assert.That(below.Y - (subBottom.Y + subBottom.H), Is.EqualTo(0.15f * 72f).Within(0.5f));
        }

        [Test]
        public async Task ARectangleWhoseContentsFit_KeepsItsHeight()
        {
            var items = await FirstPage();
            var fixedText = Text(items, "FIXED-TEXT");
            var fixedFrame = items.OfType<PageRectangle>().Single(r => Math.Abs(r.X - fixedText.X) < 1f);

            Assert.That(fixedFrame.H, Is.EqualTo(0.5f * 72f).Within(0.01f));
        }
    }
}
#endif
