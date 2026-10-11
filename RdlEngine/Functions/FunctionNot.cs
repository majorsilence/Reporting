
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// NOT operator of the form ! a
	/// </summary>
	[Serializable]
	internal class FunctionNot : IExpr
	{
		IExpr _rhs;

		/// <summary>
		/// NOT operator of the form ! a
		/// </summary>
		public FunctionNot(IExpr rhs) 
		{
			_rhs = rhs;
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.Boolean;
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
				bool b = await EvaluateBoolean(null, null).ConfigureAwait(false);
				return new ConstantBoolean(b);
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
			return !await _rhs.EvaluateBoolean(rpt, row).ConfigureAwait(false);
		}
	}
}
