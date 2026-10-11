
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Globalization;
using Majorsilence.Reporting.RdlEngine.Resources;
using Majorsilence.Reporting.Rdl;
using System.Threading.Tasks;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Fields referenced dynamically (e.g. Fields(expr) are handled by this class.
	/// </summary>
	[Serializable]
	internal class FunctionFieldCollection : IExpr
	{
		private IDictionary _Fields;
		private IExpr _ArgExpr;

		/// <summary>
		/// obtain value of Field
		/// </summary>
		public FunctionFieldCollection(IDictionary fields, IExpr arg) 
		{
			_Fields = fields;
			_ArgExpr = arg;
		}

		public virtual TypeCode GetTypeCode()
		{
			return TypeCode.Object;		// we don't know the typecode until we run the function
		}

		public virtual Task<bool> IsConstant()
		{
			return Task.FromResult(false);
		}

		public virtual async Task<IExpr> ConstantOptimization()
		{	
			_ArgExpr = await _ArgExpr.ConstantOptimization().ConfigureAwait(false);

			if (await _ArgExpr.IsConstant().ConfigureAwait(false))
			{
				string o = await _ArgExpr.EvaluateString(null, null).ConfigureAwait(false);
				if (o == null)
					throw new Exception(Strings.FunctionFieldCollection_Error_FieldCollectionNull); 
				Field f = _Fields[o] as Field;
				if (f == null)
					throw new Exception(string.Format(Strings.FunctionFieldCollection_Error_FieldCollectionInvalid, o)); 
				return new FunctionField(f);
			}

			return this;
		}

		// 
		public virtual async Task<object> Evaluate(Report rpt, Row row)
		{
			if (row == null)
				return null;
			Field f;
			string field = await _ArgExpr.EvaluateString(rpt, row).ConfigureAwait(false);
			if (field == null)
				return null;
			f = _Fields[field] as Field;
			if (f == null)
				return null;

			object o;
			if (f.Value != null)
				o = await f.Value.Evaluate(rpt, row).ConfigureAwait(false);
			else
				o = row.Data[f.ColumnNumber];

			if (o == DBNull.Value)
				return null;

			if (f.RunType == TypeCode.String && o is char)	// work around; mono odbc driver confuses string and char
				o = Convert.ChangeType(o, TypeCode.String);
			
			return o;
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
			return Convert.ToDecimal(await Evaluate(rpt, row).ConfigureAwait(false), NumberFormatInfo.InvariantInfo);
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
