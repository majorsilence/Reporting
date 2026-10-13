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
        internal readonly int Components; // colour components of a JPEG: 1 = gray, 3 = RGB
        internal ImageResource(string pdfName, byte[] data, int w, int h, bool isJpeg, bool isRgba = false, int components = 3)
        { PdfName = pdfName; Data = data; Width = w; Height = h; IsJpeg = isJpeg; IsRgba = isRgba; Components = components; }
    }

    internal sealed class PageAnnotation
    {
        internal readonly float X1, Y1, X2, Y2;
        internal readonly string? Uri;
        internal readonly string? Tooltip;
        internal readonly string? DestName;
        internal PageAnnotation(float x1, float y1, float x2, float y2, string? uri, string? tooltip, string? destName = null)
        { X1 = x1; Y1 = y1; X2 = x2; Y2 = y2; Uri = uri; Tooltip = tooltip; DestName = destName; }
    }

    internal sealed class PageDestination
    {
        internal readonly string Name;
        internal readonly float X, Y; // PDF space (bottom-left origin)
        internal PageDestination(string name, float x, float y) { Name = name; X = x; Y = y; }
    }

    internal sealed class PageBookmark
    {
        internal readonly string Title;
        internal readonly int Level;
        internal readonly float Y; // PDF space (bottom-left origin)
        internal PageBookmark(string title, int level, float y) { Title = title; Level = level; Y = y; }
    }

    internal sealed class PageData
    {
        internal readonly float Width, Height;
        internal readonly List<PageAnnotation> Annotations = new List<PageAnnotation>();
        internal readonly List<PageDestination> Destinations = new List<PageDestination>();
        internal readonly List<PageBookmark> Bookmarks = new List<PageBookmark>();

        // The content stream is text while the page is drawn on. Once the next page is added it is
        // kept compressed, and its builder (already grown to a page's size) goes to the next page.
        // Drawing on a page again after that reopens it from the compressed bytes.
        private StringBuilder? _content;
        private byte[]? _compressed;

        internal PageData(float width, float height, StringBuilder? content = null)
        {
            Width = width; Height = height;
            _content = content ?? PdfSerializer.RentContentBuilder();
        }

        internal StringBuilder Content
        {
            get
            {
                if (_content == null)
                {
                    _content = new StringBuilder(PdfSerializer.Decompress(_compressed!));
                    _compressed = null;
                }
                return _content;
            }
        }

        /// <summary>Compresses the content and returns the emptied builder for reuse.</summary>
        internal StringBuilder? Seal()
        {
            if (_content == null)
                return null;
            _compressed = PdfSerializer.Compress(_content);
            var sb = _content;
            _content = null;
            sb.Clear();
            return sb;
        }

        internal byte[] CompressedContent() => _compressed ?? PdfSerializer.Compress(_content!);
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

        // ExtGState entries for opacity; key = "fillAlpha:strokeAlpha"
        private readonly List<(string name, float fillAlpha, float strokeAlpha)> _extGStates =
            new List<(string, float, float)>();
        private readonly Dictionary<string, string> _extGStateMap =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private string _author  = "";
        private string _title   = "";
        private string _subject = "";
        private string _creator = "Majorsilence.Pdf";

        private PdfVersion         _version     = PdfVersion.Pdf14;
        private PdfConformance     _conformance = PdfConformance.None;
        private IStreamEncryptor?  _encryptor;
        private ISignatureHandler? _sigHandler;

        // ── configuration ─────────────────────────────────────────────────────

        internal void SetMetadata(string author, string title, string subject, string creator)
        { _author = author; _title = title; _subject = subject; _creator = creator; }

        internal void SetVersion(PdfVersion version) => _version = version;
        internal void SetConformance(PdfConformance conformance) => _conformance = conformance;
        internal void SetEncryptor(IStreamEncryptor? enc) => _encryptor = enc;
        internal void SetSignatureHandler(ISignatureHandler? sig) => _sigHandler = sig;

        // ── page management ──────────────────────────────────────────────────

        internal PageData CreatePage(float width, float height)
        {
            StringBuilder? reuse = _pages.Count > 0 ? _pages[_pages.Count - 1].Seal() : null;
            var p = new PageData(width, height, reuse);
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
            return GetOrAddImage(rawOrJpeg, width, height, isJpeg, 3);
        }

        internal ImageResource GetOrAddImage(byte[] rawOrJpeg, int width, int height, bool isJpeg, int components)
        {
            // Dimensions and length alone would treat two different same-sized images as one, so
            // the key includes a hash of the content.
            string key = width + "x" + height + ":" + rawOrJpeg.Length + ":" + ContentHash(rawOrJpeg) + ":c" + components;
            if (_imageMap.TryGetValue(key, out var ir)) return ir;
            string pdfName = "Im" + (_images.Count + 1);
            ir = new ImageResource(pdfName, rawOrJpeg, width, height, isJpeg, isRgba: false, components);
            _images.Add(ir); _imageMap[key] = ir;
            return ir;
        }

        private static string ContentHash(byte[] data)
        {
            ulong h = 14695981039346656037UL; // FNV-1a
            foreach (byte b in data) { h ^= b; h *= 1099511628211UL; }
            return h.ToString("x");
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

        // ── ExtGState management (opacity) ────────────────────────────────────

        internal string GetOrAddExtGState(float fillAlpha, float strokeAlpha)
        {
            string key = fillAlpha.ToString("F4") + ":" + strokeAlpha.ToString("F4");
            if (_extGStateMap.TryGetValue(key, out var name)) return name;
            name = "GS" + (_extGStates.Count + 1);
            _extGStates.Add((name, fillAlpha, strokeAlpha));
            _extGStateMap[key] = name;
            return name;
        }

        // ── serialization ────────────────────────────────────────────────────

        // One emptied content builder kept between documents: after the first page it holds a single
        // array as big as a page's content, which would otherwise be allocated (on the large object heap
        // for a busy page) by every document. One slot, so at most one builder is kept.
        private static StringBuilder? s_spareContent;
        private const int MaxSpareContentChars = 1 << 20;

        internal static StringBuilder RentContentBuilder() =>
            System.Threading.Interlocked.Exchange(ref s_spareContent, null) ?? new StringBuilder();

        private static void ReturnContentBuilder(StringBuilder? sb)
        {
            if (sb != null && sb.Capacity <= MaxSpareContentChars)
                System.Threading.Volatile.Write(ref s_spareContent, sb);
        }

        internal void Write(Stream stream)
        {
            // Compress the page still being drawn on, so its builder can serve the next document. Drawing
            // on it after saving reopens it, as for any earlier page.
            if (_pages.Count > 0)
                ReturnContentBuilder(_pages[_pages.Count - 1].Seal());

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

                stream.Write(ms.GetBuffer(), 0, (int)ms.Length);   // one write: a MemoryStream destination grows once
            }
        }

        private void WriteToMemory(MemoryStream ms, byte[] fileId,
            ref byte[]? sigPlaceholderBytes, ref long sigBodyOffset)
        {
            var  w            = new PdfWriter(ms);
            bool isPdf20      = _version == PdfVersion.Pdf20;
            bool isPdfA       = _conformance != PdfConformance.None;
            bool hasEnc       = _encryptor != null;
            bool hasSig       = _sigHandler != null;

            // PDF/A must not be encrypted (standard prohibits it).
            if (isPdfA && hasEnc)
                throw new InvalidOperationException(
                    "PDF/A documents cannot be encrypted. Remove WithSecurity() or set conformance to None.");

            string versionStr = _version switch
            {
                PdfVersion.Pdf20 => "%PDF-2.0",
                PdfVersion.Pdf17 => "%PDF-1.7",
                _                => "%PDF-1.4",
            };
            w.WriteLine(versionStr);
            w.WriteRaw(new byte[] { (byte)'%', 0xE2, 0xE3, 0xCF, 0xD3, (byte)'\n' });

            // Object numbering: list index i → PDF object number i+1.
            var objects = new List<PdfObj> { default, default }; // [0]=Catalog [1]=Pages

            // ── /Encrypt object ────────────────────────────────────────────────
            int encObjNum = -1;
            if (hasEnc)
            {
                encObjNum = objects.Count + 1;
                objects.Add(_encryptor!.BuildEncryptDict());
            }

            // ── XMP metadata (PDF 2.0 and PDF/A, never encrypted per spec) ──────
            int xmpObjNum = -1;
            if (isPdf20 || isPdfA)
            {
                xmpObjNum = objects.Count + 1;
                objects.Add(BuildXmpMetadataObj(_conformance));
            }

            // ── OutputIntents for PDF/A (sRGB ICC profile) ────────────────────
            int outputIntentsObjNum = -1;
            if (isPdfA)
            {
                int iccObjNum = objects.Count + 1;
                objects.Add(BuildIccProfileObj(iccObjNum));
                outputIntentsObjNum = objects.Count + 1;
                objects.Add(BuildOutputIntentsObj(outputIntentsObjNum, iccObjNum));
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

            // ExtGState inline dicts (for opacity)
            var extGStateStr = new StringBuilder();
            foreach (var (gsName, fa, sa) in _extGStates)
                extGStateStr.Append($"/{gsName} << /Type /ExtGState /ca {Fmt(fa)} /CA {Fmt(sa)} >> ");

            // ── page content streams + page dicts ─────────────────────────────
            var pageContentIdx = new int[_pages.Count];
            var pageDictIdx    = new int[_pages.Count];

            // Page object numbers are needed up front: internal links and bookmarks may point at
            // pages that have not been written yet. Each page is content + dict + its annotations.
            var pageObjNums = new int[_pages.Count];
            for (int pi = 0, run = objects.Count; pi < _pages.Count; pi++)
            {
                pageObjNums[pi] = run + 2;
                run += 2 + _pages[pi].Annotations.Count;
            }
            var namedDests = new Dictionary<string, (int Page, float X, float Y)>(StringComparer.Ordinal);
            for (int pi = 0; pi < _pages.Count; pi++)
                foreach (var d in _pages[pi].Destinations)
                    if (!namedDests.ContainsKey(d.Name)) namedDests[d.Name] = (pi, d.X, d.Y);

            for (int pi = 0; pi < _pages.Count; pi++)
            {
                var page = _pages[pi];

                pageContentIdx[pi] = objects.Count;
                int contentObjNum  = objects.Count + 1;
                byte[] compressed  = page.CompressedContent();
                byte[] streamData  = _encryptor != null
                    ? _encryptor.EncryptStream(contentObjNum, 0, compressed)
                    : compressed;
                string hdrStr = $"<< /Filter /FlateDecode /Length {streamData.Length} >>\nstream\n";
                objects.Add(new PdfObj(Latin1.GetBytes(hdrStr), streamData, EndStream));

                pageDictIdx[pi]   = objects.Count;
                string extGStatePart = extGStateStr.Length > 0
                    ? $" /ExtGState << {extGStateStr} >>" : "";
                string pageDict =
                    $"<< /Type /Page /Parent 2 0 R\n" +
                    $"   /MediaBox [0 0 {Fmt(page.Width)} {Fmt(page.Height)}]\n" +
                    $"   /Contents {contentObjNum} 0 R\n" +
                    $"   /Resources << /Font << {fontResEntries} >> /XObject << {imageResEntries} >>{extGStatePart} >>\n";

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
                    objects.Add(BuildAnnotationObj(ann, objects.Count + 1, pageObjNums, namedDests));
            }

            // ── info dict ─────────────────────────────────────────────────────
            int infoObjIdx = objects.Count;
            objects.Add(BuildInfoObj(objects.Count + 1));

            // ── bookmarks (outlines) ──────────────────────────────────────────
            int outlinesObjNum = BuildOutlines(objects, pageObjNums);

            // ── signature placeholder ─────────────────────────────────────────
            int sigObjIdx       = -1;
            int sigWidgetObjNum = -1;
            if (hasSig)
            {
                sigObjIdx   = objects.Count;
                int sObjNum = objects.Count + 1;
                var spb     = _sigHandler!.BuildPlaceholder(sObjNum, _encryptor);
                sigPlaceholderBytes = spb;
                objects.Add(spb);

                int pageObjNum = _pages.Count > 0 ? pageDictIdx[0] + 1 : 1;
                var appearance = _sigHandler!.VisibleAppearance;
                if (appearance.HasValue)
                {
                    int apObjNum = objects.Count + 1;
                    objects.Add(BuildSigAppearanceObj(apObjNum, appearance.Value));
                    sigWidgetObjNum = objects.Count + 1;
                    objects.Add(BuildSigWidgetObjVisible(sigWidgetObjNum, sObjNum, pageObjNum,
                        apObjNum, appearance.Value));
                }
                else
                {
                    sigWidgetObjNum = objects.Count + 1;
                    objects.Add(BuildSigWidgetObj(sigWidgetObjNum, sObjNum, pageObjNum));
                }
            }

            // ── catalog ───────────────────────────────────────────────────────
            string catalog = "<< /Type /Catalog /Pages 2 0 R";
            if (xmpObjNum > 0) catalog += $" /Metadata {xmpObjNum} 0 R";
            if (outputIntentsObjNum > 0) catalog += $" /OutputIntents [{outputIntentsObjNum} 0 R]";
            if (outlinesObjNum > 0) catalog += $" /Outlines {outlinesObjNum} 0 R /PageMode /UseOutlines";
            if (namedDests.Count > 0)
            {
                var dests = new StringBuilder(" /Dests << ");
                foreach (var kv in namedDests)
                    dests.Append($"/{PdfName(kv.Key)} [{pageObjNums[kv.Value.Page]} 0 R /XYZ {Fmt(kv.Value.X)} {Fmt(kv.Value.Y)} null] ");
                dests.Append(">>");
                catalog += dests.ToString();
            }
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
            // Every object's size is known now: size the buffer once instead of doubling it.
            long expected = ms.Position + 1024;
            foreach (var o in objects)
                expected += o.Length + 48;     // "n 0 obj", "endobj" and the xref line
            if (expected < int.MaxValue && ms.Capacity < expected)
                ms.Capacity = (int)expected;
            for (int i = 0; i < totalObjs; i++)
            {
                xrefOffsets[i] = ms.Position;
                w.WriteLine($"{i + 1} 0 obj");
                if (i == sigObjIdx) sigBodyOffset = ms.Position;
                objects[i].WriteTo(ms);
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

        private void BuildTtfFontObjs(FontResource font, List<PdfObj> objects)
        {
            var    ttf       = font.Ttf!;
            string psName    = ttf.PostScriptName;
            int    baseCount = objects.Count;

            int toUnicodeObjNum  = baseCount + 1;
            int descriptorObjNum = baseCount + 2;
            int fontFileObjNum   = baseCount + 3;
            int cidFontObjNum    = baseCount + 4;

            var used = font.UsedGlyphs;

            // The finished, unencrypted pieces depend only on the font and the glyphs used, so they are shared by
            // documents. With glyphs in use, the printable ASCII glyphs are always embedded too, so that
            // documents mostly ask for the same subset. A font nothing was drawn with is embedded whole.
            FontParts parts;
            if (used != null && used.Count > 0)
            {
                var glyphs = FontPartsCache.WithBaseGlyphs(ttf, used);
                parts = FontPartsCache.GetOrBuild(ttf, glyphs, () => BuildFontParts(ttf, glyphs, true));
            }
            else
            {
                parts = BuildFontParts(ttf, used, false);
            }

            // ── ToUnicode CMap ────────────────────────────────────────────────
            if (_encryptor == null)
            {
                objects.Add(parts.ToUnicodeObject);     // shared; an object's bytes are only ever read
            }
            else
            {
                byte[] cmapData = _encryptor.EncryptStream(toUnicodeObjNum, 0, parts.ToUnicodeCompressed);
                string cmapHdr  = $"<< /Filter /FlateDecode /Length {cmapData.Length} >>\nstream\n";
                objects.Add(new PdfObj(Latin1.GetBytes(cmapHdr), cmapData, EndStream));
            }

            string wArray = parts.Widths;

            float scale = 1000f / ttf.UnitsPerEm;
            int llx = (int)Math.Round(ttf.XMin * scale), lly = (int)Math.Round(ttf.YMin * scale);
            int urx = (int)Math.Round(ttf.XMax * scale), ury = (int)Math.Round(ttf.YMax * scale);
            int asc = (int)Math.Round(ttf.Ascender * scale), desc = (int)Math.Round(ttf.Descender * scale);

            // ── FontDescriptor ────────────────────────────────────────────────
            objects.Add(Latin1.GetBytes(
                $"<< /Type /FontDescriptor /FontName /{psName} /Flags 32 " +
                $"/FontBBox [{llx} {lly} {urx} {ury}] " +
                $"/ItalicAngle 0 /Ascent {asc} /Descent {desc} " +
                $"/CapHeight {asc} /StemV 80 /{(ttf.IsCff ? "FontFile3" : "FontFile2")} {fontFileObjNum} 0 R >>"));

            // ── FontFile2 stream ───────────────────────────────────────────────
            if (_encryptor == null)
            {
                objects.Add(parts.FontFileObject);
            }
            else
            {
                byte[] fontStream = _encryptor.EncryptStream(fontFileObjNum, 0, parts.FontCompressed);
                string ffHdr = ttf.IsCff
                    ? $"<< /Subtype /CIDFontType0C /Filter /FlateDecode /Length {fontStream.Length} >>\nstream\n"
                    : $"<< /Filter /FlateDecode /Length {fontStream.Length} /Length1 {parts.FontLength} >>\nstream\n";
                objects.Add(new PdfObj(Latin1.GetBytes(ffHdr), fontStream, EndStream));
            }

            // ── CIDFont ───────────────────────────────────────────────────────
            objects.Add(Latin1.GetBytes(
                $"<< /Type /Font /Subtype /{(ttf.IsCff ? "CIDFontType0" : "CIDFontType2")} /BaseFont /{psName} " +
                $"/CIDSystemInfo << /Registry {EncLitStr(cidFontObjNum, "Adobe")} /Ordering {EncLitStr(cidFontObjNum, "Identity")} /Supplement 0 >> " +
                $"/FontDescriptor {descriptorObjNum} 0 R /DW 1000 {wArray} >>"));

            // ── Type0 Font ────────────────────────────────────────────────────
            objects.Add(Latin1.GetBytes(
                $"<< /Type /Font /Subtype /Type0 /BaseFont /{psName} " +
                $"/Encoding /Identity-H /DescendantFonts [{cidFontObjNum} 0 R] " +
                $"/ToUnicode {toUnicodeObjNum} 0 R >>"));
        }

        // The ToUnicode CMap, /W array and font program for the glyphs in used (every glyph when null or when
        // subsetting is not wanted), compressed but not encrypted.
        private static FontParts BuildFontParts(TrueTypeFont ttf, HashSet<ushort>? used, bool subset)
        {
            var mapList = new List<(int codePoint, ushort glyphId)>();
            foreach (var m in ttf.GetCharGlyphMappings())
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
            byte[] cmapComp = Compress(Latin1.GetBytes(cmap.ToString()));

            var glyphWidths = new SortedDictionary<ushort, int>();
            foreach (var m in mapList)
            {
                int w1000 = (int)Math.Round(ttf.GetAdvanceWidth(m.glyphId) * 1000.0 / ttf.UnitsPerEm);
                glyphWidths[m.glyphId] = w1000;
            }
            var wArray = new StringBuilder("/W [");
            foreach (var kv in glyphWidths) wArray.Append($" {kv.Key} [{kv.Value}]");
            wArray.Append(" ]");

            // A CFF font is always rewritten, even when every glyph is kept, so that its CIDs are glyph IDs.
            bool subsetting = subset && used != null && used.Count > 0;
            byte[] fontData;
            if (ttf.IsCff) fontData = ttf.SubsetCff(subsetting ? used! : AllGlyphs(ttf));
            else if (subsetting) fontData = ttf.Subset(used!);
            else fontData = ttf.Data;
            byte[] fontComp = Compress(fontData);
            byte[] cmapObject = ConcatBytes(Latin1.GetBytes($"<< /Filter /FlateDecode /Length {cmapComp.Length} >>\nstream\n"),
                cmapComp, EndStream);
            string fontHdr = ttf.IsCff
                ? $"<< /Subtype /CIDFontType0C /Filter /FlateDecode /Length {fontComp.Length} >>\nstream\n"
                : $"<< /Filter /FlateDecode /Length {fontComp.Length} /Length1 {fontData.Length} >>\nstream\n";
            byte[] fontObject = ConcatBytes(Latin1.GetBytes(fontHdr),
                fontComp, EndStream);
            return new FontParts(cmapComp, wArray.ToString(), fontComp, fontData.Length, cmapObject, fontObject);
        }

        private static HashSet<ushort> AllGlyphs(TrueTypeFont ttf)
        {
            var all = new HashSet<ushort>();
            for (int g = 0; g < ttf.NumGlyphs; g++) all.Add((ushort)g);
            return all;
        }

        private PdfObj BuildImageObj(ImageResource img, int objNum)
        {
            string filter = img.IsJpeg ? "/DCTDecode" : "/FlateDecode";
            byte[] raw    = img.IsJpeg ? img.Data : Compress(img.Data);
            byte[] data   = _encryptor != null ? _encryptor.EncryptStream(objNum, 0, raw) : raw;
            string hdr =
                $"<< /Type /XObject /Subtype /Image /Width {img.Width} /Height {img.Height} " +
                $"/ColorSpace {(img.IsJpeg && img.Components == 1 ? "/DeviceGray" : "/DeviceRGB")} /BitsPerComponent 8 /Filter {filter} /Length {data.Length} >>\nstream\n";
            return new PdfObj(Latin1.GetBytes(hdr), data, EndStream);
        }

        private PdfObj BuildRgbaMainImageObj(ImageResource img, int sMaskPdfObjNum, int objNum)
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
            return new PdfObj(Latin1.GetBytes(hdr), data, EndStream);
        }

        private PdfObj BuildAlphaMaskObj(ImageResource img, int objNum)
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
            return new PdfObj(Latin1.GetBytes(hdr), data, EndStream);
        }

        private PdfObj BuildXmpMetadataObj(PdfConformance conformance = PdfConformance.None)
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
            x.Append("    </rdf:Description>\n");

            // PDF/A conformance claim required by ISO 19005.
            if (conformance != PdfConformance.None)
            {
                int part = conformance == PdfConformance.PdfA1b ? 1
                         : conformance == PdfConformance.PdfA2b ? 2
                         :                                        3;
                x.Append("    <rdf:Description rdf:about=\"\"\n");
                x.Append("      xmlns:pdfaid=\"http://www.aiim.org/pdfa/ns/id/\">\n");
                x.Append($"      <pdfaid:part>{part}</pdfaid:part>\n");
                x.Append("      <pdfaid:conformance>B</pdfaid:conformance>\n");
                x.Append("    </rdf:Description>\n");
            }

            x.Append("  </rdf:RDF>\n</x:xmpmeta>\n<?xpacket end=\"w\"?>");

            // XMP is always unencrypted so external tools can read metadata.
            byte[] xmpUtf8 = System.Text.Encoding.UTF8.GetBytes(x.ToString());
            string hdr     = $"<< /Type /Metadata /Subtype /XML /Length {xmpUtf8.Length} >>\nstream\n";
            return new PdfObj(Latin1.GetBytes(hdr), xmpUtf8, EndStream);
        }

        // ── PDF/A ICC profile helpers ─────────────────────────────────────────

        // Minimal sRGB ICC v2.1 display profile used for PDF/A OutputIntents.
        // Values from IEC 61966-2-1 (HP/MS sRGB colour space).
        private static byte[] BuildMinimalSrgbIccProfile()
        {
            // Profile layout: header(128) + tagCount(4) + tagDir(9×12) + data
            const int tagCount = 9;
            const int dataStart = 128 + 4 + tagCount * 12; // = 240

            // Build tag data blobs
            byte[] wtptData  = IccXyzTag(0x0000F6D6, 0x00010000, 0x0000D32D); // D50
            byte[] rXyzData  = IccXyzTag(0x00006FA2, 0x000038F5, 0x00000248);
            byte[] gXyzData  = IccXyzTag(0x000062F2, 0x0000B7CB, 0x000018CB);
            byte[] bXyzData  = IccXyzTag(0x000024A0, 0x00000F75, 0x0000B7C9);
            byte[] gammaData = new byte[] {
                0x63,0x75,0x72,0x76, 0,0,0,0, 0,0,0,1, 0x02,0x33  // 'curv' + reserved + count=1 + 2.20 u8.8
            };
            byte[] descData  = IccDescTag("sRGB IEC61966-2.1");
            byte[] cprtData  = IccTextTag("Public Domain");

            // TRC tags share gammaData so they all point to the same offset
            int wtptOff  = dataStart;
            int rXyzOff  = wtptOff  + wtptData.Length;
            int gXyzOff  = rXyzOff  + rXyzData.Length;
            int bXyzOff  = gXyzOff  + gXyzData.Length;
            int gammaOff = bXyzOff  + bXyzData.Length;
            int descOff  = gammaOff + gammaData.Length;
            int cprtOff  = descOff  + descData.Length;
            int totalSize = cprtOff + cprtData.Length;

            var buf = new byte[totalSize];

            // Header
            IccWI32(buf, 0, totalSize);
            buf[4]=0x6C;buf[5]=0x63;buf[6]=0x6D;buf[7]=0x73;             // 'lcms'
            IccWI32(buf, 8,  0x02100000);                                  // version 2.1.0
            buf[12]=0x6D;buf[13]=0x6E;buf[14]=0x74;buf[15]=0x72;          // 'mntr'
            buf[16]=0x52;buf[17]=0x47;buf[18]=0x42;buf[19]=0x20;          // 'RGB '
            buf[20]=0x58;buf[21]=0x59;buf[22]=0x5A;buf[23]=0x20;          // 'XYZ '
            IccWI16(buf, 24, 2026); IccWI16(buf, 26, 1); IccWI16(buf, 28, 1); // date
            buf[36]=0x61;buf[37]=0x63;buf[38]=0x73;buf[39]=0x70;          // 'acsp'
            IccWI32(buf, 64, 0);                                            // rendering intent: perceptual
            IccWI32(buf, 68, 0x0000F6D6); IccWI32(buf, 72, 0x00010000); IccWI32(buf, 76, 0x0000D32D); // D50

            // Tag count
            IccWI32(buf, 128, tagCount);

            // Tag directory entries
            static void Dir(byte[] b, int off, int a, int b1, int b2, int b3, int dataOff, int dataLen)
            {
                b[off]=(byte)a; b[off+1]=(byte)b1; b[off+2]=(byte)b2; b[off+3]=(byte)b3;
                IccWI32(b, off+4, dataOff); IccWI32(b, off+8, dataLen);
            }
            int d = 132;
            Dir(buf, d,0x77,0x74,0x70,0x74, wtptOff,  wtptData.Length); d+=12; // wtpt
            Dir(buf, d,0x72,0x58,0x59,0x5A, rXyzOff,  rXyzData.Length); d+=12; // rXYZ
            Dir(buf, d,0x67,0x58,0x59,0x5A, gXyzOff,  gXyzData.Length); d+=12; // gXYZ
            Dir(buf, d,0x62,0x58,0x59,0x5A, bXyzOff,  bXyzData.Length); d+=12; // bXYZ
            Dir(buf, d,0x72,0x54,0x52,0x43, gammaOff, gammaData.Length); d+=12; // rTRC
            Dir(buf, d,0x67,0x54,0x52,0x43, gammaOff, gammaData.Length); d+=12; // gTRC (shares data)
            Dir(buf, d,0x62,0x54,0x52,0x43, gammaOff, gammaData.Length); d+=12; // bTRC (shares data)
            Dir(buf, d,0x64,0x65,0x73,0x63, descOff,  descData.Length);  d+=12; // desc
            Dir(buf, d,0x63,0x70,0x72,0x74, cprtOff,  cprtData.Length);         // cprt

            // Tag data
            Buffer.BlockCopy(wtptData,  0, buf, wtptOff,  wtptData.Length);
            Buffer.BlockCopy(rXyzData,  0, buf, rXyzOff,  rXyzData.Length);
            Buffer.BlockCopy(gXyzData,  0, buf, gXyzOff,  gXyzData.Length);
            Buffer.BlockCopy(bXyzData,  0, buf, bXyzOff,  bXyzData.Length);
            Buffer.BlockCopy(gammaData, 0, buf, gammaOff, gammaData.Length);
            Buffer.BlockCopy(descData,  0, buf, descOff,  descData.Length);
            Buffer.BlockCopy(cprtData,  0, buf, cprtOff,  cprtData.Length);

            return buf;
        }

        private static byte[] IccXyzTag(int x, int y, int z)
        {
            // 'XYZ ' type: 4 sig + 4 reserved + 3×s15Fixed16
            var b = new byte[20];
            b[0]=0x58;b[1]=0x59;b[2]=0x5A;b[3]=0x20;
            IccWI32(b, 8, x); IccWI32(b, 12, y); IccWI32(b, 16, z);
            return b;
        }

        private static byte[] IccDescTag(string text)
        {
            // ICC v2 textDescriptionType
            byte[] ascii = Encoding.ASCII.GetBytes(text + "\0");
            int len = 8 + 4 + ascii.Length + 4 + 4 + 2 + 1 + 67;
            var b = new byte[len];
            b[0]=0x64;b[1]=0x65;b[2]=0x73;b[3]=0x63; // 'desc'
            IccWI32(b, 8, ascii.Length);
            Buffer.BlockCopy(ascii, 0, b, 12, ascii.Length);
            // rest zero (Unicode count=0, Mac count=0, padding)
            return b;
        }

        private static byte[] IccTextTag(string text)
        {
            // ICC v2 textType: 'text' + reserved + ASCII
            byte[] ascii = Encoding.ASCII.GetBytes(text + "\0");
            var b = new byte[8 + ascii.Length];
            b[0]=0x74;b[1]=0x65;b[2]=0x78;b[3]=0x74; // 'text'
            Buffer.BlockCopy(ascii, 0, b, 8, ascii.Length);
            return b;
        }

        private static void IccWI32(byte[] buf, int off, int v)
        {
            buf[off]=(byte)(v>>24); buf[off+1]=(byte)(v>>16);
            buf[off+2]=(byte)(v>>8); buf[off+3]=(byte)v;
        }
        private static void IccWI16(byte[] buf, int off, int v)
        { buf[off]=(byte)(v>>8); buf[off+1]=(byte)v; }

        private PdfObj BuildIccProfileObj(int objNum)
        {
            byte[] iccBytes = BuildMinimalSrgbIccProfile();
            string hdr = $"<< /N 3 /Alternate /DeviceRGB /Length {iccBytes.Length} >>\nstream\n";
            return new PdfObj(Latin1.GetBytes(hdr), iccBytes, EndStream);
        }

        private static byte[] BuildOutputIntentsObj(int objNum, int iccObjNum)
        {
            string dict =
                $"<< /Type /OutputIntent /S /GTS_PDFA1\n" +
                $"   /OutputConditionIdentifier (sRGB IEC61966-2.1)\n" +
                $"   /Info (sRGB IEC61966-2.1)\n" +
                $"   /DestOutputProfile {iccObjNum} 0 R\n" +
                $">>";
            return Latin1.GetBytes(dict);
        }

        // Writes the outline tree for all bookmarks, in page order. Returns the root object number,
        // or -1 when there are no bookmarks.
        private int BuildOutlines(List<PdfObj> objects, int[] pageObjNums)
        {
            var flat = new List<(PageBookmark Mark, int Page)>();
            for (int pi = 0; pi < _pages.Count; pi++)
                foreach (var b in _pages[pi].Bookmarks) flat.Add((b, pi));
            if (flat.Count == 0) return -1;

            int rootNum = objects.Count + 1;
            int n = flat.Count;
            var parent = new int[n];
            var first = new int[n]; var last = new int[n];
            var prev = new int[n]; var next = new int[n];
            var count = new int[n];
            for (int k = 0; k < n; k++) { first[k] = last[k] = prev[k] = next[k] = -1; }
            int rootFirst = -1, rootLast = -1;

            // A bookmark can nest at most one level below the previous one.
            var stack = new List<int>(); // item index per depth
            for (int k = 0; k < n; k++)
            {
                int depth = Math.Max(0, Math.Min(flat[k].Mark.Level, stack.Count));
                stack.RemoveRange(depth, stack.Count - depth);
                int par = depth == 0 ? -1 : stack[depth - 1];
                parent[k] = par;
                int sibling = par < 0 ? rootLast : last[par];
                if (sibling >= 0) { prev[k] = sibling; next[sibling] = k; }
                if (par < 0) { if (rootFirst < 0) rootFirst = k; rootLast = k; }
                else { if (first[par] < 0) first[par] = k; last[par] = k; }
                for (int a = par; a >= 0; a = parent[a]) count[a]++;
                stack.Add(k);
            }

            string Ref(int k) => $"{rootNum + 1 + k} 0 R";
            objects.Add(Latin1.GetBytes(
                $"<< /Type /Outlines /First {Ref(rootFirst)} /Last {Ref(rootLast)} /Count {n} >>"));
            for (int k = 0; k < n; k++)
            {
                int objNum = rootNum + 1 + k;
                var (mark, page) = flat[k];
                var sb = new StringBuilder();
                sb.Append($"<< /Title {EncStr(objNum, mark.Title)} ");
                sb.Append(parent[k] < 0 ? $"/Parent {rootNum} 0 R " : $"/Parent {Ref(parent[k])} ");
                if (prev[k] >= 0) sb.Append($"/Prev {Ref(prev[k])} ");
                if (next[k] >= 0) sb.Append($"/Next {Ref(next[k])} ");
                if (first[k] >= 0) sb.Append($"/First {Ref(first[k])} /Last {Ref(last[k])} /Count {count[k]} ");
                sb.Append($"/Dest [{pageObjNums[page]} 0 R /XYZ 0 {Fmt(mark.Y)} null] >>");
                objects.Add(Latin1.GetBytes(sb.ToString()));
            }
            return rootNum;
        }

        // A PDF name for a destination: anything outside [A-Za-z0-9._-] becomes #XX (UTF-8 bytes).
        private static string PdfName(string name)
        {
            var sb = new StringBuilder();
            foreach (byte b in Encoding.UTF8.GetBytes(name))
            {
                char c = (char)b;
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '.' || c == '_' || c == '-')
                    sb.Append(c);
                else
                    sb.Append('#').Append(b.ToString("X2"));
            }
            return sb.ToString();
        }

        private byte[] BuildAnnotationObj(PageAnnotation ann, int objNum, int[] pageObjNums,
            Dictionary<string, (int Page, float X, float Y)> namedDests)
        {
            if (ann.DestName != null)
            {
                if (!namedDests.TryGetValue(ann.DestName, out var dest))
                    throw new InvalidOperationException(
                        $"Internal link refers to destination '{ann.DestName}', which was never added.");
                return Latin1.GetBytes(
                    $"<< /Type /Annot /Subtype /Link " +
                    $"/Rect [{Fmt(ann.X1)} {Fmt(ann.Y1)} {Fmt(ann.X2)} {Fmt(ann.Y2)}] " +
                    $"/Border [0 0 0] /Dest [{pageObjNums[dest.Page]} 0 R /XYZ {Fmt(dest.X)} {Fmt(dest.Y)} null] >>");
            }
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

        // The signature objects encrypt their strings like every other object in an encrypted document
        // (#363): the widget's /T and the appearance stream were written in cleartext.
        private byte[] BuildSigWidgetObj(int objNum, int sigObjNum, int pageObjNum) =>
            Latin1.GetBytes(
                $"<< /Type /Annot /Subtype /Widget /FT /Sig " +
                $"/T {EncLitStr(objNum, "Signature")} /V {sigObjNum} 0 R " +
                $"/Rect [0 0 0 0] /P {pageObjNum} 0 R /F 4 >>");

        private byte[] BuildSigWidgetObjVisible(
            int objNum, int sigObjNum, int pageObjNum, int apObjNum,
            (float X, float Y, float Width, float Height, string? SignerName) app)
        {
            // Convert top-left origin to PDF bottom-left; use A4 height as default.
            const float defaultPageH = 841.89f;
            float x1 = app.X;
            float y1 = defaultPageH - app.Y - app.Height;
            float x2 = app.X + app.Width;
            float y2 = defaultPageH - app.Y;

            return Latin1.GetBytes(
                $"<< /Type /Annot /Subtype /Widget /FT /Sig " +
                $"/T {EncLitStr(objNum, "Signature")} /V {sigObjNum} 0 R " +
                $"/Rect [{Fmt(x1)} {Fmt(y1)} {Fmt(x2)} {Fmt(y2)}] " +
                $"/P {pageObjNum} 0 R /F 4 " +
                $"/AP << /N {apObjNum} 0 R >> >>");
        }

        private PdfObj BuildSigAppearanceObj(
            int objNum,
            (float X, float Y, float Width, float Height, string? SignerName) app)
        {
            float w = app.Width;
            float h = app.Height;
            string name = EscapePdfLiteral(app.SignerName ?? "");

            var content = new StringBuilder();
            content.Append("q\n");
            content.Append("0.8 w\n");
            content.Append($"0 0 {Fmt(w)} {Fmt(h)} re S\n");
            content.Append("BT\n");
            content.Append("/Helv 8 Tf\n");
            content.Append($"2 {Fmt(h - 10)} Td\n");
            content.Append("(Digitally Signed) Tj\n");
            if (!string.IsNullOrEmpty(name))
            {
                content.Append($"0 -10 Td\n");
                content.Append($"({name}) Tj\n");
            }
            content.Append("ET\n");
            content.Append("Q\n");

            byte[] plain       = Latin1.GetBytes(content.ToString());
            byte[] streamBytes = _encryptor != null ? _encryptor.EncryptStream(objNum, 0, plain) : plain;
            string resources =
                "<< /Font << /Helv << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> >> >>";
            string hdr =
                $"<< /Type /XObject /Subtype /Form /FormType 1 " +
                $"/BBox [0 0 {Fmt(w)} {Fmt(h)}] " +
                $"/Resources {resources} " +
                $"/Length {streamBytes.Length} >>\nstream\n";
            return new PdfObj(Latin1.GetBytes(hdr), streamBytes, EndStream);
        }

        private static string EscapePdfLiteral(string s) =>
            s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

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

        /// <summary>
        /// Appends the text as a PDF hex string of glyph ids ("&lt;00410042&gt;") straight into
        /// <paramref name="sb"/>, without a string per glyph.
        /// </summary>
        internal static void AppendGlyphIds(StringBuilder sb, string text, TrueTypeFont ttf,
            HashSet<ushort>? usedGlyphs = null)
        {
            const string Hex = "0123456789ABCDEF";
            sb.Append('<');
            for (int i = 0; i < text.Length; i++)
            {
                int codePoint;
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                { codePoint = char.ConvertToUtf32(text[i], text[i + 1]); i++; }
                else codePoint = text[i];
                ushort gid = ttf.GetGlyphId(codePoint);
                usedGlyphs?.Add(gid);
                sb.Append(Hex[(gid >> 12) & 0xF]).Append(Hex[(gid >> 8) & 0xF])
                  .Append(Hex[(gid >> 4) & 0xF]).Append(Hex[gid & 0xF]);
            }
            sb.Append('>');
        }

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

        /// <summary>
        /// Compresses a content stream straight from the builder's chunks as Latin-1, so the whole
        /// page is never copied into a string and a byte array first (both are large-object-heap
        /// allocations for a busy page). The bytes are the same as compressing the finished string.
        /// </summary>
        internal static byte[] Compress(StringBuilder content)
        {
#if NET6_0_OR_GREATER
            byte[] buf = System.Buffers.ArrayPool<byte>.Shared.Rent(16 * 1024);
            try
            {
                using (var ms = new MemoryStream())
                {
                    using (var zs = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true))
                    {
                        foreach (ReadOnlyMemory<char> chunk in content.GetChunks())
                        {
                            for (int offset = 0; offset < chunk.Length; offset += buf.Length)
                            {
                                int n = Latin1.GetBytes(chunk.Span.Slice(offset, Math.Min(buf.Length, chunk.Length - offset)), buf);
                                zs.Write(buf, 0, n);
                            }
                        }
                    }
                    return ms.ToArray();
                }
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buf);
            }
