using System;
using System.Threading.Tasks;
using Majorsilence.Reporting.RdlEngine.Resources;

namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Globals!RenderFormat.Name — the SSRS-style name of the format being rendered.
	/// Empty until a render starts (e.g. during a standalone BuildPages pass).
	/// </summary>
	[Serializable]
	internal class FunctionRenderFormatName : IExpr
	{
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

		public async Task<object> Evaluate(Report rpt, Row row)
		{
			return await EvaluateString(rpt, row);
		}

		public Task<string> EvaluateString(Report rpt, Row row)
		{
			return Task.FromResult(rpt == null ? "" : rpt.RenderFormatName);
		}

		public Task<double> EvaluateDouble(Report rpt, Row row)
		{
			throw new Exception("RenderFormat.Name cannot be converted to a number.");
		}

		public Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			throw new Exception("RenderFormat.Name cannot be converted to a decimal.");
		}

		public Task<int> EvaluateInt32(Report rpt, Row row)
		{
			throw new Exception("RenderFormat.Name cannot be converted to an integer.");
		}

		public Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			throw new Exception("RenderFormat.Name cannot be converted to a date.");
		}

		public Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			throw new Exception("RenderFormat.Name cannot be converted to a boolean.");
		}
	}
}
