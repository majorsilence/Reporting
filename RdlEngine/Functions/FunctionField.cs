
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Globalization;

using Majorsilence.Reporting.Rdl;
using System.Threading.Tasks;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Obtain the the Field's value from a row.
	/// </summary>
	[Serializable]
	internal class FunctionField : IExpr
	{
		protected Field f;	
		private string _Name;		// when we have an unresolved field;

		/// <summary>
		/// obtain value of Field
		/// </summary>
		public FunctionField(Field fld) 
		{
			f = fld;
		}

		public FunctionField(string name) 
		{
			_Name = name;
		}

		public string Name
		{
			get {return _Name;}
		}

		public virtual TypeCode GetTypeCode()
		{
			return f.RunType;
		}

		public virtual Field Fld
		{
			get { return f; }
			set { f = value; }
		}

		public virtual Task<bool> IsConstant()
		{
			return Task.FromResult(false);
		}

		public virtual async Task<IExpr> ConstantOptimization()
		{	
			if (f.Value != null)
				return await f.Value.ConstantOptimization().ConfigureAwait(false);

			return this;	// not a constant
		}

		// 
		public virtual async Task<object> Evaluate(Report rpt, Row row)
		{
			if (row == null)
				return null;
			object o;
			if (f.Value != null)
				o = await f.Value.Evaluate(rpt, row).ConfigureAwait(false);
			else
				o = row.Data[f.ColumnNumber];

			return Normalize(o);
		}

		/// <summary>
		/// The value of a plain field of a row, without a Task: false when the field is calculated (its Value
		/// expression has to be evaluated, which may be async).
		/// </summary>
		internal bool TryGet(Row row, out object value)
		{
			if (row == null)
			{
				value = null;
				return true;
			}
			if (f.Value != null)
			{
				value = null;
				return false;
			}

			value = Normalize(row.Data[f.ColumnNumber]);
			return true;
		}

		private object Normalize(object o)
		{
            if (o == DBNull.Value)
            {
                if (IsNumericType(f.RunType))
                    return double.NaN;
                else
                    return null;
            }
			if (f.RunType == TypeCode.String && o is char)	// work around; mono odbc driver confuses string and char
				o = Convert.ChangeType(o, TypeCode.String);
			
			return o;
		}

        private bool IsNumericType(TypeCode tc)
        {
            switch (tc)
            {
                case TypeCode.Double:
                case TypeCode.Decimal:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }

		public virtual async Task<double> EvaluateDouble(Report rpt, Row row)
		{
			if (row == null)
				return Double.NaN;
			return Convert.ToDouble(await Evaluate(rpt, row).ConfigureAwait(false), NumberFormatInfo.InvariantInfo);
		}
		
		public virtual async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			if (row == null)
				return decimal.MinValue;
			var value = await Evaluate(rpt, row).ConfigureAwait(false);
			if (value is double && double.IsNaN((double)value))
				return decimal.MinValue;
			return Convert.ToDecimal(value, NumberFormatInfo.InvariantInfo);
		}

        public virtual async Task<int> EvaluateInt32(Report rpt, Row row)
        {
            if (row == null)
                return int.MinValue;
            return Convert.ToInt32(await Evaluate(rpt, row).ConfigureAwait(false), NumberFormatInfo.InvariantInfo);
        }

		public virtual async Task<string> EvaluateString(Report rpt, Row row)
		{
			if (row == null)
				return null;
			return Convert.ToString(await Evaluate(rpt, row).ConfigureAwait(false));
		}

		public virtual async Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			if (row == null)
				return DateTime.MinValue;
			return Convert.ToDateTime(await Evaluate(rpt, row).ConfigureAwait(false));
		}

		public virtual async Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			if (row == null)
				return false;
			return Convert.ToBoolean(await Evaluate(rpt, row).ConfigureAwait(false));
		}
	}
}
