

using System;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// DataLabel definition and processing.
	///</summary>
	[Serializable]
	internal class DataLabel : ReportLink
	{
		Style _Style;	// Defines text, border and background style
						// properties for the labels
		Expression _Value;	//(Variant) Expression for the value labels. If omitted,
						// values of in the ValueAxis are used for labels.
		bool _Visible;	// Whether the data label is displayed on the
						// chart. Defaults to False.
		DataLabelPositionEnum _Position;	// Position of the label.  Default: auto
		int _Rotation;	// Angle of rotation of the label text		
	
		internal DataLabel(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_Style=null;
			_Value=null;
			_Visible=false;
			_Position=DataLabelPositionEnum.Auto;
			_Rotation=0;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "style":
						_Style = new Style(r, this, xNodeLoop);
						break;
					case "value":
						_Value = new Expression(r, this, xNodeLoop, ExpressionType.Variant);
						break;
					case "visible":
						_Visible = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					case "position":
						_Position = DataLabelPosition.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					case "rotation":
						_Rotation = XmlUtil.Integer(xNodeLoop.InnerText);
						break;
					default:
						break;
				}
			}
		

		}

		// Handle parsing of function in final pass
		async override internal Task FinalPass()
		{
			if (_Style != null)
                await _Style.FinalPass().ConfigureAwait(false);
			if (_Value != null)
                await _Value.FinalPass().ConfigureAwait(false);
			return;
		}


		internal Style Style
		{
			get { return  _Style; }
			set {  _Style = value; }
		}

		internal Expression Value
		{
			get { return  _Value; }
			set {  _Value = value; }
		}

		internal bool Visible
		{
			get { return  _Visible; }
			set {  _Visible = value; }
		}

		internal DataLabelPositionEnum Position
		{
			get { return  _Position; }
			set {  _Position = value; }
		}

		internal int Rotation
		{
			get { return  _Rotation; }
			set {  _Rotation = value; }
		}
	}
}
