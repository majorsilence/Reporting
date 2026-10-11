

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Xml;


namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Collection of report parameters.
	///</summary>
	[Serializable]
	internal class ReportParameters : ReportLink, ICollection
	{
		IDictionary _Items;			// list of report items

		internal ReportParameters(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			if (xNode.ChildNodes.Count < 10)
				_Items = new ListDictionary( StringComparer.OrdinalIgnoreCase );	// Hashtable is overkill for small lists
			else
				_Items = new Hashtable(xNode.ChildNodes.Count, StringComparer.OrdinalIgnoreCase);

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				if (string.Equals(xNodeLoop.Name, "ReportParameter", StringComparison.InvariantCultureIgnoreCase))
				{
					ReportParameter rp = new ReportParameter(r, this, xNodeLoop);
                    if (rp.Name != null)
					    _Items.Add(rp.Name.Nm, rp);
				}
				else
					OwnerReport.rl.LogError(4, "Unknown ReportParameters element '" + xNodeLoop.Name + "' ignored.");
			}
		}
		
		internal async Task SetRuntimeValues(Report rpt, IDictionary parms)
		{
			// Fill the values to use in the report parameters
			foreach (string pname in parms.Keys)	// Loop thru the passed parameters
			{
				ReportParameter rp = (ReportParameter) _Items[pname];
				if (rp == null)
				{	// When not found treat it as a warning message
					if (!pname.StartsWith("rs:"))	// don't care about report server parameters
						rpt.rl.LogError(4, "Unknown ReportParameter passed '" + pname + "' ignored.");
					continue;
				}

                // Search for the valid values
                object parmValue = parms[pname];
                if (parmValue is string && rp.ValidValues != null)
                {
                    string[] dvs = await rp.ValidValues.DisplayValues(rpt).ConfigureAwait(false);
                    if (dvs != null && dvs.Length > 0)
                    {
                        for (int i = 0; i < dvs.Length; i++)
                        {
                            if (dvs[i] == (string) parmValue)
                            {
                                object[] dv = await rp.ValidValues.DataValues(rpt).ConfigureAwait(false);
                                parmValue = dv[i];
                                break;
                            }
                        }
                    }
                }
				rp.SetRuntimeValue(rpt, parmValue);
			}

			return;
		}

		async override internal Task FinalPass()
		{
			foreach (ReportParameter rp in _Items.Values)
			{
                await rp.FinalPass().ConfigureAwait(false);
			}
			return;
		}

		internal IDictionary Items
		{
			get { return  _Items; }
		}
		#region ICollection Members

		public bool IsSynchronized
		{
			get
			{
				return _Items.IsSynchronized;
			}
		}

		public int Count
		{
			get
			{
				return _Items.Count;
			}
		}

		public void CopyTo(Array array, int index)
		{
			_Items.CopyTo(array, index);
		}

		public object SyncRoot
		{
			get
			{
				return _Items.SyncRoot;
			}
		}

		#endregion

		#region IEnumerable Members

		public IEnumerator GetEnumerator()
		{
			return _Items.Values.GetEnumerator();
		}

		#endregion
	}
}
