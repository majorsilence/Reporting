// Copyright (C) 2025 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.IO.Compression;

namespace Majorsilence.Pdf.Internal
{
    // ── small data containers ─────────────────────────────────────────────────

    internal sealed class FontResource
    {
        internal readonly string PdfName;          // e.g. "F1"
        internal readonly string? StandardName;    // set for Type-1 standard fonts
        internal readonly TrueTypeFont? Ttf;       // set for embedded TTF fonts
        internal readonly HashSet<ushort>? UsedGlyphs; // glyph IDs drawn so far (TTF only)
        internal FontResource(string pdfName, string standardName)
        {
            PdfName = pdfName; StandardName = standardName;
        }
        internal FontResource(string pdfName, TrueTypeFont ttf)
        {
            PdfName = pdfName; Ttf = ttf;
            UsedGlyphs = new HashSet<ushort>();
        }
        internal bool IsStandard => StandardName != null;
    }

    internal sealed class ImageResource
    {
        internal readonly string PdfName;          // e.g. "Im1"
        internal readonly byte[] Data;             // raw bytes (JPEG) or compressed RGB
        internal readonly int Width, Height;
        internal readonly bool IsJpeg;
        internal ImageResource(string pdfName, byte[] data, int w, int h, bool isJpeg)
        {
            PdfName = pdfName; Data = data; Width = w; Height = h; IsJpeg = isJpeg;
        }
    }

    internal sealed class PageAnnotation
    {
        internal readonly float X1, Y1, X2, Y2;   // PDF coordinate space
        internal readonly string? Uri;
        internal readonly string? Tooltip;
        internal PageAnnotation(float x1, float y1, float x2, float y2, string? uri, string? tooltip)
        {
            X1 = x1; Y1 = y1; X2 = x2; Y2 = y2; Uri = uri; Tooltip = tooltip;
        }
    }

    internal sealed class PageData
    {
        internal readonly float Width, Height;
        internal readonly StringBuilder Content = new StringBuilder();
        internal readonly List<PageAnnotation> Annotations = new List<PageAnnotation>();
        internal PageData(float width, float height) { Width = width; Height = height; }
    }

    // ── serializer ────────────────────────────────────────────────────────────

    internal sealed class PdfSerializer
    {
        private static readonly Encoding Latin1 = Encoding.GetEncoding("iso-8859-1");

        private readonly List<PageData> _pages = new List<PageData>();
        private readonly List<FontResource> _fonts = new List<FontResource>();
        private readonly Dictionary<string, FontResource> _stdFontMap =
            new Dictionary<string, FontResource>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FontResource> _ttfFontMap =
            new Dictionary<string, FontResource>(StringComparer.OrdinalIgnoreCase);
        private readonly List<ImageResource> _images = new List<ImageResource>();
        private readonly Dictionary<string, ImageResource> _imageMap =
            new Dictionary<string, ImageResource>();

        private string _author  = "";
        private string _title   = "";
        private string _subject = "";
        private string _creator = "Majorsilence.Pdf";

        // ── metadata ─────────────────────────────────────────────────────────

        internal void SetMetadata(string author, string title, string subject, string creator)
        {
            _author = author; _title = title; _subject = subject; _creator = creator;
        }

        // ── page management ──────────────────────────────────────────────────

        internal PageData CreatePage(float width, float height)
        {
            var p = new PageData(width, height);
            _pages.Add(p);
            return p;
        }

        // ── font management ──────────────────────────────────────────────────

        internal FontResource GetOrAddStandardFont(string standardName)
        {
            if (_stdFontMap.TryGetValue(standardName, out var fr)) return fr;
            string pdfName = "F" + (_fonts.Count + 1);
            fr = new FontResource(pdfName, standardName);
            _fonts.Add(fr);
            _stdFontMap[standardName] = fr;
            return fr;
        }

