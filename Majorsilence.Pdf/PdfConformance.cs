// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

namespace Majorsilence.Pdf
{
    /// <summary>
    /// PDF/A conformance level.  Setting a conformance level on a <see cref="PdfDocument"/>
    /// causes the serializer to embed the required XMP metadata, an sRGB ICC output intent,
    /// and to enforce that only embedded (TrueType) fonts are used.
    /// </summary>
    public enum PdfConformance
    {
        /// <summary>No PDF/A conformance (default).</summary>
        None = 0,

        /// <summary>PDF/A-1b — based on PDF 1.4; Level B (basic) visual reproducibility.
        /// No encryption, no transparency, all fonts must be embedded.</summary>
        PdfA1b = 1,

        /// <summary>PDF/A-2b — based on PDF 1.7; Level B visual reproducibility.
        /// Allows transparency and JPEG2000 images; all fonts must be embedded.</summary>
        PdfA2b = 2,

        /// <summary>PDF/A-3b — based on PDF 1.7; Level B with support for embedded files.
        /// Same constraints as PDF/A-2b plus arbitrary file attachments.</summary>
        PdfA3b = 3,
    }
}
