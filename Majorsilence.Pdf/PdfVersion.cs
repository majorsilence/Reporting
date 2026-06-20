// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

namespace Majorsilence.Pdf
{
    /// <summary>PDF specification version written to the output file.</summary>
    public enum PdfVersion
    {
        /// <summary>PDF 1.4 — widest reader compatibility. Default.</summary>
        Pdf14 = 14,

        /// <summary>PDF 1.7 (ISO 32000-1) — required for PDF/A-2b and PDF/A-3b conformance.</summary>
        Pdf17 = 17,

        /// <summary>
        /// PDF 2.0 (ISO 32000-2).
        /// Differences from 1.4: <c>%PDF-2.0</c> header, an XMP metadata stream
        /// attached to the document catalog, and a compressed cross-reference stream
        /// instead of a traditional xref table.
        /// </summary>
        Pdf20 = 20,
    }
}