        internal FontResource GetOrAddTtfFont(string filePath, TrueTypeFont ttf)
        {
            if (_ttfFontMap.TryGetValue(filePath, out var fr)) return fr;
            string pdfName = "F" + (_fonts.Count + 1);
            fr = new FontResource(pdfName, ttf);
            _fonts.Add(fr);
            _ttfFontMap[filePath] = fr;
            return fr;
        }

        // ── image management ─────────────────────────────────────────────────

        internal ImageResource GetOrAddImage(byte[] rawOrJpeg, int width, int height, bool isJpeg)
        {
            string key = width + "x" + height + ":" + rawOrJpeg.Length;
            if (_imageMap.TryGetValue(key, out var ir)) return ir;
            string pdfName = "Im" + (_images.Count + 1);
            ir = new ImageResource(pdfName, rawOrJpeg, width, height, isJpeg);
            _images.Add(ir);
            _imageMap[key] = ir;
            return ir;
        }

        // ── serialization ────────────────────────────────────────────────────

        internal void Write(Stream stream)
        {
            using (var ms = new MemoryStream())
            {
                WriteToMemory(ms);
                ms.Position = 0;
                ms.CopyTo(stream);
            }
        }

        private void WriteToMemory(MemoryStream ms)
        {
            var w = new PdfWriter(ms);

            // Header
            w.WriteLine("%PDF-1.4");
            w.WriteRaw(new byte[] { (byte)'%', 0xE2, 0xE3, 0xCF, 0xD3, (byte)'\n' });

            // Object numbering (1-based):
            //   1 = Catalog        (objects[0])
            //   2 = Pages          (objects[1])
            //   3+ = fonts, images, page streams, page dicts, annotations, info

            // Reserve slots 0 and 1 for Catalog and Pages — filled in later.
            var objects = new List<byte[]> { null!, null! };

            // ── font objects ──────────────────────────────────────────────────
            var fontObjStart = new int[_fonts.Count];
            for (int fi = 0; fi < _fonts.Count; fi++)
            {
                fontObjStart[fi] = objects.Count;
                if (_fonts[fi].IsStandard)
                    objects.Add(BuildStandardFontObj(_fonts[fi]));
                else
                    BuildTtfFontObjs(_fonts[fi], objects);
            }

            // ── image objects ─────────────────────────────────────────────────
            var imageObjIdx = new int[_images.Count];
            for (int ii = 0; ii < _images.Count; ii++)
            {
                imageObjIdx[ii] = objects.Count;
                objects.Add(BuildImageObj(_images[ii]));
            }

            // Font and image resource strings (object numbers are list-index + 1)
            var fontResEntries = new StringBuilder();
            for (int fi = 0; fi < _fonts.Count; fi++)
            {
                // For TTF the "Font" (Type0) is sub-object at index 4 within its group
                int objIdx = _fonts[fi].IsStandard ? fontObjStart[fi] : fontObjStart[fi] + 4;
                fontResEntries.Append($"/{_fonts[fi].PdfName} {objIdx + 1} 0 R ");
            }

            var imageResEntries = new StringBuilder();
            for (int ii = 0; ii < _images.Count; ii++)
                imageResEntries.Append($"/{_images[ii].PdfName} {imageObjIdx[ii] + 1} 0 R ");

            // ── page content streams + page dicts ─────────────────────────────
            var pageContentIdx = new int[_pages.Count];
            var pageDictIdx    = new int[_pages.Count];

            for (int pi = 0; pi < _pages.Count; pi++)
            {
                var page = _pages[pi];

                // content stream
                pageContentIdx[pi] = objects.Count;
                byte[] streamBytes = Latin1.GetBytes(page.Content.ToString());
                byte[] compressed  = Compress(streamBytes);
                string hdrStr = $"<< /Filter /FlateDecode /Length {compressed.Length} >>\nstream\n";
                byte[] hdr    = Latin1.GetBytes(hdrStr);
                byte[] tail   = Latin1.GetBytes("\nendstream");
                var contentObj = new byte[hdr.Length + compressed.Length + tail.Length];
                Buffer.BlockCopy(hdr,        0, contentObj, 0,                              hdr.Length);
                Buffer.BlockCopy(compressed, 0, contentObj, hdr.Length,                     compressed.Length);
                Buffer.BlockCopy(tail,       0, contentObj, hdr.Length + compressed.Length, tail.Length);
                objects.Add(contentObj);

                // page dict — object numbers use list-index + 1 throughout
                pageDictIdx[pi] = objects.Count;
                int contentObjNum = pageContentIdx[pi] + 1;
                string pageDict =
                    $"<< /Type /Page /Parent 2 0 R\n" +
                    $"   /MediaBox [0 0 {Fmt(page.Width)} {Fmt(page.Height)}]\n" +
                    $"   /Contents {contentObjNum} 0 R\n" +
                    $"   /Resources << /Font << {fontResEntries} >> /XObject << {imageResEntries} >> >>\n";

                if (page.Annotations.Count > 0)
                {
                    // Annotation objects start right after this page dict
                    int firstAnnotObjNum = objects.Count + 2; // +1 for page dict slot, +1 for 1-based
                    var annots = new StringBuilder("/Annots [ ");
                    for (int ai = 0; ai < page.Annotations.Count; ai++)
                        annots.Append($"{firstAnnotObjNum + ai} 0 R ");
                    annots.Append("]\n");
                    pageDict += annots.ToString();
                }

                pageDict += ">>";
                objects.Add(Latin1.GetBytes(pageDict));

                foreach (var ann in page.Annotations)
                    objects.Add(BuildAnnotationObj(ann));
            }

            // ── info dict ─────────────────────────────────────────────────────
            int infoObjIdx = objects.Count;
            objects.Add(Latin1.GetBytes(
                $"<< /Producer (Majorsilence.Pdf) /Author {PdfTextString(_author)} " +
                $"/Title {PdfTextString(_title)} /Subject {PdfTextString(_subject)} " +
                $"/Creator {PdfTextString(_creator)} >>"));

            // ── catalog (slot 0 = object 1) ───────────────────────────────────
            objects[0] = Latin1.GetBytes("<< /Type /Catalog /Pages 2 0 R >>");

            // ── pages (slot 1 = object 2) ─────────────────────────────────────
            var kids = new StringBuilder("/Kids [ ");
            for (int pi = 0; pi < _pages.Count; pi++)
                kids.Append($"{pageDictIdx[pi] + 1} 0 R ");
            kids.Append("]");
            objects[1] = Latin1.GetBytes($"<< /Type /Pages {kids} /Count {_pages.Count} >>");

            // ── write objects ─────────────────────────────────────────────────
            int totalObjs = objects.Count;
            var xrefOffsets = new long[totalObjs];
            for (int i = 0; i < totalObjs; i++)
            {
                xrefOffsets[i] = ms.Position;
                w.WriteLine($"{i + 1} 0 obj");
                w.WriteBytes(objects[i]);
                w.WriteLine("");
                w.WriteLine("endobj");
            }

            // ── xref + trailer ────────────────────────────────────────────────
            long xrefPos = ms.Position;
            w.WriteLine("xref");
            w.WriteLine($"0 {totalObjs + 1}");
            w.WriteLine("0000000000 65535 f ");
            foreach (long off in xrefOffsets)
                w.WriteLine(off.ToString("D10") + " 00000 n ");

            w.WriteLine("trailer");
            w.WriteLine($"<< /Size {totalObjs + 1} /Root 1 0 R /Info {infoObjIdx + 1} 0 R >>");
            w.WriteLine("startxref");
            w.WriteLine(xrefPos.ToString());
            w.Write("%%EOF");
        }

