// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System.Globalization;

namespace Majorsilence.Pdf.Text
{
    /// <summary>Which characters need shaping, and how they keep to their neighbours.</summary>
    internal static class ComplexScripts
    {
        /// <summary>
        /// Whether a line has characters whose glyphs depend on their neighbours: the joining
        /// scripts (Arabic, Syriac, N'Ko, Mongolian ...), the Brahmic scripts of South and South-East
        /// Asia, combining marks, and the joiners. Text without any is drawn a glyph per character.
        /// </summary>
        internal static bool NeedsShaping(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c < 0x0300) continue;
                int cp = c;
                if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                    cp = char.ConvertToUtf32(c, text[++i]);
                if (IsComplex(cp)) return true;
            }
            return false;
        }

        private static bool IsComplex(int cp)
        {
            if (cp < 0x0300) return false;
            if (cp <= 0x036F) return true;                       // combining diacritical marks
            if (cp >= 0x0483 && cp <= 0x0489) return true;       // Cyrillic combining marks
            if (cp >= 0x0591 && cp <= 0x05C7) return true;       // Hebrew points and cantillation
            if (cp >= 0x0600 && cp <= 0x08FF) return true;       // Arabic, Syriac, Thaana, N'Ko, Samaritan, Mandaic, Arabic Extended
            if (cp >= 0x0900 && cp <= 0x0DFF) return true;       // Devanagari ... Sinhala
            if (cp >= 0x0E00 && cp <= 0x0FFF) return true;       // Thai, Lao, Tibetan
            if (cp >= 0x1000 && cp <= 0x109F) return true;       // Myanmar
            if (cp >= 0x1100 && cp <= 0x11FF) return true;       // Hangul Jamo
            if (cp >= 0x1700 && cp <= 0x18AF) return true;       // Philippine scripts, Khmer, Mongolian
            if (cp >= 0x1900 && cp <= 0x1AFF) return true;       // Limbu ... Tai Tham, combining marks extended
            if (cp >= 0x1B00 && cp <= 0x1CFF) return true;       // Balinese ... Vedic extensions
            if (cp >= 0x1DC0 && cp <= 0x1DFF) return true;       // combining marks supplement
            if (cp >= 0x200C && cp <= 0x200D) return true;       // zero-width non-joiner, joiner
            if (cp >= 0x20D0 && cp <= 0x20FF) return true;       // combining marks for symbols
            if (cp >= 0xA800 && cp <= 0xA9FF) return true;       // Syloti Nagri ... Javanese
            if (cp >= 0xAA00 && cp <= 0xAAFF) return true;       // Cham, Myanmar extended, Tai Viet
            if (cp >= 0xABC0 && cp <= 0xABFF) return true;       // Meetei Mayek
            if (cp >= 0xFB1D && cp <= 0xFDFF) return true;       // Hebrew and Arabic presentation forms
            if (cp >= 0xFE00 && cp <= 0xFE0F) return true;       // variation selectors
            if (cp >= 0xFE20 && cp <= 0xFE2F) return true;       // combining half marks
            if (cp >= 0xFE70 && cp <= 0xFEFF) return true;       // Arabic presentation forms B
            if (cp >= 0x10A00 && cp <= 0x10A5F) return true;     // Kharoshthi
            if (cp >= 0x11000 && cp <= 0x11FFF) return true;     // Brahmi and the other historic Indic scripts
            if (cp >= 0x1E900 && cp <= 0x1E95F) return true;     // Adlam
            if (cp >= 0xE0100 && cp <= 0xE01EF) return true;     // variation selectors supplement
            return false;
        }

        /// <summary>
        /// A character drawn as part of the one before it - a combining mark, a joiner, a variation
        /// selector - which therefore stays in that character's font.
        /// </summary>
        internal static bool ClingsToPrevious(int cp)
        {
            if (cp == 0x200C || cp == 0x200D) return true;
            if ((cp >= 0xFE00 && cp <= 0xFE0F) || (cp >= 0xE0100 && cp <= 0xE01EF)) return true;
            if (cp > 0xFFFF)
                return false;
            var cat = CharUnicodeInfo.GetUnicodeCategory((char)cp);
            return cat == UnicodeCategory.NonSpacingMark || cat == UnicodeCategory.SpacingCombiningMark
                || cat == UnicodeCategory.EnclosingMark;
        }

        /// <summary>
        /// The bidirectional formatting characters: they order the text and draw as nothing.
        /// </summary>
        internal static bool IsInvisibleControl(int cp) =>
            cp == 0x061C || cp == 0x200E || cp == 0x200F || (cp >= 0x202A && cp <= 0x202E) || (cp >= 0x2066 && cp <= 0x2069);
    }
}
