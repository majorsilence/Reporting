
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
	/// Aggregate function: RunningValue count
	/// </summary>
	[Serializable]
	internal class FunctionAggrRvCount : FunctionAggr, IExpr, ICacheData
	{
		string _key;			// key to cached between invocations
		/// <summary>
		/// Aggregate function: RunningValue Sum returns the sum of all values of the
		///		expression within the scope up to that row
		///	Return type is decimal for decimal expressions and double for all
		///	other expressions.	
		/// </summary>
        public FunctionAggrRvCount(List<ICacheData> dataCache, IExpr e, object scp)
            : base(e, scp) 
		{
			_key = "aggrcount" + Interlocked.Increment(ref Parser.Counter).ToString();
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
		
		public async Task<int> EvaluateInt32(Report rpt, Row row)
		{
			bool bSave=true;
			IEnumerable re = this.GetDataScope(rpt, row, out bSave);
			if (re == null)
				return int.MinValue;

			Row startrow=null;
			foreach (Row r in re)
			{
				startrow = r;			// We just want the first row
				break;
			}

			int count;

			object currentValue = await _Expr.Evaluate(rpt, row).ConfigureAwait(false);
			int incr = currentValue == null? 0: 1;
			if (row == startrow)
			{
				// must be the start of a new group
				count = incr;
			}
			else
			{
				count = GetValue(rpt) + incr;
			}

			SetValue(rpt, count);
			return count;
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
			object result = await EvaluateDouble(rpt, row).ConfigureAwait(false);
			return Convert.ToString(result);
		}

		public async Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			object result = await Evaluate(rpt, row).ConfigureAwait(false);
			return Convert.ToDateTime(result);
		}
		private int GetValue(Report rpt)
		{
			OInt oi = rpt.Cache.Get(_key) as OInt;
			return oi == null? 0: oi.i;
		}

		private void SetValue(Report rpt, int i)
		{
			rpt.Cache.AddReplace(_key, new OInt(i));
		}

		#region ICacheData Members

		public void ClearCache(Report rpt)
		{
			rpt.Cache.Remove(_key);
		}

		#endregion
	}
}
