
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Unary minus operator for a integer operand
	/// </summary>
	[Serializable]
	internal class FunctionUnaryMinusInteger : IExpr
	{
		IExpr _rhs;			// rhs

		/// <summary>
		/// Do minus on decimal data type
		/// </summary>
		public FunctionUnaryMinusInteger() 
		{
			_rhs = null;
		}

		public FunctionUnaryMinusInteger(IExpr r) 
		{
			_rhs = r;
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.Int32;
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
				double d = await EvaluateDouble(null, null).ConfigureAwait(false);
				return new ConstantInteger((int) d);
			}

			return this;
		}

		// Evaluate is for interpretation  (and is relatively slow)
		public async Task<object> Evaluate(Report rpt, Row row)
		{
			return (int)await EvaluateInt32(rpt, row).ConfigureAwait(false);
		}
		
		public async Task<double> EvaluateDouble(Report rpt, Row row)
		{
			double result = await _rhs.EvaluateDouble(rpt, row).ConfigureAwait(false);

			return -result;
		}

        public async Task<int> EvaluateInt32(Report rpt, Row row)
        {
            int result = await _rhs.EvaluateInt32(rpt, row).ConfigureAwait(false);

            return -result;
        }
		
		public async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			int result = await EvaluateInt32(rpt, row).ConfigureAwait(false);

			return Convert.ToDecimal(result);
		}

		public async Task<string> EvaluateString(Report rpt, Row row)
		{
			int result = (int)await EvaluateDouble(rpt, row).ConfigureAwait(false);
			return result.ToString();
		}

		public async Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			int result = (int)await EvaluateDouble(rpt, row).ConfigureAwait(false);
			return Convert.ToDateTime(result);
		}

		public async Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			int result = (int)await EvaluateDouble(rpt, row).ConfigureAwait(false);
			return result == 0? false:true;
		}

		public IExpr Rhs
		{
			get { return  _rhs; }
			set {  _rhs = value; }
		}

	}
}
