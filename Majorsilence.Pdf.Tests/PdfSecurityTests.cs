// Copyright (C) 2025 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Majorsilence.Pdf.Security;
using NUnit.Framework;

namespace Majorsilence.Pdf.Tests
{
    [TestFixture]
    public class PdfSecurityTests
    {
        // ── helpers ───────────────────────────────────────────────────────────

        private static byte[] MakePdf(Action<PdfDocument> configure)
        {
            var doc = PdfDocument.Create()
                .WithTitle("Security Test")
                .AddPage(PageSizes.A4, c =>
                    c.DrawText("Hello, secured world!", 72, 100,
                        TextStyle.Default.WithSize(14)));

            configure(doc);
            return doc.ToBytes();
        }

        // ── PdfPermissions enum ───────────────────────────────────────────────

        [Test]
        public void PdfPermissions_All_HasAllBitsSet()
        {
            var all = PdfPermissions.All;
            Assert.That(all.HasFlag(PdfPermissions.Print),            Is.True);
            Assert.That(all.HasFlag(PdfPermissions.ModifyContent),    Is.True);
            Assert.That(all.HasFlag(PdfPermissions.CopyText),         Is.True);
            Assert.That(all.HasFlag(PdfPermissions.AddAnnotations),   Is.True);
            Assert.That(all.HasFlag(PdfPermissions.FillForms),        Is.True);
            Assert.That(all.HasFlag(PdfPermissions.ExtractText),      Is.True);
            Assert.That(all.HasFlag(PdfPermissions.Assemble),         Is.True);
            Assert.That(all.HasFlag(PdfPermissions.PrintHighQuality), Is.True);
        }

        [Test]
        public void PdfPermissions_None_HasNoBitsSet()
        {
            Assert.That(PdfPermissions.None, Is.EqualTo((PdfPermissions)0));
        }

        // ── PdfSecurity API ───────────────────────────────────────────────────

        [Test]
        public void PdfSecurity_Protect_DefaultsToAllPermissions()
        {
            var sec = PdfSecurity.Protect("user123");
            Assert.That(sec.UserPassword,  Is.EqualTo("user123"));
            Assert.That(sec.OwnerPassword, Is.EqualTo("user123")); // owner defaults to user
            Assert.That(sec.Permissions,   Is.EqualTo(PdfPermissions.All));
        }

        [Test]
        public void PdfSecurity_Protect_SeparateOwnerPassword()
        {
            var sec = PdfSecurity.Protect("user", ownerPassword: "owner");
            Assert.That(sec.UserPassword,  Is.EqualTo("user"));
            Assert.That(sec.OwnerPassword, Is.EqualTo("owner"));
        }

        [Test]
        public void PdfSecurity_WithPermissions_ReturnsNewInstance()
        {
            var original = PdfSecurity.Protect("pw");
            var restricted = original.WithPermissions(PdfPermissions.Print);
            Assert.That(restricted.Permissions, Is.EqualTo(PdfPermissions.Print));
            Assert.That(original.Permissions,   Is.EqualTo(PdfPermissions.All)); // unchanged
        }

        [Test]
        public void PdfSecurity_WithOwnerPassword_ReturnsNewInstance()
        {
            var sec = PdfSecurity.Protect("user").WithOwnerPassword("owner");
            Assert.That(sec.OwnerPassword, Is.EqualTo("owner"));
            Assert.That(sec.UserPassword,  Is.EqualTo("user"));
        }

        // ── encrypted PDF structure ───────────────────────────────────────────

        [Test]
        public void WithSecurity_Pdf14_ContainsEncryptDict()
        {
            byte[] pdf = MakePdf(doc =>
                doc.WithSecurity(PdfSecurity.Protect("test")));

            string text = Encoding.Latin1.GetString(pdf);
            Assert.That(text, Does.Contain("/Encrypt"));
            Assert.That(text, Does.Contain("/Standard"));
            Assert.That(text, Does.Contain("/StmF"));
            Assert.That(text, Does.Contain("/AESV2"));
        }

        [Test]
        public void WithSecurity_Pdf14_HeaderCorrect()
        {
            byte[] pdf = MakePdf(doc =>
                doc.WithSecurity(PdfSecurity.Protect("pw")));
            string header = Encoding.Latin1.GetString(pdf, 0, 10);
            Assert.That(header, Does.StartWith("%PDF-1.4"));
        }

        [Test]
        public void WithSecurity_Pdf20_ContainsEncryptDictAndXrefStream()
        {
            byte[] pdf = MakePdf(doc =>
                doc.WithVersion(PdfVersion.Pdf20)
                   .WithSecurity(PdfSecurity.Protect("test")));

            string text = Encoding.Latin1.GetString(pdf);
            Assert.That(text, Does.Contain("/Encrypt"));
            Assert.That(text, Does.Contain("/XRef"));  // xref stream
            Assert.That(text, Does.Contain("%PDF-2.0"));
        }

