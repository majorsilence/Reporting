using System;
using System.Linq;
using System.Collections.Generic;
using Majorsilence.Reporting.Rdl;
using System.Threading.Tasks;

using Majorsilence.Forms.Drawing;
using System.Drawing;  // value types (Color, Point, Size, Rectangle, ...) come from System.Drawing.Primitives
using Majorsilence.Forms.Drawing.Drawing2D;

namespace RdlEngine.Render.ExcelConverter
{
	internal class ExcelCellsBuilder
	{
		Graphics g;
		public Report Report { get; set; }
		public static float Tolerance = 2f;

		public List<ExcelRow> Rows { get; private set; }
		public List<ExcelColumn> Columns { get; private set; }
		public List<ExcelCell> Cells { get; private set; }
		public List<ExcelImage> Images { get; private set; }
		public List<ExcelLine> Lines { get; private set; }
		public List<ExcelTable> Tables { get; private set; }

		private float rowPosition = 0f;
		private ExcelTable CurrentExcelTable = null;

		public ExcelCellsBuilder()
		{
			g = Graphics.FromImage(new Bitmap(10, 10));
			Tables = new List<ExcelTable>();
			Rows = new List<ExcelRow>();
			AddRow(0, 1);
			Columns = new List<ExcelColumn>();
			AddColumn(0, 1);
			Cells = new List<ExcelCell>();
			Images = new List<ExcelImage>();
			Lines = new List<ExcelLine>();
		}

		public void AddImage(Majorsilence.Reporting.Rdl.Image image, byte[] data)
		{
			var img = new ExcelImage(image, data);
			float top = image.Top.Points;
			float left = image.Left.Points;
			FillAbsolutePosition(image, ref top, ref left);

			top = GetCellAboveRelativePosition(top);

			img.AbsoluteTop = top;
			img.AbsoluteLeft = left;
			Images.Add(img);
		}

		public void AddLine(Majorsilence.Reporting.Rdl.Line line, float borderWidth)
		{
			var l = new ExcelLine(line);
			float top = line.Top.Points;
			float right = line.GetX2(Report);
			float bottom = line.Y2;
			float left = line.Left.Points;
			FillAbsolutePosition(line, ref top, ref left);
			FillAbsolutePosition(line, ref bottom, ref right);

			top = GetCellAboveRelativePosition(top);
			bottom = top + line.Height.Points;

			l.AbsoluteTop = top;
			l.AbsoluteLeft = left;
			l.Right = right;
			l.Bottom = bottom;
			l.BorderWidth = borderWidth;
			Lines.Add(l);
		}

		public void AddTable(Table table)
		{
			CurrentExcelTable = new ExcelTable();
			var tableTop = table.Top?.Points ?? 0;
			var tableLeft = table.Left?.Points ?? 0;

			FillAbsolutePosition(table, ref tableTop, ref tableLeft);

			CurrentExcelTable.OriginalBottomPosition = tableTop;
			if(table.Header != null) {
				CurrentExcelTable.OriginalBottomPosition += table.Header.TableRows.Items.Sum(x => x.Height.Points);
			}
			if(table.Details != null) {
				CurrentExcelTable.OriginalBottomPosition += table.Details.TableRows.Items.Sum(x => x.Height.Points);
			}
			if(table.Footer != null) {
				CurrentExcelTable.OriginalBottomPosition += table.Footer.TableRows.Items.Sum(x => x.Height.Points);
			}

			tableTop = GetCellAboveRelativePosition(tableTop);
				
			rowPosition = tableTop;

			CurrentExcelTable.Table = table;
			Tables.Add(CurrentExcelTable);
		}

