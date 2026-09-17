
using System;
using System.Xml;
using System.IO;
using System.Collections;
using System.Text;


namespace Majorsilence.Reporting.RdlDesign
{
	/// <summary>
	/// MatrixView:  builds a simplfied representation of the matrix so that it
	///  can be drawn or hit test in a simplified fashion.
	/// </summary>
	internal class MatrixView
	{
		DesignXmlDraw _Draw;
		XmlNode _MatrixNode;
		int _Rows;
		int _HeaderRows;
		int _Columns;
		int _HeaderColumns;
		float _Height;
		float _Width;
		MatrixItem[,] _MatrixView;
		string _ViewBuilt=null;

		public MatrixView(DesignXmlDraw dxDraw, XmlNode matrix)
		{
			_Draw = dxDraw;
			_MatrixNode = matrix;
			try
			{
				BuildView();
			}
			catch (Exception e)
			{
				_ViewBuilt = e.Message;
			}
		}

		internal MatrixItem this[int row, int column] 
		{
			get {return _MatrixView[row, column]; }
		}

		internal int Columns
		{
			get {return _Columns;}
		}

		internal int Rows
		{
			get {return _Rows;}
		}

		internal int HeaderColumns
		{
			get {return _HeaderColumns;}
		}

		internal int HeaderRows
		{
			get {return _HeaderRows;}
		}

		internal float Height
		{
			get {return _Height;}
		}

		internal float Width
		{
			get {return _Width;}
		}

		void BuildView()
		{
			CountRowColumns();		// get the total count of rows and columns
			_MatrixView = new MatrixItem[_Rows, _Columns];	// allocate the 2-dimensional array
			FillMatrix();
		}

		void CountRowColumns()
		{
			int mcc = CountMatrixColumns();	
			int mrc = CountMatrixRows();

			int iColumnGroupings = this.CountColumnGroupings();
			int iRowGroupings = this.CountRowGroupings();

			_Rows = mrc + this.CountColumnGroupings() + 
				CountRowGroupingSubtotals() * mrc;

			_Columns = mcc + iRowGroupings +
				CountColumnGroupingSubtotals() * mcc;

			_HeaderRows = iColumnGroupings;
			_HeaderColumns = iRowGroupings;
		}

		void FillMatrix()
		{
			FillMatrixColumnGroupings();

			FillMatrixRowGroupings();

			FillMatrixCorner();

			FillMatrixCells();

			FillMatrixHeights();

			FillMatrixWidths();

			FillMatrixCornerHeightWidth();
		}

		void FillMatrixHeights()
		{
			// fill out the heights for each row
			this._Height = 0;
			for (int row=0; row < this.Rows; row++)
			{
				float height=0;
				for (int col= 0; col < this.Columns; col++)
				{
					MatrixItem mi = _MatrixView[row,col];
					height = Math.Max(height, mi.Height);
				}
				for (int col= 0; col < this.Columns; col++)
					_MatrixView[row,col].Height = height;

				this._Height += height;
			}
		}

		void FillMatrixWidths()
		{
			// fill out the widths for each column
			this._Width = 0;
			for (int col=0; col < this.Columns; col++)
			{
				float width=0;
				for (int row= 0; row < this.Rows; row++)
				{
					MatrixItem mi = _MatrixView[row,col];
					width = Math.Max(width, mi.Width);
				}
				for (int row= 0; row < this.Rows; row++)
					_MatrixView[row,col].Width = width;

				this._Width += width;
			}
		}

		void FillMatrixCornerHeightWidth()
		{
			if (this.Columns == 0 || this.Rows == 0)
				return;

			// set the height and width for the corner
			MatrixItem mi = _MatrixView[0,0];
			mi.Height = 0;
			for (int row=0; row < this._HeaderRows; row++)
				mi.Height += _MatrixView[row, 1].Height;
			mi.Width = 0;
			for (int col=0; col < this._HeaderColumns; col++)
				mi.Width += _MatrixView[1, col].Width;
		}

