
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
	/// Class is used to evaluate static system classes.   System meaning
	/// any class that is part of this assembly.   The parser restricts this
	/// to Math, String, Convert, Financial, ...
	/// </summary>
	[Serializable]
	internal class FunctionSystem : IExpr
	{
		string _Cls;		// class name
		string _Func;		// function/operator
		IExpr[] _Args;		// arguments 
		TypeCode _ReturnTypeCode;	// the return type

		/// <summary>
		/// passed class name, function name, and args for evaluation
		/// </summary>
		public FunctionSystem(string c, string f, IExpr[] a, TypeCode type) 
		{
			_Cls = c;
			_Func = f;
			_Args = a;
			_ReturnTypeCode = type;
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
            bool bNull = false;
			foreach(IExpr a  in _Args)
			{
				argResults[i] = await a.Evaluate(rpt, row);
                if (argResults[i] == null)
                    bNull = true;
                i++;
			}
			Type[] argTypes;
            if (bNull)
            {
                // Need to put fake values in that match the types
                object[] tempResults = new object[argResults.Length];
                for (int ix = 0; ix < argResults.Length; ix++)
                {
                    tempResults[ix] =
                        argResults[ix] == null?
                            XmlUtil.GetConstFromTypeCode(_Args[ix].GetTypeCode()):
                            argResults[ix];

                }
                argTypes = Type.GetTypeArray(tempResults);
            }
            else
                argTypes = Type.GetTypeArray(argResults);

			// We can definitely optimize this by caching some info TODO

			// Get ready to call the function
			object returnVal;
			Type theClassType= Type.GetType(_Cls, true, true);
            MethodInfo mInfo = XmlUtil.GetMethod(theClassType, _Func, argTypes);
            if (mInfo == null)
            {
                throw new Exception(string.Format(Strings.FunctionSystem_Error_MethodNotFound, _Func, _Cls));
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
