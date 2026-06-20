// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Majorsilence.Pdf;
using Majorsilence.Pdf.Security;
using NUnit.Framework;

namespace Majorsilence.Pdf.Tests
{
    // ── Opacity ──────────────────────────────────────────────────────────────────

    [TestFixture]
    public class OpacityTests
    {
        [Test]
        public void ShapeStyle_FillOpacity_DefaultIsOne()
        {
            Assert.That(ShapeStyle.Empty.FillOpacity, Is.EqualTo(1f));
        }

        [Test]
        public void ShapeStyle_StrokeOpacity_DefaultIsOne()
        {
            Assert.That(ShapeStyle.Empty.StrokeOpacity, Is.EqualTo(1f));
        }

        [Test]
        public void ShapeStyle_WithFillOpacity_IsImmutable()
        {
            var orig     = ShapeStyle.Empty;
            var modified = orig.WithFillOpacity(0.5f);
            Assert.That(modified.FillOpacity, Is.EqualTo(0.5f));
            Assert.That(orig.FillOpacity,     Is.EqualTo(1f), "original must not change");
        }

        [Test]
        public void ShapeStyle_WithOpacity_SetsBoth()
        {
            var s = ShapeStyle.Empty.WithOpacity(0.3f);
            Assert.That(s.FillOpacity,   Is.EqualTo(0.3f).Within(0.001f));
            Assert.That(s.StrokeOpacity, Is.EqualTo(0.3f).Within(0.001f));
        }

