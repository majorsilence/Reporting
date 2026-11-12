using System;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Globals!ReportServerUrl: the URL of the report server the report runs on. There is none
	/// outside SSRS, so it is whatever the host sets in Report.ReportServerUrl, empty by default;
	/// reports use it to build links, e.g. Replace(Globals!ReportServerUrl, "/ReportServer", "").
	/// </summary>
	[Serializable]
	internal class FunctionReportServerUrl : IExpr
	{
		public TypeCode GetTypeCode() => TypeCode.String;

		public Task<bool> IsConstant() => Task.FromResult(false);

		public Task<IExpr> ConstantOptimization() => Task.FromResult(this as IExpr);

		public Task<object> Evaluate(Report rpt, Row row) => Task.FromResult<object>(rpt?.ReportServerUrl ?? "");

		public Task<double> EvaluateDouble(Report rpt, Row row) => Task.FromResult(double.NaN);

		public Task<decimal> EvaluateDecimal(Report rpt, Row row) => Task.FromResult(decimal.MinValue);

		public Task<int> EvaluateInt32(Report rpt, Row row) => Task.FromResult(int.MinValue);

		public Task<string> EvaluateString(Report rpt, Row row) => Task.FromResult(rpt?.ReportServerUrl ?? "");

		public Task<DateTime> EvaluateDateTime(Report rpt, Row row) => Task.FromResult(DateTime.MinValue);

		public Task<bool> EvaluateBoolean(Report rpt, Row row) => Task.FromResult(false);
	}
}
