// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;
using System.Collections.Generic;

namespace Majorsilence.Pdf.Layout
{
    /// <summary>Takes no space and draws nothing.</summary>
    internal sealed class EmptyElement : LayoutElement
    {
        public static readonly EmptyElement Instance = new EmptyElement();
        private EmptyElement() { }
        public override Fit Measure(PdfCanvas canvas, float width, float available) => Fit.Empty;
        public override float MaxContentWidth(PdfCanvas canvas, float width) => 0f;
        public override void Draw(PdfCanvas canvas, float x, float y, float width, float height) { }
    }

    /// <summary>Moves to the next page unless at least <c>minimum</c> points remain.</summary>
    internal sealed class EnsureSpaceElement : LayoutElement
    {
        private readonly float _minimum;
        public EnsureSpaceElement(float minimum) { _minimum = minimum; }

        public override Fit Measure(PdfCanvas canvas, float width, float available) =>
            available + Epsilon < _minimum ? Fit.Wrap : Fit.Empty;

        public override void Draw(PdfCanvas canvas, float x, float y, float width, float height) { }
    }

    /// <summary>
    /// Content that appears on the first fragment only (inside a repeating <see cref="DecorationElement"/>
    /// header or footer) or, with <c>skipFirst</c>, on every fragment but the first.
    /// </summary>
    internal sealed class OnceElement : LayoutElement
    {
        private readonly LayoutElement _content;
        private readonly bool _skipFirst;
        public OnceElement(LayoutElement content, bool skipFirst) { _content = content; _skipFirst = skipFirst; }

        public override Fit Measure(PdfCanvas canvas, float width, float available) =>
            _skipFirst ? Fit.Empty : _content.Measure(canvas, width, available);

        public override float MaxContentWidth(PdfCanvas canvas, float width) =>
            _skipFirst ? 0f : _content.MaxContentWidth(canvas, width);

        // Show-once content is gone after the first fragment; skip-once content arrives with it.
        public override LayoutElement NextFragment() => _skipFirst ? _content : EmptyElement.Instance;

        public override void Draw(PdfCanvas canvas, float x, float y, float width, float height) =>
            _content.Draw(canvas, x, y, width, height);
    }

    /// <summary>
    /// A header, a splittable body and a footer. When the body splits across pages the header and
    /// footer repeat on every fragment (use show-once / skip-once inside them to vary that).
    /// </summary>
    internal sealed class DecorationElement : LayoutElement
    {
        private readonly LayoutElement _header, _content, _footer;
        private readonly float _headerHeight, _contentHeight;
        private readonly bool _prepared;

        public DecorationElement(LayoutElement header, LayoutElement content, LayoutElement footer)
        {
            _header = header; _content = content; _footer = footer;
        }

        private DecorationElement(LayoutElement header, float headerHeight, LayoutElement content,
            float contentHeight, LayoutElement footer)
        {
            _header = header; _headerHeight = headerHeight; _content = content;
            _contentHeight = contentHeight; _footer = footer; _prepared = true;
        }

        public override Fit Measure(PdfCanvas canvas, float width, float available)
        {
            var hf = _header.Measure(canvas, width, available);
            if (hf.Kind == FitKind.Wrap || hf.Kind == FitKind.Partial) return Fit.Wrap;
            float h = hf.Kind == FitKind.Full ? hf.Height : 0f;

            var ff = _footer.Measure(canvas, width, available - h);
            if (ff.Kind == FitKind.Wrap || ff.Kind == FitKind.Partial) return Fit.Wrap;
            float f = ff.Kind == FitKind.Full ? ff.Height : 0f;

            var cf = _content.Measure(canvas, width, available - h - f);
            if (cf.Kind == FitKind.Empty) return Fit.Empty;
            if (cf.Kind == FitKind.Wrap) return Fit.Wrap;

            var header = hf.Head ?? EmptyElement.Instance;
            var footer = ff.Head ?? EmptyElement.Instance;
            var head = new DecorationElement(header, h, cf.Head!, cf.Height, footer);
            float total = h + cf.Height + f;
            if (cf.Kind == FitKind.Full) return Fit.Full(head, total);

            var tail = new DecorationElement(_header.NextFragment(), cf.Tail!, _footer.NextFragment());
            return Fit.Partial(head, total, tail);
        }

        public override float MaxContentWidth(PdfCanvas canvas, float width) =>
            Math.Max(_content.MaxContentWidth(canvas, width),
                Math.Max(_header.MaxContentWidth(canvas, width), _footer.MaxContentWidth(canvas, width)));

