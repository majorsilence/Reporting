// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>


using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace Majorsilence.Pdf.Internal
{
    /// <summary>
    /// Document-scoped cache that loads each TrueType font file at most once. Fonts read from
    /// files are also kept for the life of the process, so a second document does not read and
    /// parse the same file again. A <see cref="TrueTypeFont"/> is only written while it is being
    /// constructed, so one instance can be used by any number of documents and threads.
    /// </summary>
    internal sealed class FontCache
    {
        // Keyed by file + face + size + write time, so a replaced file is loaded afresh.
        // Bounded: past the limit fonts are simply loaded per document as before.
        private const int MaxSharedFonts = 64;
        private const long MaxSharedFontBytes = 32L * 1024 * 1024;

        private static readonly ConcurrentDictionary<string, Lazy<TrueTypeFont>> Shared =
            new ConcurrentDictionary<string, Lazy<TrueTypeFont>>(StringComparer.OrdinalIgnoreCase);

        private static TrueTypeFont LoadFile(string path, int faceIndex)
        {
            FileInfo info;
            try { info = new FileInfo(path); }
            catch (Exception) { return Read(path, faceIndex); }

            if (!info.Exists || info.Length > MaxSharedFontBytes)
                return Read(path, faceIndex);

            string key = path + "#" + faceIndex + "#" + info.Length + "#" + info.LastWriteTimeUtc.Ticks;
            if (Shared.TryGetValue(key, out var hit))
                return hit.Value;
            if (Shared.Count >= MaxSharedFonts)
                return Read(path, faceIndex);
            return Shared.GetOrAdd(key, _ => new Lazy<TrueTypeFont>(() => Read(path, faceIndex))).Value;
        }

        private static TrueTypeFont Read(string path, int faceIndex) =>
            faceIndex == 0 ? new TrueTypeFont(path) : new TrueTypeFont(File.ReadAllBytes(path), faceIndex);

        private readonly Dictionary<string, TrueTypeFont> _cache =
            new Dictionary<string, TrueTypeFont>(StringComparer.OrdinalIgnoreCase);

        internal TrueTypeFont GetOrLoad(string path)
        {
            if (!_cache.TryGetValue(path, out var ttf))
            {
                if (!File.Exists(path))
                    throw new FileNotFoundException($"Font file not found: {path}", path);
                ttf = LoadFile(path, 0);
                _cache[path] = ttf;
            }
            return ttf;
        }

        internal TrueTypeFont GetOrLoad(FontSource source)
        {
            if (!_cache.TryGetValue(source.CacheKey, out var ttf))
            {
                if (source.Path == null)
                    ttf = new TrueTypeFont(source.Data!);
                else if (source.FaceIndex == 0)
                    ttf = GetOrLoad(source.Path);     // reuse path branch (already caches)
                else
                {
                    if (!File.Exists(source.Path))
                        throw new FileNotFoundException($"Font file not found: {source.Path}", source.Path);
                    ttf = LoadFile(source.Path, source.FaceIndex);
                }
                _cache[source.CacheKey] = ttf;
            }
            return ttf;
        }
    }
}
