// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;
using System.Collections.Generic;
using System.IO;

namespace Majorsilence.Pdf
{
    /// <summary>
    /// Subsets the <c>CFF </c> table of an OpenType font (Compact Font Format, version 1) the way
    /// <see cref="TrueTypeFont.Subset"/> subsets <c>glyf</c>: every glyph ID stays valid, and the
    /// outlines of glyphs that are not used are replaced by a one-byte <c>endchar</c>.
    ///
    /// Subroutines that no kept glyph can reach are replaced by a one-byte <c>return</c> (the
    /// subroutine count, and so the call bias, is unchanged). A Type 2 charstring interpreter
    /// finds the reachable ones.
    ///
    /// The CFF is rebuilt in a fixed order, with the Top DICT and Font DICT offsets recomputed.
    /// A CID-keyed font gets an identity charset (CID == glyph ID), so a PDF reader finds the
    /// glyph by the same number whether it treats the CID as a CID or as a glyph ID.
    /// CFF2 (variable fonts) is not supported.
    /// </summary>
    internal static class CffSubsetter
    {
        private const byte Endchar = 14;
        private const byte Return = 11;

        // ── INDEX ────────────────────────────────────────────────────────────

        private sealed class Index
        {
            internal byte[][] Items = Array.Empty<byte[]>();
            internal int End; // offset just past the INDEX in the source
            internal int Count => Items.Length;
        }

        private static Index ReadIndex(byte[] d, int pos)
        {
            int count = (d[pos] << 8) | d[pos + 1];
            var idx = new Index();
            if (count == 0) { idx.End = pos + 2; return idx; }
            int offSize = d[pos + 2];
            if (offSize < 1 || offSize > 4) throw new InvalidDataException("bad CFF INDEX offSize");
            int offBase = pos + 3;
            int dataBase = offBase + (count + 1) * offSize - 1;
            int Off(int i)
            {
                int v = 0;
                for (int k = 0; k < offSize; k++) v = (v << 8) | d[offBase + i * offSize + k];
                return v;
            }
            idx.Items = new byte[count][];
            int prev = Off(0);
            for (int i = 0; i < count; i++)
            {
                int next = Off(i + 1);
                if (next < prev || dataBase + next > d.Length) throw new InvalidDataException("bad CFF INDEX offset");
                var item = new byte[next - prev];
                Buffer.BlockCopy(d, dataBase + prev, item, 0, item.Length);
                idx.Items[i] = item;
                prev = next;
            }
            idx.End = dataBase + prev;
            return idx;
        }

        private static void WriteIndex(MemoryStream o, IList<byte[]> items)
        {
            o.WriteByte((byte)(items.Count >> 8));
            o.WriteByte((byte)items.Count);
            if (items.Count == 0) return;
            long total = 1;
            foreach (var it in items) total += it.Length;
            int offSize = total <= 0xFF ? 1 : total <= 0xFFFF ? 2 : total <= 0xFFFFFF ? 3 : 4;
            o.WriteByte((byte)offSize);
            long cur = 1;
            WriteOff(o, cur, offSize);
            foreach (var it in items) { cur += it.Length; WriteOff(o, cur, offSize); }
            foreach (var it in items) o.Write(it, 0, it.Length);
        }

        private static void WriteOff(MemoryStream o, long v, int size)
        {
            for (int k = size - 1; k >= 0; k--) o.WriteByte((byte)(v >> (8 * k)));
        }

        // ── DICT ─────────────────────────────────────────────────────────────

        // An operator (12 xx is 0x0C00 | xx) with its operand bytes exactly as stored, and the
        // operand values for the ones that are integers.
        private sealed class DictEntry
        {
            internal int Op;
            internal byte[] RawOperands = Array.Empty<byte>();
            internal List<int> Ints = new List<int>();
        }