		void FillMatrixCells()
		{
			// get a collection with the matrix cells
			int staticRows = this.CountMatrixRows();
			int staticCols = this.CountMatrixColumns();
			XmlNode[,] rc = new XmlNode[staticRows, staticCols];

			XmlNode mrows = DesignXmlDraw.FindNextInHierarchy(_MatrixNode, "MatrixRows");
			int ri=0;
			foreach (XmlNode mrow in mrows.ChildNodes)
			{
				int ci=0;
				XmlNode mcells = DesignXmlDraw.FindNextInHierarchy(mrow, "MatrixCells");
				foreach (XmlNode mcell in mcells.ChildNodes)
				{
					// obtain the matrix cell
					XmlNode repi = DesignXmlDraw.FindNextInHierarchy(mcell, "ReportItems");
					rc[ri,ci] = repi;
					ci++;
				}
				ri++;
			}
			// now fill out the rest of the matrix with empty entries
			MatrixItem mi;

			// Fill the middle (MatrixCells) with the contents of MatrixCells repeated
			for (int row=_HeaderRows; row < this.Rows; row++)
			{
				int rowcell = staticRows == 0? 0: (row - _HeaderRows) % staticRows;
				int mcellCount=0;
				for (int col= _HeaderColumns; col < this.Columns; col++)
				{
					if (_MatrixView[row, col] == null)
					{
						float width = GetMatrixColumnWidth(mcellCount);
						float height = GetMatrixRowHeight(rowcell);
						XmlNode n = rc[rowcell, mcellCount++] as XmlNode;
						if (mcellCount >= staticCols)
							mcellCount=0;
						mi = new MatrixItem(n);
						mi.Width = width;
						mi.Height = height;
						_MatrixView[row, col] = mi;
					}
				}
			}

			// Make sure we have no null entries
			for (int row=0; row < this.Rows; row++)
			{
				for (int col= 0; col < this.Columns; col++)
				{
					if (_MatrixView[row, col] == null)
					{
						mi = new MatrixItem(null);
						_MatrixView[row, col] = mi;
					}
				}
			}

		}

		void FillMatrixCorner()
		{
			XmlNode corner = _Draw.GetNamedChildNode(_MatrixNode, "Corner");
			if (corner == null)
				return;

			XmlNode ris = DesignXmlDraw.FindNextInHierarchy(corner, "ReportItems");
			MatrixItem mi = new MatrixItem(ris);
			_MatrixView[0,0] = mi;
		}

		float GetMatrixColumnWidth(int count)
		{
			XmlNode mcs =  DesignXmlDraw.FindNextInHierarchy(_MatrixNode, "MatrixColumns");

			foreach (XmlNode c in mcs.ChildNodes)
			{
				if (c.Name != "MatrixColumn")
					continue;
				if (count == 0)
					return _Draw.GetSize(c, "Width");
				count--;
			}
			return 0;
		}

		void FillMatrixColumnGroupings()
		{
			XmlNode cGroupings = _Draw.GetNamedChildNode(_MatrixNode, "ColumnGroupings");
			if (cGroupings == null)
				return;

			int rows=0;
			int cols=this._HeaderColumns;
			MatrixItem mi;

			XmlNode ris;			// work variable to hold reportitems
			int staticCols = this.CountMatrixColumns();

			int subTotalCols=DesignXmlDraw.CountChildren(cGroupings, "ColumnGrouping", "DynamicColumns", "Subtotal");
			foreach (XmlNode c in cGroupings.ChildNodes)
			{
				if (c.Name != "ColumnGrouping")
					continue;
				XmlNode scol = DesignXmlDraw.FindNextInHierarchy(c, "StaticColumns");
				if (scol != null)
				{	// Static columns
					int ci=0;
					foreach (XmlNode sc in scol.ChildNodes)
					{
						if (sc.Name != "StaticColumn")
							continue;
						ris = DesignXmlDraw.FindNextInHierarchy(sc, "ReportItems");
						mi = new MatrixItem(ris);
						mi.Height = _Draw.GetSize(c, "Height");
						mi.Width = GetMatrixColumnWidth(ci);
						_MatrixView[rows, _HeaderColumns+ci] = mi;
						ci++;
					}
				}
				else
				{	// Dynamic Columns
					ris = DesignXmlDraw.FindNextInHierarchy(c, "DynamicColumns", "ReportItems");
					mi = new MatrixItem(ris);
					mi.Height = _Draw.GetSize(c, "Height");
					mi.Width = GetMatrixColumnWidth(0);
					_MatrixView[rows, _HeaderColumns] = mi;

					XmlNode subtotal = DesignXmlDraw.FindNextInHierarchy(c, "DynamicColumns", "Subtotal");
					if (subtotal != null)
					{
						ris = DesignXmlDraw.FindNextInHierarchy(subtotal, "ReportItems");
						mi = new MatrixItem(ris);
						mi.Height = _Draw.GetSize(c, "Height");
						mi.Width = GetMatrixColumnWidth(0);		// TODO this is wrong!! should be total of all static widths
						_MatrixView[rows, _HeaderColumns+(staticCols-1)+subTotalCols] = mi;
						subTotalCols--;
					}
				}
				rows++;		// add a row per ColumnGrouping
			}
		}

