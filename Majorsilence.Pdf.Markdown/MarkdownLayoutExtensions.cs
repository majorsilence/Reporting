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
        /// paragraph, code block, list, thematic break, or table) at a time. Blocks split across
        /// pages like any other layout content: paragraphs and code blocks break between lines
        /// (never leaving a lone line behind), tables break between rows and repeat their header,
        /// lists break between items, and a heading is kept with the text that follows it.
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

            var blocks = MarkdownDocumentRenderer.Parse(markdown, layout.ContentWidth, style, collected);
            return layout.Box(LayoutBoxStyle.Default, column => AddBlocks(column, blocks, style));
        }

        /// <summary>
        /// Render <paramref name="markdown"/> into a column (or box) of width <paramref name="width"/>.
        /// The width is used to size markdown tables; text wraps to whatever width the column gets.
        /// </summary>
        public static LayoutColumn Markdown(this LayoutColumn column, string markdown, float width,
            MarkdownStyle? style = null, List<string>? warnings = null)
        {
            if (column == null) throw new ArgumentNullException(nameof(column));
            style ??= MarkdownStyle.Default;
            var collected = warnings ?? new List<string>();

            AddBlocks(column, MarkdownDocumentRenderer.Parse(markdown, width, style, collected), style);
            return column;
        }

        private static void AddBlocks(LayoutColumn column, List<MarkdownBlockNode> blocks, MarkdownStyle style)
        {
            foreach (var block in blocks) block.AddTo(column, style);
        }
    }
}
