// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Majorsilence.Pdf
{
    /// <summary>
    /// Merges multiple existing PDF documents into a single PDF by concatenating their pages.
    /// Handles traditional xref tables (PDF 1.4 / PDF 2.0 produced by Majorsilence.Pdf and
    /// iTextSharp / standard tools).  Encrypted source PDFs are not supported.
    /// </summary>
    /// <example>
    /// <code>
    /// var merged = new PdfMerger()
    ///     .Add(File.ReadAllBytes("a.pdf"))
    ///     .Add(File.ReadAllBytes("b.pdf"))
    ///     .WithTitle("Combined report")
    ///     .WithAuthor("Example Corp")
    ///     .Merge();
    /// File.WriteAllBytes("merged.pdf", merged);
    /// </code>
    /// </example>
    public sealed class PdfMerger
    {
        // Latin1 was added in .NET 5; use code-page 28591 for netstandard2.0
        private static readonly Encoding Latin1 = Encoding.GetEncoding(28591);

        private readonly List<byte[]> _sources = new();
        private string? _title, _author, _subject, _creator;

        /// <summary>Add a PDF document (as raw bytes) to the merge queue.</summary>
        public PdfMerger Add(byte[] pdfBytes)
        {
            if (pdfBytes != null && pdfBytes.Length > 0) _sources.Add(pdfBytes);
            return this;
        }

        /// <summary>Add a PDF document from a stream.</summary>
        public PdfMerger Add(Stream stream)
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return Add(ms.ToArray());
        }

        public PdfMerger WithTitle(string title)    { _title   = title;   return this; }
        public PdfMerger WithAuthor(string author)  { _author  = author;  return this; }
        public PdfMerger WithSubject(string subject){ _subject = subject; return this; }
        public PdfMerger WithCreator(string creator){ _creator = creator; return this; }

        /// <summary>Merge all added PDFs and return the result as a byte array.</summary>
        public byte[] Merge()
        {
            using var ms = new MemoryStream();
            Merge(ms);
            return ms.ToArray();
        }

        /// <summary>Merge all added PDFs and write the result to <paramref name="output"/>.</summary>
        public void Merge(Stream output)
        {
            if (_sources.Count == 0)
                throw new InvalidOperationException("No source PDFs added to merge.");

            // ── parse all source documents ────────────────────────────────────
            var docs = new List<SourceDoc>(_sources.Count);
            foreach (var data in _sources)
                docs.Add(SourceDoc.Parse(data));

            // ── object ID allocation ──────────────────────────────────────────
            // Fixed IDs: 0=free, 1=Catalog, 2=Pages, 3=Info
            // Source objects are renumbered starting from 4.
            const int CAT_ID   = 1;
            const int PAGES_ID = 2;
            const int INFO_ID  = 3;

            int nextId = 4;
            var idOffsets = new int[docs.Count]; // idOffsets[i] added to source obj IDs
            for (int i = 0; i < docs.Count; i++)
            {
                idOffsets[i] = nextId - 1; // source obj N → output N + offset
                nextId += docs[i].MaxObjId;
            }

            // ── collect merged page object IDs (output numbering) ─────────────
            var mergedPageIds = new List<int>();
            for (int i = 0; i < docs.Count; i++)
                foreach (int pid in docs[i].PageObjIds)
                    mergedPageIds.Add(pid + idOffsets[i]);

            // ── write PDF ─────────────────────────────────────────────────────
            var buf = new MemoryStream();
            var objPos = new Dictionary<int, long>(); // output obj ID → byte offset

            void WriteB(byte[] b) => buf.Write(b, 0, b.Length);
            void WriteS(string s) => WriteB(Latin1.GetBytes(s));

            WriteS("%PDF-1.4\n%\xe2\xe3\xcf\xd3\n");

            // ── fixed header objects ──────────────────────────────────────────

            void WriteTextObj(int id, string body)
            {
                objPos[id] = buf.Position;
                WriteS($"{id} 0 obj\n{body}\nendobj\n");
            }

            // 1 – Catalog
            WriteTextObj(CAT_ID, $"<< /Type /Catalog /Pages {PAGES_ID} 0 R >>");

            // 2 – Pages
            var kids = new StringBuilder();
            foreach (int pid in mergedPageIds) { kids.Append(pid); kids.Append(" 0 R "); }
            WriteTextObj(PAGES_ID,
                $"<< /Type /Pages /Kids [{kids.ToString().TrimEnd()}] /Count {mergedPageIds.Count} >>");

            // 3 – Info
            var info = new StringBuilder("<<");
            if (_title   != null) info.Append($" /Title ({EscStr(_title)})");
            if (_author  != null) info.Append($" /Author ({EscStr(_author)})");
            if (_subject != null) info.Append($" /Subject ({EscStr(_subject)})");
            if (_creator != null) info.Append($" /Creator ({EscStr(_creator)})");
            info.Append(" >>");
            WriteTextObj(INFO_ID, info.ToString());

            // ── source objects ────────────────────────────────────────────────

            for (int si = 0; si < docs.Count; si++)
            {
                var doc    = docs[si];
                int idOff  = idOffsets[si];

                foreach (var obj in doc.Objects)
                {
                    // Skip: source Catalog, all Pages-tree nodes (we built a new tree)
                    if (obj.Id == doc.CatalogId || doc.PagesNodeIds.Contains(obj.Id))
                        continue;

                    bool isPage = doc.PageObjIds.Contains(obj.Id);
                    int outId   = obj.Id + idOff;
                    objPos[outId] = buf.Position;

                    if (obj.StreamData != null)
                    {
                        // Text (dictionary) part — renumber refs; binary stream — verbatim
                        string dict = FixRefs(obj.DictText, idOff, isPage ? PAGES_ID : -1);
                        WriteS($"{outId} 0 obj\n{dict}\nstream\n");
                        WriteB(obj.StreamData);
                        WriteS("\nendstream\nendobj\n");
                    }
                    else
                    {
                        string text = FixRefs(obj.DictText, idOff, isPage ? PAGES_ID : -1);
                        WriteS($"{outId} 0 obj\n{text}\nendobj\n");
                    }
                }
            }

            // ── xref table ───────────────────────────────────────────────────

            long xrefOff = buf.Position;
            int  total   = nextId;

            var xref = new StringBuilder();
            xref.Append($"xref\n0 {total}\n");
            xref.Append("0000000000 65535 f \n"); // obj 0 always free

            for (int id = 1; id < total; id++)
            {
                if (objPos.TryGetValue(id, out long pos))
                    xref.Append($"{pos:D10} 00000 n \n");
                else
                    xref.Append("0000000000 65535 f \n");
            }

            WriteS(xref.ToString());

            // ── trailer ───────────────────────────────────────────────────────

            WriteS($"trailer\n<< /Size {total} /Root {CAT_ID} 0 R /Info {INFO_ID} 0 R >>\n");
            WriteS($"startxref\n{xrefOff}\n%%EOF\n");

            buf.Position = 0;
            buf.CopyTo(output);
        }

        // Renumber all "N 0 R" references in a text block by adding idOffset,
        // and replace any /Parent reference with the supplied newParentId (when >= 0).
        private static string FixRefs(string text, int idOffset, int newParentId)
        {
            text = Regex.Replace(text, @"(\d+)\s+0\s+R", m =>
                $"{int.Parse(m.Groups[1].Value) + idOffset} 0 R");

            if (newParentId >= 0)
                text = Regex.Replace(text, @"/Parent\s+\d+\s+0\s+R",
                    $"/Parent {newParentId} 0 R");

            return text;
        }

        private static string EscStr(string s)
            => s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

        // ── inner parser ─────────────────────────────────────────────────────

        internal sealed class PdfObj
        {
            public int     Id;
            public string  DictText  = "";   // text before "stream" keyword (or the full value)
            public byte[]? StreamData;       // raw stream bytes (null if no stream)
        }

        internal sealed class SourceDoc
        {
            private static readonly Encoding Latin1 = Encoding.GetEncoding(28591);

            public int        CatalogId;
            public HashSet<int> PagesNodeIds = new(); // /Type /Pages objects — skipped
            public List<int>  PageObjIds    = new(); // /Type /Page objects in order
            public int        MaxObjId;
            public List<PdfObj> Objects     = new();

            // ── entry point ──────────────────────────────────────────────────

            public static SourceDoc Parse(byte[] data)
            {
                var doc = new SourceDoc();

                // 1. find startxref
                int xrefAt = FindStartXref(data);

                // 2. build xref map (handles traditional xref tables)
                var xref = BuildXref(data, xrefAt, out int catalogId, out _);
                doc.CatalogId = catalogId;

                // 3a. if the xref came back empty (xref streams / PDF 1.5+), scan for objects
                if (xref.Count == 0)
                    xref = ScanForObjects(data, out catalogId);
                if (doc.CatalogId == 0) doc.CatalogId = catalogId;

                // 3b. read all objects
                int maxId = 0;
                foreach (var kv in xref)
                {
                    int id  = kv.Key;
                    int off = kv.Value;
                    if (id > maxId) maxId = id;
                    try
                    {
                        var obj = ReadObj(data, id, off);
                        doc.Objects.Add(obj);
                    }
                    catch { /* skip malformed objects */ }
                }
                doc.MaxObjId = maxId;

                // 4. traverse page tree to find Pages nodes and Page leaves
                if (doc.CatalogId > 0)
                {
                    var catalog = doc.FindObj(doc.CatalogId);
                    if (catalog != null)
                    {
                        int pagesId = ExtractRef(catalog.DictText, "/Pages");
                        if (pagesId > 0)
                            WalkPageTree(doc, pagesId);
                    }
                }

                return doc;
            }

            // Fallback for PDF 1.5+ xref-stream files: scan the raw bytes for
            // "N 0 obj" headers and derive /Root from the xref stream's dictionary.
            private static Dictionary<int, int> ScanForObjects(byte[] data, out int catalogId)
            {
                catalogId = 0;
                var result = new Dictionary<int, int>();

                // Scan as Latin1 text; "N 0 obj" headers are always ASCII
                string text = Latin1.GetString(data);

                foreach (Match m in Regex.Matches(text, @"(\d+)\s+0\s+obj"))
                {
                    if (int.TryParse(m.Groups[1].Value, out int id) && !result.ContainsKey(id))
                        result[id] = m.Index;
                }

                // /Root reference lives in the xref stream object's dictionary (plain text before the stream data)
                var rootM = Regex.Match(text, @"/Root\s+(\d+)\s+0\s+R");
                if (rootM.Success) int.TryParse(rootM.Groups[1].Value, out catalogId);

                return result;
            }

            // ── xref builder ─────────────────────────────────────────────────

            private static int FindStartXref(byte[] data)
            {
                int searchStart = Math.Max(0, data.Length - 1024);
                var tail = Latin1.GetString(data, searchStart, data.Length - searchStart);
                int idx = tail.LastIndexOf("startxref", StringComparison.Ordinal);
                if (idx < 0) throw new InvalidOperationException("PDF missing startxref");
                // Skip "startxref" (9 chars) plus any whitespace/newlines to reach the offset number
                int numStart = idx + 9;
                while (numStart < tail.Length && !char.IsDigit(tail[numStart])) numStart++;
                int numEnd = numStart;
                while (numEnd < tail.Length && char.IsDigit(tail[numEnd])) numEnd++;
                return int.Parse(tail.Substring(numStart, numEnd - numStart));
            }

            private static Dictionary<int, int> BuildXref(
                byte[] data, int xrefAt, out int catalogId, out int infoId)
            {
                catalogId = 0; infoId = 0;
                var result = new Dictionary<int, int>();

                // Iteratively follow /Prev chains so we pick up all incremental updates.
                var visited = new HashSet<int>();
                int pos = xrefAt;

                while (pos > 0 && !visited.Contains(pos))
                {
                    visited.Add(pos);

                    // Peek at the keyword at pos
                    int peekLen = Math.Min(8, data.Length - pos);
                    string peek = Latin1.GetString(data, pos, peekLen);

                    if (peek.StartsWith("xref", StringComparison.Ordinal))
                    {
                        ParseTraditionalXref(data, pos, result, out int cat, out int inf, out int prev);
                        if (cat  > 0 && catalogId == 0) catalogId = cat;
                        if (inf  > 0 && infoId    == 0) infoId    = inf;
                        pos = prev;
                    }
                    else
                    {
                        // xref stream (PDF 1.5+) — skip silently; object IDs
                        // already encountered via the traditional xref sections will work.
                        // For a full PDF 1.5+ implementation a stream decoder is needed.
                        break;
                    }
                }

                return result;
            }

            private static void ParseTraditionalXref(
                byte[] data, int startPos,
                Dictionary<int, int> xref,
                out int catalogId, out int infoId, out int prevXref)
            {
                catalogId = 0; infoId = 0; prevXref = 0;

                int maxRead = Math.Min(data.Length - startPos, 524288); // 512 KB
                string raw = Latin1.GetString(data, startPos, maxRead);
                var lines = raw.Split('\n');
                int li = 0;

                // skip "xref" keyword line
                if (li < lines.Length && lines[li].TrimStart().StartsWith("xref"))
                    li++;

                while (li < lines.Length)
                {
                    string line = lines[li].Trim();
                    if (line.StartsWith("trailer", StringComparison.Ordinal)) { li++; break; }
                    if (line.Length == 0) { li++; continue; }

                    // section header: "firstObj count"
                    var parts = line.Split(new[]{' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 2 || !int.TryParse(parts[0], out int first)
                                          || !int.TryParse(parts[1], out int count))
                    { li++; continue; }
                    li++;

                    for (int k = 0; k < count && li < lines.Length; k++, li++)
                    {
                        string entry = lines[li].Trim();
                        if (entry.Length < 18) continue;
                        char flag = entry[17];
                        int  id   = first + k;
                        if (flag == 'n' && int.TryParse(entry.Substring(0, 10).Trim(), out int off))
                        {
                            if (!xref.ContainsKey(id)) // earlier updates win
                                xref[id] = off;
                        }
                    }
                }

                // parse trailer dictionary
                if (li < lines.Length)
                {
                    int trailerStart = startPos;
                    // locate the trailer dict in raw
                    int tIdx = raw.IndexOf("trailer", StringComparison.Ordinal);
                    if (tIdx >= 0)
                    {
                        string trailer = raw.Substring(tIdx);
                        catalogId = ExtractRef(trailer, "/Root");
                        infoId    = ExtractRef(trailer, "/Info");
                        // /Prev <number>
                        var pm = Regex.Match(trailer, @"/Prev\s+(\d+)");
                        if (pm.Success) prevXref = int.Parse(pm.Groups[1].Value);
                    }
                }
            }

            // ── object reader ─────────────────────────────────────────────────

            private static PdfObj ReadObj(byte[] data, int id, int offset)
            {
                // Find "N G obj" header
                int maxRead = Math.Min(data.Length - offset, 131072); // 128 KB for dict
                string header = Latin1.GetString(data, offset, Math.Min(maxRead, 64));
                // skip to after "obj\n" or "obj\r\n"
                int objKw = header.IndexOf(" obj", StringComparison.Ordinal);
                if (objKw < 0) return new PdfObj { Id = id, DictText = "" };
                int bodyStart = offset + objKw + 4; // skip " obj"
                // skip whitespace
                while (bodyStart < data.Length && (data[bodyStart] == '\r' || data[bodyStart] == '\n'))
                    bodyStart++;

                // Read up to endobj (128 KB window)
                int readLen = Math.Min(data.Length - bodyStart, 131072);
                string body = Latin1.GetString(data, bodyStart, readLen);

                // Does this object have a stream?
                int streamKw = FindStreamKeyword(body);
                if (streamKw >= 0)
                {
                    string dictText = body.Substring(0, streamKw);

                    // Determine stream length
                    int length = ExtractLength(dictText, data, offset);
                    int streamDataStart = bodyStart + streamKw;

                    // skip "stream" keyword + CR/LF
                    int sk = body.IndexOf("stream", streamKw, StringComparison.Ordinal);
                    streamDataStart = bodyStart + sk + 6; // after "stream"
                    if (streamDataStart < data.Length && data[streamDataStart] == '\r') streamDataStart++;
                    if (streamDataStart < data.Length && data[streamDataStart] == '\n') streamDataStart++;

                    byte[] streamBytes;
                    if (length > 0 && streamDataStart + length <= data.Length)
                    {
                        streamBytes = new byte[length];
                        Buffer.BlockCopy(data, streamDataStart, streamBytes, 0, length);
                    }
                    else
                    {
                        // Fallback: scan for "endstream"
                        streamBytes = ExtractStreamByScanning(data, streamDataStart);
                    }

                    return new PdfObj { Id = id, DictText = dictText, StreamData = streamBytes };
                }

                // Non-stream object
                int endobj = body.IndexOf("endobj", StringComparison.Ordinal);
                string fullText = endobj >= 0 ? body.Substring(0, endobj) : body;
                return new PdfObj { Id = id, DictText = fullText.TrimEnd() };
            }

            private static int FindStreamKeyword(string body)
            {
                // Look for "\nstream\n" or "\nstream\r\n"
                int idx = body.IndexOf("\nstream", StringComparison.Ordinal);
                return idx; // -1 if not found
            }

            private static int ExtractLength(string dictText, byte[] data, int objOffset)
            {
                // Direct: /Length 1234
                var m = Regex.Match(dictText, @"/Length\s+(\d+)");
                if (m.Success) return int.Parse(m.Groups[1].Value);

                // Indirect: /Length N 0 R — we can't resolve without full xref; return -1 to trigger scan
                return -1;
            }

            private static byte[] ExtractStreamByScanning(byte[] data, int start)
            {
                // Find b"endstream" from start
                byte[] needle = Latin1.GetBytes("endstream");
                int limit = Math.Min(data.Length - needle.Length, start + 10 * 1024 * 1024);
                for (int i = start; i < limit; i++)
                {
                    bool match = true;
                    for (int j = 0; j < needle.Length; j++)
                        if (data[i + j] != needle[j]) { match = false; break; }
                    if (match)
                    {
                        // trim trailing CR/LF before "endstream"
                        int end = i;
                        while (end > start && (data[end - 1] == '\n' || data[end - 1] == '\r'))
                            end--;
                        var result = new byte[end - start];
                        Buffer.BlockCopy(data, start, result, 0, result.Length);
                        return result;
                    }
                }
                return Array.Empty<byte>();
            }

            // ── page tree walker ──────────────────────────────────────────────

            private static void WalkPageTree(SourceDoc doc, int nodeId)
            {
                var obj = doc.FindObj(nodeId);
                if (obj == null) return;

                string text = obj.DictText;

                // Determine type
                if (text.Contains("/Type /Pages") || text.Contains("/Type/Pages"))
                {
                    doc.PagesNodeIds.Add(nodeId);
                    // Extract /Kids array
                    var kidsMatch = Regex.Match(text, @"/Kids\s*\[([^\]]*)\]");
                    if (!kidsMatch.Success) return;
                    string kidsText = kidsMatch.Groups[1].Value;
                    foreach (Match rm in Regex.Matches(kidsText, @"(\d+)\s+0\s+R"))
                    {
                        int childId = int.Parse(rm.Groups[1].Value);
                        WalkPageTree(doc, childId);
                    }
                }
                else if (text.Contains("/Type /Page") || text.Contains("/Type/Page"))
                {
                    doc.PageObjIds.Add(nodeId);
                }
            }

            // ── helpers ───────────────────────────────────────────────────────

            internal PdfObj? FindObj(int id)
            {
                foreach (var o in Objects)
                    if (o.Id == id) return o;
                return null;
            }

            private static int ExtractRef(string text, string key)
            {
                var m = Regex.Match(text, Regex.Escape(key) + @"\s+(\d+)\s+0\s+R");
                return m.Success ? int.Parse(m.Groups[1].Value) : 0;
            }
        }
    }
}
