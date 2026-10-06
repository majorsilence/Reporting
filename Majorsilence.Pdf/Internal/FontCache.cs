// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>


using System;
using System.Collections.Generic;
using System.IO;

namespace Majorsilence.Pdf.Internal
{
    /// <summary>
    /// Document-scoped cache that loads each TrueType font file at most once.
    /// </summary>
    internal sealed class FontCache
    {
        private readonly Dictionary<string, TrueTypeFont> _cache =
            new Dictionary<string, TrueTypeFont>(StringComparer.OrdinalIgnoreCase);

        internal TrueTypeFont GetOrLoad(string path)
        {
            if (!_cache.TryGetValue(path, out var ttf))
            {
                if (!File.Exists(path))
                    throw new FileNotFoundException($"Font file not found: {path}", path);
                ttf = new TrueTypeFont(path);
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
                    ttf = new TrueTypeFont(File.ReadAllBytes(source.Path), source.FaceIndex);
                }
                _cache[source.CacheKey] = ttf;
            }
            return ttf;
        }
    }
}
