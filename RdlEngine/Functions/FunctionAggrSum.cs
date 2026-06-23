
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Aggregate function: sum
	/// </summary>
	[Serializable]
	internal class FunctionAggrSum : FunctionAggr, IExpr, ICacheData
	{
		private TypeCode _tc;		// type of result: decimal or double
		string _key;				// key for cache when scope is dataset we can cache the result
		/// <summary>
		/// Aggregate function: Sum returns the sum of all values of the
		///		expression within the scope
		///	Return type is decimal for decimal expressions and double for all
		///	other expressions.
		/// </summary>
        public FunctionAggrSum(List<ICacheData> dataCache, IExpr e, object scp)
            : base(e, scp) 
		{
			_key = "aggrsum" + Interlocked.Increment(ref Parser.Counter).ToString();

			// Determine the result
			_tc = e.GetTypeCode();
			if (_tc != TypeCode.Decimal && _tc != TypeCode.Object)
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
			switch (_tc)
			{
				case TypeCode.Decimal:
					return (object)await EvaluateDecimal(rpt, row);
				case TypeCode.Object:
					return EvaluateObject(rpt, row);
				case TypeCode.Double:
				default:
					return (object)await EvaluateDouble(rpt, row);
			}
		}

		public async Task<object> EvaluateObject(Report rpt, Row row)
		{
			bool bSave;
			IEnumerable re = this.GetDataScope(rpt, row, out bSave);
			if (re == null)
				return null;

			var od = GetValueObject(rpt);
			if (od != null)
				return od;

			object sum = null;
			object temp;

			foreach (Row r in re)
			{
				temp = await _Expr.Evaluate(rpt, r);
				if (temp != null)
				{
					if (sum != null)
						sum = AddObjects(sum, temp);
					else
						sum = temp;
				}
			}
			if (bSave)
				SetValue(rpt, sum);

			return sum;

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
			double temp;
			foreach (Row r in re)
			{
				temp = await _Expr.EvaluateDouble(rpt, r);
				if (temp.CompareTo(double.NaN) != 0)
					sum += temp;
			}
			if (bSave)
				SetValue(rpt, sum);

			return sum;
		}

        public async Task<int> EvaluateInt32(Report rpt, Row row)
        {
            double result = await EvaluateDouble(rpt, row);
            return Convert.ToInt32(result);
        }

		public async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			bool bSave;
			IEnumerable re = this.GetDataScope(rpt, row, out bSave);
			if (re == null)
				return decimal.MinValue;

			ODecimal od = GetValueDecimal(rpt);
			if (od != null)
				return od.d;

			decimal sum=0;
			decimal temp;
			foreach (Row r in re)
			{
				temp = await _Expr.EvaluateDecimal(rpt, r);
				if (temp != decimal.MinValue)		// indicate null value
					sum += temp;
			}
			if (bSave)
				SetValue(rpt,sum);

			return sum;
		}

		public async Task<string> EvaluateString(Report rpt, Row row)
		{
			object result = await Evaluate(rpt, row);
			return Convert.ToString(result);
		}

		public async Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			object result = await Evaluate(rpt, row);
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

		private object GetValueObject(Report rpt)
		{
			return rpt.Cache.Get(_key);
		}

		private void SetValue(Report rpt, double d)
		{
			rpt.Cache.AddReplace(_key, new ODouble(d));
		}

		private void SetValue(Report rpt, decimal d)
		{
			rpt.Cache.AddReplace(_key, new ODecimal(d));
		}

		private void SetValue(Report rpt, object d)
		{
			rpt.Cache.AddReplace(_key, d);
		}

		#region ICacheData Members

		public void ClearCache(Report rpt)
		{
			rpt.Cache.Remove(_key);
		}

		#endregion

		private static object AddObjects(object a, object b)
		{
			return Type.GetTypeCode(a.GetType()) switch
			{
				TypeCode.Int32   => (int)a    + (int)b,
				TypeCode.Int64   => (long)a   + (long)b,
				TypeCode.Double  => (double)a + (double)b,
				TypeCode.Decimal => (decimal)a + (decimal)b,
				TypeCode.Single  => (float)a  + (float)b,
				TypeCode.Int16   => (short)a  + (short)b,
				TypeCode.UInt32  => (uint)a   + (uint)b,
				TypeCode.UInt64  => (ulong)a  + (ulong)b,
				TypeCode.UInt16  => (ushort)a + (ushort)b,
				TypeCode.Byte    => (byte)a   + (byte)b,
				TypeCode.SByte   => (sbyte)a  + (sbyte)b,
				_ => Convert.ToDouble(a) + Convert.ToDouble(b),
			};
		}
	}
}
