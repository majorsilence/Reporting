

using System;
using System.Collections.Generic;
using System.Xml;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
    ///<summary>
    ///  Represents the report item (and Data region) for a matrix (cross-tabulation)
    ///</summary>
    [Serializable]
    internal class Matrix : DataRegion
    {
        Corner _Corner;     // The region that contains the elements of
                            // the upper left corner area of the matrix.
                            // If omitted, no report items are output in
                            // the corner.
        ColumnGroupings _ColumnGroupings;   // The set of column groupings for the matrix
        RowGroupings _RowGroupings; // The set of row groupings for the matrix
        MatrixRows _MatrixRows;     // The rows contained in each detail cell
                                    // of the matrix layout
        MatrixColumns _MatrixColumns;   // The columns contained in each detail
                                        // cell of the matrix layout
        MatrixLayoutDirectionEnum _LayoutDirection; // Indicates whether the matrix columns
                                                    // grow left-to-right (with headers on the
                                                    // left) or right-to-left (with headers on the
                                                    // right).
        int _GroupsBeforeRowHeaders;    // The number of instances of the
                                        // outermost column group that should
                                        // appear to the left of the row headers
                                        // (right of the row headers for RTL
                                        // matrixes). Default is 0.
        string _CellDataElementName;    // The name to use for the cell element. Default: �Cell�
        MatrixCellDataElementOutputEnum _CellDataElementOutput; // Indicates whether the cell contents
                                                                //should appear in a data rendering.  Default is Output.
        // Further sets of column groupings that sit beside the first, each with its own columns and
        // cells and sharing the row groupings (RDL 2008+ allows sibling column groups; 2005 has one).
        // Written fyi:ColumnSet in the report definition. See RunBuild.
        List<ColumnSet> _ExtraColumnSets = new List<ColumnSet>();

        // The same for rows: further sets of row groupings that stack below the first, each with its
        // own cells and sharing the column groupings. Written fyi:RowSet.
        List<RowSet> _ExtraRowSets = new List<RowSet>();

        sealed class RowSet
        {
            internal RowGroupings RowGroupings;
            internal MatrixRows MatrixRows;
        }

        sealed class ColumnSet
        {
            internal ColumnGroupings ColumnGroupings;
            internal MatrixRows MatrixRows;
            internal MatrixColumns MatrixColumns;
        }

        static string nullterminal = '\ufffe'.ToString();
        static string terminal = '\uffff'.ToString();

        internal Matrix(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p, xNode)
        {
            _Corner = null;
            _ColumnGroupings = null;
            _RowGroupings = null;
            _MatrixRows = null;
            _MatrixColumns = null;
            _LayoutDirection = MatrixLayoutDirectionEnum.LTR;
            _GroupsBeforeRowHeaders = 0;
            _CellDataElementName = null;
            _CellDataElementOutput = MatrixCellDataElementOutputEnum.Output;

            // Loop thru all the child nodes
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                if (xNodeLoop.NodeType != XmlNodeType.Element)
                    continue;
                switch (xNodeLoop.Name.ToLowerInvariant())
                {
                    case "corner":
                        _Corner = new Corner(r, this, xNodeLoop);
                        break;
                    case "columngroupings":
                        _ColumnGroupings = new ColumnGroupings(r, this, xNodeLoop);
                        break;
                    case "rowgroupings":
                        _RowGroupings = new RowGroupings(r, this, xNodeLoop);
                        break;
                    case "matrixrows":
                        _MatrixRows = new MatrixRows(r, this, xNodeLoop);
                        break;
                    case "matrixcolumns":
                        _MatrixColumns = new MatrixColumns(r, this, xNodeLoop);
                        break;
                    case "fyi:rowset":
                        _ExtraRowSets.Add(ParseRowSet(r, xNodeLoop));
                        break;
                    case "fyi:columnset":
                        _ExtraColumnSets.Add(ParseColumnSet(r, xNodeLoop));
                        break;
                    case "layoutdirection":
                        _LayoutDirection = MatrixLayoutDirection.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    case "groupsbeforerowheaders":
                        _GroupsBeforeRowHeaders = XmlUtil.Integer(xNodeLoop.InnerText);
                        break;
                    case "celldataelementname":
                        _CellDataElementName = xNodeLoop.InnerText;
                        break;
                    case "celldataelementoutput":
                        _CellDataElementOutput = MatrixCellDataElementOutput.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    default:
                        if (DataRegionElement(xNodeLoop))   // try at DataRegion level
                            break;
                        // don't know this element - log it
                        OwnerReport.rl.LogError(4, "Unknown Matrix element '" + xNodeLoop.Name + "' ignored.");
                        break;
                }
            }
            DataRegionFinish();         // Tidy up the DataRegion

            if (_ColumnGroupings == null)
                OwnerReport.rl.LogError(8, "Matrix element ColumnGroupings not specified for " + (this.Name == null ? "'name not specified'" : this.Name.Nm));
            if (_RowGroupings == null)
                OwnerReport.rl.LogError(8, "Matrix element RowGroupings not specified for " + (this.Name == null ? "'name not specified'" : this.Name.Nm));
            if (_MatrixRows == null)
                OwnerReport.rl.LogError(8, "Matrix element MatrixRows not specified for " + (this.Name == null ? "'name not specified'" : this.Name.Nm));
            if (_MatrixColumns == null)
                OwnerReport.rl.LogError(8, "Matrix element MatrixColumns not specified for " + (this.Name == null ? "'name not specified'" : this.Name.Nm));

            // MatrixCells count must be the same as the number of StaticColumns.
            //   If there are no StaticColumns it must be 1
            if (OwnerReport.rl.MaxSeverity > 4)
                return;         // don't perform this check if we've already go errors
            int mc = _MatrixRows.CellCount; // MatrixCells
            int sc = Math.Max(1, _ColumnGroupings.StaticCount);
            if (mc != sc)
            {
                OwnerReport.rl.LogError(8, "The count of MatrixCells must be 1 or equal to the number of StaticColumns if there are any.  Matrix " + (this.Name == null ? "unknown." : this.Name.Nm));
            }
            // matrix columns must also equal the static count (or 1 if no static columns)
            mc = this.CountMatrixColumns;
            if (mc != sc)
            {
                OwnerReport.rl.LogError(8, "The count of MatrixColumns must be 1 or equal to the number of StaticColumns if there are any.  Matrix " + (this.Name == null ? "unknown." : this.Name.Nm));
            }
            foreach (ColumnSet set in _ExtraColumnSets)
            {
                if (set.ColumnGroupings == null || set.MatrixRows == null || set.MatrixColumns == null)
                {
                    OwnerReport.rl.LogError(8, "A ColumnSet needs ColumnGroupings, MatrixRows and MatrixColumns. Matrix " + (this.Name == null ? "unknown." : this.Name.Nm));
                    return;
                }
                int setStatic = Math.Max(1, set.ColumnGroupings.StaticCount);
                if (set.MatrixRows.CellCount != setStatic || set.MatrixColumns.Items.Count != setStatic)
                {
                    OwnerReport.rl.LogError(8, "The count of MatrixCells and MatrixColumns in a ColumnSet must be 1 or equal to its number of StaticColumns. Matrix " + (this.Name == null ? "unknown." : this.Name.Nm));
                    return;
                }
            }
            if (_ExtraRowSets.Count > 0 && _ExtraColumnSets.Count > 0)
            {
                OwnerReport.rl.LogError(8, "A Matrix cannot have both ColumnSet and RowSet elements. Matrix " + (this.Name == null ? "unknown." : this.Name.Nm));
                return;
            }
            foreach (RowSet set in _ExtraRowSets)
            {
                if (set.RowGroupings == null || set.MatrixRows == null)
                {
                    OwnerReport.rl.LogError(8, "A RowSet needs RowGroupings and MatrixRows. Matrix " + (this.Name == null ? "unknown." : this.Name.Nm));
                    return;
                }
                if (set.MatrixRows.Items.Count != Math.Max(1, set.RowGroupings.StaticCount) || set.MatrixRows.CellCount != sc)
                {
                    OwnerReport.rl.LogError(8, "The count of MatrixRows and MatrixCells in a RowSet must match its StaticRows and the StaticColumns. Matrix " + (this.Name == null ? "unknown." : this.Name.Nm));
                    return;
                }
            }
            // matrix rows must also equal the static count (or 1 if no static rows)
            int mr = this.CountMatrixRows;
            int sr = Math.Max(1, _RowGroupings.StaticCount);
            if (mr != sr)
            {
                OwnerReport.rl.LogError(8, "The count of MatrixRows must be 1 or equal to the number of StaticRows if there are any.  Matrix " + (this.Name == null ? "unknown." : this.Name.Nm));
            }
        }

        async override internal Task FinalPass()
        {
            await base.FinalPass();

            float totalHeight = 0;
            if (_Corner != null)
                await _Corner.FinalPass();
            if (_ColumnGroupings != null)
            {
                await _ColumnGroupings.FinalPass();
                totalHeight += _ColumnGroupings.DefnHeight();
            }
            if (_RowGroupings != null)
                await _RowGroupings.FinalPass();
            if (_MatrixRows != null)
            {
                await _MatrixRows.FinalPass();
                totalHeight += _MatrixRows.DefnHeight();
            }
            if (_MatrixColumns != null)
                await _MatrixColumns.FinalPass();
            foreach (RowSet set in _ExtraRowSets)
            {
                await set.RowGroupings.FinalPass();
                await set.MatrixRows.FinalPass();
            }
            foreach (ColumnSet set in _ExtraColumnSets)
            {
                await set.ColumnGroupings.FinalPass();
                await set.MatrixRows.FinalPass();
                await set.MatrixColumns.FinalPass();
            }

            if (this.Height == null)
            {   // Calculate a height based on the sum of the TableRows
                this.Height = new RSize(this.OwnerReport, string.Format(NumberFormatInfo.InvariantInfo, "{0:0.00}pt", totalHeight));
            }

            return;
        }

        async override internal Task Run(IPresent ip, Row row)
        {
            Report rpt = ip.Report();
            WorkClass wc = GetValue(rpt);
            wc.FullData = wc.Data = await GetFilteredData(rpt, row);

            if (!await AnyRows(ip, wc.Data))      // if no rows return
                return;                 //   nothing left to do

            int maxColumns;
            int maxRows;
            MatrixCellEntry[,] matrix;

            (matrix, maxRows, maxColumns) = await RunBuild(rpt);

            // Now run thru the rows and columns of the matrix passing the information
            //   on to the rendering engine
            int headerRows = HeaderRowCount;	// number of column headers we have
            if (!await ip.MatrixStart(this, matrix, row, headerRows, maxRows, maxColumns))
                return;
            for (int iRow = 0; iRow < maxRows; iRow++)
            {
                ip.MatrixRowStart(this, iRow, row);
                for (int iColumn = 0; iColumn < maxColumns; iColumn++)
                {
                    MatrixCellEntry mce = matrix[iRow, iColumn];
                    if (mce == null)
                    {
                        await ip.MatrixCellStart(this, null, iRow, iColumn, row, float.MinValue, float.MinValue, 1);
                        await ip.MatrixCellEnd(this, null, iRow, iColumn, row);
                    }
                    else
                    {
                        wc.Data = mce.Data;     // Must set this for evaluation

                        Row lrow = wc.Data.Data.Count > 0 ? wc.Data.Data[0] : null;
                        mce.DisplayItem.SetMC(rpt, mce);    // set for use by the display item
                        SetGroupingValues(rpt, mce);

                        await ip.MatrixCellStart(this, mce.DisplayItem, iRow, iColumn, lrow, mce.Height, mce.Width, mce.ColSpan);

                        await mce.DisplayItem.Run(ip, lrow);
                        await ip.MatrixCellEnd(this, mce.DisplayItem, iRow, iColumn, lrow);
                    }
                }
                ip.MatrixRowEnd(this, iRow, row);
            }
            await ip.MatrixEnd(this, row);
            ResetInScope(rpt);
            RemoveValue(rpt);
        }

        async override internal Task RunPage(Pages pgs, Row row)
        {
            Report r = pgs.Report;
            if (await IsHidden(r, row))
                return;

            WorkClass wc = GetValue(r);

            wc.FullData = wc.Data = await GetFilteredData(r, row);

            SetPagePositionBegin(pgs);

            if (!await AnyRowsPage(pgs, wc.Data))     // if no rows return
                return;                     //   nothing left to do

            int maxColumns;
            int maxRows;
            int headerRows = HeaderRowCount;  // number of column headers we have
            MatrixCellEntry[,] matrix;

            (matrix, maxRows, maxColumns) = await RunBuild(r);

            // Now run thru the rows and columns of the matrix creating the pages
            RunPageRegionBegin(pgs);
            Page p = pgs.CurrentPage;
            p.YOffset += this.RelativeY(r);

            for (int iRow = 0; iRow < maxRows; iRow++)
            {
                float h = await HeightOfRow(pgs, matrix, iRow);
                if (h <= 0)     // there were no cells in row
                    continue;   //     skip the row

                if (p.YOffset + h > pgs.BottomOfPage)
                {
                    p = RunPageNew(pgs, p);
                    // run thru the headers again
                    for (int aRow = 0; aRow < headerRows; aRow++)
                    {
                        await RunPageColumns(pgs, wc, matrix, aRow, maxColumns);
                        p.YOffset += await HeightOfRow(pgs, matrix, aRow);
                    }
                }
                await RunPageColumns(pgs, wc, matrix, iRow, maxColumns);
                p.YOffset += h;
            }

            RunPageRegionEnd(pgs);
            SetPagePositionEnd(pgs, pgs.CurrentPage.YOffset);
            ResetInScope(r);
            RemoveValue(r);
            return;
        }

        internal void RunReset(Report rpt)
        {
            RemoveValue(rpt);
        }

        async Task<float> HeightOfRow(Pages pgs, MatrixCellEntry[,] matrix, int iRow)
        {
            Report rpt = pgs.Report;
            WorkClass wc = GetValue(rpt);

            int maxColumns = matrix.GetLength(1);
            float height = 0;
            bool bResetAllHeights = false;

            // Handle the corner;  it might span rows & columns
            bool bCorner = false;
            float cornerHeight = 0;
            if (iRow == 0 && matrix[0, 0] != null &&
                (this.ColumnGroupings.Items.Count > 1 ||
                this.RowGroupings.Items.Count > 1))
            {
                bCorner = true;
            }

            for (int iCol = 0; iCol < maxColumns; iCol++)
            {
                MatrixCellEntry mce = matrix[iRow, iCol];
                if (mce == null)
                    continue;
                if (mce.DisplayItem is Textbox)
                {
                    Textbox tb = mce.DisplayItem as Textbox;
                    if (tb.CanGrow)
                    {
                        wc.Data = mce.Data;     // Must set this for evaluation

                        Row lrow = wc.Data.Data.Count > 0 ? wc.Data.Data[0] : null;
                        mce.DisplayItem.SetMC(rpt, mce);    // set for use by the display item
                        SetGroupingValues(rpt, mce);

                        float tbh = await tb.RunTextCalcHeight(rpt, pgs.G, lrow);
                        if (height < tbh)
                        {
                            if (bCorner && iCol == 0)
                            {
                                cornerHeight = tbh;
                            }
                            else
                            {
                                bResetAllHeights = true;
                                height = tbh;
                            }
                        }
                    }
                }

                if (bCorner && iCol == 0)
                    continue;
                if (height < mce.Height)
                    height = mce.Height;
            }

            if (bResetAllHeights)   // If any text forces the row to grow; all heights must be fixed
            {
                for (int iCol = 0; iCol < maxColumns; iCol++)
                {
                    if (bCorner && iCol == 0)
                        continue;
                    MatrixCellEntry mce = matrix[iRow, iCol];
                    if (mce != null)
                        mce.Height = height;
                }
            }

            // Even with expansion room; we might need more space for the corner
            if (bCorner && cornerHeight > matrix[0, 0].Height)
            {   // add the additional space needed to the first row's height
                float newRow0Height;
                if (ColumnGroupings.Items.Count == 1)
                    newRow0Height = cornerHeight;
                else if (matrix[0, 1] != null)
                    newRow0Height = matrix[0, 1].Height + (cornerHeight - matrix[0, 0].Height);
                else
                    newRow0Height = (cornerHeight - matrix[0, 0].Height);
                height = newRow0Height;
                matrix[0, 0].Height = cornerHeight;
                for (int iCol = 1; iCol < maxColumns; iCol++)
                {
                    MatrixCellEntry mce = matrix[0, iCol];
                    if (mce != null)
                        mce.Height = newRow0Height;
                }
            }

            return height;
        }

        internal float[] ColumnWidths(MatrixCellEntry[,] matrix, int maxColumns)
        {
            float[] widths = new float[maxColumns];

            for (int iColumn = 0; iColumn < maxColumns; iColumn++)
            {
                widths[iColumn] = WidthOfColumn(matrix, iColumn);
            }
            return widths;
        }

        float WidthOfColumn(MatrixCellEntry[,] matrix, int iCol)
        {
            int maxRows = matrix.GetLength(0);
            for (int iRow = 0; iRow < maxRows; iRow++)
            {
                if (matrix[iRow, iCol] != null && matrix[iRow, iCol].ColSpan == 1)
                    return matrix[iRow, iCol].Width;
            }
            return 0;
        }

        async Task RunPageColumns(Pages pgs, WorkClass wc, MatrixCellEntry[,] matrix, int iRow, int maxColumns)
        {
            Report rpt = pgs.Report;

            float xpos = GetOffsetCalc(pgs.Report) + LeftCalc(rpt);
            for (int iColumn = 0; iColumn < maxColumns; iColumn++)
            {
                MatrixCellEntry mce = matrix[iRow, iColumn];

                if (mce == null)
                {   // have a null column but we need to fill column space
                    xpos += WidthOfColumn(matrix, iColumn);
                    continue;
                }
                wc.Data = mce.Data;     // Must set this for evaluation

                Row lrow = wc.Data.Data.Count > 0 ? wc.Data.Data[0] : null;
                SetGroupingValues(rpt, mce);
                mce.DisplayItem.SetMC(rpt, mce);    // set for use by the display item
                mce.XPosition = xpos;
                await mce.DisplayItem.RunPage(pgs, lrow);
                xpos += mce.Width;
                iColumn += (mce.ColSpan - 1);           // skip columns already accounted for
            }
        }

        // RunBuild is used by both Matrix.Run and Chart.Run to obtain the necessary data
        //   used by their respective rendering interfaces
        internal async Task<(MatrixCellEntry[,], int numRows, int numCols)> RunBuild(Report rpt)
        {
            if (_ExtraRowSets.Count > 0)
                return await RunBuildRowSets(rpt);
            if (_ExtraColumnSets.Count == 0)
                return await RunBuildSet(rpt);

            // Run the build once per column set, swapping in that set's groupings and cells, then
            // lay the grids side by side. The row groupings are shared, so each later grid repeats
            // the row-header columns, which are dropped; header rows are aligned at the bottom, next
            // to the data, so a set with fewer column levels has blank cells above it.
            var grids = new List<(MatrixCellEntry[,] Grid, int Rows, int Cols, int Header)>();
            ColumnGroupings savedGroupings = _ColumnGroupings;
            MatrixRows savedRows = _MatrixRows;
            MatrixColumns savedColumns = _MatrixColumns;
            try
            {
                var (g, r, c) = await RunBuildSet(rpt);
                grids.Add((g, r, c, _ColumnGroupings.Items.Count));
                foreach (ColumnSet set in _ExtraColumnSets)
                {
                    _ColumnGroupings = set.ColumnGroupings;
                    _MatrixRows = set.MatrixRows;
                    _MatrixColumns = set.MatrixColumns;
                    (g, r, c) = await RunBuildSet(rpt);
                    grids.Add((g, r, c, _ColumnGroupings.Items.Count));
                }
            }
            finally
            {
                _ColumnGroupings = savedGroupings;
                _MatrixRows = savedRows;
                _MatrixColumns = savedColumns;
            }

            return MergeColumnSets(grids, _RowGroupings.Items.Count);
        }

        static (MatrixCellEntry[,], int, int) MergeColumnSets(
            List<(MatrixCellEntry[,] Grid, int Rows, int Cols, int Header)> grids, int rowHeaderColumns)
        {
            int header = 0;
            int dataRows = int.MaxValue;
            foreach (var g in grids)
            {
                header = Math.Max(header, g.Header);
                dataRows = Math.Min(dataRows, g.Rows - g.Header);   // all sets share the row groupings
            }
            int rows = header + Math.Max(dataRows, 0);

            int cols = grids[0].Cols;
            for (int i = 1; i < grids.Count; i++)
                cols += Math.Max(grids[i].Cols - rowHeaderColumns, 0);

            MatrixCellEntry[,] merged = new MatrixCellEntry[rows, cols];
            int left = 0;
            for (int i = 0; i < grids.Count; i++)
            {
                var g = grids[i];
                int firstColumn = i == 0 ? 0 : rowHeaderColumns;
                int shift = header - g.Header;
                for (int iRow = 0; iRow < g.Header + Math.Max(dataRows, 0); iRow++)
                {
                    for (int iCol = firstColumn; iCol < g.Cols; iCol++)
                    {
                        MatrixCellEntry mce = g.Grid[iRow, iCol];
                        if (mce == null)
                            continue;
                        // the corner stays at the top left whatever the first set's header depth
                        int target = (i == 0 && iRow == 0 && iCol == 0) ? 0 : iRow + shift;
                        merged[target, left + iCol - firstColumn] = mce;
                    }
                }
                left += g.Cols - firstColumn;
            }
            return (merged, rows, cols);
        }

        /// <summary>The most column-header rows in any column set; repeated at the top of each page.</summary>
        int HeaderRowCount
        {
            get
            {
                int header = _ColumnGroupings.Items.Count;
                foreach (ColumnSet set in _ExtraColumnSets)
                    header = Math.Max(header, set.ColumnGroupings.Items.Count);
                return header;
            }
        }

        // The row-axis counterpart of the column sets: run the build once per row set, then stack the
        // grids. The column headers are shared and kept once (from the first set); each later set
        // contributes only its data rows. Row-header columns are aligned at the data, so a set with
        // fewer row levels has blank cells to its left.
        async Task<(MatrixCellEntry[,], int, int)> RunBuildRowSets(Report rpt)
        {
            var grids = new List<(MatrixCellEntry[,] Grid, int Rows, int Cols, int RowHeaders)>();
            RowGroupings savedGroupings = _RowGroupings;
            MatrixRows savedRows = _MatrixRows;
            try
            {
                var (g, r, c) = await RunBuildSet(rpt);
                grids.Add((g, r, c, _RowGroupings.Items.Count));
                foreach (RowSet set in _ExtraRowSets)
                {
                    _RowGroupings = set.RowGroupings;
                    _MatrixRows = set.MatrixRows;
                    (g, r, c) = await RunBuildSet(rpt);
                    grids.Add((g, r, c, _RowGroupings.Items.Count));
                }
            }
            finally
            {
                _RowGroupings = savedGroupings;
                _MatrixRows = savedRows;
            }

            return MergeRowSets(grids, _ColumnGroupings.Items.Count);
        }

        static (MatrixCellEntry[,], int, int) MergeRowSets(
            List<(MatrixCellEntry[,] Grid, int Rows, int Cols, int RowHeaders)> grids, int headerRows)
        {
            int rowHeaders = 0;
            int dataColumns = int.MaxValue;
            int rows = headerRows;
            foreach (var g in grids)
            {
                rowHeaders = Math.Max(rowHeaders, g.RowHeaders);
                dataColumns = Math.Min(dataColumns, g.Cols - g.RowHeaders);   // the columns are shared
                rows += Math.Max(g.Rows - headerRows, 0);
            }
            int cols = rowHeaders + Math.Max(dataColumns, 0);

            MatrixCellEntry[,] merged = new MatrixCellEntry[rows, cols];
            int top = headerRows;       // where the next set's data rows start
            for (int i = 0; i < grids.Count; i++)
            {
                var g = grids[i];
                int shift = rowHeaders - g.RowHeaders;
                int firstRow = i == 0 ? 0 : headerRows;
                for (int iRow = firstRow; iRow < g.Rows; iRow++)
                {
                    for (int iCol = 0; iCol < g.RowHeaders + Math.Max(dataColumns, 0); iCol++)
                    {
                        MatrixCellEntry mce = g.Grid[iRow, iCol];
                        if (mce == null)
                            continue;
                        bool corner = i == 0 && iRow == 0 && iCol == 0;
                        int targetRow = i == 0 ? iRow : top + (iRow - headerRows);
                        merged[targetRow, corner ? 0 : iCol + shift] = mce;
                        if (corner)
                            mce.ColSpan += shift;
                    }
                }
                top += Math.Max(g.Rows - headerRows, 0);
            }

            // a corner that now spans extra columns is as wide as they are
            MatrixCellEntry cornerEntry = merged[0, 0];
            if (cornerEntry != null && cornerEntry.ColSpan > 1)
            {
                float width = 0;
                for (int iCol = 0; iCol < cornerEntry.ColSpan && iCol < cols; iCol++)
                {
                    for (int iRow = 0; iRow < rows; iRow++)
                    {
                        MatrixCellEntry mce = merged[iRow, iCol];
                        if (mce != null && mce != cornerEntry && mce.ColSpan == 1)
                        {
                            width += mce.Width;
                            break;
                        }
                    }
                }
                if (width > 0)
                    cornerEntry.Width = width;
            }
            return (merged, rows, cols);
        }

        RowSet ParseRowSet(ReportDefn r, XmlNode xNode)
        {
            var set = new RowSet();
            foreach (XmlNode child in xNode.ChildNodes)
            {
                if (child.NodeType != XmlNodeType.Element)
                    continue;
                switch (child.Name.ToLowerInvariant())
                {
                    case "rowgroupings":
                        set.RowGroupings = new RowGroupings(r, this, child);
                        break;
                    case "matrixrows":
                        set.MatrixRows = new MatrixRows(r, this, child);
                        break;
                    default:
                        OwnerReport.rl.LogError(4, "Unknown RowSet element '" + child.Name + "' ignored.");
                        break;
                }
            }
            return set;
        }

        ColumnSet ParseColumnSet(ReportDefn r, XmlNode xNode)
        {
            var set = new ColumnSet();
            foreach (XmlNode child in xNode.ChildNodes)
            {
                if (child.NodeType != XmlNodeType.Element)
                    continue;
                switch (child.Name.ToLowerInvariant())
                {
                    case "columngroupings":
                        set.ColumnGroupings = new ColumnGroupings(r, this, child);
                        break;
                    case "matrixrows":
                        set.MatrixRows = new MatrixRows(r, this, child);
                        break;
                    case "matrixcolumns":
                        set.MatrixColumns = new MatrixColumns(r, this, child);
                        break;
                    default:
                        OwnerReport.rl.LogError(4, "Unknown ColumnSet element '" + child.Name + "' ignored.");
                        break;
                }
            }
            return set;
        }

        async Task<(MatrixCellEntry[,], int numRows, int numCols)> RunBuildSet(Report rpt)
        {
            int numRows;
            int numCols;

            WorkClass wc = GetValue(rpt);
            Rows _Data = wc.Data;

            // loop thru all the data;
            //    form bitmap arrays for each unique data value of each grouping (row and column) value
            int maxColumns = _RowGroupings.Items.Count; // maximum # of columns in matrix
                                                        // at top we need a row per column grouping
            int maxRows = _ColumnGroupings.Items.Count; // maximum # of rows in matrix
                                                        // at left we need a column per row grouping

            MatrixEntry mcg = new MatrixEntry(null, "", null, _Data.Data.Count);
            _ColumnGroupings.SetME(rpt, mcg);
            mcg.FirstRow = 0;
            mcg.LastRow = _Data.Data.Count - 1;
            mcg.Rows = new BitArray(_Data.Data.Count, true);    // all data

            MatrixEntry mrg = new MatrixEntry(null, "", null, _Data.Data.Count);
            _RowGroupings.SetME(rpt, mrg);
            mrg.FirstRow = 0;
            mrg.LastRow = _Data.Data.Count - 1;
            mrg.Rows = new BitArray(_Data.Data.Count, true);        // all data

            int iRow = 0;               // row counter
            foreach (Row r in _Data.Data)
            {
                // Handle the column values
                maxColumns = await HandleColumnGrouping(rpt, wc, _Data, r, mcg, 0, iRow, maxColumns);

                // Handle the row values
                maxRows = await HandleRowGrouping(rpt, wc, _Data, r, mrg, 0, iRow, maxRows);

                iRow++;
            }

            // Determine how many subtotal columns are needed
            maxColumns += RunCountSubtotalColumns(rpt, wc, mcg, 0);

            // Determine how many subtotal rows are needed
            maxRows += RunCountSubtotalRows(rpt, wc, mrg, 0);

            /////
            // Build and populate the 2 dimensional table of MatrixCellEntry
            //    that constitute the matrix
            /////
            MatrixCellEntry[,] matrix = new MatrixCellEntry[maxRows, maxColumns];

            // Do the column headings
            int iColumn = _RowGroupings.Items.Count;
            RunColumnHeaders(rpt, wc, mcg, matrix, _Data, 0, ref iColumn, 0);

            // Do the row headings
            iRow = _ColumnGroupings.Items.Count;
            RunRowHeaders(rpt, wc, mrg, matrix, _Data, ref iRow, 0, 0);

            // Do the row/column data
            iRow = _ColumnGroupings.Items.Count;
            RunDataRow(rpt, wc, mrg, mcg, matrix, _Data, ref iRow, _RowGroupings.Items.Count, 0);

            // Do the corner
            matrix[0, 0] = RunCorner(_Data);

            // now return the matrix data
            numRows = maxRows;
            numCols = maxColumns;
            return (matrix, numRows, numCols);
        }

        int CountMatrixCells
        {
            get
            {
                MatrixRow mr = this.MatrixRows.Items[0] as MatrixRow;
                return mr.MatrixCells.Items.Count;
            }
        }

        int CountMatrixColumns
        {
            get
            {
                return this.MatrixColumns.Items.Count;
            }
        }

        int CountMatrixRows
        {
            get
            {
                return this.MatrixRows.Items.Count;
            }
        }

        ColumnGrouping LastCg
        {
            get { return (ColumnGrouping)(_ColumnGroupings.Items[_ColumnGroupings.Items.Count - 1]); }
        }

        RowGrouping LastRg
        {
            get { return (RowGrouping)(_RowGroupings.Items[_RowGroupings.Items.Count - 1]); }
        }

        /// <summary>
        /// Get the last (dynamic) ColumnGrouping
        /// </summary>
        /// <returns></returns>
        ColumnGrouping LastDynColumnGrouping
        {
            get
            {
                for (int i = _ColumnGroupings.Items.Count - 1; i >= 0; i--)
                {
                    ColumnGrouping cg = (ColumnGrouping)(_ColumnGroupings.Items[i]);
                    if (cg.StaticColumns == null)
                        return cg;
                }
                return (ColumnGrouping)(_ColumnGroupings.Items[_ColumnGroupings.Items.Count - 1]);
            }
        }

        /// <summary>
        /// Get the last (dynamic) RowGrouping
        /// </summary>
        /// <returns></returns>
        RowGrouping LastDynRowGrouping
        {
            get
            {
                for (int i = _RowGroupings.Items.Count - 1; i >= 0; i--)
                {
                    RowGrouping rg = (RowGrouping)(_RowGroupings.Items[i]);
                    if (rg.StaticRows == null)
                        return rg;
                }
                return (RowGrouping)(_RowGroupings.Items[_RowGroupings.Items.Count - 1]);
            }
        }

        async Task<int> HandleRowGrouping(Report rpt, WorkClass wc, Rows rows, Row r, MatrixEntry m, int rgi, int iRow, int maxRows)
        {
            while (rgi < _RowGroupings.Items.Count)
            {
                RowGrouping rg = _RowGroupings.Items[rgi] as RowGrouping;
                Grouping grp = null;
                string result;

                if (rg.StaticRows != null)  // handle static rows
                {
                    for (int sri = 0; sri < rg.StaticRows.Items.Count; sri++)
                    {
                        result = Convert.ToChar(Convert.ToInt32('a') + sri).ToString() + terminal;  // static row; put all data in it
                        StaticRow sr = rg.StaticRows.Items[sri] as StaticRow;
                        MatrixEntry ame;
                        m.HashData.TryGetValue(result, out ame);
                        if (ame == null)
                        {
                            ame = new MatrixEntry(r, result, m, rows.Data.Count);
                            ame.RowGroup = rg;
                            ame.StaticRow = sri;
                            m.HashData.Add(result, ame);
                            if (rg == LastRg)       // Add a row when we add data at lowest level
                                maxRows++;
                        }
                        ame.Rows.Set(iRow, true);
                        // Logic in FirstRow and Last row determine whether value gets set
                        ame.FirstRow = iRow;
                        ame.LastRow = iRow;
                        maxRows = await HandleRowGrouping(rpt, wc, rows, r, ame, rgi + 1, iRow, maxRows);
                    }
                    break;  // handled ones below it recursively
                }
                else                            // handle dynamic columns
                {
                    grp = rg.DynamicRows.Grouping;

                    StringBuilder sb = new StringBuilder();
                    foreach (GroupExpression ge in grp.GroupExpressions.Items)
                    {
                        string temp = await ge.Expression.EvaluateString(rpt, r);
                        if (temp == null || temp == "")
                            sb.Append(nullterminal);
                        else
                            sb.Append(temp);
                        sb.Append(terminal);        // mark end of group 
                    }
                    result = sb.ToString();

                    MatrixEntry ame;
                    m.HashData.TryGetValue(result, out ame);
                    if (ame == null)
                    {
                        ame = new MatrixEntry(r, result, m, rows.Data.Count);
                        ame.RowGroup = rg;
                        m.HashData.Add(result, ame);
                        if (rg == LastRg)       // Add a row when we add data at lowest level
                            maxRows++;
                    }
                    ame.Rows.Set(iRow, true);
                    // Logic in FirstRow and Last row determine whether value gets set
                    ame.FirstRow = iRow;
                    ame.LastRow = iRow;
                    m = ame;            // now go down a level
                    rgi++;
                }
            }

            return maxRows;
        }

        async Task<int> HandleColumnGrouping(Report rpt, WorkClass wc, Rows rows, Row r, MatrixEntry m, int cgi, int iRow, int maxColumns)
        {
            while (cgi < _ColumnGroupings.Items.Count)
            {
                ColumnGrouping cg = _ColumnGroupings.Items[cgi] as ColumnGrouping;
                Grouping grp = null;
                string result;

                if (cg.StaticColumns != null)   // handle static columns
                {
                    for (int sci = 0; sci < cg.StaticColumns.Items.Count; sci++)
                    {
                        result = Convert.ToChar(Convert.ToInt32('a') + sci).ToString() + terminal;  // static column; put all data in it
                        StaticColumn sc = cg.StaticColumns.Items[sci] as StaticColumn;
                        MatrixEntry ame;
                        m.HashData.TryGetValue(result, out ame);
                        if (ame == null)
                        {
                            ame = new MatrixEntry(r, result, m, rows.Data.Count);
                            ame.ColumnGroup = cg;
                            ame.StaticColumn = sci;
                            m.HashData.Add(result, ame);
                            if (cg == LastCg)       // Add a column when we add data at lowest level
                                maxColumns++;
                        }
                        ame.Rows.Set(iRow, true);
                        // Logic in FirstRow and Last row determine whether value gets set
                        ame.FirstRow = iRow;
                        ame.LastRow = iRow;
                        maxColumns = await HandleColumnGrouping(rpt, wc, rows, r, ame, cgi + 1, iRow, maxColumns);
                    }
                    break;  // handled ones below it recursively
                }
                else                            // handle dynamic columns
                {
                    grp = cg.DynamicColumns.Grouping;

                    StringBuilder sb = new StringBuilder();
                    foreach (GroupExpression ge in grp.GroupExpressions.Items)
                    {
                        string temp = await ge.Expression.EvaluateString(rpt, r);
                        if (temp == null || temp == "")
                            sb.Append(nullterminal);
                        else
                            sb.Append(temp);
                        sb.Append(terminal);        // mark end of group 
                    }
                    result = sb.ToString();

                    MatrixEntry ame;
                    m.HashData.TryGetValue(result, out ame);
                    if (ame == null)
                    {
                        ame = new MatrixEntry(r, result, m, rows.Data.Count);
                        ame.ColumnGroup = cg;
                        m.HashData.Add(result, ame);
                        if (cg == LastCg)       // Add a column when we add data at lowest level
                            maxColumns++;
                    }
                    ame.Rows.Set(iRow, true);
                    // Logic in FirstRow and Last row determine whether value gets set
                    ame.FirstRow = iRow;
                    ame.LastRow = iRow;
                    m = ame;            // now go down a level
                    cgi++;
                }
            }

            return maxColumns;

        }

        int RunCountSubtotalColumns(Report rpt, WorkClass wc, MatrixEntry m, int level)
        {
            // Get the number of static columns
            int scCount = Math.Max(1, this._ColumnGroupings.StaticCount);

            int count = 0;
            // Increase the column count when subtotal is requested at this level
            ColumnGrouping cg = (ColumnGrouping)(_ColumnGroupings.Items[level]);
            if (cg.DynamicColumns != null &&
                cg.DynamicColumns.Subtotal != null)
                count = scCount;

            if (m.GetSortedData(rpt) == null || level + 1 >= _ColumnGroupings.Items.Count)
                return count;

            // Now dive into the data
            foreach (MatrixEntry ame in m.GetSortedData(rpt))
            {
                count += RunCountSubtotalColumns(rpt, wc, ame, level + 1);
            }

            return count;
        }

        int RunCountSubtotalRows(Report rpt, WorkClass wc, MatrixEntry m, int level)
        {
            // Get the number of static columns
            int srCount = Math.Max(1, this._RowGroupings.StaticCount);

            int count = 0;
            // Increase the row count when subtotal is requested at this level
            RowGrouping rg = (RowGrouping)(_RowGroupings.Items[level]);
            if (rg.DynamicRows != null &&
                rg.DynamicRows.Subtotal != null)
                count = srCount;

            if (m.GetSortedData(rpt) == null || level + 1 >= _RowGroupings.Items.Count)
                return count;

            // Now dive into the data
            foreach (MatrixEntry ame in m.GetSortedData(rpt))
            {
                count += RunCountSubtotalRows(rpt, wc, ame, level + 1);
            }

            return count;
        }

        void RunColumnHeaders(Report rpt, WorkClass wc, MatrixEntry m, MatrixCellEntry[,] matrix, Rows _Data, int iRow, ref int iColumn, int level)
        {
            foreach (MatrixEntry ame in m.GetSortedData(rpt))
            {
                matrix[iRow, iColumn] = RunGetColumnHeader(rpt, ame, _Data);
                matrix[iRow, iColumn].Width = RunGetColumnWidth(matrix[iRow, iColumn]);
                matrix[iRow, iColumn].Height = ame.ColumnGroup.Height == null ? 0 : ame.ColumnGroup.Height.Points;
                if (ame.GetSortedData(rpt) != null)
                {
                    RunColumnHeaders(rpt, wc, ame, matrix, _Data, iRow + 1, ref iColumn, level + 1);
                }
                else
                    iColumn++;
            }

            ColumnGrouping cg = (ColumnGrouping)(_ColumnGroupings.Items[level]);

            // if we need subtotal on the group
            if (cg.DynamicColumns != null &&
                cg.DynamicColumns.Subtotal != null)
            {
                ReportItem ri = cg.DynamicColumns.Subtotal.ReportItems.Items[0];
                matrix[iRow, iColumn] = new MatrixCellEntry(_Data, ri);
                matrix[iRow, iColumn].Height = cg.Height.Points;
                matrix[iRow, iColumn].Width = RunGetColumnWidth(matrix[iRow, iColumn]);
                RunColumnStaticHeaders(rpt, wc, matrix, _Data, iRow, iColumn, level);
                iColumn += this.CountMatrixCells;
            }
        }

        void RunColumnStaticHeaders(Report rpt, WorkClass wc, MatrixCellEntry[,] matrix, Rows _Data, int iRow, int iColumn, int level)
        {
            ColumnGrouping cg = null;
            for (int i = level + 1; i < _ColumnGroupings.Items.Count; i++)
            {
                iRow++;             // the row will below the headers
                cg = (ColumnGrouping)(_ColumnGroupings.Items[i]);
                if (cg.StaticColumns != null)
                    break;
            }
            if (cg == null || cg.StaticColumns == null)
                return;

            foreach (StaticColumn sc in cg.StaticColumns.Items)
            {
                ReportItem ri = sc.ReportItems.Items[0];
                matrix[iRow, iColumn] = new MatrixCellEntry(_Data, ri);
                matrix[iRow, iColumn].Height = cg.Height.Points;
                matrix[iRow, iColumn].Width = RunGetColumnWidth(matrix[iRow, iColumn]);

                iColumn++;
            }
            return;
        }

        float RunGetColumnWidth(MatrixCellEntry mce)
        {
            if (this.MatrixColumns == null)
                return 0;       // We use this routine for chart(s) and they don't build the matrix columns

            MatrixColumn mcol;  // work variable to hold a MatrixColumn

            mcol = this.MatrixColumns.Items[0] as MatrixColumn;
            float defWidth = mcol.Width.Points;

            if (CountMatrixColumns == 1)    // if only one column width is easy
                return defWidth;

            // find out which static column it is.
            ColumnGrouping cg = null;
            MatrixCells mcells = null;
            ReportItem ri = mce.DisplayItem;
            for (ReportLink rl = ri.Parent; rl != null; rl = rl.Parent)
            {
                if (rl is ColumnGrouping)
                {
                    cg = rl as ColumnGrouping;
                    break;
                }
                if (rl is MatrixCells)
                {
                    mcells = rl as MatrixCells;
                    break;
                }
                if (rl is Matrix)
                    break;
            }

            int offset = 0;

            // If the item is one of the MatrixCell; then use same offset
            if (mcells != null)
            {
                foreach (MatrixCell mcell in mcells.Items)
                {
                    ReportItem ric = mcell.ReportItems.Items[0] as ReportItem;
                    if (ric == ri)
                    {
                        mcol = this.MatrixColumns.Items[offset] as MatrixColumn;
                        return mcol.Width.Points;
                    }
                    offset++;
                }
                return defWidth;
            }

            if (cg == null || cg.StaticColumns == null)
                return defWidth;

            // Otherwise find the same relative Matrix Column from the static columns
            mcol = null;
            foreach (StaticColumn sc in cg.StaticColumns.Items)
            {
                ReportItem cri = sc.ReportItems.Items[0] as ReportItem;
                if (ri == cri)
                {
                    mcol = this.MatrixColumns.Items[offset] as MatrixColumn;
                    break;
                }
                offset++;
            }

            return mcol == null ? defWidth : mcol.Width.Points;
        }

        MatrixCellEntry RunGetColumnHeader(Report rpt, MatrixEntry me, Rows _Data)
        {
            ReportItem ri;
            if (me.ColumnGroup.StaticColumns != null)
            {   // Handle static column reference
                StaticColumn sc = me.ColumnGroup.StaticColumns.Items[me.StaticColumn] as StaticColumn;
                ri = sc.ReportItems.Items[0];
            }
            else
                ri = me.ColumnGroup.DynamicColumns.ReportItems.Items[0];    // dynamic column
            Rows subData = new Rows(rpt, _Data, me.FirstRow, me.LastRow, me.Rows);
            MatrixCellEntry mce = new MatrixCellEntry(subData, ri);

            return mce;
        }

        MatrixCellEntry RunCorner(Rows d)
        {
            if (_Corner == null)
                return null;

            ReportItem ri = _Corner.ReportItems.Items[0];
            MatrixCellEntry mce = new MatrixCellEntry(d, ri);

            float height = 0;
            foreach (ColumnGrouping cg in this.ColumnGroupings.Items)
            {
                height += cg.Height.Points;
            }
            mce.Height = height;

            float width = 0;
            foreach (RowGrouping rg in this.RowGroupings.Items)
            {
                width += rg.Width.Points;
            }
            mce.Width = width;

            mce.ColSpan = RowGroupings.Items.Count;
            return mce;
        }

        void RunDataColumn(Report rpt, WorkClass wc, MatrixEntry rm, MatrixEntry cm, MatrixCellEntry[,] matrix, Rows _Data, int iRow, ref int iColumn, int level, int rowcell)
        {
            BitArray andData;
            MatrixRow mr = this.MatrixRows.Items[rowcell] as MatrixRow;
            float height = mr.Height == null ? 0 : mr.Height.Points;

            foreach (MatrixEntry ame in cm.GetSortedData(rpt))
            {
                if (ame.ColumnGroup != LastCg)
                {
                    RunDataColumn(rpt, wc, rm, ame, matrix, _Data, iRow, ref iColumn, level + 1, rowcell);
                    continue;
                }
                andData = new BitArray(ame.Rows);   // copy the data
                andData.And(rm.Rows);               //  because And is destructive
                matrix[iRow, iColumn] = RunGetMatrixCell(rpt, ame, iRow, _Data, andData,
                        Math.Max(rm.FirstRow, ame.FirstRow),
                        Math.Min(rm.LastRow, ame.LastRow));
                matrix[iRow, iColumn].Height = height;
                matrix[iRow, iColumn].Width = RunGetColumnWidth(matrix[iRow, iColumn]);
                matrix[iRow, iColumn].ColumnME = ame;
                matrix[iRow, iColumn].RowME = rm;

                iColumn++;
            }
            // do we need to subtotal this?
            ColumnGrouping cg = (ColumnGrouping)(_ColumnGroupings.Items[level]);
            if (cg.DynamicColumns != null &&
                cg.DynamicColumns.Subtotal != null)
            {
                andData = new BitArray(cm.Rows);    // copy the data
                andData.And(rm.Rows);               //  because And is destructive
                for (int i = 0; i < this.CountMatrixCells; i++)
                {
                    matrix[iRow, iColumn] = RunGetMatrixCell(rpt, cm, rowcell, i, _Data, andData,
                        Math.Max(rm.FirstRow, cm.FirstRow),
                        Math.Min(rm.LastRow, cm.LastRow));
                    matrix[iRow, iColumn].Height = height;
                    matrix[iRow, iColumn].Width = RunGetColumnWidth(matrix[iRow, iColumn]);
                    matrix[iRow, iColumn].ColumnME = cm;
                    matrix[iRow, iColumn].RowME = rm;
                    iColumn++;
                }
            }
        }

        void RunDataRow(Report rpt, WorkClass wc, MatrixEntry rm, MatrixEntry cm, MatrixCellEntry[,] matrix, Rows _Data, ref int iRow, int iColumn, int level)
        {
            int saveColumn;
            int headerRows = _ColumnGroupings.Items.Count;  // number of column headers we have
            int rgsCount = this.RowGroupings.StaticCount;   // count of static row groups
            foreach (MatrixEntry ame in rm.GetSortedData(rpt))
            {
                if (ame.RowGroup != LastRg)
                {
                    RunDataRow(rpt, wc, ame, cm, matrix, _Data, ref iRow, iColumn, level + 1);
                    continue;
                }
                saveColumn = iColumn;
                int rowcell = rgsCount == 0 ? 0 : (iRow - headerRows) % rgsCount;
                RunDataColumn(rpt, wc, ame, cm, matrix, _Data, iRow, ref saveColumn, 0, rowcell);
                iRow++;
            }
            // do we need to subtotal this?
            RowGrouping rg = (RowGrouping)(_RowGroupings.Items[level]);
            if (rg.DynamicRows != null &&
                rg.DynamicRows.Subtotal != null)
            {
                for (int i = 0; i < this.CountMatrixRows; i++)
                {
                    saveColumn = iColumn;
                    RunDataColumn(rpt, wc, rm, cm, matrix, _Data, iRow, ref saveColumn, 0, i);
                    iRow++;
                }
            }
        }

        void RunRowHeaders(Report rpt, WorkClass wc, MatrixEntry m, MatrixCellEntry[,] matrix, Rows _Data, ref int iRow, int iColumn, int level)
        {
            foreach (MatrixEntry ame in m.GetSortedData(rpt))
            {
                matrix[iRow, iColumn] = RunGetRowHeader(rpt, ame, _Data);
                matrix[iRow, iColumn].Height = RunRowHeight(iRow);
                matrix[iRow, iColumn].Width = ame.RowGroup.Width == null ? 0 : ame.RowGroup.Width.Points;
                if (ame.GetSortedData(rpt) != null)
                {
                    RunRowHeaders(rpt, wc, ame, matrix, _Data, ref iRow, iColumn + 1, level + 1);
                }
                else
                    iRow++;
            }

            RowGrouping rg = (RowGrouping)(_RowGroupings.Items[level]);
            // do we need to subtotal this
            if (rg.DynamicRows != null &&
                rg.DynamicRows.Subtotal != null)
            {                      // TODO need to loop thru static??
                ReportItem ri = rg.DynamicRows.Subtotal.ReportItems.Items[0];
                matrix[iRow, iColumn] = new MatrixCellEntry(_Data, ri);
                matrix[iRow, iColumn].Width = rg.Width.Points;
                matrix[iRow, iColumn].Height = RunRowHeight(iRow);
                RunRowStaticHeaders(rpt, wc, matrix, _Data, iRow, level);
                iRow += Math.Max(1, this.RowGroupings.StaticCount);
            }
        }

        float RunRowHeight(int iRow)
        {
            // calculate the height of this row
            int headerRows = _ColumnGroupings.Items.Count;  // number of column headers we have
            int rgsCount = this.RowGroupings.StaticCount;   // count of static row groups
            int rowcell = rgsCount == 0 ? 0 : (iRow - headerRows) % rgsCount;
            MatrixRow mr = this.MatrixRows.Items[rowcell] as MatrixRow; // get height
            float height = mr.Height == null ? 0 : mr.Height.Points;
            return height;
        }

        void RunRowStaticHeaders(Report rpt, WorkClass wc, MatrixCellEntry[,] matrix, Rows _Data, int iRow, int level)
        {
            RowGrouping rg = null;
            int i;
            int iColumn = 0;
            for (i = level + 1; i < _RowGroupings.Items.Count; i++)
            {
                iColumn++;              // Column for the row static headers
                rg = (RowGrouping)(_RowGroupings.Items[i]);
                if (rg.StaticRows != null)
                    break;
            }
            if (rg == null || rg.StaticRows == null)
                return;

            i = 0;
            foreach (StaticRow sr in rg.StaticRows.Items)
            {
                ReportItem ri = sr.ReportItems.Items[0];
                matrix[iRow, iColumn] = new MatrixCellEntry(_Data, ri);
                matrix[iRow, iColumn].Width = rg.Width.Points;
                MatrixRow mr = this.MatrixRows.Items[i++] as MatrixRow;
                float height = mr.Height == null ? 0 : mr.Height.Points;
                matrix[iRow, iColumn].Height = height;

                iRow++;
            }
            return;
        }

        MatrixCellEntry RunGetRowHeader(Report rpt, MatrixEntry me, Rows _Data)
        {
            ReportItem ri;
            if (me.RowGroup.StaticRows != null)
            {   // Handle static row reference
                StaticRow sr = me.RowGroup.StaticRows.Items[me.StaticRow] as StaticRow;
                ri = sr.ReportItems.Items[0];
            }
            else    // handle dynamic row reference
                ri = me.RowGroup.DynamicRows.ReportItems.Items[0];
            Rows subData = new Rows(rpt, _Data, me.FirstRow, me.LastRow, me.Rows);
            MatrixCellEntry mce = new MatrixCellEntry(subData, ri);

            return mce;

        }

        MatrixCellEntry RunGetMatrixCell(Report rpt, MatrixEntry me, int iRow, Rows _Data, BitArray rows, int firstRow, int lastRow)
        {
            int headerRows = _ColumnGroupings.Items.Count;  // number of column headers we have
            int rgsCount = this.RowGroupings.StaticCount;   // count of static row groups
            int rowcell = rgsCount == 0 ? 0 : (iRow - headerRows) % rgsCount;

            return RunGetMatrixCell(rpt, me, rowcell, me.StaticColumn, _Data, rows, firstRow, lastRow);
        }

        MatrixCellEntry RunGetMatrixCell(Report rpt, MatrixEntry me, int rcell, int ccell, Rows _Data, BitArray rows, int firstRow, int lastRow)
        {
            MatrixRow mr = this._MatrixRows.Items[rcell];
            MatrixCell mc = mr.MatrixCells.Items[ccell];
            ReportItem ri = mc.ReportItems.Items[0];
            Rows subData = new Rows(rpt, _Data, firstRow, lastRow, rows);
            MatrixCellEntry mce = new MatrixCellEntry(subData, ri);

            return mce;
        }

        internal Corner Corner
        {
            get { return _Corner; }
            set { _Corner = value; }
        }

        internal ColumnGroupings ColumnGroupings
        {
            get { return _ColumnGroupings; }
            set { _ColumnGroupings = value; }
        }

        internal Rows GetMyData(Report rpt)
        {
            WorkClass wc = GetValue(rpt);
            return wc.Data;
        }

        internal void SetMyData(Report rpt, Rows data)
        {
            WorkClass wc = GetValue(rpt);
            wc.Data = data;
        }

        internal RowGroupings RowGroupings
        {
            get { return _RowGroupings; }
            set { _RowGroupings = value; }
        }

        internal MatrixRows MatrixRows
        {
            get { return _MatrixRows; }
            set { _MatrixRows = value; }
        }

        internal MatrixColumns MatrixColumns
        {
            get { return _MatrixColumns; }
            set { _MatrixColumns = value; }
        }

        internal MatrixLayoutDirectionEnum LayoutDirection
        {
            get { return _LayoutDirection; }
            set { _LayoutDirection = value; }
        }

        internal int GroupsBeforeRowHeaders
        {
            get { return _GroupsBeforeRowHeaders; }
            set { _GroupsBeforeRowHeaders = value; }
        }

        internal string CellDataElementName
        {
            get { return _CellDataElementName; }
            set { _CellDataElementName = value; }
        }

        // Rendering leaves each group flagged as the last cell left it, usually "totalled over". Put
        // them back so an InScope() evaluated after the matrix, outside it, is not misled.
        private void ResetInScope(Report rpt)
        {
            foreach (ColumnGrouping cg in this.ColumnGroupings.Items)
                cg.DynamicColumns?.Grouping?.SetInScope(rpt, true);
            foreach (ColumnSet set in _ExtraColumnSets)
            {
                foreach (ColumnGrouping cg in set.ColumnGroupings.Items)
                    cg.DynamicColumns?.Grouping?.SetInScope(rpt, true);
            }
            foreach (RowGrouping rg in this.RowGroupings.Items)
                rg.DynamicRows?.Grouping?.SetInScope(rpt, true);
            foreach (RowSet set in _ExtraRowSets)
            {
                foreach (RowGrouping rg in set.RowGroupings.Items)
                    rg.DynamicRows?.Grouping?.SetInScope(rpt, true);
            }
        }

        private void SetGroupingValues(Report rpt, MatrixCellEntry mce)
        {
            WorkClass wc = GetValue(rpt);
            Rows data = wc.FullData;

            SetGroupingValuesInit(rpt, data, mce.RowME, mce.ColumnME);
            SetGroupingValuesMe(rpt, data, mce.RowME);
            SetGroupingValuesMe(rpt, data, mce.ColumnME);

            return;
        }

        private void SetGroupingValuesInit(Report rpt, Rows data, MatrixEntry rme, MatrixEntry cme)
        {
            // handle the column grouping
            if (cme != null)
            {
                foreach (ColumnGrouping cg in this.ColumnGroupings.Items)
                {
                    if (cg.DynamicColumns != null)
                        SetGrouping(rpt, cg.DynamicColumns.Grouping, cme, data, inScope: false);
                }
                // groups of the other column sets are not on this cell's chain either
                foreach (ColumnSet set in _ExtraColumnSets)
                {
                    foreach (ColumnGrouping cg in set.ColumnGroupings.Items)
                    {
                        if (cg.DynamicColumns != null)
                            SetGrouping(rpt, cg.DynamicColumns.Grouping, cme, data, inScope: false);
                    }
                }
            }
            // handle the row grouping
            if (rme != null)
            {
                foreach (RowGrouping rg in this.RowGroupings.Items)
                {
                    if (rg.DynamicRows != null)
                        SetGrouping(rpt, rg.DynamicRows.Grouping, rme, data, inScope: false);
                }
                // groups of the other row sets are not on this cell's chain either
                foreach (RowSet set in _ExtraRowSets)
                {
                    foreach (RowGrouping rg in set.RowGroupings.Items)
                    {
                        if (rg.DynamicRows != null)
                            SetGrouping(rpt, rg.DynamicRows.Grouping, rme, data, inScope: false);
                    }
                }
            }
        }

        private void SetGroupingValuesMe(Report rpt, Rows data, MatrixEntry me)
        {
            if (me == null)
                return;
            // handle the column grouping
            if (me.ColumnGroup != null && me.ColumnGroup.DynamicColumns != null)
                SetGrouping(rpt, me.ColumnGroup.DynamicColumns.Grouping, me, data, inScope: true);

            // handle the row grouping
            if (me.RowGroup != null && me.RowGroup.DynamicRows != null)
                SetGrouping(rpt, me.RowGroup.DynamicRows.Grouping, me, data, inScope: true);

            if (me.Parent != null)  // go up the tree??
                SetGroupingValuesMe(rpt, data, me.Parent);
        }

        // inScope: the group is one the cell's entry chain passes through (true), as opposed to one
        // that SetGroupingValuesInit points at the cell's own data because the cell totals over it.
        private void SetGrouping(Report rpt, Grouping g, MatrixEntry me, Rows data, bool inScope)
        {
            if (g == null)
                return;

            if (me.Data == null)
                me.Data = new Rows(rpt, data, me.FirstRow, me.LastRow, me.Rows);
            g.SetRows(rpt, me.Data);
            g.SetInScope(rpt, inScope);
        }

        internal MatrixCellDataElementOutputEnum CellDataElementOutput
        {
            get { return _CellDataElementOutput; }
            set { _CellDataElementOutput = value; }
        }

        private WorkClass GetValue(Report rpt)
        {
            WorkClass wc = rpt.Cache.Get(this, "wc") as WorkClass;
            if (wc == null)
            {
                wc = new WorkClass();
                rpt.Cache.Add(this, "wc", wc);
            }
            return wc;
        }

        private void RemoveValue(Report rpt)
        {
            rpt.Cache.Remove(this, "wc");
        }

        class WorkClass
        {
            internal Rows Data;
            internal Rows FullData;
            internal WorkClass()
            {
                Data = null;
                FullData = null;
            }
        }
    }
}
