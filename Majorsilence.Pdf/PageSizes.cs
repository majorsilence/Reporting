// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>


namespace Majorsilence.Pdf
{
    /// <summary>Page dimensions in PDF points (1 pt = 1/72 inch).</summary>
    public readonly struct PdfPageSize
    {
        /// <summary>Width in points.</summary>
        public float Width { get; }

        /// <summary>Height in points.</summary>
        public float Height { get; }

        public PdfPageSize(float width, float height) { Width = width; Height = height; }

        /// <summary>Returns the same size rotated 90° (portrait ↔ landscape).</summary>
        public PdfPageSize Landscape() => new PdfPageSize(Height, Width);

        /// <summary>Returns the same size in portrait orientation (taller than wide).</summary>
        public PdfPageSize Portrait() => Width <= Height ? this : new PdfPageSize(Height, Width);

        public override string ToString() => $"{Width}×{Height} pt";
    }

    /// <summary>
    /// Standard ISO/ANSI page sizes as <see cref="PdfPageSize"/> values.
    /// All sizes are in PDF points (1 pt = 1/72 inch).
    /// </summary>
    public static class PageSizes
    {
        // ── ISO 216 A-series ─────────────────────────────────────────────────
        public static PdfPageSize A0 { get; } = new PdfPageSize(2383.94f, 3370.39f);
        public static PdfPageSize A1 { get; } = new PdfPageSize(1683.78f, 2383.94f);
        public static PdfPageSize A2 { get; } = new PdfPageSize(1190.55f, 1683.78f);
        public static PdfPageSize A3 { get; } = new PdfPageSize( 841.89f, 1190.55f);
        public static PdfPageSize A4 { get; } = new PdfPageSize( 595.28f,  841.89f);
        public static PdfPageSize A5 { get; } = new PdfPageSize( 419.53f,  595.28f);
        public static PdfPageSize A6 { get; } = new PdfPageSize( 297.64f,  419.53f);

        // ── ISO 216 B-series ─────────────────────────────────────────────────
        public static PdfPageSize B4 { get; } = new PdfPageSize( 708.66f, 1000.63f);
        public static PdfPageSize B5 { get; } = new PdfPageSize( 498.90f,  708.66f);

        // ── North American ────────────────────────────────────────────────────
        public static PdfPageSize Letter  { get; } = new PdfPageSize(612f, 792f);
        public static PdfPageSize Legal   { get; } = new PdfPageSize(612f, 1008f);
        public static PdfPageSize Tabloid { get; } = new PdfPageSize(792f, 1224f);
        public static PdfPageSize Executive { get; } = new PdfPageSize(522f, 756f);
    }
}
