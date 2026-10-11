
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// CategoryGroupings definition and processing.
	///</summary>
	[Serializable]
	internal class CategoryGroupings : ReportLink
	{
        List<CategoryGrouping> _Items;			// list of category groupings

		internal CategoryGroupings(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			CategoryGrouping cg;
            _Items = new List<CategoryGrouping>();
			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "categorygrouping":
						cg = new CategoryGrouping(r, this, xNodeLoop);
						break;
					default:
						cg=null;		// don't know what this is
						break;
				}
				if (cg != null)
					_Items.Add(cg);
			}
			if (_Items.Count == 0)
				OwnerReport.rl.LogError(8, "For CategoryGroupings at least one CategoryGrouping is required.");
			else
                _Items.TrimExcess();
		}
		
		async override internal Task FinalPass()
		{
			foreach (CategoryGrouping cg in _Items)
			{
                await cg.FinalPass().ConfigureAwait(false);
			}
			return;
		}

        internal List<CategoryGrouping> Items
		{
			get { return  _Items; }
		}
	}
}
