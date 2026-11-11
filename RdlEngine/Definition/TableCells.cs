
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// TableCells definition and processing.
	///</summary>
	[Serializable]
	internal class TableCells : ReportLink
	{
        List<TableCell> _Items;			// list of TableCell

		/// <summary>A Tablix row's cells, from their CellContents.</summary>
		internal TableCells(ReportDefn r, ReportLink p, IEnumerable<XmlNode> cellContents) : base(r, p)
		{
			_Items = new List<TableCell>();
			int colIndex = 0;
			foreach (XmlNode contents in cellContents)
			{
				TableCell tc = new TableCell(r, this, contents, colIndex, true);
				colIndex += tc.ColSpan;
				_Items.Add(tc);
			}
			_Items.TrimExcess();
		}

		internal TableCells(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			TableCell tc;
            _Items = new List<TableCell>();
			// Loop thru all the child nodes
			int colIndex=0;			// keep track of the column numbers
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "TableCell":
						tc = new TableCell(r, this, xNodeLoop, colIndex);
						colIndex += tc.ColSpan;
						break;
					default:	
						tc=null;		// don't know what this is
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown TableCells element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
				if (tc != null)
					_Items.Add(tc);
			}
			if (_Items.Count > 0)
                _Items.TrimExcess();
		}
		
		async override internal Task FinalPass()
		{
			foreach (TableCell tc in _Items)
			{
                await tc.FinalPass().ConfigureAwait(false);
			}
			return;
		}

		internal async Task Run(IPresent ip, Row row)
		{
			foreach (TableCell tc in _Items)
			{
                await tc.Run(ip, row).ConfigureAwait(false);
			}
			return ;
		}

		internal async Task RunPage(Pages pgs, Row row)
		{
			// Start each row in the same location
			//   e.g. if there are two embedded tables in cells they both start at same location
			Page savepg = pgs.CurrentPage;
			float savey = savepg.YOffset;
			Page maxpg = savepg;
			float maxy = savey;

			// A cell spanning rows is drawn once the last of them is laid out, from where its
			// first row started, at the rows' height together (TableCell.HeightCalc).
			if (Parent is TableRow tr && tr.SpanEnds != null)
			{
				foreach (TableCell span in tr.SpanEnds)
				{
					if (!(pgs.Report.Cache.Get(span, "spanstart") is SpanStart start))
						continue;	// its first row was hidden
					pgs.Report.Cache.Remove(span, "spanstart");
					Page sp = start.Page;
					float oldx = sp.XOffset, oldy = sp.YOffset;
					pgs.CurrentPage = sp;
					sp.XOffset = start.X;
					sp.YOffset = start.Y;
					Page.Contents before = sp.MarkContents();
					await span.RunPage(pgs, row).ConfigureAwait(false);
					// Rows too tall together for a page are paged one by one (TableRows.RunPage):
					// a cell over rows that went on to later pages is cut at the bottom of the
					// page it starts on, not drawn past it.
					if (sp != savepg)
						sp.ClipItemsAfter(before, pgs.BottomOfPage);
					sp.XOffset = oldx;
					sp.YOffset = oldy;
				}
				pgs.CurrentPage = savepg;
			}

			foreach (TableCell tc in _Items)
			{
				if (tc.RowSpan > 1)
				{	// drawn with the last row it spans, above
					pgs.Report.Cache.AddReplace(tc, "spanstart", new SpanStart(savepg, savepg.XOffset, savey));
					continue;
				}
                await tc.RunPage(pgs, row).ConfigureAwait(false);
				if (maxpg != pgs.CurrentPage)
				{	// a page break
					if (maxpg.PageNumber < pgs.CurrentPage.PageNumber)
					{
						maxpg = pgs.CurrentPage;
						maxy = maxpg.YOffset;
					}
				}
				else if (maxy > pgs.CurrentPage.YOffset)
				{
					// maxy = maxy;      TODO what was this meant to do
				}
				// restore the beginning start of the row
				pgs.CurrentPage = savepg;
				savepg.YOffset = savey;
			}
			pgs.CurrentPage = maxpg;
			savepg.YOffset = maxy;
			return ;
		}

		// Where the row a cell spanning rows starts in was laid out.
		private sealed class SpanStart
		{
			internal readonly Page Page;
			internal readonly float X, Y;
			internal SpanStart(Page page, float x, float y) { Page = page; X = x; Y = y; }
		}

        internal List<TableCell> Items
		{
			get { return  _Items; }
		}
	}
}
