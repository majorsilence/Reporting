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
    public class PageSizeTests
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
        /// A PageHeight of 11in must come out as 792 PDF points, because a PDF point is
        /// 1/72 of an inch - as is a CSS point, which is what RDL sizes are, and a GDI
        /// point. Inches used to be converted with 72.27 - TeX's printer's point - so every
        /// dimension written in inches rendered 0.375% oversized: a Letter page had a
        /// MediaBox of 794x614 instead of 612x792, and a 0.215in table row rendered
        /// 15.535pt tall instead of 15.48. On a page of rows that error is a pitch, so it
        /// compounds - by the thirtieth row the table was half a row adrift of where the
        /// same RDL renders under a correct point.
        /// </summary>
        [Test]
        public async Task LetterPage_IsExactly612By792Points()
        {
            var rdl = new Uri(_reportFolder, "ImageSizing.rdl");
            Report report = await RdlUtils.GetReport(rdl);
            report.Folder = _reportFolder.LocalPath;
            await report.RunGetData(null);

            string output = Path.Combine(_outputFolder.LocalPath, "PageSize.pdf");
            using (var sg = new OneFileStreamGen(output, true))
            {
                await report.RunRender(sg, OutputPresentationType.PDF);
            }

            using var pdf = PdfDocument.Open(output);
            var page = pdf.GetPages().First();
            Assert.Multiple(() =>
            {
                // The report declares 8.5in x 11in. Within half a point, not exact, only
                // because the writer rounds to whole points.
                Assert.That(page.Width, Is.EqualTo(612d).Within(0.5d), "8.5in in points");
                Assert.That(page.Height, Is.EqualTo(792d).Within(0.5d), "11in in points");
            });
        }
    }
}
