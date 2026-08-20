using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using UglyToad.PdfPig;
using ZXing;
#if DRAWINGCOMPAT
using Majorsilence.Forms.Drawing;
using System.Drawing;  // value types (Color, Point, Size, Rectangle, ...) come from System.Drawing.Primitives
using ZXing.SkiaSharp;

#else
using System.Drawing;
using ZXing.Windows.Compatibility;
#endif

namespace ReportTests.Utils
{
    [TestFixture]
    public class RenderPdf_Base64ImageParameter
    {
        private Uri _reportFolder = null;
        private Uri _outputFolder = null;

        [SetUp]
        public void Prepare2Tests()
        {
            if (_outputFolder == null)
            {
                _outputFolder = GeneralUtils.OutputTestsFolder();
            }

            _reportFolder = GeneralUtils.ReportsFolder();
            Directory.CreateDirectory(_outputFolder.LocalPath);
            RdlEngineConfig.RdlEngineConfigInit();
        }

        [Test]
        public async Task Base64ImageParameterTest()
        {
            Uri fileRdlUri = new Uri(_reportFolder, "barcode.rdl");
            // We change dir so the SQL lite database is found
            System.IO.Directory.SetCurrentDirectory(_reportFolder.LocalPath);
            Report rap = await RdlUtils.GetReport(fileRdlUri,
                $"Data Source={_reportFolder.LocalPath}sqlitetestdb2.db");

            rap.Folder = _reportFolder.LocalPath;

            var parameters = new Dictionary<string, string>
            {
                { "BarCode", "QrCode" },
                { "Logo", GetBase64Image(System.IO.Path.Combine(_reportFolder.LocalPath, "logo-example.png")) }
            };

            await rap.RunGetData(parameters);

            string fullOutputPath = System.IO.Path.Combine(_outputFolder.LocalPath, $".pdf");
            using var sg = new OneFileStreamGen(fullOutputPath, true);
            await rap.RunRender(sg, OutputPresentationType.PDF);

            using var pdfDocument = PdfDocument.Open(fullOutputPath);
            var images = pdfDocument.GetPages().SelectMany(page => page.GetImages()).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(images.Count, Is.GreaterThan(0), "No images found in PDF");
            });
        }

        private string GetBase64Image(string filePath)
        {
            byte[] imageBytes = System.IO.File.ReadAllBytes(filePath);
            return Convert.ToBase64String(imageBytes);
        }
    }
}