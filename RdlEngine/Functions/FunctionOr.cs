
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// OR operator  of form lhs || rhs
	/// </summary>
	[Serializable]
	internal class FunctionOr : FunctionBinary, IExpr
	{

		/// <summary>
		/// Or two boolean expressions together of the form a || b
		/// </summary>
		public FunctionOr(IExpr lhs, IExpr rhs) 
		{
			_lhs = lhs;
			_rhs = rhs;
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.Boolean;
		}

		public async Task<IExpr> ConstantOptimization()
		{
			_lhs = await _lhs.ConstantOptimization().ConfigureAwait(false);
			_rhs = await _rhs.ConstantOptimization().ConfigureAwait(false);
			bool bLeftConst = await _lhs.IsConstant().ConfigureAwait(false);
			bool bRightConst = await _rhs.IsConstant().ConfigureAwait(false);
			if (bLeftConst && bRightConst)
			{
				bool b = await EvaluateBoolean(null, null).ConfigureAwait(false);
				return new ConstantBoolean(b);
			}
			else if (bRightConst)
			{
				bool b = await _rhs.EvaluateBoolean(null, null).ConfigureAwait(false);
				if (b)
					return new ConstantBoolean(true);
				else 
					return _lhs;
			}
			else if (bLeftConst)
			{
				bool b = await _lhs.EvaluateBoolean(null, null).ConfigureAwait(false);
				if (b)
					return new ConstantBoolean(true);
				else
					return _rhs;
			}

			return this;
		}

		// Evaluate is for interpretation  (and is relatively slow)
		public async Task<object> Evaluate(Report rpt, Row row)
		{
			return await EvaluateBoolean(rpt, row).ConfigureAwait(false);
		}
		
		public Task<double> EvaluateDouble(Report rpt, Row row)
		{
			return Task.FromResult(Double.NaN);
		}
		
		public Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			return Task.FromResult(decimal.MinValue);
		}

        public Task<int> EvaluateInt32(Report rpt, Row row)
        {
            return Task.FromResult(int.MinValue);
        }

		public async Task<string> EvaluateString(Report rpt, Row row)
		{
			bool result = await EvaluateBoolean(rpt, row).ConfigureAwait(false);
			return result.ToString();
		}

		public Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			return Task.FromResult(DateTime.MinValue);
		}

		public async Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			bool r = await _lhs.EvaluateBoolean(rpt, row).ConfigureAwait(false);
			if (r)
				return true;
			return await _rhs.EvaluateBoolean(rpt, row).ConfigureAwait(false);
		}
	}
}
