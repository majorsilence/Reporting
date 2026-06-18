// Copyright (C) 2025 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Majorsilence.Pdf.Internal
{
    // Minimal PNG decoder that produces raw RGBA bytes.
    // Supports 8-bit RGB (colorType 2), 8-bit indexed/palette (colorType 3),
    // and 8-bit RGBA (colorType 6) — the formats used by color emoji fonts.
    // Non-interlaced images only.
    internal static class PngDecoder
    {
        private static readonly byte[] PngSignature =
            { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        // Returns true and fills rgba/width/height on success.
        internal static bool TryDecode(byte[] png, out byte[] rgba, out int width, out int height)
        {
            rgba = Array.Empty<byte>();
            width = height = 0;

            if (png == null || png.Length < 33) return false;

            // Validate PNG signature
            for (int i = 0; i < 8; i++)
                if (png[i] != PngSignature[i]) return false;

            // Parse chunks
            int w = 0, h = 0, bitDepth = 0, colorType = 0;
            byte[]? plte = null;   // PLTE palette (RGB triples)
            byte[]? trns = null;   // tRNS transparency data
            var idatChunks = new List<byte[]>();

            int pos = 8;
            bool done = false;
            while (!done && pos + 12 <= png.Length)
            {
                int chunkLen = ReadI32(png, pos);
                if (chunkLen < 0 || pos + 12 + chunkLen > png.Length) break;
                string type = System.Text.Encoding.ASCII.GetString(png, pos + 4, 4);
                int dataOff = pos + 8;
                pos += 12 + chunkLen;

                switch (type)
                {
                    case "IHDR":
                        if (chunkLen < 13) return false;
                        w         = ReadI32(png, dataOff);
                        h         = ReadI32(png, dataOff + 4);
                        bitDepth  = png[dataOff + 8];
                        colorType = png[dataOff + 9];
                        break;

                    case "PLTE":
                        plte = new byte[chunkLen];
                        Buffer.BlockCopy(png, dataOff, plte, 0, chunkLen);
                        break;

                    case "tRNS":
                        trns = new byte[chunkLen];
                        Buffer.BlockCopy(png, dataOff, trns, 0, chunkLen);
                        break;

                    case "IDAT":
                        var chunk = new byte[chunkLen];
                        Buffer.BlockCopy(png, dataOff, chunk, 0, chunkLen);
                        idatChunks.Add(chunk);
                        break;

                    case "IEND":
                        done = true;
                        break;
                }
            }

            if (w <= 0 || h <= 0 || bitDepth != 8) return false;
            if (colorType != 2 && colorType != 3 && colorType != 6) return false;

            // Concatenate and decompress IDAT
            int total = 0;
            foreach (var c in idatChunks) total += c.Length;
            var idat = new byte[total];
            int off = 0;
            foreach (var c in idatChunks) { Buffer.BlockCopy(c, 0, idat, off, c.Length); off += c.Length; }

            byte[] filtered;
            try
            {
                // idat is a zlib stream; skip 2-byte zlib header (CMF + FLG).
                // DeflateStream reads the deflate payload and stops at the end-of-stream marker.
                using var ms = new MemoryStream(idat, 2, idat.Length - 2);
                using var ds = new DeflateStream(ms, CompressionMode.Decompress);
                using var outMs = new MemoryStream();
                ds.CopyTo(outMs);
                filtered = outMs.ToArray();
            }
            catch { return false; }

            // Un-filter scanlines
            int bpp = colorType == 3 ? 1 : (colorType == 6 ? 4 : 3);
            int stride = w * bpp;
            int expectedFiltered = h * (stride + 1);
            if (filtered.Length < expectedFiltered) return false;

            rgba = new byte[w * h * 4];
            var prev = new byte[stride];
            var curr = new byte[stride];

            for (int row = 0; row < h; row++)
            {
                int srcBase = row * (stride + 1);
                int filterType = filtered[srcBase];
                Buffer.BlockCopy(filtered, srcBase + 1, curr, 0, stride);

                // Reconstruct scanline in-place (left-to-right preserves the dependency chain)
                for (int i = 0; i < stride; i++)
                {
                    byte a  = i >= bpp ? curr[i - bpp] : (byte)0; // left (already reconstructed)
                    byte b2 = prev[i];                             // up
                    byte c2 = i >= bpp ? prev[i - bpp] : (byte)0; // up-left
                    curr[i] = filterType switch
                    {
                        0 => curr[i],
                        1 => (byte)(curr[i] + a),
                        2 => (byte)(curr[i] + b2),
                        3 => (byte)(curr[i] + ((a + b2) >> 1)),
                        4 => (byte)(curr[i] + Paeth(a, b2, c2)),
                        _ => curr[i],
                    };
                }

                // Convert to RGBA
                int dstBase = row * w * 4;
                if (colorType == 6) // RGBA
                {
                    Buffer.BlockCopy(curr, 0, rgba, dstBase, stride);
                }
                else if (colorType == 2) // RGB
                {
                    for (int i = 0; i < w; i++)
                    {
                        rgba[dstBase + i * 4]     = curr[i * 3];
                        rgba[dstBase + i * 4 + 1] = curr[i * 3 + 1];
                        rgba[dstBase + i * 4 + 2] = curr[i * 3 + 2];
                        rgba[dstBase + i * 4 + 3] = 255;
                    }
                }
                else // colorType == 3: palette
                {
                    for (int i = 0; i < w; i++)
                    {
                        int idx = curr[i];
                        if (plte != null && idx * 3 + 2 < plte.Length)
                        {
                            rgba[dstBase + i * 4]     = plte[idx * 3];
                            rgba[dstBase + i * 4 + 1] = plte[idx * 3 + 1];
                            rgba[dstBase + i * 4 + 2] = plte[idx * 3 + 2];
                        }
                        rgba[dstBase + i * 4 + 3] = (trns != null && idx < trns.Length) ? trns[idx] : (byte)255;
                    }
                }

                // Swap prev/curr buffers
                var tmp = prev; prev = curr; curr = tmp;
            }

            width = w;
            height = h;
            return true;
        }

        private static int ReadI32(byte[] b, int o)
            => (b[o] << 24) | (b[o + 1] << 16) | (b[o + 2] << 8) | b[o + 3];

        private static byte Paeth(byte a, byte b2, byte c2)
        {
            int p = a + b2 - c2;
            int pa = Math.Abs(p - a);
            int pb = Math.Abs(p - b2);
            int pc = Math.Abs(p - c2);
            return pa <= pb && pa <= pc ? a : (pb <= pc ? b2 : c2);
        }
    }
}
