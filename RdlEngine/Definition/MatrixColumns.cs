
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Collection of matrix columns
	///</summary>
	[Serializable]
	internal class MatrixColumns : ReportLink, IEnumerable<MatrixColumn>
	{
        List<MatrixColumn> _Items;			// list of MatrixColumn

		internal MatrixColumns(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			MatrixColumn m;
            _Items = new List<MatrixColumn>();
			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "MatrixColumn":
						m = new MatrixColumn(r, this, xNodeLoop);
						break;
					default:	
						m=null;		// don't know what this is
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown MatrixColumns element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
				if (m != null)
					_Items.Add(m);
			}
			if (_Items.Count == 0)
				OwnerReport.rl.LogError(8, "For MatrixColumns at least one MatrixColumn is required.");
			else
                _Items.TrimExcess();
		}
		
		async override internal Task FinalPass()
		{
			foreach (MatrixColumn m in _Items)
			{
                await m.FinalPass().ConfigureAwait(false);
			}
			return;
		}

        internal List<MatrixColumn> Items
		{
			get { return  _Items; }
		}


        #region IEnumerable<MatrixColumn> Members

        public IEnumerator<MatrixColumn> GetEnumerator()
        {
            return _Items.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _Items.GetEnumerator();
        }

        #endregion
    }
}
