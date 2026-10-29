#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SixImage = SixLabors.ImageSharp.Image;

namespace ReportTests
{
    /// <summary>
    /// "Inking" regression tests: render a fixed set of reports the same way the TIF export does
    /// (Majorsilence.Forms.Drawing/SkiaSharp rasterizing each page) and compare every page,
    /// pixel by pixel, against a baseline PNG checked into ReportTests/InkingBaselines/. A change
    /// that alters what actually gets drawn - a font substitution, a shifted box, a wrong color, a
    /// sizing/position regression - fails here even when no individual text or coordinate
    /// assertion would catch it.
    ///
    /// To (re)create the baselines from the current rendering output, run once with
    /// MAJORSILENCE_UPDATE_INKING_BASELINES=1 set, then review and commit the PNGs it writes to
    /// ReportTests/InkingBaselines/.
    /// </summary>
    [TestFixture]
    public class InkingRegressionTests
    {
        // A pixel counts as "different" once any channel differs by more than this (0-255).
        // Covers the small amount of antialiasing noise rendering can legitimately vary by.
        private const int ChannelTolerance = 24;

        // A page fails once more than this fraction of its pixels are "different".
        private const double MaxDifferingPixelFraction = 0.0005;

        private static readonly bool UpdateBaselines =
            Environment.GetEnvironmentVariable("MAJORSILENCE_UPDATE_INKING_BASELINES") == "1";

        private Uri _reportFolder;
        private string _baselineFolder;

        [SetUp]
        public void SetUp()
        {
            _reportFolder = GeneralUtils.ReportsFolder();
            _baselineFolder = Path.Combine(_reportFolder.LocalPath, "InkingBaselines");
            RdlEngineConfig.RdlEngineConfigInit();
        }

        // A deliberately small, varied set: self-contained JSON-fed business documents (adapted
        // from the Majorsilence.RaaS template library - see Examples/InkingTemplates/README.md)
        // plus a few existing example reports already exercised elsewhere (table, matrix, chart,
        // multi-page table-heavy layout).
        private static readonly string[] Reports =
        {
            "invoice1-a4.rdl",
            "receipt1-a4.rdl",
            "purchase-order1-a4.rdl",
            "training-certificate1-a4.rdl",
            "ListReport.rdl",
            "MatrixExample.rdl",
            "ChartTypes.rdl",
            "meeting-minutes1-a4.rdl",
        };

        [Test, TestCaseSource(nameof(Reports))]
        public async Task RenderedPagesMatchBaseline(string reportFile)
        {
            Directory.SetCurrentDirectory(_reportFolder.LocalPath);
            Uri fileRdlUri = new Uri(_reportFolder, reportFile);

            Report report = await RdlUtils.GetReport(fileRdlUri);
            Assert.That(report, Is.Not.Null, $"Report '{reportFile}' failed to parse (fatal errors)");

            report.Folder = _reportFolder.LocalPath;
            await report.RunGetData();

            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.TIF);

            var stream = ms.GetStream();
            stream.Position = 0;
            using var tiff = SixImage.Load<Rgba32>(stream);

            string baseName = Path.GetFileNameWithoutExtension(reportFile);

            if (UpdateBaselines)
            {
                Directory.CreateDirectory(_baselineFolder);
                for (int pageIndex = 0; pageIndex < tiff.Frames.Count; pageIndex++)
                {
                    using Image<Rgba32> page = tiff.Frames.CloneFrame(pageIndex);
                    page.SaveAsPng(Path.Combine(_baselineFolder, $"{baseName}-page{pageIndex + 1}.png"));
                }

                Assert.Pass($"Wrote {tiff.Frames.Count} baseline page(s) for '{reportFile}' to {_baselineFolder}");
                return;
            }

            var failures = new List<string>();
            for (int pageIndex = 0; pageIndex < tiff.Frames.Count; pageIndex++)
            {
                using Image<Rgba32> page = tiff.Frames.CloneFrame(pageIndex);
                string baselinePath = Path.Combine(_baselineFolder, $"{baseName}-page{pageIndex + 1}.png");

                if (!File.Exists(baselinePath))
                {
                    failures.Add($"page {pageIndex + 1}: no baseline at {baselinePath} " +
                        "(run with MAJORSILENCE_UPDATE_INKING_BASELINES=1 to create it)");
                    continue;
                }

                using Image<Rgba32> baseline = SixImage.Load<Rgba32>(baselinePath);

                if (page.Width != baseline.Width || page.Height != baseline.Height)
                {
                    failures.Add($"page {pageIndex + 1}: rendered size {page.Width}x{page.Height} " +
                        $"!= baseline size {baseline.Width}x{baseline.Height}");
                    continue;
                }

                double differingFraction = DifferingPixelFraction(page, baseline);
                if (differingFraction > MaxDifferingPixelFraction)
                {
                    failures.Add($"page {pageIndex + 1}: {differingFraction:P3} of pixels differ by more than " +
                        $"{ChannelTolerance}/255 (allowed {MaxDifferingPixelFraction:P3})");
                }
            }

            Assert.That(failures, Is.Empty, $"Inking mismatch for '{reportFile}':\n" + string.Join("\n", failures));
        }

        private static double DifferingPixelFraction(Image<Rgba32> actual, Image<Rgba32> baseline)
        {
            int pixelCount = actual.Width * actual.Height;
            var actualPixels = new Rgba32[pixelCount];
            var baselinePixels = new Rgba32[pixelCount];
            actual.CopyPixelDataTo(actualPixels);
            baseline.CopyPixelDataTo(baselinePixels);

            long differing = 0;
            for (int i = 0; i < pixelCount; i++)
            {
                Rgba32 a = actualPixels[i];
                Rgba32 b = baselinePixels[i];
                if (Math.Abs(a.R - b.R) > ChannelTolerance ||
                    Math.Abs(a.G - b.G) > ChannelTolerance ||
                    Math.Abs(a.B - b.B) > ChannelTolerance)
                {
                    differing++;
                }
            }

            return (double)differing / pixelCount;
        }
    }
}
#endif
