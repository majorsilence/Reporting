
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Collection of row groupings.
	///</summary>
	[Serializable]
	internal class RowGroupings : ReportLink
	{
        List<RowGrouping> _Items;			// list of RowGrouping
		int _StaticCount;

		internal RowGroupings(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			RowGrouping g;
            _Items = new List<RowGrouping>();
			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "RowGrouping":
						g = new RowGrouping(r, this, xNodeLoop);
						break;
					default:	
						g=null;		// don't know what this is
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown RowGroupings element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
				if (g != null)
					_Items.Add(g);
			}
			if (_Items.Count == 0)
				OwnerReport.rl.LogError(8, "For RowGroupings at least one RowGrouping is required.");
			else
			{
                _Items.TrimExcess();
				_StaticCount = GetStaticCount();
			}
		}
		
		async override internal Task FinalPass()
		{
			foreach (RowGrouping g in _Items)
			{
                await g.FinalPass().ConfigureAwait(false);
			}
			return;
		}

        internal List<RowGrouping> Items
		{
			get { return  _Items; }
		}

		internal MatrixEntry GetME(Report rpt)
		{
			WorkClass wc = GetWC(rpt);
			return wc.ME;
		}

		internal void SetME(Report rpt, MatrixEntry me)
		{
			WorkClass wc = GetWC(rpt);
			wc.ME = me;
		}

		private WorkClass GetWC(Report rpt)
		{
			if (rpt == null)
				return new WorkClass();

			WorkClass wc = rpt.Cache.Get(this, "wc") as WorkClass;
			if (wc == null)
			{
				wc = new WorkClass();
				rpt.Cache.Add(this, "wc", wc);
			}
			return wc;
		}

		private void RemoveWC(Report rpt)
		{
			rpt.Cache.Remove(this, "wc");
		}

		private int GetStaticCount()
		{
			// Find the static column
			foreach (RowGrouping rg in _Items)
			{
				if (rg.StaticRows == null)
					continue;
				return rg.StaticRows.Items.Count;
			}
			return 0;
		}

		internal int StaticCount
		{
			get {return _StaticCount;}
		}

		class WorkClass
		{
			internal MatrixEntry ME;	// Used at runtime to contain data values	
			internal WorkClass()
			{
				ME=null;
			}
		}
	}
}
