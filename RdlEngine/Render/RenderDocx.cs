#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Draw2 = Majorsilence.Forms.Drawing;
using Majorsilence.Forms.Drawing.Imaging;

namespace Majorsilence.Reporting.Rdl
{
    ///<summary>
    /// Renders a report to a Word document (.docx, WordprocessingML / Office Open XML).
    ///
    /// Like the RTF renderer this is a flowing renderer: report items are written one after
    /// another in the order the engine runs them, not at their absolute positions.  Text boxes
    /// become paragraphs, tables and matrixes become Word tables, and images and charts become
    /// inline pictures.  The package is written with System.IO.Compression; there is no Office or
    /// OpenXML SDK dependency.
    ///</summary>
    internal class RenderDocx : IPresent
    {
        // Namespaces declared on the root of document.xml, header and footer parts
        const string RootNamespaces =
            " xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"" +
            " xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"" +
            " xmlns:wp=\"http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing\"" +
            " xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\"" +
            " xmlns:pic=\"http://schemas.openxmlformats.org/drawingml/2006/picture\"";
        const string XmlDeclaration = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>";
        const string RelImage = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/image";
        const string RelHeader = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/header";
        const string RelFooter = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/footer";
        const string RelStyles = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles";

        // While the page header and footer run, Globals!PageNumber and Globals!TotalPages are given
        //   these values; the text that comes back has them swapped for Word PAGE and NUMPAGES fields
        //   so every page shows its own number (the engine only runs the header and footer once).
        const int PageNumberSentinel = 987654301;
        const int TotalPagesSentinel = 987654302;
        static readonly string PageNumberText = PageNumberSentinel.ToString(CultureInfo.InvariantCulture);
        static readonly string TotalPagesText = TotalPagesSentinel.ToString(CultureInfo.InvariantCulture);

        // A tiny empty paragraph: used where Word needs a paragraph (between two tables, at the
        //   end of a table cell) but nothing should show
        const string EmptyParagraphProps =
            "<w:spacing w:before=\"0\" w:after=\"0\" w:line=\"20\" w:lineRule=\"exact\"/><w:rPr><w:sz w:val=\"2\"/><w:szCs w:val=\"2\"/></w:rPr>";