        [Test]
        public void WithSecurity_TrailerContainsIdArray()
        {
            byte[] pdf = MakePdf(doc =>
                doc.WithSecurity(PdfSecurity.Protect("")));
            string text = Encoding.Latin1.GetString(pdf);
            Assert.That(text, Does.Contain("/ID [<"));
        }

        [Test]
        public void WithSecurity_NoPassword_IdArrayStillPresent()
        {
            // No-password encryption (empty user password)
            byte[] pdf = MakePdf(doc =>
                doc.WithSecurity(PdfSecurity.Protect()));
            string text = Encoding.Latin1.GetString(pdf);
            Assert.That(text, Does.Contain("/ID [<"));
        }

        [Test]
        public void WithSecurity_EncryptObjContainsOUEntries()
        {
            byte[] pdf = MakePdf(doc =>
                doc.WithSecurity(PdfSecurity.Protect("hello", "world")));
            string text = Encoding.Latin1.GetString(pdf);
            // /O and /U entries are 32-byte hex values (64 hex chars each)
            Assert.That(text, Does.Contain("/O <"));
            Assert.That(text, Does.Contain("/U <"));
            Assert.That(text, Does.Contain("/P "));
        }

        [Test]
        public void WithSecurity_PermissionsNone_PValueNegative()
        {
            // With no permissions allowed, the /P value should be negative (high bits set)
            byte[] pdf = MakePdf(doc =>
                doc.WithSecurity(PdfSecurity.Protect("pw").WithPermissions(PdfPermissions.None)));
            string text = Encoding.Latin1.GetString(pdf);
            // The P value for no permissions is 0xFFFFF0C0 = -3904 signed
            Assert.That(text, Does.Contain("/P -"));
        }

        [Test]
        public void WithSecurity_StreamDataIsEncrypted_NotPlainText()
        {
            // A document with a known text string should NOT contain that text in the
            // PDF byte stream when encrypted (the content stream is AES-encrypted).
            byte[] pdf = MakePdf(doc =>
                doc.WithSecurity(PdfSecurity.Protect("key")));

            // The drawing commands won't appear as plain text in an encrypted stream.
            // We look for a PDF operator that would normally appear in the content stream.
            string text = Encoding.Latin1.GetString(pdf);
            // "BT" (Begin Text) in a content stream — should not appear as plain ASCII
            // when encrypted.  Note: it may still appear in dict keys, so we check
            // that "BT\n" (the operator with newline) from the drawing stream is absent.
            // This is a heuristic: the content stream is encrypted, so raw drawing ops
            // should not be visible as ASCII.
            Assert.That(text, Does.Not.Contain("BT\n"));
        }

        [Test]
        public void WithoutSecurity_StreamDataContainsDrawingOps()
        {
            // Control: unencrypted PDF does contain drawing operators in the compressed stream.
            // We can't see them directly (FlateDecode compressed), but the PDF structure is
            // still readable as ASCII for dicts and xref.
            byte[] pdf = MakePdf(_ => { });
            string text = Encoding.Latin1.GetString(pdf);
            // Unencrypted PDF should have readable dict entries
            Assert.That(text, Does.Contain("/Type /Page"));
        }

        [Test]
        public void WithSecurity_MultiplePages_ProducesValidStructure()
        {
            var doc = PdfDocument.Create()
                .WithSecurity(PdfSecurity.Protect("multi"))
                .AddPage(PageSizes.A4, c => c.DrawText("Page 1", 72, 100, TextStyle.Default))
                .AddPage(PageSizes.A4, c => c.DrawText("Page 2", 72, 100, TextStyle.Default))
                .AddPage(PageSizes.A4, c => c.DrawText("Page 3", 72, 100, TextStyle.Default));

            byte[] pdf = doc.ToBytes();
            Assert.That(pdf.Length, Is.GreaterThan(0));
            string text = Encoding.Latin1.GetString(pdf);
            Assert.That(text, Does.Contain("/Encrypt"));
            Assert.That(text, Does.Contain("/Count 3"));
        }

        [Test]
        public void WithSecurity_Null_RemovesSecurity()
        {
            byte[] pdf = MakePdf(doc =>
                doc.WithSecurity(PdfSecurity.Protect("pw"))
                   .WithSecurity(null));
            string text = Encoding.Latin1.GetString(pdf);
            Assert.That(text, Does.Not.Contain("/Encrypt"));
        }