        private static List<DictEntry> ParseDict(byte[] d)
        {
            var list = new List<DictEntry>();
            var cur = new DictEntry();
            int start = 0, i = 0;
            while (i < d.Length)
            {
                int b = d[i];
                if (b <= 21)
                {
                    int op = b;
                    int opLen = 1;
                    if (b == 12) { if (i + 1 >= d.Length) break; op = 0x0C00 | d[i + 1]; opLen = 2; }
                    cur.Op = op;
                    cur.RawOperands = new byte[i - start];
                    Buffer.BlockCopy(d, start, cur.RawOperands, 0, cur.RawOperands.Length);
                    list.Add(cur);
                    cur = new DictEntry();
                    i += opLen;
                    start = i;
                }
                else if (b == 28) { cur.Ints.Add((short)((d[i + 1] << 8) | d[i + 2])); i += 3; }
                else if (b == 29) { cur.Ints.Add((d[i + 1] << 24) | (d[i + 2] << 16) | (d[i + 3] << 8) | d[i + 4]); i += 5; }
                else if (b == 30)
                {
                    i++;
                    while (i < d.Length)
                    {
                        int n = d[i++];
                        if ((n & 0x0F) == 0x0F || (n >> 4) == 0x0F) break;
                    }
                    cur.Ints.Add(0);
                }
                else if (b >= 32 && b <= 246) { cur.Ints.Add(b - 139); i++; }
                else if (b >= 247 && b <= 250) { cur.Ints.Add((b - 247) * 256 + d[i + 1] + 108); i += 2; }
                else if (b >= 251 && b <= 254) { cur.Ints.Add(-(b - 251) * 256 - d[i + 1] - 108); i += 2; }
                else i++; // reserved
            }
            return list;
        }

        private static void AppendInt5(List<byte> o, int v)
        {
            o.Add(29);
            o.Add((byte)(v >> 24)); o.Add((byte)(v >> 16)); o.Add((byte)(v >> 8)); o.Add((byte)v);
        }

        private static void AppendOp(List<byte> o, int op)
        {
            if (op > 0xFF) { o.Add(12); o.Add((byte)op); } else o.Add((byte)op);
        }

        private const int OpCharset = 15, OpEncoding = 16, OpCharStrings = 17, OpPrivate = 18,
            OpSubrs = 19, OpRos = 0x0C1E, OpFdArray = 0x0C24, OpFdSelect = 0x0C25, OpCharstringType = 0x0C06;

        private static DictEntry? Find(List<DictEntry> d, int op) => d.Find(e => e.Op == op);

