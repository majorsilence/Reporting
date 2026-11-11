using System;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// DataSets!Name.CommandText (or DataSets("Name").CommandText): the dataset's query text,
	/// evaluated when it is an expression. RewrittenCommandText, the text SSRS sends after its
	/// rewriting, is the same here: the engine passes the command text through as it is.
	/// </summary>
	/// <remarks>SSMS custom reports, for example, show their own SQL with it.</remarks>
	[Serializable]
	internal class FunctionDataSetCommandText : IExpr
	{
		readonly DataSetDefn _DataSet;

		public FunctionDataSetCommandText(DataSetDefn ds)
		{
			_DataSet = ds;
		}

		public TypeCode GetTypeCode() => TypeCode.String;

		public Task<bool> IsConstant() => Task.FromResult(false);

		public Task<IExpr> ConstantOptimization() => Task.FromResult(this as IExpr);

		public async Task<object> Evaluate(Report rpt, Row row) => await EvaluateString(rpt, row).ConfigureAwait(false);

		public Task<double> EvaluateDouble(Report rpt, Row row) => Task.FromResult(double.NaN);

		public Task<decimal> EvaluateDecimal(Report rpt, Row row) => Task.FromResult(decimal.MinValue);

		public Task<int> EvaluateInt32(Report rpt, Row row) => Task.FromResult(int.MinValue);

		public async Task<string> EvaluateString(Report rpt, Row row)
		{
			Expression commandText = _DataSet.Query?.CommandText;
			if (commandText == null)
				return "";
			return await commandText.EvaluateString(rpt, row).ConfigureAwait(false) ?? "";
		}

		public Task<DateTime> EvaluateDateTime(Report rpt, Row row) => Task.FromResult(DateTime.MinValue);

		public Task<bool> EvaluateBoolean(Report rpt, Row row) => Task.FromResult(false);
	}
}