        // ── object builders ──────────────────────────────────────────────────

        private static byte[] BuildStandardFontObj(FontResource font)
        {
            string dict =
                $"<< /Type /Font /Subtype /Type1 /BaseFont /{font.StandardName} " +
                $"/Encoding /WinAnsiEncoding >>";
            return Latin1.GetBytes(dict);
        }

        private static void BuildTtfFontObjs(FontResource font, List<byte[]> objects)
        {
            var ttf = font.Ttf!;
            string psName = ttf.PostScriptName;
            int totalObjs = objects.Count; // base for computing future object numbers

            // Sub-object layout (all 0-based from totalObjs):
            //   totalObjs+0 = ToUnicode CMap stream
            //   totalObjs+1 = FontDescriptor
            //   totalObjs+2 = FontFile2 stream
            //   totalObjs+3 = CIDFont (Type2)
            //   totalObjs+4 = Font (Type0) ← what fontResEntries references

            int toUnicodeObjNum  = totalObjs + 1;  // 1-based
            int descriptorObjNum = totalObjs + 2;
            int fontFileObjNum   = totalObjs + 3;
            int cidFontObjNum    = totalObjs + 4;
            int type0ObjNum      = totalObjs + 5;

            // Glyph IDs actually used on the page (may be empty if subsetting skipped)
            var used = font.UsedGlyphs;

            // ── ToUnicode CMap (filtered to used glyphs only) ─────────────────
            var allMappings = ttf.GetCharGlyphMappings();
            var mapList = new List<(int codePoint, ushort glyphId)>();
            foreach (var m in allMappings)
                if (used == null || used.Contains(m.glyphId))
                    mapList.Add(m);

            var cmap = new StringBuilder();
            cmap.Append("/CIDInit /ProcSet findresource begin\n");
            cmap.Append("12 dict begin\n");
            cmap.Append("begincmap\n");
            cmap.Append("/CIDSystemInfo << /Registry (Adobe) /Ordering (UCS) /Supplement 0 >> def\n");
            cmap.Append("/CMapName /Adobe-Identity-UCS def\n");
            cmap.Append("/CMapType 2 def\n");
            cmap.Append("1 begincodespacerange\n");
            cmap.Append("<0000> <FFFF>\n");
            cmap.Append("endcodespacerange\n");

            for (int i = 0; i < mapList.Count; i += 100)
            {
                int chunk = Math.Min(100, mapList.Count - i);
                cmap.Append($"{chunk} beginbfchar\n");
                for (int j = i; j < i + chunk; j++)
                    cmap.Append($"<{mapList[j].glyphId:X4}> <{mapList[j].codePoint:X4}>\n");
                cmap.Append("endbfchar\n");
            }
            cmap.Append("endcmap\n");
            cmap.Append("CMapName currentdict /CMap defineresource pop\n");
            cmap.Append("end\nend");

            byte[] cmapBytes = Latin1.GetBytes(cmap.ToString());
            byte[] cmapCompressed = Compress(cmapBytes);
            string cmapStream =
                $"<< /Filter /FlateDecode /Length {cmapCompressed.Length} >>\nstream\n";
            var cmapObj = Concat(Latin1.GetBytes(cmapStream), cmapCompressed, Latin1.GetBytes("\nendstream"));
            objects.Add(cmapObj);

            // ── /W array (widths for used glyphs only, in 1000ths of em) ──────
            var glyphWidths = new SortedDictionary<ushort, int>();
            foreach (var m in mapList)
            {
                ushort gid = m.glyphId;
                int w1000 = (int)Math.Round(ttf.GetAdvanceWidth(gid) * 1000.0 / ttf.UnitsPerEm);
                glyphWidths[gid] = w1000;
            }
            var wArray = new StringBuilder("/W [");
            foreach (var kv in glyphWidths)
                wArray.Append($" {kv.Key} [{kv.Value}]");
            wArray.Append(" ]");

            // ── font bounding box in 1000ths ──────────────────────────────────
            float scale = 1000f / ttf.UnitsPerEm;
            int llx = (int)Math.Round(ttf.XMin * scale);
            int lly = (int)Math.Round(ttf.YMin * scale);
            int urx = (int)Math.Round(ttf.XMax * scale);
            int ury = (int)Math.Round(ttf.YMax * scale);
            int ascender  = (int)Math.Round(ttf.Ascender  * scale);
            int descender = (int)Math.Round(ttf.Descender * scale);

            // ── FontDescriptor ────────────────────────────────────────────────
            string descriptor =
                $"<< /Type /FontDescriptor /FontName /{psName} /Flags 32 " +
                $"/FontBBox [{llx} {lly} {urx} {ury}] " +
                $"/ItalicAngle 0 /Ascent {ascender} /Descent {descender} " +
                $"/CapHeight {ascender} /StemV 80 /FontFile2 {fontFileObjNum} 0 R >>";
            objects.Add(Latin1.GetBytes(descriptor));

            // ── FontFile2 stream (subset font binary) ─────────────────────────
            byte[] fontData = (used != null && used.Count > 0) ? ttf.Subset(used) : ttf.Data;
            byte[] fontCompressed = Compress(fontData);
            string ffHeader =
                $"<< /Filter /FlateDecode /Length {fontCompressed.Length} /Length1 {fontData.Length} >>\nstream\n";
            var ffObj = Concat(Latin1.GetBytes(ffHeader), fontCompressed, Latin1.GetBytes("\nendstream"));
            objects.Add(ffObj);

            // ── CIDFont ───────────────────────────────────────────────────────
            string cidFont =
                $"<< /Type /Font /Subtype /CIDFontType2 /BaseFont /{psName} " +
                $"/CIDSystemInfo << /Registry (Adobe) /Ordering (Identity) /Supplement 0 >> " +
                $"/FontDescriptor {descriptorObjNum} 0 R " +
                $"/DW 1000 {wArray} >>";
            objects.Add(Latin1.GetBytes(cidFont));

            // ── Type0 Font ────────────────────────────────────────────────────
            string type0 =
                $"<< /Type /Font /Subtype /Type0 /BaseFont /{psName} " +
                $"/Encoding /Identity-H /DescendantFonts [{cidFontObjNum} 0 R] " +
                $"/ToUnicode {toUnicodeObjNum} 0 R >>";
            objects.Add(Latin1.GetBytes(type0));
        }

