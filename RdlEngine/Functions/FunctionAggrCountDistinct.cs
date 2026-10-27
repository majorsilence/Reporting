
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
	/// Aggregate function: CountDistinct
	/// </summary>
	[Serializable]
	internal class FunctionAggrCountDistinct : FunctionAggr, IExpr, ICacheData
	{
		string _key;			// key used for caching value
		/// <summary>
		/// Aggregate function: CountDistinct
		/// 
		///	Return type is double
		/// </summary>
        public FunctionAggrCountDistinct(List<ICacheData> dataCache, IExpr e, object scp)
            : base(e, scp) 
		{
			_key = "countdistinct" + Interlocked.Increment(ref Parser.Counter).ToString();

			dataCache.Add(this);
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.Int32;
		}

		// Evaluate is for interpretation  (and is relatively slow)
		public async Task<object> Evaluate(Report rpt, Row row)
		{
			return (object)await EvaluateDouble(rpt, row).ConfigureAwait(false);
		}
		
		public async Task<int> EvaluateInt32(Report rpt, Row row)
		{
			bool bSave=true;
			RowEnumerable re = this.GetDataScope(rpt, row, out bSave);
			if (re == null)
				return 0;

			int v = GetValue(rpt);
			if (v < 0)
			{
				object temp;
				int count = Math.Max(2, re.LastRow - re.FirstRow);
				Hashtable ht = new Hashtable(count);
				foreach (Row r in re)
				{
					temp = await _Expr.Evaluate(rpt, r).ConfigureAwait(false);
					if (!IsNullValue(temp))
					{
						object o = ht[temp];	// search for it
						if (o == null)			// if not found; add it to the hash table
						{
							ht.Add(temp, temp);
						}
					}
				}
				v = ht.Count;
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
			int d = await EvaluateInt32(rpt, row).ConfigureAwait(false);

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
