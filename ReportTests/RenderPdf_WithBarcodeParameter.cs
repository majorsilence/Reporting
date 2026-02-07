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
using Majorsilence.Drawing;
using ZXing.SkiaSharp;
#else
using System.Drawing;
using ZXing.Windows.Compatibility;
#endif

namespace ReportTests.Utils
{
    [TestFixture]
    public class RenderPdf_WithBarcodeParameter
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

        private static readonly string[] BarCodeTypes =
        {
            "QrCode", "BarCode128", "AztecCode", "DataMatrix", "Pdf417", "BarCode39"
        };

        [Test, TestCaseSource(nameof(BarCodeTypes))]
        public async Task RenderPdf_BarcodeTypesViaParameter(string barcodeType)
        {
            Uri fileRdlUri = new Uri(_reportFolder, "barcode.rdl");
            // We change dir so the SQL lite database is found
            System.IO.Directory.SetCurrentDirectory(_reportFolder.LocalPath);
            Report rap = await RdlUtils.GetReport(fileRdlUri,
                $"Data Source={_reportFolder.LocalPath}sqlitetestdb2.db");

            rap.Folder = _reportFolder.LocalPath;

            var parameters = new Dictionary<string, string> { { "BaRcOdEtYpE", barcodeType } };

            await rap.RunGetData(parameters);

            string fullOutputPath = System.IO.Path.Combine(_outputFolder.LocalPath, $"{barcodeType}.pdf");
            using var sg = new OneFileStreamGen(fullOutputPath, true);
            await rap.RunRender(sg, OutputPresentationType.PDF);

            using var pdfDocument = PdfDocument.Open(fullOutputPath);
            var images = pdfDocument.GetPages().SelectMany(page => page.GetImages()).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(images.Count, Is.GreaterThan(0), "No images found in PDF");

                foreach (var image in images)
                {
#if DRAWINGCOMPAT
                    var writer = new ZXing.SkiaSharp.BarcodeReader();
#elif NET6_0_OR_GREATER
                    var reader = new ZXing.Windows.Compatibility.BarcodeReader();
#else
                    var reader = new ZXing.BarcodeReader();
#endif
                    using var ms = new MemoryStream(image.RawBytes.ToArray());
                    using var barcodeBitmap = new Bitmap(ms);
                    var result = reader.Decode(barcodeBitmap);

                    Assert.That(result, Is.Not.Null);

                    switch (barcodeType)
                    {
                        case "QrCode":
                            Assert.That(result.BarcodeFormat,
                                Is.EqualTo(BarcodeFormat.QR_CODE),
                                "No QrCode image found in PDF");
                            break;
                        case "BarCode128":
                            Assert.That(result.BarcodeFormat,
                                Is.EqualTo(BarcodeFormat.CODE_128),
                                "No BarCode128 image found in PDF");
                            break;
                        case "AztecCode":
                            Assert.That(result.BarcodeFormat,
                                Is.EqualTo(BarcodeFormat.AZTEC),
                                "No AztecCode image found in PDF");
                            break;
                        case "DataMatrix":
                            Assert.That(result.BarcodeFormat,
                                Is.EqualTo(BarcodeFormat.DATA_MATRIX),
                                "No DataMatrix image found in PDF");
                            break;
                        case "Pdf417":
                            Assert.That(result.BarcodeFormat,
                                Is.EqualTo(BarcodeFormat.PDF_417),
                                "No Pdf417 image found in PDF");
                            break;
                        case "BarCode39":
                            Assert.That(result.BarcodeFormat,
                                Is.EqualTo(BarcodeFormat.CODE_39),
                                "No BarCode39 image found in PDF");
                            break;
                        default:
                            Assert.Fail($"Unknown barcode type: {barcodeType}");
                            break;
                    }
                }
            });
        }
    }
}