        [Test]
        public void WithSecurity_ThenWithVersion_BothApplied()
        {
            byte[] pdf = MakePdf(doc =>
                doc.WithVersion(PdfVersion.Pdf20)
                   .WithSecurity(PdfSecurity.Protect("v20")));
            string text = Encoding.Latin1.GetString(pdf);
            Assert.That(text, Does.Contain("%PDF-2.0"));
            Assert.That(text, Does.Contain("/Encrypt"));
        }

        // ── P value bit verification ──────────────────────────────────────────

        [Test]
        public void PValue_PrintOnly_ContainsCorrectPEntry()
        {
            byte[] pdf = MakePdf(doc =>
                doc.WithSecurity(
                    PdfSecurity.Protect("pw")
                        .WithPermissions(PdfPermissions.Print)));
            // P = 0xFFFFF0C0 | 4 = 0xFFFFF0C4 = -3900 signed
            string text = Encoding.Latin1.GetString(pdf);
            Assert.That(text, Does.Contain("/P -3900"));
        }

        // ── signature placeholder ─────────────────────────────────────────────

        [Test]
        public void WithSignature_ProducesAcroFormAndSigField()
        {
            // Use a self-signed test certificate.
            using var cert = CreateTestCert();
            var sig = new PdfSignatureOptions(cert)
                .WithReason("Unit test")
                .WithSignerName("Tester");

            byte[] pdf = MakePdf(doc => doc.WithSignature(sig));

            string text = Encoding.Latin1.GetString(pdf);
            Assert.That(text, Does.Contain("/AcroForm"));
            Assert.That(text, Does.Contain("/Sig"));
            Assert.That(text, Does.Contain("/ByteRange"));
            Assert.That(text, Does.Contain("adbe.pkcs7.detached"));
        }

        [Test]
        public void WithSignature_ByteRangeIsFilledIn()
        {
            using var cert = CreateTestCert();
            byte[] pdf = MakePdf(doc =>
                doc.WithSignature(new PdfSignatureOptions(cert)));

            string text = Encoding.Latin1.GetString(pdf);
            // After fixup, ByteRange should not be all zeros — at least one non-zero decimal
            // The format is "/ByteRange [AAAAAAAAAA BBBBBBBBBB CCCCCCCCCC DDDDDDDDDD]"
            int idx = text.IndexOf("/ByteRange [", StringComparison.Ordinal);
            Assert.That(idx, Is.GreaterThan(0));
            string br = text.Substring(idx, 60);
            // The second number (offset of /Contents) should be non-zero
            Assert.That(br, Does.Not.Contain("[0000000000 0000000000"));
        }

        [Test]
        public void WithSignature_ContentsIsNotAllZeros()
        {
            using var cert = CreateTestCert();
            byte[] pdf = MakePdf(doc =>
                doc.WithSignature(new PdfSignatureOptions(cert)));

            string text = Encoding.Latin1.GetString(pdf);
            int contIdx = text.IndexOf("/Contents <", StringComparison.Ordinal);
            Assert.That(contIdx, Is.GreaterThan(0));
            // Skip past "/Contents <"
            string contHex = text.Substring(contIdx + "/Contents <".Length, 20);
            // Should not be 00000000... (all zeros = unsigned = no signature written)
            Assert.That(contHex, Is.Not.EqualTo("00000000000000000000"));
        }

        [Test]
        public void WithSignature_WithReasonAndLocation_AppearsInDict()
        {
            using var cert = CreateTestCert();
            var sig = new PdfSignatureOptions(cert)
                .WithReason("Approved")
                .WithLocation("Toronto");

            byte[] pdf = MakePdf(doc => doc.WithSignature(sig));
            string text = Encoding.Latin1.GetString(pdf);
            Assert.That(text, Does.Contain("/Reason (Approved)"));
            Assert.That(text, Does.Contain("/Location (Toronto)"));
        }

        // ── self-signed certificate factory ──────────────────────────────────

        private static X509Certificate2 CreateTestCert()
        {
            using var rsa = RSA.Create(2048);
            var req = new CertificateRequest(
                "CN=PdfSecurityTest",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            req.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, true));
            req.CertificateExtensions.Add(
                new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));

            var cert = req.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddMinutes(-1),
                DateTimeOffset.UtcNow.AddYears(1));

#if NET5_0_OR_GREATER
            // Export/import required on some platforms to ensure private key is accessible
            return new X509Certificate2(cert.Export(X509ContentType.Pkcs12));
#else
            return new X509Certificate2(cert.Export(X509ContentType.Pkcs12), (string?)null,
                X509KeyStorageFlags.Exportable);
#endif
        }
    }
}
