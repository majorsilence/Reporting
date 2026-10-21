
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// RowNumber(scope): the current row's position, from 1, among the rows of its scope.
	///
	/// It was a running count, one more each time the expression was evaluated, which is the
	/// row's position only while the expression is evaluated exactly once for every row. A
	/// RowNumber behind a condition is not: in Switch(RowNumber(Nothing) Mod 2 = 0, "A",
	/// RowNumber(Nothing) Mod 2 = 1, "B") the second one is evaluated only on rows where the
	/// first is false, fell a row behind each time the first was true, and on every fourth row
	/// matched neither case. The position is now read from the scope itself: the scope's rows
	/// are numbered once, when its first row comes up, and each row looks itself up.
	/// </summary>
	[Serializable]
	internal class FunctionAggrRowNumber : FunctionAggr, IExpr, ICacheData
	{
		string _key;			// key of the cached numbering of the current scope

		public FunctionAggrRowNumber(List<ICacheData> dataCache, object scp)
			: base(new ConstantDouble("0"), scp)
		{
			_key = "aggrrownumber" + Interlocked.Increment(ref Parser.Counter).ToString();
			dataCache.Add(this);
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.Int32;
		}

		// Evaluate is for interpretation  (and is relatively slow)
		public async Task<object> Evaluate(Report rpt, Row row)
		{
			return (object)await EvaluateInt32(rpt, row).ConfigureAwait(false);
		}

		public Task<int> EvaluateInt32(Report rpt, Row row)
		{
			bool bSave = true;
			IEnumerable re = this.GetDataScope(rpt, row, out bSave);
			if (re == null)
				return Task.FromResult(int.MinValue);

			Row startrow = null;
			foreach (Row r in re)
			{
				startrow = r;
				break;
			}

			// One numbering per scope instance, rebuilt when a new one starts.
			var numbering = rpt.Cache.Get(_key) as Numbering;
			if (numbering == null || !ReferenceEquals(numbering.Start, startrow))
			{
				numbering = new Numbering(startrow);
				int n = 0;
				foreach (Row r in re)
					numbering.Positions[r] = ++n;
				rpt.Cache.AddReplace(_key, numbering);
			}

			return Task.FromResult(numbering.Positions.TryGetValue(row, out int position) ? position : 0);
		}

		public async Task<double> EvaluateDouble(Report rpt, Row row)
		{
			return await EvaluateInt32(rpt, row).ConfigureAwait(false);
		}

		public async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			return await EvaluateInt32(rpt, row).ConfigureAwait(false);
		}

		public async Task<string> EvaluateString(Report rpt, Row row)
		{
			return Convert.ToString(await EvaluateInt32(rpt, row).ConfigureAwait(false));
		}

		public async Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			return Convert.ToDateTime(await EvaluateInt32(rpt, row).ConfigureAwait(false));
		}

		public void ClearCache(Report rpt)
		{
			rpt.Cache.Remove(_key);
		}

		sealed class Numbering
		{
			public Numbering(Row start) { Start = start; }
			public Row Start { get; }
			public Dictionary<Row, int> Positions { get; } = new Dictionary<Row, int>(ReferenceEqualityComparer.Instance);
		}
	}
}
