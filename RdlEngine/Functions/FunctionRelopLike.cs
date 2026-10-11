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
			_lhs = await _lhs.ConstantOptimization().ConfigureAwait(false);
			_rhs = await _rhs.ConstantOptimization().ConfigureAwait(false);
			if (await _lhs.IsConstant().ConfigureAwait(false) && await _rhs.IsConstant().ConfigureAwait(false))
			{
				bool b = await EvaluateBoolean(null, null).ConfigureAwait(false);
				return new ConstantBoolean(b);
			}

			return this;
		}

		public async Task<object> Evaluate(Report rpt, Row row)
		{
			return await EvaluateBoolean(rpt, row).ConfigureAwait(false);
		}

		public async Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			string text = await _lhs.EvaluateString(rpt, row).ConfigureAwait(false);
			string pattern = await _rhs.EvaluateString(rpt, row).ConfigureAwait(false);
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
			bool result = await EvaluateBoolean(rpt, row).ConfigureAwait(false);
			return result.ToString();
		}

		public Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			return Task.FromResult(DateTime.MinValue);
		}
	}
}
