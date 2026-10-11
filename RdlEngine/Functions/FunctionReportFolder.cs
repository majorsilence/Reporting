
using System;
using System.IO;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Report Folder
	/// </summary>
	[Serializable]
	internal class FunctionReportFolder : IExpr
	{
		/// <summary>
		/// Folder that holds Report
		/// </summary>
		public FunctionReportFolder() 
		{
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.String;
		}

		public Task<bool> IsConstant()
		{
			return Task.FromResult(false);
		}

		public Task<IExpr> ConstantOptimization()
		{	// not a constant expression
			return Task.FromResult(this as IExpr);
		}

		public Task<object> Evaluate(Report rpt, Row row)
		{
			return Task.FromResult(rpt.Folder as object);
		}
		
		public Task<double> EvaluateDouble(Report rpt, Row row)
		{	
			return Task.FromResult(double.NaN);
		}
		
		public Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			return Task.FromResult(Decimal.MinValue);
		}

        public Task<int> EvaluateInt32(Report rpt, Row row)
        {
            return Task.FromResult(int.MinValue);
        }

		public Task<string> EvaluateString(Report rpt, Row row)
		{
			return rpt == null? Task.FromResult(""): Task.FromResult(rpt.Folder);
		}

		public async Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			string result = await EvaluateString(rpt, row).ConfigureAwait(false);
			return Convert.ToDateTime(result);
		}

		public Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			return Task.FromResult(false);
		}
	}
}
