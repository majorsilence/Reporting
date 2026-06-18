// Copyright (C) 2025 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography.X509Certificates;

namespace Majorsilence.Pdf.Security
{
    /// <summary>
    /// Options for an invisible PKCS#7 detached digital signature embedded in the PDF.
    /// </summary>
    /// <example>
    /// <code>
    /// using Majorsilence.Pdf.Security;
    ///
    /// var cert = new X509Certificate2("signer.p12", "password",
    ///     X509KeyStorageFlags.Exportable);
    ///
    /// PdfDocument.Create()
    ///     .WithSignature(new PdfSignatureOptions(cert)
    ///                        .WithReason("Approved")
    ///                        .WithLocation("Toronto"))
    ///     .AddPage(PageSizes.A4, c => c.DrawText("Signed!", 72, 100))
    ///     .Save("signed.pdf");
    /// </code>
    /// </example>
    public sealed class PdfSignatureOptions
    {
        /// <summary>Certificate (with private key) used to sign the document.</summary>
        public X509Certificate2 Certificate { get; }
        /// <summary>Reason for signing shown in viewer signature panel.</summary>
        public string? Reason { get; private set; }
        /// <summary>Signer name shown in viewer signature panel.</summary>
        public string? SignerName { get; private set; }
        /// <summary>Signing location shown in viewer signature panel.</summary>
        public string? Location { get; private set; }

        public PdfSignatureOptions(X509Certificate2 certificate)
        {
            Certificate = certificate
                ?? throw new System.ArgumentNullException(nameof(certificate));
        }

        public PdfSignatureOptions WithReason(string reason)     { Reason     = reason;   return this; }
        public PdfSignatureOptions WithSignerName(string name)   { SignerName = name;     return this; }
        public PdfSignatureOptions WithLocation(string location) { Location   = location; return this; }
    }
}
