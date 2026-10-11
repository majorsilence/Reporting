

using System;
using System.Xml;
using System.Collections;
using System.Collections.Specialized;
using System.Threading;
using Majorsilence.Reporting.Rdl;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// A report expression: includes original source, parsed expression and type information.
	///</summary>
	[Serializable]
	internal class Expression : ReportLink, IExpr
	{
		string _Source;			// source of expression
		IExpr _Expr;			// expression after parse
		TypeCode _Type;			// type of expression; only available after parsed
		ExpressionType _ExpectedType;	// expected type of expression
		string _UniqueName;		// unique name of expression; not always created
	
		internal Expression(ReportDefn r, ReportLink p, XmlNode xNode, ExpressionType et) : this(r, p, xNode.InnerText, et){}

        internal Expression(ReportDefn r, ReportLink p, String xNode, ExpressionType et) : base(r, p)
        {
            _Source = xNode;
            _Type = TypeCode.Empty;
            _ExpectedType = et;
            _Expr = null;
        }

		// UniqueName of expression
		internal string UniqueName
		{
			get {return _UniqueName;}
		}

		override internal async Task FinalPass()
		{
			// optimization: avoid expression overhead if this isn't really an expression
			if (_Source == null)
			{
				_Expr = new Constant("");
				return;
			}
			else if (_Source == "" ||			// empty expression
				_Source[0] != '=')	// if 1st char not '='
			{
				_Expr = new Constant(_Source);	//   this is a constant value
				return;
			}

			Parser p = new Parser(OwnerReport.DataCache);

			// find the fields that are part of the DataRegion (if there is one)
			IDictionary fields=null;
			ReportLink dr = Parent;
			Grouping grp= null;		// remember if in a table group or detail group or list group
			Matrix m=null;
			ReportLink phpf=null;
			bool inDetails = false;
			while (dr != null)
			{
				if (dr is Grouping)
					p.NoAggregateFunctions = true;
				else if (dr is TableGroup)
					grp = ((TableGroup) dr).Grouping;
				else if (dr is Matrix)
				{
					m = (Matrix) dr;		// if matrix we need to pass special
					break;
				}
				else if (dr is Details)
				{
					grp = ((Details) dr).Grouping;
					inDetails = true;
				}
				else if (dr is List)
				{
					grp = ((List) dr).Grouping;
					break;
				}
				else if (dr is PageHeader || dr is PageFooter)
				{
					phpf = dr;
				}
				else if (dr is DataRegion || dr is DataSetDefn)
					break;
				dr = dr.Parent;
			}
			if (dr != null)
			{
				if (dr is DataSetDefn)
				{
					DataSetDefn d = (DataSetDefn) dr;
					if (d.Fields != null)
						fields = d.Fields.Items;
				}
				else	// must be a DataRegion
				{
					DataRegion d = (DataRegion) dr;
					if (d.DataSetDefn != null &&
						d.DataSetDefn.Fields != null)
						fields = d.DataSetDefn.Fields.Items;
				}
			}

			NameLookup lu = new NameLookup(fields, OwnerReport.LUReportParameters,
				OwnerReport.LUReportItems,OwnerReport.LUGlobals,
				OwnerReport.LUUser, OwnerReport.LUAggrScope,
				grp, m, OwnerReport.CodeModules, OwnerReport.Classes, OwnerReport.DataSetsDefn,
				OwnerReport.CodeType);

			lu.ContextTable = dr as Table;
			lu.InTableDetails = inDetails;

			if (phpf != null)
			{
				// Non-null when expression is in PageHeader or PageFooter; 
				//   Expression name needed for dynamic lookup of ReportItems on a page.
				lu.PageFooterHeader = phpf;
				lu.ExpressionName = _UniqueName = "xn_" + Interlocked.Increment(ref Parser.Counter).ToString();
			}

			try 
			{
				_Expr = await p.Parse(lu, _Source);
			}
			catch (Exception e)
			{
				_Expr = new ConstantError(e.Message);
				// Invalid expression
				OwnerReport.rl.LogError(8, ErrorText(e.Message));
			}

			// Optimize removing any expression that always result in a constant
			try
			{
				_Expr = await _Expr.ConstantOptimization();
			}
			catch(Exception ex)
			{
				OwnerReport.rl.LogError(4, "Expression:" + _Source + "\r\nConstant Optimization exception:\r\n" + ex.Message + "\r\nStack trace:\r\n" + ex.StackTrace );
			}
			_Type = _Expr.GetTypeCode();

			return;
		}

        private string ErrorText(string msg)
        {
            ReportLink rl = this.Parent;
            while (rl != null)
            {
                if (rl is ReportItem)
                    break;
                rl = rl.Parent;
            }

            string prefix="Expression";
            if (rl != null)
            {          
                ReportItem ri = rl as ReportItem;
                if (ri.Name != null)
                    prefix = ri.Name.Nm + " expression";
            }
            return prefix + " '" + _Source + "' failed to parse: " + msg;
        }

		private void ReportError(Report rpt, int severity, string err)
		{
			if (rpt == null)
				OwnerReport.rl.LogError(severity, err);
			else
				rpt.rl.LogError(severity, err);
		}

		internal string Source
		{
			get { return  _Source; }
		}
		internal IExpr Expr
		{
			get { return  _Expr; }
		}
		internal TypeCode Type
		{
			get { return  _Type; }
		}
		internal ExpressionType ExpectedType
		{
			get { return  _ExpectedType; }
		}
		#region IExpr Members

		public System.TypeCode GetTypeCode()
		{
            if (_Expr == null)
                return System.TypeCode.Object;      // we really don't know the type
			return _Expr.GetTypeCode();
		}

		public async Task<bool> IsConstant()
		{
            if (_Expr == null)
            {
                await this.FinalPass();           // expression hasn't been parsed yet -- let try to parse it.
                if (_Expr == null)
                    return false;           // no luck; then don't treat as constant
            }
            return await _Expr.IsConstant();
		}

		public Task<IExpr> ConstantOptimization()
		{
			return Task.FromResult(this as IExpr);
		}

		public async Task<object> Evaluate(Report rpt, Row row)
		{
			try 
			{
				// Check to see if we're evaluating an expression in a page header or footer;
				//   If that is the case the rows are cached by page.
				if (row == null && this.UniqueName != null)
				{
					Rows rows = rpt.GetPageExpressionRows(UniqueName);
					if (rows != null && rows.Data != null && rows.Data.Count > 0)
						row = rows.Data[0];
				}

				return await _Expr.Evaluate(rpt, row);
			}
			catch (Exception e)
			{
				string err;
				if (e.InnerException != null)
					err = String.Format("Exception evaluating {0}.  {1}.  {2}", _Source, e.Message, e.InnerException.Message);
				else
					err = String.Format("Exception evaluating {0}.  {1}", _Source, e.Message);

				ReportError(rpt, 4, err);
				return null;
			}
		}

		public async Task<string> EvaluateString(Report rpt, Row row)
		{
			try 
			{
				return await _Expr.EvaluateString(rpt, row);
			}
			catch (Exception e)
			{	
				string err = String.Format("Exception evaluating {0}.  {1}", _Source, e.Message);
				ReportError(rpt, 4, err);
				return null;
			}
		}

		public async Task<double> EvaluateDouble(Report rpt, Row row)
		{
			try 
			{
				return await _Expr.EvaluateDouble(rpt, row);
			}
			catch (Exception e)
			{	
				string err = String.Format("Exception evaluating {0}.  {1}", _Source, e.Message);
				ReportError(rpt, 4, err);
				return double.NaN;
			}
		}

		public async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			try 
			{
				return await _Expr.EvaluateDecimal(rpt, row);
			}
			catch (Exception e)
			{	
				string err = String.Format("Exception evaluating {0}.  {1}", _Source, e.Message);
				ReportError(rpt, 4, err);
				return decimal.MinValue;
			}
		}

        public async Task<int> EvaluateInt32(Report rpt, Row row)
        {
            try
            {
                return await _Expr.EvaluateInt32(rpt, row);
            }
            catch (Exception e)
            {
                string err = String.Format("Exception evaluating {0}.  {1}", _Source, e.Message);
                ReportError(rpt, 4, err);
                return int.MinValue;
            }
        }

		public async Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			try 
			{
				return await _Expr.EvaluateDateTime(rpt, row);
			}
			catch (Exception e)
			{	
				string err = String.Format("Exception evaluating {0}.  {1}", _Source, e.Message);
				ReportError(rpt, 4, err);
				return DateTime.MinValue;
			}
		}

		public async Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			try 
			{
				return await _Expr.EvaluateBoolean(rpt, row);
			}
			catch (Exception e)
			{	
				string err = String.Format("Exception evaluating {0}.  {1}", _Source, e.Message);
				ReportError(rpt, 4, err);
				return false;
			}
		}

		#endregion

        public async Task SetSource(string sql)
        {
            this._Source = sql;
            await FinalPass();
        }

	}
}
