// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

namespace Majorsilence.Pdf.Internal
{
    // Contract between PdfSerializer and an encryption implementation supplied
    // by an optional companion package (e.g. Majorsilence.Pdf.Security).
    // Exposed to friend assemblies via [assembly: InternalsVisibleTo(...)].
    internal interface IStreamEncryptor
    {
        // Called once at the start of Write(), before any object is serialized.
        // fileId is the 16-byte document identifier used in key derivation and /ID.
        void Initialize(byte[] fileId);

        // Encrypt compressed stream bytes for the given PDF object.
        // Implementations prepend a random IV so each call produces unique ciphertext.
        byte[] EncryptStream(int objNum, int genNum, byte[] data);

        // Encrypt a PDF string's raw bytes and return a hex literal <...>.
        string EncryptPdfString(int objNum, int genNum, byte[] rawStringBytes);

        // Build and return the body bytes for the /Encrypt dictionary object.
        // Called after Initialize().
        byte[] BuildEncryptDict();
    }
}
