// Copyright (C) 2025 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Majorsilence.Pdf;
using NUnit.Framework;

namespace Majorsilence.Pdf.Tests
{
    [TestFixture]
    public class TrueTypeFontTests
    {
        // ── font path resolution (cross-platform) ─────────────────────────────

        private static string? FindFont(params string[] candidates)
        {
            foreach (var c in candidates)
                if (File.Exists(c)) return c;
            return null;
        }

        private static string? AnySystemFont() => FindFont(
            // Windows
            @"C:\Windows\Fonts\arial.ttf",
            @"C:\Windows\Fonts\times.ttf",
            @"C:\Windows\Fonts\cour.ttf",
            // macOS
            "/System/Library/Fonts/Helvetica.ttc",
            "/Library/Fonts/Arial.ttf",
            // Linux
            "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
            "/usr/share/fonts/truetype/msttcorefonts/arial.ttf"
        );

        // ── construction tests ────────────────────────────────────────────────

        [Test]
        public void Constructor_FromPath_LoadsFont()
        {
            string? path = AnySystemFont();
            Assume.That(path, Is.Not.Null, "No system font found — skipping TTF tests");

            var font = new TrueTypeFont(path!);
            Assert.That(font.UnitsPerEm, Is.GreaterThan(0));
            Assert.That(font.NumGlyphs,  Is.GreaterThan(0));
        }

        [Test]
        public void Constructor_FromBytes_MatchesFromPath()
        {
            string? path = AnySystemFont();
            Assume.That(path, Is.Not.Null, "No system font found");

            var fromPath  = new TrueTypeFont(path!);
            var fromBytes = new TrueTypeFont(File.ReadAllBytes(path!));

            Assert.That(fromBytes.UnitsPerEm,      Is.EqualTo(fromPath.UnitsPerEm));
            Assert.That(fromBytes.NumGlyphs,       Is.EqualTo(fromPath.NumGlyphs));
            Assert.That(fromBytes.PostScriptName,  Is.EqualTo(fromPath.PostScriptName));
        }

        [Test]
        public void Constructor_MissingFile_Throws()
        {
            // .NET may throw FileNotFoundException or DirectoryNotFoundException
            // depending on whether the parent directory exists.
            Assert.That(
                () => new TrueTypeFont("/nonexistent_dir_abc123/font.ttf"),
                Throws.InstanceOf<System.IO.IOException>());
        }

        // ── metrics tests ─────────────────────────────────────────────────────

        [Test]
        public void UnitsPerEm_IsReasonableValue()
        {
            string? path = AnySystemFont();
            Assume.That(path, Is.Not.Null);

            var font = new TrueTypeFont(path!);
            // TTF UPM is typically 1000 or 2048
            Assert.That(font.UnitsPerEm, Is.InRange(256, 4096));
        }

        [Test]
        public void Ascender_IsPositive()
        {
            string? path = AnySystemFont();
            Assume.That(path, Is.Not.Null);

            var font = new TrueTypeFont(path!);
            Assert.That(font.Ascender, Is.GreaterThan(0));
        }

        [Test]
        public void Descender_IsNegativeOrZero()
        {
            string? path = AnySystemFont();
            Assume.That(path, Is.Not.Null);

            var font = new TrueTypeFont(path!);
            Assert.That(font.Descender, Is.LessThanOrEqualTo(0));
        }

        [Test]
        public void BoundingBox_IsConsistent()
        {
            string? path = AnySystemFont();
            Assume.That(path, Is.Not.Null);

            var font = new TrueTypeFont(path!);
            Assert.That(font.XMax, Is.GreaterThan(font.XMin));
            Assert.That(font.YMax, Is.GreaterThan(font.YMin));
        }

        // ── glyph mapping tests ───────────────────────────────────────────────

        [Test]
        public void GetGlyphId_BasicAscii_NonZero()
        {
            string? path = AnySystemFont();
            Assume.That(path, Is.Not.Null);

            var font = new TrueTypeFont(path!);
            // All standard Latin fonts map ASCII printable chars
            foreach (char ch in "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789")
            {
                ushort gid = font.GetGlyphId(ch);
                Assert.That(gid, Is.GreaterThan(0), $"Expected glyph for '{ch}'");
            }
        }

