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

namespace Majorsilence.Pdf
{
    // Minimal TrueType/OpenType font parser.
    // Extracts only what is needed for embedding a CIDFont Type2 in a PDF:
    //   PostScript name, metrics (UPM, ascender, descender, bounding box),
    //   per-glyph advance widths, and the Unicode → glyph-ID mapping.
    public sealed class TrueTypeFont
    {
        private readonly byte[] _data;
        private int _unitsPerEm;
        private short _ascender;
        private short _descender;
        private int _numGlyphs;
        private int _numberOfHMetrics;
        private ushort[]? _advanceWidths;
        private Dictionary<int, ushort> _charToGlyph = new Dictionary<int, ushort>();
        private string? _postScriptName;
        private short _xMin, _yMin, _xMax, _yMax;

        // Color bitmap (CBDT/CBLC) fields — populated only when no outline glyf table is present.
        private bool _isColorBitmapOnly;
        private int _cbdtPpem;
        // Maps glyph ID → (absolute byte offset in _data, record length)
        private readonly Dictionary<ushort, (int offset, int length)> _cbdtGlyphs =
            new Dictionary<ushort, (int, int)>();

        public int UnitsPerEm => _unitsPerEm;
        public short Ascender => _ascender;
        public short Descender => _descender;
        public int NumGlyphs => _numGlyphs;
        public string PostScriptName => _postScriptName ?? "UnknownFont";
        public short XMin => _xMin;
        public short YMin => _yMin;
        public short XMax => _xMax;
        public short YMax => _yMax;
        public byte[] Data => _data;

        // True when the font contains only color bitmap glyphs (CBDT/CBLC) and no outlines.
        public bool IsColorBitmapOnly => _isColorBitmapOnly;

        // Pixels per em for the color bitmap strike (e.g. 109 for NotoColorEmoji).
        public int ColorBitmapPpem => _cbdtPpem;

        public TrueTypeFont(string path) : this(File.ReadAllBytes(path)) { }

        public TrueTypeFont(byte[] data)
        {
            _data = data;
            Parse();
        }

        // ── binary helpers (big-endian) ──────────────────────────────────────

        private static ushort U16(byte[] b, int o) => (ushort)((b[o] << 8) | b[o + 1]);
        private static short  S16(byte[] b, int o) => (short)((b[o] << 8) | b[o + 1]);
        private static int    S32(byte[] b, int o) => (b[o] << 24) | (b[o+1] << 16) | (b[o+2] << 8) | b[o+3];

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
            if (!tables.ContainsKey("glyf") && tables.ContainsKey("CBDT") && tables.ContainsKey("CBLC"))
            {
                _isColorBitmapOnly = true;
                ParseColorBitmaps(tables);
            }
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
            _ascender         = S16(_data, o + 4);
            _descender        = S16(_data, o + 6);
            _numberOfHMetrics = U16(_data, o + 34);
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
            int segCount      = U16(_data, o + 6) / 2;
            int endCodesOff   = o + 14;
            int startCodesOff = endCodesOff + 2 + segCount * 2;
            int idDeltaOff    = startCodesOff + segCount * 2;
            int idRangeOffOff = idDeltaOff + segCount * 2;

            for (int i = 0; i < segCount; i++)
            {
                int endCode = U16(_data, endCodesOff + i * 2);
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
                int e2    = o + 16 + i * 12;
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
            int count   = U16(_data, o + 2);
            int strBase = o + U16(_data, o + 4);
            string? latinName = null, unicodeName = null;
            for (int i = 0; i < count; i++)
            {
                int r   = o + 6 + i * 12;
                int pid = U16(_data, r);
                int nid = U16(_data, r + 6);
                int len = U16(_data, r + 8);
                int off = U16(_data, r + 10);
                if (nid != 6) continue;
                if (pid == 3) unicodeName = Encoding.BigEndianUnicode.GetString(_data, strBase + off, len);
                else          latinName   = Encoding.ASCII.GetString(_data, strBase + off, len);
            }
            _postScriptName = unicodeName ?? latinName;
        }

        // ── CBDT / CBLC color bitmap parsing ────────────────────────────────

