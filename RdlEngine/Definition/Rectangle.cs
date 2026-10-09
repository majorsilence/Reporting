

using System;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Represent the rectangle report item.
	///</summary>
	[Serializable]
	internal class Rectangle : ReportItem
	{
		ReportItems _ReportItems;	// Report items contained within the bounds of the rectangle.
		bool _PageBreakAtStart;		// Indicates the report should page break at the start of the rectangle.
		bool _PageBreakAtEnd;		// Indicates the report should page break at the end of the rectangle.		

		// constructor that doesn't process syntax
		internal Rectangle(ReportDefn r, ReportLink p, XmlNode xNode, bool bNoLoop):base(r,p,xNode)
		{
			_ReportItems=null;
			_PageBreakAtStart=false;
			_PageBreakAtEnd=false;
		}

		internal Rectangle(ReportDefn r, ReportLink p, XmlNode xNode):base(r,p,xNode)
		{
			_ReportItems=null;
			_PageBreakAtStart=false;
			_PageBreakAtEnd=false;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "reportitems":
						_ReportItems = new ReportItems(r, this, xNodeLoop);
						break;
					case "pagebreakatstart":
						_PageBreakAtStart = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					case "pagebreakatend":
						_PageBreakAtEnd = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					default:	
						if (ReportItemElement(xNodeLoop))	// try at ReportItem level
							break;
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown Rectangle element " + xNodeLoop.Name + " ignored.");
						break;
				}
			}
		}
 
		async override internal Task FinalPass()
		{
            await base.FinalPass();

			if (_ReportItems != null)
                await _ReportItems.FinalPass();

			return;
		}
 
		async override internal Task Run(IPresent ip, Row row)
		{
            await base.Run(ip, row);

			if (_ReportItems == null)
				return;

			if (await ip.RectangleStart(this, row))
			{
                await _ReportItems.Run(ip, row);
                await ip.RectangleEnd(this, row);
			}
		}

		async override internal Task RunPage(Pages pgs, Row row)
		{
			Report r = pgs.Report;
            bool bHidden = await IsHidden(r, row);

			SetPagePositionBegin(pgs);

            // Handle page breaking at start
            if (this.PageBreakAtStart && !IsTableOrMatrixCell(r) && !pgs.CurrentPage.IsEmpty() && !bHidden)
            {	// force page break at beginning of dataregion
                pgs.NextOrNew();
                pgs.CurrentPage.YOffset = OwnerReport.TopOfPage;
            }

			PageRectangle pr = new PageRectangle();
            await SetPagePositionAndStyle(r, pr, row);
			if (pr.SI.BackgroundImage != null)
				pr.SI.BackgroundImage.H = pr.H;		//   and in the background image

            if (!bHidden)
            {
                Page p = pgs.CurrentPage;
                p.AddObject(pr);

                if (_ReportItems != null)
                {
                    float saveY = p.YOffset;
       //             p.YOffset += (Top == null ? 0 : this.Top.Points);
                    p.YOffset = pr.Y;       // top of rectangle is base for contained report items
                    int firstContained = p.Count;
                    await _ReportItems.RunPage(pgs, row, GetOffsetCalc(pgs.Report) + LeftCalc(r));
                    p.YOffset = saveY;
                    if (pgs.CurrentPage == p)
                        GrowToContents(p, firstContained, pr);
                }

                // Handle page breaking at end
                if (this.PageBreakAtEnd && !IsTableOrMatrixCell(r) && !pgs.CurrentPage.IsEmpty())
                {	// force page break at beginning of dataregion
                    pgs.NextOrNew();
                    pgs.CurrentPage.YOffset = OwnerReport.TopOfPage;
                }
            }
//			SetPagePositionEnd(pgs, pgs.CurrentPage.YOffset);
            SetPagePositionEnd(pgs, pr.Y + pr.H);

			return;
        }

        // A rectangle grows with what it holds, as in SSRS: a subreport or a growing text box
        // can print past its designed bottom, and the border, the background and the items
        // placed below the rectangle follow it down. Only on the rectangle's own page.
        private static void GrowToContents(Page p, int firstContained, PageRectangle pr)
        {
            float bottom = pr.Y + pr.H;
            for (int i = firstContained; i < p.Count; i++)
            {
                PageItem pi = p[i];
                float itemBottom = pi is PageLine pl ? Math.Max(pl.Y, pl.Y2) : pi.Y + pi.H;
                bottom = Math.Max(bottom, itemBottom);
            }
            pr.H = bottom - pr.Y;
            if (pr.SI.BackgroundImage != null)
                pr.SI.BackgroundImage.H = pr.H;
        }

        internal override void RemoveWC(Report rpt)
        {
            base.RemoveWC(rpt);

            if (this._ReportItems == null)
                return;

            foreach (ReportItem ri in this._ReportItems.Items)
            {
                ri.RemoveWC(rpt);
            }
        }

		internal ReportItems ReportItems
		{
			get { return  _ReportItems; }
			set {  _ReportItems = value; }
		}

		internal bool PageBreakAtStart
		{
			get { return  _PageBreakAtStart; }
			set {  _PageBreakAtStart = value; }
		}

		internal bool PageBreakAtEnd
		{
			get { return  _PageBreakAtEnd; }
			set {  _PageBreakAtEnd = value; }
		}
	}
}
