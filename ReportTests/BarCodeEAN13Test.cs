#if NET8_0_OR_GREATER
using NUnit.Framework;
using System.Collections.Generic;
using Majorsilence.Reporting.Cri;
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
    /// <summary>
    /// Issue #182: EAN-13 barcodes rendered with poor quality. The bars were drawn in
    /// millimetre coordinates so each module edge fell on a fractional pixel and was
    /// anti-aliased into a grey smear, degrading scan contrast. Bars are now snapped to
    /// whole pixels. These tests verify the rendered barcode (a) still decodes as a valid
    /// EAN-13 at several sizes and (b) has crisp, un-blurred bars.
    /// </summary>
    [TestFixture]
    public class BarCodeEAN13Test
    {
        private static Bitmap Render(int width, int height, out string expectedPrefix)
        {
            var barcode = new BarCodeEAN13();
            barcode.SetProperties(new Dictionary<string, object>
            {
                // Non-zero number system so it decodes unambiguously as EAN-13; a leading
                // zero makes it equivalent to (and reported as) UPC-A.
                { "NumberSystem", "40" },
                { "ManufacturerCode", "12345" },
                { "ProductCode", "67890" },
            });
            expectedPrefix = "401234567890"; // number system + manufacturer + product (check digit appended by encoder)

            var bm = new Bitmap(width, height);
            barcode.DrawImage(ref bm);
            return bm;
        }

        [TestCase(300, 150)]
        [TestCase(400, 200)]
        [TestCase(240, 120)]
        public void RenderedBarcode_DecodesAsEan13(int width, int height)
        {
            using var bm = Render(width, height, out string expectedPrefix);

            var reader = new BarcodeReader();
            var result = reader.Decode(bm);

            Assert.That(result, Is.Not.Null, $"EAN-13 barcode at {width}x{height} could not be decoded");
            Assert.That(result.BarcodeFormat, Is.EqualTo(BarcodeFormat.EAN_13));
            Assert.That(result.Text, Does.StartWith(expectedPrefix),
                $"Decoded '{result.Text}' does not start with '{expectedPrefix}'");
        }

        [Test]
        public void Bars_AreCrisp_NotAntiAliased()
        {
            using var bm = Render(400, 200, out _);

            // Sample the top band of the barcode: it contains only full-height bars and
            // white background (the human-readable digits sit at the bottom). With whole-
            // pixel bars every sampled pixel should be near-black or near-white; a blurred
            // (mm-positioned) barcode would produce many intermediate grey pixels.
            int bandTop = 2;
            int bandBottom = bm.Height / 5;
            int total = 0, grey = 0;
            for (int y = bandTop; y < bandBottom; y++)
            {
                for (int x = 0; x < bm.Width; x++)
                {
                    var c = bm.GetPixel(x, y);
                    bool black = c.R < 64 && c.G < 64 && c.B < 64;
                    bool white = c.R > 192 && c.G > 192 && c.B > 192;
                    total++;
                    if (!black && !white)
                        grey++;
                }
            }

            Assert.That(total, Is.GreaterThan(0), "No pixels sampled");
            double greyFraction = (double)grey / total;
            Assert.That(greyFraction, Is.LessThan(0.05),
                $"Bar region should be crisp black/white; {greyFraction:P1} of pixels were anti-aliased grey.");
        }
    }
}
#endif