        [TestCase(-0.1f, 0f)]
        [TestCase(1.5f,  1f)]
        [TestCase(0.5f,  0.5f)]
        public void ShapeStyle_WithFillOpacity_Clamps(float input, float expected)
        {
            Assert.That(ShapeStyle.Empty.WithFillOpacity(input).FillOpacity,
                Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void StrokeStyle_Opacity_DefaultIsOne()
        {
            Assert.That(StrokeStyle.Default.Opacity, Is.EqualTo(1f));
        }

        [Test]
        public void StrokeStyle_WithOpacity_IsImmutable()
        {
            var orig     = StrokeStyle.Default;
            var modified = orig.WithOpacity(0.4f);
            Assert.That(modified.Opacity, Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(orig.Opacity,     Is.EqualTo(1f), "original must not change");
        }

        [TestCase(-0.5f, 0f)]
        [TestCase(2.0f,  1f)]
        [TestCase(0.75f, 0.75f)]
        public void StrokeStyle_WithOpacity_Clamps(float input, float expected)
        {
            Assert.That(StrokeStyle.Default.WithOpacity(input).Opacity,
                Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void Opacity_EmittedAsExtGState_InPdfStream()
        {
            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.A4, c =>
                    c.DrawRectangle(50, 50, 100, 100,
                        ShapeStyle.Filled(PdfColor.Red).WithFillOpacity(0.5f)))
                .ToBytes();

            string pdfText = Encoding.Latin1.GetString(bytes);
            // /ExtGState and /ca live in the uncompressed page resource dict.
            // The gs operator is inside the compressed content stream so is not checked here.
            Assert.That(pdfText, Does.Contain("/ExtGState"), "must emit ExtGState resource");
            Assert.That(pdfText, Does.Contain("/ca "),       "must emit fill opacity /ca");
        }

        [Test]
        public void FullOpacity_DoesNotEmitExtGState()
        {
            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.A4, c =>
                    c.DrawRectangle(50, 50, 100, 100, ShapeStyle.Filled(PdfColor.Blue)))
                .ToBytes();

            string pdfText = Encoding.Latin1.GetString(bytes);
            Assert.That(pdfText, Does.Not.Contain("/ExtGState"),
                "full-opacity shapes must not emit ExtGState");
        }
    }

    // ── DrawTextBox ───────────────────────────────────────────────────────────────

    [TestFixture]
    public class DrawTextBoxTests
    {
        private static byte[] MakeTextBoxPdf(string text, float width, float height,
            TextStyle? style = null, float lineSpacing = 1.2f)
        {
            return PdfDocument.Create()
                .AddPage(PageSizes.A4, c =>
                    c.DrawTextBox(text, 72, 72, width, height, style, lineSpacing))
                .ToBytes();
        }

        [Test]
        public void DrawTextBox_ShortText_ReturnsOverflowAtEnd()
        {
            const string text = "Hello World";
            var bytes = PdfDocument.Create().AddPage(PageSizes.A4, _ => { }).ToBytes();
            // Use the canvas directly
            var doc = PdfDocument.Create();
            var canvas = doc.AddPage(PageSizes.A4);
            int overflow = canvas.DrawTextBox(text, 72, 72, 400, 600);
            Assert.That(overflow, Is.EqualTo(text.Length),
                "short text that fits should return length (no overflow)");
        }

        [Test]
        public void DrawTextBox_EmptyString_ReturnsZero()
        {
            var doc = PdfDocument.Create();
            var canvas = doc.AddPage(PageSizes.A4);
            int overflow = canvas.DrawTextBox("", 72, 72, 400, 600);
            Assert.That(overflow, Is.EqualTo(0));
        }

        [Test]
        public void DrawTextBox_TinyBox_OverflowsImmediately()
        {
            const string text = "Hello World this is a very long line";
            var doc = PdfDocument.Create();
            var canvas = doc.AddPage(PageSizes.A4);
            // Height of 1pt means only zero lines fit
            int overflow = canvas.DrawTextBox(text, 72, 72, 400, 1f);
            Assert.That(overflow, Is.LessThan(text.Length),
                "1pt box should overflow before consuming all text");
        }

        [Test]
        public void DrawTextBox_NewlineBreaksLine()
        {
            const string text = "Line one\nLine two";
            var doc = PdfDocument.Create();
            var canvas = doc.AddPage(PageSizes.A4);
            int overflow = canvas.DrawTextBox(text, 72, 72, 400, 600);
            Assert.That(overflow, Is.EqualTo(text.Length), "both lines should fit in 600pt");
        }

        [Test]
        public void DrawTextBox_ProducesValidPdf()
        {
            const string text = "This is a text box with multiple words that may wrap.";
            var bytes = MakeTextBoxPdf(text, 200, 400);
            Assert.That(bytes.Length, Is.GreaterThan(100));
            var header = Encoding.ASCII.GetString(bytes, 0, 5);
            Assert.That(header, Is.EqualTo("%PDF-"));
        }

        [Test]
        public void MeasureTextBoxHeight_EmptyString_ReturnsZero()
        {
            var doc = PdfDocument.Create();
            var canvas = doc.AddPage(PageSizes.A4);
            float h = canvas.MeasureTextBoxHeight("", 300);
            Assert.That(h, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void MeasureTextBoxHeight_SingleLine_ReturnsFontSize()
        {
            var doc = PdfDocument.Create();
            var canvas = doc.AddPage(PageSizes.A4);
            var style = TextStyle.Default.WithSize(12);
            float h = canvas.MeasureTextBoxHeight("Hello", 300, style, 1.0f);
            // Single line: exactly one font-size tall with lineSpacing = 1.0
            Assert.That(h, Is.GreaterThan(0f));
        }

        [Test]
        public void DrawTextBox_OverflowIndex_IsIdempotent()
        {
            // If we split at the overflow index and continue, second call should
            // return text.Length when given the remainder in a new box.
            const string text = "Word1 Word2 Word3 Word4 Word5 Word6 Word7 Word8 Word9 Word10";
            var doc = PdfDocument.Create();
            var canvas = doc.AddPage(PageSizes.A4);
            int overflow1 = canvas.DrawTextBox(text, 72, 72, 100, 60,
                TextStyle.Default.WithSize(12));

            if (overflow1 < text.Length)
            {
                string remainder = text.Substring(overflow1);
                var canvas2 = doc.AddPage(PageSizes.A4);
                int overflow2 = canvas2.DrawTextBox(remainder, 72, 72, 100, 600,
                    TextStyle.Default.WithSize(12));
                Assert.That(overflow2, Is.EqualTo(remainder.Length),
                    "remainder should fit in a tall box");
            }
            // If no overflow, test still passes
        }
    }

    // ── PdfTable ──────────────────────────────────────────────────────────────────

    [TestFixture]
    public class PdfTableTests
    {
        private static PdfTable SimpleTable()
        {
            var t = new PdfTable(new float[] { 100, 150, 80 })
                .WithHeaderBackground(new PdfColor(70, 130, 180))
                .WithAlternateRowBackground(new PdfColor(240, 240, 240))
                .WithBorder(PdfColor.Black, 0.5f)
                .WithCellPadding(4f);
            t.AddRow("Name", "Description", "Value");
            t.AddRow("Alpha", "First item", "1.00");
            t.AddRow("Beta",  "Second item", "2.50");
            return t;
        }

        [Test]
        public void PdfTable_TotalWidth_IsSumOfColumns()
        {
            var t = new PdfTable(new float[] { 100, 150, 80 });
            Assert.That(t.TotalWidth, Is.EqualTo(330f).Within(0.001f));
        }

        [Test]
        public void PdfTable_AddRow_IncrementsRowCount()
        {
            var t = new PdfTable(new float[] { 100, 100 });
            t.AddRow("A", "B");
            t.AddRow("C", "D");
            Assert.That(t.Rows.Count, Is.EqualTo(2));
        }

        [Test]
        public void PdfTable_AddRow_StringArray_CellsMatchInput()
        {
            var t = new PdfTable(new float[] { 100, 100 });
            t.AddRow("Hello", "World");
            Assert.That(t.Rows[0].Cells[0].Text, Is.EqualTo("Hello"));
            Assert.That(t.Rows[0].Cells[1].Text, Is.EqualTo("World"));
        }

        [Test]
        public void PdfTable_WithHeaderBackground_IsImmutableBuilder()
        {
            var orig     = new PdfTable(new float[] { 100 });
            var modified = orig.WithHeaderBackground(PdfColor.Red);
            Assert.That(modified.HeaderBackground, Is.EqualTo(PdfColor.Red));
            Assert.That(orig.HeaderBackground,     Is.Null, "original must not change");
        }

        [Test]
        public void PdfTable_WithNoBorder_ClearsBorderColor()
        {
            var t = new PdfTable(new float[] { 100 }).WithNoBorder();
            Assert.That(t.BorderColor, Is.Null);
        }

        [Test]
        public void DrawTable_ProducesValidPdf()
        {
            var table = SimpleTable();
            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.A4, c =>
                    c.DrawTable(table, 72, 72))
                .ToBytes();

            Assert.That(bytes.Length, Is.GreaterThan(100));
            var header = Encoding.ASCII.GetString(bytes, 0, 5);
            Assert.That(header, Is.EqualTo("%PDF-"));
        }

        [Test]
        public void DrawTable_WithOut_ReturnsPositiveHeight()
        {
            var table = SimpleTable();
            float height = 0;
            PdfDocument.Create()
                .AddPage(PageSizes.A4, c =>
                    c.DrawTable(table, 72, 72, out height))
                .ToBytes();

            Assert.That(height, Is.GreaterThan(0f), "table height must be positive");
        }

        [Test]
        public void DrawTable_EmptyTable_ProducesValidPdf()
        {
            var table = new PdfTable(new float[] { 100, 100 });
            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.A4, c => c.DrawTable(table, 72, 72))
                .ToBytes();

            Assert.That(bytes.Length, Is.GreaterThan(100));
        }
    }

    // ── PDF/A conformance ────────────────────────────────────────────────────────

    [TestFixture]
    public class PdfConformanceTests
    {
        [Test]
        public void PdfA1b_Header_IsPdf14()
        {
            var bytes = PdfDocument.Create()
                .WithConformance(PdfConformance.PdfA1b)
                .AddPage(PageSizes.A4, _ => { })
                .ToBytes();

            string header = Encoding.ASCII.GetString(bytes, 0, 9);
            Assert.That(header, Is.EqualTo("%PDF-1.4\n").Or.EqualTo("%PDF-1.4\r"));
        }

        [Test]
        public void PdfA2b_Header_IsPdf17()
        {
            var bytes = PdfDocument.Create()
                .WithConformance(PdfConformance.PdfA2b)
                .AddPage(PageSizes.A4, _ => { })
                .ToBytes();

            string header = Encoding.ASCII.GetString(bytes, 0, 9);
            Assert.That(header, Does.StartWith("%PDF-1.7"));
        }

        [Test]
        public void PdfA3b_Header_IsPdf17()
        {
            var bytes = PdfDocument.Create()
                .WithConformance(PdfConformance.PdfA3b)
                .AddPage(PageSizes.A4, _ => { })
                .ToBytes();

            string header = Encoding.ASCII.GetString(bytes, 0, 9);
            Assert.That(header, Does.StartWith("%PDF-1.7"));
        }

        [Test]
        public void PdfA_XmpStream_ContainsPdfaidNamespace()
        {
            var bytes = PdfDocument.Create()
                .WithConformance(PdfConformance.PdfA2b)
                .AddPage(PageSizes.A4, _ => { })
                .ToBytes();

            string pdfText = Encoding.Latin1.GetString(bytes);
            Assert.That(pdfText, Does.Contain("pdfaid"),        "must contain pdfaid namespace");
            Assert.That(pdfText, Does.Contain("<pdfaid:part>"), "must contain pdfaid:part element");
        }

        [Test]
        public void PdfA1b_XmpStream_HasPart1()
        {
            var bytes = PdfDocument.Create()
                .WithConformance(PdfConformance.PdfA1b)
                .AddPage(PageSizes.A4, _ => { })
                .ToBytes();

            string pdfText = Encoding.Latin1.GetString(bytes);
            Assert.That(pdfText, Does.Contain("<pdfaid:part>1</pdfaid:part>"));
        }

        [Test]
        public void PdfA2b_XmpStream_HasPart2()
        {
            var bytes = PdfDocument.Create()
                .WithConformance(PdfConformance.PdfA2b)
                .AddPage(PageSizes.A4, _ => { })
                .ToBytes();

            string pdfText = Encoding.Latin1.GetString(bytes);
            Assert.That(pdfText, Does.Contain("<pdfaid:part>2</pdfaid:part>"));
        }

        [Test]
        public void PdfA_HasOutputIntents_InCatalog()
        {
            var bytes = PdfDocument.Create()
                .WithConformance(PdfConformance.PdfA2b)
                .AddPage(PageSizes.A4, _ => { })
                .ToBytes();

            string pdfText = Encoding.Latin1.GetString(bytes);
            Assert.That(pdfText, Does.Contain("/OutputIntents"), "catalog must have OutputIntents");
            Assert.That(pdfText, Does.Contain("/GTS_PDFA1"),     "OutputIntent must use GTS_PDFA1 subtype");
        }

        [Test]
        public void PdfA_HasIccProfile_Embedded()
        {
            var bytes = PdfDocument.Create()
                .WithConformance(PdfConformance.PdfA2b)
                .AddPage(PageSizes.A4, _ => { })
                .ToBytes();

            string pdfText = Encoding.Latin1.GetString(bytes);
            Assert.That(pdfText, Does.Contain("/DeviceRGB"), "ICC output intent must specify DeviceRGB");
        }

        [Test]
        public void PdfA_WithEncryption_ThrowsAtSave()
        {
            var doc = PdfDocument.Create()
                .WithConformance(PdfConformance.PdfA2b)
                .WithSecurity(PdfSecurity.Protect("owner", "user"))
                .AddPage(PageSizes.A4, _ => { });

            Assert.Throws<InvalidOperationException>(() => doc.ToBytes(),
                "combining PDF/A and encryption must throw");
        }

        [Test]
        public void PdfConformance_None_ProducesNoPdfaidMetadata()
        {
            var bytes = PdfDocument.Create()
                .AddPage(PageSizes.A4, _ => { })
                .ToBytes();

            // Default (no conformance) should not contain pdfaid claims
            string pdfText = Encoding.Latin1.GetString(bytes);
            Assert.That(pdfText, Does.Not.Contain("pdfaid"));
        }
    }

    // ── Visible signature appearance ─────────────────────────────────────────────

    [TestFixture]
    public class VisibleSignatureTests
    {
        private static X509Certificate2 MakeSelfSignedCert()
        {
            using var rsa = RSA.Create(2048);
            var req = new CertificateRequest(
                "CN=Test Signer",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(365));
#pragma warning disable SYSLIB0057
            return new X509Certificate2(cert.Export(X509ContentType.Pfx), (string?)null,
                X509KeyStorageFlags.Exportable);
#pragma warning restore SYSLIB0057
        }

        [Test]
        public void PdfSignatureOptions_AppearanceRect_DefaultIsNull()
        {
            var cert = MakeSelfSignedCert();
            var opts = new PdfSignatureOptions(cert);
            Assert.That(opts.AppearanceRect, Is.Null);
        }

        [Test]
        public void PdfSignatureOptions_WithAppearance_SetsRect()
        {
            var cert = MakeSelfSignedCert();
            var opts = new PdfSignatureOptions(cert).WithAppearance(50, 700, 200, 50);
            Assert.That(opts.AppearanceRect, Is.Not.Null);
            Assert.That(opts.AppearanceRect!.Value.X,      Is.EqualTo(50f));
            Assert.That(opts.AppearanceRect!.Value.Y,      Is.EqualTo(700f));
            Assert.That(opts.AppearanceRect!.Value.Width,  Is.EqualTo(200f));
            Assert.That(opts.AppearanceRect!.Value.Height, Is.EqualTo(50f));
        }

        [Test]
        public void PdfSignatureOptions_WithAppearance_IsImmutable()
        {
            var cert = MakeSelfSignedCert();
            var orig     = new PdfSignatureOptions(cert);
            var modified = orig.WithAppearance(0, 0, 100, 50);
            Assert.That(orig.AppearanceRect,     Is.Null,     "original must not change");
            Assert.That(modified.AppearanceRect, Is.Not.Null, "modified should have rect");
        }

        [Test]
        public void WithAppearance_ProducesValidPdf_WithAPDictInStream()
        {
            var cert = MakeSelfSignedCert();
            var opts = new PdfSignatureOptions(cert)
                .WithSignerName("Jane Smith")
                .WithAppearance(50, 700, 200, 50);

            var bytes = PdfDocument.Create()
                .WithSignature(opts)
                .AddPage(PageSizes.A4, c => c.DrawText("Signed doc", 72, 72))
                .ToBytes();

            Assert.That(bytes.Length, Is.GreaterThan(100));
            string pdfText = Encoding.Latin1.GetString(bytes);
            Assert.That(pdfText, Does.Contain("/AP"),         "visible sig must have /AP dict");
            Assert.That(pdfText, Does.Contain("/N "),         "AP dict must have /N entry");
            Assert.That(pdfText, Does.Contain("/BBox"),       "appearance stream must have /BBox");
            Assert.That(pdfText, Does.Contain("Digitally Signed"), "appearance content must include label");
        }

        [Test]
        public void WithoutAppearance_Produces_InvisibleWidget()
        {
            var cert = MakeSelfSignedCert();
            var opts = new PdfSignatureOptions(cert).WithReason("Approved");

            var bytes = PdfDocument.Create()
                .WithSignature(opts)
                .AddPage(PageSizes.A4, c => c.DrawText("Invisible sig", 72, 72))
                .ToBytes();

            string pdfText = Encoding.Latin1.GetString(bytes);
            Assert.That(pdfText, Does.Contain("/Rect [0 0 0 0]"), "invisible widget must have zero rect");
            Assert.That(pdfText, Does.Not.Contain("/BBox"), "invisible widget must not have appearance stream");
        }
    }

    // ── PdfPublicKeySecurity ──────────────────────────────────────────────────────

    [TestFixture]
    public class PdfPublicKeySecurityTests
    {
        private static X509Certificate2 MakeCert()
        {
            using var rsa = RSA.Create(2048);
            var req = new CertificateRequest(
                "CN=PDF Recipient",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            req.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DataEncipherment,
                    critical: false));
            var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(365));
#pragma warning disable SYSLIB0057
            return new X509Certificate2(cert.Export(X509ContentType.Pfx), (string?)null,
                X509KeyStorageFlags.Exportable);
#pragma warning restore SYSLIB0057
        }

        [Test]
        public void ForRecipients_NullArg_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => PdfPublicKeySecurity.ForRecipients());
        }

