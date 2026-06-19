// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.IO.Compression;

namespace Majorsilence.Pdf.Internal
{
    // ── small data containers ─────────────────────────────────────────────────

    internal sealed class FontResource
    {
        internal readonly string PdfName;
        internal readonly string? StandardName;
        internal readonly TrueTypeFont? Ttf;
        internal readonly HashSet<ushort>? UsedGlyphs;
        internal FontResource(string pdfName, string standardName)
        { PdfName = pdfName; StandardName = standardName; }
        internal FontResource(string pdfName, TrueTypeFont ttf)
        { PdfName = pdfName; Ttf = ttf; UsedGlyphs = new HashSet<ushort>(); }
        internal bool IsStandard => StandardName != null;
    }

    internal sealed class ImageResource
    {
        internal readonly string PdfName;
        internal readonly byte[] Data;
        internal readonly int Width, Height;
        internal readonly bool IsJpeg;
        internal readonly bool IsRgba;
        internal ImageResource(string pdfName, byte[] data, int w, int h, bool isJpeg, bool isRgba = false)
        { PdfName = pdfName; Data = data; Width = w; Height = h; IsJpeg = isJpeg; IsRgba = isRgba; }
    }

    internal sealed class PageAnnotation
    {
        internal readonly float X1, Y1, X2, Y2;
        internal readonly string? Uri;
        internal readonly string? Tooltip;
        internal PageAnnotation(float x1, float y1, float x2, float y2, string? uri, string? tooltip)
        { X1 = x1; Y1 = y1; X2 = x2; Y2 = y2; Uri = uri; Tooltip = tooltip; }
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

        private readonly List<PageData>   _pages  = new List<PageData>();
        private readonly List<FontResource>  _fonts  = new List<FontResource>();
        private readonly Dictionary<string, FontResource> _stdFontMap =
            new Dictionary<string, FontResource>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FontResource> _ttfFontMap =
            new Dictionary<string, FontResource>(StringComparer.OrdinalIgnoreCase);
        private readonly List<ImageResource>   _images   = new List<ImageResource>();
        private readonly Dictionary<string, ImageResource> _imageMap =
            new Dictionary<string, ImageResource>();

        private string _author  = "";
        private string _title   = "";
        private string _subject = "";
        private string _creator = "Majorsilence.Pdf";

        private PdfVersion         _version   = PdfVersion.Pdf14;
        private IStreamEncryptor?  _encryptor;
        private ISignatureHandler? _sigHandler;

        // ── configuration ─────────────────────────────────────────────────────

        internal void SetMetadata(string author, string title, string subject, string creator)
        { _author = author; _title = title; _subject = subject; _creator = creator; }

        internal void SetVersion(PdfVersion version) => _version = version;
        internal void SetEncryptor(IStreamEncryptor? enc) => _encryptor = enc;
        internal void SetSignatureHandler(ISignatureHandler? sig) => _sigHandler = sig;

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
            _fonts.Add(fr); _stdFontMap[standardName] = fr;
            return fr;
        }

        internal FontResource GetOrAddTtfFont(string filePath, TrueTypeFont ttf)
        {
            if (_ttfFontMap.TryGetValue(filePath, out var fr)) return fr;
            string pdfName = "F" + (_fonts.Count + 1);
            fr = new FontResource(pdfName, ttf);
            _fonts.Add(fr); _ttfFontMap[filePath] = fr;
            return fr;
        }

        // ── image management ─────────────────────────────────────────────────

        internal ImageResource GetOrAddImage(byte[] rawOrJpeg, int width, int height, bool isJpeg)
        {
            string key = width + "x" + height + ":" + rawOrJpeg.Length;
            if (_imageMap.TryGetValue(key, out var ir)) return ir;
            string pdfName = "Im" + (_images.Count + 1);
            ir = new ImageResource(pdfName, rawOrJpeg, width, height, isJpeg);
            _images.Add(ir); _imageMap[key] = ir;
            return ir;
        }

        internal ImageResource? FindImage(string key) =>
            _imageMap.TryGetValue(key, out var ir) ? ir : null;

        internal ImageResource GetOrAddRgbaImage(string key, byte[] rgba, int width, int height)
        {
            if (_imageMap.TryGetValue(key, out var ir)) return ir;
            string pdfName = "Im" + (_images.Count + 1);
            ir = new ImageResource(pdfName, rgba, width, height, isJpeg: false, isRgba: true);
            _images.Add(ir); _imageMap[key] = ir;
            return ir;
        }

