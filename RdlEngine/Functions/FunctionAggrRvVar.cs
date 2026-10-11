
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
	/// Aggregate function: RunningValue var
	/// </summary>
	[Serializable]
	internal class FunctionAggrRvVar : FunctionAggr, IExpr, ICacheData
	{
		string _key;				// key for cache of data between invocations
		/// <summary>
		/// Aggregate function: RunningValue var returns the variance of all values of the
		///		expression within the scope up to that row
		///	Return type is double for all expressions.	
		/// </summary>
        public FunctionAggrRvVar(List<ICacheData> dataCache, IExpr e, object scp)
            : base(e, scp) 
		{
			_key = "aggrrvvar" + Interlocked.Increment(ref Parser.Counter).ToString();
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
			bool bSave=true;
			IEnumerable re = this.GetDataScope(rpt, row, out bSave);
			if (re == null)
				return double.NaN;

			Row startrow=null;
			foreach (Row r in re)
			{
				startrow = r;			// We just want the first row
				break;
			}

			WorkClass wc = GetValue(rpt);
			double currentValue = await _Expr.EvaluateDouble(rpt, row).ConfigureAwait(false);
			if (row == startrow)
			{
				// restart the group
				wc.Sum = wc.Sum2 = 0;
				wc.Count = 0;
			}
			
			if (currentValue.CompareTo(double.NaN) != 0)
			{
				wc.Sum += currentValue;
				wc.Sum2 += (currentValue*currentValue);
				wc.Count++;
			}

			double result;
			if (wc.Count > 1)
				result = ((wc.Count * wc.Sum2) - (wc.Sum*wc.Sum)) / (wc.Count * (wc.Count-1));
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
		private WorkClass GetValue(Report rpt)
		{
			WorkClass wc = rpt.Cache.Get(_key) as WorkClass;
			if (wc == null)
			{
				wc = new WorkClass();
				rpt.Cache.Add(_key, wc);
			}
			return wc;
		}

		private void SetValue(Report rpt, WorkClass w)
		{
			rpt.Cache.AddReplace(_key, w);
		}

		#region ICacheData Members

		public void ClearCache(Report rpt)
		{
			rpt.Cache.Remove(_key);
		}

		#endregion

		class WorkClass
		{
			internal double Sum;		
			internal double Sum2;		
			internal int Count;			
			internal WorkClass()
			{
				Sum = Sum2 = 0;
				Count = -1;
			}
		}
	}
}