		float GetMatrixRowHeight(int count)
		{
			XmlNode mcs =  DesignXmlDraw.FindNextInHierarchy(_MatrixNode, "MatrixRows");

			foreach (XmlNode c in mcs.ChildNodes)
			{
				if (c.Name != "MatrixRow")
					continue;
				if (count == 0)
					return _Draw.GetSize(c, "Height");
				count--;
			}
			return 0;
		}
		
		void FillMatrixRowGroupings()
		{
			XmlNode rGroupings = _Draw.GetNamedChildNode(_MatrixNode, "RowGroupings");
			if (rGroupings == null)
				return;
	    	float height = _Draw.GetSize(
				DesignXmlDraw.FindNextInHierarchy(_MatrixNode, "MatrixRows", "MatrixRow"),
				"Height");
			int cols = 0;
			int staticRows = this.CountMatrixRows();
			int subtotalrows= DesignXmlDraw.CountChildren(rGroupings, "RowGrouping", "DynamicRows", "Subtotal");
			MatrixItem mi;
			foreach (XmlNode c in rGroupings.ChildNodes)
			{
				if (c.Name != "RowGrouping")
					continue;

				XmlNode srow = DesignXmlDraw.FindNextInHierarchy(c, "StaticRows");
				if (srow != null)
				{	// Static rows
					int ri=0;
					foreach (XmlNode sr in srow.ChildNodes)
					{
						if (sr.Name != "StaticRow")
							continue;
						XmlNode ris = DesignXmlDraw.FindNextInHierarchy(sr, "ReportItems");
						mi = new MatrixItem(ris);
						mi.Width = _Draw.GetSize(c, "Width");
						mi.Height = GetMatrixRowHeight(ri);
						_MatrixView[_HeaderRows+ri, cols] = mi;
						ri++;
					}
				}
				else
				{
					XmlNode ris = DesignXmlDraw.FindNextInHierarchy(c, "DynamicRows", "ReportItems");
					mi = new MatrixItem(ris);
					mi.Width = _Draw.GetSize(c, "Width");
					mi.Height = height;
					_MatrixView[_HeaderRows, cols] = mi;

					XmlNode subtotal = DesignXmlDraw.FindNextInHierarchy(c, "DynamicRows", "Subtotal");
					if (subtotal != null)
					{
						ris = DesignXmlDraw.FindNextInHierarchy(subtotal, "ReportItems");
						mi = new MatrixItem(ris);
						mi.Width = _Draw.GetSize(c, "Width");
						mi.Height = height;
						_MatrixView[_HeaderRows+(staticRows-1)+subtotalrows, cols] = mi;
						subtotalrows--;	// these go backwards 
					}
				}
				cols++;		// add a column per RowGrouping
			}
		}

