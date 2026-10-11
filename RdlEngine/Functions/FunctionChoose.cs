
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Choose function of the form Choose(int, expr1, expr2, ...)
	/// 
	///	
	/// </summary>
	[Serializable]
	internal class FunctionChoose : IExpr
	{
		IExpr[] _expr;
		TypeCode _tc;

		/// <summary>
		/// Choose function of the form Choose(int, expr1, expr2, ...)
		/// </summary>
		public FunctionChoose(IExpr[] ie) 
		{
			_expr = ie;
			_tc = _expr[1].GetTypeCode();

		}

		public TypeCode GetTypeCode()
		{
			return _tc;
		}

		public Task<bool> IsConstant()
		{
			return Task.FromResult(false);		// we could be more sophisticated here; but not much benefit
		}

		public async Task<IExpr> ConstantOptimization()
		{
			// simplify all expression if possible
			for (int i=0; i < _expr.Length; i++)
			{
				_expr[i] = await _expr[i].ConstantOptimization().ConfigureAwait(false);
			}

			return this;
		}

		// 
		public async Task<object> Evaluate(Report rpt, Row row)
		{
			double di = await _expr[0].EvaluateDouble(rpt, row).ConfigureAwait(false);
			int i = (int) di;		// force it to integer; we'll accept truncation
			if (i >= _expr.Length || i <= 0)
				return null;
			
			return await _expr[i].Evaluate(rpt, row).ConfigureAwait(false);
		}

		public async Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			object result = await Evaluate(rpt, row).ConfigureAwait(false);
			return Convert.ToBoolean(result);
		}
		
		public async Task<double> EvaluateDouble(Report rpt, Row row)
		{
			object result = await Evaluate(rpt, row).ConfigureAwait(false);
			return Convert.ToDouble(result);
		}
		
		public async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			object result = await Evaluate(rpt, row).ConfigureAwait(false);
			return Convert.ToDecimal(result);
		}

        public async Task<int> EvaluateInt32(Report rpt, Row row)
        {
            object result = await Evaluate(rpt, row).ConfigureAwait(false);
            return Convert.ToInt32(result);
        }

		public async Task<string> EvaluateString(Report rpt, Row row)
		{
			object result = await Evaluate(rpt, row).ConfigureAwait(false);
			return Convert.ToString(result);
		}

		public async Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			object result = await Evaluate(rpt, row).ConfigureAwait(false);
			return Convert.ToDateTime(result);
		}
	}
}
