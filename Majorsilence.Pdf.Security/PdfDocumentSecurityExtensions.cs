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
    public static partial class PdfDocumentSecurityExtensions
    {
        /// <summary>
        /// Encrypt the document.  Defaults to AES-256 (PDF 2.0, Revision 6) and
        /// automatically upgrades the document version to <see cref="PdfVersion.Pdf20"/>
        /// so the output is spec-conformant; use
        /// <see cref="PdfSecurity.WithEncryptionVersion"/> with
        /// <see cref="PdfEncryptionVersion.AES128"/> to select AES-128 (Revision 4)
        /// for broader viewer compatibility without changing the PDF version.
        /// Pass <c>null</c> to remove any previously configured encryption.
        /// </summary>
        public static PdfDocument WithSecurity(this PdfDocument doc, PdfSecurity? security)
        {
            if (security?.EncryptionVersion == PdfEncryptionVersion.AES256)
                doc.WithVersion(PdfVersion.Pdf20);
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

        private sealed class EncryptionProvider : IStreamEncryptor
        {
            private readonly PdfSecurity _security;
            private PdfEncryption?   _enc4;
            private PdfEncryptionR6? _enc6;

            internal EncryptionProvider(PdfSecurity security) { _security = security; }

            public void Initialize(byte[] fileId)
            {
                if (_security.EncryptionVersion == PdfEncryptionVersion.AES256)
                    _enc6 = PdfEncryptionR6.Create(_security);
                else
                    _enc4 = PdfEncryption.Create(_security, fileId);
            }

            public byte[] EncryptStream(int objNum, int genNum, byte[] data) =>
                _enc6 != null ? _enc6.EncryptStream(objNum, genNum, data)
                              : _enc4!.EncryptStream(objNum, genNum, data);

            public string EncryptPdfString(int objNum, int genNum, byte[] rawBytes) =>
                _enc6 != null ? _enc6.EncryptPdfString(objNum, genNum, rawBytes)
                              : _enc4!.EncryptPdfString(objNum, genNum, rawBytes);

            public byte[] BuildEncryptDict() =>
                _enc6 != null ? _enc6.BuildEncryptDict()
                              : _enc4!.BuildEncryptDict();
        }

        private sealed class SignatureHandlerAdapter : ISignatureHandler
        {
            private readonly PdfSignatureOptions _opts;

            internal SignatureHandlerAdapter(PdfSignatureOptions opts) { _opts = opts; }

            public byte[] BuildPlaceholder() =>
                PdfSigner.BuildPlaceholder(_opts);

            public void Fixup(MemoryStream ms, byte[] placeholderBytes, long bodyOffset) =>
                PdfSigner.Fixup(ms, placeholderBytes, bodyOffset, _opts);

            public (float X, float Y, float Width, float Height, string? SignerName)? VisibleAppearance =>
                _opts.AppearanceRect.HasValue
                    ? (_opts.AppearanceRect.Value.X, _opts.AppearanceRect.Value.Y,
                       _opts.AppearanceRect.Value.Width, _opts.AppearanceRect.Value.Height,
                       _opts.SignerName)
                    : ((float, float, float, float, string?)?)null;
        }
    }
}
