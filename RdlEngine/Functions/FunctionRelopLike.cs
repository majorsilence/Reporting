using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Relational operator of form lhs Like rhs, matching a string against a VB pattern.
	/// </summary>
	[Serializable]
	internal class FunctionRelopLike : FunctionBinary, IExpr
	{
		public FunctionRelopLike(IExpr lhs, IExpr rhs)
		{
			_lhs = lhs;
			_rhs = rhs;
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.Boolean;
		}

		public async Task<IExpr> ConstantOptimization()
		{
			_lhs = await _lhs.ConstantOptimization();
			_rhs = await _rhs.ConstantOptimization();
			if (await _lhs.IsConstant() && await _rhs.IsConstant())
			{
				bool b = await EvaluateBoolean(null, null);
				return new ConstantBoolean(b);
			}

			return this;
		}

		public async Task<object> Evaluate(Report rpt, Row row)
		{
			return await EvaluateBoolean(rpt, row);
		}

		public async Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			string text = await _lhs.EvaluateString(rpt, row);
			string pattern = await _rhs.EvaluateString(rpt, row);
			return VBFunctions.Like(text, pattern);
		}

		public Task<double> EvaluateDouble(Report rpt, Row row)
		{
			return Task.FromResult(double.NaN);
		}

		public Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			return Task.FromResult(decimal.MinValue);
		}

		public Task<int> EvaluateInt32(Report rpt, Row row)
		{
			return Task.FromResult(int.MinValue);
		}

		public async Task<string> EvaluateString(Report rpt, Row row)
		{
			bool result = await EvaluateBoolean(rpt, row);
			return result.ToString();
		}

		public Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			return Task.FromResult(DateTime.MinValue);
		}
	}
}
