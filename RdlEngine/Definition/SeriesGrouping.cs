

using System;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Chart Series grouping (both dynamic and static).
	///</summary>
	[Serializable]
	internal class SeriesGrouping : ReportLink
	{
		DynamicSeries _DynamicSeries;	// Dynamic Series headings for this grouping
		StaticSeries _StaticSeries;		// Static Series headings for this grouping	
		Style _Style;					// border and background properties for series legend itmes and data points
										//   when dynamic exprs are evaluated per group instance
	
		internal SeriesGrouping(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_DynamicSeries=null;
			_StaticSeries=null;
			_Style=null;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "dynamicseries":
						_DynamicSeries = new DynamicSeries(r, this, xNodeLoop);
						break;
					case "staticseries":
						_StaticSeries = new StaticSeries(r, this, xNodeLoop);
						break;
					case "style":
						_Style = new Style(OwnerReport, this, xNodeLoop);
						OwnerReport.rl.LogError(4, "Style element in SeriesGrouping is currently ignored."); // TODO
						break;
					default:	
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown SeriesGrouping element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
		}
		
		async override internal Task FinalPass()
		{
			if (_DynamicSeries != null)
                await _DynamicSeries.FinalPass().ConfigureAwait(false);
			if (_StaticSeries != null)
                await _StaticSeries.FinalPass().ConfigureAwait(false);
			if (_Style != null)
                await _Style.FinalPass().ConfigureAwait(false);

			return;
		}

		internal DynamicSeries DynamicSeries
		{
			get { return  _DynamicSeries; }
			set {  _DynamicSeries = value; }
		}

		internal StaticSeries StaticSeries
		{
			get { return  _StaticSeries; }
			set {  _StaticSeries = value; }
		}

		internal Style Style
		{
			get { return  _Style; }
			set {  _Style = value; }
		}

	}
}
