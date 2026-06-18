// Copyright (C) 2025 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

namespace Majorsilence.Pdf
{
    /// <summary>PDF specification version written to the output file.</summary>
    public enum PdfVersion
    {
        /// <summary>PDF 1.4 — widest reader compatibility. Default.</summary>
        Pdf14 = 14,

        /// <summary>
        /// PDF 2.0 (ISO 32000-2).
        /// Differences from 1.4: <c>%PDF-2.0</c> header, an XMP metadata stream
        /// attached to the document catalog, and a compressed cross-reference stream
        /// instead of a traditional xref table.
        /// </summary>
        Pdf20 = 20,
    }
}