		public async Task AddRow(TableRow tr, Row row)
		{
			if(CurrentExcelTable.Table == null) {
				return;
			}
			float rowHeight = (tr.CanGrow || tr.CanShrink) ? await tr.HeightOfRow(Report, g, row) : tr.Height.Points;

			var shiftingRows = Rows.Where(x => x.YPosition >= rowPosition);
			ShiftBottomRows(shiftingRows, rowHeight);

			var currentRow = AddRow(rowPosition, rowHeight);
			foreach(var cell in tr.TableCells.Items) {
				var column = CurrentExcelTable.Table.TableColumns.Items[cell.ColIndex];
				if(await column.IsHidden(Report, row))
					continue;
				var xPosition = CurrentExcelTable.Table.Left?.Points ?? 0;
				for(int i = 0; i < cell.ColIndex; i++) {
					var columnBefore = CurrentExcelTable.Table.TableColumns.Items[i];
					if(await columnBefore.IsHidden(Report, row))
						continue;
					xPosition += columnBefore.Width.Points;
				}
				var currentColumn = AddColumn(xPosition, column.Width.Points);
				var cellTextBox = cell.ReportItems.Items.FirstOrDefault();
				if(cellTextBox == null || (cellTextBox as Textbox) == null) {
					continue;
				}
				string value = await (cellTextBox as Textbox).RunText(Report, row);
				ExcelCell currentCell = new ExcelCell(cellTextBox, value, currentRow, currentColumn);
				currentCell.ExcelTable = CurrentExcelTable;
				currentCell.OriginalWidth = column.Width.Points;
				currentCell.OriginalHeight = rowHeight;

				if(cell.ColSpan > 1) {
					float spanWidth = 0f;
					for(int i = cell.ColIndex; i < cell.ColIndex + cell.ColSpan; i++) {
						spanWidth += CurrentExcelTable.Table.TableColumns.Items[i].Width.Points;
					}
					currentCell.OriginalWidth = spanWidth;
				}
				currentCell.GrowedBottomPosition = rowPosition + rowHeight;
                await SetCellStyle(currentCell, cellTextBox, row);
				Cells.Add(currentCell);
			}

			rowPosition += rowHeight;
			CurrentExcelTable.GrowedBottomPosition = rowPosition;
		}

		private void FillAbsolutePosition(ReportItem reportItem, ref float top, ref float left)
		{
			for(ReportLink rl = reportItem.Parent; rl != null; rl = rl.Parent) {
				if(rl is PageHeader || rl is PageFooter || rl is Body) {
					break;
				}
				if((rl is DataRegion || rl is Majorsilence.Reporting.Rdl.Rectangle) && !(rl is CustomReportItem)) {
					top += (rl as ReportItem)?.Top?.Points ?? 0;
					left += (rl as ReportItem)?.Left?.Points ?? 0;
				}
			}
		}

		public async Task AddTextbox(Textbox reportItem, string value, Row row)
		{
			if(reportItem.InPageHeaderOrFooter()) {
				return;
			}

			if(reportItem.IsTableOrMatrixCell(Report) || 
			   reportItem.Top == null || reportItem.Left == null || 
			   reportItem.Height == null || reportItem.Width == null) {
				return;
			}


			float topPosition = reportItem.Top.Points;
			float leftPosition = reportItem.Left.Points;
			float height = reportItem.CanGrow ? await reportItem.RunTextCalcHeight(Report, g, row) : reportItem.Height.Points;
			float width = reportItem.Width.Points;

			FillAbsolutePosition(reportItem, ref topPosition, ref leftPosition);
			float OriginalBottomPosition = topPosition + reportItem.Height.Points;
			float bottomPosition = topPosition + height;
			float rightPosition = leftPosition + width;

			topPosition = GetCellAboveRelativePosition(topPosition);

			var currentRow = AddRow(topPosition, height);
			var currentColumn = AddColumn(leftPosition, width);

			ExcelCell currentCell = new ExcelCell(reportItem, value, currentRow, currentColumn);
			currentCell.OriginalBottomPosition = OriginalBottomPosition;
            await SetCellStyle(currentCell, reportItem, row);
			Cells.Add(currentCell);
			currentCell.OriginalHeight = height;
			currentCell.GrowedBottomPosition = topPosition + height;
		}

		private async Task SetCellStyle(ExcelCell excelCell, ReportItem reportItem, Row row)
		{
			StyleInfo si = new StyleInfo();
			if(reportItem.Style != null) {
				var itemStyleInfo = await reportItem.Style.GetStyleInfo(Report, row);
				if(itemStyleInfo != null) {
					si = itemStyleInfo;
				}
			}
			excelCell.Style = si;
		}

