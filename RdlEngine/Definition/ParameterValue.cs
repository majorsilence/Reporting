

using System;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// The value of a report paramenter.
	///</summary>
	[Serializable]
	internal class ParameterValue : ReportLink
	{
		Expression _Value;		// Possible value (variant) for the parameter
								// For Boolean: use "true" and "false"
								// For DateTime: use ISO 8601
								// For Float: use "." as the optional decimal separator.
		Expression _Label;		// Label (string) for the value to display in the UI
								// If not supplied, the _Value is used as the label. If
								// _Value not supplied, _Label is the empty string;
	
		internal ParameterValue(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_Value=null;
			_Label=null;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "Value":
						_Value = new Expression(r, this, xNodeLoop, ExpressionType.Variant);
						break;
					case "Label":
						_Label = new Expression(r, this, xNodeLoop, ExpressionType.String);
						break;
					default:
						break;
				}
			}
		

		}

		// Handle parsing of function in final pass
		async override internal Task FinalPass()
		{
			if (_Value != null)
                await _Value.FinalPass().ConfigureAwait(false);
			if (_Label != null)
                await _Label.FinalPass().ConfigureAwait(false);
			return;
		}

		internal Expression Value
		{
			get { return  _Value; }
			set {  _Value = value; }
		}


		internal Expression Label
		{
			get { return  _Label; }
			set {  _Label = value; }
		}
	}
}
