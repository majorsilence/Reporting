using System;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// ChartGridLines definition and processing.
	///</summary>
	[Serializable]
	internal class ChartGridLines : ReportLink
	{
		bool _ShowGridLines;	// Indicates the gridlines should be shown
		Style _Style;			// Line style properties for the gridlines and tickmarks
		
		internal ChartGridLines(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_ShowGridLines=true;
			_Style=null;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "showgridlines":
						_ShowGridLines = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					case "style":
						_Style = new Style(r, this, xNodeLoop);
						break;
					default:	// TODO
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown ChartGridLines element '" + xNodeLoop.Name + "' ignored.");
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

		internal bool ShowGridLines
		{
			get { return  _ShowGridLines; }
			set {  _ShowGridLines = value; }
		}

		internal Style Style
		{
			get { return  _Style; }
			set {  _Style = value; }
		}
	}
}
