

using System;
using System.Xml;
using System.IO;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// For tabular reports, defines the detail rows with grouping and sorting.
	///</summary>
	[Serializable]
	internal class Details : ReportLink
	{
		TableRows _TableRows;	// The details rows for the table. The details rows
								// cannot contain any DataRegions in any of their TableCells.
		Grouping _Grouping;		// The expressions to group the detail data by
		Sorting _Sorting;		// The expressions to sort the detail data by
		Visibility _Visibility;	// Indicates if the details should be hidden	
		Textbox _ToggleTextbox;	//  resolved TextBox for toggling visibility
	
		internal Details(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_TableRows=null;
			_Grouping=null;
			_Sorting=null;
			_Visibility=null;
			_ToggleTextbox = null;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "TableRows":
						_TableRows = new TableRows(r, this, xNodeLoop);
						break;
					case "Grouping":
						_Grouping = new Grouping(r, this, xNodeLoop);
						break;
					case "Sorting":
						_Sorting = new Sorting(r, this, xNodeLoop);
						break;
					case "Visibility":
						_Visibility = new Visibility(r, this, xNodeLoop);
						break;
					default:
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown Details element " + xNodeLoop.Name + " ignored.");
						break;
				}
			}
			if (_TableRows == null)
				OwnerReport.rl.LogError(8, "Details requires the TableRows element.");
		}
		
		async override internal Task FinalPass()
		{
            await _TableRows.FinalPass().ConfigureAwait(false);
			if (_Grouping != null)
                await _Grouping.FinalPass().ConfigureAwait(false);
			if (_Sorting != null)
                await _Sorting.FinalPass().ConfigureAwait(false);
			if (_Visibility != null)
			{
                await _Visibility.FinalPass().ConfigureAwait(false);
				if (_Visibility.ToggleItem != null)
				{
					_ToggleTextbox = (Textbox) (OwnerReport.LUReportItems[_Visibility.ToggleItem]);
					if (_ToggleTextbox != null)
						_ToggleTextbox.IsToggle = true;
				}
			}
			return;
		}
		
		internal async Task Run(IPresent ip, Rows rs, int start, int end)
		{
			// if no rows output or rows just leave
			if (rs == null || rs.Data == null)
				return;
            if (this.Visibility != null && await Visibility.IsHidden(ip.Report(), rs.Data[start]).ConfigureAwait(false) && Visibility.ToggleItem == null)
                return;                 // not visible

			for (int r=start; r <= end; r++)
			{
                await _TableRows.Run(ip, rs.Data[r]).ConfigureAwait(false);
			}
			return;
		}
		
		internal async Task RunPage(Pages pgs, Rows rs, int start, int end, float footerHeight)
		{
			// if no rows output or rows just leave
			if (rs == null || rs.Data == null)
				return;

            if (this.Visibility != null && await Visibility.IsHidden(pgs.Report, rs.Data[start]).ConfigureAwait(false))
                return;                 // not visible

			Page p;

			Row row;
			for (int r=start; r <= end; r++)
			{
				p = pgs.CurrentPage;			// this can change after running a row
				row = rs.Data[r];
				float hrows = await HeightOfRows(pgs, row).ConfigureAwait(false);	// height of all the rows in the details
				float height = p.YOffset + hrows;

                // add the footerheight that must be on every page
                height += await OwnerTable.GetPageFooterHeight(pgs, row).ConfigureAwait(false);

				if (r == end) 
					height += footerHeight;		// on last row; may need additional room for footer
				if (height > pgs.BottomOfPage)
				{
                    await OwnerTable.RunPageFooter(pgs, row, false).ConfigureAwait(false);
					p = OwnerTable.RunPageNew(pgs, p);
                    await OwnerTable.RunPageHeader(pgs, row, false, null).ConfigureAwait(false);
                    await _TableRows.RunPage(pgs, row, true).ConfigureAwait(false);   // force checking since header + hrows might be > BottomOfPage
                }
                else
                    await _TableRows.RunPage(pgs, row, hrows > pgs.BottomOfPage).ConfigureAwait(false);
			}
			return;
		}
  
		internal TableRows TableRows
		{
			get { return  _TableRows; }
			set {  _TableRows = value; }
		}

		internal async Task<float> HeightOfRows(Pages pgs, Row r)
		{
            if (this.Visibility != null && await Visibility.IsHidden(pgs.Report, r).ConfigureAwait(false))
            {
                return 0;
            }

			return await _TableRows.HeightOfRows(pgs, r).ConfigureAwait(false);
		}

		internal Grouping Grouping
		{
			get { return  _Grouping; }
			set {  _Grouping = value; }
		}

		internal Sorting Sorting
		{
			get { return  _Sorting; }
			set {  _Sorting = value; }
		}

		internal Table OwnerTable
		{
			get { return (Table) (this.Parent); }
		}

		internal Visibility Visibility
		{
			get { return  _Visibility; }
			set {  _Visibility = value; }
		}

		internal Textbox ToggleTextbox
		{
			get { return  _ToggleTextbox; }
		}
	}
}
