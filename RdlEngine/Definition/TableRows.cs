
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using Majorsilence.Reporting.RdlEngine.Resources;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// TableRows definition and processing.
	///</summary>
	[Serializable]
	internal class TableRows : ReportLink
	{
        List<TableRow> _Items;			// list of TableRow
		float _HeightOfRows;		// height of contained rows
		bool _CanGrow;				// if any TableRow contains a TextBox with CanGrow
		bool _CanShrink;			// if any TableRow contains a TextBox with CanShrink
		bool _CanHide;				// if any TableRow has a Visibility

		internal TableRows(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			TableRow t;
            _Items = new List<TableRow>();
			_CanGrow = false;
			_CanShrink = false;
			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "TableRow":
						t = new TableRow(r, this, xNodeLoop);
						break;
					default:	
						t=null;		// don't know what this is
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown TableRows element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
				if (t != null)
					_Items.Add(t);
			}
			if (_Items.Count == 0)
				OwnerReport.rl.LogError(8, "For TableRows at least one TableRow is required.");
			else
			{
                _Items.TrimExcess();
				AssignRowSpans();
			}
		}

		// A cell with a RowSpan covers its columns in the rows below it, which have no cell
		// of their own there: their cells take the columns the span leaves free, left to right.
		// A span reaches no further than these rows (a header's rows, a group footer's, the
		// rows of one detail record).
		private void AssignRowSpans()
		{
			var coveredUntil = new List<int>();		// per column: the first row no longer covered
			var spanning = new List<TableCell>();	// the cells spanning rows, in the order met
			for (int ri = 0; ri < _Items.Count; ri++)
			{
				TableRow tr = _Items[ri];
				if (tr.TableCells == null)
					continue;
				int col = 0;
				foreach (TableCell tc in tr.TableCells.Items)
				{
					while (col < coveredUntil.Count && coveredUntil[col] > ri)
						col++;
					tc.ColIndex = col;
					if (tc.RowSpan > 1)
					{
						if (ri + tc.RowSpan > _Items.Count)
						{
							OwnerReport.rl.LogError(4, string.Format("A TableCell spans {0} rows where only {1} follow it; it spans those.", tc.RowSpan, _Items.Count - ri));
							tc.RowSpan = _Items.Count - ri;
						}
						for (int c = col; c < col + tc.ColSpan; c++)
						{
							while (coveredUntil.Count <= c)
								coveredUntil.Add(0);
							coveredUntil[c] = ri + tc.RowSpan;
						}
						if (tc.RowSpan > 1)
							spanning.Add(tc);
					}
					col += tc.ColSpan;
				}
			}

			foreach (TableCell tc in spanning)
			{
				int first = _Items.IndexOf(tc.Row);
				for (int ri = first + 1; ri < first + tc.RowSpan; ri++)
					_Items[ri].AddCoveringCell(tc, ri == first + tc.RowSpan - 1);
			}
		}
		
		async override internal Task FinalPass()
		{
			_HeightOfRows = 0;
			foreach (TableRow t in _Items)
			{
				_HeightOfRows += t.Height.Points;
                await t.FinalPass().ConfigureAwait(false);
				_CanGrow |= t.CanGrow;
				_CanShrink |= t.CanShrink;
				_CanHide |= t.Visibility != null;
			}

			return;
		}

		internal async Task Run(IPresent ip, Row row)
		{
			foreach (TableRow t in _Items)
			{
                await t.Run(ip, row).ConfigureAwait(false);
			}
			return;
		}

		internal async Task RunPage(Pages pgs, Row row)
		{
            await RunPage(pgs, row, false).ConfigureAwait(false);
		}

		internal async Task RunPage(Pages pgs, Row row, bool bCheckRows)
		{
			if (bCheckRows)
			{	// we need to check to see if a row will fit on the page
				int keepUntil = -1;		// the last row of the rows a cell spans, which go to a new page together
				float? newPageRoom = null;	// what a new page leaves for rows; measured when first needed
				for (int i = 0; i < _Items.Count; i++)
				{
					TableRow t = _Items[i];
					Page p = pgs.CurrentPage;			// this can change after running a row
					float hrows = await t.HeightOfRow(pgs, row).ConfigureAwait(false);	// height of this row
					if (i > keepUntil && (keepUntil = LastRowSpanned(i, true)) > i)
					{	// a cell spanning rows starts here: the rows it spans must fit below, where they
						// fit on a page at all. Measured top down: a span's last row grows by what the
						// rows above it leave of its text. Overlapping spans are kept together as one
						// block when that fits on a new page; otherwise only the spans starting here
						// are; and rows that do not fit on a new page in any case are paged row by row,
						// as any, rather than moved to a page they would run off as well.
						newPageRoom ??= await OwnerTable.HeightOfNewPage(pgs, row, null).ConfigureAwait(false);
						float block = await HeightOfRows(pgs, row, i, keepUntil).ConfigureAwait(false);
						if (block > newPageRoom.Value)
						{
							keepUntil = LastRowSpanned(i, false);
							block = await HeightOfRows(pgs, row, i, keepUntil).ConfigureAwait(false);
							if (block > newPageRoom.Value)
							{
								keepUntil = i;
								block = hrows;
							}
						}
						hrows = block;
					}
					float height = p.YOffset + hrows;
					if (height > pgs.BottomOfPage && !p.IsEmpty())	// an empty page is kept (see Details.RunPage)
					{
                        await OwnerTable.RunPageBottomRows(pgs, row, false).ConfigureAwait(false);
						p = OwnerTable.RunPageNew(pgs, p);
                        await OwnerTable.RunPageHeader(pgs, row, false, null).ConfigureAwait(false);
					}
                    await t.RunPage(pgs, row).ConfigureAwait(false);
				}
			}
			else
			{	// all rows will fit on the page
				foreach (TableRow t in _Items)
                    await t.RunPage(pgs, row).ConfigureAwait(false);
			}
			return;
		}

		// The last row that a cell starting in row i spans; with overlapping, also the last that a
		// cell starting in a row spanned with it spans, and so on.
		private int LastRowSpanned(int i, bool overlapping)
		{
			int last = i;
			for (int ri = i; ri <= (overlapping ? last : i); ri++)
				foreach (TableCell tc in _Items[ri].TableCells.Items)
					if (tc.RowSpan > 1)
						last = Math.Max(last, ri + tc.RowSpan - 1);
			return Math.Min(last, _Items.Count - 1);
		}

		private async Task<float> HeightOfRows(Pages pgs, Row row, int first, int last)
		{
			float height = 0;
			for (int j = first; j <= last; j++)
				height += await _Items[j].HeightOfRow(pgs, row).ConfigureAwait(false);
			return height;
		}

		internal Table OwnerTable
		{
			get 
			{
				for (ReportLink rl = this.Parent; rl != null; rl = rl.Parent)
				{
					if (rl is Table)
						return rl as Table;
				}

				throw new Exception(Strings.TableRows_Error_TableRowsMustOwnedTable);
			}
		}

		internal float DefnHeight()
		{
			float height=0;
			foreach (TableRow tr in this._Items)
			{
				height += tr.Height.Points;
			}
			return height;
		}

		internal async Task<float> HeightOfRows(Pages pgs, Row r)
		{
			if (!this._CanGrow && !this._CanShrink && !this._CanHide)
				return _HeightOfRows;

			// A hidden row takes no room, and every other row takes at least its defined
			// height unless it can shrink, so the sum is all the rows need: counting a hidden
			// row's defined height pushed a footer of mostly hidden rows to a new page.
			float height=0;
			foreach (TableRow tr in this._Items)
			{
				height += await tr.HeightOfRow(pgs, r).ConfigureAwait(false);
			}
			return height;
		}

        internal List<TableRow> Items
		{
			get { return  _Items; }
		}
	}
}
