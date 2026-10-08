// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Majorsilence.Pdf.Security;
using NUnit.Framework;

namespace Majorsilence.Pdf.Tests
{
    // Writes signed and encrypted+signed PDFs for an external validator (pyHanko, in CI: see
    // .github/scripts/validate_pdf_signatures.py). Each PDF gets a <name>.json beside it with what the
    // validator should find, and the signing certificate is written as signer.pem for it to trust.
    //
    // Runs only when MAJORSILENCE_PDF_VALIDATION_DIR names the output folder; otherwise it is skipped.
    // An in-process check cannot catch #363: our own reader and our own writer can agree on a mistake.
    [TestFixture]
    [Category("PdfValidationSamples")]
    public class PdfValidationSamples
    {
        private const string OutputVariable = "MAJORSILENCE_PDF_VALIDATION_DIR";

        private string _dir = null!;
        private X509Certificate2 _cert = null!;

        [OneTimeSetUp]
        public void Setup()
        {
            var dir = Environment.GetEnvironmentVariable(OutputVariable);
            if (string.IsNullOrEmpty(dir))
                Assert.Ignore($"Set {OutputVariable} to write PDF validation samples.");

            _dir = Directory.CreateDirectory(dir!).FullName;
            _cert = CreateSigningCert();

            File.WriteAllText(Path.Combine(_dir, "signer.pem"),
                "-----BEGIN CERTIFICATE-----\n" +
                Convert.ToBase64String(_cert.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks) +
                "\n-----END CERTIFICATE-----\n");
        }

        [OneTimeTearDown]
        public void TearDown() => _cert?.Dispose();

        private static readonly (string Name, bool Encrypt, PdfEncryptionVersion Version, bool Visible, bool Details)[] Cases =
        {
            ("sign-only",                   false, PdfEncryptionVersion.AES256, false, false),
            ("sign-only-details",           false, PdfEncryptionVersion.AES256, false, true),
            ("encrypt-aes256-sign",         true,  PdfEncryptionVersion.AES256, false, false),
            ("encrypt-aes256-sign-details", true,  PdfEncryptionVersion.AES256, false, true),
            ("encrypt-aes256-sign-visible", true,  PdfEncryptionVersion.AES256, true,  true),
            ("encrypt-aes128-sign-details", true,  PdfEncryptionVersion.AES128, false, true),
        };

        [TestCaseSource(nameof(Cases))]
        public void Write((string Name, bool Encrypt, PdfEncryptionVersion Version, bool Visible, bool Details) sample)
        {
            var sig = new PdfSignatureOptions(_cert);
            if (sample.Details)
                sig = sig.WithReason("Approved").WithSignerName("Alice").WithLocation("Toronto");
            if (sample.Visible)
                sig = sig.WithAppearance(72, 600, 180, 40);

            var doc = PdfDocument.Create()
                .WithTitle(sample.Name)
                .AddPage(PageSizes.A4, c => c.DrawText("PDF validation sample: " + sample.Name, 72, 100,
                    TextStyle.Default.WithSize(14)));

            if (sample.Encrypt)
                doc.WithSecurity(PdfSecurity.Protect(userPassword: string.Empty, ownerPassword: "owner")
                    .WithPermissions(PdfPermissions.None)
                    .WithEncryptionVersion(sample.Version));

            doc.WithSignature(sig);

            File.WriteAllBytes(Path.Combine(_dir, sample.Name + ".pdf"), doc.ToBytes());
            File.WriteAllText(Path.Combine(_dir, sample.Name + ".json"), JsonSerializer.Serialize(new {
                encrypted = sample.Encrypt,
                field_name = "Signature",
                reason = sample.Details ? "Approved" : null,
                name = sample.Details ? "Alice" : null,
                location = sample.Details ? "Toronto" : null,
            }), new UTF8Encoding(false));
        }

        // pyHanko's default signer constraints ask for non-repudiation as well as digital signature.
        private static X509Certificate2 CreateSigningCert()
        {
            using var rsa = RSA.Create(2048);
            var req = new CertificateRequest("CN=Majorsilence PDF validation sample", rsa,
                HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
            req.CertificateExtensions.Add(new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation, true));

            using var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(30));

#if NET5_0_OR_GREATER
            return new X509Certificate2(cert.Export(X509ContentType.Pkcs12));
#else
            return new X509Certificate2(cert.Export(X509ContentType.Pkcs12), (string?)null, X509KeyStorageFlags.Exportable);
#endif
        }
    }
}