        // ── subset ───────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuild <paramref name="cff"/> keeping the outlines of <paramref name="used"/> (glyph 0
        /// is always kept); <c>null</c> keeps every glyph.
        /// </summary>
        internal static byte[] Subset(byte[] cff, ICollection<ushort>? used)
        {
            if (cff.Length < 4 || cff[0] != 1) throw new InvalidDataException("not a CFF 1.x table");
            int hdrSize = cff[2];

            var names = ReadIndex(cff, hdrSize);
            var topDicts = ReadIndex(cff, names.End);
            var strings = ReadIndex(cff, topDicts.End);
            var gsubrs = ReadIndex(cff, strings.End);
            if (topDicts.Count < 1) throw new InvalidDataException("CFF has no Top DICT");

            var top = ParseDict(topDicts.Items[0]);
            var csEntry = Find(top, OpCharStrings) ?? throw new InvalidDataException("CFF has no CharStrings");
            var charStrings = ReadIndex(cff, csEntry.Ints[0]);
            int nGlyphs = charStrings.Count;
            if (Find(top, OpCharstringType)?.Ints[0] == 1) throw new NotSupportedException("Type 1 charstrings");

            bool isCid = Find(top, OpRos) != null;

            // Font DICTs and their Private DICTs / local subrs. A name-keyed font has one Private.
            var fontDicts = new List<List<DictEntry>>();
            var privates = new List<byte[]>();      // Private DICT bytes as stored
            var localSubrs = new List<Index>();
            if (isCid)
            {
                var fdEntry = Find(top, OpFdArray) ?? throw new InvalidDataException("CID font without FDArray");
                var fdArray = ReadIndex(cff, fdEntry.Ints[0]);
                foreach (var fd in fdArray.Items)
                {
                    var fdd = ParseDict(fd);
                    fontDicts.Add(fdd);
                    ReadPrivate(cff, Find(fdd, OpPrivate), privates, localSubrs);
                }
            }
            else
            {
                ReadPrivate(cff, Find(top, OpPrivate), privates, localSubrs);
            }

            byte[]? fdSelect = null;
            byte[]? fdOfGlyph = null;
            if (isCid)
            {
                int fdsPos = (Find(top, OpFdSelect) ?? throw new InvalidDataException("CID font without FDSelect")).Ints[0];
                fdSelect = ReadFdSelect(cff, fdsPos, nGlyphs, out fdOfGlyph);
            }

            // Which glyphs are kept.
            var keep = new bool[nGlyphs];
            keep[0] = true;
            if (used == null) { for (int g = 0; g < nGlyphs; g++) keep[g] = true; }
            else foreach (ushort g in used) if (g < nGlyphs) keep[g] = true;

            // Walk the kept charstrings for reachable subroutines.
            var gUsed = new bool[gsubrs.Count];
            var lUsed = new bool[localSubrs.Count][];
            for (int i = 0; i < lUsed.Length; i++) lUsed[i] = new bool[localSubrs[i].Count];
            for (int g = 0; g < nGlyphs; g++)
            {
                if (!keep[g]) continue;
                int fd = fdOfGlyph != null ? fdOfGlyph[g] : 0;
                if (fd >= localSubrs.Count) fd = 0;
                new CharstringWalker(gsubrs.Items, gUsed, localSubrs[fd].Items, lUsed[fd]).Walk(charStrings.Items[g]);
            }

            var newCs = new byte[nGlyphs][];
            for (int g = 0; g < nGlyphs; g++)
                newCs[g] = keep[g] ? charStrings.Items[g] : new[] { Endchar };
            var newG = PruneSubrs(gsubrs.Items, gUsed);
            var newLocals = new List<byte[][]>();
            for (int i = 0; i < localSubrs.Count; i++) newLocals.Add(PruneSubrs(localSubrs[i].Items, lUsed[i]));

            // charset: identity for a CID font; a name-keyed font keeps its own.
            byte[] charset;
            var csetEntry = Find(top, OpCharset);
            if (isCid)
            {
                charset = nGlyphs <= 1
                    ? new byte[] { 0 }
                    : new byte[] { 2, 0, 1, (byte)((nGlyphs - 2) >> 8), (byte)(nGlyphs - 2) };
            }
            else if (csetEntry != null && csetEntry.Ints[0] > 2)
            {
                charset = CopyCharset(cff, csetEntry.Ints[0], nGlyphs);
            }
            else
            {
                charset = Array.Empty<byte>(); // predefined charset 0..2: nothing to store
            }
            bool writeCharset = charset.Length > 0;

            // Layout. The Top DICT's offsets are all five-byte integers, so its size does not depend on them.
            var nameBytes = new MemoryStream(); WriteIndex(nameBytes, names.Items);
            var strBytes = new MemoryStream(); WriteIndex(strBytes, strings.Items);
            var gsubrBytes = new MemoryStream(); WriteIndex(gsubrBytes, newG);
            var csBytes = new MemoryStream(); WriteIndex(csBytes, newCs);

            // -1 leaves the charset entry out (a name-keyed font that never had one).
            int charsetArg = writeCharset ? -2 : (csetEntry?.Ints[0] ?? -1);
            int topSize = BuildTop(top, isCid, charsetArg == -2 ? 0 : charsetArg, 0, 0, 0, 0, 0).Length;
            int topIndexSize = IndexSize(topSize);

            int pos = hdrSize + (int)nameBytes.Length + topIndexSize + (int)strBytes.Length + (int)gsubrBytes.Length;
            int charsetPos = pos; pos += charset.Length;
            int fdSelectPos = pos; pos += fdSelect?.Length ?? 0;
            int csPos = pos; pos += (int)csBytes.Length;

            // Private DICT + local subrs, one block per Private.
            var privBlocks = new List<byte[]>();
            var privPos = new int[privates.Count];
            var privSize = new int[privates.Count];
            for (int i = 0; i < privates.Count; i++)
            {
                var pd = ParseDict(privates[i]);
                var subrsEntry = Find(pd, OpSubrs);
                bool hasSubrs = subrsEntry != null && localSubrs[i].Count > 0;
                byte[] pdBytes = BuildPrivate(pd, hasSubrs);
                var block = new MemoryStream();
                block.Write(pdBytes, 0, pdBytes.Length);
                if (hasSubrs) WriteIndex(block, newLocals[i]);
                privPos[i] = pos; privSize[i] = pdBytes.Length;
                privBlocks.Add(block.ToArray());
                pos += privBlocks[i].Length;
            }

            int fdArrayPos = pos;
            byte[]? fdArrayBytes = null;
            if (isCid)
            {
                var fds = new List<byte[]>();
                for (int i = 0; i < fontDicts.Count; i++)
                    fds.Add(BuildFontDict(fontDicts[i], privSize[i], privPos[i]));
                var ms = new MemoryStream(); WriteIndex(ms, fds); fdArrayBytes = ms.ToArray();
            }

            byte[] topBytes = BuildTop(top, isCid, writeCharset ? charsetPos : charsetArg,
                csPos, isCid ? fdArrayPos : 0, isCid ? fdSelectPos : 0,
                isCid ? 0 : privSize[0], isCid ? 0 : privPos[0]);

            var o = new MemoryStream();
            o.Write(cff, 0, hdrSize);
            o.Write(nameBytes.ToArray(), 0, (int)nameBytes.Length);
            WriteIndex(o, new[] { topBytes });
            o.Write(strBytes.ToArray(), 0, (int)strBytes.Length);
            o.Write(gsubrBytes.ToArray(), 0, (int)gsubrBytes.Length);
            o.Write(charset, 0, charset.Length);
            if (fdSelect != null) o.Write(fdSelect, 0, fdSelect.Length);
            o.Write(csBytes.ToArray(), 0, (int)csBytes.Length);
            foreach (var b in privBlocks) o.Write(b, 0, b.Length);
            if (fdArrayBytes != null) o.Write(fdArrayBytes, 0, fdArrayBytes.Length);
            return o.ToArray();
        }

