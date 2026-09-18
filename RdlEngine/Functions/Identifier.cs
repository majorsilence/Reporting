
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Identifier
	/// </summary>
	[Serializable]
	internal class Identifier : IExpr
	{
		string _Value;		// value of the identifier

		/// <summary>
		/// passed class name, function name, and args for evaluation
		/// </summary>
		public Identifier(string v) 
		{
			string lv = v.ToLower();
			if (lv == "null" || lv == "nothing")
				_Value = null;
			else
				_Value = v;
		}

		internal bool IsNothing
		{
			get { return _Value == null? true: false; }
		}

		/// <summary>
		/// The identifier as written. CType's second operand is a type name that arrives here
		/// as an ordinary identifier, and the parser has to read the name back to pick the
		/// conversion.
		/// </summary>
		internal string Name
		{
			get { return _Value; }
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.Object;			// TODO
		}

		public Task<bool> IsConstant()
		{
			return Task.FromResult(false);
		}

		public Task<IExpr> ConstantOptimization()
		{	
			return Task.FromResult(this as IExpr);
		}

		public Task<object> Evaluate(Report rpt, Row row)
		{	
			return Task.FromResult(_Value as object);
		}

		public async Task<double> EvaluateDouble(Report rpt, Row row)
		{
			return Convert.ToDouble(await Evaluate(rpt, row));
		}
		
		public async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			return Convert.ToDecimal(await Evaluate(rpt, row));
		}

        public async Task<int> EvaluateInt32(Report rpt, Row row)
        {
            return Convert.ToInt32(await Evaluate(rpt, row));
        }

		public async Task<string> EvaluateString(Report rpt, Row row)
		{
			return Convert.ToString(await Evaluate(rpt, row));
		}

		public async Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			return Convert.ToDateTime(await Evaluate(rpt, row));
		}

		public async Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			return Convert.ToBoolean(await Evaluate(rpt, row));
		}
	}
}
