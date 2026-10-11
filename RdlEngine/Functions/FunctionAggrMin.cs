
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
	/// Aggregate function: min
	/// </summary>
	[Serializable]
	internal class FunctionAggrMin : FunctionAggr, IExpr, ICacheData
	{
		private TypeCode _tc;		// type of result: decimal or double
		string _key;				// key for caching; when scope is dataset we can cache the result
		/// <summary>
		/// Aggregate function: Min returns the lowest value
		///	Return type is same as input expression	
		/// </summary>
        public FunctionAggrMin(List<ICacheData> dataCache, IExpr e, object scp)
            : base(e, scp) 
		{
			_key = "aggrmax" + Interlocked.Increment(ref Parser.Counter).ToString();

			// Determine the result
			_tc = e.GetTypeCode();
			dataCache.Add(this);
		}

		public TypeCode GetTypeCode()
		{
			return _tc;
		}

		public async Task<object> Evaluate(Report rpt, Row row)
		{
			bool bSave=true;
			IEnumerable re = this.GetDataScope(rpt, row, out bSave);
			if (re == null)
				return null;

			object v = GetValue(rpt);
			if (v == null)
			{
				object min_value=null;
				object current_value;

				foreach (Row r in re)
				{
					current_value = await _Expr.Evaluate(rpt, r).ConfigureAwait(false);
					if (current_value == null || (current_value is double && double.IsNaN((double)current_value)))
						continue;
					else if (min_value == null)
						min_value = current_value;
					else if (Filter.ApplyCompare(_tc, min_value, current_value) > 0)
						min_value = current_value;
				}
				v = min_value;
				if (bSave)
					SetValue(rpt, v);
			}
			return v;
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

		private object GetValue(Report rpt)
		{
			return rpt.Cache.Get(_key);
		}

		private void SetValue(Report rpt, object o)
		{
			rpt.Cache.AddReplace(_key, o);
		}
		#region ICacheData Members

		public void ClearCache(Report rpt)
		{
			rpt.Cache.Remove(_key);
		}

		#endregion
	}
}
