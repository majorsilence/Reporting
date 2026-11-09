using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Majorsilence.Excel
{
    public enum XlsxCellType
    {
        Blank,
        String,
        Number,
        Boolean,
        Error,
        Date,
    }

    public sealed class XlsxCellRange
    {
        public int FirstRow { get; }
        public int FirstColumn { get; }
        public int LastRow { get; }
        public int LastColumn { get; }

        public XlsxCellRange(int firstRow, int firstColumn, int lastRow, int lastColumn)
        {
            FirstRow = firstRow;
            FirstColumn = firstColumn;
            LastRow = lastRow;
            LastColumn = lastColumn;
        }
    }

    public sealed class XlsxReadCell
    {
        public int Row { get; internal set; }
        public int Column { get; internal set; }
        public XlsxCellType Type { get; internal set; }

        // The cell's text for strings/errors, or its raw stored value (invariant culture) otherwise.
        public string Text { get; internal set; } = "";
        public double? Number { get; internal set; }
        public bool? Boolean { get; internal set; }
        public DateTime? DateValue { get; internal set; }
        public string? Formula { get; internal set; }
        public string? NumberFormatCode { get; internal set; }

        /// <summary>The text Excel would show for the cell, with its number format applied (en-US conventions).</summary>
        public string DisplayText { get; internal set; } = "";
    }

    public sealed class XlsxReadSheet
    {
        private Dictionary<(int, int), XlsxReadCell>? _index;

        public string Name { get; }
        public IReadOnlyList<XlsxReadCell> Cells { get; }
        public IReadOnlyList<XlsxCellRange> Merges { get; }
        public int RowCount { get; }
        public int ColumnCount { get; }

        internal XlsxReadSheet(string name, List<XlsxReadCell> cells, List<XlsxCellRange> merges)
        {
            Name = name;
            Cells = cells;
            Merges = merges;
            RowCount = cells.Count == 0 ? 0 : cells.Max(c => c.Row) + 1;
            ColumnCount = cells.Count == 0 ? 0 : cells.Max(c => c.Column) + 1;
        }

        public XlsxReadCell? GetCell(int rowIndex, int colIndex)
        {
            if (_index == null)
            {
                var index = new Dictionary<(int, int), XlsxReadCell>();
                foreach (var c in Cells)
                    index[(c.Row, c.Column)] = c;
                _index = index;
            }
            return _index.TryGetValue((rowIndex, colIndex), out var cell) ? cell : null;
        }
    }

    /// <summary>Reads sheet names, cell values, formulas, number formats and merges from an .xlsx file.</summary>
    public sealed class XlsxReader : IDisposable
    {
        private const string OfficeDocumentRelType = "/officeDocument";
        private const string WorksheetRelType = "/worksheet";
        private const string SharedStringsRelType = "/sharedStrings";
        private const string StylesRelType = "/styles";
        private const string RelationshipsNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private const string StrictRelationshipsNs = "http://purl.oclc.org/ooxml/officeDocument/relationships";

        private static readonly DateTime Epoch1900 = new DateTime(1899, 12, 30);
        private static readonly DateTime Epoch1904 = new DateTime(1904, 1, 1);

        private static readonly Dictionary<int, string> BuiltInFormats = new Dictionary<int, string>
        {
            [0] = "General", [1] = "0", [2] = "0.00", [3] = "#,##0", [4] = "#,##0.00",
            [9] = "0%", [10] = "0.00%", [11] = "0.00E+00", [12] = "# ?/?", [13] = "# ??/??",
            [14] = "mm-dd-yy", [15] = "d-mmm-yy", [16] = "d-mmm", [17] = "mmm-yy",
            [18] = "h:mm AM/PM", [19] = "h:mm:ss AM/PM", [20] = "h:mm", [21] = "h:mm:ss",
            [22] = "m/d/yy h:mm", [37] = "#,##0 ;(#,##0)", [38] = "#,##0 ;[Red](#,##0)",
            [39] = "#,##0.00;(#,##0.00)", [40] = "#,##0.00;[Red](#,##0.00)",
            [45] = "mm:ss", [46] = "[h]:mm:ss", [47] = "mmss.0", [48] = "##0.0E+0", [49] = "@",
        };

        private readonly Stream? _ownedStream;
        private readonly ZipArchive _zip;
        private readonly Dictionary<string, ZipArchiveEntry> _entries;
        private readonly List<(string Name, string Path)> _sheets = new List<(string, string)>();
        private readonly List<string> _sharedStrings = new List<string>();
        private readonly List<string?> _cellXfFormats = new List<string?>();
        private readonly bool _date1904;

        public IReadOnlyList<string> SheetNames { get; }

        public static XlsxReader Open(string path) =>
            new XlsxReader(File.OpenRead(path), ownsStream: true);

        public static XlsxReader Open(Stream input) => new XlsxReader(input, ownsStream: false);

        private XlsxReader(Stream input, bool ownsStream)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            _ownedStream = ownsStream ? input : null;
            try
            {
                _zip = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: !ownsStream);
                _entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
                foreach (var e in _zip.Entries)
                    _entries[e.FullName.TrimStart('/')] = e;

                string workbookPath = ReadRelationships("")
                    .FirstOrDefault(r => r.Type.EndsWith(OfficeDocumentRelType, StringComparison.Ordinal)).Target
                    ?? throw new InvalidDataException("The package has no workbook part.");

                var workbookRels = ReadRelationships(workbookPath);
                var workbook = LoadXml(workbookPath)
                    ?? throw new InvalidDataException($"Workbook part '{workbookPath}' is missing.");

                var workbookPr = Children(workbook.Root!, "workbookPr").FirstOrDefault();
                _date1904 = IsTrue(workbookPr?.Attribute("date1904")?.Value);

                foreach (var sheet in Children(workbook.Root!, "sheets").SelectMany(s => Children(s, "sheet")))
                {
                    string? relId = sheet.Attributes()
                        .FirstOrDefault(a => a.Name.LocalName == "id" &&
                                             (a.Name.NamespaceName == RelationshipsNs || a.Name.NamespaceName == StrictRelationshipsNs))
                        ?.Value;
                    var rel = workbookRels.FirstOrDefault(r => r.Id == relId);
                    if (rel.Target == null || !rel.Type.EndsWith(WorksheetRelType, StringComparison.Ordinal))
                        continue; // chartsheets, dialogsheets, etc. hold no cells
                    _sheets.Add((sheet.Attribute("name")?.Value ?? "", rel.Target));
                }
                SheetNames = _sheets.Select(s => s.Name).ToList();

                string? sstPath = workbookRels.FirstOrDefault(r => r.Type.EndsWith(SharedStringsRelType, StringComparison.Ordinal)).Target;
                var sst = sstPath != null ? LoadXml(sstPath) : null;
                if (sst != null)
                {
                    foreach (var si in Children(sst.Root!, "si"))
                        _sharedStrings.Add(ReadStringItem(si));
                }

                string? stylesPath = workbookRels.FirstOrDefault(r => r.Type.EndsWith(StylesRelType, StringComparison.Ordinal)).Target;
                var styles = stylesPath != null ? LoadXml(stylesPath) : null;
                if (styles != null)
                    LoadNumberFormats(styles.Root!);
            }
            catch
            {
                _zip?.Dispose();
                _ownedStream?.Dispose();
                throw;
            }
        }

        public XlsxReadSheet ReadSheet(string name)
        {
            int index = _sheets.FindIndex(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
            if (index < 0) throw new ArgumentException($"No worksheet named '{name}'.", nameof(name));
            return ReadSheet(index);
        }

        public XlsxReadSheet ReadSheet(int sheetIndex)
        {
            if (sheetIndex < 0 || sheetIndex >= _sheets.Count)
                throw new ArgumentOutOfRangeException(nameof(sheetIndex));

            var (name, path) = _sheets[sheetIndex];
            var doc = LoadXml(path) ?? throw new InvalidDataException($"Worksheet part '{path}' is missing.");
            var cells = new List<XlsxReadCell>();
            var merges = new List<XlsxCellRange>();

            int rowIndex = -1;
            foreach (var row in Children(doc.Root!, "sheetData").SelectMany(sd => Children(sd, "row")))
            {
                rowIndex = int.TryParse(row.Attribute("r")?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int r)
                    ? r - 1
                    : rowIndex + 1;

                int colIndex = -1;
                foreach (var c in Children(row, "c"))
                {
                    if (TryParseCellRef(c.Attribute("r")?.Value, out int cr, out int cc))
                    {
                        rowIndex = cr;
                        colIndex = cc;
                    }
                    else
                    {
                        colIndex++;
                    }
                    cells.Add(ReadCell(c, rowIndex, colIndex));
                }
            }

            foreach (var mc in Children(doc.Root!, "mergeCells").SelectMany(m => Children(m, "mergeCell")))
            {
                if (TryParseRange(mc.Attribute("ref")?.Value, out var range))
                    merges.Add(range);
            }

            return new XlsxReadSheet(name, cells, merges);
        }

        public void Dispose()
        {
            _zip.Dispose();
            _ownedStream?.Dispose();
        }

        private XlsxReadCell ReadCell(XElement c, int rowIndex, int colIndex)
        {
            var cell = new XlsxReadCell { Row = rowIndex, Column = colIndex };

            if (int.TryParse(c.Attribute("s")?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int s) &&
                s >= 0 && s < _cellXfFormats.Count)
                cell.NumberFormatCode = _cellXfFormats[s];

            string? formula = Children(c, "f").FirstOrDefault()?.Value;
            if (!string.IsNullOrEmpty(formula))
                cell.Formula = formula;

            string type = c.Attribute("t")?.Value ?? "n";
            string? v = Children(c, "v").FirstOrDefault()?.Value;

            switch (type)
            {
                case "s":
                    if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idx) &&
                        idx >= 0 && idx < _sharedStrings.Count)
                    {
                        cell.Type = XlsxCellType.String;
                        cell.Text = _sharedStrings[idx];
                    }
                    break;
                case "inlineStr":
                    var inline = Children(c, "is").FirstOrDefault();
                    cell.Type = XlsxCellType.String;
                    cell.Text = inline != null ? ReadStringItem(inline) : v ?? "";
                    break;
                case "str":
                    cell.Type = XlsxCellType.String;
                    cell.Text = v ?? "";
                    break;
                case "b":
                    if (v != null)
                    {
                        cell.Type = XlsxCellType.Boolean;
                        cell.Boolean = v == "1" || string.Equals(v, "true", StringComparison.OrdinalIgnoreCase);
                        cell.Text = cell.Boolean.Value ? "TRUE" : "FALSE";
                    }
                    break;
                case "e":
                    cell.Type = XlsxCellType.Error;
                    cell.Text = v ?? "";
                    break;
                case "d":
                    if (v != null && DateTime.TryParse(v, CultureInfo.InvariantCulture,
                            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var iso))
                    {
                        cell.Type = XlsxCellType.Date;
                        cell.DateValue = iso;
                        cell.Text = v;
                    }
                    break;
                default:
                    if (v != null && double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                    {
                        cell.Number = d;
                        cell.Text = v;
                        if (IsDateFormat(cell.NumberFormatCode) && TrySerialToDate(d, out var date))
                        {
                            cell.Type = XlsxCellType.Date;
                            cell.DateValue = date;
                        }
                        else
                        {
                            cell.Type = XlsxCellType.Number;
                        }
                    }
                    break;
            }
            cell.DisplayText = DisplayTextOf(cell);
            return cell;
        }

        private string DisplayTextOf(XlsxReadCell cell)
        {
            switch (cell.Type)
            {
                case XlsxCellType.String:
                    return XlsxNumberFormatter.FormatText(cell.Text, cell.NumberFormatCode);
                case XlsxCellType.Number:
                    return XlsxNumberFormatter.Format(cell.Number!.Value, cell.NumberFormatCode, _date1904);
                case XlsxCellType.Date:
                    if (cell.Number.HasValue)
                        return XlsxNumberFormatter.Format(cell.Number.Value, cell.NumberFormatCode, _date1904);
                    // An ISO (t="d") date: show it through its format when that's a date format.
                    if (IsDateFormat(cell.NumberFormatCode))
                    {
                        double serial = (cell.DateValue!.Value - (_date1904 ? Epoch1904 : Epoch1900)).TotalDays;
                        if (serial >= 0)
                            return XlsxNumberFormatter.Format(serial, cell.NumberFormatCode, _date1904);
                    }
                    return cell.Text;
                default:
                    return cell.Text;
            }
        }

        private bool TrySerialToDate(double serial, out DateTime date)
        {
            var epoch = _date1904 ? Epoch1904 : Epoch1900;
            double ms = Math.Round(serial * 86400000.0);
            if (double.IsNaN(ms) || ms < -epoch.Ticks / TimeSpan.TicksPerMillisecond ||
                ms > (DateTime.MaxValue.Ticks - epoch.Ticks) / TimeSpan.TicksPerMillisecond)
            {
                date = default;
                return false;
            }
            date = epoch.AddMilliseconds(ms);
            return true;
        }

        private void LoadNumberFormats(XElement styleSheet)
        {
            var custom = new Dictionary<int, string>();
            foreach (var nf in Children(styleSheet, "numFmts").SelectMany(n => Children(n, "numFmt")))
            {
                if (int.TryParse(nf.Attribute("numFmtId")?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
                    custom[id] = nf.Attribute("formatCode")?.Value ?? "";
            }

            foreach (var xf in Children(styleSheet, "cellXfs").SelectMany(x => Children(x, "xf")))
            {
                string? code = null;
                if (int.TryParse(xf.Attribute("numFmtId")?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
                {
                    if (custom.TryGetValue(id, out var c)) code = c;
                    else if (BuiltInFormats.TryGetValue(id, out var b)) code = b;
                    else if (IsBuiltInLocaleDateFormatId(id)) code = "m/d/yyyy";
                }
                _cellXfFormats.Add(code);
            }
        }

        // East Asian built-in date/time format ids whose codes vary by locale.
        private static bool IsBuiltInLocaleDateFormatId(int id) =>
            (id >= 27 && id <= 36) || (id >= 50 && id <= 58);

        internal static bool IsDateFormat(string? formatCode)
        {
            if (string.IsNullOrEmpty(formatCode)) return false;

            // Only the first (positive-number) section decides how a value displays.
            var sb = new StringBuilder();
            bool inQuote = false;
            for (int i = 0; i < formatCode!.Length; i++)
            {
                char ch = formatCode[i];
                if (inQuote)
                {
                    if (ch == '"') inQuote = false;
                    continue;
                }
                switch (ch)
                {
                    case '"': inQuote = true; break;
                    case '\\':
                    case '_':
                    case '*':
                        i++; // escaped/padding character follows
                        break;
                    case '[':
                        int end = formatCode.IndexOf(']', i);
                        if (end < 0) return false;
                        string bracket = formatCode.Substring(i + 1, end - i - 1);
                        if (bracket.Length > 0 && bracket.All(b => "hHmMsS".IndexOf(b) >= 0))
                            return true; // elapsed time such as [h]:mm
                        i = end;
                        break;
                    case ';':
                        i = formatCode.Length;
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }

            string body = System.Text.RegularExpressions.Regex.Replace(sb.ToString(), "general", "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
            return body.IndexOfAny(new[] { 'd', 'D', 'm', 'M', 'y', 'Y', 'h', 'H', 's', 'S' }) >= 0;
        }

        private static string ReadStringItem(XElement item)
        {
            // Plain <t>, or the concatenated <r><t> runs; phonetic (<rPh>) text is not displayed.
            var sb = new StringBuilder();
            foreach (var child in item.Elements())
            {
                if (child.Name.LocalName == "t")
                    sb.Append(child.Value);
                else if (child.Name.LocalName == "r")
                    foreach (var t in Children(child, "t"))
                        sb.Append(t.Value);
            }
            return sb.ToString();
        }

        private List<(string Id, string Type, string Target)> ReadRelationships(string partPath)
        {
            string dir = DirectoryOf(partPath);
            string file = partPath.Substring(dir.Length);
            string relsPath = dir + "_rels/" + file + ".rels";

            var result = new List<(string, string, string)>();
            var doc = LoadXml(relsPath);
            if (doc == null) return result;

            foreach (var rel in doc.Root!.Elements().Where(e => e.Name.LocalName == "Relationship"))
            {
                if (string.Equals(rel.Attribute("TargetMode")?.Value, "External", StringComparison.OrdinalIgnoreCase))
                    continue;
                string? target = rel.Attribute("Target")?.Value;
                if (target == null) continue;
                result.Add((rel.Attribute("Id")?.Value ?? "", rel.Attribute("Type")?.Value ?? "", ResolveTarget(dir, target)));
            }
            return result;
        }

        private static string DirectoryOf(string partPath)
        {
            int slash = partPath.LastIndexOf('/');
            return slash < 0 ? "" : partPath.Substring(0, slash + 1);
        }

        private static string ResolveTarget(string sourceDir, string target)
        {
            string combined = target.StartsWith("/", StringComparison.Ordinal) ? target.Substring(1) : sourceDir + target;
            var parts = new List<string>();
            foreach (var segment in combined.Split('/'))
            {
                if (segment == "..")
                {
                    if (parts.Count > 0) parts.RemoveAt(parts.Count - 1);
                }
                else if (segment.Length > 0 && segment != ".")
                {
                    parts.Add(segment);
                }
            }
            return string.Join("/", parts);
        }

        private XDocument? LoadXml(string path)
        {
            if (!_entries.TryGetValue(path, out var entry)) return null;
            using var stream = entry.Open();
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using var reader = XmlReader.Create(stream, settings);
            return XDocument.Load(reader);
        }

        // Match on local name so both transitional and strict OOXML namespaces are accepted.
        private static IEnumerable<XElement> Children(XElement parent, string localName) =>
            parent.Elements().Where(e => e.Name.LocalName == localName);

        private static bool IsTrue(string? value) =>
            value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

        internal static bool TryParseCellRef(string? reference, out int rowIndex, out int colIndex)
        {
            rowIndex = colIndex = -1;
            if (string.IsNullOrEmpty(reference)) return false;

            int i = 0, col = 0;
            string s = reference!;
            while (i < s.Length && s[i] == '$') i++;
            int letterStart = i;
            while (i < s.Length && char.IsLetter(s[i]))
            {
                col = col * 26 + (char.ToUpperInvariant(s[i]) - 'A' + 1);
                if (col > 16384) return false;
                i++;
            }
            if (i == letterStart) return false;
            if (i < s.Length && s[i] == '$') i++;
            if (!int.TryParse(s.Substring(i), NumberStyles.None, CultureInfo.InvariantCulture, out int row) || row < 1)
                return false;

            rowIndex = row - 1;
            colIndex = col - 1;
            return true;
        }

        private static bool TryParseRange(string? reference, out XlsxCellRange range)
        {
            range = null!;
            if (string.IsNullOrEmpty(reference)) return false;
            var parts = reference!.Split(':');
            if (!TryParseCellRef(parts[0], out int r1, out int c1)) return false;
            int r2 = r1, c2 = c1;
            if (parts.Length > 1 && !TryParseCellRef(parts[1], out r2, out c2)) return false;
            range = new XlsxCellRange(Math.Min(r1, r2), Math.Min(c1, c2), Math.Max(r1, r2), Math.Max(c1, c2));
            return true;
        }
    }
}
