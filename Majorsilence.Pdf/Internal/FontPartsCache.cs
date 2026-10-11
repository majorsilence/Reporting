// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Majorsilence.Pdf.Internal
{
    /// <summary>
    /// The unencrypted, finished pieces of an embedded TrueType font: the compressed ToUnicode CMap, the /W array
    /// and the compressed subset. They depend only on the font and the set of glyphs used, so documents that use the
    /// same glyphs of the same (process-shared) font share them instead of subsetting and compressing again.
    /// Every entry is immutable once published; encryption, which depends on the object number, is applied by the
    /// caller afterwards.
    /// </summary>
    internal sealed class FontParts
    {
        internal FontParts(byte[] toUnicodeCompressed, string widths, byte[] fontCompressed, int fontLength)
        {
            ToUnicodeCompressed = toUnicodeCompressed;
            Widths = widths;
            FontCompressed = fontCompressed;
            FontLength = fontLength;
        }

        internal byte[] ToUnicodeCompressed { get; }
        internal string Widths { get; }
        internal byte[] FontCompressed { get; }
        internal int FontLength { get; }
        internal long Size => ToUnicodeCompressed.Length + FontCompressed.Length + Widths.Length * 2L;
    }

    internal static class FontPartsCache
    {
        // Bounded in entries per font and in total bytes; past a limit parts are built per document as before.
        private const int MaxEntriesPerFont = 16;
        private const long MaxTotalBytes = 48L * 1024 * 1024;

        // Keyed weakly by font so entries go when the font is dropped from FontCache.
        private static readonly ConditionalWeakTable<TrueTypeFont, ConcurrentDictionary<string, FontParts>> ByFont =
            new ConditionalWeakTable<TrueTypeFont, ConcurrentDictionary<string, FontParts>>();

        private static long _totalBytes;
        private static long _hits;
        private static long _misses;

        internal static long Hits => Interlocked.Read(ref _hits);
        internal static long Misses => Interlocked.Read(ref _misses);

        /// <summary>
        /// The glyphs every embedded font carries besides the ones the document drew: printable ASCII. Documents then
        /// tend to need the same subset, which is what makes the cache hit. Fonts without these glyphs add nothing.
        /// </summary>
        internal static HashSet<ushort> WithBaseGlyphs(TrueTypeFont ttf, HashSet<ushort> used)
        {
            var all = new HashSet<ushort>(used);
            for (int cp = 0x20; cp <= 0x7E; cp++)
            {
                ushort gid = ttf.GetGlyphId(cp);
                if (gid != 0) all.Add(gid);
            }
            return all;
        }

        internal static string KeyOf(HashSet<ushort> glyphs)
        {
            var sorted = new ushort[glyphs.Count];
            glyphs.CopyTo(sorted);
            Array.Sort(sorted);
            var chars = new char[sorted.Length];
            for (int i = 0; i < sorted.Length; i++) chars[i] = (char)sorted[i];
            return new string(chars);
        }

        internal static FontParts GetOrBuild(TrueTypeFont ttf, HashSet<ushort> glyphs, Func<FontParts> build)
        {
            string key = KeyOf(glyphs);
            var perFont = ByFont.GetValue(ttf, _ => new ConcurrentDictionary<string, FontParts>(StringComparer.Ordinal));
            if (perFont.TryGetValue(key, out var hit))
            {
                Interlocked.Increment(ref _hits);
                return hit;
            }

            Interlocked.Increment(ref _misses);
            FontParts built = build();
            if (perFont.Count < MaxEntriesPerFont && Interlocked.Read(ref _totalBytes) + built.Size <= MaxTotalBytes)
            {
                FontParts winner = perFont.GetOrAdd(key, built);
                if (ReferenceEquals(winner, built))
                    Interlocked.Add(ref _totalBytes, built.Size);
                return winner;
            }
            return built;
        }
    }
}
