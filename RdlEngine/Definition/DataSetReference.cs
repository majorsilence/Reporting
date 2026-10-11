

using System;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// The query to use to obtain a list of values for a parameter.   See ValidValues.
	///</summary>
	[Serializable]
	internal class DataSetReference : ReportLink
	{
		string _DataSetName;	//Name of the data set to use.
		DataSetDefn _ds;		// DataSet that matches the name
		string _ValueField;		//Name of the field to use for the values/defaults for the parameter
		string _LabelField;		//Name of the field to use for the value to display to the		
								// user for the selection.  If not supplied or the returned
								// value is null, the value in the ValueField is used.
								//  not used for DefaultValue.
		Field _vField;			// resolved value name
		Field _lField;			// resolved label name
		internal DataSetReference(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_DataSetName=null;
			_ValueField=null;
			_LabelField=null;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "DataSetName":
						_DataSetName = xNodeLoop.InnerText;
						break;
					case "ValueField":
						_ValueField = xNodeLoop.InnerText;
						break;
					case "LabelField":
						_LabelField = xNodeLoop.InnerText;
						break;
					default:
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown DataSetReference element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
			if (_DataSetName == null)
				OwnerReport.rl.LogError(8, "DataSetReference DataSetName is required but not specified.");
			if (_ValueField == null)
				OwnerReport.rl.LogError(8, "DataSetReference ValueField is required but not specified for" + _DataSetName==null? "<unknown name>": _DataSetName);
		}
		
		override internal Task FinalPass()
		{
			_ds = OwnerReport.DataSetsDefn[this._DataSetName];
			if (_ds == null)
				OwnerReport.rl.LogError(8, "DataSetReference refers to unknown data set '" + _DataSetName + "'");
			else
			{
				_vField = _ds.Fields[_ValueField];
				if (_vField == null)
					OwnerReport.rl.LogError(8, "ValueField refers to unknown field '" + _ValueField + "'");
				else
				{
					if (_LabelField == null)
						_lField = _vField;
					else
					{
						_lField = _ds.Fields[_LabelField];
						if (_lField == null)
							OwnerReport.rl.LogError(8, "LabelField refers to unknown field '" + _LabelField + "'");
					}
				}
			}

			return Task.CompletedTask;
		}

		internal string DataSetName
		{
			get { return  _DataSetName; }
			set {  _DataSetName = value; }
		}

		internal string ValueField
		{
			get { return  _ValueField; }
			set {  _ValueField = value; }
		}

		internal string LabelField
		{
			get { return  _LabelField; }
			set {  _LabelField = value; }
		}

		internal async Task<(string[] displayValues, object[] dataValues)> SupplyValues(Report rpt)
		{
			Rows rows = _ds.Query.GetMyData(rpt);
			if (rows == null)		// do we already have data?
			{
				// TODO:  this is wasteful;  likely to reretrieve the data again when report run with parameters
				//   should mark a dataset as only having one retrieval???
				bool lConnect = _ds.Query.DataSourceDefn.IsConnected(rpt);
				if (!lConnect)
					await _ds.Query.DataSourceDefn.ConnectDataSource(rpt).ConfigureAwait(false);  // connect; since not already connected
                await _ds.GetData(rpt).ConfigureAwait(false);									// get the data
				if (!lConnect)										// if we connected; then
					_ds.Query.DataSourceDefn.CleanUp(rpt);				//   we cleanup
				rows = _ds.Query.GetMyData(rpt);
				if (rows == null)			// any data now?
					return (null, null);					// no out of luck
			}

			var displayValues = new string[rows.Data.Count];
			var dataValues = new object[displayValues.Length];
			int index=0;
			object o;
			foreach (Row r in rows.Data)
			{
				dataValues[index] = r.Data[_vField.ColumnNumber];
				o = r.Data[_lField.ColumnNumber];
				if (o == null || o == DBNull.Value)
					displayValues[index] = "";
				else
					displayValues[index] = o.ToString();
				index++;
			}
			return (displayValues, dataValues);
        }
	}
}
