
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Plus operator  of form lhs + rhs where both operands are decimal
	/// </summary>
	[Serializable]
	internal class FunctionPlusInt32 : FunctionBinary , IExpr
	{
		/// <summary>
		/// Do plus on decimal data types
		/// </summary>
		public FunctionPlusInt32() 
		{
		}

		public FunctionPlusInt32(IExpr lhs, IExpr rhs) 
		{
			_lhs = lhs;
			_rhs = rhs;
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.Int32;
		}

		public async Task<IExpr> ConstantOptimization()
		{
			_lhs = await _lhs.ConstantOptimization().ConfigureAwait(false);
			_rhs = await _rhs.ConstantOptimization().ConfigureAwait(false);
			bool bLeftConst = await _lhs.IsConstant().ConfigureAwait(false);
			bool bRightConst = await _rhs.IsConstant().ConfigureAwait(false);
			if (bLeftConst && bRightConst)
			{
				int d = await EvaluateInt32(null, null).ConfigureAwait(false);
				return new ConstantInteger(d);
			}
			else if (bRightConst)
			{
				int d = await _rhs.EvaluateInt32(null, null).ConfigureAwait(false);
				if (d == 0)
					return _lhs;
			}
			else if (bLeftConst)
			{
				int d = await _lhs.EvaluateInt32(null, null).ConfigureAwait(false);
				if (d == 0)
					return _rhs;
			}

			return this;
		}

		// Evaluate is for interpretation  (and is relatively slow)
		public async Task<object> Evaluate(Report rpt, Row row)
		{
			return await EvaluateInt32(rpt, row).ConfigureAwait(false);
		}
		
		public async Task<double> EvaluateDouble(Report rpt, Row row)
		{
			int result = await EvaluateInt32(rpt, row).ConfigureAwait(false);

			return Convert.ToDouble(result);
		}

        public async Task<decimal> EvaluateDecimal(Report rpt, Row row)
        {
            int result = await EvaluateInt32(rpt, row).ConfigureAwait(false);

            return Convert.ToDecimal(result);
        }

        public async Task<int> EvaluateInt32(Report rpt, Row row)
		{
			int lhs = await _lhs.EvaluateInt32(rpt, row).ConfigureAwait(false);
			int rhs = await _rhs.EvaluateInt32(rpt, row).ConfigureAwait(false);

			return (lhs+rhs);
		}

		public async Task<string> EvaluateString(Report rpt, Row row)
		{
			int result = await EvaluateInt32(rpt, row).ConfigureAwait(false);
			return result.ToString();
		}

		public async Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			int result = await EvaluateInt32(rpt, row).ConfigureAwait(false);
			return Convert.ToDateTime(result);
		}

		public async Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			int result = await EvaluateInt32(rpt, row).ConfigureAwait(false);
			return Convert.ToBoolean(result);
		}
	}
}
