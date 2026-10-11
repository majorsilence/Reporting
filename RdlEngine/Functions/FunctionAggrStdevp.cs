
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
	/// Aggregate function: Stdevp
	/// </summary>
	[Serializable]
	internal class FunctionAggrStdevp : FunctionAggr, IExpr, ICacheData
	{
		string _key;				// key for cache when scope is dataset we can cache the result
		/// <summary>
		/// Aggregate function: Stdevp = (sqrt(n sum(square(x)) - square((sum(x))) / n*n)
		/// Stdev assumes values are a sample of the population of data.  If the data
		/// is the entire representation then use Stdevp.
		/// 
		///	Return type is decimal for decimal expressions and double for all
		///	other expressions.	
		/// </summary>
        public FunctionAggrStdevp(List<ICacheData> dataCache, IExpr e, object scp)
            : base(e, scp) 
		{
			_key = "aggrstdevp" + Interlocked.Increment(ref Parser.Counter).ToString();

			dataCache.Add(this);
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.Double;
		}

		// Evaluate is for interpretation  (and is relatively slow)
		public async Task<object> Evaluate(Report rpt, Row row)
		{
			double d = await EvaluateDouble(rpt, row).ConfigureAwait(false);
			if (d.CompareTo(double.NaN) == 0)
				return null;
			return (object) d;
		}
		
		public async Task<double> EvaluateDouble(Report rpt, Row row)
		{
			bool bSave=true;
			IEnumerable re = this.GetDataScope(rpt, row, out bSave);
			if (re == null)
				return double.NaN;

			ODouble v = GetValue(rpt);
			if (v != null)
				return v.d;

			double sum=0;
			double sum2=0;
			int count=0;
			double temp;
			foreach (Row r in re)
			{
				temp = await _Expr.EvaluateDouble(rpt, r).ConfigureAwait(false);
				if (temp.CompareTo(double.NaN) != 0)
				{
					sum += temp;
					sum2 += (temp*temp);
					count++;
				}
			}

			double result;
			if (count > 0)
			{
				result = Math.Sqrt((((count * sum2) - (sum*sum)) / (count * count)));
			}
			else
				result = double.NaN;

			if (bSave)
				SetValue(rpt, result);

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
			double result = await EvaluateDouble(rpt, row).ConfigureAwait(false);
			if (result.CompareTo(double.NaN) == 0)
				return null;
			return Convert.ToString(result);
		}

		public async Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			object result = await Evaluate(rpt, row).ConfigureAwait(false);
			return Convert.ToDateTime(result);
		}

		private ODouble GetValue(Report rpt)
		{
			return rpt.Cache.Get(_key) as ODouble;
		}

		private void SetValue(Report rpt, double d)
		{
			rpt.Cache.AddReplace(_key, new ODouble(d));
		}
		#region ICacheData Members

		public void ClearCache(Report rpt)
		{
			rpt.Cache.Remove(_key);
		}

		#endregion
	}
}
