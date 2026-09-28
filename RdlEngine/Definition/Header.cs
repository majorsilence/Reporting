

using System;
using System.Xml;
using System.IO;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Definition of the header rows for a table.
	///</summary>
	[Serializable]
	internal class Header : ReportLink, ICacheData
	{
		TableRows _TableRows;	// The header rows for the table or group
		bool _RepeatOnNewPage;	// Indicates this header should be displayed on
								// each page that the table (or group) is displayed		

		internal Header(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_TableRows=null;
			_RepeatOnNewPage=false;

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
					case "RepeatOnNewPage":
						_RepeatOnNewPage = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					default:
						break;
				}
			}
			if (_TableRows == null)
				OwnerReport.rl.LogError(8, "Header requires the TableRows element.");
		}
		
		async override internal Task FinalPass()
		{
            await _TableRows.FinalPass();

			OwnerReport.DataCache.Add(this);
			return;
		}

		internal async Task Run(IPresent ip, Row row)
		{
            await _TableRows.Run(ip, row);
			return;
		}

		internal async Task RunPage(Pages pgs, Row row)
		{
			WorkClass wc = this.GetValue(pgs.Report);

			if (wc.OutputRow == row && wc.OutputPage == pgs.CurrentPage)
				return;

			Page p = pgs.CurrentPage;

			// If the header's own content is taller than a whole page, moving to a new
			// (equally-sized) page can never make it fit: RunPageNew -> RunPageHeader
			// would re-enter this exact method with the same inputs and recurse without
			// bound (confirmed via instrumentation: identical height/BottomOfPage on every
			// call, current page already empty). Only retry on a fresh page when the
			// current page actually has content on it already -- that's the case a new
			// page can genuinely help with. Otherwise, render on this page as-is and let
			// the content overflow rather than loop forever.
			bool currentPageAlreadyHasContent = !pgs.CurrentPage.IsEmpty();
			float height = p.YOffset + await HeightOfRows(pgs, row);
            height += await OwnerTable.GetPageFooterHeight(pgs, row);
			if (height > pgs.BottomOfPage && currentPageAlreadyHasContent)
			{
				Table t = OwnerTable;
                await t.RunPageFooter(pgs, row, false);
				p = t.RunPageNew(pgs, p);
                await t.RunPageHeader(pgs, row, false, null);
				if (this.RepeatOnNewPage)
					return;		// should already be on the page
			}

            await _TableRows.RunPage(pgs, row);
			wc.OutputRow = row;
			wc.OutputPage = pgs.CurrentPage;
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

				return null; 
			}
		}

		internal TableRows TableRows
		{
			get { return  _TableRows; }
			set {  _TableRows = value; }
		}
 
		internal async Task<float> HeightOfRows(Pages pgs, Row r)
		{
			return await _TableRows.HeightOfRows(pgs, r);
		}

		internal bool RepeatOnNewPage
		{
			get { return  _RepeatOnNewPage; }
			set {  _RepeatOnNewPage = value; }
		}
		#region ICacheData Members

		public void ClearCache(Report rpt)
		{
			rpt.Cache.Remove(this, "wc");
		}

		#endregion

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

		private void SetValue(Report rpt, WorkClass w)
		{
			rpt.Cache.AddReplace(this, "wc", w);
		}

		class WorkClass
		{
			internal Row OutputRow;		// the previous outputed row
			internal Page OutputPage;	// the previous outputed row
			internal WorkClass()
			{
				OutputRow = null;
				OutputPage = null;
			}
		}
	}
}
