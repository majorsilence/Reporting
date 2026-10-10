using System;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// InScope(scope): true when the item being evaluated is within the named group.
	/// </summary>
	/// <remarks>
	/// In a matrix, a data cell is in the scope of a group when it lies inside one instance of that
	/// group, and a subtotal cell, which totals over every instance, is not. That is how reports
	/// format totals differently from detail, e.g. =IIf(InScope("Year"), "Normal", "Bold").
	/// Outside a matrix the answer is always true, which is right for the detail rows of a table or
	/// list but not for a group footer asking about an inner group.
	/// </remarks>
	[Serializable]
	internal class FunctionInScope : IExpr
	{
		readonly object _Scope;	// a Grouping when the name matched one; anything else is always in scope

		public FunctionInScope(object scp)
		{
			_Scope = scp;
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.Boolean;
		}

		public Task<bool> IsConstant()
		{
			return Task.FromResult(false);
		}

		public Task<IExpr> ConstantOptimization()
		{
			return Task.FromResult(this as IExpr);	// depends on the cell being rendered
		}

		public Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			if (_Scope is Grouping g && g.InMatrix)
				return Task.FromResult(g.GetInScope(rpt));
			return Task.FromResult(true);
		}

		public async Task<object> Evaluate(Report rpt, Row row)
		{
			return await EvaluateBoolean(rpt, row);
		}

		public async Task<double> EvaluateDouble(Report rpt, Row row)
		{
			return await EvaluateBoolean(rpt, row) ? 1 : 0;
		}

		public async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			return await EvaluateBoolean(rpt, row) ? 1m : 0m;
		}

		public async Task<int> EvaluateInt32(Report rpt, Row row)
		{
			return await EvaluateBoolean(rpt, row) ? 1 : 0;
		}

		public async Task<string> EvaluateString(Report rpt, Row row)
		{
			return await EvaluateBoolean(rpt, row) ? "True" : "False";
		}

		public Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			return Task.FromResult(DateTime.MinValue);
		}
	}
}