        static readonly Regex HtmlBreak = new Regex(@"<\s*(br|/p|/div|/li|/tr|/h[1-6])\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex HtmlTag = new Regex(@"<[^>]*>", RegexOptions.Compiled);

        /// <summary>
        /// One XML part being written: document.xml, a header or a footer.  Images are related
        /// from the part they appear in, so each part keeps its own relationships.
        /// </summary>
        class Part
        {
            internal readonly StringBuilder Body = new StringBuilder();
            internal readonly List<(string Id, string Type, string Target)> Rels = new List<(string, string, string)>();
            internal readonly Dictionary<string, string> MediaRelIds = new Dictionary<string, string>();
            internal bool LastWasParagraph;     // Word wants a cell (and a header) to end with a paragraph
            internal bool LastWasTable;         // two tables in a row need a paragraph between them, or Word joins them
        }

        /// <summary>Work state for a table that is being written</summary>
        class TableState
        {
            internal Table Table;
            internal bool[] VisibleColumns;
            internal int[] ColumnWidths;        // twips
            internal string Open;               // <w:tbl> through </w:tblGrid>; written again when the table is split for a page break
            internal int HeaderStart = -1;      // where the header rows start in the part's text
            internal string HeaderRows;         // header rows that repeat after a page break split
            internal bool RowHidden;            // the current row is hidden
            internal bool BodyLevel;            // the table is not inside a table cell
        }

        /// <summary>Work state for a matrix that is being written</summary>
        class MatrixState
        {
            internal int[] ColumnWidths;        // twips
            internal int HeaderRows;
            internal int SkipCols;              // same bookkeeping RenderHtml uses for spanned matrix cells
        }

        Report r;                   // report
        IStreamGen _sg;             // stream generater
        Part _document = new Part();
        Part _header;               // page header; null when the report has none
        Part _footer;               // page footer; null when the report has none
        Part _current;              // the part being written
        bool _headerOnFirstPage = true;
        bool _footerOnFirstPage = true;
        float _headerHeight;        // points
        float _footerHeight;        // points

        // Media files: the package path of each distinct image, keyed by a hash of its bytes
        readonly Dictionary<string, string> _mediaByHash = new Dictionary<string, string>();
        readonly List<(string Path, byte[] Data)> _media = new List<(string, byte[])>();
        int _relCounter;
        int _drawingId;

        int _cellDepth;             // > 0 while writing inside a table or matrix cell
        int _suppress;              // > 0 while writing a hidden table row; the output is thrown away
        readonly StringBuilder _discard = new StringBuilder();
        readonly Stack<TableState> _tables = new Stack<TableState>();
        readonly Stack<MatrixState> _matrixes = new Stack<MatrixState>();
        readonly Dictionary<Grouping, int> _groupInstances = new Dictionary<Grouping, int>();

        bool _pendingPageBreak;     // a page break waits for the next body content
        bool _pendingFromGroup;     //   and it was asked for by a group (cleared when the grouping ends)
        bool _contentSinceBreak;    // something has been written to the body since the last page break

        Draw2.Bitmap _bm = null;    // bm and
        Draw2.Graphics _g = null;   //		  g are needed when converting pixels to points

        public RenderDocx(Report rep, IStreamGen sg)
        {
            r = rep;
            _sg = sg;
            _current = _document;
        }

        public void Dispose()
        {
            // These should already be cleaned up; but in case of an unexpected error
            //   these still need to be disposed of
            if (_bm != null)
                _bm.Dispose();
            if (_g != null)
                _g.Dispose();
        }

        public Report Report()
        {
            return r;
        }

        public bool IsPagingNeeded()
        {
            return false;
        }

        public void Start()
        {
        }

        private Draw2.Graphics GetGraphics
        {
            get
            {
                if (_g == null)
                {
                    _bm = new Draw2.Bitmap(10, 10);
                    _g = Draw2.Graphics.FromImage(_bm);
                }
                return _g;
            }
        }

        /// <summary>Where output goes right now; a hidden table row writes to a throw away buffer</summary>
        private StringBuilder Out
        {
            get { return _suppress > 0 ? _discard : _current.Body; }
        }

        private bool InBody
        {
            get { return _current == _document; }
        }

        public async Task End()
        {
            // the document needs a paragraph before the section properties if it ended with a table
            if (!_document.LastWasParagraph)
            {
                _current = _document;
                WriteEmptyParagraph();
            }

            Stream os = _sg.GetStream();
            using (ZipArchive zip = new ZipArchive(os, ZipArchiveMode.Create, true))
            {
                // header and footer parts, with the relationships document.xml uses to find them
                string headerId = null, footerId = null, emptyHeaderId = null, emptyFooterId = null;
                if (_header != null)
                {
                    headerId = NextRelId();
                    _document.Rels.Add((headerId, RelHeader, "header1.xml"));
                    WritePart(zip, "word/header1.xml", "hdr", _header);
                    if (!_headerOnFirstPage)
                    {
                        emptyHeaderId = NextRelId();
                        _document.Rels.Add((emptyHeaderId, RelHeader, "header2.xml"));
                        WritePart(zip, "word/header2.xml", "hdr", new Part());
                    }
                }
                if (_footer != null)
                {
                    footerId = NextRelId();
                    _document.Rels.Add((footerId, RelFooter, "footer1.xml"));
                    WritePart(zip, "word/footer1.xml", "ftr", _footer);
                    if (!_footerOnFirstPage)
                    {
                        emptyFooterId = NextRelId();
                        _document.Rels.Add((emptyFooterId, RelFooter, "footer2.xml"));
                        WritePart(zip, "word/footer2.xml", "ftr", new Part());
                    }
                }

                _document.Rels.Add(("rIdStyles", RelStyles, "styles.xml"));
                _document.Body.Append(SectionProperties(headerId, footerId, emptyHeaderId, emptyFooterId));
                WritePart(zip, "word/document.xml", "document", _document);

                WriteEntry(zip, "word/styles.xml", StylesXml());
                foreach (var m in _media)
                {
                    ZipArchiveEntry e = zip.CreateEntry(m.Path, CompressionLevel.Fastest);
                    using (Stream es = e.Open())
                        await es.WriteAsync(m.Data, 0, m.Data.Length).ConfigureAwait(false);
                }
                WriteEntry(zip, "docProps/core.xml", CoreXml());
                WriteEntry(zip, "docProps/app.xml", XmlDeclaration +
                    "<Properties xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/extended-properties\"><Application>Majorsilence Reporting</Application></Properties>");
                WriteEntry(zip, "_rels/.rels", XmlDeclaration +
                    "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/>" +
                    "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties\" Target=\"docProps/core.xml\"/>" +
                    "<Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties\" Target=\"docProps/app.xml\"/>" +
                    "</Relationships>");
                WriteEntry(zip, "[Content_Types].xml", ContentTypesXml(emptyHeaderId != null, emptyFooterId != null));
            }

            if (_g != null)
            {
                _g.Dispose();
                _g = null;
            }
            if (_bm != null)
            {
                _bm.Dispose();
                _bm = null;
            }
        }

        private string NextRelId()
        {
            return "rIdR" + (++_relCounter).ToString(CultureInfo.InvariantCulture);
        }

        private static void WriteEntry(ZipArchive zip, string path, string text)
        {
            ZipArchiveEntry e = zip.CreateEntry(path, CompressionLevel.Fastest);
            using (StreamWriter sw = new StreamWriter(e.Open(), new UTF8Encoding(false)))
                sw.Write(text);
        }

        /// <summary>
        /// Write a document, header or footer part plus its relationships part when it has any.
        /// </summary>
        private static void WritePart(ZipArchive zip, string path, string rootName, Part part)
        {
            ZipArchiveEntry e = zip.CreateEntry(path, CompressionLevel.Fastest);
            using (StreamWriter sw = new StreamWriter(e.Open(), new UTF8Encoding(false)))
            {
                sw.Write(XmlDeclaration);
                sw.Write("<w:" + rootName + RootNamespaces + ">");
                if (rootName == "document")
                    sw.Write("<w:body>");
                foreach (ReadOnlyMemory<char> chunk in part.Body.GetChunks())   // not copied into one (large object heap) string first
                    sw.Write(chunk.Span);
                // a header or footer must hold at least one paragraph, and end with one
                if (rootName != "document" && !part.LastWasParagraph)
                    sw.Write("<w:p/>");
                if (rootName == "document")
                    sw.Write("</w:body>");
                sw.Write("</w:" + rootName + ">");
            }

            if (part.Rels.Count == 0)
                return;
            int slash = path.LastIndexOf('/');
            string relsPath = path.Substring(0, slash) + "/_rels/" + path.Substring(slash + 1) + ".rels";
            StringBuilder sb = new StringBuilder(XmlDeclaration);
            sb.Append("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
            foreach (var rel in part.Rels)
                sb.AppendFormat("<Relationship Id=\"{0}\" Type=\"{1}\" Target=\"{2}\"/>", rel.Id, rel.Type, XmlEscape(rel.Target));
            sb.Append("</Relationships>");
            WriteEntry(zip, relsPath, sb.ToString());
        }

        private string ContentTypesXml(bool emptyHeader, bool emptyFooter)
        {
            StringBuilder sb = new StringBuilder(XmlDeclaration);
            sb.Append("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
            sb.Append("<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
            sb.Append("<Default Extension=\"xml\" ContentType=\"application/xml\"/>");
            sb.Append("<Default Extension=\"png\" ContentType=\"image/png\"/>");
            sb.Append("<Default Extension=\"jpeg\" ContentType=\"image/jpeg\"/>");
            sb.Append("<Default Extension=\"gif\" ContentType=\"image/gif\"/>");
            sb.Append("<Default Extension=\"bmp\" ContentType=\"image/bmp\"/>");
            sb.Append("<Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/>");
            sb.Append("<Override PartName=\"/word/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml\"/>");
            const string headerType = "application/vnd.openxmlformats-officedocument.wordprocessingml.header+xml";
            const string footerType = "application/vnd.openxmlformats-officedocument.wordprocessingml.footer+xml";
            if (_header != null)
                sb.AppendFormat("<Override PartName=\"/word/header1.xml\" ContentType=\"{0}\"/>", headerType);
            if (emptyHeader)
                sb.AppendFormat("<Override PartName=\"/word/header2.xml\" ContentType=\"{0}\"/>", headerType);
            if (_footer != null)
                sb.AppendFormat("<Override PartName=\"/word/footer1.xml\" ContentType=\"{0}\"/>", footerType);
            if (emptyFooter)
                sb.AppendFormat("<Override PartName=\"/word/footer2.xml\" ContentType=\"{0}\"/>", footerType);
            sb.Append("<Override PartName=\"/docProps/core.xml\" ContentType=\"application/vnd.openxmlformats-package.core-properties+xml\"/>");
            sb.Append("<Override PartName=\"/docProps/app.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.extended-properties+xml\"/>");
            sb.Append("</Types>");
            return sb.ToString();
        }

        private static string StylesXml()
        {
            // Paragraphs have no spacing of their own: the report decides where things go
            return XmlDeclaration +
                "<w:styles xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">" +
                "<w:docDefaults>" +
                "<w:rPrDefault><w:rPr><w:rFonts w:ascii=\"Arial\" w:hAnsi=\"Arial\" w:eastAsia=\"Arial\" w:cs=\"Arial\"/><w:sz w:val=\"20\"/><w:szCs w:val=\"20\"/></w:rPr></w:rPrDefault>" +
                "<w:pPrDefault><w:pPr><w:spacing w:before=\"0\" w:after=\"0\" w:line=\"240\" w:lineRule=\"auto\"/></w:pPr></w:pPrDefault>" +
                "</w:docDefaults>" +
                "<w:style w:type=\"paragraph\" w:default=\"1\" w:styleId=\"Normal\"><w:name w:val=\"Normal\"/><w:qFormat/></w:style>" +
                "<w:style w:type=\"table\" w:default=\"1\" w:styleId=\"TableNormal\"><w:name w:val=\"Normal Table\"/>" +
                "<w:tblPr><w:tblInd w:w=\"0\" w:type=\"dxa\"/><w:tblCellMar><w:top w:w=\"0\" w:type=\"dxa\"/><w:left w:w=\"0\" w:type=\"dxa\"/><w:bottom w:w=\"0\" w:type=\"dxa\"/><w:right w:w=\"0\" w:type=\"dxa\"/></w:tblCellMar></w:tblPr></w:style>" +
                "</w:styles>";
        }

        private string CoreXml()
        {
            StringBuilder sb = new StringBuilder(XmlDeclaration);
            sb.Append("<cp:coreProperties xmlns:cp=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\"" +
                " xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:dcterms=\"http://purl.org/dc/terms/\"" +
                " xmlns:dcmitype=\"http://purl.org/dc/dcmitype/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
            if (!string.IsNullOrEmpty(r.Name))
                sb.Append("<dc:title>").Append(XmlEscape(r.Name)).Append("</dc:title>");
            if (!string.IsNullOrEmpty(r.Description))
                sb.Append("<dc:subject>").Append(XmlEscape(r.Description)).Append("</dc:subject>");
            if (!string.IsNullOrEmpty(r.Author))
                sb.Append("<dc:creator>").Append(XmlEscape(r.Author)).Append("</dc:creator>");
            sb.Append("<dcterms:created xsi:type=\"dcterms:W3CDTF\">")
              .Append(DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture))
              .Append("</dcterms:created>");
            sb.Append("</cp:coreProperties>");
            return sb.ToString();
        }

        /// <summary>
        /// Page size, margins, and the header and footer references.  The report's top and bottom
        /// margins are where the page header and footer start, so the body margin in Word also
        /// takes in the header and footer height.
        /// </summary>
        private string SectionProperties(string headerId, string footerId, string emptyHeaderId, string emptyFooterId)
        {
            ReportDefn rd = r.ReportDefinition;
            int top = rd.TopMargin == null ? 0 : rd.TopMargin.Twips;
            int bottom = rd.BottomMargin == null ? 0 : rd.BottomMargin.Twips;
            int left = rd.LeftMargin == null ? 0 : rd.LeftMargin.Twips;
            int right = rd.RightMargin == null ? 0 : rd.RightMargin.Twips;
            int width = RSize.TwipsFromPoints(r.PageWidthPoints);
            int height = RSize.TwipsFromPoints(r.PageHeightPoints);
            bool titlePage = emptyHeaderId != null || emptyFooterId != null;

            StringBuilder sb = new StringBuilder("<w:sectPr>");
            if (headerId != null)
            {
                sb.AppendFormat("<w:headerReference w:type=\"default\" r:id=\"{0}\"/>", headerId);
                if (titlePage)
                    sb.AppendFormat("<w:headerReference w:type=\"first\" r:id=\"{0}\"/>", emptyHeaderId ?? headerId);
            }
            if (footerId != null)
            {
                sb.AppendFormat("<w:footerReference w:type=\"default\" r:id=\"{0}\"/>", footerId);
                if (titlePage)
                    sb.AppendFormat("<w:footerReference w:type=\"first\" r:id=\"{0}\"/>", emptyFooterId ?? footerId);
            }
            sb.AppendFormat(CultureInfo.InvariantCulture, "<w:pgSz w:w=\"{0}\" w:h=\"{1}\"{2}/>",
                width, height, width > height ? " w:orient=\"landscape\"" : "");
            sb.AppendFormat(CultureInfo.InvariantCulture,
                "<w:pgMar w:top=\"{0}\" w:right=\"{1}\" w:bottom=\"{2}\" w:left=\"{3}\" w:header=\"{4}\" w:footer=\"{5}\" w:gutter=\"0\"/>",
                top + (_header != null ? RSize.TwipsFromPoints(_headerHeight) : 0),
                right,
                bottom + (_footer != null ? RSize.TwipsFromPoints(_footerHeight) : 0),
                left, top, bottom);
            if (titlePage)
                sb.Append("<w:titlePg/>");
            sb.Append("</w:sectPr>");
            return sb.ToString();
        }

        // Body: main container for the report
        public void BodyStart(Body b)
        {
            _current = _document;
        }

        public void BodyEnd(Body b)
        {
        }

        public void PageHeaderStart(PageHeader ph)
        {
            _header = new Part();
            _current = _header;
            _headerOnFirstPage = ph.PrintOnFirstPage;
            _headerHeight = ph.Height == null ? 0 : ph.Height.Points;
            r.PageNumber = PageNumberSentinel;
            r.TotalPages = TotalPagesSentinel;
        }

        public void PageHeaderEnd(PageHeader ph)
        {
            r.PageNumber = r.TotalPages = 1;
            _current = _document;
        }

        public void PageFooterStart(PageFooter pf)
        {
            _footer = new Part();
            _current = _footer;
            _footerOnFirstPage = pf.PrintOnFirstPage;
            _footerHeight = pf.Height == null ? 0 : pf.Height.Points;
            r.PageNumber = PageNumberSentinel;
            r.TotalPages = TotalPagesSentinel;
        }

        public void PageFooterEnd(PageFooter pf)
        {
            r.PageNumber = r.TotalPages = 1;
            _current = _document;
        }

        public async Task Textbox(Textbox tb, string t, Row row)
        {
            if (await tb.IsHidden(r, row).ConfigureAwait(false))
                return;         // a table cell still gets its empty paragraph when the cell ends

            if (t == null)
                t = "";
            else if (await tb.IsHtml(r, row).ConfigureAwait(false))
                t = HtmlToText(t);

            if (t.Length == 0 && _cellDepth == 0)
                return;         // nothing to show outside a table

            StyleInfo si = tb.Style == null ? null : await tb.Style.GetStyleInfoShared(r, row).ConfigureAwait(false);   // read only
            WriteTextParagraph(si, t, !InBody);
        }

        /// <summary>
        /// Word has no html; keep the text and the line breaks
        /// </summary>
        private static string HtmlToText(string html)
        {
            string s = HtmlBreak.Replace(html, "\n");
            s = HtmlTag.Replace(s, "");
            s = WebUtility.HtmlDecode(s);
            return s.TrimEnd('\n', '\r');
        }

        /// <summary>
        /// Write one paragraph of text in the style of a report item
        /// </summary>
        private void WriteTextParagraph(StyleInfo si, string text, bool allowFields)
        {
            StringBuilder sb = Out;
            sb.Append("<w:p><w:pPr>");
            if (TakePageBreak())
                sb.Append("<w:pageBreakBefore/>");
            if (si != null && _cellDepth == 0)
            {   // outside a table the paragraph carries the borders and background; in a table the cell does
                string borders = Borders(si, true);
                if (borders.Length > 0)
                    sb.Append("<w:pBdr>").Append(borders).Append("</w:pBdr>");
                string shading = Shading(si.BackgroundColor);
                sb.Append(shading);
            }
            if (si != null)
            {
                switch (si.TextAlign)
                {
                    case TextAlignEnum.Center: sb.Append("<w:jc w:val=\"center\"/>"); break;
                    case TextAlignEnum.Right: sb.Append("<w:jc w:val=\"right\"/>"); break;
                    case TextAlignEnum.Justified: sb.Append("<w:jc w:val=\"both\"/>"); break;
                    default: break;
                }
            }
            sb.Append("</w:pPr>");
            WriteRuns(sb, RunProperties(si), text, allowFields);
            sb.Append("</w:p>");
            MarkParagraph();
        }

        private void MarkParagraph()
        {
            if (_suppress > 0)
                return;
            _current.LastWasParagraph = true;
            _current.LastWasTable = false;
            if (InBody && _cellDepth == 0)
                _contentSinceBreak = true;
        }

        private void MarkTableEnd()
        {
            if (_suppress > 0)
                return;
            _current.LastWasParagraph = false;
            _current.LastWasTable = true;
            if (InBody && _cellDepth == 0)
                _contentSinceBreak = true;
        }

        /// <summary>
        /// Run properties (font, size, bold, italic, underline, colour) for a style
        /// </summary>
        private static string RunProperties(StyleInfo si)
        {
            if (si == null)
                return "";
            StringBuilder sb = new StringBuilder("<w:rPr>");
            string font = XmlEscape(si.FontFamily);
            sb.AppendFormat("<w:rFonts w:ascii=\"{0}\" w:hAnsi=\"{0}\" w:eastAsia=\"{0}\" w:cs=\"{0}\"/>", font);
            if (si.IsFontBold())
                sb.Append("<w:b/><w:bCs/>");
            if (si.FontStyle == FontStyleEnum.Italic)
                sb.Append("<w:i/><w:iCs/>");
            if (si.TextDecoration == TextDecorationEnum.LineThrough)
                sb.Append("<w:strike/>");
            string color = HexColor(si.Color);
            if (color != null)
                sb.AppendFormat("<w:color w:val=\"{0}\"/>", color);
            int size = (int)Math.Round(si.FontSize * 2, 0);        // half points
            if (size > 0)
                sb.AppendFormat(CultureInfo.InvariantCulture, "<w:sz w:val=\"{0}\"/><w:szCs w:val=\"{0}\"/>", size);
            if (si.TextDecoration == TextDecorationEnum.Underline)
                sb.Append("<w:u w:val=\"single\"/>");
            sb.Append("</w:rPr>");
            return sb.ToString();
        }

        /// <summary>
        /// Write the text as runs: new lines become line breaks and tabs become tabs.  In the page
        /// header and footer the page number and total pages become Word fields.
        /// </summary>
        private static void WriteRuns(StringBuilder sb, string rPr, string text, bool allowFields)
        {
            if (text.Length == 0)
                return;
            if (allowFields && (text.Contains(PageNumberText) || text.Contains(TotalPagesText)))
            {
                int pos = 0;
                while (pos < text.Length)
                {
                    int ip = text.IndexOf(PageNumberText, pos, StringComparison.Ordinal);
                    int it = text.IndexOf(TotalPagesText, pos, StringComparison.Ordinal);
                    int next = ip < 0 ? it : (it < 0 ? ip : Math.Min(ip, it));
                    if (next < 0)
                    {
                        WriteRun(sb, rPr, text.Substring(pos));
                        break;
                    }
                    if (next > pos)
                        WriteRun(sb, rPr, text.Substring(pos, next - pos));
                    string instr = next == ip ? " PAGE " : " NUMPAGES ";
                    sb.AppendFormat("<w:fldSimple w:instr=\"{0}\"><w:r>{1}<w:t>1</w:t></w:r></w:fldSimple>", instr, rPr);
                    pos = next + PageNumberText.Length;     // both sentinels are the same length
                }
                return;
            }
            WriteRun(sb, rPr, text);
        }

        private static void WriteRun(StringBuilder sb, string rPr, string text)
        {
            if (text.Length == 0)
                return;
            sb.Append("<w:r>").Append(rPr);
            bool inText = false;
            foreach (char c in text)
            {
                if (c == '\n' || c == '\t')
                {
                    if (inText)
                    {
                        sb.Append("</w:t>");
                        inText = false;
                    }
                    sb.Append(c == '\n' ? "<w:br/>" : "<w:tab/>");
                    continue;
                }
                if (c == '\r' || !IsXmlChar(c))
                    continue;
                if (!inText)
                {
                    sb.Append("<w:t xml:space=\"preserve\">");
                    inText = true;
                }
                switch (c)
                {
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '&': sb.Append("&amp;"); break;
                    default: sb.Append(c); break;
                }
            }
            if (inText)
                sb.Append("</w:t>");
            sb.Append("</w:r>");
        }

        private static bool IsXmlChar(char c)
        {
            // XML 1.0 does not allow most control characters; surrogate pairs pass through as is
            return c >= 0x20 ? (c != 0xFFFE && c != 0xFFFF) : (c == 0x9 || c == 0xA || c == 0xD);
        }

        private static string XmlEscape(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            StringBuilder sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                if (!IsXmlChar(c))
                    continue;
                switch (c)
                {
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '&': sb.Append("&amp;"); break;
                    case '"': sb.Append("&quot;"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// RRGGBB for a colour, or null when there is no colour (empty or transparent)
        /// </summary>
        private static string HexColor(System.Drawing.Color c)
        {
            if (c.IsEmpty || c.A == 0)
                return null;
            return c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }

        private static string Shading(System.Drawing.Color c)
        {
            string fill = HexColor(c);
            return fill == null ? "" : "<w:shd w:val=\"clear\" w:color=\"auto\" w:fill=\"" + fill + "\"/>";
        }

        /// <summary>
        /// Top, left, bottom and right borders in Word's order.  Paragraph borders leave out the
        /// sides that have none; cell borders say "nil" so the cell has no border there.
        /// </summary>
        private static string Borders(StyleInfo si, bool paragraph)
        {
            StringBuilder sb = new StringBuilder();
            Border(sb, "top", si.BStyleTop, si.BWidthTop, si.BColorTop, paragraph);
            Border(sb, "left", si.BStyleLeft, si.BWidthLeft, si.BColorLeft, paragraph);
            Border(sb, "bottom", si.BStyleBottom, si.BWidthBottom, si.BColorBottom, paragraph);
            Border(sb, "right", si.BStyleRight, si.BWidthRight, si.BColorRight, paragraph);
            return sb.ToString();
        }

        private static void Border(StringBuilder sb, string side, BorderStyleEnum style, float width, System.Drawing.Color color, bool paragraph)
        {
            string val = BorderStyle(style);
            if (val == null)
            {
                if (!paragraph)
                    sb.AppendFormat("<w:{0} w:val=\"nil\"/>", side);
                return;
            }
            // border width is in eighths of a point; Word allows 2 to 96
            int sz = (int)Math.Round(width * 8);
            sz = Math.Max(2, Math.Min(96, sz));
            sb.AppendFormat(CultureInfo.InvariantCulture, "<w:{0} w:val=\"{1}\" w:sz=\"{2}\" w:space=\"0\" w:color=\"{3}\"/>",
                side, val, sz, HexColor(color) ?? "000000");
        }

        private static string BorderStyle(BorderStyleEnum style)
        {
            switch (style)
            {
                case BorderStyleEnum.Solid: return "single";
                case BorderStyleEnum.Dashed: return "dashed";
                case BorderStyleEnum.Dotted: return "dotted";
                case BorderStyleEnum.Double: return "double";
                case BorderStyleEnum.Groove: return "threeDEngrave";
                case BorderStyleEnum.Ridge: return "threeDEmboss";
                case BorderStyleEnum.Inset: return "inset";
                case BorderStyleEnum.Outset: return "outset";
                case BorderStyleEnum.WindowInset: return "inset";
                case BorderStyleEnum.None:
                default:
                    return null;
            }
        }

        // ---- Page breaks ----

        /// <summary>
        /// Ask for a page break before the next thing written to the body.  As in the paged
        /// renderers there is no break when nothing has been written since the last one, and none
        /// inside a table cell, page header or page footer.
        /// </summary>
        private void RequestPageBreak(bool fromGroup)
        {
            if (!InBody || _cellDepth > 0 || _suppress > 0 || !_contentSinceBreak)
                return;
            _pendingPageBreak = true;
            _pendingFromGroup = fromGroup;
        }

        /// <summary>
        /// When a page break is waiting and the next paragraph is in the body, take the break:
        /// the caller puts pageBreakBefore on the paragraph.
        /// </summary>
        private bool TakePageBreak()
        {
            if (!_pendingPageBreak || !InBody || _cellDepth > 0 || _suppress > 0)
                return false;
            _pendingPageBreak = _pendingFromGroup = false;
            _contentSinceBreak = false;
            return true;
        }

        /// <summary>
        /// Write a waiting page break as its own (tiny) paragraph; used before a table
        /// </summary>
        private void WritePendingPageBreakParagraph()
        {
            if (!TakePageBreak())
                return;
            Out.Append("<w:p><w:pPr><w:pageBreakBefore/>").Append(EmptyParagraphProps).Append("</w:pPr></w:p>");
            _current.LastWasParagraph = true;
            _current.LastWasTable = false;
        }

        private void WriteEmptyParagraph()
        {
            Out.Append("<w:p><w:pPr>").Append(EmptyParagraphProps).Append("</w:pPr></w:p>");
            MarkParagraph();
        }

        public async Task DataRegionNoRows(DataRegion d, string noRowsMsg)          // no rows in table
        {
            if (string.IsNullOrEmpty(noRowsMsg))
                return;         // in a table cell, the cell still gets its empty paragraph
            StyleInfo si = d.Style == null ? null : await d.Style.GetStyleInfoShared(r, null).ConfigureAwait(false);
            WriteTextParagraph(si, noRowsMsg, false);
        }

        // Lists
        public async Task<bool> ListStart(List l, Row row)
        {
            if (await l.IsHidden(r, row).ConfigureAwait(false))
                return false;
            if (l.PageBreakAtStart)
                RequestPageBreak(false);
            return true;
        }

        public Task ListEnd(List l, Row row)
        {
            if (l.PageBreakAtEnd)
                RequestPageBreak(false);
            return Task.CompletedTask;
        }

        public Task ListEntryBegin(List l, Row r)
        {
            return Task.CompletedTask;
        }

        public void ListEntryEnd(List l, Row r)
        {
        }

        // Tables					// Report item table
        public async Task<bool> TableStart(Table t, Row row)
        {
            if (await t.IsHidden(r, row).ConfigureAwait(false))
                return false;

            if (t.PageBreakAtStart)
                RequestPageBreak(false);
            WritePendingPageBreakParagraph();
            if (_current.LastWasTable)
                WriteEmptyParagraph();      // keep two tables in a row apart

            TableState ts = new TableState();
            ts.Table = t;
            ts.BodyLevel = _cellDepth == 0;
            int count = t.TableColumns == null ? 0 : t.TableColumns.Items.Count;
            ts.VisibleColumns = new bool[count];
            ts.ColumnWidths = new int[count];
            int total = 0;
            for (int ci = 0; ci < count; ci++)
            {
                TableColumn tc = t.TableColumns.Items[ci];
                ts.VisibleColumns[ci] = !await tc.IsHidden(r, row).ConfigureAwait(false);
                ts.ColumnWidths[ci] = tc.Width == null ? 0 : tc.Width.Twips;
                if (ts.VisibleColumns[ci])
                    total += ts.ColumnWidths[ci];
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("<w:tbl><w:tblPr>");
            sb.AppendFormat(CultureInfo.InvariantCulture, "<w:tblW w:w=\"{0}\" w:type=\"dxa\"/>", total);
            sb.Append("<w:tblLayout w:type=\"fixed\"/>");
            sb.Append("<w:tblCellMar><w:top w:w=\"0\" w:type=\"dxa\"/><w:left w:w=\"0\" w:type=\"dxa\"/><w:bottom w:w=\"0\" w:type=\"dxa\"/><w:right w:w=\"0\" w:type=\"dxa\"/></w:tblCellMar>");
            sb.Append("</w:tblPr><w:tblGrid>");
            for (int ci = 0; ci < count; ci++)
            {
                if (ts.VisibleColumns[ci])
                    sb.AppendFormat(CultureInfo.InvariantCulture, "<w:gridCol w:w=\"{0}\"/>", ts.ColumnWidths[ci]);
            }
            sb.Append("</w:tblGrid>");
            ts.Open = sb.ToString();
            Out.Append(ts.Open);
            _tables.Push(ts);
            return true;
        }

        public bool IsTableSortable(Table t)
        {
            return false;   // can't have tableGroups; must have 1 detail row
        }

        public Task TableEnd(Table t, Row row)
        {
            if (_tables.Count > 0)
                _tables.Pop();
            Out.Append("</w:tbl>");
            MarkTableEnd();
            if (t.PageBreakAtEnd)
                RequestPageBreak(false);
            return Task.CompletedTask;
        }

        public void TableBodyStart(Table t, Row row)
        {
        }

        public void TableBodyEnd(Table t, Row row)
        {
        }

        public void TableFooterStart(Footer f, Row row)
        {
        }

        public void TableFooterEnd(Footer f, Row row)
        {
        }

        public void TableHeaderStart(Header h, Row row)
        {
            // remember where the header rows start so they can be repeated if the table is split
            if (_tables.Count > 0 && _suppress == 0)
                _tables.Peek().HeaderStart = _current.Body.Length;
        }

        public void TableHeaderEnd(Header h, Row row)
        {
            if (_tables.Count == 0 || _suppress > 0)
                return;
            TableState ts = _tables.Peek();
            if (h.RepeatOnNewPage && ts.HeaderStart >= 0)
                ts.HeaderRows = _current.Body.ToString(ts.HeaderStart, _current.Body.Length - ts.HeaderStart);
            ts.HeaderStart = -1;
        }

        public async Task TableRowStart(TableRow tr, Row row)
        {
            TableState ts = _tables.Count > 0 ? _tables.Peek() : null;
            if (ts == null)
                return;

            // rows hidden by their group or detail visibility are left out
            ReportLink rl = tr.Parent.Parent;
            Visibility v = null;
            Header head = rl as Header;
            if (rl is Details)
                v = ((Details)rl).Visibility;
            else if (rl.Parent is TableGroup)
                v = ((TableGroup)rl.Parent).Visibility;
            ts.RowHidden = v != null && await v.IsHidden(r, row).ConfigureAwait(false);
            if (ts.RowHidden)
            {
                _suppress++;
                return;
            }

            // a group page break splits the table: Word can't break a page between two rows
            if (_pendingPageBreak && ts.BodyLevel && InBody && _cellDepth == 0 && _suppress == 0)
            {
                Out.Append("</w:tbl>");
                WritePendingPageBreakParagraph();
                Out.Append(ts.Open);
                if (ts.HeaderRows != null)
                    Out.Append(ts.HeaderRows);
            }

            StringBuilder sb = Out;
            sb.Append("<w:tr>");
            bool repeat = head != null && head.RepeatOnNewPage;
            if (tr.Height != null || repeat)
            {
                sb.Append("<w:trPr>");
                if (tr.Height != null)
                    sb.AppendFormat(CultureInfo.InvariantCulture, "<w:trHeight w:val=\"{0}\" w:hRule=\"atLeast\"/>", tr.Height.Twips);
                if (repeat)
                    sb.Append("<w:tblHeader/>");     // repeat table header on multiple pages
                sb.Append("</w:trPr>");
            }
            if (ts.BodyLevel && InBody)
                _contentSinceBreak = true;      // a page break between groups now has something before it
        }

        public void TableRowEnd(TableRow tr, Row row)
        {
            TableState ts = _tables.Count > 0 ? _tables.Peek() : null;
            if (ts == null)
                return;
            if (ts.RowHidden)
            {
                ts.RowHidden = false;
                _suppress--;
                if (_suppress == 0)
                    _discard.Clear();
                return;
            }
            Out.Append("</w:tr>");
        }

        public async Task TableCellStart(TableCell t, Row row)
        {
            TableState ts = _tables.Count > 0 ? _tables.Peek() : null;
            int width = 0, span = 0;
            for (int ci = t.ColIndex; ci < t.ColIndex + t.ColSpan; ci++)
            {
                if (ts != null && ci < ts.VisibleColumns.Length && ts.VisibleColumns[ci])
                {
                    width += ts.ColumnWidths[ci];
                    span++;
                }
            }
            ReportItem ri = t.ReportItems.Items[0];
            StyleInfo si = ri.Style == null ? null : await ri.Style.GetStyleInfoShared(r, row).ConfigureAwait(false);   // read only
            WriteCellStart(width, Math.Max(1, span), si);
        }

        /// <summary>
        /// Start a table cell: width, merged columns, borders, background, padding, vertical alignment
        /// </summary>
        private void WriteCellStart(int width, int span, StyleInfo si)
        {
            StringBuilder sb = Out;
            sb.Append("<w:tc><w:tcPr>");
            sb.AppendFormat(CultureInfo.InvariantCulture, "<w:tcW w:w=\"{0}\" w:type=\"dxa\"/>", width);
            if (span > 1)
                sb.AppendFormat(CultureInfo.InvariantCulture, "<w:gridSpan w:val=\"{0}\"/>", span);
            if (si != null)
            {
                sb.Append("<w:tcBorders>").Append(Borders(si, false)).Append("</w:tcBorders>");
                sb.Append(Shading(si.BackgroundColor));
                sb.AppendFormat(CultureInfo.InvariantCulture,
                    "<w:tcMar><w:top w:w=\"{0}\" w:type=\"dxa\"/><w:left w:w=\"{1}\" w:type=\"dxa\"/><w:bottom w:w=\"{2}\" w:type=\"dxa\"/><w:right w:w=\"{3}\" w:type=\"dxa\"/></w:tcMar>",
                    RSize.TwipsFromPoints(si.PaddingTop), RSize.TwipsFromPoints(si.PaddingLeft),
                    RSize.TwipsFromPoints(si.PaddingBottom), RSize.TwipsFromPoints(si.PaddingRight));
                if (si.VerticalAlign == VerticalAlignEnum.Middle)
                    sb.Append("<w:vAlign w:val=\"center\"/>");
                else if (si.VerticalAlign == VerticalAlignEnum.Bottom)
                    sb.Append("<w:vAlign w:val=\"bottom\"/>");
            }
            sb.Append("</w:tcPr>");
            _cellDepth++;
            if (_suppress == 0)
                _current.LastWasParagraph = _current.LastWasTable = false;
        }

        private void WriteCellEnd()
        {
            // a cell must end with a paragraph (it may hold nothing, or end with a nested table)
            if (!_current.LastWasParagraph || _suppress > 0)
                Out.Append("<w:p/>");
            Out.Append("</w:tc>");
            _cellDepth--;
            if (_suppress == 0)
                _current.LastWasParagraph = _current.LastWasTable = false;
        }

        public void TableCellEnd(TableCell t, Row row)
        {
            WriteCellEnd();
        }

        public async Task<bool> MatrixStart(Matrix m, MatrixCellEntry[,] matrix, Row row, int headerRows, int maxRows, int maxCols)               // called first
        {
            if (await m.IsHidden(r, row).ConfigureAwait(false))
                return false;

            if (m.PageBreakAtStart)
                RequestPageBreak(false);
            WritePendingPageBreakParagraph();
            if (_current.LastWasTable)
                WriteEmptyParagraph();      // keep two tables in a row apart

            MatrixState ms = new MatrixState();
            ms.HeaderRows = headerRows;
            float[] widths = m.ColumnWidths(matrix, maxCols);
            ms.ColumnWidths = new int[maxCols];
            int total = 0;
            for (int i = 0; i < maxCols; i++)
            {
                ms.ColumnWidths[i] = RSize.TwipsFromPoints(widths[i]);
                total += ms.ColumnWidths[i];
            }
            _matrixes.Push(ms);

            StringBuilder sb = Out;
            sb.Append("<w:tbl><w:tblPr>");
            sb.AppendFormat(CultureInfo.InvariantCulture, "<w:tblW w:w=\"{0}\" w:type=\"dxa\"/>", total);
            sb.Append("<w:tblLayout w:type=\"fixed\"/>");
            sb.Append("<w:tblCellMar><w:top w:w=\"0\" w:type=\"dxa\"/><w:left w:w=\"0\" w:type=\"dxa\"/><w:bottom w:w=\"0\" w:type=\"dxa\"/><w:right w:w=\"0\" w:type=\"dxa\"/></w:tblCellMar>");
            sb.Append("</w:tblPr><w:tblGrid>");
            foreach (int w in ms.ColumnWidths)
                sb.AppendFormat(CultureInfo.InvariantCulture, "<w:gridCol w:w=\"{0}\"/>", w);
            sb.Append("</w:tblGrid>");
            return true;
        }

        public void MatrixColumns(Matrix m, MatrixColumns mc)   // called just after MatrixStart
        {
        }

        public async Task MatrixCellStart(Matrix m, ReportItem ri, int row, int column, Row r, float h, float w, int colSpan)
        {
            MatrixState ms = _matrixes.Peek();
            if (ri == null)         // Empty cell?
            {
                if (ms.SkipCols == 0)
                    WriteCellStart(ms.ColumnWidths[column], 1, null);
                return;             // otherwise it is covered by the cell to its left
            }

            int width = 0;
            for (int ci = column; ci < column + colSpan && ci < ms.ColumnWidths.Length; ci++)
                width += ms.ColumnWidths[ci];
            StyleInfo si = ri.Style == null ? null : await ri.Style.GetStyleInfoShared(this.r, r).ConfigureAwait(false);   // read only
            WriteCellStart(width, colSpan, si);
            ms.SkipCols = colSpan != 1 ? -(colSpan - 1) : 0;    // start it as negative as indicator that we need to close the cell
        }

        public Task MatrixCellEnd(Matrix m, ReportItem ri, int row, int column, Row r)
        {
            MatrixState ms = _matrixes.Peek();
            if (ms.SkipCols == 0)
                WriteCellEnd();
            else if (ms.SkipCols < 0)
            {
                WriteCellEnd();
                ms.SkipCols = -ms.SkipCols;
            }
            else
                ms.SkipCols--;
            return Task.CompletedTask;
        }

        public void MatrixRowStart(Matrix m, int row, Row r)
        {
            MatrixState ms = _matrixes.Peek();
            Out.Append("<w:tr>");
            if (row < ms.HeaderRows)
                Out.Append("<w:trPr><w:tblHeader/></w:trPr>");      // repeat matrix column headings on multiple pages
            if (InBody && _cellDepth == 0 && _suppress == 0)
                _contentSinceBreak = true;
        }

        public void MatrixRowEnd(Matrix m, int row, Row r)
        {
            Out.Append("</w:tr>");
        }

        public Task MatrixEnd(Matrix m, Row r)              // called last
        {
            _matrixes.Pop();
            Out.Append("</w:tbl>");
            MarkTableEnd();
            if (m.PageBreakAtEnd)
                RequestPageBreak(false);
            return Task.CompletedTask;
        }

        public async Task Chart(Chart c, Row row, ChartBase cb)
        {
            if (await c.IsHidden(r, row).ConfigureAwait(false))
                return;
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                await cb.SaveAsync(r, ms, ImageFormat.Png).ConfigureAwait(false);
                data = ms.ToArray();
            }
            float w = c.Width != null ? c.Width.Points : 0;
            float h = c.Height != null ? c.Height.Points : 0;
            if (w <= 0 || h <= 0)
            {   // no size in the report: use the size the chart was drawn at
                using (MemoryStream ms = new MemoryStream(data))
                using (Draw2.Image im = Draw2.Image.FromStream(ms))
                {
                    w = PointsFromPixels(im.Width);
                    h = PointsFromPixels(im.Height);
                }
            }
            WritePicture(data, "png", w, h, c.Name == null ? null : c.Name.Nm);
        }

        public async Task Image(Image i, Row r, string mimeType, Stream ioin)
        {
            if (await i.IsHidden(this.r, r).ConfigureAwait(false))
                return;

            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                await ioin.CopyToAsync(ms).ConfigureAwait(false);
                data = ms.ToArray();
            }
            if (data.Length == 0)
                return;

            // Word reads png, jpeg, gif and bmp as they are; anything else is turned into a png.
            //   The picture size is needed too when the report doesn't give it, or keeps the proportions.
            string ext = ImageExtension(data);
            int pixelWidth = 0, pixelHeight = 0;
            bool needSize = i.Width == null || i.Height == null || i.Sizing == ImageSizingEnum.FitProportional;
            if (ext == null || needSize)
            {
                try
                {
                    using (MemoryStream ms = new MemoryStream(data))
                    using (Draw2.Image im = Draw2.Image.FromStream(ms))
                    {
                        pixelWidth = im.Width;
                        pixelHeight = im.Height;
                        if (ext == null)
                        {
                            using (MemoryStream png = new MemoryStream())
                            {
                                im.Save(png, ImageFormat.Png);
                                data = png.ToArray();
                            }
                            ext = "png";
                        }
                    }
                }
                catch
                {
                    if (ext == null)
                        return;     // not an image we can read
                }
            }

            float w = i.Width != null ? i.Width.Points : PointsFromPixels(pixelWidth);
            float h = i.Height != null ? i.Height.Points : PointsFromPixels(pixelHeight);
            if (i.Sizing == ImageSizingEnum.FitProportional && pixelWidth > 0 && pixelHeight > 0 && w > 0 && h > 0)
            {   // shrink one side so the picture keeps its proportions inside the box
                float ratio = Math.Min(w / pixelWidth, h / pixelHeight);
                w = pixelWidth * ratio;
                h = pixelHeight * ratio;
            }
            WritePicture(data, ext, w, h, i.Name == null ? null : i.Name.Nm);
        }

        private float PointsFromPixels(int pixels)
        {
            return pixels <= 0 ? 0 : RSize.PointsFromPixels(GetGraphics, pixels);
        }

        /// <summary>
        /// The file extension Word knows the picture by, from its first bytes; null when unknown
        /// </summary>
        private static string ImageExtension(byte[] d)
        {
            if (d.Length >= 8 && d[0] == 0x89 && d[1] == 0x50 && d[2] == 0x4E && d[3] == 0x47)
                return "png";
            if (d.Length >= 3 && d[0] == 0xFF && d[1] == 0xD8 && d[2] == 0xFF)
                return "jpeg";
            if (d.Length >= 6 && d[0] == 'G' && d[1] == 'I' && d[2] == 'F')
                return "gif";
            if (d.Length >= 2 && d[0] == 'B' && d[1] == 'M')
                return "bmp";
            return null;
        }

        /// <summary>
        /// Put a picture in its own paragraph.  The same picture (a logo on every row, say) is
        /// stored in the package only once.
        /// </summary>
        private void WritePicture(byte[] data, string ext, float widthPoints, float heightPoints, string name)
        {
            if (_suppress > 0 || widthPoints <= 0 || heightPoints <= 0)
                return;

            string hash = Convert.ToHexString(SHA256.HashData(data));
            if (!_mediaByHash.TryGetValue(hash, out string mediaPath))
            {
                mediaPath = "media/image" + (_media.Count + 1).ToString(CultureInfo.InvariantCulture) + "." + ext;
                _mediaByHash.Add(hash, mediaPath);
                _media.Add(("word/" + mediaPath, data));
            }
            if (!_current.MediaRelIds.TryGetValue(mediaPath, out string relId))
            {
                relId = NextRelId();
                _current.MediaRelIds.Add(mediaPath, relId);
                _current.Rels.Add((relId, RelImage, mediaPath));
            }

            long cx = (long)Math.Round(widthPoints * 12700);      // EMUs: 12700 to a point
            long cy = (long)Math.Round(heightPoints * 12700);
            int id = ++_drawingId;
            string picName = XmlEscape(string.IsNullOrEmpty(name) ? "Picture " + id.ToString(CultureInfo.InvariantCulture) : name);

            StringBuilder sb = Out;
            sb.Append("<w:p><w:pPr>");
            if (TakePageBreak())
                sb.Append("<w:pageBreakBefore/>");
            sb.Append("</w:pPr><w:r><w:drawing>");
            sb.AppendFormat(CultureInfo.InvariantCulture,
                "<wp:inline distT=\"0\" distB=\"0\" distL=\"0\" distR=\"0\"><wp:extent cx=\"{0}\" cy=\"{1}\"/>" +
                "<wp:docPr id=\"{2}\" name=\"{3}\"/>" +
                "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/picture\">" +
                "<pic:pic><pic:nvPicPr><pic:cNvPr id=\"{2}\" name=\"{3}\"/><pic:cNvPicPr/></pic:nvPicPr>" +
                "<pic:blipFill><a:blip r:embed=\"{4}\"/><a:stretch><a:fillRect/></a:stretch></pic:blipFill>" +
                "<pic:spPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"{0}\" cy=\"{1}\"/></a:xfrm><a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></pic:spPr>" +
                "</pic:pic></a:graphicData></a:graphic></wp:inline>",
                cx, cy, id, picName, relId);
            sb.Append("</w:drawing></w:r></w:p>");
            MarkParagraph();
        }

        public async Task Line(Line l, Row row)
        {
            // Only a horizontal line can be shown in flowing text: as the bottom border of an empty paragraph
            if (l.Style == null || (l.Height != null && Math.Abs(l.Height.Points) > 0.5f))
                return;
            if (await l.IsHidden(r, row).ConfigureAwait(false))
                return;
            StyleInfo si = await l.Style.GetStyleInfoShared(r, row).ConfigureAwait(false);   // read only
            StringBuilder border = new StringBuilder();
            Border(border, "bottom", si.BStyleTop, si.BWidthTop, si.BColorTop, true);
            if (border.Length == 0)
                return;
            StringBuilder sb = Out;
            sb.Append("<w:p><w:pPr>");
            if (TakePageBreak())
                sb.Append("<w:pageBreakBefore/>");
            sb.Append("<w:pBdr>").Append(border).Append("</w:pBdr>").Append(EmptyParagraphProps).Append("</w:pPr></w:p>");
            MarkParagraph();
        }

        public async Task<bool> RectangleStart(Rdl.Rectangle rect, Row r)
        {
            if (await rect.IsHidden(this.r, r).ConfigureAwait(false))
                return false;
            if (rect.PageBreakAtStart && !rect.IsTableOrMatrixCell(this.r))
                RequestPageBreak(false);
            return true;
        }

        public Task RectangleEnd(Rdl.Rectangle rect, Row r)
        {
            if (rect.PageBreakAtEnd && !rect.IsTableOrMatrixCell(this.r))
                RequestPageBreak(false);
            return Task.CompletedTask;
        }

        // Subreport:  the subreport's own items come through this renderer as well
        public Task Subreport(Subreport s, Row r)
        {
            return Task.CompletedTask;
        }

        public void GroupingStart(Grouping g)           // called at start of grouping
        {
            _groupInstances[g] = 0;
        }

        public void GroupingInstanceStart(Grouping g)   // called at start for each grouping instance
        {
            _groupInstances.TryGetValue(g, out int n);
            _groupInstances[g] = n + 1;
            // no break before the first group: that would only split the table from its header
            if (n > 0 && g.PageBreakAtStart)
                RequestPageBreak(true);
        }

        public void GroupingInstanceEnd(Grouping g)     // called at start for each grouping instance
        {
            if (g.PageBreakAtEnd)
                RequestPageBreak(true);
        }

        public void GroupingEnd(Grouping g)             // called at end of grouping
        {
            // a break after the last group isn't wanted; the table footer stays with the last group
            if (_pendingFromGroup)
                _pendingPageBreak = _pendingFromGroup = false;
            _groupInstances.Remove(g);
        }

        public Task RunPages(Pages pgs)     // we don't have paging turned on for docx
        {
            return Task.CompletedTask;
        }
    }
}