        public override void Draw(PdfCanvas canvas, float x, float y, float width, float height)
        {
            if (!_prepared) return;
            _header.Draw(canvas, x, y, width, _headerHeight);
            _content.Draw(canvas, x, y + _headerHeight, width, _contentHeight);
            _footer.Draw(canvas, x, y + _headerHeight + _contentHeight, width, height - _headerHeight - _contentHeight);
        }
    }

    /// <summary>
    /// A primary element that sets the size, with other elements drawn in the same rectangle
    /// (behind it when listed first, in front when listed after). Splits follow the primary;
    /// the other layers are clipped to the height the primary fragment occupies.
    /// </summary>
    internal sealed class LayersElement : LayoutElement
    {
        private readonly IReadOnlyList<LayoutElement> _back;
        private readonly LayoutElement _primary;
        private readonly IReadOnlyList<LayoutElement> _front;
        private readonly float _height;
        private readonly bool _prepared;

        public LayersElement(IReadOnlyList<LayoutElement> back, LayoutElement primary, IReadOnlyList<LayoutElement> front)
        {
            _back = back; _primary = primary; _front = front;
        }

        private LayersElement(IReadOnlyList<LayoutElement> back, LayoutElement primary,
            IReadOnlyList<LayoutElement> front, float height)
        {
            _back = back; _primary = primary; _front = front; _height = height; _prepared = true;
        }

        public override Fit Measure(PdfCanvas canvas, float width, float available)
        {
            var pf = _primary.Measure(canvas, width, available);
            if (pf.Kind == FitKind.Empty || pf.Kind == FitKind.Wrap) return pf;

            var backHeads = new List<LayoutElement>();
            var backTails = new List<LayoutElement>();
            var frontHeads = new List<LayoutElement>();
            var frontTails = new List<LayoutElement>();
            SplitLayers(canvas, width, pf.Height, _back, backHeads, backTails);
            SplitLayers(canvas, width, pf.Height, _front, frontHeads, frontTails);

            var head = new LayersElement(backHeads, pf.Head!, frontHeads, pf.Height);
            if (pf.Kind == FitKind.Full) return Fit.Full(head, pf.Height);
            return Fit.Partial(head, pf.Height, new LayersElement(backTails, pf.Tail!, frontTails));
        }

        private static void SplitLayers(PdfCanvas canvas, float width, float height,
            IReadOnlyList<LayoutElement> layers, List<LayoutElement> heads, List<LayoutElement> tails)
        {
            foreach (var layer in layers)
            {
                var fit = layer.Measure(canvas, width, height);
                if (fit.Kind == FitKind.Full || fit.Kind == FitKind.Partial) heads.Add(fit.Head!);
                if (fit.Kind == FitKind.Partial) tails.Add(fit.Tail!);
            }
        }

        public override float MaxContentWidth(PdfCanvas canvas, float width) => _primary.MaxContentWidth(canvas, width);

        public override void Draw(PdfCanvas canvas, float x, float y, float width, float height)
        {
            if (!_prepared) return;
            foreach (var layer in _back) layer.Draw(canvas, x, y, width, _height);
            _primary.Draw(canvas, x, y, width, _height);
            foreach (var layer in _front) layer.Draw(canvas, x, y, width, _height);
        }
    }

    /// <summary>
    /// Items flowing left to right and wrapping onto new lines when the row is full. Each item is
    /// as wide as its content (up to the available width). Splits between lines.
    /// </summary>
    internal sealed class InlinedElement : LayoutElement
    {
        private readonly IReadOnlyList<LayoutElement> _items;
        private readonly float _hSpacing, _vSpacing;
        private readonly TextAlignment _alignment;
        private readonly IReadOnlyList<(LayoutElement Element, float X, float Y, float Width, float Height)>? _placed;

        public InlinedElement(IReadOnlyList<LayoutElement> items, float hSpacing, float vSpacing, TextAlignment alignment)
        {
            _items = items; _hSpacing = hSpacing; _vSpacing = vSpacing; _alignment = alignment;
        }

        private InlinedElement(IReadOnlyList<(LayoutElement, float, float, float, float)> placed)
        {
            _items = Array.Empty<LayoutElement>(); _placed = placed;
        }