        /// <summary>The parts of a CFF a test or a diagnostic wants to look at.</summary>
        internal sealed class Contents
        {
            internal byte[][] CharStrings = Array.Empty<byte[]>();
            internal byte[][] GlobalSubrs = Array.Empty<byte[]>();
            internal byte[][][] LocalSubrs = Array.Empty<byte[][]>();
            internal bool IsCid;
            /// <summary>For a CID font, the CID of each glyph; null otherwise.</summary>
            internal int[]? Cids;
        }

        internal static Contents Read(byte[] cff)
        {
            int hdrSize = cff[2];
            var names = ReadIndex(cff, hdrSize);
            var topDicts = ReadIndex(cff, names.End);
            var strings = ReadIndex(cff, topDicts.End);
            var gsubrs = ReadIndex(cff, strings.End);
            var top = ParseDict(topDicts.Items[0]);
            var charStrings = ReadIndex(cff, Find(top, OpCharStrings)!.Ints[0]);
            var c = new Contents { CharStrings = charStrings.Items, GlobalSubrs = gsubrs.Items, IsCid = Find(top, OpRos) != null };
            var privates = new List<byte[]>();
            var locals = new List<Index>();
            if (c.IsCid)
            {
                foreach (var fd in ReadIndex(cff, Find(top, OpFdArray)!.Ints[0]).Items)
                    ReadPrivate(cff, Find(ParseDict(fd), OpPrivate), privates, locals);
                var cs = Find(top, OpCharset)!.Ints[0];
                int n = charStrings.Count;
                var cids = new int[n];
                if (cff[cs] == 0) for (int g = 1; g < n; g++) cids[g] = (cff[cs + 1 + 2 * (g - 1)] << 8) | cff[cs + 2 + 2 * (g - 1)];
                else
                {
                    int p = cs + 1, g = 1;
                    while (g < n)
                    {
                        int first = (cff[p] << 8) | cff[p + 1];
                        int left = cff[cs] == 1 ? cff[p + 2] : (cff[p + 2] << 8) | cff[p + 3];
                        p += cff[cs] == 1 ? 3 : 4;
                        for (int k = 0; k <= left && g < n; k++) cids[g++] = first + k;
                    }
                }
                c.Cids = cids;
            }
            else ReadPrivate(cff, Find(top, OpPrivate), privates, locals);
            c.LocalSubrs = locals.ConvertAll(l => l.Items).ToArray();
            return c;
        }

        private static int IndexSize(int itemLen)
        {
            int total = itemLen + 1;
            int offSize = total <= 0xFF ? 1 : total <= 0xFFFF ? 2 : total <= 0xFFFFFF ? 3 : 4;
            return 2 + 1 + 2 * offSize + itemLen;
        }

