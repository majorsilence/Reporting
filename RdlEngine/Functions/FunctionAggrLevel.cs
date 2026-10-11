
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Aggregate function: Level
	/// </summary>
	[Serializable]
	internal class FunctionAggrLevel : FunctionAggr, IExpr
	{
		/// <summary>
		/// Aggregate function: Level
		/// 
		///	Return type is double
		/// </summary>
		public FunctionAggrLevel(object scp):base(null, scp) 
		{
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.Double;		// although it is always an integer
		}

		public async Task<object> Evaluate(Report rpt, Row row)
		{
			return (object)await EvaluateDouble(rpt, row).ConfigureAwait(false);
		}
		
		public Task<double> EvaluateDouble(Report rpt, Row row)
		{
			if (row == null || this._Scope == null)
				return Task.FromResult(0d);

			Grouping g = this._Scope as Grouping;
			if (g == null || g.ParentGroup == null)
				return Task.FromResult(0d);

//			GroupEntry ge = row.R.CurrentGroups[g.Index];	// current group entry

			return Task.FromResult((double)row.Level);
		}
		
		public async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			double d = await EvaluateDouble(rpt, row).ConfigureAwait(false);

			return Convert.ToDecimal(d);
		}

        public async Task<int> EvaluateInt32(Report rpt, Row row)
        {
            double d = await EvaluateDouble(rpt, row).ConfigureAwait(false);

            return Convert.ToInt32(d);
        }

		public async Task<string> EvaluateString(Report rpt, Row row)
		{
			double result = await EvaluateDouble(rpt, row).ConfigureAwait(false);
			return Convert.ToString(result);
		}

		public async Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			object result = await Evaluate(rpt, row).ConfigureAwait(false);
			return Convert.ToDateTime(result);
		}
	}
}
