using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace Majorsilence.Spreadsheet
{
    public partial class XlsxWriter
    {
        private int _tableCount;
        private readonly HashSet<string> _tableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static readonly Regex TableNamePattern = new Regex(@"^[A-Za-z_\\][A-Za-z0-9_.]*$", RegexOptions.CultureInvariant);
        private static readonly Regex CellLikeName = new Regex(@"^([A-Za-z]{1,3}[0-9]+|[Rr][0-9]*[Cc][0-9]*|[RrCc])$", RegexOptions.CultureInvariant);

        /// <summary>
        /// Makes the range an Excel table: its first row is the header (column names come from those
        /// cells, which are written as text), with filter buttons and a built-in table style.
        /// </summary>
        /// <param name="name">Unique in the workbook; letters, digits, '_' and '.', not like a cell reference. Defaults to TableN.</param>
        /// <param name="styleName">A built-in style such as TableStyleLight9, TableStyleMedium2 or TableStyleDark1.</param>
        /// <returns>The table's name.</returns>
        public string AddTable(int r1, int c1, int r2, int c2, string? name = null, string styleName = "TableStyleMedium2",
            bool bandedRows = true, bool bandedColumns = false, int sheetIndex = 0)
        {
            if (r2 <= r1 || c2 < c1) throw new ArgumentException("A table needs a header row and at least one data row.");
            foreach (var other in _sheets[sheetIndex].Tables)
                if (r1 <= other.R2 && r2 >= other.R1 && c1 <= other.C2 && c2 >= other.C1)
                    throw new ArgumentException($"The range overlaps table '{other.Name}'.");

            int id = ++_tableCount;
            if (name == null)
            {
                int n = id;
                while (_tableNames.Contains("Table" + n)) n++;
                name = "Table" + n;
            }
            else if (!TableNamePattern.IsMatch(name) || CellLikeName.IsMatch(name) || name.Length > 255)
                throw new ArgumentException($"'{name}' is not a valid table name.", nameof(name));
            if (!_tableNames.Add(name))
                throw new ArgumentException($"A table named '{name}' already exists.", nameof(name));

            _sheets[sheetIndex].Tables.Add(new TableEntry(id, name, r1, c1, r2, c2, styleName ?? "TableStyleMedium2", bandedRows, bandedColumns));
            return name;
        }

        // Header cells must hold text matching the column names; non-text or missing ones are rewritten.
        private void PrepareTables()
        {
            foreach (var sheet in _sheets)
            {
                foreach (var table in sheet.Tables)
                {
                    var names = new List<string>();
                    var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    for (int c = table.C1; c <= table.C2; c++)
                    {
                        var cell = sheet.Cells.FindLast(e => e.Row == table.R1 && e.Col == c);
                        string text = cell == null ? "" : HeaderText(cell);
                        if (text.Length == 0) text = "Column" + (c - table.C1 + 1);
                        string unique = text;
                        for (int k = 2; !used.Add(unique); k++) unique = text + k.ToString(CultureInfo.InvariantCulture);
                        names.Add(unique);

                        bool isText = cell != null && (cell.Kind == CellValueKind.InlineString || cell.Kind == CellValueKind.SharedString);
                        if (!isText || HeaderText(cell!) != unique)
                        {
                            int style = cell?.StyleIndex ?? 0;
                            sheet.Cells.RemoveAll(e => e.Row == table.R1 && e.Col == c);
                            sheet.Cells.Add(new CellEntry(table.R1, c, CellValueKind.InlineString, unique, style));
                        }
                    }
                    table.ColumnNames = names;
                }
            }
        }

        private string HeaderText(CellEntry cell)
        {
            switch (cell.Kind)
            {
                case CellValueKind.SharedString:
                    return _sharedStrings[int.Parse(cell.Value, CultureInfo.InvariantCulture)];
                case CellValueKind.RichText:
                    var sb = new StringBuilder();
                    foreach (var run in cell.RichTextRuns!) sb.Append(run.Text);
                    return sb.ToString();
                case CellValueKind.Boolean:
                    return cell.Value == "1" ? "TRUE" : "FALSE";
                case CellValueKind.Formula:
                    return cell.Value ?? "";
                default:
                    return cell.Value ?? "";
            }
        }

        private static int TablesFirstRelId(Sheet sheet, bool hasDrawings) =>
            CommentsFirstRelId(sheet, hasDrawings) + (sheet.Comments.Count > 0 ? 2 : 0);

        private static int AppendTableRels(StringBuilder sb, Sheet sheet, int rid)
        {
            foreach (var t in sheet.Tables)
                sb.Append($"<Relationship Id=\"rId{rid++}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/table\" Target=\"../tables/table{t.Id}.xml\"/>");
            return rid;
        }

        private static void AppendTableParts(StringBuilder sb, Sheet sheet, bool hasDrawings)
        {
            if (sheet.Tables.Count == 0) return;
            int rid = TablesFirstRelId(sheet, hasDrawings);
            sb.Append($"<tableParts count=\"{sheet.Tables.Count}\">");
            foreach (var _ in sheet.Tables)
                sb.Append($"<tablePart r:id=\"rId{rid++}\"/>");
            sb.Append("</tableParts>");
        }

        private void AppendTableContentTypes(StringBuilder sb)
        {
            foreach (var sheet in _sheets)
                foreach (var t in sheet.Tables)
                    sb.Append($"<Override PartName=\"/xl/tables/table{t.Id}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.table+xml\"/>");
        }

        private static void WriteTableParts(ZipArchive zip, Sheet sheet)
        {
            foreach (var t in sheet.Tables)
            {
                string reference = $"{ColRef(t.C1)}{t.R1 + 1}:{ColRef(t.C2)}{t.R2 + 1}";
                var sb = new StringBuilder();
                sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                sb.Append($"<table xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" id=\"{t.Id}\" " +
                          $"name=\"{XmlEscape(t.Name)}\" displayName=\"{XmlEscape(t.Name)}\" ref=\"{reference}\" totalsRowShown=\"0\">");
                sb.Append($"<autoFilter ref=\"{reference}\"/>");
                sb.Append($"<tableColumns count=\"{t.ColumnNames!.Count}\">");
                for (int i = 0; i < t.ColumnNames.Count; i++)
                    sb.Append($"<tableColumn id=\"{i + 1}\" name=\"{XmlEscape(t.ColumnNames[i])}\"/>");
                sb.Append("</tableColumns>");
                sb.Append($"<tableStyleInfo name=\"{XmlEscape(t.StyleName)}\" showFirstColumn=\"0\" showLastColumn=\"0\" " +
                          $"showRowStripes=\"{(t.BandedRows ? 1 : 0)}\" showColumnStripes=\"{(t.BandedColumns ? 1 : 0)}\"/>");
                sb.Append("</table>");
                WriteText(zip, $"xl/tables/table{t.Id}.xml", sb.ToString());
            }
        }

        private class TableEntry
        {
            public readonly int Id, R1, C1, R2, C2;
            public readonly string Name, StyleName;
            public readonly bool BandedRows, BandedColumns;
            public List<string>? ColumnNames;

            public TableEntry(int id, string name, int r1, int c1, int r2, int c2, string styleName, bool bandedRows, bool bandedColumns)
            {
                Id = id; Name = name; R1 = r1; C1 = c1; R2 = r2; C2 = c2;
                StyleName = styleName; BandedRows = bandedRows; BandedColumns = bandedColumns;
            }
        }

        private partial class Sheet
        {
            public readonly List<TableEntry> Tables = new List<TableEntry>();
        }
    }
}
