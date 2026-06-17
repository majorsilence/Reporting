// Copyright (C) 2025 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

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
                ttf = source.Path != null
                    ? GetOrLoad(source.Path)          // reuse path branch (already caches)
                    : new TrueTypeFont(source.Data!);
                _cache[source.CacheKey] = ttf;
            }
            return ttf;
        }
    }
}
