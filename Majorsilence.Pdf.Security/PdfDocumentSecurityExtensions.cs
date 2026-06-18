// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System.IO;
using Majorsilence.Pdf;
using Majorsilence.Pdf.Internal;          // IStreamEncryptor, ISignatureHandler (friend)
using Majorsilence.Pdf.Security.Internal;

namespace Majorsilence.Pdf.Security
{
    /// <summary>
    /// Extension methods that add password protection and digital-signature support
    /// to <see cref="PdfDocument"/>.  Requires a reference to Majorsilence.Pdf.Security.
    /// </summary>
    public static class PdfDocumentSecurityExtensions
    {
        /// <summary>
        /// Encrypt the document with AES-128 (Standard Security Handler Revision 4).
        /// Pass <c>null</c> to remove any previously configured encryption.
        /// </summary>
        public static PdfDocument WithSecurity(this PdfDocument doc, PdfSecurity? security)
        {
            doc.SetStreamEncryptor(security != null ? new EncryptionProvider(security) : null);
            return doc;
        }

        /// <summary>
        /// Embed an invisible PKCS#7 detached digital signature.
        /// Pass <c>null</c> to remove a previously configured signature.
        /// </summary>
        public static PdfDocument WithSignature(this PdfDocument doc, PdfSignatureOptions? opts)
        {
            doc.SetSignatureHandler(opts != null ? new SignatureHandlerAdapter(opts) : null);
            return doc;
        }

        // ── bridge implementations ────────────────────────────────────────────

        // Adapts PdfEncryption to IStreamEncryptor.
        private sealed class EncryptionProvider : IStreamEncryptor
        {
            private readonly PdfSecurity _security;
            private PdfEncryption?       _enc;

            internal EncryptionProvider(PdfSecurity security) { _security = security; }

            public void Initialize(byte[] fileId) =>
                _enc = PdfEncryption.Create(_security, fileId);

            public byte[] EncryptStream(int objNum, int genNum, byte[] data) =>
                _enc!.EncryptStream(objNum, genNum, data);

            public string EncryptPdfString(int objNum, int genNum, byte[] rawBytes) =>
                _enc!.EncryptPdfString(objNum, genNum, rawBytes);

            public byte[] BuildEncryptDict() =>
                _enc!.BuildEncryptDict();
        }

        // Adapts PdfSigner to ISignatureHandler.
        private sealed class SignatureHandlerAdapter : ISignatureHandler
        {
            private readonly PdfSignatureOptions _opts;

            internal SignatureHandlerAdapter(PdfSignatureOptions opts) { _opts = opts; }

            public byte[] BuildPlaceholder() =>
                PdfSigner.BuildPlaceholder(_opts);

            public void Fixup(MemoryStream ms, byte[] placeholderBytes, long bodyOffset) =>
                PdfSigner.Fixup(ms, placeholderBytes, bodyOffset, _opts);
        }
    }
}
