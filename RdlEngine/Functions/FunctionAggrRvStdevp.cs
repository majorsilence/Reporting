
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
	/// Aggregate function: RunningValue stdevp
	/// </summary>
	[Serializable]
	internal class FunctionAggrRvStdevp : FunctionAggr, IExpr, ICacheData
	{
		string _key;				// key for caching holding between invocations
		/// <summary>
		/// Aggregate function: RunningValue Stdevp returns the Stdevp of all values of the
		///		expression within the scope up to that row
		///	Return type is double for all expressions.	
		/// </summary>
        public FunctionAggrRvStdevp(List<ICacheData> dataCache, IExpr e, object scp)
            : base(e, scp) 
		{
			_key = "aggrrvstdevp" + Interlocked.Increment(ref Parser.Counter).ToString();
			dataCache.Add(this);
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.Double;
		}

		public async Task<object> Evaluate(Report rpt, Row row)
		{
			return (object)await EvaluateDouble(rpt, row).ConfigureAwait(false);
		}
		
		public async Task<double> EvaluateDouble(Report rpt, Row row)
		{
			RowEnumerable re = this.GetDataScope(rpt, row, out bool bSave);
			if (re == null)
				return double.NaN;

			var (sum, sum2, count) = ((double, double, int)) await RunningState(rpt, _key, row, re, async (r, s) =>
			{
				var (sumSoFar, sum2SoFar, countSoFar) = s is ValueTuple<double, double, int> t ? t : (0d, 0d, 0);
				double v = await _Expr.EvaluateDouble(rpt, r).ConfigureAwait(false);
				return (object)(v.CompareTo(double.NaN) == 0
					? (sumSoFar, sum2SoFar, countSoFar)
					: (sumSoFar + v, sum2SoFar + v * v, countSoFar + 1));
			}).ConfigureAwait(false);

			double result;
			if (count > 0)
				result = Math.Sqrt(((count * sum2) - (sum*sum)) / (count * count));
			else
				result = double.NaN;

			return result;
		}
		
		public async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			double d = await EvaluateDouble(rpt, row).ConfigureAwait(false);
			if (d.CompareTo(double.NaN) == 0)
				return decimal.MinValue;

			return Convert.ToDecimal(d);
		}

        public async Task<int> EvaluateInt32(Report rpt, Row row)
        {
            double d = await EvaluateDouble(rpt, row).ConfigureAwait(false);
            if (d.CompareTo(double.NaN) == 0)
                return int.MinValue;

            return Convert.ToInt32(d);
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
			rpt.Cache.Remove(_key);
		}

		#endregion
	}
}
