#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// Issue #121: a List of long text rows rendered as HTML showed "Page 1 of 1" and the
    /// PDF lost content. Every row must reach every output and the page count must be right.
    /// </summary>
    [TestFixture]
    public class ListPagingTests
    {
        private Uri _reportFolder;

        [SetUp]
        public void SetUp()
        {
            _reportFolder = GeneralUtils.ReportsFolder();
            RdlEngineConfig.RdlEngineConfigInit();
        }

        private async Task<Report> LoadReport()
        {
            Directory.SetCurrentDirectory(_reportFolder.LocalPath);
            Report report = await RdlUtils.GetReport(new Uri(_reportFolder, "ListPagingTest.rdl"));
            Assert.That(report, Is.Not.Null);
            report.Folder = _reportFolder.LocalPath;
            await report.RunGetData();
            return report;
        }

        [Test]
        public async Task Pages_SpanSeveralPages_AndKeepEveryRow()
        {
            Report report = await LoadReport();
            Pages pgs = await report.BuildPages();

            Assert.That(pgs.Count, Is.GreaterThan(5), "Every tall row needs more than one page.");
            string all = AllText(pgs);
            for (int i = 1; i <= 5; i++)
            {
                Assert.That(all, Does.Contain($"ROW{i} "), $"Row {i} start is missing.");
                Assert.That(all, Does.Contain($"ROWEND{i}"), $"Row {i} end was clipped.");
                Assert.That(all, Does.Contain($"HEND{i}"), $"Html row {i} end was clipped (#121).");
            }
        }

        [Test]
        public async Task Pages_HtmlParagraphs_AreEachShownExactlyOnce_AndStayOnThePage()
        {
            Report report = await LoadReport();
            Pages pgs = await report.BuildPages();

            string all = AllText(pgs);
            for (int i = 1; i <= 30; i++)
                Assert.That(CountOf(all, $"HPARA{i:00}"), Is.EqualTo(5), $"HPARA{i:00} must appear once per row.");

            foreach (Page page in pgs)
                foreach (PageItem item in page)
                    if (item is PageTextHtml html)
                    {
                        Assert.That(item.Y + item.H, Is.LessThanOrEqualTo(pgs.BottomOfPage + 1),
                            "A piece of html text runs off the bottom of its page.");
                        Assert.That(CountOf(html.Text, "<p>"), Is.EqualTo(CountOf(html.Text, "</p>")),
                            "A split must not leave a paragraph open.");
                        Assert.That(CountOf(html.Text, "<b>"), Is.EqualTo(CountOf(html.Text, "</b>")),
                            "A split must not leave bold open.");
                    }
        }

        [Test]
        public async Task Pages_RowsDoNotOverlap()
        {
            Report report = await LoadReport();
            Pages pgs = await report.BuildPages();

            foreach (Page page in pgs)
            {
                var boxes = page.Cast<PageItem>().OfType<PageText>().Where(t => t.HtmlParent == null && !string.IsNullOrEmpty(t.Text) && !t.Text.StartsWith("Page ")).ToList();
                for (int a = 0; a < boxes.Count; a++)
                    for (int b = a + 1; b < boxes.Count; b++)
                        Assert.That(boxes[a].Y + boxes[a].H <= boxes[b].Y + 1 || boxes[b].Y + boxes[b].H <= boxes[a].Y + 1,
                            Is.True, $"'{Head(boxes[a])}' overlaps '{Head(boxes[b])}'.");
            }
        }

        [Test]
        public async Task Html_ContainsEveryRow()
        {
            Report report = await LoadReport();
            var sg = new MemoryStreamGen("ShowFile?type=", null, "html");
            await report.RunRender(sg, OutputPresentationType.HTML, Guid.NewGuid().ToString());
            string html = sg.GetText();

            for (int i = 1; i <= 5; i++)
            {
                Assert.That(html, Does.Contain($"ROWEND{i}"), $"Row {i} missing from HTML.");
                Assert.That(html, Does.Contain($"HEND{i}"), $"Html row {i} missing from HTML.");
            }
        }

        [Test]
        public async Task Pdf_HasEveryPage()
        {
            Report report = await LoadReport();
            int expected = (await report.BuildPages()).Count;

            var sg = new MemoryStreamGen("ShowFile?type=", null, "pdf");
            Report report2 = await LoadReport();
            await report2.RunRender(sg, OutputPresentationType.PDF, Guid.NewGuid().ToString());
            byte[] pdf = ((MemoryStream)sg.MemoryList[0]).ToArray();
            string text = System.Text.Encoding.Latin1.GetString(pdf);
            int pageObjects = Regex.Matches(text, @"/Type\s*/Page[^s]").Count;
            Assert.That(pageObjects, Is.EqualTo(expected), "PDF page count differs from the laid-out page count.");
        }

        private static string AllText(Pages pgs) =>
            string.Join(" ", pgs.Cast<Page>().SelectMany(p => p.Cast<PageItem>().OfType<PageText>()).Select(t => t.Text));

        private static string Head(PageText t) => t.Text.Length > 20 ? t.Text.Substring(0, 20) : t.Text;

        private static int CountOf(string text, string token) =>
            Regex.Matches(text, Regex.Escape(token)).Count;
    }
}
#endif
