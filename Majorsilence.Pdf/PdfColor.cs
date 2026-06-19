// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;
using System.Globalization;

namespace Majorsilence.Pdf
{
    /// <summary>
    /// Immutable 24-bit RGB colour used throughout the Majorsilence.Pdf API.
    /// </summary>
    public readonly struct PdfColor : IEquatable<PdfColor>
    {
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }

        public PdfColor(byte r, byte g, byte b) { R = r; G = g; B = b; }

        // ── factory helpers ───────────────────────────────────────────────────

        /// <summary>Create from individual byte channel values 0–255.</summary>
        public static PdfColor FromRgb(byte r, byte g, byte b) => new PdfColor(r, g, b);

        /// <summary>Create from a CSS hex string, e.g. <c>"#FF8800"</c> or <c>"FF8800"</c>.</summary>
        public static PdfColor FromHex(string hex)
        {
            if (hex == null) throw new ArgumentNullException(nameof(hex));
            string h = hex.TrimStart('#');
            if (h.Length != 6)
                throw new ArgumentException("Expected a 6-character hex colour string.", nameof(hex));
            return new PdfColor(
                byte.Parse(h.Substring(0, 2), NumberStyles.HexNumber),
                byte.Parse(h.Substring(2, 2), NumberStyles.HexNumber),
                byte.Parse(h.Substring(4, 2), NumberStyles.HexNumber));
        }

        // ── named colours ─────────────────────────────────────────────────────

        public static readonly PdfColor Black       = new PdfColor(  0,   0,   0);
        public static readonly PdfColor White       = new PdfColor(255, 255, 255);
        public static readonly PdfColor Red         = new PdfColor(255,   0,   0);
        public static readonly PdfColor Green       = new PdfColor(  0, 128,   0);
        public static readonly PdfColor Blue        = new PdfColor(  0,   0, 255);
        public static readonly PdfColor Yellow      = new PdfColor(255, 255,   0);
        public static readonly PdfColor Orange      = new PdfColor(255, 165,   0);
        public static readonly PdfColor Gray        = new PdfColor(128, 128, 128);
        public static readonly PdfColor LightGray   = new PdfColor(211, 211, 211);
        public static readonly PdfColor DarkGray    = new PdfColor( 64,  64,  64);
        public static readonly PdfColor Transparent = new PdfColor(  0,   0,   0); // use absence of colour in styles

        // ── PDF normalised channel values (0.0–1.0) ──────────────────────────

        internal float Rf => R / 255f;
        internal float Gf => G / 255f;
        internal float Bf => B / 255f;

        // ── equality ─────────────────────────────────────────────────────────

        public bool Equals(PdfColor other) => R == other.R && G == other.G && B == other.B;
        public override bool Equals(object? obj) => obj is PdfColor c && Equals(c);
        public override int GetHashCode() => (R << 16) | (G << 8) | B;
        public static bool operator ==(PdfColor a, PdfColor b) => a.Equals(b);
        public static bool operator !=(PdfColor a, PdfColor b) => !a.Equals(b);

        public override string ToString() => $"#{R:X2}{G:X2}{B:X2}";
    }
}
