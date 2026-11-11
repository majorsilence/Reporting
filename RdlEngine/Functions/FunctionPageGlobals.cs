using System;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// A report global read from the report at run time: the base for Globals!OverallPageNumber,
	/// Globals!OverallTotalPages and Globals!PageName (RDL 2008 R2 and later).
	/// </summary>
	[Serializable]
	internal abstract class FunctionReportGlobal : IExpr
	{
		protected abstract object Value(Report rpt);

		public abstract TypeCode GetTypeCode();

		public Task<bool> IsConstant() => Task.FromResult(false);

		public Task<IExpr> ConstantOptimization() => Task.FromResult<IExpr>(this);

		public Task<object> Evaluate(Report rpt, Row row) => Task.FromResult(Value(rpt));

		public Task<double> EvaluateDouble(Report rpt, Row row) => Task.FromResult(Convert.ToDouble(Value(rpt) ?? double.NaN));

		public Task<decimal> EvaluateDecimal(Report rpt, Row row) => Task.FromResult(Convert.ToDecimal(Value(rpt) ?? 0));

		public Task<int> EvaluateInt32(Report rpt, Row row) => Task.FromResult(Convert.ToInt32(Value(rpt) ?? 0));

		public Task<string> EvaluateString(Report rpt, Row row) => Task.FromResult(Value(rpt)?.ToString());

		public Task<DateTime> EvaluateDateTime(Report rpt, Row row) => Task.FromResult(DateTime.MinValue);

		public Task<bool> EvaluateBoolean(Report rpt, Row row) => Task.FromResult(false);
	}

	/// <summary>The page's number counted over the whole report, not restarting at a ResetPageNumber break.</summary>
	[Serializable]
	internal sealed class FunctionOverallPageNumber : FunctionReportGlobal
	{
		protected override object Value(Report rpt) => rpt == null ? 1 : rpt.OverallPageNumber;
		public override TypeCode GetTypeCode() => TypeCode.Int32;
	}

	/// <summary>The number of pages in the whole report.</summary>
	[Serializable]
	internal sealed class FunctionOverallTotalPages : FunctionReportGlobal
	{
		protected override object Value(Report rpt) => rpt == null ? 1 : rpt.OverallTotalPages;
		public override TypeCode GetTypeCode() => TypeCode.Int32;
	}

	/// <summary>The name of the current page (PageName on a data region, rectangle or group).</summary>
	[Serializable]
	internal sealed class FunctionPageName : FunctionReportGlobal
	{
		protected override object Value(Report rpt) => rpt?.PageName ?? string.Empty;
		public override TypeCode GetTypeCode() => TypeCode.String;
	}
}
