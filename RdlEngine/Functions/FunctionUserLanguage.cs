
using System;
using System.IO;
using System.Globalization;
using Majorsilence.Reporting.RdlEngine.Resources;
using Majorsilence.Reporting.Rdl;
using System.Threading.Tasks;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// The Language field in the User collection.
	/// </summary>
	[Serializable]
	internal class FunctionUserLanguage : IExpr
	{
		/// <summary>
		/// Client user language
		/// </summary>
		public FunctionUserLanguage() 
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
			return await EvaluateString(rpt, row);
		}
		
		public Task<double> EvaluateDouble(Report rpt, Row row)
		{	
			throw new Exception(Strings.FunctionUserLanguage_Error_ConvertToDouble);
		}
		
		public Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			throw new Exception(Strings.FunctionUserLanguage_Error_ConvertToDecimal);
		}

        public Task<int> EvaluateInt32(Report rpt, Row row)
        {
            throw new Exception(Strings.FunctionUserLanguage_Error_ConvertToInt32);
        }
		public Task<string> EvaluateString(Report rpt, Row row)
		{
			// Read the raw host-supplied language, never Report.ClientLanguage: that getter
			// falls back to evaluating the report's Language expression, and when Language is
			// "=User!Language" (a stock Report Builder default) the two recurse forever.
			if (rpt == null || rpt.ClientLanguageRaw == null)
				return Task.FromResult(CultureInfo.CurrentCulture.Name);
			else
				return Task.FromResult(rpt.ClientLanguageRaw);
		}

		public Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			throw new Exception(Strings.FunctionUserLanguage_Error_ConvertToDateTime);
		}

		public Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			throw new Exception(Strings.FunctionUserLanguage_Error_ConvertToBoolean);
		}
	}
}
