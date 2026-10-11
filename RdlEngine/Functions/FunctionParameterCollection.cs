
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
	/// Parameters referenced via the collection.  For example, Parameters(expr)
	/// </summary>
	[Serializable]
	internal class FunctionParameterCollection : IExpr
	{
		private IDictionary _Parameters;
		private IExpr _ArgExpr;

		/// <summary>
		/// obtain value of Field
		/// </summary>
		public FunctionParameterCollection(IDictionary parameters, IExpr arg) 
		{
			_Parameters = parameters;
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
					throw new Exception(Strings.FunctionParameterCollection_Error_ParameterCollectionNull); 
				ReportParameter rp = _Parameters[o] as ReportParameter;
				if (rp == null)
					throw new Exception(string.Format(Strings.FunctionParameterCollection_Error_ParameterCollectionInvalid, o)); 
				return new FunctionReportParameter(rp);
			}

			return this;
		}

		// 
		public virtual async Task<object> Evaluate(Report rpt, Row row)
		{
			string o = await _ArgExpr.EvaluateString(rpt, row).ConfigureAwait(false);
			if (o == null)
				return null; 
			ReportParameter rp = _Parameters[o] as ReportParameter;
			if (rp == null)
                return null;

			return rp.GetRuntimeValue(rpt);
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
