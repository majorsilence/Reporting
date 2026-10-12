// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;
using System.Collections.Generic;

namespace Majorsilence.Pdf.Layout
{
    /// <summary>
    /// Splitting rules shared by the text elements: given N lines of which K fit, how many go on
    /// this page. Applies orphan/widow control (never leave a single line stranded at the bottom
    /// of a page or the top of the next) unless <see cref="LayoutContext.RelaxSplitting"/> is set.
    /// </summary>
    internal static class LineSplit
    {
        /// <returns>Lines to keep on this page; 0 means move the whole element to the next page.</returns>
        public static int Choose(int total, int fits)
        {
            if (fits >= total) return total;
            if (fits <= 0) return 0;
            if (LayoutContext.RelaxSplitting) return fits;
            if (fits < 2) return 0;                   // orphan
            if (total - fits == 1) return fits > 2 ? fits - 1 : 0; // widow
            return fits;
        }
    }

    /// <summary>The wrapped lines of one text, cached per width and shared by every fragment of a split.</summary>
    internal sealed class TextSource
    {
        private readonly Dictionary<float, IReadOnlyList<TextLine>> _byWidth = new Dictionary<float, IReadOnlyList<TextLine>>();
        public string Text { get; }
        public TextStyle Style { get; }

        public TextSource(string text, TextStyle style) { Text = text; Style = style; }

        public IReadOnlyList<TextLine> Lines(PdfCanvas canvas, float width)
        {
            if (!_byWidth.TryGetValue(width, out var lines))
            {
                lines = canvas.BreakLines(Text, width, Style);
                _byWidth[width] = lines;
            }
            return lines;
        }
    }

    /// <summary>Word-wrapped text that splits between lines.</summary>
    internal sealed class TextElement : LayoutElement
    {
        private readonly TextSource _source;
        private readonly int _first;
        private readonly int _count; // -1 = every line from _first

        public TextElement(string text, TextStyle style) : this(new TextSource(text, style), 0, -1) { }

        private TextElement(TextSource source, int first, int count)
        {
            _source = source; _first = first; _count = count;
        }

        private static float HeightOf(TextStyle style, float lineHeight, int lines) =>
            lines <= 0 ? 0f : style.FontSize + (lines - 1) * lineHeight;

        public override Fit Measure(PdfCanvas canvas, float width, float available)
        {
            var lines = _source.Lines(canvas, width);
            int n = (_count < 0 ? lines.Count - _first : _count);
            if (n <= 0) return Fit.Empty;

            var style = _source.Style;
            float lh = canvas.GetLineHeight(style);
            float full = HeightOf(style, lh, n);
            if (full <= available + Epsilon) return Fit.Full(new TextElement(_source, _first, n), full);

            float room = available + Epsilon - style.FontSize;
            int fits = room < 0f ? 0 : (int)Math.Floor(room / lh) + 1;
            int keep = LineSplit.Choose(n, fits);
            if (keep <= 0) return Fit.Wrap;

            return Fit.Partial(new TextElement(_source, _first, keep), HeightOf(style, lh, keep),
                new TextElement(_source, _first + keep, n - keep));
        }

        public override float MaxContentWidth(PdfCanvas canvas, float width)
        {
            var lines = _source.Lines(canvas, width);
            float max = 0f;
            int end = _count < 0 ? lines.Count : _first + _count;
            for (int i = _first; i < end; i++) if (lines[i].Width > max) max = lines[i].Width;
            return Math.Min(width, max + WidthSlack);
        }

        public override void Draw(PdfCanvas canvas, float x, float y, float width, float height)
        {
            var lines = _source.Lines(canvas, width);
            var style = _source.Style;
            float lh = canvas.GetLineHeight(style);
            int end = _count < 0 ? lines.Count : _first + _count;
            float drawX = style.Alignment == TextAlignment.Right ? x + width
                        : style.Alignment == TextAlignment.Center ? x + width / 2f
                        : x;
            for (int i = _first; i < end; i++)
            {
                if (lines[i].Text.Length > 0)
                    canvas.DrawText(lines[i].Text, drawX, y + style.FontSize + (i - _first) * lh, style);
            }
        }
    }

    // ── rich text ─────────────────────────────────────────────────────────────

    internal enum SpanKind { Text, LineBreak, PageNumber, TotalPages }

    /// <summary>One word-sized piece of rich text: text plus its style and optional link target.</summary>
    internal readonly struct SpanToken
    {
        public SpanKind Kind { get; }
        public string Text { get; }
        public TextStyle Style { get; }
        public string? Uri { get; }
        public string? Destination { get; }

        public SpanToken(SpanKind kind, string text, TextStyle style, string? uri, string? destination)
        {
            Kind = kind; Text = text; Style = style; Uri = uri; Destination = destination;
        }

        public string DisplayText => Kind == SpanKind.PageNumber ? LayoutContext.PageNumber.ToString()
                                   : Kind == SpanKind.TotalPages ? LayoutContext.TotalPages.ToString()
                                   : Text;
    }

    internal sealed class RichLine
    {
        public int FirstToken, TokenCount;
        public float Width, FontSize, Height;
        public float[] TokenWidths = Array.Empty<float>();
    }

