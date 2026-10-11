
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Division operator  of form lhs / rhs where both arguments are decimal.
	/// </summary>
	[Serializable]
	internal class FunctionDivDecimal : FunctionBinary, IExpr
	{
		/// <summary>
		/// Do division on decimal data types
		/// </summary>
		public FunctionDivDecimal() 
		{
			_lhs = null;
			_rhs = null;
		}

		public FunctionDivDecimal(IExpr lhs, IExpr rhs) 
		{
			_lhs = lhs;
			_rhs = rhs;
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.Decimal;
		}

		public async Task<IExpr> ConstantOptimization()
		{
			_lhs = await _lhs.ConstantOptimization().ConfigureAwait(false);
			_rhs = await _rhs.ConstantOptimization().ConfigureAwait(false);
			bool bLeftConst = await _lhs.IsConstant().ConfigureAwait(false);
			bool bRightConst = await _rhs.IsConstant().ConfigureAwait(false);
			if (bLeftConst && bRightConst)
			{
				decimal d = await EvaluateDecimal(null, null).ConfigureAwait(false);
				return new ConstantDecimal(d);
			}
			else if (bRightConst)
			{
				decimal d = await _rhs.EvaluateDecimal(null, null).ConfigureAwait(false);
				if (d == 1m)
					return _lhs;
			}
			else if (bLeftConst)
			{
				decimal d = await _lhs.EvaluateDecimal(null, null).ConfigureAwait(false);
				if (d == 0)
					return new ConstantDecimal(0m);
			}

			return this;
		}

		// Evaluate is for interpretation  (and is relatively slow)
		public async Task<object> Evaluate(Report rpt, Row row)
		{
			return await EvaluateDecimal(rpt, row).ConfigureAwait(false);
		}
		
		public async Task<double> EvaluateDouble(Report rpt, Row row)
		{
			decimal result = await EvaluateDecimal(rpt, row).ConfigureAwait(false);

			return Convert.ToDouble(result);
		}

        public async Task<int> EvaluateInt32(Report rpt, Row row)
        {
            decimal result = await EvaluateDecimal(rpt, row).ConfigureAwait(false);

            return Convert.ToInt32(result);
        }
		
		public async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			decimal lhs = await _lhs.EvaluateDecimal(rpt, row).ConfigureAwait(false);
			decimal rhs = await _rhs.EvaluateDecimal(rpt, row).ConfigureAwait(false);

			return (decimal) (lhs/rhs);
		}

		public async Task<string> EvaluateString(Report rpt, Row row)
		{
			decimal result = await EvaluateDecimal(rpt, row).ConfigureAwait(false);
			return result.ToString();
		}

		public async Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			decimal result = await EvaluateDecimal(rpt, row).ConfigureAwait(false);
			return Convert.ToDateTime(result);
		}

		public async Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			double result = await EvaluateDouble(rpt, row).ConfigureAwait(false);
			return Convert.ToBoolean(result);
		}
	}
}
