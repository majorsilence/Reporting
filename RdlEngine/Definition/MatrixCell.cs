

using System;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// A Cell within a Matrix.
	///</summary>
	[Serializable]
	internal class MatrixCell : ReportLink
	{
		ReportItems _ReportItems;	// The report items contained in each detail cell of the matrix layout.
						// This ReportItems collection must contain exactly one
						// ReportItem. The Top, Left, Height and Width for this
						// ReportItem are ignored. The position is taken to be 0,
						// 0 and the size to be 100%, 100%.		
	
		internal MatrixCell(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
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
						break;
				}
			}
			if (_ReportItems == null)
				OwnerReport.rl.LogError(8, "MatrixCell requires the ReportItems element.");
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