        private static void ReadPrivate(byte[] cff, DictEntry? priv, List<byte[]> privates, List<Index> localSubrs)
        {
            if (priv == null || priv.Ints.Count < 2) { privates.Add(Array.Empty<byte>()); localSubrs.Add(new Index()); return; }
            int size = priv.Ints[0], off = priv.Ints[1];
            if (size < 0 || off < 0 || off + size > cff.Length) throw new InvalidDataException("bad Private DICT");
            var pd = new byte[size];
            Buffer.BlockCopy(cff, off, pd, 0, size);
            privates.Add(pd);
            var subrs = Find(ParseDict(pd), OpSubrs);
            localSubrs.Add(subrs != null && subrs.Ints.Count > 0 ? ReadIndex(cff, off + subrs.Ints[0]) : new Index());
        }

        // The Private DICT with its Subrs offset set (or removed when subrsOffset < 0).
        private static byte[] BuildPrivate(List<DictEntry> pd, bool withSubrs)
        {
            // The Subrs offset is five bytes whatever its value, and the local subrs follow the
            // Private DICT directly, so the offset is the DICT's own length.
            int len = EmitPrivate(pd, withSubrs ? 0 : -1).Length;
            return EmitPrivate(pd, withSubrs ? len : -1);
        }

        private static byte[] EmitPrivate(List<DictEntry> pd, int subrsOffset)
        {
            var o = new List<byte>();
            foreach (var e in pd)
            {
                if (e.Op == OpSubrs) continue;
                o.AddRange(e.RawOperands); AppendOp(o, e.Op);
            }
            if (subrsOffset >= 0) { AppendInt5(o, subrsOffset); AppendOp(o, OpSubrs); }
            return o.ToArray();
        }

        private static byte[] BuildFontDict(List<DictEntry> fd, int privSize, int privOffset)
        {
            var o = new List<byte>();
            bool wrote = false;
            foreach (var e in fd)
            {
                if (e.Op == OpPrivate)
                {
                    AppendInt5(o, privSize); AppendInt5(o, privOffset); AppendOp(o, OpPrivate);
                    wrote = true;
                    continue;
                }
                o.AddRange(e.RawOperands); AppendOp(o, e.Op);
            }
            if (!wrote) { AppendInt5(o, privSize); AppendInt5(o, privOffset); AppendOp(o, OpPrivate); }
            return o.ToArray();
        }

        private static byte[] BuildTop(List<DictEntry> top, bool isCid, int charset, int charStrings,
            int fdArray, int fdSelect, int privSize, int privOffset)
        {
            var o = new List<byte>();
            foreach (var e in top)
            {
                switch (e.Op)
                {
                    case OpCharset: case OpEncoding: case OpCharStrings: case OpPrivate:
                    case OpFdArray: case OpFdSelect:
                        continue;
                }
                o.AddRange(e.RawOperands); AppendOp(o, e.Op);
                // ROS must come first among the entries after it, which the original order already ensures.
            }
            if (charset >= 0) { AppendInt5(o, charset); AppendOp(o, OpCharset); }
            AppendInt5(o, charStrings); AppendOp(o, OpCharStrings);
            if (isCid)
            {
                AppendInt5(o, fdArray); AppendOp(o, OpFdArray);
                AppendInt5(o, fdSelect); AppendOp(o, OpFdSelect);
            }
            else
            {
                AppendInt5(o, privSize); AppendInt5(o, privOffset); AppendOp(o, OpPrivate);
            }
            return o.ToArray();
        }

        // A name-keyed font's charset is copied: its length depends on the format.
        private static byte[] CopyCharset(byte[] cff, int pos, int nGlyphs)
        {
            int format = cff[pos];
            int len;
            if (format == 0) len = 1 + 2 * (nGlyphs - 1);
            else
            {
                int i = pos + 1, covered = 0, step = format == 1 ? 3 : 4;
                while (covered < nGlyphs - 1)
                {
                    int left = format == 1 ? cff[i + 2] : (cff[i + 2] << 8) | cff[i + 3];
                    covered += left + 1;
                    i += step;
                }
                len = i - pos;
            }
            var r = new byte[len];
            Buffer.BlockCopy(cff, pos, r, 0, len);
            return r;
        }

