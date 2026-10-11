
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Collection of Chart series groupings.
	///</summary>
	[Serializable]
	internal class SeriesGroupings : ReportLink
	{
        List<SeriesGrouping> _Items;			// list of SeriesGrouping

		internal SeriesGroupings(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			SeriesGrouping sg;
            _Items = new List<SeriesGrouping>();
			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "seriesgrouping":
						sg = new SeriesGrouping(r, this, xNodeLoop);
						break;
					default:
						sg=null;		// don't know what this is
						break;
				}
				if (sg != null)
					_Items.Add(sg);
			}
			if (_Items.Count == 0)
				OwnerReport.rl.LogError(8, "For SeriesGroupings at least one SeriesGrouping is required.");
			else
                _Items.TrimExcess();
		}
		
		async override internal Task FinalPass()
		{
			foreach (SeriesGrouping sg in _Items)
			{
                await sg.FinalPass().ConfigureAwait(false);
			}
			return;
		}

        internal List<SeriesGrouping> Items
		{
			get { return  _Items; }
		}
	}
}
