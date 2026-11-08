using System;
using System.Collections.Generic;
using Majorsilence.Excel;
using Majorsilence.Reporting.Rdl;

namespace RdlEngine.Render.ExcelConverter
{
    /// <summary>Writes builder cells to the workbook, typed when the builder found a typed value.</summary>
    internal sealed class ExcelCellWriter
    {
        private readonly XlsxWriter _workbook;
        private readonly bool _styled;

        // Cells with a constant style share one StyleInfo instance, so each style/number format pair is made once.
        private readonly Dictionary<StyleInfo, Dictionary<string, XlsxCellFormat>> _formats =
            new Dictionary<StyleInfo, Dictionary<string, XlsxCellFormat>>(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<string, XlsxCellFormat> _unstyledFormats = new Dictionary<string, XlsxCellFormat>();

        public ExcelCellWriter(XlsxWriter workbook, bool styled)
        {
            _workbook = workbook;
            _styled = styled;
        }

        public void Write(int rowIndex, int colIndex, ExcelCell cell)
        {
            string code = cell.TypedValue != null ? cell.NumberFormatCode : null;
            var format = FormatFor(cell.Style, code);
            switch (cell.TypedValue)
            {
                case double number:
                    _workbook.SetCell(rowIndex, colIndex, number, format);
                    break;
                case bool boolean:
                    _workbook.SetCell(rowIndex, colIndex, boolean, format);
                    break;
                case DateTime date:
                    _workbook.SetCell(rowIndex, colIndex, date, format);
                    break;
                default:
                    _workbook.SetCell(rowIndex, colIndex, cell.Value, format);
                    break;
            }
        }

        private XlsxCellFormat FormatFor(StyleInfo style, string numberFormatCode)
        {
            string key = numberFormatCode ?? "";
            if (!_styled || style == null)
            {
                if (numberFormatCode == null) return null;
                if (!_unstyledFormats.TryGetValue(key, out var plain))
                    _unstyledFormats[key] = plain = new XlsxCellFormat { NumberFormatCode = numberFormatCode };
                return plain;
            }

            if (!_formats.TryGetValue(style, out var byCode))
                _formats[style] = byCode = new Dictionary<string, XlsxCellFormat>();
            if (!byCode.TryGetValue(key, out var format))
            {
                format = new ExcelCellStyle(style).ToXlsxFormat();
                format.NumberFormatCode = numberFormatCode;
                byCode[key] = format;
            }
            return format;
        }
    }
}
