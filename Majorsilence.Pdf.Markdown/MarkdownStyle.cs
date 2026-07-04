// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;

namespace Majorsilence.Pdf.Markdown
{
    /// <summary>
    /// Fluent, immutable style map for markdown rendering. Every <c>With*</c> method returns a
    /// new instance; the original is unchanged.
    /// </summary>
    public sealed class MarkdownStyle
    {
        private TextStyle[] _headings = null!;
        private TextStyle _body = null!;
        private TextStyle _code = null!;
        private PdfColor _codeBackground;
        private PdfColor _linkColor;
        private float _paragraphSpacing = 8f;
        private float _blockSpacing = 10f;
        private float _listIndent = 18f;
        private float _codeBlockPadding = 8f;

        private MarkdownStyle() { }

        /// <summary>The default style: standard fonts, sizes 22/18/15/13/12/11 for H1–H6, 11pt body.</summary>
        public static MarkdownStyle Default { get; } = CreateDefault();

        private static MarkdownStyle CreateDefault()
        {
            var s = new MarkdownStyle
            {
                _body = TextStyle.Default.WithSize(11),
                _code = TextStyle.Default.WithFamily("Courier").WithSize(10),
                _codeBackground = PdfColor.FromRgb(240, 240, 240),
                _linkColor = PdfColor.Blue,
                _headings = new[]
                {
                    TextStyle.Default.WithSize(22).WithBold(),
                    TextStyle.Default.WithSize(18).WithBold(),
                    TextStyle.Default.WithSize(15).WithBold(),
                    TextStyle.Default.WithSize(13).WithBold(),
                    TextStyle.Default.WithSize(12).WithBold(),
                    TextStyle.Default.WithSize(11).WithBold(),
                },
            };
            return s;
        }

        internal TextStyle BodyStyle => _body;
        internal TextStyle CodeStyle => _code;
        internal PdfColor CodeBackgroundValue => _codeBackground;
        internal PdfColor LinkColorValue => _linkColor;
        internal float ParagraphSpacingValue => _paragraphSpacing;
        internal float BlockSpacingValue => _blockSpacing;
        internal float ListIndentValue => _listIndent;
        internal float CodeBlockPaddingValue => _codeBlockPadding;

        internal TextStyle HeadingStyle(int level) => _headings[Math.Min(Math.Max(level, 1), 6) - 1];

        private MarkdownStyle Clone() => new MarkdownStyle
        {
            _headings = (TextStyle[])_headings.Clone(),
            _body = _body,
            _code = _code,
            _codeBackground = _codeBackground,
            _linkColor = _linkColor,
            _paragraphSpacing = _paragraphSpacing,
            _blockSpacing = _blockSpacing,
            _listIndent = _listIndent,
            _codeBlockPadding = _codeBlockPadding,
        };

        /// <summary>Set the style for headings at <paramref name="level"/> (1–6, where 1 is <c># H1</c>).</summary>
        public MarkdownStyle WithHeading(int level, TextStyle style)
        {
            if (level < 1 || level > 6) throw new ArgumentOutOfRangeException(nameof(level), "Heading level must be between 1 and 6.");
            var clone = Clone();
            clone._headings[level - 1] = style;
            return clone;
        }

        /// <summary>Set the style for paragraph body text.</summary>
        public MarkdownStyle WithBody(TextStyle style) { var c = Clone(); c._body = style; return c; }

        /// <summary>Set the style and background color for inline code and fenced code blocks.</summary>
        public MarkdownStyle WithCode(TextStyle style, PdfColor background) { var c = Clone(); c._code = style; c._codeBackground = background; return c; }

        /// <summary>Set the text color used for link text.</summary>
        public MarkdownStyle WithLinkColor(PdfColor color) { var c = Clone(); c._linkColor = color; return c; }

        /// <summary>Set the vertical gap after each paragraph. Default: 8pt.</summary>
        public MarkdownStyle WithParagraphSpacing(float points) { var c = Clone(); c._paragraphSpacing = points; return c; }

        /// <summary>Set the vertical gap between top-level blocks (headings, lists, code blocks, tables). Default: 10pt.</summary>
        public MarkdownStyle WithBlockSpacing(float points) { var c = Clone(); c._blockSpacing = points; return c; }

        /// <summary>Set the horizontal indent per nested list level. Default: 18pt.</summary>
        public MarkdownStyle WithListIndent(float points) { var c = Clone(); c._listIndent = points; return c; }

        /// <summary>Set the padding inside fenced code block backgrounds. Default: 8pt.</summary>
        public MarkdownStyle WithCodeBlockPadding(float points) { var c = Clone(); c._codeBlockPadding = points; return c; }
    }
}
