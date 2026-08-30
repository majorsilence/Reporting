using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using SkiaSharp;
using UglyToad.PdfPig;
using ZXing;

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

        private static BarcodeFormat ExpectedFormat(string barcodeType) => barcodeType switch
        {
            "QrCode" => BarcodeFormat.QR_CODE,
            "BarCode128" => BarcodeFormat.CODE_128,
            "AztecCode" => BarcodeFormat.AZTEC,
            "DataMatrix" => BarcodeFormat.DATA_MATRIX,
            "Pdf417" => BarcodeFormat.PDF_417,
            "BarCode39" => BarcodeFormat.CODE_39,
            _ => throw new ArgumentOutOfRangeException(nameof(barcodeType), barcodeType, "Unknown barcode type"),
        };

        // Renders barcode.rdl to PDF with the barcode type chosen by parameter, then reads the
        // barcode back out of the PDF to prove the render is machine-decodable. Everything runs the
        // SkiaSharp path now (ZXing.Net.Bindings.SkiaSharp for both the writer in RdlCri and the
        // reader here). The report lays the barcode out at several sizes and also emits 1x1 spacer
        // images, so the assertion is that *some* extracted image decodes as the requested format.
        [Test, TestCaseSource(nameof(BarCodeTypes))]
        public async Task RenderPdf_BarcodeTypesViaParameter(string barcodeType)
        {
            var expected = ExpectedFormat(barcodeType);

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

            Assert.That(images, Is.Not.Empty, "No images found in PDF");

            var reader = new ZXing.SkiaSharp.BarcodeReader { Options = { TryHarder = true } };
            var decoded = new List<BarcodeFormat>();

            foreach (var image in images)
            {
                var imageBytes = image.TryGetPng(out var pngBytes) ? pngBytes : image.RawBytes.ToArray();
                using var barcodeBitmap = SKBitmap.Decode(imageBytes);
                if (barcodeBitmap is null || barcodeBitmap.Width < 8 || barcodeBitmap.Height < 8)
                    continue;

                var result = reader.Decode(barcodeBitmap);
                if (result != null)
                    decoded.Add(result.BarcodeFormat);
            }

            Assert.That(decoded, Does.Contain(expected),
                $"No image in the PDF decoded as {expected}. Decoded formats: [{string.Join(", ", decoded)}]");
        }
    }
}
