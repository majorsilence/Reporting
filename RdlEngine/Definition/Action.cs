

using System;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Action definition and processing.
	///</summary>
	[Serializable]
	internal class Action : ReportLink
	{
		Expression _Hyperlink;	// (URL)An expression that evaluates to the URL of the hyperlink
		Drillthrough _Drillthrough;	// The drillthrough report that should be executed
									// by clicking on the hyperlink
		Expression _BookmarkLink;	// (string)
								//An expression that evaluates to the ID of a
								//bookmark within the report to go to when this
								//report item is clicked on.
								//(If no bookmark with this ID is found, the link
								//will not be included in the report. If the
								//bookmark is hidden, the link will go to the start
								//of the page the bookmark is on. If multiple
								//bookmarks with this ID are found, the link will
								//go to the first one)		
		// Constructor
		internal Action(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_Hyperlink = null;
			_Drillthrough = null;	
			_BookmarkLink = null;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "hyperlink":
						_Hyperlink = new Expression(r, this, xNodeLoop, ExpressionType.URL);
						break;
					case "drillthrough":
						_Drillthrough = new Drillthrough(r, this, xNodeLoop);
						break;
					case "bookmarklink":
						_BookmarkLink = new Expression(r, this, xNodeLoop, ExpressionType.String);
						break;
					default:
						break;
				}
			}
		}

		// Handle parsing of function in final pass
		async override internal Task FinalPass()
		{
			if (_Hyperlink != null)
                await _Hyperlink.FinalPass().ConfigureAwait(false);
			if (_Drillthrough != null)
                await _Drillthrough.FinalPass().ConfigureAwait(false);
			if (_BookmarkLink != null)
                await _BookmarkLink.FinalPass().ConfigureAwait(false);
			return;
		}

		internal Expression Hyperlink
		{
			get { return _Hyperlink; }
			set { _Hyperlink = value; }
		}

		internal async Task<String> HyperLinkValue(Report rpt, Row r)
		{
			if (_Hyperlink == null)
				return null;

			return await _Hyperlink.EvaluateString(rpt, r).ConfigureAwait(false);
		}

		internal Drillthrough Drill
		{
			get { return _Drillthrough; }
			set { _Drillthrough = value; }
		}

		internal Expression BookmarkLink
		{
			get { return _BookmarkLink; }
			set { _BookmarkLink = value; }
		}

		internal async Task<String> BookmarkLinkValue(Report rpt, Row r)
		{
			if (_BookmarkLink == null)
				return null;

			return await _BookmarkLink.EvaluateString(rpt, r).ConfigureAwait(false);
		}
	}
}