        private static byte[] BuildImageObj(ImageResource img)
        {
            string filter = img.IsJpeg ? "/DCTDecode" : "/FlateDecode";
            byte[] data   = img.IsJpeg ? img.Data : Compress(img.Data);
            string hdr =
                $"<< /Type /XObject /Subtype /Image /Width {img.Width} /Height {img.Height} " +
                $"/ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter {filter} /Length {data.Length} >>\nstream\n";
            return Concat(Latin1.GetBytes(hdr), data, Latin1.GetBytes("\nendstream"));
        }

        private static byte[] BuildAnnotationObj(PageAnnotation ann)
        {
            if (ann.Uri != null)
            {
                string dict =
                    $"<< /Type /Annot /Subtype /Link " +
                    $"/Rect [{Fmt(ann.X1)} {Fmt(ann.Y1)} {Fmt(ann.X2)} {Fmt(ann.Y2)}] " +
                    $"/Border [0 0 0] /A << /S /URI /URI ({ann.Uri}) >> >>";
                return Latin1.GetBytes(dict);
            }
            else
            {
                string dict =
                    $"<< /Type /Annot /Subtype /Text " +
                    $"/Rect [{Fmt(ann.X1)} {Fmt(ann.Y1)} {Fmt(ann.X2)} {Fmt(ann.Y2)}] " +
                    $"/Contents {PdfTextString(ann.Tooltip ?? "")} >>";
                return Latin1.GetBytes(dict);
            }
        }

