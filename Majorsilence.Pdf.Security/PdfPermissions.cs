// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;

namespace Majorsilence.Pdf.Security
{
    /// <summary>
    /// Permission flags stored in the PDF encryption dictionary (bits 3-12).
    /// Combine with | to allow multiple operations.
    /// </summary>
    [Flags]
    public enum PdfPermissions
    {
        None             = 0,
        /// <summary>Allow printing (low-quality if <see cref="PrintHighQuality"/> is not also set).</summary>
        Print            = 1 << 2,   // bit 3
        /// <summary>Allow modifying document content.</summary>
        ModifyContent    = 1 << 3,   // bit 4
        /// <summary>Allow copying text and graphics.</summary>
        CopyText         = 1 << 4,   // bit 5
        /// <summary>Allow adding or modifying annotations and form fields.</summary>
        AddAnnotations   = 1 << 5,   // bit 6
        /// <summary>Allow filling in existing interactive form fields (PDF 1.3+).</summary>
        FillForms        = 1 << 8,   // bit 9
        /// <summary>Allow extracting text for accessibility (PDF 1.3+).</summary>
        ExtractText      = 1 << 9,   // bit 10
        /// <summary>Allow assembling the document (PDF 1.3+).</summary>
        Assemble         = 1 << 10,  // bit 11
        /// <summary>Allow high-quality printing (PDF 1.3+).</summary>
        PrintHighQuality = 1 << 11,  // bit 12
        /// <summary>All permissions granted.</summary>
        All = Print | ModifyContent | CopyText | AddAnnotations |
              FillForms | ExtractText | Assemble | PrintHighQuality,
    }
}
