

using System;
using System.Xml;
using System.IO;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Defines the page footer of the report
	///</summary>
	[Serializable]
	internal class PageFooter : ReportLink
	{
		RSize _Height;		// Height of the page footer
		bool _PrintOnFirstPage;	// Indicates if the page footer should be shown on
								// the first page of the report
		bool _PrintOnLastPage;	// Indicates if the page footer should be shown on
								// the last page of the report. Not used in singlepage reports.
		ReportItems _ReportItems;	// The region that contains the elements of the footer layout
							// No data regions or subreports are allowed in the page footer
		Style _Style;		// Style information for the page footer		
	
		internal PageFooter(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_Height=null;
			_PrintOnFirstPage=false;
			_PrintOnLastPage=false;
			_ReportItems=null;
			_Style=null;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "Height":
						_Height = new RSize(r, xNodeLoop);
						break;
					case "PrintOnFirstPage":
						_PrintOnFirstPage = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					case "PrintOnLastPage":
						_PrintOnLastPage = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					case "ReportItems":
						_ReportItems = new ReportItems(r, this, xNodeLoop);
						break;
					case "Style":
						_Style = new Style(r, this, xNodeLoop);
						break;
					default:
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown PageFooter element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
			if (_Height == null)
				OwnerReport.rl.LogError(8, "PageFooter Height is required.");
		}
		
		async override internal Task FinalPass()
		{
			if (_ReportItems != null)
                await _ReportItems.FinalPass().ConfigureAwait(false);
			if (_Style != null)
                await _Style.FinalPass().ConfigureAwait(false);
			return;
		}

		internal async Task Run(IPresent ip, Row row)
		{
			if (OwnerReport.Subreport != null)
				return;		// don't process page footers for sub-reports
			Report rpt = ip.Report();
			rpt.TotalPages = rpt.PageNumber = 1;
			ip.PageFooterStart(this);
			if (_ReportItems != null)
                await _ReportItems.Run(ip, row).ConfigureAwait(false);
			ip.PageFooterEnd(this);
		}
 
		internal async Task RunPage(Pages pgs)
		{
			if (OwnerReport.Subreport != null)
				return;		// don't process page footers for sub-reports
			if (_ReportItems == null)
				return;
			Report rpt = pgs.Report;

			rpt.TotalPages = pgs.PageCount;
            for (int i = 0; i < rpt.TotalPages; i++)
			{

                rpt.CurrentPage = pgs[i];		// needs to know for page header/footer expr processing
				pgs[i].YOffset = OwnerReport.PageHeight.Points 
									- OwnerReport.BottomMargin.Points
									- this._Height.Points;
                pgs[i].XOffset = 0;
                pgs.CurrentPage = pgs[i];
                rpt.PageNumber = pgs[i].PageNumber;
                if (pgs[i].PageNumber == 1 && pgs.Count > 1 && !_PrintOnFirstPage)
					continue;		// Don't put footer on the first page
                if (pgs[i].PageNumber == pgs.Count && !_PrintOnLastPage)
					continue;       // Don't put footer on the last page
                await _ReportItems.RunPage(pgs, null, OwnerReport.LeftMargin.Points).ConfigureAwait(false);
			}
		}

		internal RSize Height
		{
			get { return  _Height; }
			set {  _Height = value; }
		}

		internal bool PrintOnFirstPage
		{
			get { return  _PrintOnFirstPage; }
			set {  _PrintOnFirstPage = value; }
		}

		internal bool PrintOnLastPage
		{
			get { return  _PrintOnLastPage; }
			set {  _PrintOnLastPage = value; }
		}

		internal ReportItems ReportItems
		{
			get { return  _ReportItems; }
			set {  _ReportItems = value; }
		}

		internal Style Style
		{
			get { return  _Style; }
			set {  _Style = value; }
		}
	}
}