        // ── helpers ──────────────────────────────────────────────────────────

        internal static string Fmt(float v) =>
            v.ToString("0.###", CultureInfo.InvariantCulture);

        // Encode a PDF text string as UTF-16BE hex with BOM (<FEFF...>).
        // Supported by all PDF 1.x and 2.x readers; handles the full Unicode range.
        internal static string PdfTextString(string s)
        {
            if (string.IsNullOrEmpty(s)) return "<FEFF>";
            var sb = new StringBuilder("<FEFF");
            // .NET strings are UTF-16; iterate chars directly so surrogate pairs are preserved.
            foreach (char c in s)
            {
                sb.Append(((int)c).ToString("X4"));
            }
            sb.Append('>');
            return sb.ToString();
        }

        internal static string PdfString(string s)
        {
            // Simple Latin-1 escaping; for ASCII content this is fine
            return s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        }

        internal static string EncodeGlyphIds(string text, TrueTypeFont ttf,
            HashSet<ushort>? usedGlyphs = null)
        {
            var sb = new StringBuilder("<");
            for (int i = 0; i < text.Length; i++)
            {
                int codePoint;
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    codePoint = char.ConvertToUtf32(text[i], text[i + 1]);
                    i++;
                }
                else
                {
                    codePoint = text[i];
                }
                ushort gid = ttf.GetGlyphId(codePoint);
                usedGlyphs?.Add(gid);
                sb.Append(gid.ToString("X4"));
            }
            sb.Append(">");
            return sb.ToString();
        }

