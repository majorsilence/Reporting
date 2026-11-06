
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Shared matching logic for the SSRS Lookup, LookupSet and MultiLookup functions. All
	/// three find the row(s) of a target DataSet whose Destination expression equals a
	/// source value -- evaluated against the calling row, not the target DataSet -- and
	/// return (part of) the Result expression evaluated against the matching row(s).
	/// Destination and Result are parsed by Parser.MatchFuncIDent with Fields! bound to
	/// the target DataSet (see Parser.ResolveLookupDataSet), so they already resolve
	/// correctly when evaluated against one of its rows.
	/// </summary>
	[Serializable]
	internal abstract class FunctionLookupBase
	{
		protected IExpr _Source;
		protected IExpr _Destination;
		protected IExpr _Result;
		protected DataSetDefn _DataSet;

		protected FunctionLookupBase(IExpr source, IExpr destination, IExpr result, DataSetDefn dataSet)
		{
			_Source = source;
			_Destination = destination;
			_Result = result;
			_DataSet = dataSet;
		}

		public Task<bool> IsConstant()
		{
			return Task.FromResult(false);
		}

		public async Task<IExpr> ConstantOptimization()
		{
			_Source = await _Source.ConstantOptimization().ConfigureAwait(false);
			_Destination = await _Destination.ConstantOptimization().ConfigureAwait(false);
			_Result = await _Result.ConstantOptimization().ConfigureAwait(false);
			return (IExpr)this;
		}

		// The target DataSet's rows, or an empty list when the DataSet has no data yet
		// (e.g. it never ran, or ran to zero rows).
		protected async Task<List<Row>> GetTargetRows(Report rpt)
		{
			if (_DataSet == null || _DataSet.Query == null)
				return EmptyRows;

			Rows rows = _DataSet.Query.GetMyData(rpt);
			return rows == null ? EmptyRows : rows.Data;
		}

		private static readonly List<Row> EmptyRows = new List<Row>();

		// The first row of the target DataSet whose Destination equals source (SSRS
		// coerces the comparison by the Destination's declared type, same as the
		// relational = operator -- see FunctionRelopEQ).
		protected async Task<Row> FindFirstMatch(Report rpt, List<Row> targetRows, object source)
		{
			TypeCode tc = _Destination.GetTypeCode();
			foreach (Row candidate in targetRows)
			{
				object dest = await _Destination.Evaluate(rpt, candidate).ConfigureAwait(false);
				if (Filter.ApplyCompare(tc, dest, source) == 0)
					return candidate;
			}
			return null;
		}
	}
}
