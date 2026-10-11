
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Collection of query parameters.
	///</summary>
	[Serializable]
	internal class QueryParameters : ReportLink
	{
        List<QueryParameter> _Items;			// list of QueryParameter
        bool _ContainsArray;                   // true if any of the parameters is an array reference

		internal QueryParameters(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
            _ContainsArray = false;
			QueryParameter q;
            _Items = new List<QueryParameter>();
			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "queryparameter":
						q = new QueryParameter(r, this, xNodeLoop);
						break;
					default:	
						q=null;		// don't know what this is
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown QueryParameters element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
				if (q != null)
					_Items.Add(q);
			}
			if (_Items.Count == 0)
				OwnerReport.rl.LogError(8, "For QueryParameters at least one QueryParameter is required.");
			else
                _Items.TrimExcess();
		}
		
		async override internal Task FinalPass()
		{
			foreach (QueryParameter q in _Items)
			{
                await q.FinalPass().ConfigureAwait(false);
                if (q.IsArray)
                    _ContainsArray = true;
			}
			return;
		}

        internal List<QueryParameter> Items
		{
			get { return  _Items; }
		}
        internal bool ContainsArray
        {
            get { return _ContainsArray; }
        }
	}
}
