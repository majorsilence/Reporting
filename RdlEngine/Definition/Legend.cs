

using System;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Chart legend definition (style, position, ...)
	///</summary>
	[Serializable]
	internal class Legend : ReportLink
	{
		bool _Visible;		// Specifies whether a legend is displayed.
							// Defaults to false.
		Style _Style;		// Defines text, border and background style
							// properties for the legend. All Textbox properties apply.
		LegendPositionEnum _Position;	// The position of the legend
									// Default: RightTop
		LegendLayoutEnum _Layout;	// The arrangement of labels within the legend
								// Default: Column
		bool _InsidePlotArea;	//Boolean If true, draw legend inside plot area, otherwise
								// draw outside plot area (default).
	
		internal Legend(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_Visible=false;
			_Style=null;
			_Position=LegendPositionEnum.RightTop;
			_Layout=LegendLayoutEnum.Column;
			_InsidePlotArea=false;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "visible":
						_Visible = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					case "style":
						_Style = new Style(r, this, xNodeLoop);
						break;
					case "position":
						_Position = LegendPosition.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					case "layout":
						_Layout = LegendLayout.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					case "insideplotarea":
						_InsidePlotArea = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					default:
						break;
				}
			}
		

		}
		
		async override internal Task FinalPass()
		{
			if (_Style != null)
                await _Style.FinalPass().ConfigureAwait(false);
			return;
		}

		internal bool Visible
		{
			get { return  _Visible; }
			set {  _Visible = value; }
		}

		internal Style Style
		{
			get { return  _Style; }
			set {  _Style = value; }
		}

		internal LegendPositionEnum Position
		{
			get { return  _Position; }
			set {  _Position = value; }
		}

		internal LegendLayoutEnum Layout
		{
			get { return  _Layout; }
			set {  _Layout = value; }
		}

		internal bool InsidePlotArea
		{
			get { return  _InsidePlotArea; }
			set {  _InsidePlotArea = value; }
		}
	}
}