#else
            return Compress(Latin1.GetBytes(content.ToString()));
#endif
        }

        /// <summary>Inflates a zlib stream made by <see cref="Compress(StringBuilder)"/> back to its Latin-1 text.</summary>
        internal static string Decompress(byte[] zlib)
        {
            // 2-byte zlib header, raw deflate data, 4-byte Adler-32 trailer
            using (var input = new MemoryStream(zlib, 2, zlib.Length - 6))
            using (var ds = new DeflateStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                ds.CopyTo(output);
                return Latin1.GetString(output.GetBuffer(), 0, (int)output.Length);
            }
        }

        private static byte[] Compress(byte[] data)
        {
#if NET6_0_OR_GREATER
            using (var ms = new MemoryStream())
            {
                using (var zs = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true))
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

        private static byte[] ConcatBytes(byte[] a, byte[] b, byte[] c)
        {
            var result = new byte[a.Length + b.Length + c.Length];
            Buffer.BlockCopy(a, 0, result, 0, a.Length);
            Buffer.BlockCopy(b, 0, result, a.Length, b.Length);
            Buffer.BlockCopy(c, 0, result, a.Length + b.Length, c.Length);
            return result;
        }

        private static readonly byte[] EndStream = Latin1.GetBytes("\nendstream");

        /// <summary>
        /// An object's bytes. A stream object is kept as its dictionary, its (compressed) data and
        /// "endstream" and written in three parts, so the data is not copied into one array first.
        /// </summary>
        private readonly struct PdfObj
        {
            private readonly byte[] _head;
            private readonly byte[]? _body;
            private readonly byte[]? _tail;

            internal PdfObj(byte[] head, byte[]? body = null, byte[]? tail = null)
            {
                _head = head; _body = body; _tail = tail;
            }

            public static implicit operator PdfObj(byte[] bytes) => new PdfObj(bytes);

            internal int Length => _head.Length + (_body?.Length ?? 0) + (_tail?.Length ?? 0);

            internal void WriteTo(Stream s)
            {
                s.Write(_head, 0, _head.Length);
                if (_body != null) s.Write(_body, 0, _body.Length);
                if (_tail != null) s.Write(_tail, 0, _tail.Length);
            }
        }

        // ── inner writer ─────────────────────────────────────────────────────

        private sealed class PdfWriter
        {
            private readonly Stream _s;
            private static readonly Encoding Latin1 = Encoding.GetEncoding("iso-8859-1");
            private byte[] _buf = new byte[128];   // lines are encoded here instead of into a new array each
            internal PdfWriter(Stream s) { _s = s; }
            internal void Write(string s)
            {
                int max = Latin1.GetMaxByteCount(s.Length);
                if (max > _buf.Length)
                    _buf = new byte[Math.Max(max, _buf.Length * 2)];
                int n = Latin1.GetBytes(s, 0, s.Length, _buf, 0);
                _s.Write(_buf, 0, n);
            }
            internal void WriteLine(string s)  { Write(s); _s.WriteByte((byte)'\n'); }
            internal void WriteBytes(byte[] b) => _s.Write(b, 0, b.Length);
            internal void WriteRaw(byte[] b)   => _s.Write(b, 0, b.Length);
        }
    }
}