        // ── serialization ────────────────────────────────────────────────────

        internal void Write(Stream stream)
        {
            // Generate a random 16-byte file ID used in the /ID array and (when
            // encryption is active) in the key-derivation algorithm.
            byte[] fileId = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(fileId);

            // Let the encryptor derive its session key before any object is built.
            _encryptor?.Initialize(fileId);

            byte[]? sigPlaceholderBytes = null;
            long    sigBodyOffset       = 0;

            using (var ms = new MemoryStream())
            {
                WriteToMemory(ms, fileId, ref sigPlaceholderBytes, ref sigBodyOffset);

                if (_sigHandler != null && sigPlaceholderBytes != null)
                    _sigHandler.Fixup(ms, sigPlaceholderBytes, sigBodyOffset);

                ms.Position = 0;
                ms.CopyTo(stream);
            }
        }

        private void WriteToMemory(MemoryStream ms, byte[] fileId,
            ref byte[]? sigPlaceholderBytes, ref long sigBodyOffset)
        {
            var  w       = new PdfWriter(ms);
            bool isPdf20 = _version == PdfVersion.Pdf20;
            bool hasEnc  = _encryptor != null;
            bool hasSig  = _sigHandler != null;

            w.WriteLine(isPdf20 ? "%PDF-2.0" : "%PDF-1.4");
            w.WriteRaw(new byte[] { (byte)'%', 0xE2, 0xE3, 0xCF, 0xD3, (byte)'\n' });

            // Object numbering: list index i → PDF object number i+1.
            var objects = new List<byte[]> { null!, null! }; // [0]=Catalog [1]=Pages

            // ── /Encrypt object ────────────────────────────────────────────────
            int encObjNum = -1;
            if (hasEnc)
            {
                encObjNum = objects.Count + 1;
                objects.Add(_encryptor!.BuildEncryptDict());
            }

            // ── XMP metadata (PDF 2.0, never encrypted per spec) ──────────────
            int xmpObjNum = -1;
            if (isPdf20)
            {
                xmpObjNum = objects.Count + 1;
                objects.Add(BuildXmpMetadataObj());
            }

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
                if (_images[ii].IsRgba)
                {
                    int mainObjNum = objects.Count + 1;
                    int maskObjNum = objects.Count + 2;
                    objects.Add(BuildRgbaMainImageObj(_images[ii], maskObjNum, mainObjNum));
                    objects.Add(BuildAlphaMaskObj(_images[ii], maskObjNum));
                }
                else
                {
                    objects.Add(BuildImageObj(_images[ii], objects.Count + 1));
                }
            }

            // ── resource name strings ─────────────────────────────────────────
            var fontResEntries  = new StringBuilder();
            for (int fi = 0; fi < _fonts.Count; fi++)
            {
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

                pageContentIdx[pi] = objects.Count;
                int contentObjNum  = objects.Count + 1;
                byte[] raw         = Latin1.GetBytes(page.Content.ToString());
                byte[] compressed  = Compress(raw);
                byte[] streamData  = _encryptor != null
                    ? _encryptor.EncryptStream(contentObjNum, 0, compressed)
                    : compressed;
                string hdrStr = $"<< /Filter /FlateDecode /Length {streamData.Length} >>\nstream\n";
                objects.Add(Concat(Latin1.GetBytes(hdrStr), streamData, Latin1.GetBytes("\nendstream")));

                pageDictIdx[pi]   = objects.Count;
                string pageDict =
                    $"<< /Type /Page /Parent 2 0 R\n" +
                    $"   /MediaBox [0 0 {Fmt(page.Width)} {Fmt(page.Height)}]\n" +
                    $"   /Contents {contentObjNum} 0 R\n" +
                    $"   /Resources << /Font << {fontResEntries} >> /XObject << {imageResEntries} >> >>\n";

                if (page.Annotations.Count > 0)
                {
                    int firstAnnotObjNum = objects.Count + 2;
                    var annots = new StringBuilder("/Annots [ ");
                    for (int ai = 0; ai < page.Annotations.Count; ai++)
                        annots.Append($"{firstAnnotObjNum + ai} 0 R ");
                    annots.Append("]\n");
                    pageDict += annots.ToString();
                }
                pageDict += ">>";
                objects.Add(Latin1.GetBytes(pageDict));

                foreach (var ann in page.Annotations)
                    objects.Add(BuildAnnotationObj(ann, objects.Count + 1));
            }

