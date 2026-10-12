// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;
using System.Collections.Generic;
using System.IO;
using Majorsilence.Pdf;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// Fonts an application adds to PDF export: fonts shipped with the app rather than installed
    /// system-wide (containers, locked-down servers), fonts in a folder the renderer does not scan,
    /// barcode or MICR fonts, and the choice of fallback for characters a font lacks (CJK, say).
    ///
    /// Registration is process-wide: call these once at startup, before rendering. It applies to
    /// every PDF render after the call; a render that began earlier keeps the fonts it started with.
    ///
    /// Precedence, highest first: a family given to <c>RegisterFont</c>, a family found in a
    /// <c>RegisterFontDirectory</c> folder (the folder registered first wins), a family found in a
    /// folder named by the <c>MAJORSILENCE_PDF_FONT_DIRS</c> environment variable, then the fonts
    /// installed on the machine and bundled with the library. Fallback families added with
    /// <see cref="AddFallbackFamily"/> are tried, in the order added, before the built-in fallback chain.
    ///
    /// The environment variable lets a host without code, such as RdlCmd or RdlMcp, point at a font
    /// folder: a list of folders separated by the platform's path separator (<c>:</c> or <c>;</c>),
    /// each searched with its subfolders.
    /// </summary>
    public static class PdfFonts
    {
        /// <summary>Name of the environment variable holding extra font folders.</summary>
        public const string FontDirectoriesVariable = "MAJORSILENCE_PDF_FONT_DIRS";

        private sealed class Family
        {
            internal string Name = "";
            internal FontSource? Regular, Bold, Italic, BoldItalic;
        }

        private static readonly object Gate = new object();
        private static readonly List<Family> Families = new List<Family>();
        private static readonly List<(string Path, bool Subfolders)> Directories = new List<(string, bool)>();
        private static readonly List<string> Fallbacks = new List<string>();
        private static int _version;

        /// <summary>
        /// Register a family from font files. Any style may be omitted; a missing one is drawn
        /// with the nearest style given. Registering a name again replaces the earlier registration.
        /// </summary>
        public static void RegisterFont(string family,
            string? regular = null, string? bold = null, string? italic = null, string? boldItalic = null)
        {
            RegisterFont(family,
                regular == null ? null : FontSource.FromPath(regular),
                bold == null ? null : FontSource.FromPath(bold),
                italic == null ? null : FontSource.FromPath(italic),
                boldItalic == null ? null : FontSource.FromPath(boldItalic));
        }

        /// <summary>
        /// Register a family from <see cref="FontSource"/> values, which can name one font of a
        /// TrueType collection (<c>.ttc</c>) or hold the font's bytes.
        /// </summary>
        public static void RegisterFont(string family,
            FontSource? regular, FontSource? bold = null, FontSource? italic = null, FontSource? boldItalic = null)
        {
            if (string.IsNullOrEmpty(family)) throw new ArgumentNullException(nameof(family));
            if (regular == null && bold == null && italic == null && boldItalic == null)
                throw new ArgumentException("At least one style of the font is required.", nameof(regular));
            lock (Gate)
            {
                Families.RemoveAll(f => string.Equals(f.Name, family, StringComparison.OrdinalIgnoreCase));
                Families.Add(new Family { Name = family, Regular = regular, Bold = bold, Italic = italic, BoldItalic = boldItalic });
                _version++;
            }
        }

        /// <summary>Register a family from the bytes of its font files.</summary>
        public static void RegisterFont(string family,
            byte[]? regular, byte[]? bold = null, byte[]? italic = null, byte[]? boldItalic = null)
        {
            RegisterFont(family,
                regular == null ? null : FontSource.FromBytes(family + "-Regular", regular),
                bold == null ? null : FontSource.FromBytes(family + "-Bold", bold),
                italic == null ? null : FontSource.FromBytes(family + "-Italic", italic),
                boldItalic == null ? null : FontSource.FromBytes(family + "-BoldItalic", boldItalic));
        }

        /// <summary>
        /// Register every font in a folder under the family name each declares (as the operating
        /// system does), with its subfolders unless <paramref name="includeSubfolders"/> is false.
        /// A folder that does not exist adds nothing.
        /// </summary>
        public static void RegisterFontDirectory(string directory, bool includeSubfolders = true)
        {
            if (string.IsNullOrEmpty(directory)) throw new ArgumentNullException(nameof(directory));
            lock (Gate)
            {
                Directories.Add((Path.GetFullPath(directory), includeSubfolders));
                _version++;
            }
        }

        /// <summary>
        /// Add a family to the fallback chain used for characters the font in use has no glyph for,
        /// ahead of the built-in chain. Families are tried in the order added. The family has to be
        /// registered (here, or installed) to be used.
        /// </summary>
        public static void AddFallbackFamily(string family)
        {
            if (string.IsNullOrEmpty(family)) throw new ArgumentNullException(nameof(family));
            lock (Gate)
            {
                if (!Fallbacks.Exists(f => string.Equals(f, family, StringComparison.OrdinalIgnoreCase))) Fallbacks.Add(family);
                _version++;
            }
        }

        /// <summary>Remove everything registered through this class.</summary>
        public static void Reset()
        {
            lock (Gate)
            {
                Families.Clear();
                Directories.Clear();
                Fallbacks.Clear();
                _version++;
            }
        }

        // Part of the shared registry's cache key: a registration made after the first render
        // gets a registry that includes it.
        internal static string CacheKey
        {
            get
            {
                lock (Gate)
                    return _version + "|" + Environment.GetEnvironmentVariable(FontDirectoriesVariable);
            }
        }

        internal static IReadOnlyList<string> EnvironmentDirectories()
        {
            var list = new List<string>();
            string? value = Environment.GetEnvironmentVariable(FontDirectoriesVariable);
            if (string.IsNullOrWhiteSpace(value)) return list;
            foreach (string part in value!.Split(new[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries))
            {
                string dir = part.Trim();
                if (dir.Length > 0) list.Add(dir);
            }
            return list;
        }

        /// <summary>Apply what was registered to a registry being built, over what it already holds.</summary>
        internal static void ApplyTo(FontRegistry reg)
        {
            List<Family> families;
            List<(string Path, bool Subfolders)> dirs;
            List<string> fallbacks;
            lock (Gate)
            {
                families = new List<Family>(Families);
                dirs = new List<(string, bool)>(Directories);
                fallbacks = new List<string>(Fallbacks);
            }

            // Lowest precedence first, each replacing what came before: the environment's folders,
            // then registered folders (reversed, so the one registered first wins), then families.
            var env = EnvironmentDirectories();
            for (int i = env.Count - 1; i >= 0; i--)
                reg.AddInstalledFonts(env[i], includeSubfolders: true, replaceExisting: true);
            for (int i = dirs.Count - 1; i >= 0; i--)
                reg.AddInstalledFonts(dirs[i].Path, dirs[i].Subfolders, replaceExisting: true);
            foreach (var f in families)
                reg.AddFamily(f.Name, f.Regular, f.Bold, f.Italic, f.BoldItalic);

            foreach (string fb in fallbacks)
                reg.AddFallback(fb);
        }
    }
}
