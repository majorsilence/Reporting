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
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace Majorsilence.Pdf
{
    // ── per-page annotation ──────────────────────────────────────────────────

    public sealed class PdfAnnotation
    {
        public float X1, Y1, X2, Y2; // in PDF coordinates (origin bottom-left)
        public string? Uri;           // non-null ⇒ link annotation
        public string? Tooltip;       // non-null ⇒ text annotation (used when Uri is null)
    }

    // ── registered font resource ─────────────────────────────────────────────

    public sealed class PdfFontResource
    {
        public int    ObjNum;        // first/only PDF object number for this font
        public string PdfName = "";  // resource dictionary name, e.g. "F1"
        public string BaseFontName = ""; // /BaseFont value (PostScript name)
        public TrueTypeFont? Ttf;   // null for standard Type 1 fonts
        public bool   IsStandard14;
    }

    // ── registered image resource ────────────────────────────────────────────

    public sealed class PdfImageResource
    {
        public int    ObjNum;
        public string PdfName = ""; // resource dictionary name, e.g. "Im1"
        public int    SamplesW, SamplesH;
        public bool   IsJpeg;
        public byte[] Data = Array.Empty<byte>(); // raw JPEG bytes or raw RGB (R G B per pixel)
    }

    // ── per-page data ────────────────────────────────────────────────────────

    public sealed class PdfPage
    {
        public float Width, Height;
        public readonly StringBuilder Content   = new StringBuilder(4096);
        public readonly List<PdfFontResource>  Fonts  = new List<PdfFontResource>();
        public readonly List<PdfImageResource> Images = new List<PdfImageResource>();
        public readonly List<PdfAnnotation>    Annots = new List<PdfAnnotation>();
    }

    // ── main document writer ─────────────────────────────────────────────────

    /// <summary>
    /// Builds a PDF 1.4 document with no external dependencies beyond SharpZipLib.
    /// Supports:
    ///   • 14 standard Type1 fonts (no embedding, WinAnsiEncoding)
    ///   • TrueType CIDFont Type2 with Identity-H encoding and full font embedding
    ///   • JPEG images (DCTDecode) and raw-RGB images (FlateDecode)
    ///   • Link and tooltip annotations
    ///   • Document metadata
    /// </summary>
    public sealed class PdfDocumentWriter
    {
        // Encoding.Latin1 is only available on .NET Core 2.1+; use ISO-8859-1 for compatibility.
        private static readonly Encoding _latin1 = Encoding.GetEncoding("iso-8859-1");

        private readonly List<PdfPage>                        _pages     = new List<PdfPage>();
        private readonly Dictionary<string, PdfFontResource>  _fontCache = new Dictionary<string, PdfFontResource>(StringComparer.Ordinal);
        private readonly Dictionary<string, PdfImageResource> _imgCache  = new Dictionary<string, PdfImageResource>(StringComparer.Ordinal);
        private int _fontCount, _imgCount;
        private string _author = "", _title = "", _subject = "", _creator = "Majorsilence.Pdf";

        // ── document setup ───────────────────────────────────────────────────

        public void SetMetadata(string? author, string? title, string? subject, string? creator)
        {
            _author  = author  ?? "";
            _title   = title   ?? "";
            _subject = subject ?? "";
            _creator = creator ?? "Majorsilence.Pdf";
        }

        public PdfPage AddPage(float widthPt, float heightPt)
        {
            var p = new PdfPage { Width = widthPt, Height = heightPt };
            _pages.Add(p);
            return p;
        }

        // ── font registration ────────────────────────────────────────────────

        public PdfFontResource GetOrAddStandardFont(string baseFont)
        {
            string key = "std:" + baseFont;
            if (_fontCache.TryGetValue(key, out var f)) return f;
            f = new PdfFontResource
            {
                PdfName      = "F" + (++_fontCount),
                BaseFontName = baseFont,
                IsStandard14 = true,
            };
            _fontCache[key] = f;
            return f;
        }

        public PdfFontResource GetOrAddTtfFont(string filePath, TrueTypeFont ttf)
        {
            string key = "ttf:" + filePath;
            if (_fontCache.TryGetValue(key, out var f)) return f;
            f = new PdfFontResource
            {
                PdfName      = "F" + (++_fontCount),
                BaseFontName = (ttf.PostScriptName ?? Path.GetFileNameWithoutExtension(filePath)).Replace(" ", "-"),
                IsStandard14 = false,
                Ttf          = ttf,
            };
            _fontCache[key] = f;
            return f;
        }

        // ── image registration ───────────────────────────────────────────────

        /// <summary>
        /// Register an image for embedding.
        /// <paramref name="data"/> must be raw JPEG bytes when <paramref name="isJpeg"/> is true,
        /// or tightly-packed 24-bit RGB bytes (R G B R G B …) otherwise.
        /// </summary>
        public PdfImageResource GetOrAddImage(byte[] data, int samplesW, int samplesH, bool isJpeg)
        {
            string key = $"{data.Length}:{samplesW}x{samplesH}:{BitConverter.ToString(data, 0, Math.Min(32, data.Length))}";
            if (_imgCache.TryGetValue(key, out var img)) return img;
            img = new PdfImageResource
            {
                PdfName  = "Im" + (++_imgCount),
                SamplesW = samplesW,
                SamplesH = samplesH,
                IsJpeg   = isJpeg,
                Data     = data,
            };
            _imgCache[key] = img;
            return img;
        }

        // ── serialization ────────────────────────────────────────────────────

        public void Write(Stream output)
        {
            long pos = 0;
            void RawWrite(byte[] b) { output.Write(b, 0, b.Length); pos += b.Length; }
            void Str(string s)  { RawWrite(_latin1.GetBytes(s)); }

            // ── object allocation ────────────────────────────────────────────
            int nextObj = 1;
            int AllocObj() => nextObj++;

            int catalogObj = AllocObj(); // 1
            int pagesObj   = AllocObj(); // 2
            int infoObj    = AllocObj(); // 3

            // fonts: standard = 1 obj, TTF = 5 objs
            foreach (var f in _fontCache.Values)
            {
                f.ObjNum = AllocObj();
                if (!f.IsStandard14)
                {
                    AllocObj(); // CIDFont
                    AllocObj(); // FontDescriptor
                    AllocObj(); // FontFile2
                    AllocObj(); // ToUnicode
                }
            }

            // images
            foreach (var img in _imgCache.Values)
                img.ObjNum = AllocObj();

            // pages: content stream + page dict + N annotation objects each
            int[] pageContentObjs = new int[_pages.Count];
            int[] pageDictObjs    = new int[_pages.Count];
            int[][] annotObjs     = new int[_pages.Count][];
            for (int i = 0; i < _pages.Count; i++)
            {
                pageContentObjs[i] = AllocObj();
                pageDictObjs[i]    = AllocObj();
                annotObjs[i]       = new int[_pages[i].Annots.Count];
                for (int j = 0; j < _pages[i].Annots.Count; j++)
                    annotObjs[i][j] = AllocObj();
            }

            int totalObjs = nextObj;
            var offsets   = new long[totalObjs];

            // ── local write helpers ──────────────────────────────────────────

            void BeginObj(int n) { offsets[n] = pos; Str($"{n} 0 obj\n"); }
            void EndObj() => Str("endobj\n");

            void WriteDictObj(int n, string body)
            {
                BeginObj(n); Str($"<<\n{body}>>\n"); EndObj();
            }

            void WriteStreamObj(int n, string? extraDict, byte[] payload, bool compress)
            {
                if (compress) payload = Compress(payload);
                BeginObj(n);
                Str("<<\n");
                if (compress)             Str("/Filter /FlateDecode\n");
                if (extraDict != null)    Str(extraDict);
                Str($"/Length {payload.Length}\n");
                Str(">>\nstream\n");
                RawWrite(payload);
                Str("\nendstream\n");
                EndObj();
            }

            // ── %PDF header ──────────────────────────────────────────────────
            Str("%PDF-1.4\n%\xff\xff\xff\xff\n");

            // ── catalog ──────────────────────────────────────────────────────
            WriteDictObj(catalogObj, $"/Type /Catalog\n/Pages {pagesObj} 0 R\n");

            // ── info ─────────────────────────────────────────────────────────
            string date = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            WriteDictObj(infoObj,
                $"/Author ({Esc(_author)})\n/Title ({Esc(_title)})\n" +
                $"/Subject ({Esc(_subject)})\n/Creator ({Esc(_creator)})\n" +
                $"/CreationDate (D:{date}Z)\n");

            // ── pages tree ───────────────────────────────────────────────────
            string kids = string.Join(" ", pageDictObjs.Select(n => $"{n} 0 R"));
            WriteDictObj(pagesObj, $"/Type /Pages\n/Kids [{kids}]\n/Count {_pages.Count}\n");

            // ── fonts ────────────────────────────────────────────────────────
            foreach (var font in _fontCache.Values)
            {
                if (font.IsStandard14)
                {
                    WriteDictObj(font.ObjNum,
                        $"/Type /Font\n/Subtype /Type1\n" +
                        $"/BaseFont /{font.BaseFontName}\n/Encoding /WinAnsiEncoding\n");
                }
                else
                {
                    var ttf    = font.Ttf!;
                    int type0  = font.ObjNum;
                    int cidObj = type0 + 1;
                    int fdObj  = type0 + 2;
                    int ff2Obj = type0 + 3;
                    int toUObj = type0 + 4;
                    int upm    = ttf.UnitsPerEm;
                    string psn = font.BaseFontName;

                    // FontFile2 (raw TTF bytes)
                    WriteStreamObj(ff2Obj, $"/Length1 {ttf.Data.Length}\n", ttf.Data, compress: true);

                    // FontDescriptor
                    int Scale(short v) => upm > 0 ? (int)Math.Round(1000.0 * v / upm) : 0;
                    int asc  = Scale(ttf.Ascender);
                    int dsc  = Scale(ttf.Descender);
                    WriteDictObj(fdObj,
                        $"/Type /FontDescriptor\n/FontName /{psn}\n/Flags 32\n" +
                        $"/FontBBox [{Scale(ttf.XMin)} {Scale(ttf.YMin)} {Scale(ttf.XMax)} {Scale(ttf.YMax)}]\n" +
                        $"/ItalicAngle 0\n/Ascent {asc}\n/Descent {dsc}\n" +
                        $"/CapHeight {asc}\n/StemV 80\n/FontFile2 {ff2Obj} 0 R\n");

                    // /W widths array [0 [w0 w1 ... wN-1]]
                    var wSb = new StringBuilder("[0 [");
                    for (int g = 0; g < ttf.NumGlyphs; g++)
                    {
                        if (g > 0) wSb.Append(' ');
                        wSb.Append(upm > 0 ? (int)Math.Round(1000.0 * ttf.GetAdvanceWidth((ushort)g) / upm) : 500);
                    }
                    wSb.Append("]]");

                    // CIDFont
                    WriteDictObj(cidObj,
                        $"/Type /Font\n/Subtype /CIDFontType2\n/BaseFont /{psn}\n" +
                        $"/CIDSystemInfo << /Registry (Adobe) /Ordering (Identity) /Supplement 0 >>\n" +
                        $"/FontDescriptor {fdObj} 0 R\n/DW 1000\n/W {wSb}\n");

                    // ToUnicode CMap
                    var pairs = ttf.GetCharGlyphMappings()
                                   .Where(p => p.codePoint <= 0xFFFF)
                                   .OrderBy(p => p.glyphId)
                                   .ToList();
                    var cmap = new StringBuilder(pairs.Count * 20 + 256);
                    cmap.Append("/CIDInit /ProcSet findresource begin\n12 dict begin\nbegincmap\n");
                    cmap.Append("/CIDSystemInfo << /Registry (Adobe) /Ordering (UCS) /Supplement 0 >> def\n");
                    cmap.Append("/CMapName /Adobe-Identity-UCS def\n/CMapType 2 def\n");
                    for (int i = 0; i < pairs.Count; i += 100)
                    {
                        int chunk = Math.Min(100, pairs.Count - i);
                        cmap.Append($"{chunk} beginbfchar\n");
                        for (int j = 0; j < chunk; j++)
                        {
                            var (cp, gid) = pairs[i + j];
                            cmap.Append($"<{gid:X4}> <{cp:X4}>\n");
                        }
                        cmap.Append("endbfchar\n");
                    }
                    cmap.Append("endcmap\nCMapType end\nend\n");
                    WriteStreamObj(toUObj, null, Encoding.ASCII.GetBytes(cmap.ToString()), compress: true);

                    // Type0 composite font
                    WriteDictObj(type0,
                        $"/Type /Font\n/Subtype /Type0\n/BaseFont /{psn}\n" +
                        $"/Encoding /Identity-H\n/DescendantFonts [{cidObj} 0 R]\n" +
                        $"/ToUnicode {toUObj} 0 R\n");
                }
            }

            // ── images ───────────────────────────────────────────────────────
            foreach (var img in _imgCache.Values)
            {
                offsets[img.ObjNum] = pos;
                BeginObj(img.ObjNum);
                if (img.IsJpeg)
                {
                    Str($"<</Type /XObject /Subtype /Image\n" +
                        $"/Width {img.SamplesW} /Height {img.SamplesH}\n" +
                        $"/ColorSpace /DeviceRGB /BitsPerComponent 8\n" +
                        $"/Filter /DCTDecode /Length {img.Data.Length}>>\nstream\n");
                    RawWrite(img.Data);
                }
                else
                {
                    byte[] compressed = Compress(img.Data);
                    Str($"<</Type /XObject /Subtype /Image\n" +
                        $"/Width {img.SamplesW} /Height {img.SamplesH}\n" +
                        $"/ColorSpace /DeviceRGB /BitsPerComponent 8\n" +
                        $"/Filter /FlateDecode /Length {compressed.Length}>>\nstream\n");
                    RawWrite(compressed);
                }
                Str("\nendstream\n");
                EndObj();
            }

            // ── pages ────────────────────────────────────────────────────────
            for (int pi = 0; pi < _pages.Count; pi++)
            {
                var page = _pages[pi];

                // content stream
                byte[] contentBytes = _latin1.GetBytes(page.Content.ToString());
                WriteStreamObj(pageContentObjs[pi], null, contentBytes, compress: true);

                // resource dictionary
                var fontRes = string.Join(" ", page.Fonts.Distinct().Select(f => $"/{f.PdfName} {f.ObjNum} 0 R"));
                var imgRes  = string.Join(" ", page.Images.Distinct().Select(i => $"/{i.PdfName} {i.ObjNum} 0 R"));
                string resources =
                    $"/Font <<{fontRes}>>\n" +
                    $"/XObject <<{imgRes}>>\n" +
                    $"/ProcSet [/PDF /Text /ImageC /ImageB /ImageI]";

                string annotRefs = annotObjs[pi].Length > 0
                    ? "[" + string.Join(" ", annotObjs[pi].Select(n => $"{n} 0 R")) + "]"
                    : "[]";

                WriteDictObj(pageDictObjs[pi],
                    $"/Type /Page\n/Parent {pagesObj} 0 R\n" +
                    $"/MediaBox [0 0 {Fmt(page.Width)} {Fmt(page.Height)}]\n" +
                    $"/Contents {pageContentObjs[pi]} 0 R\n" +
                    $"/Resources <<{resources}>>\n" +
                    $"/Annots {annotRefs}\n");

                // annotation objects
                for (int ai = 0; ai < page.Annots.Count; ai++)
                {
                    var ann = page.Annots[ai];
                    string annDict = ann.Uri != null
                        ? $"/Type /Annot\n/Subtype /Link\n" +
                          $"/Rect [{Fmt(ann.X1)} {Fmt(ann.Y1)} {Fmt(ann.X2)} {Fmt(ann.Y2)}]\n" +
                          $"/Border [0 0 0]\n" +
                          $"/A <</Type /Action /S /URI /URI ({Esc(ann.Uri)})/>>>\n"
                        : $"/Type /Annot\n/Subtype /Text\n" +
                          $"/Rect [{Fmt(ann.X1)} {Fmt(ann.Y1)} {Fmt(ann.X2)} {Fmt(ann.Y2)}]\n" +
                          $"/Contents ({Esc(ann.Tooltip ?? "")})\n";
                    WriteDictObj(annotObjs[pi][ai], annDict);
                }
            }

            // ── cross-reference table ────────────────────────────────────────
            long xrefPos = pos;
            Str($"xref\n0 {totalObjs}\n");
            Str("0000000000 65535 f \n");
            for (int i = 1; i < totalObjs; i++)
                Str($"{offsets[i]:D10} 00000 n \n");

            // ── trailer ──────────────────────────────────────────────────────
            Str($"trailer\n<<\n/Size {totalObjs}\n/Root {catalogObj} 0 R\n/Info {infoObj} 0 R\n>>\n");
            Str($"startxref\n{xrefPos}\n%%EOF\n");
        }

        // ── private helpers ───────────────────────────────────────────────────

        private static byte[] Compress(byte[] data)
        {
            using var ms = new MemoryStream();
            using (var ds = new DeflaterOutputStream(ms, new Deflater(Deflater.DEFAULT_COMPRESSION, false)))
                ds.Write(data, 0, data.Length);
            return ms.ToArray();
        }

        // Escape a string for use inside a PDF literal string () pair.
        private static string Esc(string? s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        }

        // Format a float for a PDF real number (up to 3 decimal places).
        public static string Fmt(float v)  => v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        public static string Fmt(double v) => v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>
        /// Encode a text string for use in a PDF content stream Tj operator.
        /// Standard Type1 fonts use a WinAnsi literal string; CIDFont Type2 (Identity-H)
        /// uses a hex string of big-endian glyph IDs.
        /// </summary>
        public static string EncodeText(string text, PdfFontResource font)
        {
            if (font.IsStandard14)
            {
                var sb = new StringBuilder(text.Length + 2);
                sb.Append('(');
                foreach (char c in text)
                {
                    if      (c == '\\') sb.Append("\\\\");
                    else if (c == '(')  sb.Append("\\(");
                    else if (c == ')')  sb.Append("\\)");
                    else if (c < 256)   sb.Append(c);
                    else                sb.Append('?');
                }
                sb.Append(')');
                return sb.ToString();
            }
            else
            {
                var sb = new StringBuilder(text.Length * 4 + 2);
                sb.Append('<');
                foreach (char c in text)
                    sb.Append(font.Ttf!.GetGlyphId(c).ToString("X4"));
                sb.Append('>');
                return sb.ToString();
            }
        }
    }
}
