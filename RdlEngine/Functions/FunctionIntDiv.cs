
using System;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Integer division operator of form lhs \ rhs (VB's "\", distinct from "/"). As in VB,
	/// each operand is first made a whole number the way CLng does - rounded, halves to the
	/// even number - and the quotient drops its remainder: 7.5 \ 2 is 8 \ 2 = 4, 2.5 \ 1 is 2.
	/// The result is a Long.
	/// </summary>
	[Serializable]
	internal class FunctionIntDiv : FunctionBinary, IExpr
	{
		public FunctionIntDiv()
		{
		}

		public FunctionIntDiv(IExpr lhs, IExpr rhs)
		{
			_lhs = lhs;
			_rhs = rhs;
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.Int64;
		}

		public async Task<IExpr> ConstantOptimization()
		{
			_lhs = await _lhs.ConstantOptimization();
			_rhs = await _rhs.ConstantOptimization();

			// Nothing to fold into (there is no Long constant) and no shortcuts: x \ 1 is not
			// x, since x is rounded, and 0 \ x still fails when x rounds to 0.
			return this;
		}

		// Evaluate is for interpretation (and is relatively slow)
		public async Task<object> Evaluate(Report rpt, Row row)
		{
			return await EvaluateInt64(rpt, row);
		}

		private async Task<long> EvaluateInt64(Report rpt, Row row)
		{
			long lhs = ToLong(await _lhs.EvaluateDouble(rpt, row));
			long rhs = ToLong(await _rhs.EvaluateDouble(rpt, row));

			return lhs / rhs;		// DivideByZeroException when rhs rounds to 0, as in VB
		}

		// CLng: the nearest whole number, halves to even; OverflowException outside Long.
		private static long ToLong(double d)
		{
			return checked((long)Math.Round(d, MidpointRounding.ToEven));
		}

		public async Task<double> EvaluateDouble(Report rpt, Row row)
		{
			return await EvaluateInt64(rpt, row);
		}

		public async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			return await EvaluateInt64(rpt, row);
		}

		public async Task<int> EvaluateInt32(Report rpt, Row row)
		{
			return checked((int)await EvaluateInt64(rpt, row));
		}

		public async Task<string> EvaluateString(Report rpt, Row row)
		{
			long result = await EvaluateInt64(rpt, row);
			return result.ToString();
		}

		public async Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			long result = await EvaluateInt64(rpt, row);
			return Convert.ToDateTime(result);
		}

		public async Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			long result = await EvaluateInt64(rpt, row);
			return Convert.ToBoolean(result);
		}
	}
}
