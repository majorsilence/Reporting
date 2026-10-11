
using System;
using System.IO;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// User ID- Report.UserID must be set by the client to be accurate in multi-user case
	/// </summary>
	[Serializable]
	internal class FunctionUserID : IExpr
	{
		/// <summary>
		/// Client user id
		/// </summary>
		public FunctionUserID() 
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
		{	
			return Task.FromResult(this as IExpr);
		}

		// Evaluate is for interpretation  
		public async Task<object> Evaluate(Report rpt, Row row)
		{
			return await EvaluateString(rpt, row).ConfigureAwait(false);
		}
		
		public async Task<double> EvaluateDouble(Report rpt, Row row)
		{	
			string result = await EvaluateString(rpt, row).ConfigureAwait(false);
			return Convert.ToDouble(result);		
		}
		
		public async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			string result = await EvaluateString(rpt, row).ConfigureAwait(false);

			return Convert.ToDecimal(result);
		}

        public async Task<int> EvaluateInt32(Report rpt, Row row)
        {
            string result = await EvaluateString(rpt, row).ConfigureAwait(false);

            return Convert.ToInt32(result);
        }
		public Task<string> EvaluateString(Report rpt, Row row)
		{
			if (rpt == null || rpt.UserID == null)
				return Task.FromResult(Environment.UserName);
			else
				return Task.FromResult(rpt.UserID);
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
