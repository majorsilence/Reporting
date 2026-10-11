using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// ChartData definition and processing.
	///</summary>
	[Serializable]
	internal class ChartData : ReportLink
	{
        List<ChartSeries> _Items;			// list of chart series

		internal ChartData(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			ChartSeries cs;
            _Items = new List<ChartSeries>();
			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "chartseries":
						cs = new ChartSeries(r, this, xNodeLoop);
						break;
					default:	
						cs=null;		// don't know what this is
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown ChartData element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
				if (cs != null)
					_Items.Add(cs);
			}
			if (_Items.Count == 0)
				OwnerReport.rl.LogError(8, "For ChartData at least one ChartSeries is required.");
			else
                _Items.TrimExcess();
		}
		
		async override internal Task FinalPass()
		{
			foreach (ChartSeries cs in _Items)
			{
                await cs.FinalPass().ConfigureAwait(false);
			}
			return;
		}

        internal List<ChartSeries> Items
		{
			get { return  _Items; }
		}
	}
}
