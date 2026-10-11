

using System;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// A single sort expression and direction.
	///</summary>
	[Serializable]
	internal class SortBy : ReportLink
	{
			Expression _SortExpression;	// (Variant) The expression to sort the groups by.
						// The functions RunningValue and RowNumber
						// are not allowed in SortExpression.
						// References to report items are not allowed.
			SortDirectionEnum _Direction;	// Indicates the direction of the sort
										// Ascending (Default) | Descending
	
		internal SortBy(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_SortExpression=null;
			_Direction=SortDirectionEnum.Ascending;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "sortexpression":
						_SortExpression = new Expression(r, this, xNodeLoop, ExpressionType.Variant);
						break;
					case "direction":
						_Direction = SortDirection.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					default:
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown SortBy element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
			if (_SortExpression == null)
				OwnerReport.rl.LogError(8, "SortBy requires the SortExpression element.");
		}

		// Handle parsing of function in final pass
		async override internal Task FinalPass()
		{
			if (_SortExpression != null)
                await _SortExpression.FinalPass().ConfigureAwait(false);
			return;
		}

		internal Expression SortExpression
		{
			get { return  _SortExpression; }
			set {  _SortExpression = value; }
		}

		internal SortDirectionEnum Direction
		{
			get { return  _Direction; }
			set {  _Direction = value; }
		}
	}
}