		public float GetCellAboveRelativePosition(float top)
		{
			var aboveCell = Cells.Where(x => x.OriginalBottomPosition < top)
			                     .OrderByDescending(x => x.GrowedBottomPosition)
			                     .FirstOrDefault();
			if(aboveCell == null) {
				return top;
			}

			float growedPosition = 0f;
			float deltaOriginPosition = 0f;

			if(aboveCell.ExcelTable != null) {
				growedPosition = aboveCell.ExcelTable.GrowedBottomPosition;
				deltaOriginPosition = top - aboveCell.ExcelTable.OriginalBottomPosition;
			}else {
				growedPosition = aboveCell.GrowedBottomPosition;
				deltaOriginPosition = top - aboveCell.OriginalBottomPosition;
			}

			return growedPosition + (deltaOriginPosition < 0 ? 0 : deltaOriginPosition);
		}

		public ExcelRow AddRow(float top, float height)
		{
			//Insert row at Top position
			var currentRow = GetRowAtPosition(top);
			int rowIndex;
			if(currentRow == null) {
				rowIndex = InsertRow(top);
				currentRow = Rows[rowIndex];
			} else {
				rowIndex = Rows.IndexOf(currentRow);
			}
			//Insert row at Bottom position
			float bottomPosition = top + height;
			var bottomRow = GetRowAtPosition(bottomPosition);
			if(bottomRow == null) {
				int bottomRowIndex = InsertRow(bottomPosition);
				bottomRow = Rows[bottomRowIndex];
			}

			return currentRow;
		}

		private ExcelColumn AddColumn(float left, float width){
			//Insert column at Left position
			var currentColumn = GetColumnAtPosition(left);
			int columnIndex;
			if(currentColumn == null) {
				columnIndex = InsertColumn(left);
				currentColumn = Columns[columnIndex];
			} else {
				columnIndex = Columns.IndexOf(currentColumn);
			}
			//Insert column at Right position
			float rightPosition = left + width;
			var rightColumn = GetColumnAtPosition(rightPosition);
			if(rightColumn == null) {
				int rightColumnIndex = InsertColumn(rightPosition);
				rightColumn = Columns[rightColumnIndex];
			}
			return currentColumn;
		}

		public ExcelColumn GetRightAttachColumn(ExcelCell cell)
		{
			var c = Columns.LastOrDefault(x => x.XPosition < (cell.Column.XPosition + cell.ActualWidth) - Tolerance);

			return c;
		}

		public int GetRightAttachCells(ExcelCell cell)
		{
			int result = 0;
			if(cell.RightAttachCol != null) {
				var ri = Columns.IndexOf(cell.RightAttachCol);
				var li = Columns.IndexOf(cell.Column);
				result = (ri - li) - 1;
				return result < 0 ? 0 : result;
			}

			result = Columns.Count(x => x.XPosition > cell.Column.XPosition
			                       && x.XPosition < (cell.Column.XPosition + cell.ActualWidth) - Tolerance);
			return result < 0 ? 0 : result;
		}

		public int GetBottomAttachCells(ExcelCell cell)
		{
			int result = 0;
			if(cell.BottomAttachRow != null) {
				var bi = Rows.IndexOf(cell.BottomAttachRow);
				var ti = Rows.IndexOf(cell.Row);
				result = (bi - ti) - 1;
				return result < 0 ? 0 : result;
			}

			return Rows.Count(y => y.YPosition > cell.Row.YPosition
			                  && y.YPosition < (cell.Row.YPosition + cell.ActualHeight) - Tolerance);
		}

		public ExcelRow GetBottomAttachRow(ExcelCell cell)
		{
			var c = Rows.LastOrDefault(x => x.YPosition < (cell.Row.YPosition + cell.ActualHeight) - Tolerance);

			return c;
		}

		private ExcelRow GetRowAtPosition(float yPositionPoints)
		{
			return Rows.FirstOrDefault(y => Math.Abs(y.YPosition - yPositionPoints) <= Tolerance);
		}

		private ExcelColumn GetColumnAtPosition(float xPositionPoints)
		{
			return Columns.FirstOrDefault(x => Math.Abs(x.XPosition - xPositionPoints) <= Tolerance);
		}