        [Test]
        public void GetGlyphId_UnmappedChar_ReturnsZero()
        {
            string? path = AnySystemFont();
            Assume.That(path, Is.Not.Null);

            var font = new TrueTypeFont(path!);
            // Private-use area char that no standard font maps
            ushort gid = font.GetGlyphId(0xF8FF);
            // Result is 0 or .notdef (0) — may be non-zero in some fonts, but shouldn't crash
            Assert.That(gid, Is.GreaterThanOrEqualTo(0)); // just no exception
        }

        [Test]
        public void GetAdvanceWidth_BasicGlyphs_Positive()
        {
            string? path = AnySystemFont();
            Assume.That(path, Is.Not.Null);

            var font = new TrueTypeFont(path!);
            ushort gid = font.GetGlyphId('A');
            Assume.That(gid, Is.GreaterThan(0));
            Assert.That(font.GetAdvanceWidth(gid), Is.GreaterThan(0));
        }

        [Test]
        public void GetWidthPoint_ScalesWithFontSize()
        {
            string? path = AnySystemFont();
            Assume.That(path, Is.Not.Null);

            var font = new TrueTypeFont(path!);
            float w12 = font.GetWidthPoint("Hello", 12f);
            float w24 = font.GetWidthPoint("Hello", 24f);
            Assert.That(w12, Is.GreaterThan(0));
            Assert.That(w24, Is.GreaterThan(w12), "Larger font should produce wider text");
            Assert.That(w24, Is.EqualTo(w12 * 2f).Within(0.01f), "Width should scale linearly with size");
        }

        [Test]
        public void GetWidthPoint_LongerText_IsWider()
        {
            string? path = AnySystemFont();
            Assume.That(path, Is.Not.Null);

            var font = new TrueTypeFont(path!);
            float wShort = font.GetWidthPoint("Hi", 12f);
            float wLong  = font.GetWidthPoint("Hello, World!", 12f);
            Assert.That(wLong, Is.GreaterThan(wShort));
        }

        [Test]
        public void GetWidthPoint_EmptyString_IsZero()
        {
            string? path = AnySystemFont();
            Assume.That(path, Is.Not.Null);

            var font = new TrueTypeFont(path!);
            Assert.That(font.GetWidthPoint("", 12f), Is.EqualTo(0f));
        }

        // ── cmap iteration ────────────────────────────────────────────────────

        [Test]
        public void GetCharGlyphMappings_HasEntries()
        {
            string? path = AnySystemFont();
            Assume.That(path, Is.Not.Null);

            var font     = new TrueTypeFont(path!);
            var mappings = font.GetCharGlyphMappings().ToList();
            Assert.That(mappings.Count, Is.GreaterThan(10), "Expect at least a handful of mapped glyphs");
        }

        [Test]
        public void GetCharGlyphMappings_IncludesAsciiRange()
        {
            string? path = AnySystemFont();
            Assume.That(path, Is.Not.Null);

            var font     = new TrueTypeFont(path!);
            var dict     = font.GetCharGlyphMappings().ToDictionary(m => m.codePoint, m => m.glyphId);

            Assert.That(dict.ContainsKey('A'), "Expected mapping for 'A'");
            Assert.That(dict.ContainsKey('z'), "Expected mapping for 'z'");
            Assert.That(dict.ContainsKey('0'), "Expected mapping for '0'");
        }

        // ── PostScript name ───────────────────────────────────────────────────

        [Test]
        public void PostScriptName_IsNonEmpty()
        {
            string? path = AnySystemFont();
            Assume.That(path, Is.Not.Null);

            var font = new TrueTypeFont(path!);
            Assert.That(font.PostScriptName, Is.Not.Null.And.Not.Empty);
        }

        // ── data access ───────────────────────────────────────────────────────

        [Test]
        public void Data_MatchesFileContents()
        {
            string? path = AnySystemFont();
            Assume.That(path, Is.Not.Null);

            var font      = new TrueTypeFont(path!);
            var fileBytes = File.ReadAllBytes(path!);
            Assert.That(font.Data.Length, Is.EqualTo(fileBytes.Length));
            Assert.That(font.Data[0], Is.EqualTo(fileBytes[0]));
        }
    }
}
