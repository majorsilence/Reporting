using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using RdlEngine.Render.ExcelConverter;

namespace Majorsilence.Reporting.Rdl
{
    ///<summary>
    /// Renders a report to Excel 2007 (data values only, no rich styling).
    ///</summary>
    internal class RenderExcel2007DataOnly : IPresent
    {
        Report report;
        IStreamGen _sg;

        ExcelCellsBuilder excelBuilder = new ExcelCellsBuilder();
        XlsxWriter workbook;
        double k = 5.637142013; // points per Excel character width

        public RenderExcel2007DataOnly(Report rep, IStreamGen sg)
        {
            report = rep;
            _sg = sg;

            excelBuilder.Report = report;
            string sheetName = string.IsNullOrEmpty(rep.Name) ? "NewSheet" : rep.Name;
            workbook = new XlsxWriter(sheetName);

            double leftIn = rep.LeftMarginPoints / 72.0;
            double topIn = rep.TopMarginPoints / 72.0;
            double rightIn = rep.RightMarginPoints / 72.0;
            double bottomIn = rep.BottomMarginPoints / 72.0;
            workbook.SetPrintSetup(leftIn, topIn, rightIn, bottomIn, landscape: true);
        }

        protected IStreamGen StreamGen { get => _sg; set => _sg = value; }

        public void Dispose() { }

        public Report Report() => report;

        public bool IsPagingNeeded() => false;

        public void Start() { }

        public virtual Task End()
        {
            excelBuilder.CellsCorrection();

            for (int i = 0; i < excelBuilder.Columns.Count; i++)
            {
                if (i + 1 < excelBuilder.Columns.Count)
                {
                    double widthInPoints = excelBuilder.Columns[i + 1].XPosition - excelBuilder.Columns[i].XPosition;
                    double widthInChars = widthInPoints / k;
                    workbook.SetColumnWidth(i, widthInChars);
                }
            }

            for (int i = 0; i < excelBuilder.Rows.Count - 1; i++)
            {
                double rowHeight = excelBuilder.Rows[i + 1].YPosition - excelBuilder.Rows[i].YPosition;
                workbook.SetRowHeight(i, rowHeight);
            }

            for (int i = 0; i < excelBuilder.Rows.Count - 1; i++)
            {
                var builderRow = excelBuilder.Rows[i];

                for (int j = 0; j < builderRow.Cells.Count; j++)
                {
                    var builderCell = builderRow.Cells[j];
                    var columnIndex = excelBuilder.Columns.IndexOf(builderCell.Column);

                    if (builderCell.ReportItem != null)
                    {
                        var rightAttach = excelBuilder.GetRightAttachCells(builderCell);
                        var bottomAttach = excelBuilder.GetBottomAttachCells(builderCell);

                        var rowIndex = excelBuilder.Rows.IndexOf(builderCell.Row);
                        var colIndex = excelBuilder.Columns.IndexOf(builderCell.Column);

                        if (rightAttach > 0 || bottomAttach > 0)
                            workbook.AddMerge(rowIndex, colIndex, rowIndex + bottomAttach, colIndex + rightAttach);

                        workbook.SetCell(rowIndex, colIndex, builderCell.Value);
                    }
                }
            }

            // Images
            foreach (var image in excelBuilder.Images)
            {
                long posX = (long)(image.AbsoluteLeft * XlsxUnits.EmuPerPoint);
                long posY = (long)(image.AbsoluteTop * XlsxUnits.EmuPerPoint);
                long cx = (long)(image.ImageWidth * XlsxUnits.EmuPerPoint);
                long cy = (long)(image.ImageHeight * XlsxUnits.EmuPerPoint);
                workbook.AddImage(image.Data, posX, posY, cx, cy);
            }

            // Lines
            foreach (var line in excelBuilder.Lines)
            {
                long x1 = (long)(line.AbsoluteLeft * XlsxUnits.EmuPerPoint);
                long y1 = (long)(line.AbsoluteTop * XlsxUnits.EmuPerPoint);
                long x2 = (long)(line.Right * XlsxUnits.EmuPerPoint);
                long y2 = (long)(line.Bottom * XlsxUnits.EmuPerPoint);
                workbook.AddLine(x1, y1, x2, y2, line.BorderWidth);
            }

            workbook.Write(_sg.GetStream());
            return Task.CompletedTask;
        }

