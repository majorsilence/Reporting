

using System;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Definition of an expression within a group.
	///</summary>
	[Serializable]
	internal class GroupExpression : ReportLink
	{
		Expression _Expression;			// 

		internal GroupExpression(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_Expression = new Expression(r,this,xNode,ExpressionType.Variant);
		}

		// Handle parsing of function in final pass
		async override internal Task FinalPass()
		{
			if (_Expression != null)
                await _Expression.FinalPass().ConfigureAwait(false);
			return;
		}

		internal Expression Expression
		{
			get { return  _Expression; }
			set {  _Expression = value; }
		}
	}
}
