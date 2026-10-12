// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;
using System.Collections.Generic;

namespace Majorsilence.Pdf.Layout
{
    internal enum FitKind
    {
        /// <summary>Nothing to draw; takes no space.</summary>
        Empty,
        /// <summary>Does not fit in the available height; try again on a fresh page.</summary>
        Wrap,
        /// <summary>Fits entirely.</summary>
        Full,
        /// <summary>The head fits; the tail must continue on the next page.</summary>
        Partial,
    }

    /// <summary>
    /// The outcome of <see cref="LayoutElement.Measure"/>. <see cref="Head"/> is the element to
    /// draw into <see cref="Height"/> points; <see cref="Tail"/> (Partial only) is what is left.
    /// </summary>
    internal readonly struct Fit
    {
        public FitKind Kind { get; }
        public float Height { get; }
        public LayoutElement? Head { get; }
        public LayoutElement? Tail { get; }

        private Fit(FitKind kind, float height, LayoutElement? head, LayoutElement? tail)
        {
            Kind = kind; Height = height; Head = head; Tail = tail;
        }

        public static Fit Empty => new Fit(FitKind.Empty, 0f, null, null);
        public static Fit Wrap => new Fit(FitKind.Wrap, 0f, null, null);
        public static Fit Full(LayoutElement head, float height) => new Fit(FitKind.Full, height, head, null);
        public static Fit Partial(LayoutElement head, float height, LayoutElement tail) =>
            new Fit(FitKind.Partial, height, head, tail);
    }

    /// <summary>
    /// A node of the layout tree. Elements are immutable, so measuring is repeatable and has no
    /// side effects; splitting produces new elements instead of mutating the original.
    /// </summary>
    internal abstract class LayoutElement
    {
        internal const float Epsilon = 0.01f;

        /// <summary>Measure at <paramref name="width"/> given <paramref name="available"/> points of height.</summary>
        public abstract Fit Measure(PdfCanvas canvas, float width, float available);

        /// <summary>Draw a head previously returned by <see cref="Measure"/> into the given box.</summary>
        public abstract void Draw(PdfCanvas canvas, float x, float y, float width, float height);
    }

    internal sealed class PageBreakElement : LayoutElement
    {
        public override Fit Measure(PdfCanvas canvas, float width, float available) => Fit.Empty;
        public override void Draw(PdfCanvas canvas, float x, float y, float width, float height) { }
    }

    internal sealed class SpacerElement : LayoutElement
    {
        private readonly float _height;
        public SpacerElement(float height) { _height = height; }

        public override Fit Measure(PdfCanvas canvas, float width, float available) =>
            _height <= available + Epsilon ? Fit.Full(this, _height) : Fit.Wrap;

        public override void Draw(PdfCanvas canvas, float x, float y, float width, float height) { }
    }

    internal sealed class LineElement : LayoutElement
    {
        private readonly StrokeStyle? _style;
        private readonly float _gapAfter;
        public LineElement(StrokeStyle? style, float gapAfter) { _style = style; _gapAfter = gapAfter; }

        public override Fit Measure(PdfCanvas canvas, float width, float available) =>
            _gapAfter <= available + Epsilon ? Fit.Full(this, _gapAfter) : Fit.Wrap;

        public override void Draw(PdfCanvas canvas, float x, float y, float width, float height) =>
            canvas.DrawLine(x, y, x + width, y, _style);
    }

    internal sealed class CanvasElement : LayoutElement
    {
        private readonly Action<PdfCanvas, LayoutRect> _draw;
        private readonly float _height;
        public CanvasElement(Action<PdfCanvas, LayoutRect> draw, float height) { _draw = draw; _height = height; }

        public override Fit Measure(PdfCanvas canvas, float width, float available) =>
            _height <= available + Epsilon ? Fit.Full(this, _height) : Fit.Wrap;

