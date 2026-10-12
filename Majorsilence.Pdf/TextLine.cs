// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

namespace Majorsilence.Pdf
{
    /// <summary>One wrapped line of text returned by <see cref="PdfCanvas.BreakLines"/>.</summary>
    public readonly struct TextLine
    {
        /// <summary>The visible text of the line, without a trailing newline or trailing spaces.</summary>
        public string Text { get; }

        /// <summary>Index into the source string where this line starts.</summary>
        public int StartIndex { get; }

        /// <summary>Number of source characters consumed by this line, including the newline and trailing spaces.</summary>
        public int Length { get; }

        /// <summary>Rendered width of <see cref="Text"/> in points.</summary>
        public float Width { get; }

        public TextLine(string text, int startIndex, int length, float width)
        {
            Text = text; StartIndex = startIndex; Length = length; Width = width;
        }
    }
}
