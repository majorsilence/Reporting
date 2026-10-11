
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Plus operator  of form lhs + rhs where operands are strings
	/// </summary>
	[Serializable]
	internal class FunctionPlusString : FunctionBinary, IExpr
	{

		/// <summary>
		/// append two strings together
		/// </summary>
		public FunctionPlusString(IExpr lhs, IExpr rhs) 
		{
			_lhs = lhs;
			_rhs = rhs;
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.String;
		}

		// Evaluate is for interpretation  (and is relatively slow)
		public async Task<object> Evaluate(Report rpt, Row row)
		{
			return await EvaluateString(rpt, row).ConfigureAwait(false);
		}
	
		public async Task<IExpr> ConstantOptimization()
		{
			_lhs = await _lhs.ConstantOptimization().ConfigureAwait(false);
			_rhs = await _rhs.ConstantOptimization().ConfigureAwait(false);
			if (await _lhs.IsConstant().ConfigureAwait(false) && await _rhs.IsConstant().ConfigureAwait(false))
			{
				string s = await EvaluateString(null, null).ConfigureAwait(false);
				return new ConstantString(s);
			}

			return this;
		}
	
		public async Task<double> EvaluateDouble(Report rpt, Row row)
		{
			string result = await EvaluateString(rpt, row).ConfigureAwait(false);

			return Convert.ToDouble(result);
		}
		
		public async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			string result = await EvaluateString(rpt, row).ConfigureAwait(false);
			return Convert.ToDecimal(result);
		}

        public async Task<int> EvaluateInt32(Report rpt, Row row)
        {
            string result = await EvaluateString(rpt, row).ConfigureAwait(false);
            return Convert.ToInt32(result);
        }

		public async Task<string> EvaluateString(Report rpt, Row row)
		{
			string lhs = await _lhs.EvaluateString(rpt, row).ConfigureAwait(false);
			string rhs = await _rhs.EvaluateString(rpt, row).ConfigureAwait(false);

			if (lhs != null && rhs != null)
				return lhs + rhs;
			else
				return null;
		}

		public async Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			string result = await EvaluateString(rpt, row).ConfigureAwait(false);
			return Convert.ToDateTime(result);
		}

		public async Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			string result = await EvaluateString(rpt, row).ConfigureAwait(false);
			return Convert.ToBoolean(result);
		}
	}
}
