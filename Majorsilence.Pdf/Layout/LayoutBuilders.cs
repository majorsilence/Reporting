// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;
using System.Collections.Generic;

namespace Majorsilence.Pdf.Layout
{
    /// <summary>
    /// A reusable piece of layout. Implement <see cref="Compose"/> to describe the content with
    /// the same calls as any column, then add it anywhere with <c>Component(...)</c>.
    /// </summary>
    public interface ILayoutComponent
    {
        void Compose(LayoutColumn column);
    }

    /// <summary>Collects mixed-style runs for <see cref="LayoutColumn.Spans"/>.</summary>
    public sealed class LayoutSpans
    {
        private readonly List<SpanToken> _tokens = new List<SpanToken>();
        private readonly TextStyle _default;

        internal LayoutSpans(TextStyle defaultStyle) { _default = defaultStyle; }

        internal RichSource ToSource(TextAlignment alignment) => new RichSource(_tokens.ToArray(), alignment);

        /// <summary>Add a run of text. <c>\n</c> inside it starts a new line.</summary>
        public LayoutSpans Span(string text, TextStyle? style = null) => Add(text, style, null, null);

        /// <summary>Add a run of text that links to <paramref name="uri"/>.</summary>
        public LayoutSpans Link(string text, string uri, TextStyle? style = null)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            return Add(text, style, uri, null);
        }

        /// <summary>Add a run of text that jumps to the destination <paramref name="destination"/> (see <c>Destination</c>).</summary>
        public LayoutSpans GoTo(string text, string destination, TextStyle? style = null)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            return Add(text, style, null, destination);
        }

        /// <summary>Start a new line.</summary>
        public LayoutSpans LineBreak()
        {
            _tokens.Add(new SpanToken(SpanKind.LineBreak, "", _default, null, null));
            return this;
        }

        /// <summary>The number of the page this text lands on.</summary>
        public LayoutSpans PageNumber(TextStyle? style = null)
        {
            _tokens.Add(new SpanToken(SpanKind.PageNumber, "", style ?? _default, null, null));
            return this;
        }

        /// <summary>The total number of pages in the document.</summary>
        public LayoutSpans TotalPages(TextStyle? style = null)
        {
            _tokens.Add(new SpanToken(SpanKind.TotalPages, "", style ?? _default, null, null));
            return this;
        }

        private LayoutSpans Add(string text, TextStyle? style, string? uri, string? destination)
        {
            style ??= _default;
            if (string.IsNullOrEmpty(text)) return this;

            // One token per word, each carrying its trailing spaces, so lines break between words.
            int start = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\n')
                {
                    if (i > start) _tokens.Add(new SpanToken(SpanKind.Text, text.Substring(start, i - start), style, uri, destination));
                    _tokens.Add(new SpanToken(SpanKind.LineBreak, "", style, null, null));
                    start = i + 1;
                }
                else if (c == ' ' && (i + 1 == text.Length || text[i + 1] != ' '))
                {
                    _tokens.Add(new SpanToken(SpanKind.Text, text.Substring(start, i + 1 - start), style, uri, destination));
                    start = i + 1;
                }
            }
            if (start < text.Length)
                _tokens.Add(new SpanToken(SpanKind.Text, text.Substring(start), style, uri, destination));
            return this;
        }
    }

    /// <summary>Collects the items of an <see cref="LayoutColumn.Inlined"/> flow.</summary>
    public sealed class LayoutInline
    {
        internal readonly List<LayoutElement> Items = new List<LayoutElement>();
        internal LayoutInline() { }

        /// <summary>Add an item. It is as wide as its content, up to the available width.</summary>
        public LayoutInline Item(Action<LayoutColumn> content)
        {
            Items.Add(LayoutBuilders.Stack(content));
            return this;
        }
    }

    /// <summary>Collects the layers of <see cref="LayoutColumn.Layers"/>.</summary>
    public sealed class LayoutLayers
    {
        internal readonly List<LayoutElement> Back = new List<LayoutElement>();
        internal readonly List<LayoutElement> Front = new List<LayoutElement>();
        internal LayoutElement? PrimaryElement;
        internal LayoutLayers() { }

        /// <summary>A layer drawn behind the primary layer.</summary>
        public LayoutLayers Behind(Action<LayoutColumn> content) { Back.Add(LayoutBuilders.Stack(content)); return this; }

        /// <summary>The layer that determines the size (and page splitting) of the whole group. Exactly one is required.</summary>
        public LayoutLayers Primary(Action<LayoutColumn> content)
        {
            if (PrimaryElement != null) throw new InvalidOperationException("Layers can only have one primary layer.");
            PrimaryElement = LayoutBuilders.Stack(content);
            return this;
        }

        /// <summary>A layer drawn in front of the primary layer.</summary>
        public LayoutLayers InFront(Action<LayoutColumn> content) { Front.Add(LayoutBuilders.Stack(content)); return this; }
    }

    internal static class LayoutBuilders
    {
        public static StackElement Stack(Action<LayoutColumn>? content)
        {
            var column = new LayoutColumn();
            content?.Invoke(column);
            return column.ToElement();
        }

        public static LayoutElement Spans(Action<LayoutSpans> content, TextAlignment alignment, TextStyle? defaultStyle)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            var spans = new LayoutSpans(defaultStyle ?? TextStyle.Default);
            content(spans);
            return new RichTextElement(spans.ToSource(alignment));
        }

        public static LayoutElement Inlined(Action<LayoutInline> content, float hSpacing, float vSpacing, TextAlignment alignment)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            var inline = new LayoutInline();
            content(inline);
            return new InlinedElement(inline.Items.ToArray(), hSpacing, vSpacing, alignment);
        }

        public static LayoutElement Decoration(Action<LayoutColumn>? header, Action<LayoutColumn> content, Action<LayoutColumn>? footer)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            return new DecorationElement(Stack(header), Stack(content), Stack(footer));
        }

        public static LayoutElement Layers(Action<LayoutLayers> content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            var layers = new LayoutLayers();
            content(layers);
            if (layers.PrimaryElement == null)
                throw new InvalidOperationException("Layers needs a primary layer; call Primary(...).");
            return new LayersElement(layers.Back.ToArray(), layers.PrimaryElement, layers.Front.ToArray());
        }

        public static LayoutElement Once(Action<LayoutColumn> content, bool skipFirst)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            return new OnceElement(Stack(content), skipFirst);
        }

        public static LayoutElement Component(ILayoutComponent component)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            return Stack(component.Compose);
        }

        public static LayoutElement Image(PdfImage image, float? width, float? height, ImageFit fit, TextAlignment alignment)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (width <= 0f) throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");
            if (height <= 0f) throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive.");
            return new ImageElement(image, width, height, fit, alignment);
        }

        public static LayoutElement Marker(string marker, TextStyle? markerStyle, float indent, Action<LayoutColumn> content)
        {
            if (marker == null) throw new ArgumentNullException(nameof(marker));
            if (content == null) throw new ArgumentNullException(nameof(content));
            return new MarkerElement(marker, markerStyle ?? TextStyle.Default, indent, Stack(content));
        }
    }
}
