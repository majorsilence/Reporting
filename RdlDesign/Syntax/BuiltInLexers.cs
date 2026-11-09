// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;

namespace Majorsilence.Reporting.RdlDesign.Syntax
{
    /// <summary>
    /// Stand-ins for the SQL and XML lexers built into real Scintilla, which the shim has no
    /// equivalent of (#527). Each splits the text into runs of the style indices ScintillaSqlStyle
    /// and ScintillaXMLStyle configure (<see cref="Style.Sql"/>, <see cref="Style.Xml"/>); text
    /// in no run keeps the editor's own colour. They colour what an editor needs to be read, not
    /// every case Scintilla's lexers tell apart.
    /// </summary>
    internal static class BuiltInLexers
    {
        /// <summary>
        /// SQL: <c>--</c> and <c>/* */</c> comments, quoted strings, numbers, operators, and words in
        /// the keyword lists, which are matched ignoring case. <paramref name="keywordStyle"/> gives a
        /// word's style, or null for a word in no list.
        /// </summary>
        internal static List<(int Start, int Length, int Style)> Sql(string text, Func<string, int?> keywordStyle)
        {
            var runs = new List<(int, int, int)>();
            int i = 0, n = text.Length;
            while (i < n)
            {
                char c = text[i];
                int start = i;
                if (c == '-' && i + 1 < n && text[i + 1] == '-')
                {
                    while (i < n && text[i] != '\n') i++;
                    runs.Add((start, i - start, Style.Sql.CommentLine));
                }
                else if (c == '/' && i + 1 < n && text[i + 1] == '*')
                {
                    int end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    i = end < 0 ? n : end + 2;
                    runs.Add((start, i - start, Style.Sql.Comment));
                }
                else if (c == '\'' || c == '"')
                {
                    i = QuotedEnd(text, i, c);
                    runs.Add((start, i - start, c == '\'' ? Style.Sql.Character : Style.Sql.String));
                }
                else if (c == '[')
                {
                    // a bracketed name such as [Order Details]: never a keyword
                    int end = text.IndexOf(']', i + 1);
                    i = end < 0 ? n : end + 1;
                }
                else if (char.IsDigit(c) || (c == '.' && i + 1 < n && char.IsDigit(text[i + 1])))
                {
                    while (i < n && (char.IsLetterOrDigit(text[i]) || text[i] == '.')) i++;
                    runs.Add((start, i - start, Style.Sql.Number));
                }
                else if (IsWordChar(c))
                {
                    while (i < n && IsWordChar(text[i])) i++;
                    // @variables and #temp tables are names, as are words qualified by a dot (dbo.Sum)
                    bool name = c == '@' || c == '#' || (start > 0 && text[start - 1] == '.');
                    int? style = name ? null : keywordStyle(text.Substring(start, i - start));
                    if (style != null)
                        runs.Add((start, i - start, style.Value));
                }
                else if (IsSqlOperator(c))
                {
                    while (i < n && IsSqlOperator(text[i])) i++;
                    runs.Add((start, i - start, Style.Sql.Operator));
                }
                else
                    i++;
            }
            return runs;
        }

        private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_' || c == '@' || c == '#' || c == '$';

        private static bool IsSqlOperator(char c) => "+-*/%=<>!&|^~(),;.".IndexOf(c) >= 0;

        // The index after a quoted run starting at `start`; a doubled quote is part of it. An
        // unterminated one runs to the end of the line, as it does in Scintilla.
        private static int QuotedEnd(string text, int start, char quote)
        {
            int i = start + 1;
            while (i < text.Length)
            {
                if (text[i] == quote)
                {
                    if (i + 1 < text.Length && text[i + 1] == quote) { i += 2; continue; }
                    return i + 1;
                }
                if (text[i] == '\n')
                    return i;
                i++;
            }
            return i;
        }

        /// <summary>
        /// XML: comments, tags (their <c>&lt;</c>, name and closing <c>&gt;</c>), attribute names,
        /// quoted attribute values, and entities in text. CDATA and the text between tags keep the
        /// editor's colour.
        /// </summary>
        internal static List<(int Start, int Length, int Style)> Xml(string text)
        {
            var runs = new List<(int, int, int)>();
            int i = 0, n = text.Length;
            while (i < n)
            {
                char c = text[i];
                int start = i;
                if (c == '<' && string.CompareOrdinal(text, i, "<!--", 0, 4) == 0)
                {
                    int end = text.IndexOf("-->", i + 4, StringComparison.Ordinal);
                    i = end < 0 ? n : end + 3;
                    runs.Add((start, i - start, Style.Xml.Comment));
                }
                else if (c == '<' && string.CompareOrdinal(text, i, "<![CDATA[", 0, 9) == 0)
                {
                    int end = text.IndexOf("]]>", i + 9, StringComparison.Ordinal);
                    i = end < 0 ? n : end + 3;
                }
                else if (c == '<')
                {
                    i = XmlTag(text, i, runs);
                }
                else if (c == '&')
                {
                    int end = i + 1;
                    while (end < n && end - i <= 32 && (char.IsLetterOrDigit(text[end]) || text[end] == '#')) end++;
                    if (end < n && text[end] == ';' && end > i + 1)
                    {
                        i = end + 1;
                        runs.Add((start, i - start, Style.Xml.Entity));
                    }
                    else
                        i++;
                }
                else
                    i++;
            }
            return runs;
        }

        // Styles the tag starting at the '<' at `start` and returns the index after it.
        private static int XmlTag(string text, int start, List<(int, int, int)> runs)
        {
            int n = text.Length;
            int i = start + 1;
            bool closing = i < n && text[i] == '/';
            if (i < n && (text[i] == '/' || text[i] == '?' || text[i] == '!')) i++;
            while (i < n && IsNameChar(text[i])) i++;
            runs.Add((start, i - start, closing ? Style.Xml.TagEnd : Style.Xml.Tag));

            while (i < n)
            {
                char c = text[i];
                if (c == '>' || ((c == '/' || c == '?') && i + 1 < n && text[i + 1] == '>'))
                {
                    int s = i;
                    i += c == '>' ? 1 : 2;
                    runs.Add((s, i - s, closing ? Style.Xml.TagEnd : Style.Xml.Tag));
                    return i;
                }
                if (c == '<')
                    return i;       // a tag left open; the next one starts here
                if (c == '"' || c == '\'')
                {
                    int s = i;
                    int end = text.IndexOf(c, i + 1);
                    i = end < 0 ? n : end + 1;
                    runs.Add((s, i - s, c == '"' ? Style.Xml.DoubleString : Style.Xml.SingleString));
                }
                else if (IsNameChar(c))
                {
                    int s = i;
                    while (i < n && IsNameChar(text[i])) i++;
                    runs.Add((s, i - s, Style.Xml.Attribute));
                }
                else
                    i++;
            }
            return i;
        }

        private static bool IsNameChar(char c) => char.IsLetterOrDigit(c) || c == '_' || c == ':' || c == '-' || c == '.';
    }
}