		private int InsertRow(float yPositionPoints)
		{
			var nextRow = Rows.Where(x => x.YPosition > yPositionPoints).OrderBy(x => x.YPosition).FirstOrDefault();

			if(nextRow == null) {
				//add new row to end of list
				var newIndex = Rows.Count;
				Rows.Insert(newIndex, new ExcelRow(yPositionPoints));
				return newIndex;
			}

			var nextRowIndex = Rows.IndexOf(nextRow);
			Rows.Insert(nextRowIndex, new ExcelRow(yPositionPoints));
			return nextRowIndex;
		}

		private int InsertColumn(float xPositionPoints)
		{
			var nextColumn = Columns.Where(x => x.XPosition > xPositionPoints).OrderBy(x => x.XPosition).FirstOrDefault();

			if(nextColumn == null) {
				//add new column to end of list
				var newIndex = Columns.Count;
				Columns.Insert(newIndex, new ExcelColumn(xPositionPoints));
				return newIndex;
			}

			var nextColumnIndex = Columns.IndexOf(nextColumn);
			Columns.Insert(nextColumnIndex, new ExcelColumn(xPositionPoints));
			return nextColumnIndex;
		}
	
		private bool ResolveIntersectionConflict(ExcelCell A, ExcelCell B)
		{
			var BLeft = B.Column.XPosition;
			var BRight = B.Column.XPosition + B.OriginalWidth;
			var BTop = B.Row.YPosition;
			var BBottom = B.Row.YPosition + B.OriginalHeight;

			return ResolveIntersectionConflict(A, BLeft, BRight, BTop, BBottom);
		}

