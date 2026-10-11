
using System;
using System.IO;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// DateTime Report started; actually the time that data is retrieved
	/// </summary>
	[Serializable]
	internal class FunctionExecutionTime : IExpr
	{
		/// <summary>
		/// DateTime report started
		/// </summary>
		public FunctionExecutionTime() 
		{
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.DateTime;
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
		public async Task<object> Evaluate(Report rpt, Row row)
		{
			return await EvaluateDateTime(rpt, row).ConfigureAwait(false);
		}
		
		public async Task<double> EvaluateDouble(Report rpt, Row row)
		{	
			DateTime result = await EvaluateDateTime(rpt, row).ConfigureAwait(false);
			return Convert.ToDouble(result);
		}
		
		public async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			DateTime result = await EvaluateDateTime(rpt, row).ConfigureAwait(false);

			return Convert.ToDecimal(result);
		}

        public async Task<int> EvaluateInt32(Report rpt, Row row)
        {
            DateTime result = await EvaluateDateTime(rpt, row).ConfigureAwait(false);

            return Convert.ToInt32(result);
        }

		public async Task<string> EvaluateString(Report rpt, Row row)
		{
			DateTime result = await EvaluateDateTime(rpt, row).ConfigureAwait(false);
			return result.ToString();
		}

		public Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			return Task.FromResult(rpt.ExecutionTime);
		}
		
		public Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			return Task.FromResult(false);
		}
	}
}
