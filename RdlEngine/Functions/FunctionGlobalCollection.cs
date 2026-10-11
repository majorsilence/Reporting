
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
	/// Global fields accessed dynamically. i.e. Globals(expr).
	/// </summary>
	[Serializable]
	internal class FunctionGlobalCollection : IExpr
	{
		private IDictionary _Globals;
		private IExpr _ArgExpr;

		/// <summary>
		/// obtain value of Field
		/// </summary>
		public FunctionGlobalCollection(IDictionary globals, IExpr arg) 
		{
			_Globals = globals;
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
					throw new Exception(Strings.FunctionGlobalCollection_Error_GlobalsNull); 
				switch (o.ToLower())
				{
					case "pagenumber":
						return new FunctionPageNumber();
					case "totalpages":
						return new FunctionTotalPages();
					case "executiontime":
						return new FunctionExecutionTime();
					case "reportfolder":
						return new FunctionReportFolder();
					case "reportname":
						return new FunctionReportName();
					default:
						throw new Exception(string.Format(Strings.FunctionGlobalCollection_Error_GlobalsUnknown, o)); 
				}
			}

			return this;
		}

		// 
		public virtual async Task<object> Evaluate(Report rpt, Row row)
		{
			if (rpt == null)
				return null;

			string g = await _ArgExpr.EvaluateString(rpt, row).ConfigureAwait(false);
			if (g == null)
				return null;

			switch (g.ToLower())
			{
				case "pagenumber":
					return rpt.PageNumber;
				case "totalpages":
					return rpt.TotalPages;
				case "executiontime":
					return rpt.ExecutionTime;
				case "reportfolder":
					return rpt.Folder;
				case "reportname":
					return rpt.Name;
				default:
					return null;
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
