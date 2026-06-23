
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
                _Items.TrimExcess();
		}
		
		async override internal Task FinalPass()
		{
			_HeightOfRows = 0;
			foreach (TableRow t in _Items)
			{
				_HeightOfRows += t.Height.Points;
                await t.FinalPass();
				_CanGrow |= t.CanGrow;
				_CanShrink |= t.CanShrink;
			}

			return;
		}

		internal async Task Run(IPresent ip, Row row)
		{
			foreach (TableRow t in _Items)
			{
                await t.Run(ip, row);
			}
			return;
		}

		internal async Task RunPage(Pages pgs, Row row)
		{
            await RunPage(pgs, row, false);
		}

		internal async Task RunPage(Pages pgs, Row row, bool bCheckRows)
		{
			if (bCheckRows)
			{	// we need to check to see if a row will fit on the page
				foreach (TableRow t in _Items)
				{
					Page p = pgs.CurrentPage;			// this can change after running a row
					float hrows = await t.HeightOfRow(pgs, row);	// height of this row
					float height = p.YOffset + hrows;
					if (height > pgs.BottomOfPage)
					{
						p = OwnerTable.RunPageNew(pgs, p);
                        await OwnerTable.RunPageHeader(pgs, row, false, null);
					}
                    await t.RunPage(pgs, row);
				}
			}
			else
			{	// all rows will fit on the page
				foreach (TableRow t in _Items)
                    await t.RunPage(pgs, row);
			}
			return;
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
			if (!this._CanGrow && !this._CanShrink)
				return _HeightOfRows;

			float height=0;
			foreach (TableRow tr in this._Items)
			{
				height += await tr.HeightOfRow(pgs, r);
			}

			// When rows can shrink, the total may legitimately fall below the sum of
			// defined heights, so the defined-height floor must not be applied.
			if (this._CanShrink)
				return height;

			return Math.Max(height, _HeightOfRows);
		}

        internal List<TableRow> Items
		{
			get { return  _Items; }
		}
	}
}
