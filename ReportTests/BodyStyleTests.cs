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
    /// The Body's own Style (background, border) is drawn behind its items, as a rectangle -
    /// for the top-level report on every page, and for a subreport's body (#545). Previously
    /// Body.Style was parsed but only read by the HTML renderers, so a page renderer (PDF,
    /// image formats, ...) never drew it, and a subreport's body never drew it in any format.
    /// </summary>
    [TestFixture]
    public class BodyStyleTests
    {
        private Uri _reportFolder;

        [SetUp]
        public void SetUp()
        {
            _reportFolder = GeneralUtils.ReportsFolder();
            RdlEngineConfig.RdlEngineConfigInit();
        }

        // Parsed with the folder set first, because a Subreport is resolved during the
        // parse's final pass.
        private async Task<Report> LoadReport(string rdlFile)
        {
            Uri fileRdlUri = new Uri(_reportFolder, rdlFile);
            Directory.SetCurrentDirectory(_reportFolder.LocalPath);

            var parser = new RDLParser(File.ReadAllText(fileRdlUri.LocalPath))
            {
                Folder = _reportFolder.LocalPath
            };
            Report report = await parser.Parse();
            Assert.That(report, Is.Not.Null, "Report failed to parse");
            report.Folder = _reportFolder.LocalPath;
            await report.RunGetData();
            return report;
        }

        private async Task<List<PageItem>> FirstPage(string rdlFile)
        {
            Report report = await LoadReport(rdlFile);
            Pages pages = await report.BuildPages();
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(8),
                "errors: " + (report.ErrorItems == null ? "(none)" : string.Join(" | ", report.ErrorItems.OfType<object>())));
            return pages.Cast<Page>().First().Cast<PageItem>().ToList();
        }

        private static PageText Text(List<PageItem> items, string text) =>
            items.OfType<PageText>().Single(t => t.Text == text);

        [Test]
        public async Task TopLevelBody_DrawsItsStyleBehindItsItems()
        {
            var items = await FirstPage("BodyStyleTest.rdl");
            var bodyText = Text(items, "BODY-TEXT");
            var background = items.OfType<PageRectangle>()
                .Single(r => r.SI.BackgroundColor == System.Drawing.ColorTranslator.FromHtml("#f9f9f9"));

            Assert.Multiple(() =>
            {
                Assert.That(background.X, Is.EqualTo(bodyText.X).Within(0.5f), "the background starts at the body's left margin");
                Assert.That(background.Y, Is.EqualTo(bodyText.Y).Within(0.5f), "the background starts at the top of the page");
                Assert.That(background.W, Is.EqualTo(6.5f * 72f).Within(0.5f), "the background spans the report's Width");
                // The content (0.25in) is shorter than the Body's designed Height (3in): the
                // background keeps the designed Height instead of shrinking to the content.
                Assert.That(background.H, Is.EqualTo(3f * 72f).Within(0.5f));
                Assert.That(background.SI.BStyleTop, Is.EqualTo(BorderStyleEnum.Solid));

                // The background is behind the item, not drawn over it.
                Assert.That(items.IndexOf(background), Is.LessThan(items.IndexOf(bodyText)));
            });
        }

        [Test]
        public async Task SubreportBody_DrawsItsStyleBehindItsItemsAtTheSubreportsPosition()
        {
            var items = await FirstPage("BodyStyleSubreportTest.rdl");
            var subText = Text(items, "SUB-TEXT");
            var background = items.OfType<PageRectangle>()
                .Single(r => r.SI.BackgroundColor == System.Drawing.ColorTranslator.FromHtml("#f9f9f9"));

            Assert.Multiple(() =>
            {
                // The subreport is placed at Top=0.5in, Left=1in in the parent report.
                Assert.That(background.X, Is.EqualTo(subText.X).Within(0.5f));
                Assert.That(background.Y, Is.EqualTo(subText.Y).Within(0.5f));
                Assert.That(background.W, Is.EqualTo(3f * 72f).Within(0.5f), "the background spans the subreport's own report Width");
                // The content (0.25in) is shorter than the subreport body's designed Height (1in).
                Assert.That(background.H, Is.EqualTo(1f * 72f).Within(0.5f));
                Assert.That(background.SI.BStyleTop, Is.EqualTo(BorderStyleEnum.Solid));

                Assert.That(items.IndexOf(background), Is.LessThan(items.IndexOf(subText)));
            });
        }

        // #628: SSRS keeps the space the design leaves below the body's lowest item, not the
        // designed Height. The 2in body holds a shown 0.25in textbox and a hidden one ending at
        // 1.75in, so it renders 0.25in of text plus the 0.25in left below the hidden one.
        [Test]
        public async Task TopLevelBody_ShrinksWhenItsItemsRenderShorter()
        {
            var items = await FirstPage("BodyShrinkTest.rdl");
            var shown = Text(items, "SHOWN-TEXT");
            var background = items.OfType<PageRectangle>()
                .Single(r => r.SI.BackgroundColor == System.Drawing.ColorTranslator.FromHtml("#f9f9f9"));

            Assert.Multiple(() =>
            {
                Assert.That(items.OfType<PageText>().Any(t => t.Text == "HIDDEN-TEXT"), Is.False);
                Assert.That(background.Y, Is.EqualTo(shown.Y).Within(0.5f));
                Assert.That(background.H, Is.EqualTo(0.5f * 72f).Within(0.5f));
            });
        }

        [Test]
        public async Task SubreportBody_ShrinksWhenItsItemsRenderShorter_AndItemsBelowFollow()
        {
            var items = await FirstPage("BodyShrinkSubreportTest.rdl");
            var shown = Text(items, "SHOWN-TEXT");
            var below = Text(items, "BELOW-TEXT");
            var background = items.OfType<PageRectangle>()
                .Single(r => r.SI.BackgroundColor == System.Drawing.ColorTranslator.FromHtml("#f9f9f9"));

            Assert.Multiple(() =>
            {
                Assert.That(background.Y, Is.EqualTo(shown.Y).Within(0.5f));
                Assert.That(background.H, Is.EqualTo(0.5f * 72f).Within(0.5f));
                // Designed 0.25in below the subreport; it follows the box's real bottom.
                Assert.That(below.Y, Is.EqualTo(background.Y + background.H + 0.25f * 72f).Within(0.5f));
            });
        }

        [TestCase("BodyStyleTest.rdl")]
        [TestCase("BodyStyleSubreportTest.rdl")]
        [TestCase("BodyShrinkTest.rdl")]
        [TestCase("BodyShrinkSubreportTest.rdl")]
        public async Task RendersToPdf_WithoutError(string rdlFile)
        {
            Report report = await LoadReport(rdlFile);

            Uri outputFolder = GeneralUtils.OutputTestsFolder();
            Directory.CreateDirectory(outputFolder.LocalPath);
            string outFile = Path.Combine(outputFolder.LocalPath, rdlFile + "_BodyStyle.pdf");

            using var sg = new OneFileStreamGen(outFile, true);
            await report.RunRender(sg, OutputPresentationType.RenderPdf_Majorsilence);

            Assert.That(new FileInfo(outFile).Length, Is.GreaterThan(0));
        }
    }
}
#endif
