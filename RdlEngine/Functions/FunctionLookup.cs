
using System;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// SSRS Lookup(source, destination, result, dataset) function: the Result from the
	/// first row of DataSet whose Destination equals source, or Nothing if no row
	/// matches. See FunctionLookupBase for the shared matching logic.
	/// </summary>
	[Serializable]
	internal class FunctionLookup : FunctionLookupBase, IExpr
	{
		public FunctionLookup(IExpr source, IExpr destination, IExpr result, DataSetDefn dataSet)
			: base(source, destination, result, dataSet)
		{
		}

		public TypeCode GetTypeCode()
		{
			return _Result.GetTypeCode();
		}

		public async Task<object> Evaluate(Report rpt, Row row)
		{
			object source = await _Source.Evaluate(rpt, row).ConfigureAwait(false);
			var targetRows = await GetTargetRows(rpt).ConfigureAwait(false);
			Row match = await FindFirstMatch(rpt, targetRows, source).ConfigureAwait(false);
			if (match == null)
				return null;

			return await _Result.Evaluate(rpt, match).ConfigureAwait(false);
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
