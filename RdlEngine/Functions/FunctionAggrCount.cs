
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
	/// Aggregate function: Count
	/// </summary>
	[Serializable]
	internal class FunctionAggrCount : FunctionAggr, IExpr, ICacheData
	{
		string _key;
		/// <summary>
		/// Aggregate function: Count
		/// 
		///	Return type is double
		/// </summary>
        public FunctionAggrCount(List<ICacheData> dataCache, IExpr e, object scp)
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
				return 0;

			int v = GetValue(rpt);
			if (v < 0)
			{
				object temp;
				int count=0;
				foreach (Row r in re)
				{
					temp = await _Expr.Evaluate(rpt, r).ConfigureAwait(false);
					if (temp != null)
					{
						count++;
					}
				}
				v = count;

				if (bSave)
					SetValue(rpt, v);
			}
			return  v;
		}

        public async Task<double> EvaluateDouble(Report rpt, Row row)
        {
            int d = await EvaluateInt32(rpt, row).ConfigureAwait(false);

            return Convert.ToDouble(d);
        }
		
		public async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			double d = await EvaluateDouble(rpt, row).ConfigureAwait(false);

			return Convert.ToDecimal(d);
		}

		public async Task<string> EvaluateString(Report rpt, Row row)
		{
			double result = await EvaluateDouble(rpt, row).ConfigureAwait(false);
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
			return oi == null? -1: oi.i;
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