        private static byte[] Compress(byte[] data)
        {
#if NET6_0_OR_GREATER
            // ZLibStream handles the zlib header + Adler-32 footer automatically
            // and SmallestSize maps to zlib level 9 for best compression.
            using (var ms = new MemoryStream())
            {
                using (var zs = new ZLibStream(ms, CompressionLevel.SmallestSize, leaveOpen: true))
                    zs.Write(data, 0, data.Length);
                return ms.ToArray();
            }
#else
            // netstandard2.0: manually wrap raw deflate output in the zlib envelope (RFC 1950).
            // 0x78 0x9C = CM=8 (deflate) + CINFO=7 (32K window) + FCHECK so header % 31 == 0.
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0x78);
                ms.WriteByte(0x9C);
                using (var ds = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true))
                    ds.Write(data, 0, data.Length);
                uint adler = Adler32(data);
                ms.WriteByte((byte)(adler >> 24));
                ms.WriteByte((byte)(adler >> 16));
                ms.WriteByte((byte)(adler >>  8));
                ms.WriteByte((byte) adler);
                return ms.ToArray();
            }
#endif
        }

#if !NET6_0_OR_GREATER
        private static uint Adler32(byte[] data)
        {
            const uint MOD = 65521;
            uint s1 = 1, s2 = 0;
            foreach (byte b in data) { s1 = (s1 + b) % MOD; s2 = (s2 + s1) % MOD; }
            return (s2 << 16) | s1;
        }
#endif

        private static byte[] Concat(byte[] a, byte[] b, byte[] c)
        {
            var result = new byte[a.Length + b.Length + c.Length];
            Buffer.BlockCopy(a, 0, result, 0, a.Length);
            Buffer.BlockCopy(b, 0, result, a.Length, b.Length);
            Buffer.BlockCopy(c, 0, result, a.Length + b.Length, c.Length);
            return result;
        }

        // ── inner writer ─────────────────────────────────────────────────────

        private sealed class PdfWriter
        {
            private readonly Stream _s;
            private static readonly Encoding Latin1 = Encoding.GetEncoding("iso-8859-1");

            internal PdfWriter(Stream s) { _s = s; }

            internal void Write(string s)   { var b = Latin1.GetBytes(s); _s.Write(b, 0, b.Length); }
            internal void WriteLine(string s) => Write(s + "\n");
            internal void WriteLine(long v)   => WriteLine(v.ToString());
            internal void WriteBytes(byte[] b) => _s.Write(b, 0, b.Length);
            internal void WriteRaw(byte[] b)   => _s.Write(b, 0, b.Length);
        }
    }
}
