

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;


namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Contains information about which classes to instantiate during report initialization.
	/// These instances can then be used in expressions throughout the report.
	///</summary>
	[Serializable]
	internal class Classes : ReportLink, IEnumerable
	{
        List<ReportClass> _Items;			// list of report class

		internal Classes(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
            _Items = new List<ReportClass>();
			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				if (xNodeLoop.Name == "Class")
				{
					ReportClass rc = new ReportClass(r, this, xNodeLoop);
					_Items.Add(rc);
				}
			}
			if (_Items.Count == 0)
				OwnerReport.rl.LogError(8, "For Classes at least one Class is required.");
			else
                _Items.TrimExcess();
		}
		
		internal ReportClass this[string s]
		{
			get 
			{
				foreach (ReportClass rc in _Items)
				{
					if (rc.InstanceName.Nm == s)
						return rc;
				}
				return null;
			}
		}

		async override internal Task FinalPass()
		{
			foreach (ReportClass rc in _Items)
			{
                await rc.FinalPass().ConfigureAwait(false);
			}
			return;
		}

		internal void Load(Report rpt)
		{
			foreach (ReportClass rc in _Items)
			{
				rc.Load(rpt);
			}
			return;
		}

        internal List<ReportClass> Items
		{
			get { return  _Items; }
		}
		#region IEnumerable Members

		public IEnumerator GetEnumerator()
		{
			return _Items.GetEnumerator();
		}

		#endregion
	}
}