    internal sealed class RichSource
    {
        private readonly Dictionary<float, IReadOnlyList<RichLine>> _byWidth = new Dictionary<float, IReadOnlyList<RichLine>>();
        public IReadOnlyList<SpanToken> Tokens { get; }
        public TextAlignment Alignment { get; }

        public RichSource(IReadOnlyList<SpanToken> tokens, TextAlignment alignment)
        {
            Tokens = tokens; Alignment = alignment;
        }

        // Page-number placeholders are measured as two digits: the total is not known while
        // measuring, and sizing them up front is what lets pagination run before drawing.
        private static float WidthOf(PdfCanvas canvas, SpanToken t) =>
            canvas.MeasureTextWidth(t.Kind == SpanKind.Text ? t.Text : "00", t.Style);

        public IReadOnlyList<RichLine> Lines(PdfCanvas canvas, float width)
        {
            if (_byWidth.TryGetValue(width, out var cached)) return cached;

            var lines = new List<RichLine>();
            var widths = new List<float>();
            int first = 0;
            float lineWidth = 0f, maxFont = 0f;

            void Flush(int endExclusive)
            {
                int count = endExclusive - first;
                if (count > 0)
                {
                    float font = maxFont > 0f ? maxFont : 11f;
                    lines.Add(new RichLine
                    {
                        FirstToken = first, TokenCount = count, Width = lineWidth, FontSize = font,
                        Height = font * 1.2f, TokenWidths = widths.ToArray(),
                    });
                }
                widths.Clear();
                first = endExclusive;
                lineWidth = 0f; maxFont = 0f;
            }

            for (int i = 0; i < Tokens.Count; i++)
            {
                var t = Tokens[i];
                if (t.Kind == SpanKind.LineBreak)
                {
                    Flush(i);          // a break on an empty line adds nothing
                    first = i + 1;
                    continue;
                }

                float w = WidthOf(canvas, t);
                if (lineWidth + w > width && widths.Count > 0) Flush(i);
                widths.Add(w);
                lineWidth += w;
                if (t.Style.FontSize > maxFont) maxFont = t.Style.FontSize;
            }
            Flush(Tokens.Count);

            _byWidth[width] = lines;
            return lines;
        }
    }

    /// <summary>Mixed-style text (bold/colour/link runs, page-number placeholders) that splits between lines.</summary>
    internal sealed class RichTextElement : LayoutElement
    {
        private readonly RichSource _source;
        private readonly int _first;
        private readonly int _count; // -1 = every line from _first

        public RichTextElement(RichSource source) : this(source, 0, -1) { }

        private RichTextElement(RichSource source, int first, int count)
        {
            _source = source; _first = first; _count = count;
        }

        public override Fit Measure(PdfCanvas canvas, float width, float available)
        {
            var lines = _source.Lines(canvas, width);
            int n = _count < 0 ? lines.Count - _first : _count;
            if (n <= 0) return Fit.Empty;

            float total = 0f;
            for (int i = 0; i < n; i++) total += lines[_first + i].Height;
            if (total <= available + Epsilon) return Fit.Full(new RichTextElement(_source, _first, n), total);

            float used = 0f;
            int fits = 0;
            while (fits < n && used + lines[_first + fits].Height <= available + Epsilon)
                used += lines[_first + fits++].Height;

            int keep = LineSplit.Choose(n, fits);
            if (keep <= 0) return Fit.Wrap;
            float headHeight = 0f;
            for (int i = 0; i < keep; i++) headHeight += lines[_first + i].Height;
            return Fit.Partial(new RichTextElement(_source, _first, keep), headHeight,
                new RichTextElement(_source, _first + keep, n - keep));
        }

        public override float MaxContentWidth(PdfCanvas canvas, float width)
        {
            var lines = _source.Lines(canvas, width);
            float max = 0f;
            int end = _count < 0 ? lines.Count : _first + _count;
            for (int i = _first; i < end; i++) if (lines[i].Width > max) max = lines[i].Width;
            return Math.Min(width, max + WidthSlack);
        }

        public override void Draw(PdfCanvas canvas, float x, float y, float width, float height)
        {
            var lines = _source.Lines(canvas, width);
            int end = _count < 0 ? lines.Count : _first + _count;
            float top = y;
            for (int i = _first; i < end; i++)
            {
                var line = lines[i];
                float free = Math.Max(0f, width - line.Width);
                float drawX = x + (_source.Alignment == TextAlignment.Right ? free
                                 : _source.Alignment == TextAlignment.Center ? free / 2f : 0f);
                float baseline = top + line.FontSize;
                for (int k = 0; k < line.TokenCount; k++)
                {
                    var token = _source.Tokens[line.FirstToken + k];
                    float w = line.TokenWidths[k];
                    string text = token.DisplayText;
                    // Words keep their trailing space so text extractors see real word boundaries.
                    if (text.Length > 0) canvas.DrawText(text, drawX, baseline, token.Style);
                    if (token.Uri != null)
                        canvas.AddLink(drawX, baseline - line.FontSize, w, line.FontSize * 1.2f, token.Uri);
                    else if (token.Destination != null)
                        canvas.AddInternalLink(drawX, baseline - line.FontSize, w, line.FontSize * 1.2f, token.Destination);
                    drawX += w;
                }
                top += line.Height;
            }
        }
    }
}
