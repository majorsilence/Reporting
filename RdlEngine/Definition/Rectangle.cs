

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
		bool _KeepTogether;			// Indicates the rectangle should be kept on one page if it can be.
		bool _DrawBeneath;			// drawn beneath what follows it, taking no room; see DrawBeneathOf

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
					case "keeptogether":
						_KeepTogether = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					default:	
						if (ReportItemElement(xNodeLoop))	// try at ReportItem level
							break;
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown Rectangle element " + xNodeLoop.Name + " ignored.");
						break;
				}
			}

			_DrawBeneath = DrawBeneathOf(Custom);
		}

		// <Custom><DrawBeneath>true</DrawBeneath></Custom> asks that a rectangle be drawn beneath
		// what follows it, as a backdrop or a watermark is, and take no room: neither it nor what
		// it holds makes its container or its table row taller, and what it holds is drawn at its
		// ZIndex, a subreport's rows included. In a table row of its own, the rows after it print
		// over it, and its group header, detail record or footer still ends below its Height (see
		// TableRow.RunPageRow and Table.HoldBeneath).
		private static bool DrawBeneathOf(Custom custom)
		{
			if (custom == null)
				return false;
			foreach (XmlNode xNode in custom.CustomXmlNode.ChildNodes)
			{
				if (xNode.Name == "DrawBeneath")
					return string.Equals(xNode.InnerText.Trim(), "true", StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}

		async override internal Task FinalPass()
		{
            await base.FinalPass().ConfigureAwait(false);

			if (_ReportItems != null)
                await _ReportItems.FinalPass().ConfigureAwait(false);

			return;
		}
 
		async override internal Task Run(IPresent ip, Row row)
		{
            await base.Run(ip, row).ConfigureAwait(false);

			if (_ReportItems == null)
				return;

			if (await ip.RectangleStart(this, row).ConfigureAwait(false))
			{
                await _ReportItems.Run(ip, row).ConfigureAwait(false);
                await ip.RectangleEnd(this, row).ConfigureAwait(false);
			}
		}

		async override internal Task RunPage(Pages pgs, Row row)
		{
			Report r = pgs.Report;
            bool bHidden = await IsHidden(r, row).ConfigureAwait(false);

			SetPagePositionBegin(pgs);

            // Handle page breaking at start
            if (this.PageBreakAtStart && !IsTableOrMatrixCell(r) && !pgs.CurrentPage.IsEmpty() && !bHidden)
            {	// force page break at beginning of dataregion
                pgs.NextOrNew();
                pgs.CurrentPage.YOffset = OwnerReport.TopOfPage;
            }

			PageRectangle pr = new PageRectangle();
            await SetPagePositionAndStyle(r, pr, row).ConfigureAwait(false);
            float? bottom = null;       // set when the rectangle ends on a later page
			if (pr.SI.BackgroundImage != null)
				pr.SI.BackgroundImage.H = pr.H;		//   and in the background image

            if (!bHidden)
            {
                Page p = pgs.CurrentPage;
                p.AddObject(pr);
                if (_DrawBeneath)
                    p.DrawBeneath(pr);

                if (_ReportItems != null)
                {
                    float saveY = p.YOffset;
       //             p.YOffset += (Top == null ? 0 : this.Top.Points);
                    p.YOffset = pr.Y;       // top of rectangle is base for contained report items
                    int pageIndex = IndexOf(pgs, p);
                    var itemsBefore = new System.Collections.Generic.List<int>();
                    for (int i = pageIndex; i < pgs.Count; i++)
                        itemsBefore.Add(pgs[i].Count);
                    await _ReportItems.RunPage(pgs, row, GetOffsetCalc(pgs.Report) + LeftCalc(r)).ConfigureAwait(false);
                    p.YOffset = saveY;
                    if (pgs.CurrentPage == p)
                    {
                        // What a rectangle drawn beneath holds is drawn beneath with it.
                        if (_DrawBeneath)
                            p.DrawBeneath(itemsBefore[0], ZIndex);
                        GrowToContents(p, itemsBefore[0], pr);
                    }
                    else
                        bottom = SplitAcrossPages(pgs, pageIndex, itemsBefore, pr);
                }

                // Handle page breaking at end
                if (this.PageBreakAtEnd && !IsTableOrMatrixCell(r) && !pgs.CurrentPage.IsEmpty())
                {	// force page break at beginning of dataregion
                    pgs.NextOrNew();
                    pgs.CurrentPage.YOffset = OwnerReport.TopOfPage;
                }
            }
//			SetPagePositionEnd(pgs, pgs.CurrentPage.YOffset);
            if (bHidden)
                SetPagePositionEndHidden(pgs, pr.Y + pr.H);
            else
                SetPagePositionEnd(pgs, bottom ?? pr.Y + pr.H);

			return;
        }

        // A rectangle grows with what it holds, as in SSRS: a subreport or a growing text box
        // can print past its designed bottom, and the border, the background and the items
        // placed below the rectangle follow it down. Only on the rectangle's own page. What a
        // rectangle drawn beneath draws takes no room, and the rectangle does not grow to hold it.
        private static void GrowToContents(Page p, int firstContained, PageRectangle pr)
        {
            float bottom = pr.Y + pr.H;
            for (int i = firstContained; i < p.Count; i++)
            {
                PageItem pi = p[i];
                if (p.IsBeneath(pi))
                    continue;
                float itemBottom = pi is PageLine pl ? Math.Max(pl.Y, pl.Y2) : pi.Y + pi.H;
                bottom = Math.Max(bottom, itemBottom);
            }
            pr.H = bottom - pr.Y;
            if (pr.SI.BackgroundImage != null)
                pr.SI.BackgroundImage.H = pr.H;
        }

        // As SSRS splits a rectangle whose items run onto later pages: the first page keeps the top and
        // side borders down to its last item there, a middle page has the sides, and the last page the
        // sides and the bottom border below its last item. Returns the bottom of the last piece.
        private static float SplitAcrossPages(Pages pgs, int pageIndex, System.Collections.Generic.List<int> itemsBefore, PageRectangle pr)
        {
            GrowToContents(pgs[pageIndex], itemsBefore[0], pr);
            var bottomBorder = pr.SI.BStyleBottom;
            pr.SI = (StyleInfo)pr.SI.Clone();
            pr.SI.BStyleBottom = BorderStyleEnum.None;

            int lastIndex = IndexOf(pgs, pgs.CurrentPage);
            float end = pr.Y + pr.H;
            for (int i = pageIndex + 1; i <= lastIndex; i++)
            {
                Page q = pgs[i];
                int first = i - pageIndex < itemsBefore.Count ? itemsBefore[i - pageIndex] : 0;
                if (first >= q.Count)
                    continue;
                var (top, itemsBottom) = ItemsBounds(q, first);
                var piece = (PageRectangle)pr.Clone();
                piece.SI = (StyleInfo)pr.SI.Clone();
                piece.SI.BStyleTop = BorderStyleEnum.None;
                piece.SI.BStyleBottom = i == lastIndex ? bottomBorder : BorderStyleEnum.None;
                piece.SI.BackgroundImage = null;
                q.InsertObject(piece);      // behind the items it holds
                piece.ItemNumber = -1;      // and stays behind them when the page is sorted
                piece.X = pr.X;
                piece.Y = top;
                piece.W = pr.W;
                piece.H = itemsBottom - top;
                end = piece.Y + piece.H;
            }
            return end;
        }

        private static (float Top, float Bottom) ItemsBounds(Page p, int first)
        {
            float top = float.MaxValue, bottom = float.MinValue;
            for (int i = first; i < p.Count; i++)
            {
                PageItem pi = p[i];
                top = Math.Min(top, pi is PageLine tl ? Math.Min(tl.Y, tl.Y2) : pi.Y);
                bottom = Math.Max(bottom, pi is PageLine bl ? Math.Max(bl.Y, bl.Y2) : pi.Y + pi.H);
            }
            return (top, bottom);
        }

        private static int IndexOf(Pages pgs, Page p)
        {
            for (int i = 0; i < pgs.Count; i++)
            {
                if (pgs[i] == p)
                    return i;
            }
            return pgs.Count - 1;
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

		// Only a Rectangle that fills a table row acts on this: see TableRow.RunPage.
		internal bool KeepTogether
		{
			get { return  _KeepTogether; }
			set {  _KeepTogether = value; }
		}

		internal bool DrawBeneath
		{
			get { return  _DrawBeneath; }
		}
	}
}
