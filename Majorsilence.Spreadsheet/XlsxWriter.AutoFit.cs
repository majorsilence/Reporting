using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Majorsilence.Spreadsheet
{
    public partial class XlsxWriter
    {
        /// <summary>
        /// Sets each column's width from the cells written to it so far, approximating Excel's AutoFit:
        /// the widest displayed value (numbers and dates through their number format, the longest line
        /// of multi-line text), scaled by the cell's font size and weight. Cells merged across columns
        /// and rotated text are not measured. Columns with nothing to measure keep their width.
        /// </summary>
        /// <param name="minWidth">Smallest width to set, in characters.</param>
        /// <param name="maxWidth">Largest width to set, in characters (Excel's limit is 255).</param>
        public void AutoFitColumns(int sheetIndex = 0, double minWidth = 0, double maxWidth = 100)
        {
            var sheet = _sheets[sheetIndex];
            var mergedAcross = new HashSet<(int, int)>();
            foreach (var m in sheet.Merges)
                if (m.c2 > m.c1)
                    for (int r = m.r1; r <= m.r2; r++)
                        for (int c = m.c1; c <= m.c2; c++)
                            mergedAcross.Add((r, c));

            var widths = new Dictionary<int, double>();
            foreach (var cell in sheet.Cells)
            {
                if (mergedAcross.Contains((cell.Row, cell.Col))) continue;
                var xf = _cellXfs[cell.StyleIndex];
                if (xf.Rotation != 0) continue;
                var font = _fonts[xf.FontId];

                string text = DisplayTextOf(cell, xf);
                if (text.Length == 0) continue;
                double width = 0;
                foreach (var line in text.Split('\n'))
                    width = Math.Max(width, MeasureChars(line));
                width = width * font.SizePt / 11.0 * (font.Bold ? 1.07 : 1.0) + 1.0; // cell padding

                if (!widths.TryGetValue(cell.Col, out double current) || width > current)
                    widths[cell.Col] = width;
            }

            foreach (var kv in widths)
                sheet.ColumnWidths[kv.Key] = Math.Round(Math.Min(Math.Max(kv.Value, minWidth), Math.Min(maxWidth, 255)), 2);
        }

        private string DisplayTextOf(CellEntry cell, XfDef xf)
        {
            string? code = xf.NumFmtId >= FirstCustomNumFmtId ? _customNumFmts[xf.NumFmtId - FirstCustomNumFmtId] : null;
            switch (cell.Kind)
            {
                case CellValueKind.Number:
                    return double.TryParse(cell.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)
                        ? XlsxNumberFormatter.Format(d, code)
                        : cell.Value;
                case CellValueKind.Formula:
                    return cell.Value != null && double.TryParse(cell.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double f)
                        ? XlsxNumberFormatter.Format(f, code)
                        : "";
                case CellValueKind.Boolean:
                    return cell.Value == "1" ? "TRUE" : "FALSE";
                case CellValueKind.SharedString:
                    return XlsxNumberFormatter.FormatText(_sharedStrings[int.Parse(cell.Value, CultureInfo.InvariantCulture)], code);
                case CellValueKind.RichText:
                    var sb = new StringBuilder();
                    foreach (var run in cell.RichTextRuns!) sb.Append(run.Text);
                    return sb.ToString();
                default:
                    return XlsxNumberFormatter.FormatText(cell.Value ?? "", code);
            }
        }

        // Width in Excel character units (the default font's digit width), from rough glyph classes.
        private static double MeasureChars(string text)
        {
            double w = 0;
            foreach (char ch in text)
            {
                if (ch >= '⺀') w += 1.8;                       // CJK and other full-width scripts
                else if ("iIljt.,:;!|'`f r()[]".IndexOf(ch) >= 0) w += 0.5;
                else if ("MWmw@%".IndexOf(ch) >= 0) w += 1.35;
                else if (char.IsUpper(ch)) w += 1.1;
                else if (char.IsLower(ch)) w += 0.9;
                else w += 1.0;                                      // digits are the unit
            }
            return w;
        }
    }
}
