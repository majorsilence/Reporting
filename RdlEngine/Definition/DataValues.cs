
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// In Charts, the collection of data values for a single data point.
	///</summary>
	[Serializable]
	internal class DataValues : ReportLink
	{
        List<DataValue> _Items;			// list of DataValue

		internal DataValues(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			DataValue dv;
            _Items = new List<DataValue>();
			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "datavalue":
						dv = new DataValue(r, this, xNodeLoop);
						break;
					default:	
						dv=null;		// don't know what this is
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown DataValues element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
				if (dv != null)
					_Items.Add(dv);
			}
			if (_Items.Count == 0)
				OwnerReport.rl.LogError(8, "For DataValues at least one DataValue is required.");
			else
                _Items.TrimExcess();
		}
		
		async override internal Task FinalPass()
		{
			foreach (DataValue dv in _Items)
			{
                await dv.FinalPass().ConfigureAwait(false);
			}
			return;
		}

        internal List<DataValue> Items
		{
			get { return  _Items; }
		}
	}
}