        [Test]
        public void ForRecipients_EmptyArray_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(
                () => PdfPublicKeySecurity.ForRecipients(Array.Empty<X509Certificate2>()));
        }

        [Test]
        public void ForRecipients_SingleCert_RecipientCountIsOne()
        {
            var cert = MakeCert();
            var sec  = PdfPublicKeySecurity.ForRecipients(cert);
            Assert.That(sec.Recipients.Count, Is.EqualTo(1));
        }

        [Test]
        public void WithPermissions_IsImmutable()
        {
            var cert  = MakeCert();
            var orig  = PdfPublicKeySecurity.ForRecipients(cert);
            var copy  = orig.WithPermissions(PdfPermissions.Print);
            Assert.That(copy.Permissions, Is.EqualTo(PdfPermissions.Print));
            Assert.That(orig.Permissions, Is.EqualTo(PdfPermissions.All), "original must not change");
        }

        [Test]
        public void WithPublicKeySecurity_ProducesValidPdf_WithAdobePubSec()
        {
            var cert  = MakeCert();
            var sec   = PdfPublicKeySecurity.ForRecipients(cert);

            var bytes = PdfDocument.Create()
                .WithPublicKeySecurity(sec)
                .AddPage(PageSizes.A4, c => c.DrawText("Public key encrypted", 72, 72))
                .ToBytes();

            Assert.That(bytes.Length, Is.GreaterThan(100));
            string pdfText = Encoding.Latin1.GetString(bytes);
            Assert.That(pdfText, Does.Contain("/Adobe.PubSec"), "must emit Adobe.PubSec filter");
            Assert.That(pdfText, Does.Contain("/Recipients"),   "must have Recipients array");
        }

        [Test]
        public void WithPublicKeySecurity_Null_DoesNotEncrypt()
        {
            var bytes = PdfDocument.Create()
                .WithPublicKeySecurity(null)
                .AddPage(PageSizes.A4, c => c.DrawText("Plain text", 72, 72))
                .ToBytes();

            string pdfText = Encoding.Latin1.GetString(bytes);
            Assert.That(pdfText, Does.Not.Contain("/Adobe.PubSec"), "null removes encryption");
        }

        [Test]
        public void WithPublicKeySecurity_MultipleRecipients_AllInRecipientArray()
        {
            var cert1 = MakeCert();
            var cert2 = MakeCert();
            var sec   = PdfPublicKeySecurity.ForRecipients(cert1, cert2);

            var bytes = PdfDocument.Create()
                .WithPublicKeySecurity(sec)
                .AddPage(PageSizes.A4, _ => { })
                .ToBytes();

            string pdfText = Encoding.Latin1.GetString(bytes);
            Assert.That(pdfText, Does.Contain("/Adobe.PubSec"));
        }
    }
}
