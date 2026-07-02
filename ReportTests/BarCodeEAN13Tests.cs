using System;
using System.IO;
using System.Threading.Tasks;
using Majorsilence.Reporting.Cri;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using ZXing;
#if DRAWINGCOMPAT
using Majorsilence.Drawing;
using ZXing.SkiaSharp;
#else
using System.Drawing;
using ZXing.Windows.Compatibility;
#endif

namespace ReportTests
{
    [TestFixture]
    public class BarCodeEAN13Tests
    {
        private Uri _outputFolder = null;

        [SetUp]
        public void Prepare2Tests()
        {
            if (_outputFolder == null)
            {
                _outputFolder = GeneralUtils.OutputTestsFolder();
            }

            Directory.CreateDirectory(_outputFolder.LocalPath);

            RdlEngineConfig.RdlEngineConfigInit();
        }

        [Test]
        public void TestEAN13BarcodeGeneration()
        {
            // Create a barcode instance
            var barcode = new BarCodeEAN13();
            
            // Set properties
            var props = new System.Collections.Generic.Dictionary<string, object>
            {
                { "NumberSystem", "00" },
                { "ManufacturerCode", "12345" },
                { "ProductCode", "67890" }
            };
            
            barcode.SetProperties(props);
            
            // Create bitmap (typical size for barcodes)
            int width = 300;
            int height = 200;
            var bm = new Bitmap(width, height);
            
            // Draw the barcode
            barcode.DrawImage(ref bm);
            
            // Save to file for visual inspection
            string outputPath = Path.Combine(_outputFolder.LocalPath, "ean13_test.png");
            bm.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
            
            // Try to decode the barcode using ZXing
            var reader = new BarcodeReader();
            var result = reader.Decode(bm);
            
            // Assert that the barcode can be decoded
            Assert.That(result, Is.Not.Null, "Barcode could not be decoded");
            Assert.That(result.BarcodeFormat, Is.EqualTo(BarcodeFormat.EAN_13), "Wrong barcode format");
            
            // The expected value is NumberSystem + ManufacturerCode + ProductCode + CheckDigit
            // For "00" + "12345" + "67890", the check digit should be calculated
            string expectedPrefix = "001234567890";
            Assert.That(result.Text, Does.StartWith(expectedPrefix), 
                $"Barcode text '{result.Text}' does not start with expected prefix '{expectedPrefix}'");
            
            Console.WriteLine($"Successfully decoded EAN-13 barcode: {result.Text}");
            
            bm.Dispose();
        }

        [Test]
        public void TestEAN13BarcodeWithDifferentSizes()
        {
            var barcode = new BarCodeEAN13();
            
            var props = new System.Collections.Generic.Dictionary<string, object>
            {
                { "NumberSystem", "50" },
                { "ManufacturerCode", "11111" },
                { "ProductCode", "22222" }
            };
            
            barcode.SetProperties(props);
            
            // Test with different sizes
            int[] widths = { 200, 300, 400 };
            int[] heights = { 150, 200, 250 };
            
            for (int i = 0; i < widths.Length; i++)
            {
                var bm = new Bitmap(widths[i], heights[i]);
                barcode.DrawImage(ref bm);
                
                string outputPath = Path.Combine(_outputFolder.LocalPath, $"ean13_test_{widths[i]}x{heights[i]}.png");
                bm.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
                
                // Try to decode
                var reader = new BarcodeReader();
                var result = reader.Decode(bm);
                
                Assert.That(result, Is.Not.Null, 
                    $"Barcode at size {widths[i]}x{heights[i]} could not be decoded");
                Assert.That(result.BarcodeFormat, Is.EqualTo(BarcodeFormat.EAN_13));
                
                Console.WriteLine($"Size {widths[i]}x{heights[i]}: Successfully decoded EAN-13 barcode: {result.Text}");
                
                bm.Dispose();
            }
        }

        [Test]
        public void TestEAN13DesignerImage()
        {
            var barcode = new BarCodeEAN13();
            
            // Test the designer image (used at design time)
            var bm = new Bitmap(300, 200);
            barcode.DrawDesignerImage(ref bm);
            
            string outputPath = Path.Combine(_outputFolder.LocalPath, "ean13_designer_test.png");
            bm.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
            
            // Try to decode the designer barcode
            var reader = new BarcodeReader();
            var result = reader.Decode(bm);
            
            Assert.That(result, Is.Not.Null, "Designer barcode could not be decoded");
            Assert.That(result.BarcodeFormat, Is.EqualTo(BarcodeFormat.EAN_13));
            
            Console.WriteLine($"Successfully decoded designer EAN-13 barcode: {result.Text}");
            
            bm.Dispose();
        }
    }
}
