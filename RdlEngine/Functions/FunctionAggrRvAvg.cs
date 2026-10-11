
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
			double currentValue = await _Expr.EvaluateDouble(rpt, row).ConfigureAwait(false);
			WorkClass wc = GetValue(rpt);
			if (row == startrow)
			{
				// must be the start of a new group
				wc.Value = currentValue;
				wc.Count = 1;
			}
			else
			{
				wc.Value = ((double) wc.Value + currentValue);
				wc.Count++;
			}

			return (double) wc.Value / wc.Count;
		}
		
		public async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			bool bSave;
			IEnumerable re = this.GetDataScope(rpt, row, out bSave);
			if (re == null)
				return decimal.MinValue;

			Row startrow=null;
			foreach (Row r in re)
			{
				startrow = r;			// We just want the first row
				break;
			}

			decimal currentValue = await _Expr.EvaluateDecimal(rpt, row).ConfigureAwait(false);
			WorkClass wc = GetValue(rpt);
			if (row == startrow)
			{
				// must be the start of a new group
				wc.Value = currentValue;
				wc.Count = 1;
			}
			else
			{
				wc.Value = ((decimal) wc.Value + currentValue);
				wc.Count++;
			}

			return (decimal) wc.Value / wc.Count;
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
			internal object Value;		
			internal int Count;			
			internal WorkClass()
			{
				Value = null;
				Count = 0;
			}
		}
	}

}
