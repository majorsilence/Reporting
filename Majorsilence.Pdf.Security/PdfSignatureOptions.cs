// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System.Security.Cryptography.X509Certificates;

namespace Majorsilence.Pdf.Security
{
    /// <summary>
    /// Options for an invisible PKCS#7 detached digital signature embedded in the PDF.
    /// All <c>With*</c> methods return a new instance (immutable builder pattern).
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
    ///                        .WithLocation("Toronto")
    ///                        .WithTimestampAuthority("http://timestamp.digicert.com"))
    ///     .AddPage(PageSizes.A4, c => c.DrawText("Signed!", 72, 100))
    ///     .Save("signed.pdf");
    /// </code>
    /// </example>
    public sealed class PdfSignatureOptions
    {
        /// <summary>Certificate (with private key) used to sign the document.</summary>
        public X509Certificate2 Certificate { get; }
        /// <summary>Reason for signing shown in viewer signature panel.</summary>
        public string? Reason { get; }
        /// <summary>Signer name shown in viewer signature panel.</summary>
        public string? SignerName { get; }
        /// <summary>Signing location shown in viewer signature panel.</summary>
        public string? Location { get; }
        /// <summary>
        /// RFC 3161 timestamp authority URL.  When set, an authenticated timestamp
        /// is embedded in the signature so it remains verifiable after the signing
        /// certificate expires.  Null (default) skips timestamping.
        /// </summary>
        public string? TimestampAuthorityUrl { get; }

        public PdfSignatureOptions(X509Certificate2 certificate)
            : this(certificate, null, null, null, null) { }

        private PdfSignatureOptions(
            X509Certificate2 certificate,
            string? reason,
            string? signerName,
            string? location,
            string? timestampAuthorityUrl)
        {
            Certificate          = certificate ?? throw new System.ArgumentNullException(nameof(certificate));
            Reason               = reason;
            SignerName           = signerName;
            Location             = location;
            TimestampAuthorityUrl = timestampAuthorityUrl;
        }

        /// <summary>Return a copy with the given signing reason.</summary>
        public PdfSignatureOptions WithReason(string reason)
            => new PdfSignatureOptions(Certificate, reason, SignerName, Location, TimestampAuthorityUrl);

        /// <summary>Return a copy with the given signer name.</summary>
        public PdfSignatureOptions WithSignerName(string name)
            => new PdfSignatureOptions(Certificate, Reason, name, Location, TimestampAuthorityUrl);

        /// <summary>Return a copy with the given signing location.</summary>
        public PdfSignatureOptions WithLocation(string location)
            => new PdfSignatureOptions(Certificate, Reason, SignerName, location, TimestampAuthorityUrl);

        /// <summary>
        /// Return a copy that will request an RFC 3161 timestamp from <paramref name="url"/>
        /// and embed it in the signature as an <c>id-aa-signatureTimeStampToken</c>
        /// unsigned attribute.
        /// </summary>
        public PdfSignatureOptions WithTimestampAuthority(string url)
            => new PdfSignatureOptions(Certificate, Reason, SignerName, Location, url);
    }
}
