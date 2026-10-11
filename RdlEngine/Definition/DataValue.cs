

using System;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// In charts, the DataValue defines a single value for the DataPoint.
	///</summary>
	[Serializable]
	internal class DataValue : ReportLink
	{
		Expression _Value;	// (Variant) Value expression. Same restrictions as
							//  the expressions in a matrix cell		
		internal DataValue(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_Value=null;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "value":
						_Value = new Expression(r, this, xNodeLoop, ExpressionType.Variant);
						break;
					default:
						break;
				}
			}
			if (_Value == null)
				OwnerReport.rl.LogError(8, "DataValue requires the Value element.");
		}

		// Handle parsing of function in final pass
		async override internal Task FinalPass()
		{
			if (_Value != null)
                await _Value.FinalPass().ConfigureAwait(false);
			return;
		}


		internal Expression Value
		{
			get { return  _Value; }
			set {  _Value = value; }
		}
	}
}
