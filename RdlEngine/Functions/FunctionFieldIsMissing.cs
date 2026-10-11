
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// IsMissing attribute
	/// </summary>
	[Serializable]
	internal class FunctionFieldIsMissing : FunctionField
	{
		/// <summary>
		/// Determine if value of Field is available
		/// </summary>
		public FunctionFieldIsMissing(Field fld) : base(fld)
		{
		}
		public FunctionFieldIsMissing(string method) : base(method)
		{
		}

		public override TypeCode GetTypeCode()
		{
			return TypeCode.Boolean;
		}

		public override Task<bool> IsConstant()
		{
			return Task.FromResult(false);
		}

		public override Task<IExpr> ConstantOptimization()
		{	
			return Task.FromResult(this as IExpr);	// not a constant
		}

		// 
		public override async Task<object> Evaluate(Report rpt, Row row)
		{
			return await EvaluateBoolean(rpt, row).ConfigureAwait(false);
		}
		
		public override async Task<double> EvaluateDouble(Report rpt, Row row)
		{
			return await EvaluateBoolean(rpt, row).ConfigureAwait(false) ? 1: 0;
		}
		
		public override async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			return await EvaluateBoolean(rpt, row).ConfigureAwait(false) ? 1m: 0m;
		}

		public override async Task<string> EvaluateString(Report rpt, Row row)
		{
			return await EvaluateBoolean(rpt, row).ConfigureAwait(false) ? "True": "False";
		}

		public override Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			return Task.FromResult(DateTime.MinValue);
		}

		public override async Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			object o = await base.Evaluate(rpt, row).ConfigureAwait(false);
			if(o is double)
				return double.IsNaN((double)o) ? true : false;
			else
				return o == null? true: false;
		}
	}
}