        // The raw FDSelect and the font DICT index for each glyph.
        private static byte[] ReadFdSelect(byte[] cff, int pos, int nGlyphs, out byte[] fdOf)
        {
            fdOf = new byte[nGlyphs];
            int format = cff[pos];
            int len;
            if (format == 0)
            {
                for (int g = 0; g < nGlyphs; g++) fdOf[g] = cff[pos + 1 + g];
                len = 1 + nGlyphs;
            }
            else if (format == 3)
            {
                int nRanges = (cff[pos + 1] << 8) | cff[pos + 2];
                int p = pos + 3;
                for (int r = 0; r < nRanges; r++)
                {
                    int first = (cff[p] << 8) | cff[p + 1];
                    byte fd = cff[p + 2];
                    int next = (cff[p + 3] << 8) | cff[p + 4];
                    if (next > nGlyphs) next = nGlyphs;
                    for (int g = first; g < next; g++) fdOf[g] = fd;
                    p += 3;
                }
                len = 3 + nRanges * 3 + 2;
            }
            else throw new NotSupportedException("FDSelect format " + format);
            var raw = new byte[len];
            Buffer.BlockCopy(cff, pos, raw, 0, len);
            return raw;
        }

        // Unused subroutines become a bare 'return', so the numbering and the bias stay as they were.
        private static byte[][] PruneSubrs(byte[][] subrs, bool[] used)
        {
            var r = new byte[subrs.Length][];
            for (int i = 0; i < subrs.Length; i++)
                r[i] = used[i] ? subrs[i] : new[] { Return };
            return r;
        }

        // ── Type 2 charstring walker ─────────────────────────────────────────

        private sealed class CharstringWalker
        {
            private readonly byte[][] _gsubrs, _lsubrs;
            private readonly bool[] _gUsed, _lUsed;
            private readonly double[] _stack = new double[64];
            private int _sp;
            private int _stems;
            private bool _done;

            internal CharstringWalker(byte[][] gsubrs, bool[] gUsed, byte[][] lsubrs, bool[] lUsed)
            {
                _gsubrs = gsubrs; _gUsed = gUsed; _lsubrs = lsubrs; _lUsed = lUsed;
            }

            internal void Walk(byte[] cs) => Run(cs, 0);

            private static int Bias(int n) => n < 1240 ? 107 : n < 33900 ? 1131 : 32768;

            private void Push(double v) { if (_sp < _stack.Length) _stack[_sp++] = v; }

            // Stem-hint operators take pairs; an odd operand count means the first is the width.
            private void Stems()
            {
                _stems += _sp / 2;
                _sp = 0;
            }

            private void Run(byte[] cs, int depth)
            {
                if (depth > 12) return;
                int i = 0;
                while (i < cs.Length && !_done)
                {
                    int b = cs[i++];
                    if (b >= 32 || b == 28)
                    {
                        if (b == 28) { Push((short)((cs[i] << 8) | cs[i + 1])); i += 2; }
                        else if (b <= 246) Push(b - 139);
                        else if (b <= 250) { Push((b - 247) * 256 + cs[i] + 108); i++; }
                        else if (b <= 254) { Push(-(b - 251) * 256 - cs[i] - 108); i++; }
                        else { Push(((cs[i] << 24) | (cs[i + 1] << 16) | (cs[i + 2] << 8) | cs[i + 3]) / 65536.0); i += 4; }
                        continue;
                    }
                    switch (b)
                    {
                        case 1: case 3: case 18: case 23: // hstem vstem hstemhm vstemhm
                            Stems();
                            break;
                        case 19: case 20: // hintmask cntrmask: implicit vstem operands, then a mask
                            Stems();
                            i += (_stems + 7) / 8;
                            break;
                        case 10: // callsubr
                        case 29: // callgsubr
                        {
                            if (_sp == 0) return;
                            var subrs = b == 10 ? _lsubrs : _gsubrs;
                            var used = b == 10 ? _lUsed : _gUsed;
                            int n = (int)_stack[--_sp] + Bias(subrs.Length);
                            if (n < 0 || n >= subrs.Length) return;
                            used[n] = true;
                            Run(subrs[n], depth + 1);
                            break;
                        }
                        case 11: return; // return
                        case 14: _done = true; return; // endchar
                        case 12:
                            i++; // two-byte operators: all clear the stack
                            _sp = 0;
                            break;
                        default:
                            // moveto/lineto/curveto family and the rest clear the stack
                            _sp = 0;
                            break;
                    }
                }
            }
        }
    }
}
