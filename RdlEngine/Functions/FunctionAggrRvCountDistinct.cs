using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Aggregate function: RunningValue CountDistinct
	/// </summary>
	[Serializable]
	internal class FunctionAggrRvCountDistinct : FunctionAggr, IExpr, ICacheData
	{
		string _key;			// key to cached between invocations
		/// <summary>
		/// Aggregate function: RunningValue CountDistinct returns the number of distinct non-null
		///		values of the expression within the scope up to that row
		///	Return type is int.
		/// </summary>
        public FunctionAggrRvCountDistinct(List<ICacheData> dataCache, IExpr e, object scp)
            : base(e, scp)
		{
			_key = "aggrrvcountdistinct" + Interlocked.Increment(ref Parser.Counter).ToString();
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

		// The state is the set of distinct values so far: it holds each value once, however many
		// rows the scope has, and the next row adds its value to the set in place.
		public async Task<int> EvaluateInt32(Report rpt, Row row)
		{
			RowEnumerable re = this.GetDataScope(rpt, row, out bool bSave);
			if (re == null)
				return int.MinValue;

			object seen = await RunningState(rpt, _key, row, re,
				async (r, s) =>
				{
					HashSet<object> set = s as HashSet<object> ?? new HashSet<object>();
					object v = await _Expr.Evaluate(rpt, r).ConfigureAwait(false);
					if (v != null && v is not DBNull)
						set.Add(v);
					return (object)set;
				}).ConfigureAwait(false);
			return ((HashSet<object>) seen).Count;
		}

        public async Task<double> EvaluateDouble(Report rpt, Row row)
        {
            return (double)await EvaluateInt32(rpt, row).ConfigureAwait(false);
        }

		public async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			return (decimal)await EvaluateInt32(rpt, row).ConfigureAwait(false);
		}

		public async Task<string> EvaluateString(Report rpt, Row row)
		{
			object result = await EvaluateInt32(rpt, row).ConfigureAwait(false);
			return Convert.ToString(result);
		}

		public async Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			object result = await Evaluate(rpt, row).ConfigureAwait(false);
			return Convert.ToDateTime(result);
		}

		#region ICacheData Members

		public void ClearCache(Report rpt)
		{
			rpt.Cache.Remove(_key);
		}

		#endregion
	}
}
