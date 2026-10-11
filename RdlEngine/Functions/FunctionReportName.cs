
using System;
using System.IO;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Report name
	/// </summary>
	[Serializable]
	internal class FunctionReportName : IExpr
	{
		/// <summary>
		/// Current page number
		/// </summary>
		public FunctionReportName() 
		{
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.String;
		}

		public Task<bool> IsConstant()
		{
			return Task.FromResult(true);
		}

		public Task<IExpr> ConstantOptimization()
		{	// not a constant expression
			return Task.FromResult(this as IExpr);
		}

		// Evaluate is for interpretation  
		public async Task<object> Evaluate(Report rpt, Row row)
		{
			return await EvaluateString(rpt, row).ConfigureAwait(false);
		}
		
		public Task<double> EvaluateDouble(Report rpt, Row row)
		{	
			return Task.FromResult(double.NaN);
		}
		
		public Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			return Task.FromResult(decimal.MinValue);
		}

        public Task<int> EvaluateInt32(Report rpt, Row row)
        {
            return Task.FromResult(int.MinValue);
        }

		public Task<string> EvaluateString(Report rpt, Row row)
		{
			return rpt == null? Task.FromResult(""): Task.FromResult(rpt.Name);
		}

		public Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			return Task.FromResult(DateTime.MinValue);
		}

		public Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			return Task.FromResult(false);
		}
	}
}
