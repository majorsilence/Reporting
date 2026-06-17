/*
 * Copyright (C) 2025 Peter Gill <peter@majorsilence.com>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Majorsilence.Reporting.Rdl.Pdf
{
    // Minimal TrueType/OpenType font parser.
    // Extracts only what is needed for embedding a CIDFont Type2 in a PDF:
    //   PostScript name, metrics (UPM, ascender, descender, bounding box),
    //   per-glyph advance widths, and the Unicode → glyph-ID mapping.
    internal sealed class TrueTypeFont
    {
        private readonly byte[] _data;
        private int _unitsPerEm;
        private short _ascender;
        private short _descender;
        private int _numGlyphs;
        private int _numberOfHMetrics;
        private ushort[] _advanceWidths;
        private Dictionary<int, ushort> _charToGlyph;
        private string _postScriptName;
        private short _xMin, _yMin, _xMax, _yMax;

        internal int UnitsPerEm => _unitsPerEm;
        internal short Ascender => _ascender;
        internal short Descender => _descender;
        internal int NumGlyphs => _numGlyphs;
        internal string PostScriptName => _postScriptName ?? "UnknownFont";
        internal short XMin => _xMin;
        internal short YMin => _yMin;
        internal short XMax => _xMax;
        internal short YMax => _yMax;
        internal byte[] Data => _data;

        internal TrueTypeFont(string path) : this(File.ReadAllBytes(path)) { }

        internal TrueTypeFont(byte[] data)
        {
            _data = data;
            Parse();
        }

        // ── binary helpers (big-endian) ──────────────────────────────────────

        private static ushort U16(byte[] b, int o) => (ushort)((b[o] << 8) | b[o + 1]);
        private static short  S16(byte[] b, int o) => (short)((b[o] << 8) | b[o + 1]);
        private static int    S32(byte[] b, int o) => (b[o] << 24) | (b[o+1] << 16) | (b[o+2] << 8) | b[o+3];
        private static uint   U32(byte[] b, int o) =>
            ((uint)b[o] << 24) | ((uint)b[o+1] << 16) | ((uint)b[o+2] << 8) | b[o+3];

        // ── parsing ──────────────────────────────────────────────────────────

        private void Parse()
        {
            int numTables = U16(_data, 4);
            var tables = new Dictionary<string, (int offset, int length)>(numTables, StringComparer.Ordinal);
            for (int i = 0; i < numTables; i++)
            {
                int e = 12 + i * 16;
                string tag = Encoding.ASCII.GetString(_data, e, 4);
                int off = S32(_data, e + 8);
                int len = S32(_data, e + 12);
                tables[tag] = (off, len);
            }

            ParseHead(tables);
            ParseHhea(tables);
            ParseMaxp(tables);
            ParseHmtx(tables);
            ParseCmap(tables);
            ParseName(tables);
        }

        private void ParseHead(Dictionary<string, (int offset, int length)> t)
        {
            if (!t.TryGetValue("head", out var e)) return;
            int o = e.offset;
            _unitsPerEm = U16(_data, o + 18);
            _xMin = S16(_data, o + 36);
            _yMin = S16(_data, o + 38);
            _xMax = S16(_data, o + 40);
            _yMax = S16(_data, o + 42);
        }

        private void ParseHhea(Dictionary<string, (int offset, int length)> t)
        {
            if (!t.TryGetValue("hhea", out var e)) return;
            int o = e.offset;
            _ascender          = S16(_data, o + 4);
            _descender         = S16(_data, o + 6);
            _numberOfHMetrics  = U16(_data, o + 34);
        }

        private void ParseMaxp(Dictionary<string, (int offset, int length)> t)
        {
            if (!t.TryGetValue("maxp", out var e)) return;
            _numGlyphs = U16(_data, e.offset + 4);
        }

        private void ParseHmtx(Dictionary<string, (int offset, int length)> t)
        {
            if (!t.TryGetValue("hmtx", out var e)) return;
            int o = e.offset;
            _advanceWidths = new ushort[_numGlyphs];
            int nHM = Math.Min(_numberOfHMetrics, _numGlyphs);
            for (int i = 0; i < nHM; i++)
                _advanceWidths[i] = U16(_data, o + i * 4);
            ushort last = nHM > 0 ? _advanceWidths[nHM - 1] : (ushort)0;
            for (int i = nHM; i < _numGlyphs; i++)
                _advanceWidths[i] = last;
        }

        private void ParseCmap(Dictionary<string, (int offset, int length)> t)
        {
            _charToGlyph = new Dictionary<int, ushort>();
            if (!t.TryGetValue("cmap", out var e)) return;
            int tableBase = e.offset;
            int numSub = U16(_data, tableBase + 2);

            // Prefer: platform 3 enc 10 format 12, then 3/1 format 4, then 0/* format 4
            int bestOff = -1, bestPri = -1;
            for (int i = 0; i < numSub; i++)
            {
                int r = tableBase + 4 + i * 8;
                int pid = U16(_data, r);
                int eid = U16(_data, r + 2);
                int subOff = tableBase + S32(_data, r + 4);
                int fmt = U16(_data, subOff);
                int pri = -1;
                if (pid == 3 && eid == 10 && fmt == 12) pri = 3;
                else if (pid == 3 && eid ==  1 && fmt ==  4) pri = 2;
                else if (pid == 0 && eid ==  3 && fmt ==  4) pri = 1;
                else if (pid == 0 &&               fmt ==  4) pri = 0;
                if (pri > bestPri) { bestPri = pri; bestOff = subOff; }
            }

            if (bestOff < 0) return;
            int format = U16(_data, bestOff);
            if (format == 4)  ParseCmapFormat4(bestOff);
            if (format == 12) ParseCmapFormat12(bestOff);
        }

        private void ParseCmapFormat4(int o)
        {
            int segCount       = U16(_data, o + 6) / 2;
            int endCodesOff    = o + 14;
            int startCodesOff  = endCodesOff + 2 + segCount * 2;
            int idDeltaOff     = startCodesOff + segCount * 2;
            int idRangeOffOff  = idDeltaOff + segCount * 2;
            int glyphIdArrOff  = idRangeOffOff + segCount * 2;

            for (int i = 0; i < segCount; i++)
            {
                int endCode   = U16(_data, endCodesOff + i * 2);
                if (endCode == 0xFFFF) break;
                int startCode = U16(_data, startCodesOff + i * 2);
                int delta     = S16(_data, idDeltaOff + i * 2);
                int rangeOff  = U16(_data, idRangeOffOff + i * 2);

                for (int c = startCode; c <= endCode; c++)
                {
                    ushort gid;
                    if (rangeOff == 0)
                    {
                        gid = (ushort)((c + delta) & 0xFFFF);
                    }
                    else
                    {
                        int ptr = idRangeOffOff + i * 2 + rangeOff + (c - startCode) * 2;
                        if (ptr + 1 >= _data.Length) { gid = 0; }
                        else
                        {
                            ushort raw = U16(_data, ptr);
                            gid = raw == 0 ? (ushort)0 : (ushort)((raw + delta) & 0xFFFF);
                        }
                    }
                    if (gid != 0) _charToGlyph[c] = gid;
                }
            }
        }

        private void ParseCmapFormat12(int o)
        {
            int numGroups = S32(_data, o + 12);
            for (int i = 0; i < numGroups; i++)
            {
                int e2 = o + 16 + i * 12;
                int start = S32(_data, e2);
                int end   = S32(_data, e2 + 4);
                int glyph = S32(_data, e2 + 8);
                for (int c = start; c <= end; c++)
                    _charToGlyph[c] = (ushort)(glyph + (c - start));
            }
        }

        private void ParseName(Dictionary<string, (int offset, int length)> t)
        {
            if (!t.TryGetValue("name", out var e)) return;
            int o = e.offset;
            int count    = U16(_data, o + 2);
            int strBase  = o + U16(_data, o + 4);
            string latinName = null, unicodeName = null;
            for (int i = 0; i < count; i++)
            {
                int r  = o + 6 + i * 12;
                int pid = U16(_data, r);
                int nid = U16(_data, r + 6);
                int len = U16(_data, r + 8);
                int off = U16(_data, r + 10);
                if (nid != 6) continue;
                if (pid == 3) unicodeName = Encoding.BigEndianUnicode.GetString(_data, strBase + off, len);
                else          latinName  = Encoding.ASCII.GetString(_data, strBase + off, len);
            }
            _postScriptName = unicodeName ?? latinName;
        }

        // ── public API ───────────────────────────────────────────────────────

        internal ushort GetGlyphId(int codePoint)
        {
            _charToGlyph.TryGetValue(codePoint, out var gid);
            return gid;
        }

        internal ushort GetAdvanceWidth(ushort glyphId)
        {
            if (_advanceWidths == null || glyphId >= _advanceWidths.Length) return 0;
            return _advanceWidths[glyphId];
        }

        internal float GetWidthPoint(string text, float fontSize)
        {
            if (_unitsPerEm == 0 || _advanceWidths == null || text == null) return 0f;
            float total = 0f;
            foreach (char ch in text)
                total += GetAdvanceWidth(GetGlyphId(ch));
            return total / _unitsPerEm * fontSize;
        }

        // Returns all Unicode code-point → glyph-ID pairs present in the font.
        internal IEnumerable<(int codePoint, ushort glyphId)> GetCharGlyphMappings()
            => _charToGlyph.Select(kv => (kv.Key, kv.Value));
    }
}
