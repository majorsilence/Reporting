// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;

namespace Majorsilence.Pdf
{
    /// <summary>
    /// An image ready to draw: decoded from an encoded JPEG or PNG file, or built from raw pixels.
    /// Drawing the same <see cref="PdfImage"/> (or identical bytes) on several pages embeds it once.
    /// </summary>
    /// <remarks>
    /// Supported encodings: baseline/progressive JPEG with 1 (gray) or 3 (YCbCr) components, and
    /// non-interlaced 8-bit PNG (grayscale, grayscale+alpha, RGB, palette, RGBA) with transparency.
    /// PNG is decoded in the core library, so no extra dependency is needed; for any other format
    /// decode the pixels yourself and use <see cref="FromRgb24"/> or <see cref="FromRgba"/>.
    /// </remarks>
    public sealed class PdfImage
    {
        internal enum Kind { Jpeg, Rgb24, Rgba }

        internal Kind Format { get; }
        internal byte[] Data { get; }
        internal int Components { get; }

        /// <summary>Width in pixels.</summary>
        public int PixelWidth { get; }

        /// <summary>Height in pixels.</summary>
        public int PixelHeight { get; }

        private PdfImage(Kind format, byte[] data, int width, int height, int components)
        {
            Format = format; Data = data; PixelWidth = width; PixelHeight = height; Components = components;
        }

        /// <summary>Load an encoded JPEG or PNG image.</summary>
        /// <exception cref="ArgumentException">The bytes are not a supported JPEG or PNG image.</exception>
        public static PdfImage FromBytes(byte[] encoded)
        {
            if (encoded == null) throw new ArgumentNullException(nameof(encoded));

            if (encoded.Length > 3 && encoded[0] == 0xFF && encoded[1] == 0xD8)
            {
                if (!TryReadJpegInfo(encoded, out int w, out int h, out int comps))
                    throw new ArgumentException("The JPEG data is truncated or malformed.", nameof(encoded));
                if (comps != 1 && comps != 3)
                    throw new ArgumentException(
                        $"JPEG images with {comps} colour components (e.g. CMYK) are not supported; convert to RGB or gray.",
                        nameof(encoded));
                return new PdfImage(Kind.Jpeg, encoded, w, h, comps);
            }

            if (encoded.Length > 8 && encoded[0] == 0x89 && encoded[1] == 0x50 && encoded[2] == 0x4E && encoded[3] == 0x47)
            {
                if (!Internal.PngDecoder.TryDecode(encoded, out var rgba, out int w, out int h))
                    throw new ArgumentException(
                        "The PNG is not supported: only non-interlaced 8-bit grayscale, grayscale+alpha, RGB, palette and RGBA images can be read.",
                        nameof(encoded));
                return FromDecodedRgba(rgba, w, h);
            }

            throw new ArgumentException("Unrecognised image format: expected a JPEG or PNG file.", nameof(encoded));
        }

        /// <summary>Build an image from packed 8-bit RGB pixels (3 bytes per pixel, row by row from the top).</summary>
        public static PdfImage FromRgb24(byte[] rgb, int pixelWidth, int pixelHeight)
        {
            if (rgb == null) throw new ArgumentNullException(nameof(rgb));
            CheckSize(pixelWidth, pixelHeight);
            if (rgb.Length != pixelWidth * pixelHeight * 3)
                throw new ArgumentException($"Expected {pixelWidth * pixelHeight * 3} bytes for {pixelWidth}x{pixelHeight} RGB pixels, got {rgb.Length}.", nameof(rgb));
            return new PdfImage(Kind.Rgb24, rgb, pixelWidth, pixelHeight, 3);
        }

        /// <summary>Build an image from packed 8-bit RGBA pixels (4 bytes per pixel, row by row from the top, straight alpha).</summary>
        public static PdfImage FromRgba(byte[] rgba, int pixelWidth, int pixelHeight)
        {
            if (rgba == null) throw new ArgumentNullException(nameof(rgba));
            CheckSize(pixelWidth, pixelHeight);
            if (rgba.Length != pixelWidth * pixelHeight * 4)
                throw new ArgumentException($"Expected {pixelWidth * pixelHeight * 4} bytes for {pixelWidth}x{pixelHeight} RGBA pixels, got {rgba.Length}.", nameof(rgba));
            return FromDecodedRgba(rgba, pixelWidth, pixelHeight);
        }

        // Fully opaque pixels are stored as plain RGB: no soft mask, a smaller file.
        private static PdfImage FromDecodedRgba(byte[] rgba, int w, int h)
        {
            int pixels = w * h;
            for (int i = 0; i < pixels; i++)
                if (rgba[i * 4 + 3] != 255)
                    return new PdfImage(Kind.Rgba, rgba, w, h, 4);

            var rgb = new byte[pixels * 3];
            for (int i = 0; i < pixels; i++)
            {
                rgb[i * 3] = rgba[i * 4];
                rgb[i * 3 + 1] = rgba[i * 4 + 1];
                rgb[i * 3 + 2] = rgba[i * 4 + 2];
            }
            return new PdfImage(Kind.Rgb24, rgb, w, h, 3);
        }

        private static void CheckSize(int w, int h)
        {
            if (w <= 0) throw new ArgumentOutOfRangeException(nameof(w), "Width must be positive.");
            if (h <= 0) throw new ArgumentOutOfRangeException(nameof(h), "Height must be positive.");
        }

        // Walks the JPEG marker segments to the first start-of-frame marker.
        private static bool TryReadJpegInfo(byte[] d, out int width, out int height, out int components)
        {
            width = height = components = 0;
            int i = 2;
            while (i + 4 <= d.Length)
            {
                if (d[i] != 0xFF) { i++; continue; }
                byte marker = d[i + 1];
                if (marker == 0xFF) { i++; continue; }
                if (marker == 0xD8 || marker == 0x01 || (marker >= 0xD0 && marker <= 0xD7)) { i += 2; continue; }

                int length = (d[i + 2] << 8) | d[i + 3];
                bool isFrame = marker >= 0xC0 && marker <= 0xCF && marker != 0xC4 && marker != 0xC8 && marker != 0xCC;
                if (isFrame)
                {
                    if (i + 10 > d.Length) return false;
                    height = (d[i + 5] << 8) | d[i + 6];
                    width = (d[i + 7] << 8) | d[i + 8];
                    components = d[i + 9];
                    return width > 0 && height > 0;
                }
                i += 2 + length;
            }
            return false;
        }
    }
}
