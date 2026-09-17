// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;
using System.Collections.Generic;
using Majorsilence.Pdf.Layout;
using Majorsilence.Pdf.Markdown.Internal;

namespace Majorsilence.Pdf.Markdown
{
    /// <summary>Markdown rendering extension methods for <see cref="PdfLayout"/>.</summary>
    public static class MarkdownLayoutExtensions
    {
        /// <summary>
        /// Render <paramref name="markdown"/> into the flow layout, one block (heading,
        /// paragraph, code block, list, thematic break, or table) at a time. Each block is
        /// atomic — like <see cref="PdfLayout.Row"/> and <see cref="PdfLayout.Table"/>, a block
        /// that doesn't fit in the remaining space on the current page moves to the next page as
        /// a whole rather than splitting mid-block, and a single block taller than a full page's
        /// content area throws <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <param name="warnings">
        /// If supplied, messages about unsupported constructs (images, blockquotes) are appended
        /// to this list rather than thrown.
        /// </param>
        public static PdfLayout Markdown(this PdfLayout layout, string markdown,
            MarkdownStyle? style = null, List<string>? warnings = null)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            style ??= MarkdownStyle.Default;
            var collected = warnings ?? new List<string>();

            float width = layout.ContentWidth;
            var canvas = layout.CurrentCanvas; // for measurement only; safe to reuse across pages
            var blocks = MarkdownDocumentRenderer.Parse(markdown, width, style, collected);

            foreach (var block in blocks)
            {
                float height = block.Measure(canvas, width, style);
                if (height <= 0f) continue;
                layout.Canvas((c, rect) => block.Draw(c, rect.X, rect.Y, rect.Width, style), height);
            }
            return layout;
        }
    }
}