        public override Fit Measure(PdfCanvas canvas, float width, float available)
        {
            var placed = new List<(LayoutElement, float, float, float, float)>();
            float y = 0f;
            int index = 0;
            while (index < _items.Count)
            {
                // Gather one line.
                var line = new List<(LayoutElement Element, float Width, float Height)>();
                float x = 0f, lineHeight = 0f;
                int next = index;
                while (next < _items.Count)
                {
                    var item = _items[next];
                    float w = Math.Min(width, item.MaxContentWidth(canvas, width));
                    var fit = item.Measure(canvas, w, float.PositiveInfinity);
                    if (fit.Kind == FitKind.Empty) { next++; continue; }
                    float needed = (line.Count > 0 ? _hSpacing : 0f) + w;
                    if (line.Count > 0 && x + needed > width + Epsilon) break;
                    line.Add((fit.Head!, w, fit.Height));
                    x += needed;
                    lineHeight = Math.Max(lineHeight, fit.Height);
                    next++;
                }
                if (line.Count == 0) { index = next; continue; }

                float top = placed.Count > 0 ? y + _vSpacing : y;
                if (top + lineHeight > available + Epsilon)
                {
                    if (placed.Count == 0) return Fit.Wrap;
                    var rest = new LayoutElement[_items.Count - index];
                    for (int i = index; i < _items.Count; i++) rest[i - index] = _items[i];
                    return Fit.Partial(new InlinedElement(placed), y,
                        new InlinedElement(rest, _hSpacing, _vSpacing, _alignment));
                }

                float free = Math.Max(0f, width - (x));
                float cx = _alignment == TextAlignment.Right ? free : _alignment == TextAlignment.Center ? free / 2f : 0f;
                foreach (var (element, w, h) in line)
                {
                    placed.Add((element, cx, top, w, h));
                    cx += w + _hSpacing;
                }
                y = top + lineHeight;
                index = next;
            }
            if (placed.Count == 0) return Fit.Empty;
            return Fit.Full(new InlinedElement(placed), y);
        }

        public override void Draw(PdfCanvas canvas, float x, float y, float width, float height)
        {
            if (_placed == null) return;
            foreach (var (element, ex, ey, ew, eh) in _placed)
                element.Draw(canvas, x + ex, y + ey, ew, eh);
        }
    }

    /// <summary>
    /// Indents its content and draws a marker (a bullet, a number) in the hanging space beside the
    /// first line. When the content splits, only the first fragment carries the marker.
    /// </summary>
    internal sealed class MarkerElement : LayoutElement
    {
        private readonly string _marker;
        private readonly TextStyle _markerStyle;
        private readonly float _indent;
        private readonly LayoutElement _content;
        private readonly bool _showMarker;

        public MarkerElement(string marker, TextStyle markerStyle, float indent, LayoutElement content,
            bool showMarker = true)
        {
            _marker = marker; _markerStyle = markerStyle; _indent = indent; _content = content;
            _showMarker = showMarker;
        }

        public override Fit Measure(PdfCanvas canvas, float width, float available)
        {
            var fit = _content.Measure(canvas, Math.Max(0f, width - _indent), available);
            switch (fit.Kind)
            {
                case FitKind.Empty:
                case FitKind.Wrap:
                    return fit;
                case FitKind.Full:
                    return Fit.Full(new MarkerElement(_marker, _markerStyle, _indent, fit.Head!, _showMarker), fit.Height);
                default:
                    return Fit.Partial(
                        new MarkerElement(_marker, _markerStyle, _indent, fit.Head!, _showMarker), fit.Height,
                        new MarkerElement(_marker, _markerStyle, _indent, fit.Tail!, showMarker: false));
            }
        }

        public override float MaxContentWidth(PdfCanvas canvas, float width) =>
            _indent + _content.MaxContentWidth(canvas, Math.Max(0f, width - _indent));

        public override LayoutElement NextFragment() =>
            new MarkerElement(_marker, _markerStyle, _indent, _content.NextFragment(), showMarker: false);

        public override void Draw(PdfCanvas canvas, float x, float y, float width, float height)
        {
            if (_showMarker && _marker.Length > 0)
            {
                // Right-align the marker against the content so "9." and "10." line up.
                float w = canvas.MeasureTextWidth(_marker, _markerStyle);
                canvas.DrawText(_marker, x + _indent - 4f - w, y + _markerStyle.FontSize, _markerStyle);
            }
            _content.Draw(canvas, x + _indent, y, Math.Max(0f, width - _indent), height);
        }
    }
}
