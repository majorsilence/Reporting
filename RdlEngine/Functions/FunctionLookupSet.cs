
using System;
using System.Collections;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// SSRS LookupSet(source, destination, result, dataset) function: an array of Result
	/// values from every row of DataSet whose Destination equals source (an empty array
	/// when no row matches). Typically wrapped in Join(...) for display. Its result is
	/// TypeCode.Object, same convention FunctionReportParameter uses for a MultiValue
	/// parameter's Value: the array itself, not one of its elements, is the "value".
	/// </summary>
	[Serializable]
	internal class FunctionLookupSet : FunctionLookupBase, IExpr
	{
		public FunctionLookupSet(IExpr source, IExpr destination, IExpr result, DataSetDefn dataSet)
			: base(source, destination, result, dataSet)
		{
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.Object;
		}

		public async Task<object> Evaluate(Report rpt, Row row)
		{
			object source = await _Source.Evaluate(rpt, row).ConfigureAwait(false);
			var targetRows = await GetTargetRows(rpt).ConfigureAwait(false);

			TypeCode tc = _Destination.GetTypeCode();
			ArrayList results = new ArrayList();
			foreach (Row candidate in targetRows)
			{
				object dest = await _Destination.Evaluate(rpt, candidate).ConfigureAwait(false);
				if (Filter.ApplyCompare(tc, dest, source) == 0)
					results.Add(await _Result.Evaluate(rpt, candidate).ConfigureAwait(false));
			}
			return results;
		}

		// An array has no meaningful numeric/date value; same convention
		// FunctionReportParameter follows for its own TypeCode.Object (MultiValue) case.
		public Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			return Task.FromResult(false);
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
			object result = await Evaluate(rpt, row).ConfigureAwait(false);
			return result == null ? null : result.ToString();
		}

		public Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			return Task.FromResult(DateTime.MinValue);
		}
	}
}
