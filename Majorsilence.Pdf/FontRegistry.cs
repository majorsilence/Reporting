// Copyright (C) 2025 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.IO;

namespace Majorsilence.Pdf
{
    /// <summary>
    /// Maps logical font family names to TrueType font files or byte arrays,
    /// and defines an ordered fallback chain used when the primary font lacks a glyph.
    /// </summary>
    /// <example>
    /// <code>
    /// var fonts = new FontRegistry()
    ///     .AddFamily("LiberationSans",
    ///         regular:    @"Fonts\LiberationSans-Regular.ttf",
    ///         bold:       @"Fonts\LiberationSans-Bold.ttf",
    ///         italic:     @"Fonts\LiberationSans-Italic.ttf",
    ///         boldItalic: @"Fonts\LiberationSans-BoldItalic.ttf")
    ///     .AddFamily("NotoSans",
    ///         regular: @"Fonts\NotoSans-Regular.ttf",
    ///         bold:    @"Fonts\NotoSans-Bold.ttf")
    ///     .AddFallback("NotoSans");   // used when primary font has no glyph
    ///
    /// PdfDocument.Create()
    ///     .WithFontRegistry(fonts)
    ///     .AddPage(PageSizes.A4, canvas =>
    ///         canvas.DrawText("Hello 世界", 72, 100,
    ///             TextStyle.Default.WithFamily("LiberationSans").WithSize(14)))
    ///     .Save("out.pdf");
    /// </code>
    /// </example>
    public sealed class FontRegistry
    {
        private readonly Dictionary<string, FontFamily> _families =
            new Dictionary<string, FontFamily>(StringComparer.OrdinalIgnoreCase);

        private readonly List<string> _fallbackOrder = new List<string>();

        // ── adding families ───────────────────────────────────────────────────

        /// <summary>Register a font family by providing file paths for each weight/style variant.</summary>
        public FontRegistry AddFamily(string name,
            string? regular = null, string? bold = null,
            string? italic = null, string? boldItalic = null)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            _families[name] = new FontFamily(name,
                regular    == null ? null : FontSource.FromPath(regular),
                bold       == null ? null : FontSource.FromPath(bold),
                italic     == null ? null : FontSource.FromPath(italic),
                boldItalic == null ? null : FontSource.FromPath(boldItalic));
            return this;
        }

