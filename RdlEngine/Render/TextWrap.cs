using System;
using System.Collections.Generic;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// How a text box's text is broken into lines: at its line breaks, and each paragraph that does
    /// not fit wrapped at its spaces. The PDF renderer draws the lines this way, and a growing box is
    /// measured this way, so that it grows by the lines it draws and no others.
    /// </summary>
    internal static class TextWrap
    {
        /// <summary>
        /// Wraps one paragraph at its spaces to <paramref name="maxWidth"/>: a word goes on the line
        /// when the line with it, measured without the space after it, still fits, and a word wider
        /// than <paramref name="maxWidth"/> has a line of its own.
        /// </summary>
        internal static void WrapParagraph(string text, float maxWidth, Func<string, float> width, List<string> output)
        {
            string[] words = text.Split(' ');
            string current = null;      // the line being built; the candidate (line + " " + word) is the only string made per word

            foreach (string word in words)
            {
                if (string.IsNullOrEmpty(current))
                {
                    current = word;
                }
                else
                {
                    string candidate = current + " " + word;
                    if (width(candidate) <= maxWidth)
                    {
                        current = candidate;
                    }
                    else
                    {
                        output.Add(current);
                        current = word;
                    }
                }
            }
            if (!string.IsNullOrEmpty(current))
                output.Add(current);
        }

        /// <summary>
        /// The number of lines <paramref name="text"/> is drawn in at <paramref name="maxWidth"/>: a
        /// line for each paragraph that fits, the spaces ending it left out, and the lines
        /// <see cref="WrapParagraph"/> makes of one that does not.
        /// </summary>
        internal static int LineCount(string text, float maxWidth, Func<string, float> width)
        {
            text = Textbox.LineBreaksAsLineFeeds(text ?? "");
            if (text.IndexOf('\n') < 0 && FitsOnALine(text, maxWidth, width, out _))
                return 1;           // the usual box: one line that fits

            int lines = 0;
            List<string> wrapped = null;
            foreach (string paragraph in text.Split('\n'))
            {
                if (FitsOnALine(paragraph, maxWidth, width, out string line))
                {
                    lines++;
                    continue;
                }
                wrapped ??= new List<string>();
                wrapped.Clear();
                WrapParagraph(line, maxWidth, width, wrapped);
                lines += Math.Max(1, wrapped.Count);
            }
            return lines;
        }

        private static bool FitsOnALine(string paragraph, float maxWidth, Func<string, float> width, out string line)
        {
            line = paragraph.Length > 0 && paragraph[paragraph.Length - 1] == ' ' ? paragraph.TrimEnd(' ') : paragraph;
            return line.Length == 0 || width(line) <= maxWidth;
        }
    }
}
