

using System;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Query to execute for valid values of a parameter.
	///</summary>
	[Serializable]
	internal class ValidValues : ReportLink
	{
		DataSetReference _DataSetReference;	// The query to execute to obtain a list of
											// possible values for the parameter.
		ParameterValues _ParameterValues;	// Hardcoded values for the parameter	

		internal ValidValues(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_DataSetReference=null;
			_ParameterValues=null;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "DataSetReference":
						_DataSetReference = new DataSetReference(r, this, xNodeLoop);
						break;
					case "ParameterValues":
						_ParameterValues = new ParameterValues(r, this, xNodeLoop);
						break;
					default:
						OwnerReport.rl.LogError(4, "Unknown ValidValues element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
			if (_DataSetReference == null)
			{
				if (_ParameterValues == null)				
				{
					OwnerReport.rl.LogError(8, "For ValidValues element either DataSetReference or ParameterValue must be specified, but not both.");
				}
			}
			else if (_ParameterValues != null)
			{
				OwnerReport.rl.LogError(8, "For ValidValues element either DataSetReference or ParameterValue must be specified, but not both.");
			}
		}
		
		async override internal Task FinalPass()
		{
			if (_DataSetReference != null)
                await _DataSetReference.FinalPass().ConfigureAwait(false);
			if (_ParameterValues != null)
                await _ParameterValues.FinalPass().ConfigureAwait(false);
			return;
		}

		internal DataSetReference DataSetReference
		{
			get { return  _DataSetReference; }
			set {  _DataSetReference = value; }
		}

		internal ParameterValues ParameterValues
		{
			get { return  _ParameterValues; }
			set {  _ParameterValues = value; }
		}

		internal async Task<string[]> DisplayValues(Report rpt)
		{
			string[] dsplValues = rpt.Cache.Get(this, "displayvalues") as string[];
			object[] dataValues;

			if (dsplValues != null)
				return dsplValues;

			if (_DataSetReference != null)
				(dsplValues, dataValues) = await _DataSetReference.SupplyValues(rpt).ConfigureAwait(false);
			else
				(dsplValues, dataValues) = await _ParameterValues.SupplyValues(rpt).ConfigureAwait(false);

            if (dataValues == null)
                dataValues = new object[0];
            if (dsplValues == null)
                dsplValues = new string[0];

			// there shouldn't be a problem; but if there is it doesn't matter as values can be recreated
			try {rpt.Cache.Add(this, "datavalues", dataValues);} 
			catch (Exception e1)
			{
				rpt.rl.LogError(4, "Error caching data values.  " + e1.Message);
			}
			try {rpt.Cache.Add(this, "displayvalues", dsplValues);} 
			catch (Exception e2)
			{
				rpt.rl.LogError(4, "Error caching display values.  " + e2.Message);
			}

			return dsplValues;
		}

		internal async Task<object[]> DataValues(Report rpt)
		{
			string[] dsplValues;
			object[] dataValues = rpt.Cache.Get(this, "datavalues") as object[];

			if (dataValues != null)
				return dataValues;

			if (_DataSetReference != null)
				(dsplValues, dataValues)= await _DataSetReference.SupplyValues(rpt).ConfigureAwait(false);
			else
				(dsplValues, dataValues) = await _ParameterValues.SupplyValues(rpt).ConfigureAwait(false);

            if (dataValues == null)
                dataValues = new object[0];
            if (dsplValues == null)
                dsplValues = new string[0];

			// there shouldn't be a problem; but if there is it doesn't matter as values can be recreated
			try {rpt.Cache.Add(this, "datavalues", dataValues);} 
			catch (Exception e1)
			{
				rpt.rl.LogError(4, "Error caching data values.  " + e1.Message);
			}
			try {rpt.Cache.Add(this, "displayvalues", dsplValues);} 
			catch (Exception e2)
			{
				rpt.rl.LogError(4, "Error caching display values.  " + e2.Message);
			}
			return dataValues;		
		}
	}
}
