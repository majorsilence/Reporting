

using System;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Definition of a subtotal column or row.
	///</summary>
	[Serializable]
	internal class Subtotal : ReportLink
	{
		ReportItems _ReportItems;	// The header cell for a subtotal column or row.
					// This ReportItems collection must contain
					// exactly one Textbox. The Top, Left, Height
					// and Width for this ReportItem are ignored.
					// The position is taken to be 0, 0 and the size to
					// be 100%, 100%.
		Style _Style;	// Style properties that override the style
						// properties for all top-level report items
						// contained in the subtotal column/row
						// At Subtotal Column/Row intersections, Row
						// style takes priority
		SubtotalPositionEnum _Position;	// Before | After (default)
							// Indicates whether this subtotal column/row
							// should appear before (left/above) or after
							// (right/below) the detail columns/rows.
		string _DataElementName;	// The name to use for this subtotal.
									//  Default: �Total�
		DataElementOutputEnum _DataElementOutput;	// Indicates whether the subtotal should appear in a data rendering.
									// Default: NoOutput
	
		internal Subtotal(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_ReportItems=null;
			_Style=null;
			_Position=SubtotalPositionEnum.After;
			_DataElementName="Total";
			_DataElementOutput=DataElementOutputEnum.NoOutput;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "ReportItems":
						_ReportItems = new ReportItems(r, this, xNodeLoop);
						break;
					case "Style":
						_Style = new Style(r, this, xNodeLoop);
						break;
					case "Position":
						_Position = SubtotalPosition.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					case "DataElementName":
						_DataElementName = xNodeLoop.InnerText;
						break;
					case "DataElementOutput":
						_DataElementOutput = Majorsilence.Reporting.Rdl.DataElementOutput.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					default:
						break;
				}
			}
			if (_ReportItems == null)
				OwnerReport.rl.LogError(8, "Subtotal requires the ReportItems element.");
		}
		
		async override internal Task FinalPass()
		{
			if (_ReportItems != null)
                await _ReportItems.FinalPass().ConfigureAwait(false);
			if (_Style != null)
                await _Style.FinalPass().ConfigureAwait(false);
			return;
		}

		internal ReportItems ReportItems
		{
			get { return  _ReportItems; }
			set {  _ReportItems = value; }
		}

		internal Style Style
		{
			get { return  _Style; }
			set {  _Style = value; }
		}

		internal SubtotalPositionEnum Position
		{
			get { return  _Position; }
			set {  _Position = value; }
		}

		internal string DataElementName
		{
			get { return  _DataElementName; }
			set {  _DataElementName = value; }
		}

		internal DataElementOutputEnum DataElementOutput
		{
			get { return  _DataElementOutput; }
			set {  _DataElementOutput = value; }
		}
	}
}
