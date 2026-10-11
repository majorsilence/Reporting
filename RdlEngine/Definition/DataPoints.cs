
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// DataPoints definition and processing.
	///</summary>
	[Serializable]
	internal class DataPoints : ReportLink
	{
        List<DataPoint> _Items;			// list of datapoint

		internal DataPoints(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			DataPoint dp;
            _Items = new List<DataPoint>();
			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "datapoint":
						dp = new DataPoint(r, this, xNodeLoop);
						break;
					default:	
						dp=null;		// don't know what this is
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown DataPoints element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
				if (dp != null)
					_Items.Add(dp);
			}
			if (_Items.Count == 0)
				OwnerReport.rl.LogError(8, "For DataPoints at least one DataPoint is required.");
			else
                _Items.TrimExcess();
		}
		
		async override internal Task FinalPass()
		{
			foreach (DataPoint dp in _Items)
			{
                await dp.FinalPass().ConfigureAwait(false);
			}
			return;
		}

        internal List<DataPoint> Items
		{
			get { return  _Items; }
		}
	}
}
