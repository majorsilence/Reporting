using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Majorsilence.Excel
{
#if NETSTANDARD2_0
    // System.Collections.Generic.ReferenceEqualityComparer isn't available before net5.0.
    internal sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();
        private ReferenceEqualityComparer() { }
        public bool Equals(object x, object y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
#endif

    // EMU (English Metric Units): 1 point = 12700 EMU, 1 inch = 914400 EMU
    public static class XlsxUnits
    {
        public const long EmuPerPoint = 12700L;
        public const long EmuPerInch = 914400L;
    }

    public class XlsxBorderSide
    {
        public string Style = ""; // "thin","medium","thick","dotted","dashed","double","hair", "" = none
        public (byte R, byte G, byte B) Color = (0, 0, 0);

        public bool Equals(XlsxBorderSide o) =>
            o != null && Style == o.Style && Color == o.Color;
    }

    public class XlsxCellFormat
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

        // Number format. An OOXML format code string (e.g. "0.00%", "m/d/yyyy"), or null for
        // General. See XlsxNumberFormats for common presets.
        public string? NumberFormatCode;
    }

    // Common OOXML number format codes for use with XlsxCellFormat.NumberFormatCode.
    public static class XlsxNumberFormats
    {
        public const string Integer = "0";
        public const string TwoDecimal = "0.00";
        public const string ThousandsTwoDecimal = "#,##0.00";
        public const string Currency = "$#,##0.00";
        public const string Percentage = "0.00%";
        public const string ShortDate = "m/d/yyyy";
        public const string ShortDateTime = "m/d/yyyy h:mm";
        public const string Time = "h:mm:ss";
    }

    public class XlsxWriter
    {
        // Sheets, in workbook order. Index 0 is always the sheet created by the constructor.
        private readonly List<Sheet> _sheets = new List<Sheet>();

        // Style tables (workbook-level: shared and deduplicated across all sheets)
        private readonly List<FontDef> _fonts = new List<FontDef>();
        private readonly List<FillDef> _fills = new List<FillDef>();
        private readonly List<BorderDef> _borders = new List<BorderDef>();
        private readonly List<XfDef> _cellXfs = new List<XfDef>();
        private readonly List<string> _customNumFmts = new List<string>();
        private const int FirstCustomNumFmtId = 164; // ids below 164 are reserved for built-ins

        public XlsxWriter(string sheetName)
        {
            // Required default entries per OOXML spec
            _fonts.Add(new FontDef { Name = "Calibri", SizePt = 11 });
            _fills.Add(new FillDef { PatternType = "none" });   // index 0
            _fills.Add(new FillDef { PatternType = "gray125" }); // index 1
            _borders.Add(new BorderDef());                       // index 0 = empty
            _cellXfs.Add(new XfDef());                          // index 0 = default

            _sheets.Add(new Sheet(string.IsNullOrEmpty(sheetName) ? "Sheet1" : sheetName));
        }

        // Adds another worksheet to the workbook and returns its index, for use as the
        // sheetIndex argument to the other methods on this class (0 is the sheet created by
        // the constructor).
        public int AddSheet(string sheetName)
        {
            string name = string.IsNullOrEmpty(sheetName) ? $"Sheet{_sheets.Count + 1}" : sheetName;
            _sheets.Add(new Sheet(name));
            return _sheets.Count - 1;
        }

        public void SetColumnWidth(int colIndex, double widthInChars, int sheetIndex = 0)
            => _sheets[sheetIndex].ColumnWidths[colIndex] = widthInChars;

        public void SetRowHeight(int rowIndex, double heightInPoints, int sheetIndex = 0)
            => _sheets[sheetIndex].RowHeights[rowIndex] = heightInPoints;

        // Outline (grouping) level for Excel's collapsible row/column groups. 0 = not grouped.
        public void SetColumnOutlineLevel(int colIndex, int level, int sheetIndex = 0)
            => _sheets[sheetIndex].ColumnOutlineLevels[colIndex] = level;

        public void SetRowOutlineLevel(int rowIndex, int level, int sheetIndex = 0)
            => _sheets[sheetIndex].RowOutlineLevels[rowIndex] = level;

        public void SetColumnHidden(int colIndex, bool hidden, int sheetIndex = 0)
        {
            var hiddenCols = _sheets[sheetIndex].HiddenColumns;
            if (hidden) hiddenCols.Add(colIndex); else hiddenCols.Remove(colIndex);
        }

        public void SetRowHidden(int rowIndex, bool hidden, int sheetIndex = 0)
        {
            var hiddenRows = _sheets[sheetIndex].HiddenRows;
            if (hidden) hiddenRows.Add(rowIndex); else hiddenRows.Remove(rowIndex);
        }

        public void SetCell(int rowIndex, int colIndex, string value, XlsxCellFormat format = null, int sheetIndex = 0)
        {
            int styleIndex = ResolveStyleIndex(format);
            _sheets[sheetIndex].Cells.Add(new CellEntry(rowIndex, colIndex, CellValueKind.InlineString, value ?? "", styleIndex));
        }

        public void SetCell(int rowIndex, int colIndex, double value, XlsxCellFormat format = null, int sheetIndex = 0)
        {
            int styleIndex = ResolveStyleIndex(format);
            _sheets[sheetIndex].Cells.Add(new CellEntry(rowIndex, colIndex, CellValueKind.Number, FormatDouble(value), styleIndex));
        }

        public void SetCell(int rowIndex, int colIndex, bool value, XlsxCellFormat format = null, int sheetIndex = 0)
        {
            int styleIndex = ResolveStyleIndex(format);
            _sheets[sheetIndex].Cells.Add(new CellEntry(rowIndex, colIndex, CellValueKind.Boolean, value ? "1" : "0", styleIndex));
        }

        // Written as a date/time serial number (days since 1899-12-30, Excel's epoch) with a
        // default date number format, unless the caller's own format already specifies one.
        public void SetCell(int rowIndex, int colIndex, DateTime value, XlsxCellFormat format = null, int sheetIndex = 0)
        {
            if (format == null)
            {
                format = new XlsxCellFormat
                {
                    NumberFormatCode = value.TimeOfDay == TimeSpan.Zero
                        ? XlsxNumberFormats.ShortDate
                        : XlsxNumberFormats.ShortDateTime
                };
            }
            int styleIndex = ResolveStyleIndex(format);
            double serial = (value - ExcelEpoch).TotalDays;
            _sheets[sheetIndex].Cells.Add(new CellEntry(rowIndex, colIndex, CellValueKind.Number, FormatDouble(serial), styleIndex));
        }

        private static readonly DateTime ExcelEpoch = new DateTime(1899, 12, 30);

        // Writes a formula cell. formula is the OOXML formula text without a leading '='
        // (a leading '=' is stripped if present). cachedValue, if given, is stored as the
        // cell's <v> so the file shows a sensible result before Excel recalculates.
        public void SetFormula(int rowIndex, int colIndex, string formula, double? cachedValue = null, XlsxCellFormat format = null, int sheetIndex = 0)
        {
            if (formula == null) throw new ArgumentNullException(nameof(formula));
            int styleIndex = ResolveStyleIndex(format);
            string f = formula.Length > 0 && formula[0] == '=' ? formula.Substring(1) : formula;
            var entry = new CellEntry(rowIndex, colIndex, CellValueKind.Formula,
                cachedValue.HasValue ? FormatDouble(cachedValue.Value) : null, styleIndex)
            { Formula = f };
            _sheets[sheetIndex].Cells.Add(entry);
        }

        // The style index of each format instance passed to SetCell; a format is not changed after it is passed.
        private readonly Dictionary<XlsxCellFormat, int> _styleIndexes = new Dictionary<XlsxCellFormat, int>(ReferenceEqualityComparer.Instance);

        private int ResolveStyleIndex(XlsxCellFormat format)
        {
            if (format == null) return 0;
            if (!_styleIndexes.TryGetValue(format, out int styleIndex))
                _styleIndexes[format] = styleIndex = GetOrAddCellStyle(format);
            return styleIndex;
        }

        public void AddMerge(int r1, int c1, int r2, int c2, int sheetIndex = 0)
            => _sheets[sheetIndex].Merges.Add((r1, c1, r2, c2));

        public void AddImage(byte[] data, long posXEmu, long posYEmu, long widthEmu, long heightEmu, int sheetIndex = 0)
            => _sheets[sheetIndex].Images.Add(new ImageEntry(data, posXEmu, posYEmu, widthEmu, heightEmu));

        public void AddLine(long x1Emu, long y1Emu, long x2Emu, long y2Emu, double lineWidthPt,
            byte r = 0, byte g = 0, byte b = 0, int sheetIndex = 0)
            => _sheets[sheetIndex].Lines.Add(new LineEntry(x1Emu, y1Emu, x2Emu, y2Emu, lineWidthPt, r, g, b));

        public void SetPrintSetup(double leftIn, double topIn, double rightIn, double bottomIn, bool landscape, int sheetIndex = 0)
        {
            var sheet = _sheets[sheetIndex];
            sheet.LeftMarginInches = leftIn;
            sheet.TopMarginInches = topIn;
            sheet.RightMarginInches = rightIn;
            sheet.BottomMarginInches = bottomIn;
            sheet.Landscape = landscape;
        }

        // Freezes the given number of leading rows and/or columns so they stay visible while
        // scrolling. Pass 0 for either to leave that axis unfrozen.
        public void FreezePanes(int frozenRows, int frozenCols, int sheetIndex = 0)
        {
            var sheet = _sheets[sheetIndex];
            sheet.FrozenRows = frozenRows;
            sheet.FrozenCols = frozenCols;
        }

        // Enables Excel's column filter dropdowns over the given cell range.
        public void SetAutoFilter(int r1, int c1, int r2, int c2, int sheetIndex = 0)
            => _sheets[sheetIndex].AutoFilterRange = (r1, c1, r2, c2);

        // Workbook-level defined names (named ranges), keyed by name -> absolute formula text.
        private readonly List<(string Name, string Formula)> _definedNames = new List<(string, string)>();

        // Defines a workbook-level name for a cell range on the given sheet.
        public void AddDefinedName(string name, int sheetIndex, int r1, int c1, int r2, int c2)
        {
            string sheetRef = "'" + _sheets[sheetIndex].Name.Replace("'", "''") + "'";
            string range = r1 == r2 && c1 == c2
                ? AbsoluteCellRef(c1, r1)
                : $"{AbsoluteCellRef(c1, r1)}:{AbsoluteCellRef(c2, r2)}";
            _definedNames.Add((name, $"{sheetRef}!{range}"));
        }

        // Defines a workbook-level name for a single cell on the given sheet.
        public void AddDefinedName(string name, int sheetIndex, int row, int col)
            => AddDefinedName(name, sheetIndex, row, col, row, col);

        private static string AbsoluteCellRef(int colIndex, int rowIndex) => "$" + ColRef(colIndex) + "$" + (rowIndex + 1);

        // Attaches a hyperlink to an external URL on the given cell.
        public void AddHyperlink(int rowIndex, int colIndex, string url, int sheetIndex = 0)
            => _sheets[sheetIndex].ExternalHyperlinks.Add((rowIndex, colIndex, url));

        // Attaches a hyperlink to another location within the workbook (e.g. "Sheet2!A1" or
        // "'My Sheet'!A1:B2") on the given cell.
        public void AddInternalHyperlink(int rowIndex, int colIndex, string location, int sheetIndex = 0)
            => _sheets[sheetIndex].InternalHyperlinks.Add((rowIndex, colIndex, location));

        public void Write(Stream output)
        {
            bool anyImages = false;
            foreach (var s in _sheets)
                if (s.Images.Count > 0) { anyImages = true; break; }

            using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
            {
                WriteText(zip, "[Content_Types].xml", BuildContentTypes(anyImages));
                WriteText(zip, "_rels/.rels", BuildRootRels());
                WriteText(zip, "xl/workbook.xml", BuildWorkbook());
                WriteText(zip, "xl/_rels/workbook.xml.rels", BuildWorkbookRels());
                WriteText(zip, "xl/styles.xml", BuildStyles());

                int imageOffset = 0; // images are numbered globally across all sheets
                for (int s = 0; s < _sheets.Count; s++)
                {
                    var sheet = _sheets[s];
                    int sheetNum = s + 1;
                    bool hasDrawings = sheet.Images.Count > 0 || sheet.Lines.Count > 0;
                    bool hasSheetRels = hasDrawings || sheet.ExternalHyperlinks.Count > 0;

                    WriteText(zip, $"xl/worksheets/sheet{sheetNum}.xml", BuildSheet(sheet, hasDrawings));

                    if (hasSheetRels)
                    {
                        WriteText(zip, $"xl/worksheets/_rels/sheet{sheetNum}.xml.rels", BuildSheetRels(sheetNum, sheet, hasDrawings));
                        if (hasDrawings)
                        {
                            WriteText(zip, $"xl/drawings/drawing{sheetNum}.xml", BuildDrawing(sheet));
                            if (sheet.Images.Count > 0)
                            {
                                WriteText(zip, $"xl/drawings/_rels/drawing{sheetNum}.xml.rels", BuildDrawingRels(sheet, imageOffset));
                                for (int i = 0; i < sheet.Images.Count; i++)
                                    WriteBytes(zip, $"xl/media/image{imageOffset + i + 1}.jpg", sheet.Images[i].Data);
                                imageOffset += sheet.Images.Count;
                            }
                        }
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

            int numFmtId = fmt.NumberFormatCode != null ? GetOrAddNumFmt(fmt.NumberFormatCode) : 0;

            var xf = new XfDef
            {
                FontId = fontId,
                FillId = fillId,
                BorderId = borderId,
                NumFmtId = numFmtId,
                HorizAlign = fmt.HorizontalAlign,
                VertAlign = fmt.VerticalAlign,
                WrapText = fmt.WrapText,
                Rotation = fmt.Rotation
            };
            return GetOrAdd(_cellXfs, xf);
        }

        private int GetOrAddNumFmt(string formatCode)
        {
            int existing = _customNumFmts.IndexOf(formatCode);
            if (existing >= 0) return FirstCustomNumFmtId + existing;
            _customNumFmts.Add(formatCode);
            return FirstCustomNumFmtId + _customNumFmts.Count - 1;
        }

        private static int GetOrAdd<T>(List<T> list, T item) where T : IEquatable<T>
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i].Equals(item)) return i;
            list.Add(item);
            return list.Count - 1;
        }

        // ── XML builders ──────────────────────────────────────────────────────────

        private string BuildContentTypes(bool anyImages)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
            sb.Append("<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
            sb.Append("<Default Extension=\"xml\" ContentType=\"application/xml\"/>");
            if (anyImages)
                sb.Append("<Default Extension=\"jpg\" ContentType=\"image/jpeg\"/>");
            sb.Append("<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
            for (int s = 0; s < _sheets.Count; s++)
                sb.Append($"<Override PartName=\"/xl/worksheets/sheet{s + 1}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
            sb.Append("<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
            for (int s = 0; s < _sheets.Count; s++)
                if (_sheets[s].Images.Count > 0 || _sheets[s].Lines.Count > 0)
                    sb.Append($"<Override PartName=\"/xl/drawings/drawing{s + 1}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.drawing+xml\"/>");
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
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
                      "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");
            sb.Append("<sheets>");
            for (int s = 0; s < _sheets.Count; s++)
                sb.Append($"<sheet name=\"{XmlEscape(_sheets[s].Name)}\" sheetId=\"{s + 1}\" r:id=\"rId{s + 1}\"/>");
            sb.Append("</sheets>");
            if (_definedNames.Count > 0)
            {
                sb.Append("<definedNames>");
                foreach (var dn in _definedNames)
                    sb.Append($"<definedName name=\"{XmlEscape(dn.Name)}\">{XmlEscape(dn.Formula)}</definedName>");
                sb.Append("</definedNames>");
            }
            sb.Append("</workbook>");
            return sb.ToString();
        }

        private string BuildWorkbookRels()
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
            for (int s = 0; s < _sheets.Count; s++)
                sb.Append($"<Relationship Id=\"rId{s + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{s + 1}.xml\"/>");
            sb.Append($"<Relationship Id=\"rId{_sheets.Count + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>");
            sb.Append("</Relationships>");
            return sb.ToString();
        }

        private string BuildStyles()
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");

            // Custom number formats (built-ins, numFmtId 0-163, need no entry here)
            if (_customNumFmts.Count > 0)
            {
                sb.Append($"<numFmts count=\"{_customNumFmts.Count}\">");
                for (int i = 0; i < _customNumFmts.Count; i++)
                    sb.Append($"<numFmt numFmtId=\"{FirstCustomNumFmtId + i}\" formatCode=\"{XmlEscape(_customNumFmts[i])}\"/>");
                sb.Append("</numFmts>");
            }

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
                bool applyNumFmt = xf.NumFmtId != 0;
                bool applyAlign = !string.IsNullOrEmpty(xf.HorizAlign) || !string.IsNullOrEmpty(xf.VertAlign) || xf.WrapText || xf.Rotation != 0;

                sb.Append($"<xf numFmtId=\"{xf.NumFmtId}\" fontId=\"{xf.FontId}\" fillId=\"{xf.FillId}\" borderId=\"{xf.BorderId}\" xfId=\"0\"");
                if (applyFont) sb.Append(" applyFont=\"1\"");
                if (applyFill) sb.Append(" applyFill=\"1\"");
                if (applyBorder) sb.Append(" applyBorder=\"1\"");
                if (applyNumFmt) sb.Append(" applyNumberFormat=\"1\"");
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

        private string BuildSheet(Sheet sheet, bool hasDrawings)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
                      "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");

            // Frozen panes (sheetViews must precede cols per OOXML ordering)
            if (sheet.FrozenRows > 0 || sheet.FrozenCols > 0)
            {
                string topLeftCell = ColRef(sheet.FrozenCols) + (sheet.FrozenRows + 1);
                string activePane = sheet.FrozenRows > 0 && sheet.FrozenCols > 0 ? "bottomRight"
                    : sheet.FrozenRows > 0 ? "bottomLeft" : "topRight";
                sb.Append("<sheetViews><sheetView workbookViewId=\"0\"><pane");
                if (sheet.FrozenCols > 0) sb.Append($" xSplit=\"{sheet.FrozenCols}\"");
                if (sheet.FrozenRows > 0) sb.Append($" ySplit=\"{sheet.FrozenRows}\"");
                sb.Append($" topLeftCell=\"{topLeftCell}\" activePane=\"{activePane}\" state=\"frozen\"/>");
                sb.Append("</sheetView></sheetViews>");
            }

            // Column widths/outline/hidden state (must come before sheetData per OOXML ordering)
            var colIndices = new SortedSet<int>(sheet.ColumnWidths.Keys);
            foreach (var k in sheet.ColumnOutlineLevels.Keys) colIndices.Add(k);
            foreach (var k in sheet.HiddenColumns) colIndices.Add(k);
            if (colIndices.Count > 0)
            {
                sb.Append("<cols>");
                foreach (int colIdx in colIndices)
                {
                    int col = colIdx + 1; // 1-based in xlsx
                    sb.Append($"<col min=\"{col}\" max=\"{col}\"");
                    if (sheet.ColumnWidths.TryGetValue(colIdx, out double w))
                        sb.Append($" width=\"{FormatDouble(w)}\" customWidth=\"1\"");
                    if (sheet.ColumnOutlineLevels.TryGetValue(colIdx, out int colLvl) && colLvl > 0)
                        sb.Append($" outlineLevel=\"{colLvl}\"");
                    if (sheet.HiddenColumns.Contains(colIdx))
                        sb.Append(" hidden=\"1\"");
                    sb.Append("/>");
                }
                sb.Append("</cols>");
            }

            // Sheet data — group cells by row
            var byRow = new SortedDictionary<int, List<CellEntry>>();
            foreach (var c in sheet.Cells)
            {
                if (!byRow.TryGetValue(c.Row, out var list))
                {
                    list = new List<CellEntry>();
                    byRow[c.Row] = list;
                }
                list.Add(c);
            }

            // A row needs an element if it has cells, or just a height/outline/hidden setting.
            var rowIndices = new SortedSet<int>(byRow.Keys);
            foreach (var k in sheet.RowHeights.Keys) rowIndices.Add(k);
            foreach (var k in sheet.RowOutlineLevels.Keys) rowIndices.Add(k);
            foreach (var k in sheet.HiddenRows) rowIndices.Add(k);

            sb.Append("<sheetData>");
            foreach (int rowIdx in rowIndices)
            {
                string htAttr = sheet.RowHeights.TryGetValue(rowIdx, out double ht)
                    ? $" ht=\"{FormatDouble(ht)}\" customHeight=\"1\""
                    : "";
                string outlineAttr = sheet.RowOutlineLevels.TryGetValue(rowIdx, out int rowLvl) && rowLvl > 0
                    ? $" outlineLevel=\"{rowLvl}\""
                    : "";
                string hiddenAttr = sheet.HiddenRows.Contains(rowIdx) ? " hidden=\"1\"" : "";
                sb.Append($"<row r=\"{rowIdx + 1}\"{htAttr}{outlineAttr}{hiddenAttr}>");
                if (byRow.TryGetValue(rowIdx, out var cells))
                {
                    cells.Sort((a, b) => a.Col.CompareTo(b.Col));
                    foreach (var cell in cells)
                    {
                        string cellRef = ColRef(cell.Col) + (cell.Row + 1);
                        string styleAttr = cell.StyleIndex > 0 ? $" s=\"{cell.StyleIndex}\"" : "";
                        switch (cell.Kind)
                        {
                            case CellValueKind.Number:
                                sb.Append($"<c r=\"{cellRef}\"{styleAttr}><v>{cell.Value}</v></c>");
                                break;
                            case CellValueKind.Boolean:
                                sb.Append($"<c r=\"{cellRef}\" t=\"b\"{styleAttr}><v>{cell.Value}</v></c>");
                                break;
                            case CellValueKind.Formula:
                                string cachedValue = cell.Value != null ? $"<v>{cell.Value}</v>" : "";
                                sb.Append($"<c r=\"{cellRef}\"{styleAttr}><f>{XmlEscape(cell.Formula)}</f>{cachedValue}</c>");
                                break;
                            default:
                                sb.Append($"<c r=\"{cellRef}\" t=\"inlineStr\"{styleAttr}><is><t>{XmlEscape(cell.Value)}</t></is></c>");
                                break;
                        }
                    }
                }
                sb.Append("</row>");
            }
            sb.Append("</sheetData>");

            // Auto-filter (must follow sheetData and precede mergeCells per OOXML ordering)
            if (sheet.AutoFilterRange.HasValue)
            {
                var af = sheet.AutoFilterRange.Value;
                string ref1 = ColRef(af.c1) + (af.r1 + 1);
                string ref2 = ColRef(af.c2) + (af.r2 + 1);
                sb.Append($"<autoFilter ref=\"{ref1}:{ref2}\"/>");
            }

            // Merged cells
            if (sheet.Merges.Count > 0)
            {
                sb.Append($"<mergeCells count=\"{sheet.Merges.Count}\">");
                foreach (var m in sheet.Merges)
                {
                    string ref1 = ColRef(m.c1) + (m.r1 + 1);
                    string ref2 = ColRef(m.c2) + (m.r2 + 1);
                    sb.Append($"<mergeCell ref=\"{ref1}:{ref2}\"/>");
                }
                sb.Append("</mergeCells>");
            }

            // Hyperlinks (must follow mergeCells and precede pageMargins per OOXML ordering)
            if (sheet.ExternalHyperlinks.Count > 0 || sheet.InternalHyperlinks.Count > 0)
            {
                sb.Append("<hyperlinks>");
                int baseRid = hasDrawings ? 2 : 1;
                for (int i = 0; i < sheet.ExternalHyperlinks.Count; i++)
                {
                    var hl = sheet.ExternalHyperlinks[i];
                    string cellRef = ColRef(hl.Col) + (hl.Row + 1);
                    sb.Append($"<hyperlink ref=\"{cellRef}\" r:id=\"rId{baseRid + i}\"/>");
                }
                foreach (var hl in sheet.InternalHyperlinks)
                {
                    string cellRef = ColRef(hl.Col) + (hl.Row + 1);
                    sb.Append($"<hyperlink ref=\"{cellRef}\" location=\"{XmlEscape(hl.Location)}\"/>");
                }
                sb.Append("</hyperlinks>");
            }

            // Page setup (must follow mergeCells and precede drawing per OOXML ordering)
            sb.Append($"<pageMargins left=\"{FormatDouble(sheet.LeftMarginInches)}\" right=\"{FormatDouble(sheet.RightMarginInches)}\" " +
                      $"top=\"{FormatDouble(sheet.TopMarginInches)}\" bottom=\"{FormatDouble(sheet.BottomMarginInches)}\" " +
                      "header=\"0.5\" footer=\"0.5\"/>");
            string orient = sheet.Landscape ? "landscape" : "portrait";
            sb.Append($"<pageSetup paperSize=\"{sheet.PaperSize}\" orientation=\"{orient}\"/>");

            if (hasDrawings)
                sb.Append("<drawing r:id=\"rId1\"/>");

            sb.Append("</worksheet>");
            return sb.ToString();
        }

        // Drawing relationship (if any) is always rId1, matching the hardcoded r:id="rId1" on
        // <drawing> in BuildSheet; external hyperlinks follow at rId2, rId3, ... in list order,
        // matching the rIds BuildSheet assigns them.
        private string BuildSheetRels(int sheetNum, Sheet sheet, bool hasDrawings)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
            int rid = 1;
            if (hasDrawings)
            {
                sb.Append($"<Relationship Id=\"rId{rid}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/drawing\" Target=\"../drawings/drawing{sheetNum}.xml\"/>");
                rid++;
            }
            foreach (var hl in sheet.ExternalHyperlinks)
            {
                sb.Append($"<Relationship Id=\"rId{rid}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink\" Target=\"{XmlEscape(hl.Url)}\" TargetMode=\"External\"/>");
                rid++;
            }
            sb.Append("</Relationships>");
            return sb.ToString();
        }

        private string BuildDrawing(Sheet sheet)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<xdr:wsDr xmlns:xdr=\"http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing\" " +
                      "xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" " +
                      "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");

            int shapeId = 1;

            for (int i = 0; i < sheet.Images.Count; i++)
            {
                var img = sheet.Images[i];
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

            for (int i = 0; i < sheet.Lines.Count; i++)
            {
                var ln = sheet.Lines[i];
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

        // imageOffset: how many images earlier sheets have already claimed, so media file names
        // (which live in one shared xl/media folder) stay globally unique across all sheets.
        private string BuildDrawingRels(Sheet sheet, int imageOffset)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
            for (int i = 0; i < sheet.Images.Count; i++)
                sb.Append($"<Relationship Id=\"rId{i + 1}\" " +
                    "Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/image\" " +
                    $"Target=\"../media/image{imageOffset + i + 1}.jpg\"/>");
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
            public int FontId, FillId, BorderId, NumFmtId;
            public string HorizAlign = "general";
            public string VertAlign = "top";
            public bool WrapText;
            public int Rotation;

            public bool Equals(XfDef o) =>
                o != null && FontId == o.FontId && FillId == o.FillId &&
                BorderId == o.BorderId && NumFmtId == o.NumFmtId && HorizAlign == o.HorizAlign &&
                VertAlign == o.VertAlign && WrapText == o.WrapText && Rotation == o.Rotation;
        }

        // ── Data holders ──────────────────────────────────────────────────────────

        // Everything that is per-worksheet rather than shared at the workbook level (which is
        // only the style tables: fonts/fills/borders/cellXfs/customNumFmts stay on XlsxWriter
        // itself so equal styles are deduplicated across sheets too).
        private class Sheet
        {
            public readonly string Name;
            public readonly SortedDictionary<int, double> ColumnWidths = new SortedDictionary<int, double>();
            public readonly SortedDictionary<int, double> RowHeights = new SortedDictionary<int, double>();
            public readonly SortedDictionary<int, int> ColumnOutlineLevels = new SortedDictionary<int, int>();
            public readonly SortedDictionary<int, int> RowOutlineLevels = new SortedDictionary<int, int>();
            public readonly HashSet<int> HiddenColumns = new HashSet<int>();
            public readonly HashSet<int> HiddenRows = new HashSet<int>();
            public readonly List<CellEntry> Cells = new List<CellEntry>();
            public readonly List<(int r1, int c1, int r2, int c2)> Merges = new List<(int, int, int, int)>();
            public readonly List<ImageEntry> Images = new List<ImageEntry>();
            public readonly List<LineEntry> Lines = new List<LineEntry>();

            public double LeftMarginInches = 0.75;
            public double TopMarginInches = 1.0;
            public double RightMarginInches = 0.75;
            public double BottomMarginInches = 1.0;
            public bool Landscape;
            public int PaperSize = 9; // A4

            public int FrozenRows;
            public int FrozenCols;
            public (int r1, int c1, int r2, int c2)? AutoFilterRange;

            public readonly List<(int Row, int Col, string Url)> ExternalHyperlinks = new List<(int, int, string)>();
            public readonly List<(int Row, int Col, string Location)> InternalHyperlinks = new List<(int, int, string)>();

            public Sheet(string name) { Name = name; }
        }

        private enum CellValueKind { InlineString, Number, Boolean, Formula }

        private class CellEntry
        {
            public int Row, Col, StyleIndex;
            public CellValueKind Kind;
            public string Value; // raw text for InlineString; invariant-culture numeric text for Number/Boolean/Formula's cached value
            public string? Formula; // formula text (Formula kind only), without the leading '='
            public CellEntry(int row, int col, CellValueKind kind, string value, int style)
            { Row = row; Col = col; Kind = kind; Value = value; StyleIndex = style; }
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
