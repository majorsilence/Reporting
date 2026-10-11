
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// String constant
	/// </summary>
	[Serializable]
	internal class ConstantString : IExpr, ICachedConstant
	{
		string _Value;		// value of the constant
		readonly Task<object> _objectTask;
		readonly Task<string> _stringTask;
		readonly Task<IExpr> _optimizedTask;

		static readonly Task<bool> TrueTask = Task.FromResult(true);

		public Task<object> ObjectTask { get { return _objectTask; } }
		public Task<string> StringTask { get { return _stringTask; } }

		/// <summary>
		/// passed class name, function name, and args for evaluation
		/// </summary>
		public ConstantString(string v) 
		{
			_Value = v;
			_objectTask = Task.FromResult((object)v);
			_stringTask = Task.FromResult(v);
			_optimizedTask = Task.FromResult((IExpr)this);
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.String;
		}

		public Task<bool> IsConstant()
		{
			return TrueTask;
		}

		public Task<IExpr> ConstantOptimization()
		{	// already constant expression
			return _optimizedTask;
		}

		public Task<object> Evaluate(Report rpt, Row row)
		{
			return _objectTask;
		}

		public Task<string> EvaluateString(Report rpt, Row row)
		{
			return _stringTask;
		}
		
		public Task<double> EvaluateDouble(Report rpt, Row row)
		{
			return Task.FromResult(Convert.ToDouble(_Value));
		}
		
		public Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			return Task.FromResult(Convert.ToDecimal(_Value));
		}

        public Task<int> EvaluateInt32(Report rpt, Row row)
        {
            return Task.FromResult(Convert.ToInt32(_Value));
        }

		public Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			return Task.FromResult(Convert.ToDateTime(_Value));
		}

		public Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			return Task.FromResult(Convert.ToBoolean(_Value));
		}
	}
}
