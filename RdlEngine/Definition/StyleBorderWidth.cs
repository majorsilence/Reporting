

using System;
using System.Xml;
using System.Text;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// The width of the border.  Expressions for all sides as well as default expression.
	///</summary>
	[Serializable]
	internal class StyleBorderWidth : ReportLink
	{
		Expression _Default;	//(Size) Width of the border (unless overridden for a specific side)
								// Borders are centered on the edge of the object
								// Default: 1 pt Max: 20 pt Min: 0.25 pt
		Expression _Left;	//(Size) Width of the left border. Max: 20 pt Min: 0.25 pt
		Expression _Right;	//(Size) Width of the right border. Max: 20 pt Min: 0.25 pt
		Expression _Top;	//(Size) Width of the top border. Max: 20 pt Min: 0.25 pt
		Expression _Bottom;	//(Size) Width of the bottom border. Max: 20 pt Min: 0.25 pt
	
		internal StyleBorderWidth(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_Default=null;
			_Left=null;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "Default":
						_Default = new Expression(r, this, xNodeLoop, ExpressionType.ReportUnit);
						break;
					case "Left":
						_Left = new Expression(r, this, xNodeLoop, ExpressionType.ReportUnit);
						break;
					case "Right":
						_Right = new Expression(r, this, xNodeLoop, ExpressionType.ReportUnit);
						break;
					case "Top":
						_Top = new Expression(r, this, xNodeLoop, ExpressionType.ReportUnit);
						break;
					case "Bottom":
						_Bottom = new Expression(r, this, xNodeLoop, ExpressionType.ReportUnit);
						break;
					default:	
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown BorderWidth element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
		}

		// Handle parsing of function in final pass
		async override internal Task FinalPass()
		{
			if (_Default != null)
                await _Default.FinalPass().ConfigureAwait(false);
			if (_Left != null)
                await _Left.FinalPass().ConfigureAwait(false);
			if (_Right != null)
                await _Right.FinalPass().ConfigureAwait(false);
			if (_Top != null)
                await _Top.FinalPass().ConfigureAwait(false);
			if (_Bottom != null)
                await _Bottom.FinalPass().ConfigureAwait(false);
			return;
		}

		// Generate a CSS string from the specified styles
		internal async Task<string> GetCSS(Report rpt, Row row, bool bDefaults)
		{
			StringBuilder sb = new StringBuilder();

			if (_Default != null)
				sb.AppendFormat("border-width:{0};", await _Default.EvaluateString(rpt, row).ConfigureAwait(false));
			else if (bDefaults)
				sb.Append("border-width:1pt;");

			if (_Left != null)
				sb.AppendFormat("border-left-width:{0};", await _Left.EvaluateString(rpt, row).ConfigureAwait(false));

			if (_Right != null)
				sb.AppendFormat("border-right-width:{0};", await _Right.EvaluateString(rpt, row).ConfigureAwait(false));

			if (_Top != null)
				sb.AppendFormat("border-top-width:{0};", await _Top.EvaluateString(rpt, row).ConfigureAwait(false));

			if (_Bottom != null)
				sb.AppendFormat("border-bottom-width:{0};", await _Bottom.EvaluateString(rpt, row).ConfigureAwait(false));

			return sb.ToString();
		}

		internal async Task<bool> IsConstant()
		{
			bool rc = true;

			if (_Default != null)
				rc = await _Default.IsConstant().ConfigureAwait(false);

			if (!rc)
				return false;

			if (_Left != null)
				rc = await _Left.IsConstant().ConfigureAwait(false);

			if (!rc)
				return false;

			if (_Right != null)
				rc = await _Right.IsConstant().ConfigureAwait(false);

			if (!rc)
				return false;

			if (_Top != null)
				rc = await _Top.IsConstant().ConfigureAwait(false);

			if (!rc)
				return false;

			if (_Bottom != null)
				rc = await _Bottom.IsConstant().ConfigureAwait(false);

			return rc;
		}

		static internal string GetCSSDefaults()
		{
			return "border-width:1pt;";
		}

		internal Expression Default
		{
			get { return  _Default; }
			set {  _Default = value; }
		}

		internal async Task<float> EvalDefault(Report rpt, Row r)	// return points
		{
			if (_Default == null)
				return 1;

			string sw;
			sw = await _Default.EvaluateString(rpt, r).ConfigureAwait(false);

			RSize rs = new RSize(this.OwnerReport, sw);
			return rs.Points;
		}

		internal Expression Left
		{
			get { return  _Left; }
			set {  _Left = value; }
		}

		internal async Task<float> EvalLeft(Report rpt, Row r)	// return points
		{
			if (_Left == null)
				return await EvalDefault(rpt, r).ConfigureAwait(false);

			string sw = await _Left.EvaluateString(rpt, r).ConfigureAwait(false);
			RSize rs = new RSize(this.OwnerReport, sw);
			return rs.Points;
		}

		internal Expression Right
		{
			get { return  _Right; }
			set {  _Right = value; }
		}

		internal async Task<float> EvalRight(Report rpt, Row r)	// return points
		{
			if (_Right == null)
				return await EvalDefault(rpt, r).ConfigureAwait(false);

			string sw = await _Right.EvaluateString(rpt, r).ConfigureAwait(false);
			RSize rs = new RSize(this.OwnerReport, sw);
			return rs.Points;
		}

		internal Expression Top
		{
			get { return  _Top; }
			set {  _Top = value; }
		}

		internal async Task<float> EvalTop(Report rpt, Row r)	// return points
		{
			if (_Top == null)
				return await EvalDefault(rpt, r).ConfigureAwait(false);

			string sw = await _Top.EvaluateString(rpt, r).ConfigureAwait(false);
			RSize rs = new RSize(this.OwnerReport, sw);
			return rs.Points;
		}

		internal Expression Bottom
		{
			get { return  _Bottom; }
			set {  _Bottom = value; }
		}

		internal async Task<float> EvalBottom(Report rpt, Row r)	// return points
		{
			if (_Bottom == null)
				return await EvalDefault(rpt, r).ConfigureAwait(false);

			string sw = await _Bottom.EvaluateString(rpt, r).ConfigureAwait(false);
			RSize rs = new RSize(this.OwnerReport, sw);
			return rs.Points;
		}
	}
}