		/// <summary>
		/// Returns the count of static columns or 1
		/// </summary>
		/// <returns></returns>
		int CountMatrixColumns()
		{
			XmlNode cGroupings = _Draw.GetNamedChildNode(_MatrixNode, "ColumnGroupings");
			if (cGroupings == null)
				return 1;	// 1 column

			// Get the number of static columns
			foreach (XmlNode c in cGroupings.ChildNodes)
			{
				if (c.Name != "ColumnGrouping")
					continue;
				XmlNode scol = DesignXmlDraw.FindNextInHierarchy(c, "StaticColumns");
				if (scol == null)	// must be dynamic column
					continue;
				int ci=0;
				foreach (XmlNode sc in scol.ChildNodes)
				{
					if (sc.Name == "StaticColumn")
						ci++;
				}
				return ci;		// only one StaticColumns allowed in a column grouping
			}
			return 1;	// 1 column
		}
		/// <summary>
		/// Returns the count of static rows or 1
		/// </summary>
		/// <returns></returns>
		int CountMatrixRows()
		{
			XmlNode rGroupings = _Draw.GetNamedChildNode(_MatrixNode, "RowGroupings");
			if (rGroupings == null)
				return 1;	// 1 row

			// Get the number of static columns
			foreach (XmlNode c in rGroupings.ChildNodes)
			{
				if (c.Name != "RowGrouping")
					continue;
				XmlNode scol = DesignXmlDraw.FindNextInHierarchy(c, "StaticRows");
				if (scol == null)	// must be dynamic column
					continue;
				int ci=0;
				foreach (XmlNode sc in scol.ChildNodes)
				{
					if (sc.Name == "StaticRow")
						ci++;
				}
				return ci;		// only one StaticRows allowed in a row grouping
			}
			return 1;	// 1 row
		}
		/// <summary>
		/// Returns the count of ColumnGroupings
		/// </summary>
		/// <returns></returns>
		int CountColumnGroupings()
		{
			XmlNode cGroupings = _Draw.GetNamedChildNode(_MatrixNode, "ColumnGroupings");
			if (cGroupings == null)
				return 0;

			// Get the number of column groups
			int ci=0;
			foreach (XmlNode c in cGroupings.ChildNodes)
			{
				if (c.Name != "ColumnGrouping")
					continue;
				ci++;
			}
			return ci;	
		}
		/// <summary>
		/// Returns the count of row grouping
		/// </summary>
		/// <returns></returns>
		int CountRowGroupings()
		{
			XmlNode rGroupings = _Draw.GetNamedChildNode(_MatrixNode, "RowGroupings");
			if (rGroupings == null)
				return 0;	// 1 row

			// Get the number of row groupings
			int ri=0;
			foreach (XmlNode c in rGroupings.ChildNodes)
			{
				if (c.Name != "RowGrouping")
					continue;
				ri++;
			}
			return ri;	// row groupings
		}

		/// <summary>
		/// Returns the count of ColumnGroupings with subtotals
		/// </summary>
		/// <returns></returns>
		int CountColumnGroupingSubtotals()
		{
			XmlNode cGroupings = _Draw.GetNamedChildNode(_MatrixNode, "ColumnGroupings");
			if (cGroupings == null)
				return 0;

			// Get the number of column groups with subtotals
			int ci=0;
			foreach (XmlNode c in cGroupings.ChildNodes)
			{
				if (c.Name != "ColumnGrouping")
					continue;

				XmlNode subtotal = DesignXmlDraw.FindNextInHierarchy(c, "DynamicColumns", "Subtotal");
				if (subtotal != null)																						
					ci++;
			}
			return ci;	
		}
		/// <summary>
		/// Returns the count of row grouping subtotals
		/// </summary>
		/// <returns></returns>
		int CountRowGroupingSubtotals()
		{
			XmlNode rGroupings = _Draw.GetNamedChildNode(_MatrixNode, "RowGroupings");
			if (rGroupings == null)
				return 0;	// 1 row

			// Get the number of row groupings
			int ri=0;
			foreach (XmlNode c in rGroupings.ChildNodes)
			{
				if (c.Name != "RowGrouping")
					continue;
				XmlNode subtotal = DesignXmlDraw.FindNextInHierarchy(c, "DynamicRows", "Subtotal");
				if (subtotal != null)																						
					ri++;
			}
			return ri;	// row grouping subtotals
		}

	}

	class MatrixItem
	{
		XmlNode _ReportItem;
		float _Width;
		float _Height;

		public MatrixItem(XmlNode ri)
		{
			_ReportItem = ri;
		}

		internal XmlNode ReportItem
		{
			get {return _ReportItem;}
			set {_ReportItem = value;}
		}

		internal float Width
		{
			get {return _Width;}
			set {_Width = value;}
		}

		internal float Height
		{
			get {return _Height;}
			set {_Height = value;}
		}
	}
}
