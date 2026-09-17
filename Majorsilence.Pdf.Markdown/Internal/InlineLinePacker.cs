// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System.Collections.Generic;

namespace Majorsilence.Pdf.Markdown.Internal
{
    // Packs a sequence of mixed-style word tokens onto lines within a fixed width, word-wrapping
    // when the next word would overflow and honoring explicit line breaks. All runs in a line
    // share one baseline, computed from the line's largest font size -- a deliberate v1
    // simplification (no per-run baseline alignment).
    internal static class InlineLinePacker
    {
        public static float Pack(PdfCanvas canvas, List<InlineRun> runs, float width, bool draw, float x, float y)
        {
            float cursorY = 0f;
            float lineWidth = 0f;
            var line = new List<(InlineRun run, float width)>();

            void FlushLine()
            {
                if (line.Count == 0) return;

                float maxFontSize = 0f;
                foreach (var (run, _) in line)
                    if (run.Style.FontSize > maxFontSize) maxFontSize = run.Style.FontSize;
                if (maxFontSize <= 0f) maxFontSize = 11f;

                if (draw)
                {
                    float drawX = x;
                    float baselineY = y + cursorY + maxFontSize;
                    foreach (var (run, w) in line)
                    {
                        // Draw the word with its trailing space intact (rather than trimming it)
                        // so the content stream has a real space glyph -- some text extractors
                        // (including PdfPig's ContentOrderTextExtractor) reconstruct word
                        // boundaries from glyphs present, not from positional gaps alone, and
                        // will otherwise glue adjacent words together (e.g. "Heading One" reads
                        // back as "HeadingOne").
                        if (run.Text.Length > 0)
                            canvas.DrawText(run.Text, drawX, baselineY, run.Style);
                        if (run.LinkUrl != null)
                            canvas.AddLink(drawX, baselineY - maxFontSize, w, maxFontSize * 1.2f, run.LinkUrl);
                        drawX += w;
                    }
                }

                cursorY += maxFontSize * 1.2f;
                line.Clear();
                lineWidth = 0f;
            }

            foreach (var run in runs)
            {
                if (run.IsLineBreak)
                {
                    FlushLine();
                    continue;
                }
                if (run.Text.Length == 0) continue;

                float w = canvas.MeasureTextWidth(run.Text, run.Style);
                if (lineWidth + w > width && line.Count > 0)
                    FlushLine();

                line.Add((run, w));
                lineWidth += w;
            }
            FlushLine();

            return cursorY;
        }
    }
}
