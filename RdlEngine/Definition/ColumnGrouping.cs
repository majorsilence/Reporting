

using System;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// ColumnGrouping definition and processing.
	///</summary>
	[Serializable]
	internal class ColumnGrouping : ReportLink
	{
		RSize _Height;		// Height of the column header
		DynamicColumns _DynamicColumns;	// Dynamic column headings for this grouping
		StaticColumns _StaticColumns;		// Static column headings for this grouping		
	
		internal ColumnGrouping(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_Height=null;
			_DynamicColumns=null;
			_StaticColumns=null;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "Height":
						_Height = new RSize(r, xNodeLoop);
						break;
					case "DynamicColumns":
						_DynamicColumns = new DynamicColumns(r, this, xNodeLoop);
						break;
					case "StaticColumns":
						_StaticColumns = new StaticColumns(r, this, xNodeLoop);
						break;
					default:
						break;
				}
			}
			if (_Height == null)
				OwnerReport.rl.LogError(8, "ColumnGrouping requires the Height element to be specified.");

			if ((_DynamicColumns != null && _StaticColumns != null) ||
				(_DynamicColumns == null && _StaticColumns == null))
				OwnerReport.rl.LogError(8, "ColumnGrouping requires either the DynamicColumns element or StaticColumns element but not both.");
		}
		
		async override internal Task FinalPass()
		{
			if (_DynamicColumns != null)
                await _DynamicColumns.FinalPass().ConfigureAwait(false);
			if (_StaticColumns != null)
                await _StaticColumns.FinalPass().ConfigureAwait(false);
			return ;
		}


		internal RSize Height
		{
			get { return  _Height; }
			set {  _Height = value; }
		}

		internal DynamicColumns DynamicColumns
		{
			get { return  _DynamicColumns; }
			set {  _DynamicColumns = value; }
		}

		internal StaticColumns StaticColumns
		{
			get { return  _StaticColumns; }
			set {  _StaticColumns = value; }
		}
	}
}
