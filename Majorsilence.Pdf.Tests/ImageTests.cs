// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Majorsilence.Pdf;
using Majorsilence.Pdf.Layout;
using NUnit.Framework;
using PdfPig = UglyToad.PdfPig.PdfDocument;

namespace Majorsilence.Pdf.Tests
{
    [TestFixture]
    public class ImageTests
    {
        private const float Margin = 36f;

        // A minimal PNG writer: filter type 0 on every scanline, zlib-wrapped.
        internal static byte[] MakePng(int w, int h, int colorType, Func<int, int, byte[]> pixel, byte[]? palette = null)
        {
            int bpp = colorType == 0 || colorType == 3 ? 1 : colorType == 4 ? 2 : colorType == 6 ? 4 : 3;
            var raw = new MemoryStream();
            for (int y = 0; y < h; y++)
            {
                raw.WriteByte(0);
                for (int x = 0; x < w; x++) raw.Write(pixel(x, y), 0, bpp);
            }
            var z = new MemoryStream();
            using (var zs = new ZLibStream(z, CompressionLevel.Fastest, leaveOpen: true)) zs.Write(raw.ToArray());

            var png = new MemoryStream();
            png.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
            void Chunk(string type, byte[] data)
            {
                var len = BitConverter.GetBytes(data.Length); Array.Reverse(len);
                png.Write(len);
                png.Write(System.Text.Encoding.ASCII.GetBytes(type));
                png.Write(data);
                png.Write(new byte[4]); // CRC is not checked by the decoder
            }
            var ihdr = new byte[13];
            ihdr[3] = (byte)w; ihdr[7] = (byte)h; ihdr[8] = 8; ihdr[9] = (byte)colorType;
            Chunk("IHDR", ihdr);
            if (palette != null) Chunk("PLTE", palette);
            Chunk("IDAT", z.ToArray());
            Chunk("IEND", Array.Empty<byte>());
            return png.ToArray();
        }

        internal static byte[] SolidRgbPng(int w, int h) =>
            MakePng(w, h, 2, (x, y) => new byte[] { 200, 30, 30 });

        private static string SampleJpegPath => Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "sample_photo.jpg");

        private static byte[] Render(Action<PdfLayout> configure)
        {
            var layout = PdfLayout.Begin(PdfDocument.Create(), PageSizes.A4).WithMargins(Margin);
            configure(layout);
            return layout.End().ToBytes();
        }

        private static UglyToad.PdfPig.Content.IPdfImage OnlyImage(byte[] pdfBytes)
        {
            using var pdf = PdfPig.Open(pdfBytes);
            var img = pdf.GetPage(1).GetImages().Single();
            return img;
        }

        [Test]
        public void FromBytes_Png_ReadsDimensions_AndAllSupportedColorTypes()
        {
            Assert.That(PdfImage.FromBytes(SolidRgbPng(8, 4)).PixelWidth, Is.EqualTo(8));
            Assert.That(PdfImage.FromBytes(MakePng(5, 3, 0, (x, y) => new byte[] { 128 })).PixelHeight, Is.EqualTo(3));
            Assert.That(PdfImage.FromBytes(MakePng(5, 3, 4, (x, y) => new byte[] { 128, 255 })).PixelWidth, Is.EqualTo(5));
            Assert.That(PdfImage.FromBytes(MakePng(5, 3, 6, (x, y) => new byte[] { 1, 2, 3, 128 })).PixelWidth, Is.EqualTo(5));
            Assert.That(PdfImage.FromBytes(MakePng(5, 3, 3, (x, y) => new byte[] { 1 }, new byte[] { 0, 0, 0, 255, 0, 0 })).PixelWidth, Is.EqualTo(5));
        }

        [Test]
        public void FromBytes_Jpeg_ReadsDimensionsFromTheFrameHeader()
        {
            var img = PdfImage.FromBytes(File.ReadAllBytes(SampleJpegPath));
            Assert.That(img.PixelWidth, Is.GreaterThan(0));
            Assert.That(img.PixelHeight, Is.GreaterThan(0));
        }

