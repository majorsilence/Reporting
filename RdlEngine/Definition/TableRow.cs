

using System;
using System.Xml;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Majorsilence.Reporting.RdlEngine.Resources;
using System.Threading.Tasks;

#if DRAWINGCOMPAT
using Majorsilence.Drawing;
#else
using System.Drawing;
#endif

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
		List<Textbox> _ShrinkList;	// list of TextBox's that need to be checked for shrinking

		internal TableRow(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_TableCells=null;
			_Height=null;
			_Visibility=null;
			_CanGrow = false;
			_CanShrink = false;
			_GrowList = null;
			_ShrinkList = null;

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
            await _TableCells.FinalPass();
			if (_Visibility != null)
                await _Visibility.FinalPass();

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
				{
					if (this._ShrinkList == null)
						_ShrinkList = new List<Textbox>();
					_ShrinkList.Add(tb);
					_CanShrink = true;
				}
			}

			if (_CanGrow)				// shrink down the resulting list
                _GrowList.TrimExcess();
			if (_CanShrink)
				_ShrinkList.TrimExcess();

			return;
		}

		internal async Task Run(IPresent ip, Row row)
		{
			if (this.Visibility != null && await Visibility.IsHidden(ip.Report(), row))
				return;

            await ip.TableRowStart(this, row);
            await _TableCells.Run(ip, row);
			ip.TableRowEnd(this, row);
			return ;
		}
 
		internal async Task RunPage(Pages pgs, Row row)
		{
			if (this.Visibility != null && await Visibility.IsHidden(pgs.Report, row))
				return;

            await _TableCells.RunPage(pgs, row);

			WorkClass wc = GetWC(pgs.Report);
			pgs.CurrentPage.YOffset += wc.CalcHeight;
			return ;
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
            return await HeightOfRow(pgs.Report, pgs.G, r);
        }
		internal async Task<float> HeightOfRow(Report rpt, Graphics g, Row r)
		{
			WorkClass wc = GetWC(rpt);
			if (this.Visibility != null && await Visibility.IsHidden(rpt, r))
			{
				wc.CalcHeight = 0;
				return 0;
			}

			float defnHeight = _Height.Points;
			
			// If neither CanGrow nor CanShrink, use defined height
			if (!_CanGrow && !_CanShrink)
			{
				wc.CalcHeight = defnHeight;
				return defnHeight;
			}

            TableColumns tcs= this.Table.TableColumns;
			float height=0;
			
			// Calculate height for CanGrow textboxes
			if (_CanGrow)
			{
				foreach (Textbox tb in this._GrowList)
				{
					int ci = tb.TC.ColIndex;
					if (await tcs[ci].IsHidden(rpt, r))    // if column is hidden don't use in calculation
						continue;
					height = Math.Max(height, await tb.RunTextCalcHeight(rpt, g, r));
				}
			}
			
			// Calculate height for CanShrink textboxes
			if (_CanShrink)
			{
				float minHeight = defnHeight;  // Start with defined height as maximum
				foreach (Textbox tb in this._ShrinkList)
				{
					int ci = tb.TC.ColIndex;
					if (await tcs[ci].IsHidden(rpt, r))    // if column is hidden don't use in calculation
						continue;
					float calcHeight = await tb.RunTextCalcHeight(rpt, g, r);
					// For shrink, we want the minimum of calculated heights
					minHeight = Math.Min(minHeight, calcHeight);
				}
				
				// If we have both CanGrow and CanShrink textboxes
				if (_CanGrow)
				{
					// Use the calculated height from grow, but allow shrinking below defined height
					height = Math.Max(height, minHeight);
				}
				else
				{
					// Only CanShrink - use the minimum
					height = minHeight;
				}
			}
			
			// Apply the final height calculation
			if (_CanGrow && !_CanShrink)
			{
				// Only CanGrow: height can be larger than defined but not smaller
				wc.CalcHeight = Math.Max(height, defnHeight);
			}
			else if (_CanShrink && !_CanGrow)
			{
				// Only CanShrink: height can be smaller than defined but not larger
				wc.CalcHeight = Math.Min(height, defnHeight);
			}
			else
			{
				// Both CanGrow and CanShrink: height can be either larger or smaller
				wc.CalcHeight = height;
			}
			
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
