

using System;
using System.IO;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using RdlEngine.Render.ExcelConverter;
using NPOI.XSSF.UserModel.Helpers;
using System.Linq;
using NPOI.SS.Util;
using NPOI.Util;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Renders a report to Excel 2007.   This handles some page formating but does not do true page formatting.
	///</summary>
	internal class RenderExcel2007 : IPresent
	{
		Report report;                   // report
		IStreamGen _sg;             // stream generater

		ExcelCellsBuilder excelBuilder = new ExcelCellsBuilder();
		XSSFWorkbook workbook = new XSSFWorkbook();
		XSSFSheet worksheet;
		double k = 5.637142013; //points per excel character width

		List<XSSFCellStyle> styles;
		List<XSSFFont> fonts;

		public RenderExcel2007(Report rep, IStreamGen sg)
		{
			report = rep;
			_sg = sg;                   // We need this in future

			excelBuilder.Report = report;
			styles = new List<XSSFCellStyle>();
			fonts = new List<XSSFFont>();
			worksheet = (XSSFSheet)workbook.CreateSheet(string.IsNullOrEmpty(rep.Name) ? "NewSheet" : rep.Name);
			var ps = (XSSFPrintSetup)worksheet.PrintSetup;
			ps.SetPaperSize(PaperSize.A4);
			ps.Orientation = PrintOrientation.LANDSCAPE;
			worksheet.SetMargin(MarginType.LeftMargin, rep.LeftMarginPoints / 72);
			worksheet.SetMargin(MarginType.TopMargin, rep.TopMarginPoints / 72);
			worksheet.SetMargin(MarginType.RightMargin, rep.RightMarginPoints / 72);
			worksheet.SetMargin(MarginType.BottomMargin, rep.BottomMarginPoints / 72);

		}

		// Added to expose data to Excel2003 file generation
		protected IStreamGen StreamGen { get => _sg; set => _sg = value; }

		public void Dispose()
		{
			if(workbook != null)
				workbook.Dispose();
		}

		public Report Report()
		{
			return report;
		}

		public bool IsPagingNeeded()
		{
			return false;
		}

		public void Start()
		{

		}

		public virtual Task End()
		{
			excelBuilder.CellsCorrection();
			for(int i = 0; i < excelBuilder.Columns.Count; i++) {
				if(i + 1 < excelBuilder.Columns.Count) {
					var widthInPoints = excelBuilder.Columns[i + 1].XPosition - excelBuilder.Columns[i].XPosition;
					var widthInSymbols = widthInPoints / k;
					worksheet.SetColumnWidth(i, (int)(widthInSymbols * 256));
				}
			}

			for(int i = 0; i < excelBuilder.Rows.Count - 1; i++) {
				var builderRow = excelBuilder.Rows[i];
				XSSFRow row = (XSSFRow)worksheet.CreateRow(i);
				row.HeightInPoints = (excelBuilder.Rows[i + 1].YPosition - builderRow.YPosition);
			}

			for(int i = 0; i < excelBuilder.Rows.Count - 1; i++) {
				var builderRow = excelBuilder.Rows[i];
				XSSFRow row = (XSSFRow)worksheet.GetRow(i);

				for(int j = 0; j < builderRow.Cells.Count; j++) {
					var builderCell = builderRow.Cells[j];
					var columnIndex = excelBuilder.Columns.IndexOf(builderCell.Column);
					XSSFCell cell = (XSSFCell)row.CreateCell(columnIndex);

					if(builderCell.ReportItem != null) {
						ExcelCellStyle style = null;
						style = new ExcelCellStyle(builderCell.Style);

						XSSFCellStyle xssfStyle = null;
						for(int s = 0; s < workbook.NumCellStyles; s++) {
							XSSFCellStyle innerStyle = (XSSFCellStyle)workbook.GetCellStyleAt(s);

							if(style.CompareWithXSSFStyle(innerStyle)) {
								xssfStyle = innerStyle;
								break;
							}
						}
						if(xssfStyle == null) {
							xssfStyle = (XSSFCellStyle)workbook.CreateCellStyle();
							style.SetToStyle(xssfStyle);
							XSSFFont font = null;
							for(short f = 0; f < workbook.NumberOfFonts; f++) {
								XSSFFont innerfont = (XSSFFont)workbook.GetFontAt(f);
								if(style.CompareWithXSSFFont(innerfont)) {
									font = innerfont;
									break;
								}

							}
							if(font == null) {
								font = (XSSFFont)workbook.CreateFont();
								style.SetToFont(font);
							}
							font.FontHeightInPoints -= 1;
							xssfStyle.SetFont(font);
						}

						var rightAttach = excelBuilder.GetRightAttachCells(builderCell);
						var bottomAttach = excelBuilder.GetBottomAttachCells(builderCell);

						var rowIndex = excelBuilder.Rows.IndexOf(builderCell.Row);
						var colIndex = excelBuilder.Columns.IndexOf(builderCell.Column);

						if(rightAttach > 0 || bottomAttach > 0) {
							var mergeRegion = new NPOI.SS.Util.CellRangeAddress(rowIndex,
																				rowIndex + bottomAttach,
																				colIndex,
																				colIndex + rightAttach);
							worksheet.AddMergedRegion(mergeRegion);

							RegionUtil.SetBorderTop(xssfStyle.BorderTop, mergeRegion, worksheet);
							RegionUtil.SetTopBorderColor(xssfStyle.TopBorderColor, mergeRegion, worksheet);
							RegionUtil.SetBorderRight(xssfStyle.BorderRight, mergeRegion, worksheet);
							RegionUtil.SetRightBorderColor(xssfStyle.RightBorderColor, mergeRegion, worksheet);
							RegionUtil.SetBorderBottom(xssfStyle.BorderBottom, mergeRegion, worksheet);
							RegionUtil.SetBottomBorderColor(xssfStyle.BottomBorderColor, mergeRegion, worksheet);
							RegionUtil.SetBorderLeft(xssfStyle.BorderLeft, mergeRegion, worksheet);
							RegionUtil.SetLeftBorderColor(xssfStyle.LeftBorderColor, mergeRegion, worksheet);
						}

						xssfStyle.WrapText = true;

						cell.CellStyle = xssfStyle;

						cell.SetCellValue(builderCell.Value);
					}
				}
			}


			//Create images
			XSSFDrawing drawing = worksheet.CreateDrawingPatriarch() as XSSFDrawing;
			foreach(var image in excelBuilder.Images) {

				var anchor = CreateAnchor(image.AbsoluteTop, image.AbsoluteLeft + image.ImageWidth, image.AbsoluteTop + image.ImageHeight, image.AbsoluteLeft);
				anchor.AnchorType = AnchorType.DontMoveAndResize;
				drawing.CreatePicture(anchor, image.ImageIndex);
			}

			//Create lines
			foreach(var line in excelBuilder.Lines) {

				var anchorLeft = line.FlipH ? line.Right : line.AbsoluteLeft;
				var anchorRight = line.FlipH ? line.AbsoluteLeft : line.Right;
				var anchorTop = line.FlipV ? line.Bottom : line.AbsoluteTop;
				var anchorBottom = line.FlipV ? line.AbsoluteTop : line.Bottom;

				var anchorLine = CreateAnchor(anchorTop, anchorRight, anchorBottom, anchorLeft);

				XSSFSimpleShape lineShape = drawing.CreateSimpleShape(anchorLine);
				lineShape.ShapeType = (int)ShapeTypes.Line;
				lineShape.LineWidth = line.BorderWidth;
				//BUG. incorrect set line style
				//lineShape.LineStyle = LineStyle.Solid;
				lineShape.SetLineStyleColor(0, 0, 0);
				//lineShape.GetCTShape().spPr.xfrm.flipH = line.FlipH;
				//lineShape.GetCTShape().spPr.xfrm.flipV = line.FlipV;
			}

			workbook.Write(_sg.GetStream());
			return Task.CompletedTask;
		}

		private XSSFClientAnchor CreateAnchor(float top, float right, float bottom, float left)
		{
			
			var col1 = excelBuilder.Columns.LastOrDefault(x => x.XPosition < left);
			var row1 = excelBuilder.Rows.LastOrDefault(x => x.YPosition < top);

			var col2 = excelBuilder.Columns.LastOrDefault(x => x.XPosition < right);
			var row2 = excelBuilder.Rows.LastOrDefault(x => x.YPosition < bottom);

			var dx1 = (left - (col1 == null ? 0 : col1.XPosition)) * Units.EMU_PER_POINT;
			var dy1 = (top - (row1 == null ? 0 : row1.YPosition)) * Units.EMU_PER_POINT;

			var dx2 = (right - (col2 == null ? 0 : col2.XPosition)) * Units.EMU_PER_POINT;
			var dy2 = (bottom - (row2 == null ? 0 : row2.YPosition)) * Units.EMU_PER_POINT;

			var col1Index = col1 == null ? 0 : excelBuilder.Columns.IndexOf(col1);
			var row1Index = row1 == null ? 0 : excelBuilder.Rows.IndexOf(row1);

			var col2Index = col2 == null ? 0 : excelBuilder.Columns.IndexOf(col2);
			var row2Index = row2 == null ? 0 : excelBuilder.Rows.IndexOf(row2);

			return new XSSFClientAnchor((int)dx1, (int)dy1,
			                            (int)dx2, (int)dy2,
			                            col1Index, row1Index,
			                            col2Index, row2Index);
		}


		// Body: main container for the report
		public void BodyStart(Body b)
		{
		}

		public void BodyEnd(Body b)
		{
		}

		public void PageHeaderStart(PageHeader ph)
		{
		}

		public void PageHeaderEnd(PageHeader ph)
		{
		}

		public void PageFooterStart(PageFooter pf)
		{
		}

		public void PageFooterEnd(PageFooter pf)
		{
		}

		public async Task Textbox(Textbox tb, string t, Row row)
		{
			if(!await tb.IsHidden(report, row)) {
                await excelBuilder.AddTextbox(tb, t, row);
			}
		}

		private async Task<StyleInfo> GetStyle(ReportItem ri, Row row)
		{
			if(ri.Style == null)
				return null;

			return await ri.Style.GetStyleInfo(report, row);
		}

		private static bool InTable(ReportItem tb)
		{
			Type tp = tb.Parent.Parent.GetType();
			return (tp == typeof(TableCell) ||
					 tp == typeof(Corner) ||
					 tp == typeof(DynamicColumns) ||
					 tp == typeof(DynamicRows) ||
					 tp == typeof(StaticRow) ||
					 tp == typeof(StaticColumn) ||
					 tp == typeof(Subtotal) ||
					 tp == typeof(MatrixCell));
		}

		private static bool InList(ReportItem tb)
		{
			Type tp = tb.Parent.Parent.GetType();
			return (tp == typeof(List));
		}

		public Task DataRegionNoRows(DataRegion d, string noRowsMsg)            // no rows in table
		{
			return Task.CompletedTask;
		}

		// Lists
		public Task<bool> ListStart(List l, Row r)
		{
			return Task.FromResult(true);
		}

		public Task ListEnd(List l, Row r)
		{
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
			if(t.Visibility == null || (t.Visibility != null && !await t.Visibility.IsHidden(report, row))) {
				excelBuilder.AddTable(t);
				return true;
			}
			return false;
		}

		public bool IsTableSortable(Table t)
		{
			return false;   // can't have tableGroups; must have 1 detail row
		}

		public Task TableEnd(Table t, Row row)
		{
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
		}

		public void TableHeaderEnd(Header h, Row row)
		{
		}

		public async Task TableRowStart(TableRow tr, Row row)
		{
            await excelBuilder.AddRow(tr, row);
		}

		public void TableRowEnd(TableRow tr, Row row)
		{
		}

		public Task TableCellStart(TableCell t, Row row)
		{
			return Task.CompletedTask;
		}

		public void TableCellEnd(TableCell t, Row row)
		{
			return;
		}

		public Task<bool> MatrixStart(Matrix m, MatrixCellEntry[,] matrix, Row r, int headerRows, int maxRows, int maxCols)               // called first
		{
			return Task.FromResult(true);
		}

		public void MatrixColumns(Matrix m, MatrixColumns mc)   // called just after MatrixStart
		{
		}

		public Task MatrixCellStart(Matrix m, ReportItem ri, int row, int column, Row r, float h, float w, int colSpan)
		{
			return Task.CompletedTask;
		}

		public Task MatrixCellEnd(Matrix m, ReportItem ri, int row, int column, Row r)
		{
			return Task.CompletedTask;
		}

		public void MatrixRowStart(Matrix m, int row, Row r)
		{
		}

		public void MatrixRowEnd(Matrix m, int row, Row r)
		{
		}

		public Task MatrixEnd(Matrix m, Row r)              // called last
		{
			return Task.CompletedTask;
		}

		public Task Chart(Chart c, Row row, ChartBase cb)
		{
			return Task.CompletedTask;
		}
		public async Task Image(Image i, Row r, string mimeType, Stream ioin)
		{
			if(i.Visibility == null || (i.Visibility != null && !await i.Visibility.IsHidden(report, r))) {
				int picIndex = workbook.AddPicture(ioin, XSSFWorkbook.PICTURE_TYPE_JPEG);
				excelBuilder.AddImage(i, picIndex);
			}
		}

		public async Task Line(Line l, Row row)
		{
			float borderWidth = 1;
			if(l.Style.BorderWidth != null) {
				borderWidth = await l.Style.BorderWidth.EvalDefault(report, row);
			} else if(l.Style.BorderStyle != null) {
				borderWidth = (float)await l.Style.BorderStyle.EvalDefault(report, row);
			}
			excelBuilder.AddLine(l, borderWidth);
		}

		public Task<bool> RectangleStart(Rdl.Rectangle rect, Row r)
		{
			return Task.FromResult(true);
		}

		public Task RectangleEnd(Rdl.Rectangle rect, Row r)
		{
			return Task.CompletedTask;
		}

		// Subreport:  
		public Task Subreport(Subreport s, Row r)
		{
            return Task.CompletedTask;
        }
		public void GroupingStart(Grouping g)           // called at start of grouping
		{
		}
		public void GroupingInstanceStart(Grouping g)   // called at start for each grouping instance
		{
		}
		public void GroupingInstanceEnd(Grouping g) // called at start for each grouping instance
		{
		}
		public void GroupingEnd(Grouping g)         // called at end of grouping
		{
		}
		public Task RunPages(Pages pgs) // we don't have paging turned on for html
		{
            return Task.CompletedTask;
        }
	}
}
