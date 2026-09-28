// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Majorsilence.Pdf
{
    /// <summary>
    /// Finds the fonts installed in a folder by the family name each one declares, the way the
    /// operating system does, rather than by what the file happens to be called.
    ///
    /// A family is its legacy name (the <c>name</c> table's nameID 1, which is what GDI matches
    /// a face name against), and a file's place in it is its OS/2 fsSelection bold and italic
    /// bits. Only the table directory, <c>name</c>, <c>OS/2</c> and <c>head</c> are read, so a
    /// folder of several hundred fonts is scanned quickly; the result is kept per folder for
    /// the life of the process.
    ///
    /// Only files the embedder can use are returned: TrueType outlines in a single-font file.
    /// A collection (<c>.ttc</c>) and a CFF-outline <c>.otf</c> are skipped.
    /// </summary>
    internal static class InstalledFontScanner
    {
        internal readonly struct Face
        {
            internal Face(string family, bool bold, bool italic, string path)
            {
                Family = family;
                Bold = bold;
                Italic = italic;
                Path = path;
            }

            internal string Family { get; }
            internal bool Bold { get; }
            internal bool Italic { get; }
            internal string Path { get; }
        }

        private static readonly ConcurrentDictionary<string, IReadOnlyList<Face>> Cache =
            new ConcurrentDictionary<string, IReadOnlyList<Face>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Every usable face in <paramref name="folder"/>, in file-name order.</summary>
        internal static IReadOnlyList<Face> Scan(string folder) =>
            Cache.GetOrAdd(Path.GetFullPath(folder), ScanUncached);

        private static IReadOnlyList<Face> ScanUncached(string folder)
        {
            var faces = new List<Face>();
            if (!Directory.Exists(folder)) return faces;

            var files = new List<string>();
            files.AddRange(Directory.GetFiles(folder, "*.ttf"));
            files.AddRange(Directory.GetFiles(folder, "*.otf"));
            files.Sort(StringComparer.OrdinalIgnoreCase);

            foreach (var file in files)
            {
                try
                {
                    if (TryRead(file, out var face)) faces.Add(face);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            return faces;
        }

        /// <summary>Read one file's family and style, or false if it is not a usable font.</summary>
        internal static bool TryRead(string path, out Face face)
        {
            face = default;
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var head = new byte[12];
            if (fs.Read(head, 0, 12) != 12) return false;

            uint tag = ReadU32(head, 0);
            // 0x00010000 is TrueType; 'true' is the old Apple spelling. 'OTTO' is CFF outlines
            // and 'ttcf' a collection, neither of which the embedder handles.
            if (tag != 0x00010000 && tag != 0x74727565) return false;

            int numTables = ReadU16(head, 4);
            var dir = new byte[numTables * 16];
            if (fs.Read(dir, 0, dir.Length) != dir.Length) return false;

            uint nameOff = 0, nameLen = 0, os2Off = 0, os2Len = 0, headOff = 0;
            bool hasGlyf = false;
            for (int i = 0; i < numTables; i++)
            {
                string t = Encoding.ASCII.GetString(dir, i * 16, 4);
                uint off = ReadU32(dir, i * 16 + 8), len = ReadU32(dir, i * 16 + 12);
                switch (t)
                {
                    case "name": nameOff = off; nameLen = len; break;
                    case "OS/2": os2Off = off; os2Len = len; break;
                    case "head": headOff = off; break;
                    case "glyf": hasGlyf = true; break;
                }
            }
            if (!hasGlyf || nameOff == 0 || nameLen < 6) return false;

            string? family = ReadFamilyName(fs, nameOff, nameLen);
            if (string.IsNullOrWhiteSpace(family)) return false;

            bool bold, italic;
            if (os2Off != 0 && os2Len >= 64)
            {
                var sel = ReadAt(fs, os2Off + 62, 2);
                int fsSelection = ReadU16(sel, 0);
                italic = (fsSelection & 0x01) != 0;
                bold = (fsSelection & 0x20) != 0;
            }
            else if (headOff != 0)
            {
                var mac = ReadAt(fs, headOff + 44, 2);
                int macStyle = ReadU16(mac, 0);
                bold = (macStyle & 0x01) != 0;
                italic = (macStyle & 0x02) != 0;
            }
            else
            {
                bold = italic = false;
            }

            face = new Face(family!.Trim(), bold, italic, path);
            return true;
        }

        /// <summary>
        /// nameID 1, the legacy family. The Windows English record is preferred, then any
        /// Windows Unicode record, then Mac Roman.
        /// </summary>
        private static string? ReadFamilyName(FileStream fs, uint offset, uint length)
        {
            var name = ReadAt(fs, offset, (int)Math.Min(length, 1 << 20));
            if (name.Length < 6) return null;
            int count = ReadU16(name, 2), storage = ReadU16(name, 4);

            string? winEnglish = null, winAny = null, mac = null;
            for (int i = 0; i < count; i++)
            {
                int r = 6 + i * 12;
                if (r + 12 > name.Length) break;
                int platform = ReadU16(name, r), encoding = ReadU16(name, r + 2);
                int language = ReadU16(name, r + 4), nameId = ReadU16(name, r + 6);
                int len = ReadU16(name, r + 8), off = ReadU16(name, r + 10);
                if (nameId != 1) continue;
                int start = storage + off;
                if (start < 0 || start + len > name.Length) continue;

                if (platform == 3 && (encoding == 1 || encoding == 10))
                {
                    string s = Encoding.BigEndianUnicode.GetString(name, start, len);
                    if (language == 0x0409) winEnglish ??= s;
                    else winAny ??= s;
                }
                else if (platform == 1 && encoding == 0)
                {
                    mac ??= Encoding.ASCII.GetString(name, start, len);
                }
            }
            return winEnglish ?? winAny ?? mac;
        }

        private static byte[] ReadAt(FileStream fs, long offset, int count)
        {
            var buf = new byte[count];
            fs.Seek(offset, SeekOrigin.Begin);
            int read = 0;
            while (read < count)
            {
                int n = fs.Read(buf, read, count - read);
                if (n <= 0) break;
                read += n;
            }
            if (read < count) Array.Resize(ref buf, read);
            return buf;
        }

        private static int ReadU16(byte[] b, int i) => (b[i] << 8) | b[i + 1];

        private static uint ReadU32(byte[] b, int i) =>
            ((uint)b[i] << 24) | ((uint)b[i + 1] << 16) | ((uint)b[i + 2] << 8) | b[i + 3];
    }
}