        /// <summary>Register a font family by providing raw TTF byte arrays for each variant.</summary>
        public FontRegistry AddFamily(string name,
            byte[]? regular = null, byte[]? bold = null,
            byte[]? italic = null, byte[]? boldItalic = null)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            _families[name] = new FontFamily(name,
                regular    == null ? null : FontSource.FromBytes(name + "-Regular",    regular),
                bold       == null ? null : FontSource.FromBytes(name + "-Bold",       bold),
                italic     == null ? null : FontSource.FromBytes(name + "-Italic",     italic),
                boldItalic == null ? null : FontSource.FromBytes(name + "-BoldItalic", boldItalic));
            return this;
        }

        /// <summary>
        /// Scan a directory and register font families detected from filenames.
        /// Expects names like <c>FamilyName-Regular.ttf</c>, <c>FamilyName-Bold.ttf</c>, etc.
        /// </summary>
        public FontRegistry AddDirectory(string directory)
        {
            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException($"Font directory not found: {directory}");

            // Collect (familyName, variant, path) from each .ttf / .otf file
            var found = new Dictionary<string, (string? r, string? b, string? i, string? bi)>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var file in Directory.GetFiles(directory, "*.ttf"))
                RegisterFile(file, found);
            foreach (var file in Directory.GetFiles(directory, "*.otf"))
                RegisterFile(file, found);

            foreach (var kv in found)
                AddFamily(kv.Key, kv.Value.r, kv.Value.b, kv.Value.i, kv.Value.bi);

            return this;
        }

        private static void RegisterFile(
            string path,
            Dictionary<string, (string? r, string? b, string? i, string? bi)> found)
        {
            string stem = Path.GetFileNameWithoutExtension(path);

            // Detect variant suffix: -BoldItalic, -Bold, -Italic, -Regular, -Oblique, -BoldOblique
            string family;
            string variant;
            if (stem.EndsWith("-BoldItalic",  StringComparison.OrdinalIgnoreCase)) { family = stem.Substring(0, stem.Length - 11); variant = "bi"; }
            else if (stem.EndsWith("-BoldOblique", StringComparison.OrdinalIgnoreCase)) { family = stem.Substring(0, stem.Length - 12); variant = "bi"; }
            else if (stem.EndsWith("-Bold",   StringComparison.OrdinalIgnoreCase)) { family = stem.Substring(0, stem.Length - 5);  variant = "b"; }
            else if (stem.EndsWith("-Italic", StringComparison.OrdinalIgnoreCase)) { family = stem.Substring(0, stem.Length - 7);  variant = "i"; }
            else if (stem.EndsWith("-Oblique",StringComparison.OrdinalIgnoreCase)) { family = stem.Substring(0, stem.Length - 8);  variant = "i"; }
            else if (stem.EndsWith("-Regular",StringComparison.OrdinalIgnoreCase)) { family = stem.Substring(0, stem.Length - 8);  variant = "r"; }
            else { family = stem; variant = "r"; }

            if (!found.TryGetValue(family, out var entry)) entry = (null, null, null, null);
            switch (variant)
            {
                case "r":  entry = (path, entry.b, entry.i, entry.bi); break;
                case "b":  entry = (entry.r, path, entry.i, entry.bi); break;
                case "i":  entry = (entry.r, entry.b, path, entry.bi); break;
                case "bi": entry = (entry.r, entry.b, entry.i, path);  break;
            }
            found[family] = entry;
        }

        // ── fallback chain ────────────────────────────────────────────────────

        /// <summary>
        /// Append a family to the fallback chain.  When the primary font has no glyph for a
        /// character, each fallback is tried in the order they were added.
        /// </summary>
        public FontRegistry AddFallback(string familyName)
        {
            if (string.IsNullOrEmpty(familyName)) throw new ArgumentNullException(nameof(familyName));
            _fallbackOrder.Add(familyName);
            return this;
        }

        // ── resolution ────────────────────────────────────────────────────────

        /// <summary>
        /// Resolve a family name + bold/italic flags to a <see cref="FontSource"/>,
        /// or <c>null</c> if the family is not registered.
        /// </summary>
        public FontSource? Resolve(string family, bool bold, bool italic)
        {
            if (!_families.TryGetValue(family, out var fam)) return null;
            // Prefer exact variant, degrade gracefully
            if (bold && italic && fam.BoldItalic != null) return fam.BoldItalic;
            if (bold && fam.Bold != null) return fam.Bold;
            if (italic && fam.Italic != null) return fam.Italic;
            return fam.Regular ?? fam.Bold ?? fam.Italic ?? fam.BoldItalic;
        }

        /// <summary>
        /// Returns an ordered list of <see cref="FontSource"/> instances to try as fallback,
        /// using the same bold/italic flags as the primary style.
        /// </summary>
        public IReadOnlyList<FontSource> GetFallbackSources(bool bold, bool italic)
        {
            var list = new List<FontSource>(_fallbackOrder.Count);
            foreach (var name in _fallbackOrder)
            {
                var src = Resolve(name, bold, italic);
                if (src != null) list.Add(src);
            }
            return list;
        }

        /// <summary>Returns true if the named family has been registered.</summary>
        public bool Contains(string family) =>
            !string.IsNullOrEmpty(family) && _families.ContainsKey(family);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FontFamily — four variants for one logical family
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class FontFamily
    {
        internal string Name       { get; }
        internal FontSource? Regular    { get; }
        internal FontSource? Bold       { get; }
        internal FontSource? Italic     { get; }
        internal FontSource? BoldItalic { get; }

        internal FontFamily(string name,
            FontSource? regular, FontSource? bold,
            FontSource? italic, FontSource? boldItalic)
        {
            Name = name;
            Regular    = regular;
            Bold       = bold;
            Italic     = italic;
            BoldItalic = boldItalic;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FontSource — either a file path or in-memory bytes
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Represents a TrueType font supplied either as a file path or as raw bytes.
    /// </summary>
    public sealed class FontSource
    {
        /// <summary>File path, or <c>null</c> when the font is supplied as bytes.</summary>
        public string?  Path     { get; }

        /// <summary>Raw TTF/OTF bytes, or <c>null</c> when a file path is used.</summary>
        public byte[]?  Data     { get; }

        /// <summary>Unique key used for caching within a document.</summary>
        internal string CacheKey { get; }

        private FontSource(string? path, byte[]? data, string cacheKey)
        {
            Path     = path;
            Data     = data;
            CacheKey = cacheKey;
        }

        /// <summary>Create a <see cref="FontSource"/> backed by a file.</summary>
        public static FontSource FromPath(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            return new FontSource(path, null, path);
        }

        /// <summary>Create a <see cref="FontSource"/> backed by raw bytes.</summary>
        public static FontSource FromBytes(string cacheKey, byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return new FontSource(null, data, cacheKey ?? throw new ArgumentNullException(nameof(cacheKey)));
        }
    }
}