        [Test]
        public void FromBytes_RejectsUnknownFormats()
        {
            Assert.Throws<ArgumentException>(() => PdfImage.FromBytes(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }));
            Assert.Throws<ArgumentException>(() => PdfImage.FromBytes(new byte[] { 0xFF, 0xD8, 0xFF }));
        }

        [Test]
        public void FromBytes_InterlacedPng_IsRejectedWithAClearMessage()
        {
            var png = SolidRgbPng(4, 4);
            png[8 + 8 + 12] = 1; // IHDR interlace method
            var ex = Assert.Throws<ArgumentException>(() => PdfImage.FromBytes(png))!;
            Assert.That(ex.Message, Does.Contain("not supported"));
        }

        [Test]
        public void FromRgb24_And_FromRgba_ValidateBufferSize()
        {
            Assert.Throws<ArgumentException>(() => PdfImage.FromRgb24(new byte[5], 2, 2));
            Assert.Throws<ArgumentException>(() => PdfImage.FromRgba(new byte[5], 2, 2));
            Assert.DoesNotThrow(() => PdfImage.FromRgb24(new byte[12], 2, 2));
        }

        [Test]
        public void Canvas_DrawImage_PlacesPngAtTheRequestedRectangle()
        {
            var doc = PdfDocument.Create();
            doc.AddPage(PageSizes.A4).DrawImage(PdfImage.FromBytes(SolidRgbPng(8, 8)), 100, 100, 80, 40);
            var img = OnlyImage(doc.ToBytes());
            Assert.That(img.Bounds.Width, Is.EqualTo(80).Within(0.5));
            Assert.That(img.Bounds.Height, Is.EqualTo(40).Within(0.5));
            Assert.That(img.Bounds.Left, Is.EqualTo(100).Within(0.5));
        }

        [Test]
        public void PngWithAlpha_IsWrittenWithASoftMask()
        {
            var doc = PdfDocument.Create();
            doc.AddPage(PageSizes.A4).DrawImage(PdfImage.FromBytes(MakePng(4, 4, 6, (x, y) => new byte[] { 255, 0, 0, 100 })), 10, 10, 50, 50);
            Assert.That(System.Text.Encoding.Latin1.GetString(doc.ToBytes()), Does.Contain("/SMask"));
        }

        [Test]
        public void SameSizedDifferentImages_AreNotMergedIntoOne()
        {
            var doc = PdfDocument.Create();
            var page = doc.AddPage(PageSizes.A4);
            page.DrawImage(PdfImage.FromRgb24(Enumerable.Repeat((byte)10, 12).ToArray(), 2, 2), 0, 0, 10, 10);
            page.DrawImage(PdfImage.FromRgb24(Enumerable.Repeat((byte)200, 12).ToArray(), 2, 2), 20, 0, 10, 10);
            using var pdf = PdfPig.Open(doc.ToBytes());
            var images = pdf.GetPage(1).GetImages().ToList();
            Assert.That(images, Has.Count.EqualTo(2));
            Assert.That(images[0].RawBytes.ToArray(), Is.Not.EqualTo(images[1].RawBytes.ToArray()));
        }

        [Test]
        public void SameImageDrawnTwice_IsEmbeddedOnce()
        {
            var doc = PdfDocument.Create();
            var image = PdfImage.FromBytes(SolidRgbPng(8, 8));
            doc.AddPage(PageSizes.A4).DrawImage(image, 0, 0, 10, 10);
            doc.AddPage(PageSizes.A4).DrawImage(image, 0, 0, 10, 10);
            string raw = System.Text.Encoding.Latin1.GetString(doc.ToBytes());
            Assert.That(System.Text.RegularExpressions.Regex.Matches(raw, "/Subtype /Image").Count, Is.EqualTo(1));
        }

        [Test]
        public void Layout_Image_WidthOnly_KeepsAspectRatio()
        {
            var image = PdfImage.FromBytes(SolidRgbPng(40, 20));
            var bounds = OnlyImage(Render(l => l.Image(image, width: 200))).Bounds;
            Assert.That(bounds.Width, Is.EqualTo(200).Within(0.5));
            Assert.That(bounds.Height, Is.EqualTo(100).Within(0.5));
        }

        [Test]
        public void Layout_Image_HeightOnly_DerivesTheWidth()
        {
            var image = PdfImage.FromBytes(SolidRgbPng(40, 20));
            var bounds = OnlyImage(Render(l => l.Image(image, height: 50))).Bounds;
            Assert.That(bounds.Width, Is.EqualTo(100).Within(0.5));
            Assert.That(bounds.Height, Is.EqualTo(50).Within(0.5));
        }

        [Test]
        public void Layout_Image_Contain_LetterboxesInsideTheBox()
        {
            var image = PdfImage.FromBytes(SolidRgbPng(40, 20)); // 2:1
            var bounds = OnlyImage(Render(l => l.Image(image, 100, 100, ImageFit.Contain))).Bounds;
            Assert.That(bounds.Width, Is.EqualTo(100).Within(0.5));
            Assert.That(bounds.Height, Is.EqualTo(50).Within(0.5));
        }

        [Test]
        public void Layout_Image_Fill_StretchesToTheBox()
        {
            var image = PdfImage.FromBytes(SolidRgbPng(40, 20));
            var bounds = OnlyImage(Render(l => l.Image(image, 100, 100, ImageFit.Fill))).Bounds;
            Assert.That(bounds.Width, Is.EqualTo(100).Within(0.5));
            Assert.That(bounds.Height, Is.EqualTo(100).Within(0.5));
        }

        [Test]
        public void Layout_Image_Cover_OverflowsTheBoxAndIsClipped()
        {
            var image = PdfImage.FromBytes(SolidRgbPng(40, 20));
            var bytes = Render(l => l.Image(image, 100, 100, ImageFit.Cover));
            var bounds = OnlyImage(bytes).Bounds;
            Assert.That(bounds.Height, Is.EqualTo(100).Within(0.5));
            Assert.That(bounds.Width, Is.EqualTo(200).Within(0.5));
            Assert.That(bytes.Length, Is.GreaterThan(0));
        }

        [Test]
        public void Layout_Image_RightAligned()
        {
            var image = PdfImage.FromBytes(SolidRgbPng(10, 10));
            var bounds = OnlyImage(Render(l => l.Image(image, 100, 100, alignment: TextAlignment.Right))).Bounds;
            Assert.That(bounds.Right, Is.EqualTo(PageSizes.A4.Width - Margin).Within(0.5));
        }

        [Test]
        public void Layout_Image_MovesToNextPage_WhenItDoesNotFit()
        {
            var image = PdfImage.FromBytes(SolidRgbPng(10, 10));
            var bytes = Render(l => { l.Spacer(PageSizes.A4.Height - 2 * Margin - 50); l.Image(image, 100, 100); });
            using var pdf = PdfPig.Open(bytes);
            Assert.That(pdf.NumberOfPages, Is.EqualTo(2));
            Assert.That(pdf.GetPage(2).GetImages().Count(), Is.EqualTo(1));
        }

        [Test]
        public void Layout_Image_TallerThanAPage_Throws()
        {
            var image = PdfImage.FromBytes(SolidRgbPng(10, 10));
            var layout = PdfLayout.Begin(PdfDocument.Create(), PageSizes.A4).WithMargins(Margin);
            Assert.Throws<InvalidOperationException>(() => layout.Image(image, 100, 5000));
        }

        [Test]
        public void Layout_Image_InsideBoxAndRowColumns()
        {
            var image = PdfImage.FromBytes(SolidRgbPng(10, 10));
            var bytes = Render(l => l.Row(r =>
            {
                r.Column(0.5f, c => c.Image(image, 50));
                r.Column(0.5f, c => c.Box(LayoutBoxStyle.Default.WithPadding(5), b => b.Image(image, 30)));
            }));
            using var pdf = PdfPig.Open(bytes);
            Assert.That(pdf.GetPage(1).GetImages().Count(), Is.EqualTo(2));
        }

        [Test]
        public void Jpeg_DrawsAsAnImage()
        {
            var image = PdfImage.FromBytes(File.ReadAllBytes(SampleJpegPath));
            var bytes = Render(l => l.Image(image, 100));
            Assert.That(OnlyImage(bytes).Bounds.Width, Is.EqualTo(100).Within(0.5));
        }
    }
}
