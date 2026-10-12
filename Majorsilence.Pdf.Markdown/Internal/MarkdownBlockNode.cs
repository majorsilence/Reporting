// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System.Collections.Generic;
using Majorsilence.Pdf.Layout;

namespace Majorsilence.Pdf.Markdown.Internal
{
    // A single rendered block (heading, paragraph, code block, list, thematic break, or table).
    // Measure and Draw must agree on height for a given width -- PdfLayout's atomic Canvas()
    // escape hatch relies on Measure's answer being exactly what Draw will consume.
    internal abstract class MarkdownBlockNode
    {
        public abstract float Measure(PdfCanvas canvas, float width, MarkdownStyle style);
        public abstract float Draw(PdfCanvas canvas, float x, float y, float width, MarkdownStyle style);

        // Adds this block to the layout tree, where it splits across pages like any other content.
        public abstract void AddTo(LayoutColumn column, MarkdownStyle style);

        protected static void AddSpacing(LayoutColumn column, float points)
        {
            if (points > 0f) column.Spacer(points);
        }
    }

    // Heading or paragraph: a word-wrapped sequence of mixed-style runs (bold/italic/code/links).
    internal sealed class RichTextBlockNode : MarkdownBlockNode
    {
        public List<InlineRun> Runs { get; set; } = new List<InlineRun>();
        public float SpacingAfter { get; set; }
        public float IndentLeft { get; set; }

        /// <summary>Headings ask for this much room below them so they are never stranded at the foot of a page.</summary>
        public float KeepWithNextHeight { get; set; }

        public override float Measure(PdfCanvas canvas, float width, MarkdownStyle style)
            => InlineLinePacker.Pack(canvas, Runs, width - IndentLeft, draw: false, x: 0, y: 0) + SpacingAfter;

        public override float Draw(PdfCanvas canvas, float x, float y, float width, MarkdownStyle style)
            => InlineLinePacker.Pack(canvas, Runs, width - IndentLeft, draw: true, x: x + IndentLeft, y: y) + SpacingAfter;

        public override void AddTo(LayoutColumn column, MarkdownStyle style)
        {
            if (KeepWithNextHeight > 0f) column.EnsureSpace(KeepWithNextHeight);
            AddRuns(column);
            AddSpacing(column, SpacingAfter);
        }

        // Words are already tokenised with their trailing space, so each run becomes one span.
        public void AddRuns(LayoutColumn column)
        {
            if (Runs.Count == 0) return;
            column.Spans(spans =>
            {
                foreach (var run in Runs)
                {
                    if (run.IsLineBreak) spans.LineBreak();
                    else if (run.LinkUrl != null) spans.Link(run.Text, run.LinkUrl, run.Style);
                    else spans.Span(run.Text, run.Style);
                }
            });
        }
    }

    internal sealed class ListItemNode
    {
        public string Marker { get; set; } = "";
        public RichTextBlockNode Content { get; set; } = null!;
    }

    internal sealed class ListBlockNode : MarkdownBlockNode
    {
        public List<ListItemNode> Items { get; } = new List<ListItemNode>();
        public float SpacingAfter { get; set; }

        public override float Measure(PdfCanvas canvas, float width, MarkdownStyle style)
        {
            float total = 0f;
            foreach (var item in Items) total += item.Content.Measure(canvas, width, style);
            return total + SpacingAfter;
        }

        public override float Draw(PdfCanvas canvas, float x, float y, float width, MarkdownStyle style)
        {
            float curY = y;
            foreach (var item in Items)
            {
                float firstLineFontSize = item.Content.Runs.Count > 0 ? item.Content.Runs[0].Style.FontSize : style.BodyStyle.FontSize;
                canvas.DrawText(item.Marker, x + item.Content.IndentLeft - 16f, curY + firstLineFontSize, style.BodyStyle);
                curY += item.Content.Draw(canvas, x, curY, width, style);
            }
            return (curY - y) + SpacingAfter;
        }

        public override void AddTo(LayoutColumn column, MarkdownStyle style)
        {
            foreach (var item in Items)
            {
                var content = item.Content;
                column.Bullet(item.Marker, content.IndentLeft, c => content.AddRuns(c), style.BodyStyle);
                AddSpacing(column, content.SpacingAfter);
            }
            AddSpacing(column, SpacingAfter);
        }
    }

    internal sealed class CodeBlockNode : MarkdownBlockNode
    {
        public string[] Lines { get; set; } = System.Array.Empty<string>();
        public float SpacingAfter { get; set; }

        private float ContentHeight(MarkdownStyle style)
            => Lines.Length * (style.CodeStyle.FontSize * 1.2f) + 2 * style.CodeBlockPaddingValue;

        public override float Measure(PdfCanvas canvas, float width, MarkdownStyle style)
            => ContentHeight(style) + SpacingAfter;

        public override float Draw(PdfCanvas canvas, float x, float y, float width, MarkdownStyle style)
        {
            float lineHeight = style.CodeStyle.FontSize * 1.2f;
            float contentHeight = ContentHeight(style);
            canvas.DrawRectangle(x, y, width, contentHeight, ShapeStyle.Filled(style.CodeBackgroundValue));

            float curY = y + style.CodeBlockPaddingValue;
            foreach (var line in Lines)
            {
                canvas.DrawText(line, x + style.CodeBlockPaddingValue, curY + style.CodeStyle.FontSize, style.CodeStyle);
                curY += lineHeight;
            }
            return contentHeight + SpacingAfter;
        }

        public override void AddTo(LayoutColumn column, MarkdownStyle style)
        {
            var box = LayoutBoxStyle.Default
                .WithPadding(style.CodeBlockPaddingValue)
                .WithBackground(style.CodeBackgroundValue);
            column.Box(box, c => c.Text(string.Join("\n", Lines), style.CodeStyle));
            AddSpacing(column, SpacingAfter);
        }
    }

    internal sealed class ThematicBreakNode : MarkdownBlockNode
    {
        public float SpacingAfter { get; set; }

        public override float Measure(PdfCanvas canvas, float width, MarkdownStyle style) => 1f + SpacingAfter;

        public override float Draw(PdfCanvas canvas, float x, float y, float width, MarkdownStyle style)
        {
            canvas.DrawLine(x, y, x + width, y);
            return 1f + SpacingAfter;
        }

        public override void AddTo(LayoutColumn column, MarkdownStyle style) => column.Line(null, 1f + SpacingAfter);
    }

    internal sealed class TableBlockNode : MarkdownBlockNode
    {
        public PdfTable Table { get; set; } = null!;
        public float SpacingAfter { get; set; }

        public override float Measure(PdfCanvas canvas, float width, MarkdownStyle style)
            => canvas.MeasureTableHeight(Table) + SpacingAfter;

        public override float Draw(PdfCanvas canvas, float x, float y, float width, MarkdownStyle style)
        {
            canvas.DrawTable(Table, x, y, out float height);
            return height + SpacingAfter;
        }

        public override void AddTo(LayoutColumn column, MarkdownStyle style)
        {
            column.Table(Table);
            AddSpacing(column, SpacingAfter);
        }
    }
}
