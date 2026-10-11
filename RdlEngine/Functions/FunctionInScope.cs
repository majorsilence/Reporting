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
	/// In a table the answer depends only on where the expression sits, so it is settled when the
	/// expression is parsed: in a group's header or footer it is inside that group and the outer
	/// ones, in the detail section inside all of them, and in the table's own header or footer
	/// inside none. Anywhere else the answer is true.
	/// </remarks>
	[Serializable]
	internal class FunctionInScope : IExpr
	{
		readonly object _Scope;	// a Grouping when the name matched one; anything else is always in scope
		readonly bool _TableAnswer = true;	// the settled answer when the group belongs to a table

		public FunctionInScope(object scp, NameLookup lookup)
		{
			_Scope = scp;
			if (scp is Grouping g && !g.InMatrix)
				_TableAnswer = AnswerForTable(g, lookup);
		}

		static T Ancestor<T>(ReportLink link) where T : class
		{
			for (ReportLink l = link; l != null; l = l.Parent)
			{
				if (l is T found)
					return found;
			}
			return null;
		}

		static bool AnswerForTable(Grouping target, NameLookup lookup)
		{
			TableGroup targetGroup = Ancestor<TableGroup>(target);
			Table table = lookup?.ContextTable;
			if (targetGroup == null || table == null || Ancestor<Table>(targetGroup) != table)
				return true;			// a list, a details group, or another table: not something we can place
			if (lookup.InTableDetails)
				return true;			// the detail rows are inside every group of the table
			TableGroup here = Ancestor<TableGroup>(lookup.LookupGrouping());
			if (here == null)
				return false;			// the table's own header or footer is outside every group

			var groups = table.TableGroups?.Items;
			if (groups == null)
				return true;
			int target_ = groups.IndexOf(targetGroup);
			int context = groups.IndexOf(here);
			return target_ < 0 || context < 0 || target_ <= context;
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
			return Task.FromResult(_TableAnswer);
		}

		public async Task<object> Evaluate(Report rpt, Row row)
		{
			return await EvaluateBoolean(rpt, row).ConfigureAwait(false);
		}

		public async Task<double> EvaluateDouble(Report rpt, Row row)
		{
			return await EvaluateBoolean(rpt, row).ConfigureAwait(false) ? 1 : 0;
		}

		public async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			return await EvaluateBoolean(rpt, row).ConfigureAwait(false) ? 1m : 0m;
		}

		public async Task<int> EvaluateInt32(Report rpt, Row row)
		{
			return await EvaluateBoolean(rpt, row).ConfigureAwait(false) ? 1 : 0;
		}

		public async Task<string> EvaluateString(Report rpt, Row row)
		{
			return await EvaluateBoolean(rpt, row).ConfigureAwait(false) ? "True" : "False";
		}

		public Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			return Task.FromResult(DateTime.MinValue);
		}
	}
}
