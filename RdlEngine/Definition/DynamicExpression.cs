

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
	internal class DynamicExpression: IExpr
	{
		string _Source;			// source of expression
		IExpr _Expr;			// expression after parse
        TypeCode _Type;
        ReportLink _rl;
	
		private DynamicExpression(string expr, ReportLink p)
		{
			_Source=expr;
			_Expr = null;
            _rl = p;
		}

		// Parsing is async, which a constructor cannot be.
		internal static async Task<DynamicExpression> CreateAsync(Report rpt, ReportLink p, string expr, Row row)
		{
			var de = new DynamicExpression(expr, p);
			de._Type = await de.DoParse(rpt).ConfigureAwait(false);
			return de;
		}

		internal async Task<TypeCode> DoParse(Report rpt)
		{
			// optimization: avoid expression overhead if this isn't really an expression
			if (_Source == null)
			{
				_Expr = new Constant("");
                return _Expr.GetTypeCode();
			}
			else if (_Source == string.Empty ||			// empty expression
				_Source[0] != '=')	// if 1st char not '='
			{
				_Expr = new Constant(_Source);	//   this is a constant value
                return _Expr.GetTypeCode();
			}

			Parser p = new Parser(new System.Collections.Generic.List<ICacheData>());

			// find the fields that are part of the DataRegion (if there is one)
			IDictionary fields=null;
			ReportLink dr = _rl.Parent;
			Grouping grp= null;		// remember if in a table group or detail group or list group
			Matrix m=null;

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
				}
				else if (dr is List)
				{
					grp = ((List) dr).Grouping;
					break;
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

			NameLookup lu = new NameLookup(fields, rpt.ReportDefinition.LUReportParameters,
                rpt.ReportDefinition.LUReportItems, rpt.ReportDefinition.LUGlobals,
                rpt.ReportDefinition.LUUser, rpt.ReportDefinition.LUAggrScope,
                grp, m, rpt.ReportDefinition.CodeModules, rpt.ReportDefinition.Classes, rpt.ReportDefinition.DataSetsDefn,
                rpt.ReportDefinition.CodeType);

			try 
			{
				_Expr = await p.Parse(lu, _Source).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				_Expr = new ConstantError(e.Message);
				// Invalid expression
				rpt.rl.LogError(8, ErrorText(e.Message));
			}

			// Optimize removing any expression that always result in a constant
			try
			{
				_Expr = await _Expr.ConstantOptimization().ConfigureAwait(false);
			}
			catch(Exception ex)
			{
				rpt.rl.LogError(4, "Expression:" + _Source + "\r\nConstant Optimization exception:\r\n" + ex.Message + "\r\nStack trace:\r\n" + ex.StackTrace );
			}

            return _Expr.GetTypeCode();
		}

        private string ErrorText(string msg)
        {
            ReportLink rl = _rl.Parent;
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
		#region IExpr Members

		public System.TypeCode GetTypeCode()
		{
			return _Expr.GetTypeCode();
		}

		public async Task<bool> IsConstant()
		{
			return await _Expr.IsConstant().ConfigureAwait(false);
		}

		public Task<IExpr> ConstantOptimization()
		{
			return Task.FromResult(this as IExpr);
		}

		public async Task<object> Evaluate(Report rpt, Row row)
		{
			try 
			{
				return await _Expr.Evaluate(rpt, row).ConfigureAwait(false);
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
				return await _Expr.EvaluateString(rpt, row).ConfigureAwait(false);
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
				return await _Expr.EvaluateDouble(rpt, row).ConfigureAwait(false);
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
				return await _Expr.EvaluateDecimal(rpt, row).ConfigureAwait(false);
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
                return await _Expr.EvaluateInt32(rpt, row).ConfigureAwait(false);
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
				return await _Expr.EvaluateDateTime(rpt, row).ConfigureAwait(false);
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
                return await _Expr.EvaluateBoolean(rpt, row).ConfigureAwait(false);
			}
			catch (Exception e)
			{	
				string err = String.Format("Exception evaluating {0}.  {1}", _Source, e.Message);
				ReportError(rpt, 4, err);
				return false;
			}
		}

		#endregion
	}
}
