// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

namespace Majorsilence.Pdf.Markdown.Internal
{
    // A single word-wrap token: either a word (with trailing space, if any, already included in
    // Text) or a forced line break. Runs share styling and an optional hyperlink target so mixed
    // bold/italic/code/link spans can be packed word-by-word onto lines.
    internal readonly struct InlineRun
    {
        public string Text { get; }
        public TextStyle Style { get; }
        public string? LinkUrl { get; }
        public bool IsLineBreak { get; }

        public InlineRun(string text, TextStyle style, string? linkUrl)
        {
            Text = text;
            Style = style;
            LinkUrl = linkUrl;
            IsLineBreak = false;
        }

        private InlineRun(bool isLineBreak)
        {
            Text = "";
            Style = TextStyle.Default;
            LinkUrl = null;
            IsLineBreak = isLineBreak;
        }

        public static InlineRun LineBreak { get; } = new InlineRun(isLineBreak: true);
    }
}
