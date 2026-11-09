
using System;
using System.Collections;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// SSRS MultiLookup(source, destination, result, dataset) function: source is an
	/// array of keys (typically Parameters!X.Value of a MultiValue parameter); the
	/// returned array has one element per source key, each the Result of the key's
	/// first matching row in DataSet, or Nothing for a key with no match. Unlike
	/// LookupSet, the returned array's length and order always mirror source's.
	/// </summary>
	[Serializable]
	internal class FunctionMultiLookup : FunctionLookupBase, IExpr
	{
		public FunctionMultiLookup(IExpr source, IExpr destination, IExpr result, DataSetDefn dataSet)
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

			ArrayList results = new ArrayList();
			foreach (object key in AsKeys(source))
			{
				Row match = await FindFirstMatch(rpt, targetRows, key).ConfigureAwait(false);
				results.Add(match == null ? null : await _Result.Evaluate(rpt, match).ConfigureAwait(false));
			}
			return results;
		}

		// A string is IEnumerable (of char) but, like VB's Join, is a scalar key here, not
		// an array of one-character keys; anything else non-enumerable is a single key.
		private static IEnumerable AsKeys(object source)
		{
			if (source is string || source is not IEnumerable en)
				return new object[] { source };
			return en;
		}

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
