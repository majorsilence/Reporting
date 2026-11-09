using System;
using System.IO;
using Majorsilence.Spreadsheet;

namespace RdlEngine.Render.ExcelConverter
{
    /// <summary>What the Excel renderers write to: an .xlsx or a legacy .xls workbook.</summary>
    internal interface IExcelWorkbook
    {
        void SetPrintSetup(double leftIn, double topIn, double rightIn, double bottomIn, bool landscape);
        void SetColumnWidth(int colIndex, double widthInChars);
        void SetRowHeight(int rowIndex, double heightInPoints);
        void AddMerge(int r1, int c1, int r2, int c2);
        void SetCell(int rowIndex, int colIndex, string value, XlsxCellFormat format);
        void SetCell(int rowIndex, int colIndex, double value, XlsxCellFormat format);
        void SetCell(int rowIndex, int colIndex, bool value, XlsxCellFormat format);
        void SetCell(int rowIndex, int colIndex, DateTime value, XlsxCellFormat format);
        void FreezePanes(int frozenRows, int frozenCols);
        void SetAutoFilter(int r1, int c1, int r2, int c2);
        void AddImage(byte[] data, long posXEmu, long posYEmu, long widthEmu, long heightEmu);
        void AddLine(long x1Emu, long y1Emu, long x2Emu, long y2Emu, double lineWidthPt);
        void Write(Stream output);
    }

    internal sealed class XlsxWorkbook : IExcelWorkbook
    {
        private readonly XlsxWriter _w;
        public XlsxWorkbook(string sheetName) { _w = new XlsxWriter(sheetName); }

        public void SetPrintSetup(double l, double t, double r, double b, bool landscape) => _w.SetPrintSetup(l, t, r, b, landscape);
        public void SetColumnWidth(int c, double w) => _w.SetColumnWidth(c, w);
        public void SetRowHeight(int r, double h) => _w.SetRowHeight(r, h);
        public void AddMerge(int r1, int c1, int r2, int c2) => _w.AddMerge(r1, c1, r2, c2);
        public void SetCell(int r, int c, string v, XlsxCellFormat f) => _w.SetCell(r, c, v, f);
        public void SetCell(int r, int c, double v, XlsxCellFormat f) => _w.SetCell(r, c, v, f);
        public void SetCell(int r, int c, bool v, XlsxCellFormat f) => _w.SetCell(r, c, v, f);
        public void SetCell(int r, int c, DateTime v, XlsxCellFormat f) => _w.SetCell(r, c, v, f);
        public void FreezePanes(int rows, int cols) => _w.FreezePanes(rows, cols);
        public void SetAutoFilter(int r1, int c1, int r2, int c2) => _w.SetAutoFilter(r1, c1, r2, c2);
        public void AddImage(byte[] d, long x, long y, long w, long h) => _w.AddImage(d, x, y, w, h);
        public void AddLine(long x1, long y1, long x2, long y2, double lw) => _w.AddLine(x1, y1, x2, y2, lw);
        public void Write(Stream output) => _w.Write(output);
    }

    /// <summary>
    /// Excel 97-2003 (.xls). BIFF8 has no room for what's past row 65,536 or column 256, and this
    /// writer has no drawings or filters, so those are left out with a warning rather than failing the render.
    /// </summary>
    internal sealed class XlsWorkbook : IExcelWorkbook
    {
        private readonly XlsWriter _w;
        private readonly Action<string> _warn;
        private bool _warnedLimits, _warnedDrawings;

        public XlsWorkbook(string sheetName, Action<string> warn)
        {
            _w = new XlsWriter(sheetName);
            _warn = warn;
        }

        public void SetPrintSetup(double l, double t, double r, double b, bool landscape) => _w.SetPrintSetup(l, t, r, b, landscape);

        public void SetColumnWidth(int c, double w) { if (Fits(0, c)) _w.SetColumnWidth(c, w); }
        public void SetRowHeight(int r, double h) { if (Fits(r, 0)) _w.SetRowHeight(r, h); }

        public void AddMerge(int r1, int c1, int r2, int c2)
        {
            if (Fits(r2, c2)) _w.AddMerge(r1, c1, r2, c2);
        }

        public void SetCell(int r, int c, string v, XlsxCellFormat f) { if (Fits(r, c)) _w.SetCell(r, c, v, f); }
        public void SetCell(int r, int c, double v, XlsxCellFormat f) { if (Fits(r, c)) _w.SetCell(r, c, v, f); }
        public void SetCell(int r, int c, bool v, XlsxCellFormat f) { if (Fits(r, c)) _w.SetCell(r, c, v, f); }
        public void SetCell(int r, int c, DateTime v, XlsxCellFormat f) { if (Fits(r, c)) _w.SetCell(r, c, v, f); }

        public void FreezePanes(int rows, int cols) => _w.FreezePanes(rows, cols);
        public void SetAutoFilter(int r1, int c1, int r2, int c2) { }

        public void AddImage(byte[] d, long x, long y, long w, long h) => WarnDrawings();
        public void AddLine(long x1, long y1, long x2, long y2, double lw) => WarnDrawings();

        public void Write(Stream output) => _w.Write(output);

        private bool Fits(int row, int col)
        {
            if (row < XlsWriter.MaxRows && col < XlsWriter.MaxColumns) return true;
            if (!_warnedLimits)
            {
                _warnedLimits = true;
                _warn($"Excel 97-2003 (.xls) holds {XlsWriter.MaxRows:N0} rows and {XlsWriter.MaxColumns} columns; the rest of the report was left out. Use .xlsx for larger reports.");
            }
            return false;
        }

        private void WarnDrawings()
        {
            if (_warnedDrawings) return;
            _warnedDrawings = true;
            _warn("Images, charts and lines are not included in Excel 97-2003 (.xls) output.");
        }
    }
}
