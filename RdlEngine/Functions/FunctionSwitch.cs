
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// switch function like this example: Switch(a=1, "a1", a=2, "a2", true, "other")
	/// </summary>
	[Serializable]
	internal class FunctionSwitch : IExpr
	{
		IExpr[] _expr;		// boolean expression
		TypeCode _tc;

		/// <summary>
		/// Switch function.  Evaluates boolean expression in order and returns result of
		/// the first true.  For example, Switch(a=1, "a1", a=2, "a2", true, "other")
		/// </summary>
		public FunctionSwitch(IExpr[] expr) 
		{
			_expr = expr;
			_tc = _expr[1].GetTypeCode();
		}

		public TypeCode GetTypeCode()
		{
			return _tc;
		}

		public Task<bool> IsConstant()
		{
			return Task.FromResult(false);		// we could be more sophisticated here; but not much benefit
		}

		public async Task<IExpr> ConstantOptimization()
		{
			// simplify all expression if possible
			for (int i=0; i < _expr.Length; i++)
			{
				_expr[i] = await _expr[i].ConstantOptimization().ConfigureAwait(false);
			}

			return this;
		}

		// Evaluate is for interpretation  (and is relatively slow)
		public async Task<object> Evaluate(Report rpt, Row row)
		{
			bool result;
			for (int i=0; i < _expr.Length; i = i+2)
			{
				result = await _expr[i].EvaluateBoolean(rpt, row).ConfigureAwait(false);
				if (result)
				{
					object o = await _expr[i+1].Evaluate(rpt, row).ConfigureAwait(false);
					// We may need to convert to same type as first type
					if (i == 0 || _tc == _expr[i+1].GetTypeCode())	// first typecode will always match 
						return o;

					return Convert.ChangeType(o, _tc);
				}
			}

			return null;
		}

		public async Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			object result = await Evaluate(rpt, row).ConfigureAwait(false);
			return Convert.ToBoolean(result);
		}
		
		public async Task<double> EvaluateDouble(Report rpt, Row row)
		{
			object result = await Evaluate(rpt, row).ConfigureAwait(false);
			return Convert.ToDouble(result);
		}
		
		public async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			object result = await Evaluate(rpt, row).ConfigureAwait(false);
			return Convert.ToDecimal(result);
		}

        public async Task<int> EvaluateInt32(Report rpt, Row row)
        {
            object result = await Evaluate(rpt, row).ConfigureAwait(false);
            return Convert.ToInt32(result);
        }

        public async Task<string> EvaluateString(Report rpt, Row row)
		{
			object result = await Evaluate(rpt, row).ConfigureAwait(false);
			return Convert.ToString(result);
		}

		public async Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			object result = await Evaluate(rpt, row).ConfigureAwait(false);
			return Convert.ToDateTime(result);
		}
	}
}
