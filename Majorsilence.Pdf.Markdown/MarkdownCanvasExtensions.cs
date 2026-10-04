// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;
using System.Collections.Generic;
using Majorsilence.Pdf.Markdown.Internal;

namespace Majorsilence.Pdf.Markdown
{
    /// <summary>Markdown rendering extension methods for <see cref="PdfCanvas"/>.</summary>
    public static class MarkdownCanvasExtensions
    {
        /// <summary>
        /// Render <paramref name="markdown"/> starting at (<paramref name="x"/>, <paramref name="y"/>),
        /// word-wrapped to <paramref name="width"/>. Draws everything in one continuous pass with
        /// no page breaks — for auto-paginating markdown across pages, use
        /// <see cref="Majorsilence.Pdf.Layout.PdfLayout"/>'s <c>Markdown</c> extension instead.
        /// </summary>
        /// <param name="warnings">
        /// If supplied, messages about unsupported constructs (images, blockquotes) are appended
        /// to this list rather than thrown.
        /// </param>
        /// <returns>The total height, in PDF points, consumed by the rendered markdown.</returns>
        public static float DrawMarkdown(this PdfCanvas canvas, string markdown, float x, float y, float width,
            MarkdownStyle? style = null, List<string>? warnings = null)
        {
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            style ??= MarkdownStyle.Default;
            var collected = warnings ?? new List<string>();

            var blocks = MarkdownDocumentRenderer.Parse(markdown, width, style, collected);

            float curY = y;
            foreach (var block in blocks)
                curY += block.Draw(canvas, x, curY, width, style);

            return curY - y;
        }
    }
}
