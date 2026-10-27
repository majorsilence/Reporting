
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Aggregate function: RunningValue avg
	/// </summary>
	[Serializable]
	internal class FunctionAggrRvAvg : FunctionAggr, IExpr, ICacheData
	{
		private TypeCode _tc;		// type of result: decimal or double
		string _key;				// key for retaining prior rows values
		/// <summary>
		/// Aggregate function: RunningValue Sum returns the sum of all values of the
		///		expression within the scope up to that row
		///	Return type is decimal for decimal expressions and double for all
		///	other expressions.	
		/// </summary>
        public FunctionAggrRvAvg(List<ICacheData> dataCache, IExpr e, object scp)
            : base(e, scp) 
		{
			_key = "aggrrvavg" + Interlocked.Increment(ref Parser.Counter).ToString();

			// Determine the result
			_tc = e.GetTypeCode();
			if (_tc != TypeCode.Decimal)	// if not decimal
				_tc = TypeCode.Double;		// force result to double
			dataCache.Add(this);
		}

		public TypeCode GetTypeCode()
		{
			return _tc;
		}

		// Evaluate is for interpretation  (and is relatively slow)
		public async Task<object> Evaluate(Report rpt, Row row)
		{
			return _tc==TypeCode.Decimal? (object)await EvaluateDecimal(rpt, row).ConfigureAwait(false) : (object)await EvaluateDouble(rpt, row).ConfigureAwait(false);
		}
		
		public async Task<double> EvaluateDouble(Report rpt, Row row)
		{
			RowEnumerable re = this.GetDataScope(rpt, row, out bool bSave);
			if (re == null)
				return double.NaN;

			var (sum, count) = ((double, int)) await RunningState(rpt, _key + "d", row, re, async (r, s) =>
			{
				var (sumSoFar, countSoFar) = s is ValueTuple<double, int> t ? t : (0d, 0);
				return (object)(sumSoFar + await _Expr.EvaluateDouble(rpt, r).ConfigureAwait(false), countSoFar + 1);
			}).ConfigureAwait(false);
			return sum / count;
		}
		
		public async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			RowEnumerable re = this.GetDataScope(rpt, row, out bool bSave);
			if (re == null)
				return decimal.MinValue;

			var (sum, count) = ((decimal, int)) await RunningState(rpt, _key + "m", row, re, async (r, s) =>
			{
				var (sumSoFar, countSoFar) = s is ValueTuple<decimal, int> t ? t : (0m, 0);
				return (object)(sumSoFar + await _Expr.EvaluateDecimal(rpt, r).ConfigureAwait(false), countSoFar + 1);
			}).ConfigureAwait(false);
			return sum / count;
		}

        public async Task<int> EvaluateInt32(Report rpt, Row row)
        {
            return Convert.ToInt32(await Evaluate(rpt, row).ConfigureAwait(false));
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

		#region ICacheData Members

		public void ClearCache(Report rpt)
		{
			rpt.Cache.Remove(_key + "d");
			rpt.Cache.Remove(_key + "m");
		}

		#endregion
	}

}
