
using System;
using System.IO;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Total Pages
	/// </summary>
	[Serializable]
	internal class FunctionTotalPages : IExpr
	{
		/// <summary>
		/// Total page count; relys on PageHeader, PageFooter to set Report.TotalPages
		/// </summary>
		public FunctionTotalPages() 
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
            return rpt == null ? Task.FromResult((int) 1 as object) : Task.FromResult((int) rpt.TotalPages as object);
		}
		
		public Task<double> EvaluateDouble(Report rpt, Row row)
		{	
			return rpt == null? Task.FromResult(1d): Task.FromResult((double)rpt.TotalPages);
		}

        public Task<int> EvaluateInt32(Report rpt, Row row)
        {
            return rpt == null ? Task.FromResult(1) : Task.FromResult(rpt.TotalPages);
        }
		
		public async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			int result = await EvaluateInt32(rpt, row).ConfigureAwait(false);

			return Convert.ToDecimal(result);
		}

		public async Task<string> EvaluateString(Report rpt, Row row)
		{
			int result = await EvaluateInt32(rpt, row).ConfigureAwait(false);
			return result.ToString();
		}

		public async Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			int result = await EvaluateInt32(rpt, row).ConfigureAwait(false);
			return Convert.ToDateTime(result);
		}

		public async Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			int result = await EvaluateInt32(rpt, row).ConfigureAwait(false);
			return Convert.ToBoolean(result);
		}
		
	}
}
