
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
	/// Aggregate function: average
	/// </summary>
	[Serializable]
	internal class FunctionAggrAvg : FunctionAggr, IExpr, ICacheData
	{
		private TypeCode _tc;		// type of result: decimal or double
		string _key;
		/// <summary>
		/// Aggregate function: Sum returns the sum of all values of the
		///		expression within the scope
		///	Return type is decimal for decimal expressions and double for all
		///	other expressions.	
		/// </summary>
        public FunctionAggrAvg(List<ICacheData> dataCache, IExpr e, object scp)
            : base(e, scp) 
		{
			_key = "aggravg" + Interlocked.Increment(ref Parser.Counter).ToString();

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
			return _tc==TypeCode.Decimal? NullIfNoValue(await EvaluateDecimal(rpt, row).ConfigureAwait(false)) : (object) await EvaluateDouble(rpt, row).ConfigureAwait(false);
		}
		
		public async Task<double> EvaluateDouble(Report rpt, Row row)
		{
			bool bSave=true;
			IEnumerable re = this.GetDataScope(rpt, row, out bSave);
			if (re == null)
				return double.NaN;

			ODouble v = GetValueDouble(rpt);
			if (v != null)
				return v.d;

			double sum=0;
			int count=0;
			double temp;
			foreach (Row r in re)
			{
				temp = await _Expr.EvaluateDouble(rpt, r).ConfigureAwait(false);
				if (temp.CompareTo(double.NaN) != 0)
				{
					sum += temp;
					count++;
				}
			}
			double result;
			if (count > 0)
				result = (sum/count);
			else
				result = double.NaN;

			if (bSave)
				SetValue(rpt, result);

			return result;
		}
        
        public async Task<int> EvaluateInt32(Report rpt, Row row)
        {
            if (row == null)
                return int.MinValue;
            return Convert.ToInt32(await EvaluateDouble(rpt, row).ConfigureAwait(false));
        }		
		public async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			bool bSave;
			IEnumerable re = this.GetDataScope(rpt, row, out bSave);
			if (re == null)
				return decimal.MinValue;

			ODecimal v = GetValueDecimal(rpt);
			if (v != null)
				return v.d;

			decimal sum=0;
			int count=0;
			decimal temp;
			foreach (Row r in re)
			{
				temp = await _Expr.EvaluateDecimal(rpt, r).ConfigureAwait(false);
				if (temp != decimal.MinValue)		// indicate null value
				{
					sum += temp;
					count++;
				}
			}
			decimal result;
			if (count > 0)
				result = (sum/count);
			else
				result = decimal.MinValue;
			if (bSave)
				SetValue(rpt, result);

			return result;
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

		private ODecimal GetValueDecimal(Report rpt)
		{
			return rpt.Cache.Get(_key) as ODecimal;
		}

		private ODouble GetValueDouble(Report rpt)
		{
			return rpt.Cache.Get(_key) as ODouble;
		}

		private void SetValue(Report rpt, double d)
		{
			rpt.Cache.AddReplace(_key, new ODouble(d));
		}

		private void SetValue(Report rpt, decimal d)
		{
			rpt.Cache.AddReplace(_key, new ODecimal(d));
		}
		#region ICacheData Members

		public void ClearCache(Report rpt)
		{
			rpt.Cache.Remove(_key);
		}

		#endregion
	}
}
