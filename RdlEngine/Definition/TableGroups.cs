
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// TableGroups definition and processing.
	///</summary>
	[Serializable]
	internal class TableGroups : ReportLink
	{
        List<TableGroup> _Items;			// list of TableGroup entries

		internal TableGroups(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			TableGroup tg;
            _Items = new List<TableGroup>();
			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "TableGroup":
						tg = new TableGroup(r, this, xNodeLoop);
						break;
					default:	
						tg=null;		// don't know what this is
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown TableGroups element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
				if (tg != null)
					_Items.Add(tg);
			}
			if (_Items.Count == 0)
				OwnerReport.rl.LogError(8, "For TableGroups at least one TableGroup is required.");
			else
                _Items.TrimExcess();
		}
		
		async override internal Task FinalPass()
		{
			foreach(TableGroup tg in _Items)
			{
                await tg.FinalPass().ConfigureAwait(false);
			}

			return;
		}

		internal float DefnHeight()
		{
			float height=0;
			foreach(TableGroup tg in _Items)
			{
				height += tg.DefnHeight();
			}
			return height;
		}

        internal List<TableGroup> Items
		{
			get { return  _Items; }
		}
	}
}
