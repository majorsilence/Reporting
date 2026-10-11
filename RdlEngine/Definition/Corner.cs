

using System;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Matrix Corner definition and processing.
	///</summary>
	[Serializable]
	internal class Corner : ReportLink
	{
		ReportItems _ReportItems;	// The region that contains the elements of the corner layout
									// This ReportItems collection must contain exactly
									// one ReportItem. The Top, Left, Height and Width
									// for this ReportItem are ignored. The position is
									// taken to be 0, 0 and the size to be 100%, 100%.		
	
		internal Corner(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_ReportItems=null;

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
					default:	
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown Corner element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
		}
		
		async override internal Task FinalPass()
		{
			if (_ReportItems != null)
                await _ReportItems.FinalPass().ConfigureAwait(false);
			return;
		}

		internal ReportItems ReportItems
		{
			get { return  _ReportItems; }
			set {  _ReportItems = value; }
		}
	}
}