        // Parses the CBLC index and maps every glyph ID to its record position in CBDT.
        // Supports IndexSubTable formats 1 (uint32 offsets) and 3 (uint16 offsets).
        // Images are expected to be CBDT format 17: SmallGlyphMetrics(5) + uint32 length + PNG.
        private void ParseColorBitmaps(Dictionary<string, (int offset, int length)> tables)
        {
            if (!tables.TryGetValue("CBLC", out var cblcEntry) ||
                !tables.TryGetValue("CBDT", out var cbdtEntry)) return;

            int cblc = cblcEntry.offset;
            int cbdt = cbdtEntry.offset;

            int numSizes = S32(_data, cblc + 4);
            if (numSizes <= 0) return;

            // Pick the size table with the largest ppem (best quality).
            int bestPpem = -1, bestBst = -1;
            for (int si = 0; si < numSizes; si++)
            {
                int bst = cblc + 8 + si * 48;
                int ppem = _data[bst + 44]; // ppemX
                if (ppem > bestPpem) { bestPpem = ppem; bestBst = bst; }
            }
            if (bestBst < 0) return;
            _cbdtPpem = bestPpem;

            int idxArrOff = S32(_data, bestBst);      // offset within CBLC
            int numSubTables = S32(_data, bestBst + 8);
            int istaBase = cblc + idxArrOff;

            for (int sti = 0; sti < numSubTables; sti++)
            {
                int entry = istaBase + sti * 8;
                int firstGlyph = U16(_data, entry);
                int lastGlyph  = U16(_data, entry + 2);
                int addlOff    = S32(_data, entry + 4); // from istaBase
                int ist = istaBase + addlOff;           // IndexSubTable header

                int idxFmt     = U16(_data, ist);
                // imageFormat at ist+2 (not needed — we always parse as format 17)
                int imgDataOff = S32(_data, ist + 4);   // offset from CBDT start

                if (idxFmt == 1)
                {
                    // uint32 offsets
                    for (int gi = firstGlyph; gi <= lastGlyph; gi++)
                    {
                        int idx = gi - firstGlyph;
                        int o0 = S32(_data, ist + 8 + idx * 4);
                        int o1 = S32(_data, ist + 8 + (idx + 1) * 4);
                        if (o1 <= o0) continue;
                        _cbdtGlyphs[(ushort)gi] = (cbdt + imgDataOff + o0, o1 - o0);
                    }
                }
                else if (idxFmt == 3)
                {
                    // uint16 offsets
                    for (int gi = firstGlyph; gi <= lastGlyph; gi++)
                    {
                        int idx = gi - firstGlyph;
                        int o0 = U16(_data, ist + 8 + idx * 2);
                        int o1 = U16(_data, ist + 8 + (idx + 1) * 2);
                        if (o1 <= o0) continue;
                        _cbdtGlyphs[(ushort)gi] = (cbdt + imgDataOff + o0, o1 - o0);
                    }
                }
            }
        }

        // Extracts the raw PNG bytes for a color bitmap glyph and returns its pixel metrics.
        // The record layout is: SmallGlyphMetrics(5 bytes) + uint32 pngLength + PNG data.
        public bool TryGetGlyphPng(ushort glyphId,
            out byte[] pngData,
            out byte pixelWidth, out byte pixelHeight,
            out sbyte bearingX, out sbyte bearingY)
        {
            pngData = Array.Empty<byte>();
            pixelWidth = pixelHeight = 0;
            bearingX = bearingY = 0;

            if (!_cbdtGlyphs.TryGetValue(glyphId, out var rec)) return false;
            int o = rec.offset;
            if (o + 9 > _data.Length) return false;

            pixelHeight = _data[o];
            pixelWidth  = _data[o + 1];
            bearingX    = (sbyte)_data[o + 2];
            bearingY    = (sbyte)_data[o + 3];
            // advance at _data[o + 4] — use hmtx instead

            int pngLen = S32(_data, o + 5);
            if (pngLen <= 0 || o + 9 + pngLen > _data.Length) return false;

            pngData = new byte[pngLen];
            Buffer.BlockCopy(_data, o + 9, pngData, 0, pngLen);
            return true;
        }

        // ── public API ───────────────────────────────────────────────────────

        public ushort GetGlyphId(int codePoint)
        {
            _charToGlyph.TryGetValue(codePoint, out var gid);
            return gid;
        }

        public ushort GetAdvanceWidth(ushort glyphId)
        {
            if (_advanceWidths == null || glyphId >= _advanceWidths.Length) return 0;
            return _advanceWidths[glyphId];
        }

