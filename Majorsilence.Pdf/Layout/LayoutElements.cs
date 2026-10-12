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

        /// <summary>
        /// Extra width given to shrink-to-fit text: line breaking sums per-character widths, so a line
        /// measured as a whole string can differ by a fraction of a point and would otherwise re-wrap.
        /// </summary>
        internal const float WidthSlack = 0.5f;

        /// <summary>Measure at <paramref name="width"/> given <paramref name="available"/> points of height.</summary>
        public abstract Fit Measure(PdfCanvas canvas, float width, float available);

        /// <summary>
        /// The widest this element wants to be when laid out in <paramref name="width"/>; used by
        /// shrink-to-fit boxes and inlined items. Defaults to the full width.
        /// </summary>
        public virtual float MaxContentWidth(PdfCanvas canvas, float width) => width;

        /// <summary>
        /// The element to use for the next fragment of a repeating container (a decoration's header
        /// or footer): show-once content disappears, skip-once content appears. Defaults to itself.
        /// </summary>
        public virtual LayoutElement NextFragment() => this;

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

        public override float MaxContentWidth(PdfCanvas canvas, float width) => 0f;

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

    /// <summary>
    /// A table that splits between rows. When the table has a header row (a header background is
    /// set) the header is repeated at the top of every continuation.
    /// </summary>
    internal sealed class TableElement : LayoutElement
    {
        private readonly PdfTable _table;
        public TableElement(PdfTable table) { _table = table; }

        public override float MaxContentWidth(PdfCanvas canvas, float width) => Math.Min(width, _table.TotalWidth);

        public override Fit Measure(PdfCanvas canvas, float width, float available)
        {
            float full = canvas.MeasureTableHeight(_table);
            if (full <= available + Epsilon) return Fit.Full(this, full);

            var rows = _table.Rows;
            var grid = TableGrid.Place(_table);
            int headerRows = _table.HeaderBackground.HasValue ? TableGrid.HeaderRowCount(grid) : 0;
            int minRows = headerRows + 1;
            if (rows.Count <= minRows) return Fit.Wrap;

            // Largest prefix of rows (at least the header plus one data row) that fits.
            int lo = minRows, hi = rows.Count - 1, best = 0;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                float h = canvas.MeasureTableHeight(_table.WithRows(Slice(rows, 0, mid)));
                if (h <= available + Epsilon) { best = mid; lo = mid + 1; }
                else hi = mid - 1;
            }
            // A row-spanning cell must not be cut in two: back up to a row boundary it does not cross.
            while (best >= minRows && !TableGrid.CanSplitBefore(grid, best)) best--;
            if (best < minRows) return Fit.Wrap;

            var headRows = Slice(rows, 0, best);
            var headTable = _table.WithRows(headRows);
            var tailRows = new List<PdfTableRow>();
            tailRows.AddRange(Slice(rows, 0, headerRows)); // the header repeats on the continuation
            tailRows.AddRange(Slice(rows, best, rows.Count));
            return Fit.Partial(new TableElement(headTable), canvas.MeasureTableHeight(headTable),
                new TableElement(_table.WithRows(tailRows)));
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

        public override float MaxContentWidth(PdfCanvas canvas, float width)
        {
            float max = 0f;
            foreach (var child in _children)
                max = Math.Max(max, child.MaxContentWidth(canvas, width));
            return max;
        }

        public override LayoutElement NextFragment()
        {
            var next = new LayoutElement[_children.Count];
            bool changed = false;
            for (int i = 0; i < next.Length; i++)
            {
                next[i] = _children[i].NextFragment();
                changed |= !ReferenceEquals(next[i], _children[i]);
            }
            return changed ? new StackElement(next) : this;
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

        public override LayoutElement NextFragment()
        {
            var next = new (LayoutColumnWidth, StackElement)[_columns.Count];
            bool changed = false;
            for (int i = 0; i < next.Length; i++)
            {
                var content = (StackElement)_columns[i].Content.NextFragment();
                next[i] = (_columns[i].Width, content);
                changed |= !ReferenceEquals(content, _columns[i].Content);
            }
            return changed ? new RowElement(next) : this;
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
        private readonly float _resolvedWidth = -1f; // set on measured heads so Draw does not re-measure

        public BoxElement(LayoutBoxStyle style, LayoutElement content, bool padTop = true, bool padBottom = true,
            float resolvedWidth = -1f)
        {
            _style = style; _content = content; _padTop = padTop; _padBottom = padBottom;
            _resolvedWidth = resolvedWidth;
        }

        private float Left => _style.PaddingLeft + _style.BorderWidth;
        private float Right => _style.PaddingRight + _style.BorderWidth;
        private float Top => (_padTop ? _style.PaddingTop : 0f) + _style.BorderWidth;
        private float Bottom => (_padBottom ? _style.PaddingBottom : 0f) + _style.BorderWidth;

        private float InsetsX => Left + Right;

        private float BoxWidth(PdfCanvas canvas, float width)
        {
            if (_resolvedWidth >= 0f) return Math.Min(_resolvedWidth, width);
            float w = _style.Width ?? (_style.IsShrink
                ? _content.MaxContentWidth(canvas, Math.Max(0f, width - InsetsX)) + InsetsX
                : width);
            if (_style.MinWidth > 0f) w = Math.Max(w, _style.MinWidth);
            if (_style.MaxWidth > 0f) w = Math.Min(w, _style.MaxWidth);
            return Math.Min(w, width);
        }

        private float OffsetX(float width, float boxWidth)
        {
            float free = width - boxWidth;
            return _style.Alignment == TextAlignment.Right ? free
                 : _style.Alignment == TextAlignment.Center ? free / 2f
                 : 0f;
        }

        public override float MaxContentWidth(PdfCanvas canvas, float width) => BoxWidth(canvas, width);

        public override LayoutElement NextFragment() => new BoxElement(_style, _content.NextFragment(), _padTop, _padBottom);

        public override Fit Measure(PdfCanvas canvas, float width, float available)
        {
            float boxWidth = BoxWidth(canvas, width);
            float innerWidth = Math.Max(0f, boxWidth - InsetsX);
            float limit = _style.MaxHeight > 0f ? Math.Min(available, _style.MaxHeight) : available;
            float minHeight = _padBottom ? _style.MinHeight : 0f;
            var fit = _content.Measure(canvas, innerWidth, limit - Top - Bottom);
            switch (fit.Kind)
            {
                case FitKind.Empty:
                    // An empty box still occupies its minimum height (e.g. a coloured bar).
                    if (minHeight > 0f)
                        return minHeight <= limit + Epsilon
                            ? Fit.Full(new BoxElement(_style, new StackElement(Array.Empty<LayoutElement>()), _padTop, _padBottom, boxWidth), minHeight)
                            : Fit.Wrap;
                    return Fit.Empty;
                case FitKind.Full:
                    float full = Math.Max(fit.Height + Top + Bottom, minHeight);
                    if (full > limit + Epsilon) return Fit.Wrap;
                    return Fit.Full(new BoxElement(_style, fit.Head!, _padTop, _padBottom, boxWidth), full);
                case FitKind.Partial when !_style.IsKeepTogether:
                    // The head loses the bottom padding, the tail the top padding.
                    float headHeight = fit.Height + Top + _style.BorderWidth;
                    if (headHeight > limit + Epsilon) return Fit.Wrap;
                    return Fit.Partial(
                        new BoxElement(_style, fit.Head!, _padTop, padBottom: false, resolvedWidth: boxWidth), headHeight,
                        new BoxElement(_style, fit.Tail!, padTop: false, _padBottom));
                default:
                    return Fit.Wrap;
            }
        }

        public override void Draw(PdfCanvas canvas, float x, float y, float width, float height)
        {
            float boxWidth = BoxWidth(canvas, width);
            x += OffsetX(width, boxWidth);
            width = boxWidth;
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

        public override float MaxContentWidth(PdfCanvas canvas, float width) =>
            _condition() ? _content.MaxContentWidth(canvas, width) : 0f;

        public override LayoutElement NextFragment() => new ConditionalElement(_condition, _content.NextFragment());

        public override void Draw(PdfCanvas canvas, float x, float y, float width, float height) =>
            _content.Draw(canvas, x, y, width, height);
    }
}

namespace Majorsilence.Pdf.Layout
{
    /// <summary>Zero-height marker that registers a bookmark and/or named destination where it lands.</summary>
    internal sealed class AnchorElement : LayoutElement
    {
        private readonly string? _bookmark;
        private readonly int _level;
        private readonly string? _destination;

        public AnchorElement(string? bookmark, int level, string? destination)
        {
            _bookmark = bookmark; _level = level; _destination = destination;
        }

        public override Fit Measure(PdfCanvas canvas, float width, float available) => Fit.Full(this, 0f);

        public override void Draw(PdfCanvas canvas, float x, float y, float width, float height)
        {
            if (_bookmark != null) canvas.AddBookmark(_bookmark, y, _level);
            if (_destination != null) canvas.AddDestination(_destination, x, y);
        }
    }
}