        public override void Draw(PdfCanvas canvas, float x, float y, float width, float height) =>
            _draw(canvas, new LayoutRect(x, y, width, _height));
    }

    /// <summary>Word-wrapped text that splits between lines.</summary>
    internal sealed class TextElement : LayoutElement
    {
        private readonly string _text;
        private readonly TextStyle _style;

        public TextElement(string text, TextStyle style) { _text = text; _style = style; }

        public override Fit Measure(PdfCanvas canvas, float width, float available)
        {
            if (_text.Length == 0) return Fit.Empty;
            float height = canvas.MeasureTextBoxHeight(_text, width, _style);
            if (height <= available + Epsilon) return Fit.Full(this, height);

            int count = canvas.FitTextBox(_text, width, available + Epsilon, _style);
            if (count <= 0) return Fit.Wrap;
            if (count >= _text.Length) return Fit.Full(this, height);

            var head = new TextElement(_text.Substring(0, count), _style);
            var tail = new TextElement(_text.Substring(count), _style);
            return Fit.Partial(head, canvas.MeasureTextBoxHeight(head._text, width, _style), tail);
        }

        public override void Draw(PdfCanvas canvas, float x, float y, float width, float height) =>
            canvas.DrawTextBox(_text, x, y, width, height + 1f, _style);
    }

    /// <summary>
    /// A table that splits between rows. When the table has a header row (a header background is
    /// set) the header is repeated at the top of every continuation.
    /// </summary>
    internal sealed class TableElement : LayoutElement
    {
        private readonly PdfTable _table;
        public TableElement(PdfTable table) { _table = table; }

        public override Fit Measure(PdfCanvas canvas, float width, float available)
        {
            float full = canvas.MeasureTableHeight(_table);
            if (full <= available + Epsilon) return Fit.Full(this, full);

            var rows = _table.Rows;
            bool header = _table.HeaderBackground.HasValue;
            int minRows = header ? 2 : 1;
            if (rows.Count <= minRows) return Fit.Wrap;

            // Largest prefix of rows (at least the header plus one data row) that fits.
            int lo = 0, hi = rows.Count - 1;
            float bestHeight = 0f;
            int best = 0;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                if (mid < minRows) { lo = mid + 1; continue; }
                float h = canvas.MeasureTableHeight(_table.WithRows(Slice(rows, 0, mid)));
                if (h <= available + Epsilon) { best = mid; bestHeight = h; lo = mid + 1; }
                else hi = mid - 1;
            }
            if (best == 0) return Fit.Wrap;

            var head = _table.WithRows(Slice(rows, 0, best));
            var tailRows = new List<PdfTableRow>();
            if (header) tailRows.Add(rows[0]);
            tailRows.AddRange(Slice(rows, best, rows.Count));
            return Fit.Partial(new TableElement(head), bestHeight, new TableElement(_table.WithRows(tailRows)));
        }

        private static List<PdfTableRow> Slice(IReadOnlyList<PdfTableRow> rows, int start, int end)
        {
            var list = new List<PdfTableRow>(end - start);
            for (int i = start; i < end; i++) list.Add(rows[i]);
            return list;
        }

