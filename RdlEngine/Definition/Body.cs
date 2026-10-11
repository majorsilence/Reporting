

using System;
using System.Xml;
using System.IO;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Body definition and processing.  Contains the elements of the report body.
	///</summary>
	[Serializable]
	internal class Body : ReportLink
	{
		ReportItems _ReportItems;		// The region that contains the elements of the report body
		RSize _Height;		// Height of the body
		int _Columns;		// Number of columns for the report
							// Default: 1. Min: 1. Max: 1000
		RSize _ColumnSpacing; // Size Spacing between each column in multi-column
							// output 	Default: 0.5 in
		Style _Style;		// Default style information for the body	
		
		internal Body(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_ReportItems = null;
			_Height = null;
			_Columns = 1;
			_ColumnSpacing=null;
			_Style=null;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "reportitems":
						_ReportItems = new ReportItems(r, this, xNodeLoop);	// need a class for this
						break;
					case "height":
						_Height = new RSize(r, xNodeLoop);
						break;
					case "columns":
						_Columns = XmlUtil.Integer(xNodeLoop.InnerText);
						break;
					case "columnspacing":
						_ColumnSpacing = new RSize(r, xNodeLoop);
						break;
					case "style":
						_Style = new Style(r, this, xNodeLoop);
						break;
					default:	
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown Body element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
			if (_Height == null)
				OwnerReport.rl.LogError(8, "Body Height not specified.");
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
			ip.BodyStart(this);

			if (_ReportItems != null)
                await _ReportItems.Run(ip, null).ConfigureAwait(false);	// not sure about the row here?

			ip.BodyEnd(this);
			return ;
		}

		internal async Task RunPage(Pages pgs)
		{
			if (OwnerReport.Subreport == null)
			{	// Only set bottom of pages when on top level report
				pgs.BottomOfPage = OwnerReport.BottomOfPage;
				pgs.CurrentPage.YOffset = OwnerReport.TopOfPage;
			}
			this.SetCurrentColumn(pgs.Report, 0);

			if (_ReportItems != null)
                await _ReportItems.RunPage(pgs, null, OwnerReport.LeftMargin.Points).ConfigureAwait(false);

			return ;
		}

		internal ReportItems ReportItems
		{
			get { return  _ReportItems; }
		}

		internal RSize Height
		{
			get { return  _Height; }
			set {  _Height = value; }
		}

		internal int Columns
		{
			get { return  _Columns; }
			set {  _Columns = value; }
		}

		internal int GetCurrentColumn(Report rpt)
		{
			OInt cc = rpt.Cache.Get(this, "currentcolumn") as OInt;
			return cc == null? 0: cc.i;
		}

		internal int IncrCurrentColumn(Report rpt)
		{
			OInt cc = rpt.Cache.Get(this, "currentcolumn") as OInt;
			if (cc == null)
			{
				SetCurrentColumn(rpt, 0);
				cc = rpt.Cache.Get(this, "currentcolumn") as OInt;
			}
			cc.i++;
			return cc.i;
		}

		internal void SetCurrentColumn(Report rpt, int col)
		{
			OInt cc = rpt.Cache.Get(this, "currentcolumn") as OInt;
			if (cc == null)
				rpt.Cache.AddReplace(this, "currentcolumn", new OInt(col));
			else
				cc.i = col;
		}

		internal RSize ColumnSpacing
		{
			get 
			{
				if (_ColumnSpacing == null)
					_ColumnSpacing = new RSize(this.OwnerReport, ".5 in");

				return  _ColumnSpacing; 
			}
			set {  _ColumnSpacing = value; }
		}

		internal Style Style
		{
			get { return  _Style; }
			set {  _Style = value; }
		}

        internal void RemoveWC(Report rpt)
        {
            if (_ReportItems == null)
                return;

            foreach (ReportItem ri in this._ReportItems.Items)
            {
                ri.RemoveWC(rpt);
            }
        }
    }
}
