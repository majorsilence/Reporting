#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// An External-source image asks the host-supplied IExternalImageResolver on the Report for
    /// its bytes instead of the engine's own HttpClient/file path. No resolver set keeps the
    /// engine's existing behaviour; a null result keeps the engine's missing-image behaviour; a
    /// value repeated across instances resolves once through the per-report external-image cache.
    /// </summary>
    [TestFixture]
    public class ExternalImageResolverTests
    {
        private Uri _reportFolder;

        [SetUp]
        public void SetUp()
        {
            _reportFolder = GeneralUtils.ReportsFolder();
            RdlEngineConfig.RdlEngineConfigInit();
        }

        private sealed class StubResolver : IExternalImageResolver
        {
            private readonly Func<string, ExternalImageResult> _resolve;

            public StubResolver(Func<string, ExternalImageResult> resolve)
            {
                _resolve = resolve;
            }

            public ConcurrentQueue<(string Value, int? WidthPx, int? HeightPx)> Calls { get; } = new();

            public Task<ExternalImageResult> Resolve(string value, int? widthPx, int? heightPx, CancellationToken cancellationToken)
            {
                Calls.Enqueue((value, widthPx, heightPx));
                return Task.FromResult(_resolve(value));
            }
        }

        private async Task<Pages> BuildPages(string rdlFile, IExternalImageResolver resolver)
        {
            Directory.SetCurrentDirectory(_reportFolder.LocalPath);
            Report report = await RdlUtils.GetReport(new Uri(_reportFolder, rdlFile));
            Assert.That(report, Is.Not.Null);
            report.Folder = _reportFolder.LocalPath;
            report.ExternalImageResolver = resolver;
            await report.RunGetData();
            return await report.BuildPages();
        }

        private byte[] CheckerPng()
        {
            // 40x20 PNG, the same picture ImageSizing.rdl embeds.
            return File.ReadAllBytes(Path.Combine(_reportFolder.LocalPath, "checker.png"));
        }

        private static List<PageImage> DrawnImages(Pages pgs)
        {
            return pgs.Cast<Page>()
                .SelectMany(p => p.Cast<PageItem>().OfType<PageImage>())
                .Where(i => i.GetImageData() != null && i.GetImageData().Length > 0)
                .ToList();
        }

        [Test]
        public async Task ExternalImage_WithResolver_DrawsResolvedBytes()
        {
            var resolver = new StubResolver(_ => new ExternalImageResult { Content = CheckerPng(), MimeType = "image/png" });

            Pages pgs = await BuildPages("ExternalImageResolverTest.rdl", resolver);

            var images = DrawnImages(pgs);
            Assert.That(images, Has.Count.EqualTo(2), "both expression-image instances draw");
            Assert.That(images.All(i => i.SamplesW == 40 && i.SamplesH == 20), Is.True,
                "the drawn bytes are the resolver's 40x20 PNG");
            Assert.That(resolver.Calls, Is.Not.Empty);
            (string value, int? widthPx, int? heightPx) = resolver.Calls.First();
            Assert.That(value, Is.EqualTo("http://img.example/render?patient=1"), "the evaluated expression value reaches the resolver");
            Assert.That(widthPx, Is.EqualTo(192), "width hint is the 2in design width at 96 dpi");
            Assert.That(heightPx, Is.EqualTo(96), "height hint is the 1in design height at 96 dpi");
        }

        [Test]
        public async Task ExternalImage_ResolverCalledOncePerDistinctValue()
        {
            var resolver = new StubResolver(_ => new ExternalImageResult { Content = CheckerPng(), MimeType = "image/png" });

            Pages pgs = await BuildPages("ExternalImageResolverTest.rdl", resolver);

            Assert.That(DrawnImages(pgs), Has.Count.EqualTo(2), "both instances draw");
            Assert.That(resolver.Calls.Count, Is.EqualTo(1), "the second instance of the same value is served by the per-report cache");
        }

        [Test]
        public async Task ExternalImage_ResolverReturnsNull_ImageOmitted()
        {
            var resolver = new StubResolver(_ => null);

            Pages pgs = await BuildPages("ExternalImageResolverTest.rdl", resolver);

            Assert.That(DrawnImages(pgs), Is.Empty, "a null result keeps the engine's missing-image behaviour");
            Assert.That(pgs.Cast<Page>().Count(), Is.GreaterThanOrEqualTo(1), "the report still renders");
        }

        [Test]
        public async Task ExternalBackgroundImage_WithResolver_GoesThroughResolver()
        {
            var resolver = new StubResolver(_ => new ExternalImageResult { Content = CheckerPng(), MimeType = "image/png" });

            await BuildPages("ExternalBackgroundImageTest.rdl", resolver);

            Assert.That(resolver.Calls.Select(c => c.Value), Has.Some.EqualTo("http://img.example/band.png"),
                "a background image's bytes come from the resolver, never the engine's own fetch path");
        }

        [Test]
        public async Task HiddenExternalImage_IsNeverResolved_EvenInHtml()
        {
            var resolver = new StubResolver(_ => new ExternalImageResult { Content = CheckerPng(), MimeType = "image/png" });
            Directory.SetCurrentDirectory(_reportFolder.LocalPath);
            Report report = await RdlUtils.GetReport(new Uri(_reportFolder, "ExternalImageHiddenTest.rdl"));
            Assert.That(report, Is.Not.Null);
            report.Folder = _reportFolder.LocalPath;
            report.ExternalImageResolver = resolver;
            await report.RunGetData();
            await report.BuildPages();

            using var sink = new MemoryStreamGen();
            await report.RunRender(sink, OutputPresentationType.HTML);

            Assert.That(resolver.Calls, Is.Empty, "no output format costs a fetch for a hidden image instance");
        }

        [Test]
        public async Task ExternalImage_ResolverNull_KeepsEngineBehaviour()
        {
            Pages pgs = await BuildPages("ExternalImageFileTest.rdl", null);

            var images = DrawnImages(pgs);
            Assert.That(images, Has.Count.EqualTo(1), "with no resolver the engine's own external path still loads the file");
            Assert.That(images[0].SamplesW == 40 && images[0].SamplesH == 20, Is.True,
                "the engine loaded checker.png itself");
        }
    }
}
#endif