		private bool ResolveIntersectionConflict(ExcelCell A, float BLeft, float BRight, float BTop, float BBottom)
		{
			if(A == null) {
				return false;
			}
			var ALeft = A.Column.XPosition;
			var ARight = A.Column.XPosition + A.OriginalWidth;
			var ATop = A.Row.YPosition;
			var ABottom = A.Row.YPosition + A.OriginalHeight;

			bool leftEqualPosition = Math.Abs(ALeft - BLeft) <= Tolerance;
			bool topEqualPosition = Math.Abs(ATop - BTop) <= Tolerance;
			bool rightEqualPosition = Math.Abs(ARight - BRight) <= Tolerance;
			bool bottomEqualPosition = Math.Abs(ABottom - BBottom) <= Tolerance;

			bool Cross_ALeft_BTop = 	Intersection(ALeft, ATop, ALeft, ABottom, BLeft, BTop, BRight, BTop);
			bool Cross_ALeft_BBottom = 	Intersection(ALeft, ATop, ALeft, ABottom, BLeft, BBottom, BRight, BBottom);
			bool Cross_ARight_BTop = 	Intersection(ARight, ATop, ARight, ABottom, BLeft, BTop, BRight, BTop);
			bool Cross_ARight_BBottom = Intersection(ARight, ATop, ARight, ABottom, BLeft, BBottom, BRight, BBottom);

			//currently not used
			bool Cross_ATop_BLeft = 	Intersection(ALeft, ATop, ARight, ATop, BLeft, BTop, BLeft, BBottom);
			bool Cross_ATop_BRight = 	Intersection(ALeft, ATop, ARight, ATop, BRight, BTop, BRight, BBottom);
			bool Cross_ABottom_BLeft = 	Intersection(ALeft, ABottom, ARight, ABottom, BLeft, BTop, BLeft, BBottom);
			bool Cross_ABottom_BRight = Intersection(ALeft, ABottom, ARight, ABottom, BRight, BTop, BRight, BBottom);

			bool isCrossed = !(ABottom < BTop || BBottom < ATop || ARight < BLeft || BRight < ALeft);

			if(!isCrossed && !leftEqualPosition && !topEqualPosition && !rightEqualPosition && !bottomEqualPosition) {
				return true;
			}
			 
			if(!topEqualPosition && !bottomEqualPosition) {
				//Cut Upper cell to top position of a Lower cell

				//if A is upper
				if(ATop < BTop && ABottom < BBottom && isCrossed) {
					CutCellHeight(A, BTop);
					return true;
				}
				//Cut A cell to top position of a B cell
				//    ┌───┐
				//    │  A│
				// ┌──┼───┼─┐    
				// │B │   │ │
				// └──┼───┼─┘
				//    └───┘
				//            
				if(Cross_ALeft_BTop && Cross_ALeft_BBottom && BLeft < ALeft && BRight > ARight) {
					CutCellHeight(A, BTop);
					return true;
				}

				//Cut A cell to left position of a B cell
				//   ┌───┐ 
				//   │  B│ 
				//┌──┼─┐ │    
				//│A │ │ │ 
				//└──┼─┘ │
				//   └───┘ 
				//            
				if(Cross_ATop_BLeft && Cross_ABottom_BLeft && ALeft < BLeft && ARight < BRight) {
					CutCellWidthFromRight(A, BLeft);
					return true;
				}
			}

			//Cut Left cell to left position of a Right cell

			// ┌──┬───┬─┐  ┌──┬─┬─┐  
			// │A │   │ │  │A │ │ │ 
			// └──┼───┼─┘  └──┼─┘ │
			//    │  B│       │  B│
			//    └───┘       └───┘

			if(topEqualPosition && ABottom < BBottom && ALeft < BLeft && isCrossed) {
				CutCellWidthFromRight(A, BLeft);
				return true;
			}

			//Cut Left cell to left position of a Right cell
			if(ALeft < BLeft && ARight < BRight && ARight > BLeft &&
			   	(
					//┌───┬─┬───┐
					//│A  │ │  B│
					//└───┴─┴───┘
				  	(topEqualPosition && bottomEqualPosition)
					//┌────┐
					//│  ┌─┼──┐  
					//│A │ │ B│
					//│  └─┼──┘   
					//└────┘
				   	|| (!topEqualPosition && !bottomEqualPosition && Cross_ARight_BTop && Cross_ARight_BBottom)
					//┌──┬─┬──┐
					//│A │ │ B│
					//│  └─┼──┘
					//└────┘
				   	|| (topEqualPosition && !bottomEqualPosition && Cross_ARight_BBottom)
					//┌────┐
					//│  ┌─┼──┐
					//│A │ │ B│
					//└──┴─┴──┘
				   	|| (!topEqualPosition && bottomEqualPosition && Cross_ARight_BTop)
				)){
				CutCellWidthFromRight(A, BLeft);
				return true;
			}

			//Cut A cell to right position of a Left cell
			//┌──┬──┐
			//│  │ A│
			//├──┼──┘
			//│B │
			//└──┘
			if(topEqualPosition && leftEqualPosition && !bottomEqualPosition && !rightEqualPosition
			   && Cross_ABottom_BRight){
				CutCellWidthFromLeft(A, BRight);
				return true;
			}

			//B region full contain A
			if((BTop < ATop || topEqualPosition) 
			   && (BLeft < ALeft || leftEqualPosition) 
			   && (BRight > ARight || rightEqualPosition)
			   && (BBottom > ABottom || bottomEqualPosition)) {
				//delete A
				RemoveCell(A);
				if(!A.Column.Cells.Any()) {
					Columns.Remove(A.Column);
				}
				return true;
			}

			return false;
		}

		private void CutCellWidthFromLeft(ExcelCell cell, float toPos)
		{
			var LeftPosition = cell.Column.XPosition;
			var RightPosition = LeftPosition + cell.OriginalWidth;
			if(!(toPos > LeftPosition && toPos < RightPosition)) {
				return;
			}
			ExcelColumn StartColumn = GetColumnAtPosition(LeftPosition);
			ExcelColumn newStartColumn = GetColumnAtPosition(toPos);
			if(newStartColumn == null) {
				var index = InsertColumn(toPos);
				newStartColumn = Columns[index];
				Columns.Remove(newStartColumn);
			}
			var newCell = new ExcelCell(cell.ReportItem, cell.Value, cell.Row, newStartColumn);
			ExcelCell existCell = newStartColumn.Cells.FirstOrDefault(x => x.Row == cell.Row);
			if(existCell != null) {
				ResolveIntersectionConflict(cell, existCell);
			}
			newCell.Style = cell.Style;
			Cells.Add(newCell);
			newCell.Row.Cells.Add(newCell);
			newStartColumn.Cells.Add(newCell);
			RemoveCell(cell);

			LeftPosition = newStartColumn.XPosition;
			RightPosition = LeftPosition + cell.OriginalWidth;
			cell.RightAttachCol = GetColumnAtPosition(RightPosition);
			cell.CorrectedWidth = RightPosition - LeftPosition;
		}

