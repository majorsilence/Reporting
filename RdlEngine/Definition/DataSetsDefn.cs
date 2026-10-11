

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Xml;


namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// The sets of data (defined by DataSet) that are retrieved as part of the Report.
	///</summary>
	[Serializable]
	internal class DataSetsDefn : ReportLink
	{
		IDictionary _Items;			// list of report items

		internal DataSetsDefn(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			if (xNode.ChildNodes.Count < 10)
				_Items = new ListDictionary();	// Hashtable is overkill for small lists
			else
				_Items = new Hashtable(xNode.ChildNodes.Count);

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				if (xNodeLoop.Name == "DataSet")
				{
					DataSetDefn ds = new DataSetDefn(r, this, xNodeLoop);
					if (ds != null && ds.Name != null)
						_Items.Add(ds.Name.Nm, ds);
				}
			}
		}

		internal DataSetDefn this[string name]
		{
			get 
			{
				return _Items[name] as DataSetDefn;
			}
		}
		
		async override internal Task FinalPass()
		{
			foreach (DataSetDefn ds in _Items.Values)
			{
				await ds.FinalPass().ConfigureAwait(false);
			}
			return;
		}

		internal async Task<bool> GetData(Report rpt)
		{
            bool haveRows = false;
			foreach (DataSetDefn ds in _Items.Values)
			{
				haveRows |= await ds.GetData(rpt).ConfigureAwait(false);
			}

			return haveRows;
		}

		internal IDictionary Items
		{
			get { return  _Items; }
		}
	}
}
