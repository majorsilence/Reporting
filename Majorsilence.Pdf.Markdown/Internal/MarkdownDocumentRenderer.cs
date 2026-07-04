// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System.Collections.Generic;
using System.Text;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Majorsilence.Pdf.Markdown.Internal
{
    // Walks a Markdig document and produces a flat list of MarkdownBlockNode, each of which knows
    // how to measure and draw itself at a fixed width. Unsupported constructs (images,
    // blockquotes, anything else Markdig might parse) are skipped and recorded in `warnings`
    // rather than thrown -- markdown rendering should degrade gracefully.
    internal static class MarkdownDocumentRenderer
    {
        private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UsePipeTables().Build();

        public static List<MarkdownBlockNode> Parse(string markdown, float width, MarkdownStyle style, List<string> warnings)
        {
            var document = Markdig.Markdown.Parse(markdown ?? "", Pipeline);
            var blocks = new List<MarkdownBlockNode>();
            foreach (var block in document)
                AppendBlock(blocks, block, width, style, warnings);
            return blocks;
        }

        private static void AppendBlock(List<MarkdownBlockNode> blocks, Block block, float width, MarkdownStyle style, List<string> warnings)
        {
            switch (block)
            {
                case HeadingBlock heading:
                    blocks.Add(new RichTextBlockNode
                    {
                        Runs = CollectInlineRuns(heading.Inline, style.HeadingStyle(heading.Level), style, warnings),
                        SpacingAfter = style.BlockSpacingValue,
                    });
                    break;

                case ParagraphBlock paragraph:
                    blocks.Add(new RichTextBlockNode
                    {
                        Runs = CollectInlineRuns(paragraph.Inline, style.BodyStyle, style, warnings),
                        SpacingAfter = style.ParagraphSpacingValue,
                    });
                    break;

                case FencedCodeBlock fenced:
                    blocks.Add(new CodeBlockNode
                    {
                        Lines = fenced.Lines.ToString().Split('\n'),
                        SpacingAfter = style.BlockSpacingValue,
                    });
                    break;

                case ThematicBreakBlock:
                    blocks.Add(new ThematicBreakNode { SpacingAfter = style.BlockSpacingValue });
                    break;

                case ListBlock list:
                    {
                        var node = new ListBlockNode { SpacingAfter = style.BlockSpacingValue };
                        AppendListItems(node.Items, list, style, warnings, indentLevel: 0);
                        blocks.Add(node);
                    }
                    break;

                case Table table:
                    blocks.Add(BuildTable(table, width, style, warnings));
                    break;

                case QuoteBlock:
                    warnings.Add("Blockquote not rendered (unsupported in this version).");
                    break;

                default:
                    warnings.Add($"Unsupported markdown block '{block.GetType().Name}' skipped.");
                    break;
            }
        }

        private static void AppendListItems(List<ListItemNode> items, ListBlock list, MarkdownStyle style, List<string> warnings, int indentLevel)
        {
            int number = 1;
            foreach (var child in list)
            {
                if (child is not ListItemBlock item) continue;

                foreach (var itemChild in item)
                {
                    if (itemChild is ParagraphBlock paragraph)
                    {
                        string marker = list.IsOrdered ? $"{number}." : "•";
                        items.Add(new ListItemNode
                        {
                            Marker = marker,
                            Content = new RichTextBlockNode
                            {
                                Runs = CollectInlineRuns(paragraph.Inline, style.BodyStyle, style, warnings),
                                IndentLeft = (indentLevel + 1) * style.ListIndentValue,
                                SpacingAfter = 2f,
                            },
                        });
                    }
                    else if (itemChild is ListBlock nested)
                    {
                        AppendListItems(items, nested, style, warnings, indentLevel + 1);
                    }
                }
                number++;
            }
        }

        private static MarkdownBlockNode BuildTable(Table table, float width, MarkdownStyle style, List<string> warnings)
        {
            int columnCount = table.ColumnDefinitions.Count;
            if (columnCount == 0) columnCount = 1;
            var columnWidths = new float[columnCount];
            float perColumn = width / columnCount;
            for (int i = 0; i < columnCount; i++) columnWidths[i] = perColumn;

            var pdfTable = new PdfTable(columnWidths)
                .WithHeaderBackground(PdfColor.FromRgb(235, 235, 235))
                .WithBorder(PdfColor.Gray, 0.5f)
                .WithCellPadding(4f)
                .WithCellTextStyle(style.BodyStyle)
                .WithHeaderTextStyle(style.BodyStyle.WithBold());

            foreach (var rowObj in table)
            {
                if (rowObj is not TableRow row) continue;
                var cells = new string[columnCount];
                int ci = 0;
                foreach (var cellObj in row)
                {
                    if (ci >= columnCount) break;
                    if (cellObj is TableCell cell)
                        cells[ci] = ExtractPlainText(cell, warnings);
                    ci++;
                }
                for (int i = 0; i < columnCount; i++) cells[i] ??= "";
                pdfTable.AddRow(cells);
            }

            return new TableBlockNode { Table = pdfTable, SpacingAfter = style.BlockSpacingValue };
        }

        private static string ExtractPlainText(ContainerBlock container, List<string> warnings)
        {
            var sb = new StringBuilder();
            foreach (var block in container)
            {
                if (block is LeafBlock leaf && leaf.Inline != null)
                    AppendPlainInline(sb, leaf.Inline, warnings);
            }
            return sb.ToString();
        }

        private static void AppendPlainInline(StringBuilder sb, ContainerInline container, List<string> warnings)
        {
            foreach (var inline in container)
            {
                switch (inline)
                {
                    case LiteralInline literal:
                        sb.Append(literal.Content.ToString());
                        break;
                    case CodeInline code:
                        sb.Append(code.Content);
                        break;
                    case ContainerInline nested:
                        AppendPlainInline(sb, nested, warnings);
                        break;
                }
            }
        }

        private static List<InlineRun> CollectInlineRuns(ContainerInline? container, TextStyle baseStyle, MarkdownStyle style, List<string> warnings, string? linkUrl = null)
        {
            var runs = new List<InlineRun>();
            if (container == null) return runs;
            CollectInto(runs, container, baseStyle, style, warnings, linkUrl);
            return runs;
        }

        private static void CollectInto(List<InlineRun> runs, ContainerInline container, TextStyle baseStyle, MarkdownStyle style, List<string> warnings, string? linkUrl)
        {
            foreach (var inline in container)
            {
                switch (inline)
                {
                    case LiteralInline literal:
                        AppendWords(runs, literal.Content.ToString(), baseStyle, linkUrl);
                        break;

                    case EmphasisInline emphasis:
                        var emphasisStyle = emphasis.DelimiterCount >= 2 ? baseStyle.WithBold() : baseStyle.WithItalic();
                        CollectInto(runs, emphasis, emphasisStyle, style, warnings, linkUrl);
                        break;

                    case CodeInline code:
                        AppendWords(runs, code.Content, style.CodeStyle, linkUrl);
                        break;

                    case LinkInline link:
                        if (link.IsImage)
                        {
                            warnings.Add($"Image not rendered (unsupported in this version): {link.Url}");
                        }
                        else
                        {
                            var linkStyle = baseStyle.WithColor(style.LinkColorValue).WithUnderline();
                            CollectInto(runs, link, linkStyle, style, warnings, link.Url);
                        }
                        break;

                    case LineBreakInline:
                        runs.Add(InlineRun.LineBreak);
                        break;

                    case ContainerInline nested:
                        CollectInto(runs, nested, baseStyle, style, warnings, linkUrl);
                        break;

                    default:
                        // LeafInline types with no text payload we care about (e.g. autolinks
                        // handled via LiteralInline already, HTML inlines) are silently ignored.
                        break;
                }
            }
        }

        private static void AppendWords(List<InlineRun> runs, string text, TextStyle style, string? linkUrl)
        {
            if (string.IsNullOrEmpty(text)) return;

            // A leading space is significant when this literal segment follows a differently
            // styled run (e.g. the " adds:" after a bold "**2.0**") -- Split(' ') turns it into
            // an empty first element, which the loop below skips, silently gluing the previous
            // run's text to this one ("2.0adds:"). Emit it as its own token so the gap survives.
            if (text[0] == ' ' && runs.Count > 0)
                runs.Add(new InlineRun(" ", style, linkUrl));

            var words = text.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length == 0) continue;
                string token = i < words.Length - 1 ? words[i] + " " : words[i];
                runs.Add(new InlineRun(token, style, linkUrl));
            }
        }
    }
}
