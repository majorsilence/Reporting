
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Collection of matrix cells.
	///</summary>
	[Serializable]
	internal class MatrixCells : ReportLink
	{
        List<MatrixCell> _Items;			// list of MatrixCell

		internal MatrixCells(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			MatrixCell m;
            _Items = new List<MatrixCell>();
			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "MatrixCell":
						m = new MatrixCell(r, this, xNodeLoop);
						break;
					default:	
						m=null;		// don't know what this is
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown MatrixCells element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
				if (m != null)
					_Items.Add(m);
			}
			if (_Items.Count == 0)
				OwnerReport.rl.LogError(8, "For MatrixCells at least one MatrixCell is required.");
			else
                _Items.TrimExcess();
		}
		
		async override internal Task FinalPass()
		{
			foreach (MatrixCell m in _Items)
			{
                await m.FinalPass().ConfigureAwait(false);
			}
			return;
		}

        internal List<MatrixCell> Items
		{
			get { return  _Items; }
		}
	}
}
