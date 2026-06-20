// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

namespace Majorsilence.Pdf.Security
{
    /// <summary>
    /// PDF Standard Security Handler version and cipher strength.
    /// </summary>
    public enum PdfEncryptionVersion
    {
        /// <summary>
        /// V=4, R=4 — AES-128-CBC (PDF 1.5+).
        /// Supported by virtually all PDF readers.
        /// </summary>
        AES128 = 4,

        /// <summary>
        /// V=5, R=6 — AES-256-CBC (PDF 2.0, ISO 32000-2:2020).
        /// Uses Algorithm 2.A key derivation (iterative SHA-256/384/512 + AES-128).
        /// Pair with <c>.WithVersion(PdfVersion.Pdf20)</c> for a fully spec-conformant output.
        /// Default.
        /// </summary>
        AES256 = 6,
    }
}