        public override void Draw(PdfCanvas canvas, float x, float y, float width, float height) =>
            canvas.DrawTable(_table, x, y, out _);
    }

    /// <summary>A vertical stack of children. Splits between (and inside) its children.</summary>
    internal sealed class StackElement : LayoutElement
    {
        private readonly IReadOnlyList<LayoutElement> _children;
        private readonly IReadOnlyList<(LayoutElement Element, float Offset, float Height)>? _placed;

        public StackElement(IReadOnlyList<LayoutElement> children) { _children = children; }

        private StackElement(IReadOnlyList<(LayoutElement, float, float)> placed)
        {
            _children = Array.Empty<LayoutElement>();
            _placed = placed;
        }

        public override Fit Measure(PdfCanvas canvas, float width, float available)
        {
            var placed = new List<(LayoutElement, float, float)>();
            float y = 0f;
            for (int i = 0; i < _children.Count; i++)
            {
                var fit = _children[i].Measure(canvas, width, available - y);
                switch (fit.Kind)
                {
                    case FitKind.Empty:
                        continue;
                    case FitKind.Full:
                        placed.Add((fit.Head!, y, fit.Height));
                        y += fit.Height;
                        continue;
                    case FitKind.Partial:
                        placed.Add((fit.Head!, y, fit.Height));
                        return Fit.Partial(new StackElement(placed), y + fit.Height,
                            new StackElement(Remainder(fit.Tail!, i + 1)));
                    default: // Wrap
                        if (placed.Count == 0) return Fit.Wrap;
                        return Fit.Partial(new StackElement(placed), y, new StackElement(Remainder(null, i)));
                }
            }
            if (placed.Count == 0) return Fit.Empty;
            return Fit.Full(new StackElement(placed), y);
        }

        private List<LayoutElement> Remainder(LayoutElement? first, int fromIndex)
        {
            var rest = new List<LayoutElement>();
            if (first != null) rest.Add(first);
            for (int i = fromIndex; i < _children.Count; i++) rest.Add(_children[i]);
            return rest;
        }

        public override void Draw(PdfCanvas canvas, float x, float y, float width, float height)
        {
            if (_placed == null) return;
            foreach (var (element, offset, h) in _placed)
                element.Draw(canvas, x, y + offset, width, h);
        }
    }

    /// <summary>Side-by-side columns; the row is as tall as its tallest column and is atomic.</summary>
    internal sealed class RowElement : LayoutElement
    {
        private readonly IReadOnlyList<(LayoutColumnWidth Width, StackElement Content)> _columns;
        private readonly IReadOnlyList<(float X, float Width, float Height, LayoutElement Content)>? _placed;

        public RowElement(IReadOnlyList<(LayoutColumnWidth, StackElement)> columns) { _columns = columns; }

        private RowElement(IReadOnlyList<(float, float, float, LayoutElement)> placed)
        {
            _columns = Array.Empty<(LayoutColumnWidth, StackElement)>();
            _placed = placed;
        }

        public override Fit Measure(PdfCanvas canvas, float width, float available)
        {
            var placed = new List<(float, float, float, LayoutElement)>();
            float x = 0f, max = 0f;
            foreach (var (spec, content) in _columns)
            {
                float w = spec.IsFraction ? width * spec.Value : spec.Value;
                var fit = content.Measure(canvas, w, float.PositiveInfinity);
                if (fit.Kind == FitKind.Full)
                {
                    placed.Add((x, w, fit.Height, fit.Head!));
                    if (fit.Height > max) max = fit.Height;
                }
                x += w;
            }
            if (placed.Count == 0) return Fit.Empty;
            if (max > available + Epsilon) return Fit.Wrap;
            return Fit.Full(new RowElement(placed), max);
        }

        public override void Draw(PdfCanvas canvas, float x, float y, float width, float height)
        {
            if (_placed == null) return;
            foreach (var (cx, cw, ch, content) in _placed)
                content.Draw(canvas, x + cx, y, cw, ch);
        }
    }

    /// <summary>
    /// A container with optional padding, background and border. Splits with its content unless
    /// <see cref="LayoutBoxStyle.IsKeepTogether"/> is set; on a split the top padding stays on
    /// the first fragment and the bottom padding on the last.
    /// </summary>
    internal sealed class BoxElement : LayoutElement
    {
        private readonly LayoutBoxStyle _style;
        private readonly LayoutElement _content;
        private readonly bool _padTop;
        private readonly bool _padBottom;

        public BoxElement(LayoutBoxStyle style, LayoutElement content, bool padTop = true, bool padBottom = true)
        {
            _style = style; _content = content; _padTop = padTop; _padBottom = padBottom;
        }

        private float Left => _style.PaddingLeft + _style.BorderWidth;
        private float Right => _style.PaddingRight + _style.BorderWidth;
        private float Top => (_padTop ? _style.PaddingTop : 0f) + _style.BorderWidth;
        private float Bottom => (_padBottom ? _style.PaddingBottom : 0f) + _style.BorderWidth;

        private float BoxWidth(float width) =>
            _style.Width.HasValue ? Math.Min(width, _style.Width.Value) : width;

        private float OffsetX(float width)
        {
            float free = width - BoxWidth(width);
            return _style.Alignment == TextAlignment.Right ? free
                 : _style.Alignment == TextAlignment.Center ? free / 2f
                 : 0f;
        }

        public override Fit Measure(PdfCanvas canvas, float width, float available)
        {
            float innerWidth = Math.Max(0f, BoxWidth(width) - Left - Right);
            var fit = _content.Measure(canvas, innerWidth, available - Top - Bottom);
            switch (fit.Kind)
            {
                case FitKind.Empty:
                    // An empty box still occupies its minimum height (e.g. a coloured bar).
                    if (_style.MinHeight > 0f && _padBottom)
                        return _style.MinHeight <= available + Epsilon
                            ? Fit.Full(new BoxElement(_style, new StackElement(Array.Empty<LayoutElement>()), _padTop, _padBottom), _style.MinHeight)
                            : Fit.Wrap;
                    return Fit.Empty;
                case FitKind.Full:
                    float full = Math.Max(fit.Height + Top + Bottom, _padBottom ? _style.MinHeight : 0f);
                    if (full > available + Epsilon) return Fit.Wrap;
                    return Fit.Full(new BoxElement(_style, fit.Head!, _padTop, _padBottom), full);
                case FitKind.Partial when !_style.IsKeepTogether:
                    // The head loses the bottom padding, the tail the top padding.
                    float headHeight = fit.Height + Top + _style.BorderWidth;
                    if (headHeight > available + Epsilon) return Fit.Wrap;
                    return Fit.Partial(
                        new BoxElement(_style, fit.Head!, _padTop, padBottom: false), headHeight,
                        new BoxElement(_style, fit.Tail!, padTop: false, _padBottom));
                default:
                    return Fit.Wrap;
            }
        }

        public override void Draw(PdfCanvas canvas, float x, float y, float width, float height)
        {
            x += OffsetX(width);
            width = BoxWidth(width);
            if (_style.Background.HasValue || _style.BorderWidth > 0f)
            {
                var shape = ShapeStyle.Empty;
                if (_style.Background.HasValue) shape = shape.WithFill(_style.Background.Value);
                if (_style.BorderWidth > 0f && _style.BorderColor.HasValue)
                    shape = shape.WithStroke(_style.BorderColor.Value, _style.BorderWidth);
                float half = _style.BorderWidth / 2f;
                canvas.DrawRectangle(x + half, y + half, width - _style.BorderWidth, height - _style.BorderWidth, shape);
            }
            float innerHeight = Math.Max(0f, height - Top - Bottom);
            _content.Draw(canvas, x + Left, y + Top, Math.Max(0f, width - Left - Right), innerHeight);
        }
    }
}

namespace Majorsilence.Pdf.Layout
{
    /// <summary>Content included only when a condition, evaluated at pagination time, is true.</summary>
    internal sealed class ConditionalElement : LayoutElement
    {
        private readonly Func<bool> _condition;
        private readonly LayoutElement _content;

        public ConditionalElement(Func<bool> condition, LayoutElement content)
        {
            _condition = condition; _content = content;
        }

        public override Fit Measure(PdfCanvas canvas, float width, float available) =>
            _condition() ? _content.Measure(canvas, width, available) : Fit.Empty;

        public override void Draw(PdfCanvas canvas, float x, float y, float width, float height) =>
            _content.Draw(canvas, x, y, width, height);
    }
}