		private void CutCellWidthFromRight(ExcelCell cell, float toPos)
		{
			var LeftPosition = cell.Column.XPosition;
			var RightPosition = LeftPosition + cell.OriginalWidth;
			if(!(toPos > LeftPosition && toPos < RightPosition)) {
				return;
			}
			cell.RightAttachCol = GetColumnAtPosition(toPos);
			ExcelColumn endColumn = GetColumnAtPosition(RightPosition);
			if(endColumn != null && endColumn.IsEmpty) {
				Columns.Remove(endColumn);
			}
			cell.CorrectedWidth = toPos - LeftPosition;
		}

		private void CutCellHeight(ExcelCell cell, float toPos)
		{
			var TopPosition = cell.Row.YPosition;
			var BottomPosition = TopPosition + cell.OriginalHeight;
			if(!(toPos > TopPosition && toPos < BottomPosition)) {
				return;
			}
			cell.BottomAttachRow = GetRowAtPosition(toPos);
			ExcelRow endRow = GetRowAtPosition(BottomPosition);
			if(endRow != null && endRow.IsEmpty) {
				Rows.Remove(endRow);
			}
			cell.CorrectedHeight = toPos - TopPosition;
		}

		private void RemoveCell(ExcelCell cell)
		{
			Cells.Remove(cell);
			cell.Column.Cells.Remove(cell);
			cell.Row.Cells.Remove(cell);
		}

		private bool Intersection(double ax1, double ay1, double ax2, double ay2, double bx1, double by1, double bx2, double by2)
		{
			double v1, v2, v3, v4;
			v1 = ((bx2 - bx1) * (ay1 - by1)) - ((by2 - by1) * (ax1 - bx1));
			v2 = ((bx2 - bx1) * (ay2 - by1)) - ((by2 - by1) * (ax2 - bx1));
			v3 = ((ax2 - ax1) * (by1 - ay1)) - ((ay2 - ay1) * (bx1 - ax1));
			v4 = ((ax2 - ax1) * (by2 - ay1)) - ((ay2 - ay1) * (bx2 - ax1));
			bool res = (v1 * v2 < 0) && (v3 * v4 < 0);
			return res;
		}

		public void CellsCorrection()
		{
			for(int i = 0; i < Cells.Count; i++) {
				var count = Cells.Count;

				var cell = Cells[i];
				if(cell.ExcelTable != null) {
					continue;
				}

				var unresolvedCells = Cells
					//didn't apply correction to table cells
					.Where(x => x.ExcelTable == null)
					//simple check intersection, for select only intersection available cells
					.Where(x => 
					       !((x.OriginalHeight + x.Row.YPosition) < cell.Row.YPosition ||
					       (cell.OriginalHeight + cell.Row.YPosition) < x.Row.YPosition ||
					       (x.OriginalWidth + x.Column.XPosition) < cell.Column.XPosition ||
					       (cell.OriginalWidth + cell.Column.XPosition) < x.Column.XPosition))
					.ToList();

				/*foreach(var table in Tables) {
					float tableLeft = table.Table.Left.Points;
					float tableTop = table.Table.Top.Points;
					FillAbsolutePosition(table.Table, ref tableTop, ref tableLeft);

					float tableRight = tableLeft + table.TableWidth;
					float tableBottom = table.GrowedBottomPosition;
					ResolveIntersectionConflict(cell, tableLeft, tableRight, tableTop, tableBottom);
				}*/

				while(unresolvedCells.Count > 0) {
					var uc = unresolvedCells[unresolvedCells.Count - 1];
					if(uc != cell) {
						ResolveIntersectionConflict(cell, uc);
					}
					unresolvedCells.Remove(uc);
				}

				var dCount = i - (count - Cells.Count);
				i = dCount < 0 ? 0 : dCount;
			}
		}

		private void ShiftBottomRows(IEnumerable<ExcelRow> rows, float yOffset)
		{
			foreach(var row in rows) {
				row.yOffset += yOffset;
				row.YPosition += yOffset;
			}
		}

	}
}
