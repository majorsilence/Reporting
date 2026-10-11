
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Unary minus operator with a decimal operand
	/// </summary>
	[Serializable]
	internal class FunctionUnaryMinusDecimal : IExpr
	{
		IExpr _rhs;			// rhs

		/// <summary>
		/// Do minus on decimal data type
		/// </summary>
		public FunctionUnaryMinusDecimal() 
		{
			_rhs = null;
		}

		public FunctionUnaryMinusDecimal(IExpr r) 
		{
			_rhs = r;
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.Decimal;
		}

		public async Task<bool> IsConstant()
		{
			return await _rhs.IsConstant().ConfigureAwait(false);
		}

		public async Task<IExpr> ConstantOptimization()
		{
			_rhs = await _rhs.ConstantOptimization().ConfigureAwait(false);
			if (await _rhs.IsConstant().ConfigureAwait(false))
			{
				decimal d = await EvaluateDecimal(null, null).ConfigureAwait(false);
				return new ConstantDecimal(d);
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
			decimal rhs = await _rhs.EvaluateDecimal(rpt, row).ConfigureAwait(false);

			return (decimal) (-rhs);
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

		public Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			return Task.FromResult(false);
		}

		public IExpr Rhs
		{
			get { return  _rhs; }
			set {  _rhs = value; }
		}

	}
}
