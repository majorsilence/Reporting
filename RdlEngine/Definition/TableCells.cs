
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

			foreach (TableCell tc in _Items)
			{
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

        internal List<TableCell> Items
		{
			get { return  _Items; }
		}
	}
}
