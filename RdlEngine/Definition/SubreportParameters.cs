
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// The collection of parameters for a subreport.
	///</summary>
	[Serializable]
	internal class SubReportParameters : ReportLink
	{
        List<SubreportParameter> _Items;			// list of SubreportParameter

		internal SubReportParameters(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			SubreportParameter rp;
            _Items = new List<SubreportParameter>();
			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "Parameter":
						rp = new SubreportParameter(r, this, xNodeLoop);
						break;
					default:	
						rp=null;		// don't know what this is
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown SubreportParameters element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
				if (rp != null)
					_Items.Add(rp);
			}
			if (_Items.Count > 0)
                _Items.TrimExcess();
		}
		
		async override internal Task FinalPass()
		{
			foreach (SubreportParameter rp in _Items)
			{
                await rp.FinalPass().ConfigureAwait(false);
			}
			return;
		}

        internal List<SubreportParameter> Items
		{
			get { return  _Items; }
		}
	}
}
