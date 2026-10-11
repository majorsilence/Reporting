
using System;
using System.IO;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Page number operator.   Relies on render to set the proper page #.
	/// </summary>
	[Serializable]
	internal class FunctionPageNumber : IExpr
	{
		/// <summary>
		/// Current page number
		/// </summary>
		public FunctionPageNumber() 
		{
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.Int32;
		}

		public Task<bool> IsConstant()
		{
			return Task.FromResult(false);
		}

		public Task<IExpr> ConstantOptimization()
		{	// not a constant expression
			return Task.FromResult(this as IExpr);
		}

		// Evaluate is for interpretation  
		public Task<object> Evaluate(Report rpt, Row row)
		{
            return rpt == null ? Task.FromResult((int) 0 as object) : Task.FromResult((int) rpt.PageNumber as object);
		}
		
		public Task<double> EvaluateDouble(Report rpt, Row row)
		{	
			return rpt == null? Task.FromResult(0d): Task.FromResult((double)rpt.PageNumber);
		}

        public Task<int> EvaluateInt32(Report rpt, Row row)
        {
            return rpt == null ? Task.FromResult(0) : Task.FromResult(rpt.PageNumber);
        }
		
		public async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			double result = await EvaluateDouble(rpt, row).ConfigureAwait(false);

			return Convert.ToDecimal(result);
		}

		public async Task<string> EvaluateString(Report rpt, Row row)
		{
			double result = await EvaluateDouble(rpt, row).ConfigureAwait(false);
			return result.ToString();
		}

		public async Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			double result = await EvaluateDouble(rpt, row).ConfigureAwait(false);
			return Convert.ToDateTime(result);
		}

		public Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			return Task.FromResult(false);
		}
	}
}
