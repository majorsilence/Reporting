// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System.IO;

namespace Majorsilence.Pdf.Internal
{
    // Contract between PdfSerializer and a digital-signature implementation
    // supplied by an optional companion package (e.g. Majorsilence.Pdf.Security).
    // Exposed to friend assemblies via [assembly: InternalsVisibleTo(...)].
    internal interface ISignatureHandler
    {
        // Build the body bytes for the signature dictionary object (placeholder state).
        // /ByteRange and /Contents contain fixed-width zeros that are filled in later.
        byte[] BuildPlaceholder();

        // Called after the complete PDF (including xref/trailer) has been written to ms.
        // Seeks into ms, writes the real /ByteRange values, creates and writes the
        // PKCS#7 signature into /Contents.
        //
        // sigPlaceholderBytes  – the byte[] previously returned by BuildPlaceholder()
        // sigBodyOffset        – position in ms where those bytes begin
        void Fixup(MemoryStream ms, byte[] sigPlaceholderBytes, long sigBodyOffset);

        // Optional visible signature appearance.
        // When null  → invisible widget (/Rect [0 0 0 0], no /AP).
        // When set   → visible widget with /Rect and an appearance Form XObject.
        //   X, Y     – top-left origin coordinates (PdfCanvas convention)
        //   Width, Height – size of the appearance box in points
        //   SignerName – optional text to show inside the appearance box
        (float X, float Y, float Width, float Height, string? SignerName)? VisibleAppearance { get; }
    }
}
