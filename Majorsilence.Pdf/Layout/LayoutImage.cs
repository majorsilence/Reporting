// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;

namespace Majorsilence.Pdf.Layout
{
    /// <summary>How an image is fitted into its box when both a width and a height are given.</summary>
    public enum ImageFit
    {
        /// <summary>Scale to fit entirely inside the box, keeping the aspect ratio (letterboxed).</summary>
        Contain,
        /// <summary>Scale to cover the whole box, keeping the aspect ratio; the overflow is clipped.</summary>
        Cover,
        /// <summary>Stretch to the box, ignoring the aspect ratio.</summary>
        Fill,
        /// <summary>Draw at one point per pixel, centred in the box and clipped to it.</summary>
        None,
    }

    /// <summary>An image in a box. Atomic: it never splits across pages.</summary>
    internal sealed class ImageElement : LayoutElement
    {
        private readonly PdfImage _image;
        private readonly float? _width, _height;
        private readonly ImageFit _fit;
        private readonly TextAlignment _alignment;

        // Set on measured heads.
        private readonly float _boxWidth = -1f;

        public ImageElement(PdfImage image, float? width, float? height, ImageFit fit, TextAlignment alignment)
        {
            _image = image; _width = width; _height = height; _fit = fit; _alignment = alignment;
        }

        private ImageElement(ImageElement source, float boxWidth)
            : this(source._image, source._width, source._height, source._fit, source._alignment)
        {
            _boxWidth = boxWidth;
        }

        private (float Width, float Height) BoxSize(float available)
        {
            float pw = _image.PixelWidth, ph = _image.PixelHeight;
            if (_fit == ImageFit.None && !_width.HasValue && !_height.HasValue)
                return (Math.Min(pw, available), ph * Math.Min(1f, available / pw));

            float w = Math.Min(_width ?? available, available);
            float h = _height ?? w * ph / pw;
            if (!_width.HasValue && _height.HasValue)
            {
                // Only a height was given: derive the width from the aspect ratio.
                w = Math.Min(h * pw / ph, available);
                if (h * pw / ph > available) h = w * ph / pw;
            }
            return (w, h);
        }

        public override Fit Measure(PdfCanvas canvas, float width, float available)
        {
            var (w, h) = BoxSize(width);
            if (h > available + Epsilon) return Fit.Wrap;
            return Fit.Full(new ImageElement(this, w), h);
        }

        public override float MaxContentWidth(PdfCanvas canvas, float width) => BoxSize(width).Width;

        public override void Draw(PdfCanvas canvas, float x, float y, float width, float height)
        {
            float bw = _boxWidth >= 0f ? _boxWidth : BoxSize(width).Width;
            float free = Math.Max(0f, width - bw);
            x += _alignment == TextAlignment.Right ? free : _alignment == TextAlignment.Center ? free / 2f : 0f;

            float pw = _image.PixelWidth, ph = _image.PixelHeight;
            float dw = bw, dh = height;
            switch (_fit)
            {
                case ImageFit.Contain:
                {
                    float scale = Math.Min(bw / pw, height / ph);
                    dw = pw * scale; dh = ph * scale;
                    break;
                }
                case ImageFit.Cover:
                {
                    float scale = Math.Max(bw / pw, height / ph);
                    dw = pw * scale; dh = ph * scale;
                    break;
                }
                case ImageFit.None:
                    dw = pw; dh = ph;
                    break;
            }

            float dx = x + (bw - dw) / 2f, dy = y + (height - dh) / 2f;
            bool clip = dw > bw + Epsilon || dh > height + Epsilon;
            if (clip) canvas.SaveState().ClipRectangle(x, y, bw, height);
            canvas.DrawImage(_image, dx, dy, dw, dh);
            if (clip) canvas.RestoreState();
        }
    }
}