        public void BodyStart(Body b) { }
        public void BodyEnd(Body b) { }
        public void PageHeaderStart(PageHeader ph) { }
        public void PageHeaderEnd(PageHeader ph) { }
        public void PageFooterStart(PageFooter pf) { }
        public void PageFooterEnd(PageFooter pf) { }

        public async Task Textbox(Textbox tb, string t, Row row)
        {
            if (!await tb.IsHidden(report, row))
                await excelBuilder.AddTextbox(tb, t, row);
        }

        public Task DataRegionNoRows(DataRegion d, string noRowsMsg) => Task.CompletedTask;

        public Task<bool> ListStart(List l, Row r) => Task.FromResult(true);
        public Task ListEnd(List l, Row r) => Task.FromResult(true);
        public Task ListEntryBegin(List l, Row r) => Task.CompletedTask;
        public void ListEntryEnd(List l, Row r) { }

        public async Task<bool> TableStart(Table t, Row row)
        {
            if (t.Visibility == null || !await t.Visibility.IsHidden(report, row))
            {
                excelBuilder.AddTable(t);
                return true;
            }
            return false;
        }

        public bool IsTableSortable(Table t) => false;
        public Task TableEnd(Table t, Row row) => Task.CompletedTask;
        public void TableBodyStart(Table t, Row row) { }
        public void TableBodyEnd(Table t, Row row) { }
        public void TableFooterStart(Footer f, Row row) { }
        public void TableFooterEnd(Footer f, Row row) { }
        public void TableHeaderStart(Header h, Row row) { }
        public void TableHeaderEnd(Header h, Row row) { }

        public async Task TableRowStart(TableRow tr, Row row)
            => await excelBuilder.AddRow(tr, row);

        public void TableRowEnd(TableRow tr, Row row) { }
        public Task TableCellStart(TableCell t, Row row) => Task.CompletedTask;
        public void TableCellEnd(TableCell t, Row row) { }

        public Task<bool> MatrixStart(Matrix m, MatrixCellEntry[,] matrix, Row r, int headerRows, int maxRows, int maxCols)
            => Task.FromResult(true);

        public void MatrixColumns(Matrix m, MatrixColumns mc) { }
        public Task MatrixCellStart(Matrix m, ReportItem ri, int row, int column, Row r, float h, float w, int colSpan) => Task.FromResult(true);
        public Task MatrixCellEnd(Matrix m, ReportItem ri, int row, int column, Row r) => Task.FromResult(true);
        public void MatrixRowStart(Matrix m, int row, Row r) { }
        public void MatrixRowEnd(Matrix m, int row, Row r) { }
        public Task MatrixEnd(Matrix m, Row r) => Task.CompletedTask;
        public Task Chart(Chart c, Row row, ChartBase cb) => Task.CompletedTask;

        public async Task Image(Image i, Row r, string mimeType, Stream ioin)
        {
            if (i.Visibility == null || !await i.Visibility.IsHidden(report, r))
            {
                byte[] data;
                using (var ms = new MemoryStream())
                {
                    await ioin.CopyToAsync(ms);
                    data = ms.ToArray();
                }
                excelBuilder.AddImage(i, data);
            }
        }

        public async Task Line(Line l, Row row)
        {
            float borderWidth = 1;
            if (l.Style.BorderWidth != null)
                borderWidth = await l.Style.BorderWidth.EvalDefault(report, row);
            else if (l.Style.BorderStyle != null)
                borderWidth = (float)await l.Style.BorderStyle.EvalDefault(report, row);
            excelBuilder.AddLine(l, borderWidth);
        }

        public Task<bool> RectangleStart(Rdl.Rectangle rect, Row r) => Task.FromResult(true);
        public Task RectangleEnd(Rdl.Rectangle rect, Row r) => Task.CompletedTask;
        public Task Subreport(Subreport s, Row r) => Task.CompletedTask;
        public void GroupingStart(Grouping g) { }
        public void GroupingInstanceStart(Grouping g) { }
        public void GroupingInstanceEnd(Grouping g) { }
        public void GroupingEnd(Grouping g) { }
        public Task RunPages(Pages pgs) => Task.CompletedTask;
    }
}