            // ── info dict ─────────────────────────────────────────────────────
            int infoObjIdx = objects.Count;
            objects.Add(BuildInfoObj(objects.Count + 1));

            // ── signature placeholder ─────────────────────────────────────────
            int sigObjIdx       = -1;
            int sigWidgetObjNum = -1;
            if (hasSig)
            {
                sigObjIdx   = objects.Count;
                int sObjNum = objects.Count + 1;
                var spb     = _sigHandler!.BuildPlaceholder();
                sigPlaceholderBytes = spb;
                objects.Add(spb);

                sigWidgetObjNum = objects.Count + 1;
                objects.Add(BuildSigWidgetObj(sigWidgetObjNum, sObjNum,
                    _pages.Count > 0 ? pageDictIdx[0] + 1 : 1));
            }

            // ── catalog ───────────────────────────────────────────────────────
            string catalog = "<< /Type /Catalog /Pages 2 0 R";
            if (xmpObjNum > 0) catalog += $" /Metadata {xmpObjNum} 0 R";
            if (hasSig) catalog += $" /AcroForm << /Fields [{sigWidgetObjNum} 0 R] /SigFlags 3 >>";
            catalog += " >>";
            objects[0] = Latin1.GetBytes(catalog);

            // ── pages ─────────────────────────────────────────────────────────
            var kids = new StringBuilder("/Kids [ ");
            for (int pi = 0; pi < _pages.Count; pi++)
                kids.Append($"{pageDictIdx[pi] + 1} 0 R ");
            kids.Append("]");
            objects[1] = Latin1.GetBytes($"<< /Type /Pages {kids} /Count {_pages.Count} >>");

            // ── write objects ─────────────────────────────────────────────────
            int   totalObjs   = objects.Count;
            var   xrefOffsets = new long[totalObjs];
            for (int i = 0; i < totalObjs; i++)
            {
                xrefOffsets[i] = ms.Position;
                w.WriteLine($"{i + 1} 0 obj");
                if (i == sigObjIdx) sigBodyOffset = ms.Position;
                w.WriteBytes(objects[i]);
                w.WriteLine("");
                w.WriteLine("endobj");
            }

            // /ID — required when encryption is present; good practice always.
            string idHex   = BytesToHex(fileId);
            string idArray = $"[<{idHex}> <{idHex}>]";

