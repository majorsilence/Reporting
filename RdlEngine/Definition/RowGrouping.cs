

using System;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Matrix row grouping definition.
	///</summary>
	[Serializable]
	internal class RowGrouping : ReportLink
	{
		RSize _Width;	// Width of the row header
		DynamicRows _DynamicRows;	// Dynamic row headings for this grouping
		StaticRows _StaticRows;	// Static row headings for this grouping		
	
		internal RowGrouping(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_Width=null;
			_DynamicRows=null;
			_StaticRows=null;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "Width":
						_Width = new RSize(r, xNodeLoop);
						break;
					case "DynamicRows":
						_DynamicRows = new DynamicRows(r, this, xNodeLoop);
						break;
					case "StaticRows":
						_StaticRows = new StaticRows(r, this, xNodeLoop);
						break;
					default:	
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown RowGrouping element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
			if (_Width == null)
				OwnerReport.rl.LogError(8, "RowGrouping requires the Width element.");
		}
		
		async override internal Task FinalPass()
		{
			if (_DynamicRows != null)
                await _DynamicRows.FinalPass().ConfigureAwait(false);
			if (_StaticRows != null)
                await _StaticRows.FinalPass().ConfigureAwait(false);
			return;
		}

		internal RSize Width
		{
			get { return  _Width; }
			set {  _Width = value; }
		}

		internal DynamicRows DynamicRows
		{
			get { return  _DynamicRows; }
			set {  _DynamicRows = value; }
		}

		internal StaticRows StaticRows
		{
			get { return  _StaticRows; }
			set {  _StaticRows = value; }
		}
	}
}
