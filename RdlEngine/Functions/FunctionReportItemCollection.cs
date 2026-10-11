
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
	/// Handles references to the report item collection.
	/// </summary>
	[Serializable]
	internal class FunctionReportItemCollection : IExpr
	{
		private IDictionary _ReportItems;
		private IExpr _ArgExpr;

		/// <summary>
		/// obtain value of Field
		/// </summary>
		public FunctionReportItemCollection(IDictionary reportitems, IExpr arg) 
		{
			_ReportItems = reportitems;
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
					throw new Exception(Strings.FunctionReportItemCollection_Error_ReportItemCollectionNull); 
				Textbox ri = _ReportItems[o] as Textbox;
				if (ri == null)
					throw new Exception(string.Format(Strings.FunctionReportItemCollection_Error_ReportItemCollectionInvalid, o)); 
				return new FunctionTextbox(ri, null);	// no access to unique name
			}

			return this;
		}

		// 
		public virtual async Task<object> Evaluate(Report rpt, Row row)
		{
			if (row == null)
				return null;
			Textbox tb;
			string t = await _ArgExpr.EvaluateString(rpt, row).ConfigureAwait(false);
			if (t == null)
				return null;
			tb = _ReportItems[t] as Textbox;
			if (tb == null)
				return null;

			return await tb.Evaluate(rpt, row).ConfigureAwait(false);
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
