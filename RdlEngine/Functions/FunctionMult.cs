
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Mult operator  of form lhs * rhs
	/// </summary>
	[Serializable]
	internal class FunctionMult : FunctionBinary, IExpr
	{

		/// <summary>
		/// Do multiplication on double data types
		/// </summary>
		public FunctionMult() 
		{
			_lhs = null;
			_rhs = null;
		}

		public FunctionMult(IExpr lhs, IExpr rhs) 
		{
			_lhs = lhs;
			_rhs = rhs;
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.Double;
		}

		public async Task<IExpr> ConstantOptimization()
		{
			_lhs = await _lhs.ConstantOptimization().ConfigureAwait(false);
			_rhs = await _rhs.ConstantOptimization().ConfigureAwait(false);
			bool bLeftConst = await _lhs.IsConstant().ConfigureAwait(false);
			bool bRightConst = await _rhs.IsConstant().ConfigureAwait(false);
			if (bLeftConst && bRightConst)
			{
				double d = await EvaluateDouble(null, null).ConfigureAwait(false);
				return new ConstantDouble(d);
			}
			else if (bLeftConst)
			{
				double d = await _lhs.EvaluateDouble(null, null).ConfigureAwait(false);
				if (d == 1)
					return _rhs;
				else if (d == 0)
					return new ConstantDouble(0);
			}
			else if (bRightConst)
			{
				double d = await _rhs.EvaluateDouble(null, null).ConfigureAwait(false);
				if (d == 1)
					return _lhs;
				else if (d == 0)
					return new ConstantDouble(0);
			}

			return this;
		}

		// Evaluate is for interpretation  (and is relatively slow)
		public async Task<object> Evaluate(Report rpt, Row row)
		{
			return await EvaluateDouble(rpt, row).ConfigureAwait(false);
		}
		
		public async Task<double> EvaluateDouble(Report rpt, Row row)
		{
			double lhs = await _lhs.EvaluateDouble(rpt, row).ConfigureAwait(false);
			double rhs = await _rhs.EvaluateDouble(rpt, row).ConfigureAwait(false);

			return lhs*rhs;
		}
		
		public async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			double result = await EvaluateDouble(rpt, row).ConfigureAwait(false);

			return Convert.ToDecimal(result);
		}

        public async Task<int> EvaluateInt32(Report rpt, Row row)
        {
            double result = await EvaluateDouble(rpt, row).ConfigureAwait(false);

            return Convert.ToInt32(result);
        }

		public async Task<string> EvaluateString(Report rpt, Row row)
		{
			double result = await EvaluateDouble(rpt, row).ConfigureAwait(false);
			return result.ToString();
		}

		public async Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			double result = await EvaluateDouble(rpt, row).ConfigureAwait(false);
			return Convert.ToDateTime(result);
		}

		public async Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			double result = await EvaluateDouble(rpt, row).ConfigureAwait(false);
			return Convert.ToBoolean(result);
		}
	}
}
