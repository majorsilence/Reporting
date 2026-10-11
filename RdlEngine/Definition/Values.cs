
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Ordered list of values used as a default for a parameter
	///</summary>
	[Serializable]
    internal class Values : ReportLink, System.Collections.Generic.ICollection<Expression>
	{
        List<Expression> _Items;			// list of expression items

		internal Values(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			Expression v;
            _Items = new List<Expression>();
			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "Value":
						v = new Expression(r, this, xNodeLoop, ExpressionType.Variant);
						break;
					default:	
						v=null;	
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown Value element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
				if (v != null)
					_Items.Add(v);
			}
			if (_Items.Count > 0)
                _Items.TrimExcess();
		}

		// Handle parsing of function in final pass
		async override internal Task FinalPass()
		{
			foreach (Expression e in _Items)
			{
                await e.FinalPass().ConfigureAwait(false);
			}
			return;
		}

        internal List<Expression> Items
		{
			get { return  _Items; }
		}
		#region IEnumerable Members

		public IEnumerator GetEnumerator()
		{
			return _Items.GetEnumerator();
		}

		#endregion

        #region ICollection<Expression> Members

        public void Add(Expression item)
        {
            _Items.Add(item);
        }

        public void Clear()
        {
            _Items.Clear();
        }

        public bool Contains(Expression item)
        {
            return _Items.Contains(item);
        }

        public void CopyTo(Expression[] array, int arrayIndex)
        {
            _Items.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _Items.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(Expression item)
        {
            return _Items.Remove(item);
        }

        #endregion

        #region IEnumerable<Expression> Members

        IEnumerator<Expression> IEnumerable<Expression>.GetEnumerator()
        {
            return _Items.GetEnumerator();
        }

        #endregion
    }
}
