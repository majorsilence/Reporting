
using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Majorsilence.Reporting.RdlEngine.Resources;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Process a custom static method invokation.
	/// </summary>
	[Serializable]
	internal class FunctionCustomStatic : IExpr
	{
		string _Cls;		// class name
		string _Func;		// function/operator
		IExpr[] _Args;		// arguments 
		CodeModules _Cm;		// the loaded assemblies
		TypeCode _ReturnTypeCode;	// the return type
		Type[] _ArgTypes;	// argument types

		/// <summary>
		/// passed class name, function name, and args for evaluation
		/// </summary>
		public FunctionCustomStatic(CodeModules cm, string c, string f, IExpr[] a, TypeCode type) 
		{
			_Cls = c;
			_Func = f;
			_Args = a;
			_Cm = cm;
			_ReturnTypeCode = type;

			_ArgTypes = new Type[a.Length];
			int i=0;
			foreach (IExpr ex in a)
			{
				_ArgTypes[i++] = XmlUtil.GetTypeFromTypeCode(ex.GetTypeCode());
			}

		}

		public TypeCode GetTypeCode()
		{
			return _ReturnTypeCode;
		}

		public Task<bool> IsConstant()
		{
			return Task.FromResult(false);		// Can't know what the function does
		}

		public async Task<IExpr> ConstantOptimization()
		{
			// Do constant optimization on all the arguments
			for (int i=0; i < _Args.GetLength(0); i++)
			{
				IExpr e = (IExpr)_Args[i];
				_Args[i] = await e.ConstantOptimization();
			}

			// Can't assume that the function doesn't vary
			//   based on something other than the args e.g. Now()
			return this;
		}

		// Evaluate is for interpretation  (and is relatively slow)
		[RequiresDynamicCode("Invokes methods by name at runtime; not AOT-compatible")]
		[RequiresUnreferencedCode("Type members may be removed by the trimmer")]
		public async Task<object> Evaluate(Report rpt, Row row)
		{
			// get the results
			object[] argResults = new object[_Args.Length];
			int i=0;
			bool bUseArg=true;
			foreach(IExpr a  in _Args)
			{
				argResults[i] = await a.Evaluate(rpt, row);
				if (argResults[i] != null && argResults[i].GetType() != _ArgTypes[i])
					bUseArg = false;
				i++;
			}
			// we build the arguments based on the type
			Type[] argTypes = bUseArg? _ArgTypes: Type.GetTypeArray(argResults);

			// We can definitely optimize this by caching some info TODO

			// Get ready to call the function
			Object returnVal;
			Type theClassType= _Cm[_Cls];
            MethodInfo mInfo = XmlUtil.GetMethod(theClassType, _Func, argTypes);
            if (mInfo == null)
            {
                throw new Exception(string.Format(Strings.FunctionCustomStatic_Error_MethodNotFoundInClass, _Func, _Cls));
            }

            returnVal = mInfo.Invoke(theClassType, argResults);

			return returnVal;
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

		public string Cls
		{
			get { return  _Cls; }
			set {  _Cls = value; }
		}

		public string Func
		{
			get { return  _Func; }
			set {  _Func = value; }
		}

		public IExpr[] Args
		{
			get { return  _Args; }
			set {  _Args = value; }
		}
	}

}
