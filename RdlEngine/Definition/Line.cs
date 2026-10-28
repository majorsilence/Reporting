

using System;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Represents the report item for a line.
	///</summary>
	[Serializable]
	internal class Line : ReportItem
	{
		// Line has no additional elements/attributes beyond ReportItem
		internal Line(ReportDefn r, ReportLink p, XmlNode xNode) : base(r,p,xNode)
		{
			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				// nothing beyond reportitem for now
				if (!ReportItemElement(xNodeLoop))	// try at ReportItem level
				{					
					// don't know this element - log it
					OwnerReport.rl.LogError(4, "Unknown Line element " + xNodeLoop.Name + " ignored.");
				}
			}
		}
		override internal Task Run(IPresent ip, Row row)
		{
			ip.Line(this, row);
			return Task.CompletedTask;
		}

		override internal async Task RunPage(Pages pgs, Row row)
		{
			Report r = pgs.Report;
            bool bHidden = await IsHidden(r, row).ConfigureAwait(false);

			SetPagePositionBegin(pgs);
			PageLine pl = new PageLine();
            await SetPagePositionAndStyle(r, pl, row).ConfigureAwait(false);
            // The items below are placed from the line's end, its top and height (see Gap): its
            // start, which a vertical line's is, put them its height too high.
            if (bHidden)
			    SetPagePositionEndHidden(pgs, pl.Y2);
			else
			{
			    pgs.CurrentPage.AddObject(pl);
			    SetPagePositionEnd(pgs, pl.Y2);
			}
		}

		internal float GetX2(Report rpt)
		{
			float x2=GetOffsetCalc(rpt)+LeftCalc(rpt);
			if (Width != null)
				x2 += Width.Points;
			return x2;
		}		

		internal int iX2
		{
			get 
			{
				int x2=0;
				if (Left != null)
					x2 = Left.Size;
				if (Width != null)
					x2 += Width.Size;
				return x2;
			}
		}

        internal int iY2
        {
            get
            {
                int y2 = 0;
                if (Top != null)
                    y2 = Top.Size;
                if (Height != null)
                    y2 += Height.Size;
                return y2;
            }
        }

        internal float Y2
        {
            get
            {
                float y2 = 0;
                if (Top != null)
                    y2 = Top.Points;
                if (Height != null)
                    y2 += Height.Points;
                return y2;
            }
        }
    }
}
