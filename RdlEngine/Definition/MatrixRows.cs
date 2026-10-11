
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Collection of matrix rows
	///</summary>
	[Serializable]
	internal class MatrixRows : ReportLink
	{
        List<MatrixRow> _Items;			// list of MatrixRow

		internal MatrixRows(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			MatrixRow m;
            _Items = new List<MatrixRow>();
			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "MatrixRow":
						m = new MatrixRow(r, this, xNodeLoop);
						break;
					default:	
						m=null;		// don't know what this is
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown MatrixRows element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
				if (m != null)
					_Items.Add(m);
			}
			if (_Items.Count == 0)
				OwnerReport.rl.LogError(8, "For MatrixRows at least one MatrixRow is required.");
			else
                _Items.TrimExcess();
		}
		
		async override internal Task FinalPass()
		{
			foreach (MatrixRow m in _Items)
			{
                await m.FinalPass().ConfigureAwait(false);
			}
			return;
		}

		internal float DefnHeight()
		{
			float height=0;
			foreach (MatrixRow m in _Items)
			{
				height += m.Height.Points;
			}
			return height;
		}

        internal List<MatrixRow> Items
		{
			get { return  _Items; }
		}

		/// <summary>
		/// CellCount requires a correctly configured Matrix structure.  This is true at runtime
		/// but not necessarily true during Matrix parse.
		/// </summary>
		internal int CellCount
		{
			get 
			{
				MatrixRow mr = _Items[0] as MatrixRow;
				return mr.MatrixCells.Items.Count;
			}
		}
	}
}
