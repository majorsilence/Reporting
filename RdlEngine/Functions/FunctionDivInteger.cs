using System;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// VB integer division operator, lhs \ rhs: divide, then truncate toward zero.
	/// </summary>
	[Serializable]
	internal class FunctionDivInteger : FunctionBinary, IExpr
	{
		public FunctionDivInteger(IExpr lhs, IExpr rhs)
		{
			_lhs = lhs;
			_rhs = rhs;
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.Double;
		}

		public async Task<IExpr> ConstantOptimization()
		{
			_lhs = await _lhs.ConstantOptimization();
			_rhs = await _rhs.ConstantOptimization();
			if (await _lhs.IsConstant() && await _rhs.IsConstant())
			{
				double d = await EvaluateDouble(null, null);
				return new ConstantDouble(d);
			}
			return this;
		}

		// Evaluate is for interpretation  (and is relatively slow)
		public async Task<object> Evaluate(Report rpt, Row row)
		{
			return await EvaluateDouble(rpt, row);
		}

		public async Task<double> EvaluateDouble(Report rpt, Row row)
		{
			double lhs = await _lhs.EvaluateDouble(rpt, row);
			double rhs = await _rhs.EvaluateDouble(rpt, row);
			return Math.Truncate(lhs / rhs);
		}

		public async Task<int> EvaluateInt32(Report rpt, Row row)
		{
			double result = await EvaluateDouble(rpt, row);
			return Convert.ToInt32(result);
		}

		public async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			double result = await EvaluateDouble(rpt, row);
			return Convert.ToDecimal(result);
		}

		public async Task<string> EvaluateString(Report rpt, Row row)
		{
			double result = await EvaluateDouble(rpt, row);
			return result.ToString();
		}

		public async Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			double result = await EvaluateDouble(rpt, row);
			return Convert.ToDateTime(result);
		}

		public async Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			double result = await EvaluateDouble(rpt, row);
			return Convert.ToBoolean(result);
		}
	}
}
