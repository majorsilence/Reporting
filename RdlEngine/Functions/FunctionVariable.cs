using System;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Variables!Name.Value (RDL 2008+). A report variable is evaluated the first time it is read
	/// in a report run and keeps that value, as Now() in one must not change from page to page.
	/// A group variable is evaluated where it is read, in the scope of the row being processed,
	/// so an aggregate in it is that group instance's.
	/// </summary>
	[Serializable]
	internal class FunctionVariable : IExpr
	{
		private const string CacheKey = "variablevalue";
		private readonly Variable _Variable;

		public FunctionVariable(Variable v)
		{
			_Variable = v;
		}

		public TypeCode GetTypeCode() => _Variable.Value.GetTypeCode();

		public Task<bool> IsConstant() => Task.FromResult(false);

		public Task<IExpr> ConstantOptimization() => Task.FromResult<IExpr>(this);

		public async Task<object> Evaluate(Report rpt, Row row)
		{
			if (!_Variable.IsReportVariable || rpt == null)
				return await _Variable.Value.Evaluate(rpt, row).ConfigureAwait(false);

			if (rpt.Cache.Get(_Variable, CacheKey) is Box cached)
				return cached.Value;
			object value = await _Variable.Value.Evaluate(rpt, null).ConfigureAwait(false);
			rpt.Cache.AddReplace(_Variable, CacheKey, new Box(value));
			return value;
		}

		public async Task<double> EvaluateDouble(Report rpt, Row row)
		{
			object v = await Evaluate(rpt, row).ConfigureAwait(false);
			return v == null ? double.NaN : Convert.ToDouble(v);
		}

		public async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			object v = await Evaluate(rpt, row).ConfigureAwait(false);
			return v == null ? decimal.MinValue : Convert.ToDecimal(v);
		}

		public async Task<int> EvaluateInt32(Report rpt, Row row)
		{
			object v = await Evaluate(rpt, row).ConfigureAwait(false);
			return v == null ? int.MinValue : Convert.ToInt32(v);
		}

		public async Task<string> EvaluateString(Report rpt, Row row)
		{
			object v = await Evaluate(rpt, row).ConfigureAwait(false);
			return v == null ? null : Convert.ToString(v);
		}

		public async Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			object v = await Evaluate(rpt, row).ConfigureAwait(false);
			return v == null ? DateTime.MinValue : Convert.ToDateTime(v);
		}

		public async Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			object v = await Evaluate(rpt, row).ConfigureAwait(false);
			return v != null && Convert.ToBoolean(v);
		}

		// A cached value of null must be told apart from no value cached.
		private sealed class Box
		{
			internal readonly object Value;
			internal Box(object value) { Value = value; }
		}
	}
}
