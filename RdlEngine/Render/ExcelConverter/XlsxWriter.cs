using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace RdlEngine.Render.ExcelConverter
{
    // EMU (English Metric Units): 1 point = 12700 EMU, 1 inch = 914400 EMU
    internal static class XlsxUnits
    {
        public const long EmuPerPoint = 12700L;
        public const long EmuPerInch = 914400L;
    }

    internal class XlsxBorderSide
    {
        public string Style = ""; // "thin","medium","thick","dotted","dashed","double","hair", "" = none
        public (byte R, byte G, byte B) Color = (0, 0, 0);

        public bool Equals(XlsxBorderSide o) =>
            o != null && Style == o.Style && Color == o.Color;
    }

    internal class XlsxCellFormat
    {
        // Font
        public string FontName = "Calibri";
        public double FontSizePt = 11;
        public bool Bold;
        public bool Italic;
        public bool Strikeout;
        public bool Underline;
        public (byte R, byte G, byte B) FontColor = (0, 0, 0);

        // Fill
        public bool HasBackground;
        public (byte R, byte G, byte B) BackgroundColor = (255, 255, 255);

        // Borders
        public XlsxBorderSide BorderTop = new XlsxBorderSide();
        public XlsxBorderSide BorderRight = new XlsxBorderSide();
        public XlsxBorderSide BorderBottom = new XlsxBorderSide();
        public XlsxBorderSide BorderLeft = new XlsxBorderSide();

        // Alignment
        public string HorizontalAlign = "general"; // "general","left","center","right","justify"
        public string VerticalAlign = "top";        // "top","center","bottom"
        public bool WrapText = true;
        public int Rotation = 0;
    }

    internal class XlsxWriter
    {
        private readonly string _sheetName;
        private readonly SortedDictionary<int, double> _columnWidths = new SortedDictionary<int, double>();
        private readonly SortedDictionary<int, double> _rowHeights = new SortedDictionary<int, double>();
        private readonly List<CellEntry> _cells = new List<CellEntry>();
        private readonly List<(int r1, int c1, int r2, int c2)> _merges = new List<(int, int, int, int)>();
        private readonly List<ImageEntry> _images = new List<ImageEntry>();
        private readonly List<LineEntry> _lines = new List<LineEntry>();

        // Print setup
        private double _leftMarginInches = 0.75;
        private double _topMarginInches = 1.0;
        private double _rightMarginInches = 0.75;
        private double _bottomMarginInches = 1.0;
        private bool _landscape = false;
        private int _paperSize = 9; // A4

        // Style tables
        private readonly List<FontDef> _fonts = new List<FontDef>();
        private readonly List<FillDef> _fills = new List<FillDef>();
        private readonly List<BorderDef> _borders = new List<BorderDef>();
        private readonly List<XfDef> _cellXfs = new List<XfDef>();

        public XlsxWriter(string sheetName)
        {
            _sheetName = string.IsNullOrEmpty(sheetName) ? "Sheet1" : sheetName;
            // Required default entries per OOXML spec
            _fonts.Add(new FontDef { Name = "Calibri", SizePt = 11 });
            _fills.Add(new FillDef { PatternType = "none" });   // index 0
            _fills.Add(new FillDef { PatternType = "gray125" }); // index 1
            _borders.Add(new BorderDef());                       // index 0 = empty
            _cellXfs.Add(new XfDef());                          // index 0 = default
        }

        public void SetColumnWidth(int colIndex, double widthInChars)
            => _columnWidths[colIndex] = widthInChars;

        public void SetRowHeight(int rowIndex, double heightInPoints)
            => _rowHeights[rowIndex] = heightInPoints;

        public void SetCell(int rowIndex, int colIndex, string value, XlsxCellFormat format = null)
        {
            int styleIndex = format != null ? GetOrAddCellStyle(format) : 0;
            _cells.Add(new CellEntry(rowIndex, colIndex, value ?? "", styleIndex));
        }

        public void AddMerge(int r1, int c1, int r2, int c2)
            => _merges.Add((r1, c1, r2, c2));

        public void AddImage(byte[] data, long posXEmu, long posYEmu, long widthEmu, long heightEmu)
            => _images.Add(new ImageEntry(data, posXEmu, posYEmu, widthEmu, heightEmu));

        public void AddLine(long x1Emu, long y1Emu, long x2Emu, long y2Emu, double lineWidthPt,
            byte r = 0, byte g = 0, byte b = 0)
            => _lines.Add(new LineEntry(x1Emu, y1Emu, x2Emu, y2Emu, lineWidthPt, r, g, b));

        public void SetPrintSetup(double leftIn, double topIn, double rightIn, double bottomIn, bool landscape)
        {
            _leftMarginInches = leftIn;
            _topMarginInches = topIn;
            _rightMarginInches = rightIn;
            _bottomMarginInches = bottomIn;
            _landscape = landscape;
        }

        public void Write(Stream output)
        {
            bool hasDrawings = _images.Count > 0 || _lines.Count > 0;

            using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
            {
                WriteText(zip, "[Content_Types].xml", BuildContentTypes(hasDrawings));
                WriteText(zip, "_rels/.rels", BuildRootRels());
                WriteText(zip, "xl/workbook.xml", BuildWorkbook());
                WriteText(zip, "xl/_rels/workbook.xml.rels", BuildWorkbookRels());
                WriteText(zip, "xl/styles.xml", BuildStyles());
                WriteText(zip, "xl/worksheets/sheet1.xml", BuildSheet(hasDrawings));

                if (hasDrawings)
                {
                    WriteText(zip, "xl/worksheets/_rels/sheet1.xml.rels", BuildSheetRels());
                    WriteText(zip, "xl/drawings/drawing1.xml", BuildDrawing());
                    if (_images.Count > 0)
                    {
                        WriteText(zip, "xl/drawings/_rels/drawing1.xml.rels", BuildDrawingRels());
                        for (int i = 0; i < _images.Count; i++)
                            WriteBytes(zip, $"xl/media/image{i + 1}.jpg", _images[i].Data);
                    }
                }
            }
        }

        private int GetOrAddCellStyle(XlsxCellFormat fmt)
        {
            var font = new FontDef
            {
                Name = fmt.FontName ?? "Calibri",
                SizePt = fmt.FontSizePt > 0 ? fmt.FontSizePt : 11,
                Bold = fmt.Bold,
                Italic = fmt.Italic,
                Strikeout = fmt.Strikeout,
                Underline = fmt.Underline,
                Color = fmt.FontColor
            };
            int fontId = GetOrAdd(_fonts, font);

            FillDef fill;
            if (fmt.HasBackground)
                fill = new FillDef { PatternType = "solid", FgColor = fmt.BackgroundColor };
            else
                fill = new FillDef { PatternType = "none" };
            int fillId = GetOrAdd(_fills, fill);

            var border = new BorderDef
            {
                Left = fmt.BorderLeft,
                Right = fmt.BorderRight,
                Top = fmt.BorderTop,
                Bottom = fmt.BorderBottom
            };
            int borderId = GetOrAdd(_borders, border);

            var xf = new XfDef
            {
                FontId = fontId,
                FillId = fillId,
                BorderId = borderId,
                HorizAlign = fmt.HorizontalAlign,
                VertAlign = fmt.VerticalAlign,
                WrapText = fmt.WrapText,
                Rotation = fmt.Rotation
            };
            return GetOrAdd(_cellXfs, xf);
        }

        private static int GetOrAdd<T>(List<T> list, T item) where T : IEquatable<T>
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i].Equals(item)) return i;
            list.Add(item);
            return list.Count - 1;
        }

        // ── XML builders ──────────────────────────────────────────────────────────

        private string BuildContentTypes(bool hasDrawings)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
            sb.Append("<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
            sb.Append("<Default Extension=\"xml\" ContentType=\"application/xml\"/>");
            if (_images.Count > 0)
                sb.Append("<Default Extension=\"jpg\" ContentType=\"image/jpeg\"/>");
            sb.Append("<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
            sb.Append("<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
            sb.Append("<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
            if (hasDrawings)
                sb.Append("<Override PartName=\"/xl/drawings/drawing1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.drawing+xml\"/>");
            sb.Append("</Types>");
            return sb.ToString();
        }

        private string BuildRootRels() =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
            "</Relationships>";

        private string BuildWorkbook()
        {
            string name = XmlEscape(_sheetName);
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
                "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                "<sheets><sheet name=\"" + name + "\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
                "</workbook>";
        }

        private string BuildWorkbookRels() =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
            "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
            "</Relationships>";

        private string BuildStyles()
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");

            // Fonts
            sb.Append($"<fonts count=\"{_fonts.Count}\">");
            foreach (var f in _fonts)
            {
                sb.Append("<font>");
                if (f.Bold) sb.Append("<b/>");
                if (f.Italic) sb.Append("<i/>");
                if (f.Strikeout) sb.Append("<strike/>");
                if (f.Underline) sb.Append("<u/>");
                sb.Append($"<sz val=\"{FormatDouble(f.SizePt)}\"/>");
                sb.Append($"<color rgb=\"FF{f.Color.R:X2}{f.Color.G:X2}{f.Color.B:X2}\"/>");
                sb.Append($"<name val=\"{XmlEscape(f.Name)}\"/>");
                sb.Append("</font>");
            }
            sb.Append("</fonts>");

            // Fills
            sb.Append($"<fills count=\"{_fills.Count}\">");
            foreach (var f in _fills)
            {
                sb.Append("<fill><patternFill patternType=\"" + f.PatternType + "\">");
                if (f.PatternType == "solid")
                    sb.Append($"<fgColor rgb=\"FF{f.FgColor.R:X2}{f.FgColor.G:X2}{f.FgColor.B:X2}\"/>");
                sb.Append("</patternFill></fill>");
            }
            sb.Append("</fills>");

            // Borders
            sb.Append($"<borders count=\"{_borders.Count}\">");
            foreach (var bd in _borders)
            {
                sb.Append("<border>");
                AppendBorderSide(sb, "left", bd.Left);
                AppendBorderSide(sb, "right", bd.Right);
                AppendBorderSide(sb, "top", bd.Top);
                AppendBorderSide(sb, "bottom", bd.Bottom);
                sb.Append("<diagonal/>");
                sb.Append("</border>");
            }
            sb.Append("</borders>");

            // cellStyleXfs (required, 1 default entry)
            sb.Append("<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>");

            // cellXfs
            sb.Append($"<cellXfs count=\"{_cellXfs.Count}\">");
            foreach (var xf in _cellXfs)
            {
                bool applyFont = xf.FontId != 0;
                bool applyFill = xf.FillId != 0;
                bool applyBorder = xf.BorderId != 0;
                bool applyAlign = !string.IsNullOrEmpty(xf.HorizAlign) || !string.IsNullOrEmpty(xf.VertAlign) || xf.WrapText || xf.Rotation != 0;

                sb.Append($"<xf numFmtId=\"0\" fontId=\"{xf.FontId}\" fillId=\"{xf.FillId}\" borderId=\"{xf.BorderId}\" xfId=\"0\"");
                if (applyFont) sb.Append(" applyFont=\"1\"");
                if (applyFill) sb.Append(" applyFill=\"1\"");
                if (applyBorder) sb.Append(" applyBorder=\"1\"");
                if (applyAlign) sb.Append(" applyAlignment=\"1\"");
                sb.Append(">");
                if (applyAlign)
                {
                    sb.Append("<alignment");
                    if (!string.IsNullOrEmpty(xf.HorizAlign) && xf.HorizAlign != "general")
                        sb.Append($" horizontal=\"{xf.HorizAlign}\"");
                    if (!string.IsNullOrEmpty(xf.VertAlign))
                        sb.Append($" vertical=\"{xf.VertAlign}\"");
                    if (xf.WrapText) sb.Append(" wrapText=\"1\"");
                    if (xf.Rotation != 0)
                    {
                        // OOXML: 0-90 = counterclockwise, 91-180 = clockwise (stored as 90+(90-angle))
                        int rot = xf.Rotation;
                        if (rot < 0) rot = 90 + (90 + rot); // e.g. -90 → 180
                        sb.Append($" textRotation=\"{rot}\"");
                    }
                    sb.Append("/>");
                }
                sb.Append("</xf>");
            }
            sb.Append("</cellXfs>");

            // cellStyles (required)
            sb.Append("<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>");

            sb.Append("</styleSheet>");
            return sb.ToString();
        }

        private static void AppendBorderSide(StringBuilder sb, string side, XlsxBorderSide bs)
        {
            if (bs == null || string.IsNullOrEmpty(bs.Style))
            {
                sb.Append($"<{side}/>");
                return;
            }
            sb.Append($"<{side} style=\"{bs.Style}\">");
            sb.Append($"<color rgb=\"FF{bs.Color.R:X2}{bs.Color.G:X2}{bs.Color.B:X2}\"/>");
            sb.Append($"</{side}>");
        }

        private string BuildSheet(bool hasDrawings)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
                      "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");

            // Page setup
            string orient = _landscape ? "landscape" : "portrait";
            sb.Append($"<pageSetup paperSize=\"{_paperSize}\" orientation=\"{orient}\"/>");
            sb.Append($"<pageMargins left=\"{FormatDouble(_leftMarginInches)}\" right=\"{FormatDouble(_rightMarginInches)}\" " +
                      $"top=\"{FormatDouble(_topMarginInches)}\" bottom=\"{FormatDouble(_bottomMarginInches)}\" " +
                      "header=\"0.5\" footer=\"0.5\"/>");

            // Column widths
            if (_columnWidths.Count > 0)
            {
                sb.Append("<cols>");
                foreach (var kv in _columnWidths)
                {
                    int col = kv.Key + 1; // 1-based in xlsx
                    sb.Append($"<col min=\"{col}\" max=\"{col}\" width=\"{FormatDouble(kv.Value)}\" customWidth=\"1\"/>");
                }
                sb.Append("</cols>");
            }

            // Sheet data — group cells by row
            var byRow = new SortedDictionary<int, List<CellEntry>>();
            foreach (var c in _cells)
            {
                if (!byRow.TryGetValue(c.Row, out var list))
                {
                    list = new List<CellEntry>();
                    byRow[c.Row] = list;
                }
                list.Add(c);
            }

            sb.Append("<sheetData>");
            foreach (var kv in byRow)
            {
                int rowIdx = kv.Key;
                string htAttr = _rowHeights.TryGetValue(rowIdx, out double ht)
                    ? $" ht=\"{FormatDouble(ht)}\" customHeight=\"1\""
                    : "";
                sb.Append($"<row r=\"{rowIdx + 1}\"{htAttr}>");
                kv.Value.Sort((a, b) => a.Col.CompareTo(b.Col));
                foreach (var cell in kv.Value)
                {
                    string cellRef = ColRef(cell.Col) + (cell.Row + 1);
                    string styleAttr = cell.StyleIndex > 0 ? $" s=\"{cell.StyleIndex}\"" : "";
                    string val = cell.Value;
                    sb.Append($"<c r=\"{cellRef}\" t=\"inlineStr\"{styleAttr}><is><t>{XmlEscape(val)}</t></is></c>");
                }
                sb.Append("</row>");
            }
            sb.Append("</sheetData>");

            // Merged cells
            if (_merges.Count > 0)
            {
                sb.Append($"<mergeCells count=\"{_merges.Count}\">");
                foreach (var m in _merges)
                {
                    string ref1 = ColRef(m.c1) + (m.r1 + 1);
                    string ref2 = ColRef(m.c2) + (m.r2 + 1);
                    sb.Append($"<mergeCell ref=\"{ref1}:{ref2}\"/>");
                }
                sb.Append("</mergeCells>");
            }

            if (hasDrawings)
                sb.Append("<drawing r:id=\"rId1\"/>");

            sb.Append("</worksheet>");
            return sb.ToString();
        }

        private string BuildSheetRels() =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/drawing\" Target=\"../drawings/drawing1.xml\"/>" +
            "</Relationships>";

        private string BuildDrawing()
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<xdr:wsDr xmlns:xdr=\"http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing\" " +
                      "xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" " +
                      "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");

            int shapeId = 1;

            for (int i = 0; i < _images.Count; i++)
            {
                var img = _images[i];
                sb.Append("<xdr:absoluteAnchor>");
                sb.Append($"<xdr:pos x=\"{img.PosXEmu}\" y=\"{img.PosYEmu}\"/>");
                sb.Append($"<xdr:ext cx=\"{img.WidthEmu}\" cy=\"{img.HeightEmu}\"/>");
                sb.Append("<xdr:pic>");
                sb.Append($"<xdr:nvPicPr><xdr:cNvPr id=\"{shapeId++}\" name=\"Image {i + 1}\"/><xdr:cNvPicPr/></xdr:nvPicPr>");
                sb.Append($"<xdr:blipFill><a:blip r:embed=\"rId{i + 1}\"/><a:stretch><a:fillRect/></a:stretch></xdr:blipFill>");
                sb.Append("<xdr:spPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"" + img.WidthEmu + "\" cy=\"" + img.HeightEmu + "\"/></a:xfrm>" +
                          "<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></xdr:spPr>");
                sb.Append("</xdr:pic>");
                sb.Append("<xdr:clientData/></xdr:absoluteAnchor>");
            }

            for (int i = 0; i < _lines.Count; i++)
            {
                var ln = _lines[i];
                long x = Math.Min(ln.X1, ln.X2);
                long y = Math.Min(ln.Y1, ln.Y2);
                long cx = Math.Abs(ln.X2 - ln.X1);
                long cy = Math.Abs(ln.Y2 - ln.Y1);
                // Ensure non-zero extents (zero-size anchors are invalid)
                if (cx == 0) cx = 1;
                if (cy == 0) cy = 1;
                bool flipH = ln.X2 < ln.X1;
                bool flipV = ln.Y2 < ln.Y1;
                long lineWidthEmu = (long)(ln.LineWidthPt * 12700);
                string color = $"{ln.R:X2}{ln.G:X2}{ln.B:X2}";

                sb.Append("<xdr:absoluteAnchor>");
                sb.Append($"<xdr:pos x=\"{x}\" y=\"{y}\"/>");
                sb.Append($"<xdr:ext cx=\"{cx}\" cy=\"{cy}\"/>");
                sb.Append("<xdr:sp macro=\"\" textlink=\"\">");
                sb.Append($"<xdr:nvSpPr><xdr:cNvPr id=\"{shapeId++}\" name=\"Line {i + 1}\"/>" +
                          "<xdr:cNvSpPr><a:spLocks noGrp=\"1\"/></xdr:cNvSpPr></xdr:nvSpPr>");
                sb.Append("<xdr:spPr>");
                sb.Append($"<a:xfrm{(flipH ? " flipH=\"1\"" : "")}{(flipV ? " flipV=\"1\"" : "")}>");
                sb.Append($"<a:off x=\"0\" y=\"0\"/><a:ext cx=\"{cx}\" cy=\"{cy}\"/></a:xfrm>");
                sb.Append("<a:prstGeom prst=\"line\"><a:avLst/></a:prstGeom>");
                sb.Append("<a:noFill/>");
                sb.Append($"<a:ln w=\"{lineWidthEmu}\"><a:solidFill><a:srgbClr val=\"{color}\"/></a:solidFill></a:ln>");
                sb.Append("</xdr:spPr>");
                sb.Append("<xdr:txBody><a:bodyPr/><a:lstStyle/><a:p/></xdr:txBody>");
                sb.Append("</xdr:sp>");
                sb.Append("<xdr:clientData/></xdr:absoluteAnchor>");
            }

            sb.Append("</xdr:wsDr>");
            return sb.ToString();
        }

        private string BuildDrawingRels()
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
            for (int i = 0; i < _images.Count; i++)
                sb.Append($"<Relationship Id=\"rId{i + 1}\" " +
                    "Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/image\" " +
                    $"Target=\"../media/image{i + 1}.jpg\"/>");
            sb.Append("</Relationships>");
            return sb.ToString();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string ColRef(int colIndex)
        {
            // 0-based column index to A, B, ... Z, AA, AB, ...
            string result = "";
            int n = colIndex + 1;
            while (n > 0)
            {
                n--;
                result = (char)('A' + n % 26) + result;
                n /= 26;
            }
            return result;
        }

        private static string XmlEscape(string s)
        {
            if (s == null) return "";
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
                    .Replace("\"", "&quot;").Replace("'", "&apos;");
        }

        private static string FormatDouble(double d) => d.ToString("G", System.Globalization.CultureInfo.InvariantCulture);

        private static void WriteText(ZipArchive zip, string path, string content)
        {
            var entry = zip.CreateEntry(path, CompressionLevel.Fastest);
            using (var stream = entry.Open())
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                writer.Write(content);
        }

        private static void WriteBytes(ZipArchive zip, string path, byte[] data)
        {
            var entry = zip.CreateEntry(path, CompressionLevel.Fastest);
            using (var stream = entry.Open())
                stream.Write(data, 0, data.Length);
        }

        // ── Style record types ────────────────────────────────────────────────────

        private class FontDef : IEquatable<FontDef>
        {
            public string Name = "Calibri";
            public double SizePt = 11;
            public bool Bold, Italic, Strikeout, Underline;
            public (byte R, byte G, byte B) Color;

            public bool Equals(FontDef o) =>
                o != null && Name == o.Name && SizePt == o.SizePt &&
                Bold == o.Bold && Italic == o.Italic && Strikeout == o.Strikeout &&
                Underline == o.Underline && Color == o.Color;
        }

        private class FillDef : IEquatable<FillDef>
        {
            public string PatternType = "none";
            public (byte R, byte G, byte B) FgColor;

            public bool Equals(FillDef o) =>
                o != null && PatternType == o.PatternType && FgColor == o.FgColor;
        }

        private class BorderDef : IEquatable<BorderDef>
        {
            public XlsxBorderSide Left, Right, Top, Bottom;

            public bool Equals(BorderDef o)
            {
                if (o == null) return false;
                return SideEquals(Left, o.Left) && SideEquals(Right, o.Right) &&
                       SideEquals(Top, o.Top) && SideEquals(Bottom, o.Bottom);
            }

            private static bool SideEquals(XlsxBorderSide a, XlsxBorderSide b)
            {
                if (a == null && b == null) return true;
                if (a == null || b == null) return false;
                return a.Style == b.Style && a.Color == b.Color;
            }
        }

        private class XfDef : IEquatable<XfDef>
        {
            public int FontId, FillId, BorderId;
            public string HorizAlign = "general";
            public string VertAlign = "top";
            public bool WrapText;
            public int Rotation;

            public bool Equals(XfDef o) =>
                o != null && FontId == o.FontId && FillId == o.FillId &&
                BorderId == o.BorderId && HorizAlign == o.HorizAlign &&
                VertAlign == o.VertAlign && WrapText == o.WrapText && Rotation == o.Rotation;
        }

        // ── Data holders ──────────────────────────────────────────────────────────

        private class CellEntry
        {
            public int Row, Col, StyleIndex;
            public string Value;
            public CellEntry(int row, int col, string value, int style)
            { Row = row; Col = col; Value = value; StyleIndex = style; }
        }

        private class ImageEntry
        {
            public byte[] Data;
            public long PosXEmu, PosYEmu, WidthEmu, HeightEmu;
            public ImageEntry(byte[] data, long x, long y, long w, long h)
            { Data = data; PosXEmu = x; PosYEmu = y; WidthEmu = w; HeightEmu = h; }
        }

        private class LineEntry
        {
            public long X1, Y1, X2, Y2;
            public double LineWidthPt;
            public byte R, G, B;
            public LineEntry(long x1, long y1, long x2, long y2, double lw, byte r, byte g, byte b)
            { X1 = x1; Y1 = y1; X2 = x2; Y2 = y2; LineWidthPt = lw; R = r; G = g; B = b; }
        }
    }
}
