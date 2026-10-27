

using System;
using System.Xml;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Majorsilence.Reporting.RdlEngine.Resources;
using System.Threading.Tasks;

using Majorsilence.Forms.Drawing;
using System.Drawing;  // value types (Color, Point, Size, Rectangle, ...) come from System.Drawing.Primitives

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// TableRow represents a Row in a table.  This can be part of a header, footer, or detail definition.
	///</summary>
	[Serializable]
	internal class TableRow : ReportLink
	{
		TableCells _TableCells;	// Contents of the row. One cell per column
		RSize _Height;				// Height of the row
		Visibility _Visibility;		// Indicates if the row should be hidden		
		bool _CanGrow;			// indicates that row height can increase in size
		bool _CanShrink;		// indicates that row height can decrease in size
		List<Textbox> _GrowList;	// list of TextBox's that need to be checked for growth

		internal TableRow(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_TableCells=null;
			_Height=null;
			_Visibility=null;
			_CanGrow = false;
			_CanShrink = false;
			_GrowList = null;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "TableCells":
						_TableCells = new TableCells(r, this, xNodeLoop);
						break;
					case "Height":
						_Height = new RSize(r, xNodeLoop);
						break;
					case "Visibility":
						_Visibility = new Visibility(r, this, xNodeLoop);
						break;
					default:
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown TableRow element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
			if (_TableCells == null)
				OwnerReport.rl.LogError(8, "TableRow requires the TableCells element.");
			if (_Height == null)
				OwnerReport.rl.LogError(8, "TableRow requires the Height element.");
		}
		
		async override internal Task FinalPass()
		{
            await _TableCells.FinalPass().ConfigureAwait(false);
			if (_Visibility != null)
                await _Visibility.FinalPass().ConfigureAwait(false);

			foreach (TableCell tc in _TableCells.Items)
			{
				ReportItem ri = tc.ReportItems.Items[0] as ReportItem;
				if (!(ri is Textbox))
					continue;
				Textbox tb = ri as Textbox;
				if (tb.CanGrow)
				{
					if (this._GrowList == null)
						_GrowList = new List<Textbox>();
					_GrowList.Add(tb);
					_CanGrow = true;
				}
				if (tb.CanShrink)
					_CanShrink = true;
			}

			if (_CanGrow)				// shrink down the resulting list
                _GrowList.TrimExcess();

			return;
		}

		internal async Task Run(IPresent ip, Row row)
		{
			if (this.Visibility != null && await Visibility.IsHidden(ip.Report(), row).ConfigureAwait(false))
				return;

            await ip.TableRowStart(this, row).ConfigureAwait(false);
            await _TableCells.Run(ip, row).ConfigureAwait(false);
			ip.TableRowEnd(this, row);
			return ;
		}
 
		internal async Task RunPage(Pages pgs, Row row)
		{
			if (this.Visibility != null && await Visibility.IsHidden(pgs.Report, row).ConfigureAwait(false))
				return;

			// A Rectangle that fills the row's only cell stands for the whole row, so its page
			// breaks, which it does not act on itself in a table cell, break the table before or
			// after the row. A header repeated on each new page runs again on the page a break
			// starts, so its rows neither break the page nor move to another.
			bool repeated = this.Parent?.Parent is Header h && h.RepeatOnNewPage;
			Rectangle band = repeated ? null : BandRectangle();
			if (band != null && (band.PageBreakAtStart || band.PageBreakAtEnd)
				&& await band.IsHidden(pgs.Report, row).ConfigureAwait(false))
				band = null;

			if (band != null && band.PageBreakAtStart && !pgs.CurrentPage.IsEmpty())
				await RunPageNew(pgs, row).ConfigureAwait(false);

			if (!repeated && KeepTogether() && !pgs.CurrentPage.IsEmpty())
				await RunPageKeptTogether(pgs, row).ConfigureAwait(false);
			else
				await RunPageRow(pgs, row).ConfigureAwait(false);

			if (band != null && band.PageBreakAtEnd && !pgs.CurrentPage.IsEmpty())
				await RunPageNew(pgs, row).ConfigureAwait(false);
		}

		// A row whose Rectangle is to be kept together, and which ran past the bottom of a page
		// that already held something, is taken off that page and run again at the top of the
		// next, as a row whose measured height does not fit is. Its height is not known until
		// it has run: what a Rectangle or Subreport holds grows. A row that does not fit on a
		// page of its own runs across the break from the top of the next page.
		private async Task RunPageKeptTogether(Pages pgs, Row row)
		{
			Page page = pgs.CurrentPage;
			bool lastPage = page == pgs.LastPage;
			float x = page.XOffset;
			float y = page.YOffset;
			Page.Contents before = page.MarkContents();

			await RunPageRow(pgs, row).ConfigureAwait(false);

			bool overflowed = pgs.CurrentPage == page
				? page.XOffset == x && page.YOffset > pgs.BottomOfPage
				: lastPage && OwnerReport.Body.Columns <= 1;	// only pages the row itself started
			if (!overflowed)
				return;

			while (pgs.LastPage != page)
				pgs.RemoveLastPage();
			pgs.CurrentPage = page;
			page.RemoveContentsAfter(before);
			page.XOffset = x;
			page.YOffset = y;

			await RunPageNew(pgs, row).ConfigureAwait(false);
			await RunPageRow(pgs, row).ConfigureAwait(false);
		}

		private async Task RunPageNew(Pages pgs, Row row)
		{
			Table t = this.Table;
			t.RunPageNew(pgs, pgs.CurrentPage);
			await t.RunPageHeader(pgs, row, false, null).ConfigureAwait(false);
		}

		private async Task RunPageRow(Pages pgs, Row row)
		{
			// A Rectangle or Subreport in a cell grows with what it holds, which HeightOfRow
			// does not measure ahead of drawing it: a growing textbox or a subreport's rows
			// inside one were drawn past the row's bottom and over the rows after it. So the
			// row takes the height its items actually reached, where that is more. A row that
			// moved to another page while it drew keeps its measured height, as before. Only
			// text and images count: a line or a box drawn past the row's bottom is drawn so on
			// purpose, over the rows after it.
			Page page = pgs.CurrentPage;
			float top = page.YOffset;
			HashSet<PageItem> before = HoldsContainers()
				? new HashSet<PageItem>(System.Linq.Enumerable.Cast<PageItem>(page), ReferenceEqualityComparer.Instance)
				: null;

            await _TableCells.RunPage(pgs, row).ConfigureAwait(false);

			WorkClass wc = GetWC(pgs.Report);
			float height = wc.CalcHeight;
			if (before != null && pgs.CurrentPage == page)
			{
				float reached = height;
				foreach (PageItem pi in page)
					if (!before.Contains(pi) && (pi is PageText || pi is PageImage))
						reached = Math.Max(reached, pi.Y + pi.H - top);
				// Past rounding at the band's edge only, which must not move every row down.
				if (reached > height + 0.5f)
					height = reached;
			}
			pgs.CurrentPage.YOffset += height;
		}

		private bool HoldsContainers()
		{
			foreach (TableCell tc in _TableCells.Items)
				if (tc.ReportItems != null && tc.ReportItems.Items.Count > 0
					&& (tc.ReportItems.Items[0] is Majorsilence.Reporting.Rdl.Rectangle || tc.ReportItems.Items[0] is Subreport))
					return true;
			return false;
		}

		// The Rectangle in the row's only cell, if that is what the row holds.
		private Rectangle BandRectangle()
		{
			if (_TableCells.Items.Count != 1)
				return null;
			TableCell tc = _TableCells.Items[0];
			if (tc.ReportItems == null || tc.ReportItems.Items.Count != 1)
				return null;
			return tc.ReportItems.Items[0] as Rectangle;
		}

		private bool KeepTogether()
		{
			foreach (TableCell tc in _TableCells.Items)
				if (tc.ReportItems != null && tc.ReportItems.Items.Count > 0
					&& tc.ReportItems.Items[0] is Rectangle rect && rect.KeepTogether)
					return true;
			return false;
		}

		internal TableCells TableCells
		{
			get { return  _TableCells; }
			set {  _TableCells = value; }
		}

		internal RSize Height
		{
			get { return  _Height; }
			set {  _Height = value; }
		}
        internal async Task<float> HeightOfRow(Pages pgs, Row r)
        {
            return await HeightOfRow(pgs.Report, pgs.G, r).ConfigureAwait(false);
        }
		internal async Task<float> HeightOfRow(Report rpt, Graphics g, Row r)
		{
			WorkClass wc = GetWC(rpt);
			if (this.Visibility != null && await Visibility.IsHidden(rpt, r).ConfigureAwait(false))
			{
				wc.CalcHeight = 0;
				return 0;
			}

			float defnHeight = _Height.Points;
			if (!_CanGrow && !_CanShrink)
			{
				wc.CalcHeight = defnHeight;
				return defnHeight;
			}

			// Determine the row height from the natural (content) height of each cell.
			// A fixed cell always needs the defined height; a CanGrow cell may exceed
			// it; a CanShrink cell may fall below it.  The row takes the tallest
			// requirement of its cells.
			TableColumns tcs = this.Table.TableColumns;
			float height = 0;
			foreach (TableCell tc in _TableCells.Items)
			{
				Textbox tb = tc.ReportItems.Items[0] as Textbox;
				float cellHeight;
				if (tb != null && (tb.CanGrow || tb.CanShrink))
				{
					int ci = tb.TC.ColIndex;
					if (await tcs[ci].IsHidden(rpt, r).ConfigureAwait(false))    // if column is hidden don't use in calculation
						continue;
					float content = await tb.RunTextCalcHeight(rpt, g, r).ConfigureAwait(false);
					if (tb.CanGrow && !tb.CanShrink)
						cellHeight = Math.Max(content, defnHeight);  // grow only
					else
						cellHeight = content;                        // shrink-only or grow+shrink
				}
				else
					cellHeight = defnHeight;                         // fixed item needs full height
				height = Math.Max(height, cellHeight);
			}

			// Preserve prior behaviour: a CanGrow row never drops below its defined
			// height.  A CanShrink-only row may shrink but never exceed it.
			if (_CanGrow)
				height = Math.Max(height, defnHeight);
			else
				height = Math.Min(height, defnHeight);

			wc.CalcHeight = height;
			return wc.CalcHeight;
		}

		internal float HeightCalc(Report rpt)
		{
			WorkClass wc = GetWC(rpt);
			return wc.CalcHeight;
		}

        private Table Table
        {
            get
            {
                ReportLink p= this.Parent;
                while (p != null)
                {
                    if (p is Table)
                        return p as Table;
                    p = p.Parent;
                }
                throw new Exception(Strings.TableRow_Error_TableRowNotRelatedToTable);
            }
        }

            internal Visibility Visibility
		{
			get { return  _Visibility; }
			set {  _Visibility = value; }
		}

		internal bool CanGrow
		{
			get { return _CanGrow; }
		}

		internal bool CanShrink
		{
			get { return _CanShrink; }
		}

		internal List<Textbox> GrowList
		{
			get { return _GrowList; }
		}

		private WorkClass GetWC(Report rpt)
		{
			WorkClass wc = rpt.Cache.Get(this, "wc") as WorkClass;
			if (wc == null)
			{
				wc = new WorkClass(this);
				rpt.Cache.Add(this, "wc", wc);
			}
			return wc;
		}

		private void RemoveWC(Report rpt)
		{
			rpt.Cache.Remove(this, "wc");
		}

		class WorkClass
		{
			internal float CalcHeight;		// dynamic when CanGrow true
			internal WorkClass(TableRow tr)
			{
				CalcHeight = tr.Height.Points;
			}
		}
	}
}
