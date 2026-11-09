using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Majorsilence.Spreadsheet
{
    /// <summary>
    /// Writes an .xlsx with bounded memory: each row goes to the sheet part as it is added, so
    /// rows (and cells within a row) must be written in increasing order. Column widths and
    /// frozen panes must be set before a sheet's first row. Supports text (inline), numbers,
    /// booleans, dates, formulas, cell formats, merges, AutoFilter and print setup; use
    /// <see cref="XlsxWriter"/> for images, comments, tables, validation and the other features.
    /// </summary>
    public sealed class XlsxStreamWriter : IDisposable
    {
        private static readonly DateTime ExcelEpoch = new DateTime(1899, 12, 30);
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        private readonly XlsxWriter _styles = new XlsxWriter("styles"); // holds the shared style tables only
        private readonly ZipArchive _zip;
        private readonly List<string> _sheetNames = new List<string>();
        private XlsxCellFormat? _defaultDate, _defaultDateTime;

        private StreamWriter? _sheet;
        private bool _sheetDataStarted;
        private bool _rowOpen;
        private int _lastRow = -1;
        private int _lastCol = -1;
        private readonly SortedDictionary<int, double> _columnWidths = new SortedDictionary<int, double>();
        private int _frozenRows, _frozenCols;
        private readonly List<(int r1, int c1, int r2, int c2)> _merges = new List<(int, int, int, int)>();
        private (int r1, int c1, int r2, int c2)? _autoFilter;
        private (double l, double t, double r, double b, bool landscape) _print = (0.75, 1.0, 0.75, 1.0, false);
        private bool _finished;

        public XlsxStreamWriter(Stream output, string firstSheetName)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            _zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
            StartSheet(firstSheetName);
        }

        /// <summary>Finishes the current sheet and starts a new one; returns its index.</summary>
        public int StartSheet(string name)
        {
            ThrowIfFinished();
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("A sheet needs a name.", nameof(name));
            FinishSheet();
            _sheetNames.Add(name);
            var entry = _zip.CreateEntry($"xl/worksheets/sheet{_sheetNames.Count}.xml", CompressionLevel.Optimal);
            _sheet = new StreamWriter(entry.Open(), Utf8NoBom, 1 << 16);
            _sheet.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            _sheet.Write("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
                         "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");
            _sheetDataStarted = false;
            _rowOpen = false;
            _lastRow = _lastCol = -1;
            _columnWidths.Clear();
            _frozenRows = _frozenCols = 0;
            _merges.Clear();
            _autoFilter = null;
            _print = (0.75, 1.0, 0.75, 1.0, false);
            return _sheetNames.Count - 1;
        }

        public void SetColumnWidth(int colIndex, double widthInChars)
        {
            ThrowIfRowsStarted(nameof(SetColumnWidth));
            _columnWidths[colIndex] = widthInChars;
        }

        public void FreezePanes(int frozenRows, int frozenCols)
        {
            ThrowIfRowsStarted(nameof(FreezePanes));
            _frozenRows = Math.Max(0, frozenRows);
            _frozenCols = Math.Max(0, frozenCols);
        }

        public void AddMerge(int r1, int c1, int r2, int c2) { ThrowIfFinished(); _merges.Add((r1, c1, r2, c2)); }

        public void SetAutoFilter(int r1, int c1, int r2, int c2) { ThrowIfFinished(); _autoFilter = (r1, c1, r2, c2); }

        public void SetPrintSetup(double leftIn, double topIn, double rightIn, double bottomIn, bool landscape)
        {
            ThrowIfFinished();
            _print = (leftIn, topIn, rightIn, bottomIn, landscape);
        }

        /// <summary>Starts a row; rows must be started in increasing order. The previous row is closed.</summary>
        public void StartRow(int rowIndex, double? heightInPoints = null, bool hidden = false, int outlineLevel = 0)
        {
            ThrowIfFinished();
            if (rowIndex <= _lastRow)
                throw new InvalidOperationException($"Row {rowIndex} must come after row {_lastRow}.");
            if (!_sheetDataStarted) WriteSheetHeader();
            if (_rowOpen) _sheet!.Write("</row>");

            _sheet!.Write("<row r=\"");
            _sheet.Write((rowIndex + 1).ToString(CultureInfo.InvariantCulture));
            _sheet.Write('"');
            if (heightInPoints.HasValue)
                _sheet.Write($" ht=\"{XlsxWriter.FormatDouble(heightInPoints.Value)}\" customHeight=\"1\"");
            if (outlineLevel > 0) _sheet.Write($" outlineLevel=\"{outlineLevel}\"");
            if (hidden) _sheet.Write(" hidden=\"1\"");
            _sheet.Write('>');
            _rowOpen = true;
            _lastRow = rowIndex;
            _lastCol = -1;
        }

        public void WriteCell(int colIndex, string value, XlsxCellFormat? format = null)
        {
            BeginCell(colIndex, format, " t=\"inlineStr\"");
            _sheet!.Write("<is><t xml:space=\"preserve\">");
            _sheet.Write(XlsxWriter.XmlEscape(value ?? ""));
            _sheet.Write("</t></is></c>");
        }

        public void WriteCell(int colIndex, double value, XlsxCellFormat? format = null)
        {
            BeginCell(colIndex, format, "");
            _sheet!.Write("<v>");
            _sheet.Write(XlsxWriter.FormatDouble(value));
            _sheet.Write("</v></c>");
        }

        public void WriteCell(int colIndex, bool value, XlsxCellFormat? format = null)
        {
            BeginCell(colIndex, format, " t=\"b\"");
            _sheet!.Write(value ? "<v>1</v></c>" : "<v>0</v></c>");
        }

        /// <summary>Writes a date serial; without a format it gets a short date (or date and time) format, as XlsxWriter does.</summary>
        public void WriteCell(int colIndex, DateTime value, XlsxCellFormat? format = null)
            => WriteCell(colIndex, (value - ExcelEpoch).TotalDays, DateFormatFor(format, value));

        public void WriteFormula(int colIndex, string formula, double? cachedValue = null, XlsxCellFormat? format = null)
        {
            if (formula == null) throw new ArgumentNullException(nameof(formula));
            BeginCell(colIndex, format, "");
            string f = formula.Length > 0 && formula[0] == '=' ? formula.Substring(1) : formula;
            _sheet!.Write("<f>");
            _sheet.Write(XlsxWriter.XmlEscape(f));
            _sheet.Write("</f>");
            if (cachedValue.HasValue) _sheet.Write($"<v>{XlsxWriter.FormatDouble(cachedValue.Value)}</v>");
            _sheet.Write("</c>");
        }

        /// <summary>Finishes the workbook. Disposing does the same; the output stream is left open.</summary>
        public void Finish()
        {
            if (_finished) return;
            FinishSheet();
            _finished = true;

            Write("xl/styles.xml", _styles.BuildStyles());
            var wb = new StringBuilder();
            wb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            wb.Append("<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
                      "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>");
            for (int i = 0; i < _sheetNames.Count; i++)
                wb.Append($"<sheet name=\"{XlsxWriter.XmlEscape(_sheetNames[i])}\" sheetId=\"{i + 1}\" r:id=\"rId{i + 1}\"/>");
            wb.Append("</sheets></workbook>");
            Write("xl/workbook.xml", wb.ToString());

            var rels = new StringBuilder();
            rels.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            rels.Append("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
            for (int i = 0; i < _sheetNames.Count; i++)
                rels.Append($"<Relationship Id=\"rId{i + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i + 1}.xml\"/>");
            rels.Append($"<Relationship Id=\"rId{_sheetNames.Count + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>");
            rels.Append("</Relationships>");
            Write("xl/_rels/workbook.xml.rels", rels.ToString());

            Write("_rels/.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                "</Relationships>");

            var ct = new StringBuilder();
            ct.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            ct.Append("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
            ct.Append("<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
            ct.Append("<Default Extension=\"xml\" ContentType=\"application/xml\"/>");
            ct.Append("<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
            for (int i = 0; i < _sheetNames.Count; i++)
                ct.Append($"<Override PartName=\"/xl/worksheets/sheet{i + 1}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
            ct.Append("<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
            ct.Append("</Types>");
            Write("[Content_Types].xml", ct.ToString());

            _zip.Dispose();
        }

        public void Dispose() => Finish();

        private void BeginCell(int colIndex, XlsxCellFormat? format, string typeAttr)
        {
            ThrowIfFinished();
            if (!_rowOpen) throw new InvalidOperationException("Call StartRow before writing cells.");
            if (colIndex <= _lastCol)
                throw new InvalidOperationException($"Column {colIndex} must come after column {_lastCol} in a row.");
            _lastCol = colIndex;

            int style = format == null ? 0 : _styles.ResolveStyleIndex(format);
            _sheet!.Write("<c r=\"");
            _sheet.Write(XlsxWriter.ColRef(colIndex));
            _sheet.Write((_lastRow + 1).ToString(CultureInfo.InvariantCulture));
            _sheet.Write('"');
            if (style > 0) _sheet.Write($" s=\"{style}\"");
            _sheet.Write(typeAttr);
            _sheet.Write('>');
        }

        private XlsxCellFormat DateFormatFor(XlsxCellFormat? format, DateTime value)
        {
            if (format != null) return format;
            return value.TimeOfDay != TimeSpan.Zero
                ? _defaultDateTime ??= new XlsxCellFormat { NumberFormatCode = XlsxNumberFormats.ShortDateTime }
                : _defaultDate ??= new XlsxCellFormat { NumberFormatCode = XlsxNumberFormats.ShortDate };
        }

        private void WriteSheetHeader()
        {
            var w = _sheet!;
            if (_frozenRows > 0 || _frozenCols > 0)
            {
                string topLeft = XlsxWriter.ColRef(_frozenCols) + (_frozenRows + 1);
                string pane = _frozenRows > 0 && _frozenCols > 0 ? "bottomRight" : _frozenRows > 0 ? "bottomLeft" : "topRight";
                w.Write("<sheetViews><sheetView workbookViewId=\"0\"><pane");
                if (_frozenCols > 0) w.Write($" xSplit=\"{_frozenCols}\"");
                if (_frozenRows > 0) w.Write($" ySplit=\"{_frozenRows}\"");
                w.Write($" topLeftCell=\"{topLeft}\" activePane=\"{pane}\" state=\"frozen\"/></sheetView></sheetViews>");
            }
            if (_columnWidths.Count > 0)
            {
                w.Write("<cols>");
                foreach (var kv in _columnWidths)
                    w.Write($"<col min=\"{kv.Key + 1}\" max=\"{kv.Key + 1}\" width=\"{XlsxWriter.FormatDouble(kv.Value)}\" customWidth=\"1\"/>");
                w.Write("</cols>");
            }
            w.Write("<sheetData>");
            _sheetDataStarted = true;
        }

        private void FinishSheet()
        {
            if (_sheet == null) return;
            if (!_sheetDataStarted) WriteSheetHeader();
            var w = _sheet;
            if (_rowOpen) w.Write("</row>");
            w.Write("</sheetData>");
            if (_autoFilter.HasValue)
            {
                var a = _autoFilter.Value;
                w.Write($"<autoFilter ref=\"{XlsxWriter.ColRef(a.c1)}{a.r1 + 1}:{XlsxWriter.ColRef(a.c2)}{a.r2 + 1}\"/>");
            }
            if (_merges.Count > 0)
            {
                w.Write($"<mergeCells count=\"{_merges.Count}\">");
                foreach (var m in _merges)
                    w.Write($"<mergeCell ref=\"{XlsxWriter.ColRef(m.c1)}{m.r1 + 1}:{XlsxWriter.ColRef(m.c2)}{m.r2 + 1}\"/>");
                w.Write("</mergeCells>");
            }
            w.Write($"<pageMargins left=\"{XlsxWriter.FormatDouble(_print.l)}\" right=\"{XlsxWriter.FormatDouble(_print.r)}\" " +
                    $"top=\"{XlsxWriter.FormatDouble(_print.t)}\" bottom=\"{XlsxWriter.FormatDouble(_print.b)}\" header=\"0.5\" footer=\"0.5\"/>");
            w.Write($"<pageSetup paperSize=\"9\" orientation=\"{(_print.landscape ? "landscape" : "portrait")}\"/>");
            w.Write("</worksheet>");
            w.Dispose();
            _sheet = null;
            _rowOpen = false;
        }

        private void Write(string path, string content)
        {
            var entry = _zip.CreateEntry(path, CompressionLevel.Optimal);
            using var s = new StreamWriter(entry.Open(), Utf8NoBom);
            s.Write(content);
        }

        private void ThrowIfRowsStarted(string member)
        {
            ThrowIfFinished();
            if (_sheetDataStarted)
                throw new InvalidOperationException($"{member} must be called before the sheet's first row.");
        }

        private void ThrowIfFinished()
        {
            if (_finished) throw new ObjectDisposedException(nameof(XlsxStreamWriter));
        }
    }
}
