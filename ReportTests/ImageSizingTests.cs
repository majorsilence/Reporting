using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using UglyToad.PdfPig;

namespace ReportTests
{
    [TestFixture]
    public class ImageSizingTests
    {
        private Uri _reportFolder;
        private Uri _outputFolder;

        [SetUp]
        public void Prepare()
        {
            _outputFolder = GeneralUtils.OutputTestsFolder();
            _reportFolder = GeneralUtils.ReportsFolder();
            Directory.CreateDirectory(_outputFolder.LocalPath);
        }

        /// <summary>
        /// An image must be drawn at the size its report item asks for.
        ///
        /// The rectangle handed to the renderer used to carry its x and y in points and its
        /// width and height in pixels, so every picture came out DpiX/72 too large - half as
        /// big again at the usual 96 dpi - anchored at the correct top-left corner. Nothing
        /// caught it, because a too-large image still renders and still decodes.
        /// </summary>
        [Test]
        public async Task Image_IsDrawnAtTheSizeItsReportItemAsksFor()
        {
            var rdl = new Uri(_reportFolder, "ImageSizing.rdl");
            Report report = await RdlUtils.GetReport(rdl);
            report.Folder = _reportFolder.LocalPath;
            await report.RunGetData(null);

            string output = Path.Combine(_outputFolder.LocalPath, "ImageSizing.pdf");
            using (var sg = new OneFileStreamGen(output, true))
            {
                await report.RunRender(sg, OutputPresentationType.PDF);
            }

            using var pdf = PdfDocument.Open(output);
            var images = pdf.GetPages().SelectMany(p => p.GetImages()).ToList();
            Assert.That(images.Count, Is.EqualTo(1), "the report has exactly one picture");

            // 2in x 1in, in PDF's own units of 72 to the inch.
            var bounds = images[0].Bounds;
            Assert.Multiple(() =>
            {
                Assert.That(bounds.Width, Is.EqualTo(144d).Within(1d), "width in points");
                Assert.That(bounds.Height, Is.EqualTo(72d).Within(1d), "height in points");
            });
        }
    }
}