            if (isPdf20)
            {
                long xrefStreamStart = ms.Position;
                WriteXrefStream(w, ms, xrefOffsets, totalObjs,
                    infoObjIdx + 1, encObjNum, xrefStreamStart, idArray);
                w.WriteLine("startxref");
                w.WriteLine(xrefStreamStart.ToString());
                w.Write("%%EOF");
            }
            else
            {
                long xrefPos = ms.Position;
                w.WriteLine("xref");
                w.WriteLine($"0 {totalObjs + 1}");
                w.WriteLine("0000000000 65535 f ");
                foreach (long off in xrefOffsets)
                    w.WriteLine(off.ToString("D10") + " 00000 n ");

                w.WriteLine("trailer");
                string trailer = $"<< /Size {totalObjs + 1} /Root 1 0 R /Info {infoObjIdx + 1} 0 R /ID {idArray}";
                if (encObjNum > 0) trailer += $" /Encrypt {encObjNum} 0 R";
                trailer += " >>";
                w.WriteLine(trailer);
                w.WriteLine("startxref");
                w.WriteLine(xrefPos.ToString());
                w.Write("%%EOF");
            }
        }

        // ── PDF 2.0 compressed xref stream ────────────────────────────────────

        private static void WriteXrefStream(PdfWriter w, MemoryStream ms,
            long[] xrefOffsets, int totalObjs, int infoObjNum, int encObjNum,
            long xrefStreamStart, string idArray)
        {
            int xrefObjNum = totalObjs + 1;
            int size       = totalObjs + 2;
            var entries    = new byte[size * 7];

            entries[5] = 0xFF; entries[6] = 0xFF; // free head

            for (int i = 0; i < totalObjs; i++)
            {
                int  b   = (i + 1) * 7;
                long off = xrefOffsets[i];
                entries[b]     = 0x01;
                entries[b + 1] = (byte)(off >> 24);
                entries[b + 2] = (byte)(off >> 16);
                entries[b + 3] = (byte)(off >> 8);
                entries[b + 4] = (byte)off;
            }

            int last = (totalObjs + 1) * 7;
            entries[last]     = 0x01;
            entries[last + 1] = (byte)(xrefStreamStart >> 24);
            entries[last + 2] = (byte)(xrefStreamStart >> 16);
            entries[last + 3] = (byte)(xrefStreamStart >> 8);
            entries[last + 4] = (byte)xrefStreamStart;

            byte[] compressed = Compress(entries);

            w.WriteLine($"{xrefObjNum} 0 obj");
            w.Write($"<< /Type /XRef /Size {size} /W [1 4 2]");
            w.Write($" /Root 1 0 R /Info {infoObjNum} 0 R /ID {idArray}");
            if (encObjNum > 0) w.Write($" /Encrypt {encObjNum} 0 R");
            w.WriteLine($" /Filter /FlateDecode /Length {compressed.Length} >>");
            w.WriteLine("stream");
            w.WriteBytes(compressed);
            w.WriteLine("\nendstream");
            w.WriteLine("endobj");
        }

        // ── object builders ───────────────────────────────────────────────────

        private byte[] BuildInfoObj(int objNum)
        {
            string auth  = EncStr(objNum, _author);
            string title = EncStr(objNum, _title);
            string subj  = EncStr(objNum, _subject);
            string creat = EncStr(objNum, _creator);
            string prod  = EncStr(objNum, "Majorsilence.Pdf");
            return Latin1.GetBytes(
                $"<< /Producer {prod} /Author {auth} " +
                $"/Title {title} /Subject {subj} /Creator {creat} >>");
        }

        private static byte[] BuildStandardFontObj(FontResource font) =>
            Latin1.GetBytes(
                $"<< /Type /Font /Subtype /Type1 /BaseFont /{font.StandardName} " +
                $"/Encoding /WinAnsiEncoding >>");

        private void BuildTtfFontObjs(FontResource font, List<byte[]> objects)
        {
            var    ttf       = font.Ttf!;
            string psName    = ttf.PostScriptName;
            int    baseCount = objects.Count;

            int toUnicodeObjNum  = baseCount + 1;
            int descriptorObjNum = baseCount + 2;
            int fontFileObjNum   = baseCount + 3;
            int cidFontObjNum    = baseCount + 4;

            var used = font.UsedGlyphs;

            // ── ToUnicode CMap ────────────────────────────────────────────────
            var allMappings = ttf.GetCharGlyphMappings();
            var mapList = new List<(int codePoint, ushort glyphId)>();
            foreach (var m in allMappings)
                if (used == null || used.Contains(m.glyphId)) mapList.Add(m);

            var cmap = new StringBuilder();
            cmap.Append("/CIDInit /ProcSet findresource begin\n12 dict begin\nbegincmap\n");
            cmap.Append("/CIDSystemInfo << /Registry (Adobe) /Ordering (UCS) /Supplement 0 >> def\n");
            cmap.Append("/CMapName /Adobe-Identity-UCS def\n/CMapType 2 def\n");
            cmap.Append("1 begincodespacerange\n<0000> <FFFF>\nendcodespacerange\n");
            for (int i = 0; i < mapList.Count; i += 100)
            {
                int chunk = Math.Min(100, mapList.Count - i);
                cmap.Append($"{chunk} beginbfchar\n");
                for (int j = i; j < i + chunk; j++)
                    cmap.Append($"<{mapList[j].glyphId:X4}> <{mapList[j].codePoint:X4}>\n");
                cmap.Append("endbfchar\n");
            }
            cmap.Append("endcmap\nCMapName currentdict /CMap defineresource pop\nend\nend");

            byte[] cmapBytes = Latin1.GetBytes(cmap.ToString());
            byte[] cmapComp  = Compress(cmapBytes);
            byte[] cmapData  = _encryptor != null
                ? _encryptor.EncryptStream(toUnicodeObjNum, 0, cmapComp) : cmapComp;
            string cmapHdr   = $"<< /Filter /FlateDecode /Length {cmapData.Length} >>\nstream\n";
            objects.Add(Concat(Latin1.GetBytes(cmapHdr), cmapData, Latin1.GetBytes("\nendstream")));

            // ── /W array ──────────────────────────────────────────────────────
            var glyphWidths = new SortedDictionary<ushort, int>();
            foreach (var m in mapList)
            {
                int w1000 = (int)Math.Round(ttf.GetAdvanceWidth(m.glyphId) * 1000.0 / ttf.UnitsPerEm);
                glyphWidths[m.glyphId] = w1000;
            }
            var wArray = new StringBuilder("/W [");
            foreach (var kv in glyphWidths) wArray.Append($" {kv.Key} [{kv.Value}]");
            wArray.Append(" ]");

            float scale = 1000f / ttf.UnitsPerEm;
            int llx = (int)Math.Round(ttf.XMin * scale), lly = (int)Math.Round(ttf.YMin * scale);
            int urx = (int)Math.Round(ttf.XMax * scale), ury = (int)Math.Round(ttf.YMax * scale);
            int asc = (int)Math.Round(ttf.Ascender * scale), desc = (int)Math.Round(ttf.Descender * scale);

            // ── FontDescriptor ────────────────────────────────────────────────
            objects.Add(Latin1.GetBytes(
                $"<< /Type /FontDescriptor /FontName /{psName} /Flags 32 " +
                $"/FontBBox [{llx} {lly} {urx} {ury}] " +
                $"/ItalicAngle 0 /Ascent {asc} /Descent {desc} " +
                $"/CapHeight {asc} /StemV 80 /FontFile2 {fontFileObjNum} 0 R >>"));

            // ── FontFile2 stream ───────────────────────────────────────────────
            byte[] fontData  = (used != null && used.Count > 0) ? ttf.Subset(used) : ttf.Data;
            byte[] fontComp  = Compress(fontData);
            byte[] fontStream = _encryptor != null
                ? _encryptor.EncryptStream(fontFileObjNum, 0, fontComp) : fontComp;
            string ffHdr = $"<< /Filter /FlateDecode /Length {fontStream.Length} /Length1 {fontData.Length} >>\nstream\n";
            objects.Add(Concat(Latin1.GetBytes(ffHdr), fontStream, Latin1.GetBytes("\nendstream")));

            // ── CIDFont ───────────────────────────────────────────────────────
            objects.Add(Latin1.GetBytes(
                $"<< /Type /Font /Subtype /CIDFontType2 /BaseFont /{psName} " +
                $"/CIDSystemInfo << /Registry {EncLitStr(cidFontObjNum, "Adobe")} /Ordering {EncLitStr(cidFontObjNum, "Identity")} /Supplement 0 >> " +
                $"/FontDescriptor {descriptorObjNum} 0 R /DW 1000 {wArray} >>"));

            // ── Type0 Font ────────────────────────────────────────────────────
            objects.Add(Latin1.GetBytes(
                $"<< /Type /Font /Subtype /Type0 /BaseFont /{psName} " +
                $"/Encoding /Identity-H /DescendantFonts [{cidFontObjNum} 0 R] " +
                $"/ToUnicode {toUnicodeObjNum} 0 R >>"));
        }

        private byte[] BuildImageObj(ImageResource img, int objNum)
        {
            string filter = img.IsJpeg ? "/DCTDecode" : "/FlateDecode";
            byte[] raw    = img.IsJpeg ? img.Data : Compress(img.Data);
            byte[] data   = _encryptor != null ? _encryptor.EncryptStream(objNum, 0, raw) : raw;
            string hdr =
                $"<< /Type /XObject /Subtype /Image /Width {img.Width} /Height {img.Height} " +
                $"/ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter {filter} /Length {data.Length} >>\nstream\n";
            return Concat(Latin1.GetBytes(hdr), data, Latin1.GetBytes("\nendstream"));
        }

        private byte[] BuildRgbaMainImageObj(ImageResource img, int sMaskPdfObjNum, int objNum)
        {
            byte[] rgba   = img.Data;
            int    pixels = img.Width * img.Height;
            byte[] rgb    = new byte[pixels * 3];
            for (int i = 0; i < pixels; i++)
            {
                rgb[i * 3]     = rgba[i * 4];
                rgb[i * 3 + 1] = rgba[i * 4 + 1];
                rgb[i * 3 + 2] = rgba[i * 4 + 2];
            }
            byte[] compressed = Compress(rgb);
            byte[] data       = _encryptor != null ? _encryptor.EncryptStream(objNum, 0, compressed) : compressed;
            string hdr =
                $"<< /Type /XObject /Subtype /Image /Width {img.Width} /Height {img.Height} " +
                $"/ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /FlateDecode " +
                $"/SMask {sMaskPdfObjNum} 0 R /Length {data.Length} >>\nstream\n";
            return Concat(Latin1.GetBytes(hdr), data, Latin1.GetBytes("\nendstream"));
        }

        private byte[] BuildAlphaMaskObj(ImageResource img, int objNum)
        {
            byte[] rgba   = img.Data;
            int    pixels = img.Width * img.Height;
            byte[] alpha  = new byte[pixels];
            for (int i = 0; i < pixels; i++) alpha[i] = rgba[i * 4 + 3];
            byte[] compressed = Compress(alpha);
            byte[] data       = _encryptor != null ? _encryptor.EncryptStream(objNum, 0, compressed) : compressed;
            string hdr =
                $"<< /Type /XObject /Subtype /Image /Width {img.Width} /Height {img.Height} " +
                $"/ColorSpace /DeviceGray /BitsPerComponent 8 /Filter /FlateDecode /Length {data.Length} >>\nstream\n";
            return Concat(Latin1.GetBytes(hdr), data, Latin1.GetBytes("\nendstream"));
        }

        private byte[] BuildXmpMetadataObj()
        {
            var x = new StringBuilder();
            x.Append("<?xpacket begin=\"\xEF\xBB\xBF\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>\n");
            x.Append("<x:xmpmeta xmlns:x=\"adobe:ns:meta/\">\n");
            x.Append("  <rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">\n");
            x.Append("    <rdf:Description rdf:about=\"\"\n");
            x.Append("      xmlns:dc=\"http://purl.org/dc/elements/1.1/\"\n");
            x.Append("      xmlns:xmp=\"http://ns.adobe.com/xap/1.0/\"\n");
            x.Append("      xmlns:pdf=\"http://ns.adobe.com/pdf/1.3/\">\n");
            if (!string.IsNullOrEmpty(_title))
            {
                x.Append("      <dc:title><rdf:Alt><rdf:li xml:lang=\"x-default\">");
                x.Append(XmlEscape(_title)); x.Append("</rdf:li></rdf:Alt></dc:title>\n");
            }
            if (!string.IsNullOrEmpty(_author))
            {
                x.Append("      <dc:creator><rdf:Seq><rdf:li>");
                x.Append(XmlEscape(_author)); x.Append("</rdf:li></rdf:Seq></dc:creator>\n");
            }
            if (!string.IsNullOrEmpty(_subject))
            {
                x.Append("      <dc:description><rdf:Alt><rdf:li xml:lang=\"x-default\">");
                x.Append(XmlEscape(_subject)); x.Append("</rdf:li></rdf:Alt></dc:description>\n");
            }
            x.Append("      <pdf:Producer>Majorsilence.Pdf</pdf:Producer>\n");
            if (!string.IsNullOrEmpty(_creator))
            {
                x.Append("      <xmp:CreatorTool>");
                x.Append(XmlEscape(_creator)); x.Append("</xmp:CreatorTool>\n");
            }
            x.Append("    </rdf:Description>\n  </rdf:RDF>\n</x:xmpmeta>\n<?xpacket end=\"w\"?>");

            // XMP is always unencrypted so external tools can read metadata.
            byte[] xmpUtf8 = System.Text.Encoding.UTF8.GetBytes(x.ToString());
            string hdr     = $"<< /Type /Metadata /Subtype /XML /Length {xmpUtf8.Length} >>\nstream\n";
            return Concat(Latin1.GetBytes(hdr), xmpUtf8, Latin1.GetBytes("\nendstream"));
        }

        private byte[] BuildAnnotationObj(PageAnnotation ann, int objNum)
        {
            if (ann.Uri != null)
            {
                string uriStr = _encryptor != null
                    ? _encryptor.EncryptPdfString(objNum, 0, Latin1.GetBytes(ann.Uri))
                    : $"({PdfString(ann.Uri)})";
                return Latin1.GetBytes(
                    $"<< /Type /Annot /Subtype /Link " +
                    $"/Rect [{Fmt(ann.X1)} {Fmt(ann.Y1)} {Fmt(ann.X2)} {Fmt(ann.Y2)}] " +
                    $"/Border [0 0 0] /A << /S /URI /URI {uriStr} >> >>");
            }
            else
            {
                return Latin1.GetBytes(
                    $"<< /Type /Annot /Subtype /Text " +
                    $"/Rect [{Fmt(ann.X1)} {Fmt(ann.Y1)} {Fmt(ann.X2)} {Fmt(ann.Y2)}] " +
                    $"/Contents {EncStr(objNum, ann.Tooltip ?? "")} >>");
            }
        }

        private static byte[] BuildSigWidgetObj(int objNum, int sigObjNum, int pageObjNum) =>
            Latin1.GetBytes(
                $"<< /Type /Annot /Subtype /Widget /FT /Sig " +
                $"/T (Signature) /V {sigObjNum} 0 R " +
                $"/Rect [0 0 0 0] /P {pageObjNum} 0 R /F 4 >>");

        // ── string helpers ────────────────────────────────────────────────────

        // Encrypts a plain ASCII/Latin-1 string literal: returns <hex> when
        // encrypted, or (...) parenthetical form otherwise.  Use for dict values
        // whose encoding must stay as-is (e.g. /CIDSystemInfo /Registry).
        private string EncLitStr(int objNum, string asciiText)
        {
            if (_encryptor != null) return _encryptor.EncryptPdfString(objNum, 0, Latin1.GetBytes(asciiText));
            return "(" + asciiText + ")";
        }

        // Returns an encrypted hex literal when encryption is active,
        // otherwise a UTF-16BE hex literal.
        private string EncStr(int objNum, string text)
        {
            var raw = new System.Collections.Generic.List<byte> { 0xFE, 0xFF };
            foreach (char c in text ?? "") { raw.Add((byte)((int)c >> 8)); raw.Add((byte)((int)c & 0xFF)); }
            byte[] bytes = raw.ToArray();
            if (_encryptor != null) return _encryptor.EncryptPdfString(objNum, 0, bytes);
            if (bytes.Length == 2) return "<FEFF>";
            var hex = new StringBuilder("<");
            foreach (byte b in bytes) hex.Append(b.ToString("X2"));
            hex.Append('>');
            return hex.ToString();
        }

        // ── static helpers ────────────────────────────────────────────────────

        private static string XmlEscape(string s) =>
            s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
             .Replace("\"", "&quot;").Replace("'", "&apos;");

        internal static string Fmt(float v) =>
            v.ToString("0.###", CultureInfo.InvariantCulture);

        internal static string PdfTextString(string s)
        {
            if (string.IsNullOrEmpty(s)) return "<FEFF>";
            var sb = new StringBuilder("<FEFF");
            foreach (char c in s) sb.Append(((int)c).ToString("X4"));
            sb.Append('>');
            return sb.ToString();
        }

        internal static string PdfString(string s) =>
            s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

        internal static string EncodeGlyphIds(string text, TrueTypeFont ttf,
            HashSet<ushort>? usedGlyphs = null)
        {
            var sb = new StringBuilder("<");
            for (int i = 0; i < text.Length; i++)
            {
                int codePoint;
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                { codePoint = char.ConvertToUtf32(text[i], text[i + 1]); i++; }
                else codePoint = text[i];
                ushort gid = ttf.GetGlyphId(codePoint);
                usedGlyphs?.Add(gid);
                sb.Append(gid.ToString("X4"));
            }
            sb.Append(">");
            return sb.ToString();
        }

        private static string BytesToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) sb.Append(b.ToString("X2"));
            return sb.ToString();
        }

        private static byte[] Compress(byte[] data)
        {
#if NET6_0_OR_GREATER
            using (var ms = new MemoryStream())
            {
                using (var zs = new ZLibStream(ms, CompressionLevel.SmallestSize, leaveOpen: true))
                    zs.Write(data, 0, data.Length);
                return ms.ToArray();
            }
#else
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0x78); ms.WriteByte(0x9C);
                using (var ds = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true))
                    ds.Write(data, 0, data.Length);
                uint adler = Adler32(data);
                ms.WriteByte((byte)(adler >> 24)); ms.WriteByte((byte)(adler >> 16));
                ms.WriteByte((byte)(adler >> 8));  ms.WriteByte((byte)adler);
                return ms.ToArray();
            }
#endif
        }

#if !NET6_0_OR_GREATER
        private static uint Adler32(byte[] data)
        {
            const uint MOD = 65521; uint s1 = 1, s2 = 0;
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
            internal void Write(string s)      { var b = Latin1.GetBytes(s); _s.Write(b, 0, b.Length); }
            internal void WriteLine(string s)  => Write(s + "\n");
            internal void WriteBytes(byte[] b) => _s.Write(b, 0, b.Length);
            internal void WriteRaw(byte[] b)   => _s.Write(b, 0, b.Length);
        }
    }
}