        public float GetWidthPoint(string text, float fontSize)
        {
            if (_unitsPerEm == 0 || _advanceWidths == null || text == null) return 0f;
            float total = 0f;
            for (int i = 0; i < text.Length; i++)
            {
                int cp;
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    cp = char.ConvertToUtf32(text[i], text[i + 1]);
                    i++;
                }
                else
                {
                    cp = text[i];
                }
                total += GetAdvanceWidth(GetGlyphId(cp));
            }
            return total / _unitsPerEm * fontSize;
        }

        // Returns all Unicode code-point → glyph-ID pairs present in the font.
        public IEnumerable<(int codePoint, ushort glyphId)> GetCharGlyphMappings()
            => _charToGlyph.Select(kv => (kv.Key, kv.Value));

        // ── font subsetting ──────────────────────────────────────────────────

        // Returns a new font binary containing only the glyphs in usedGlyphIds
        // (plus glyph 0 / .notdef and any composite components).
        // All other glyph slots are present in loca/glyf as zero-length entries
        // so original glyph IDs remain valid — no re-encoding needed.
        public byte[] Subset(HashSet<ushort> usedGlyphIds)
        {
            // Parse table directory
            int numTables = U16(_data, 4);
            var tables = new Dictionary<string, (int offset, int length)>(numTables, StringComparer.Ordinal);
            for (int i = 0; i < numTables; i++)
            {
                int e = 12 + i * 16;
                string tag = Encoding.ASCII.GetString(_data, e, 4);
                tables[tag] = (S32(_data, e + 8), S32(_data, e + 12));
            }

            if (!tables.TryGetValue("loca", out var locaEntry) ||
                !tables.TryGetValue("glyf", out var glyfEntry) ||
                !tables.TryGetValue("head", out var headEntry))
                return _data; // can't subset without these tables

            int locFormat = S16(_data, headEntry.offset + 50); // 0=short, 1=long

            // Read loca — build array of (offset, length) per glyph
            var glyphOffsets = new int[_numGlyphs + 1];
            if (locFormat == 0)
                for (int i = 0; i <= _numGlyphs; i++)
                    glyphOffsets[i] = U16(_data, locaEntry.offset + i * 2) * 2;
            else
                for (int i = 0; i <= _numGlyphs; i++)
                    glyphOffsets[i] = (int)(uint)S32(_data, locaEntry.offset + i * 4);

            // Expand to include glyph 0 and composite components
            var needed = new HashSet<ushort>(usedGlyphIds) { 0 };
            var queue = new Queue<ushort>(needed);
            while (queue.Count > 0)
            {
                ushort gid = queue.Dequeue();
                if (gid >= _numGlyphs) continue;
                int gLen = glyphOffsets[gid + 1] - glyphOffsets[gid];
                if (gLen < 10) continue;
                int gOff = glyfEntry.offset + glyphOffsets[gid];
                if (S16(_data, gOff) >= 0) continue; // simple glyph, no components

                // Composite glyph — walk component records
                int pos = gOff + 10;
                while (pos + 4 <= _data.Length)
                {
                    ushort flags = U16(_data, pos);
                    ushort compGid = U16(_data, pos + 2);
                    pos += 4;
                    pos += (flags & 0x0001) != 0 ? 4 : 2; // ARG_1_AND_2_ARE_WORDS
                    if      ((flags & 0x0080) != 0) pos += 8; // WE_HAVE_A_TWO_BY_TWO
                    else if ((flags & 0x0040) != 0) pos += 4; // WE_HAVE_AN_X_AND_Y_SCALE
                    else if ((flags & 0x0008) != 0) pos += 2; // WE_HAVE_A_SCALE
                    if (needed.Add(compGid)) queue.Enqueue(compGid);
                    if ((flags & 0x0020) == 0) break;         // MORE_COMPONENTS
                }
            }

            // Build new glyf — only include outline data for needed glyphs
            var newGlyf = new MemoryStream();
            var newLocaOff = new int[_numGlyphs + 1];
            for (int gid = 0; gid < _numGlyphs; gid++)
            {
                newLocaOff[gid] = (int)newGlyf.Position;
                if (needed.Contains((ushort)gid))
                {
                    int srcOff = glyfEntry.offset + glyphOffsets[gid];
                    int srcLen = glyphOffsets[gid + 1] - glyphOffsets[gid];
                    if (srcLen > 0)
                        newGlyf.Write(_data, srcOff, srcLen);
                }
                // else: loca[gid]==loca[gid+1] → zero-length / empty glyph
            }
            newLocaOff[_numGlyphs] = (int)newGlyf.Position;
            byte[] newGlyfData = newGlyf.ToArray();

            // Build new loca (always long format to keep it simple)
            byte[] newLocaData = new byte[(_numGlyphs + 1) * 4];
            for (int i = 0; i <= _numGlyphs; i++)
                WriteU32(newLocaData, i * 4, (uint)newLocaOff[i]);

            // Patch head.indexToLocFormat = 1 (long)
            byte[] newHead = new byte[headEntry.length];
            Buffer.BlockCopy(_data, headEntry.offset, newHead, 0, headEntry.length);
            newHead[50] = 0; newHead[51] = 1;

            // Assemble all tables: keep everything, replace glyf / loca / head
            var tagList = new List<string>(tables.Keys);
            tagList.Sort(StringComparer.Ordinal);
            int nT = tagList.Count;

            // First pass: compute table data bytes
            var tableData = new Dictionary<string, byte[]>(nT, StringComparer.Ordinal);
            foreach (var tag in tagList)
            {
                if      (tag == "glyf") tableData[tag] = newGlyfData;
                else if (tag == "loca") tableData[tag] = newLocaData;
                else if (tag == "head") tableData[tag] = newHead;
                else
                {
                    var (off, len) = tables[tag];
                    var td = new byte[len];
                    Buffer.BlockCopy(_data, off, td, 0, len);
                    tableData[tag] = td;
                }
            }

            // Second pass: lay out table offsets (each table padded to 4 bytes)
            int headerBytes = 12 + nT * 16;
            var tableOffset = new Dictionary<string, int>(nT, StringComparer.Ordinal);
            int cursor = headerBytes;
            foreach (var tag in tagList)
            {
                tableOffset[tag] = cursor;
                cursor += tableData[tag].Length;
                if (cursor % 4 != 0) cursor += 4 - cursor % 4;
            }

            // Third pass: write font bytes
            byte[] output = new byte[cursor];

            // Offset table (sfVersion, numTables, searchRange, entrySelector, rangeShift)
            int sfVer = S32(_data, 0);
            WriteU32(output, 0, (uint)sfVer);
            WriteU16(output, 4, (ushort)nT);
            int p2 = 1; while (p2 * 2 <= nT) p2 *= 2;
            WriteU16(output, 6, (ushort)(p2 * 16));
            int es = 0; int tmp = p2; while (tmp > 1) { tmp >>= 1; es++; }
            WriteU16(output, 8,  (ushort)es);
            WriteU16(output, 10, (ushort)((nT - p2) * 16));

            // Table directory + data
            int dirBase = 12;
            foreach (var tag in tagList)
            {
                byte[] td   = tableData[tag];
                int    toff = tableOffset[tag];
                uint   tcs  = TableChecksum(td);
                Encoding.ASCII.GetBytes(tag.PadRight(4), 0, 4, output, dirBase);
                WriteU32(output, dirBase + 4,  tcs);
                WriteU32(output, dirBase + 8,  (uint)toff);
                WriteU32(output, dirBase + 12, (uint)td.Length);
                dirBase += 16;
                Buffer.BlockCopy(td, 0, output, toff, td.Length);
            }

            // Fix head.checkSumAdjustment (bytes 8-11 of the head table in output)
            int headOutOff = tableOffset["head"];
            output[headOutOff + 8] = output[headOutOff + 9] =
            output[headOutOff + 10] = output[headOutOff + 11] = 0;
            uint whole = TableChecksum(output);
            WriteU32(output, headOutOff + 8, 0xB1B0AFBA - whole);

            return output;
        }

        // ── binary write helpers ─────────────────────────────────────────────

        private static void WriteU16(byte[] b, int o, ushort v)
            { b[o] = (byte)(v >> 8); b[o + 1] = (byte)v; }

        private static void WriteU32(byte[] b, int o, uint v)
            { b[o] = (byte)(v >> 24); b[o+1] = (byte)(v >> 16); b[o+2] = (byte)(v >> 8); b[o+3] = (byte)v; }

        private static uint TableChecksum(byte[] data)
        {
            uint sum = 0;
            int i = 0;
            for (; i + 3 < data.Length; i += 4)
                sum += ((uint)data[i] << 24) | ((uint)data[i+1] << 16) | ((uint)data[i+2] << 8) | data[i+3];
            if (i < data.Length)
            {
                uint tail = 0;
                for (int j = i; j < data.Length; j++)
                    tail |= (uint)data[j] << ((3 - (j - i)) * 8);
                sum += tail;
            }
            return sum;
        }
    }
}
