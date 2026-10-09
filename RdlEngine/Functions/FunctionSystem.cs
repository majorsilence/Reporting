
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

			// Get ready to call the function
			Type theClassType = ResolveSystemType(_Cls);
			MethodInfo mInfo = GetCachedMethod(theClassType, argTypes);
            if (mInfo == null)
            {
                throw new Exception(string.Format(Strings.FunctionSystem_Error_MethodNotFound, _Func, _Cls));
            }

			return mInfo.Invoke(theClassType, argResults);
		}

		// The class name always comes from the parser's closed set of built-in classes, so it is
		// mapped with typeof (which the trimmer and AOT compiler can see) instead of Type.GetType.
		[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
		static Type ResolveSystemType(string cls)
		{
			return cls switch
			{
				"System.Math" => typeof(System.Math),
				"System.String" => typeof(string),
				"System.Convert" => typeof(System.Convert),
				"Majorsilence.Reporting.Rdl.Financial" => typeof(Financial),
				"Majorsilence.Reporting.Rdl.VBFunctions" => typeof(VBFunctions),
				"Majorsilence.Reporting.Rdl.StringFunctions" => typeof(StringFunctions),
				_ => throw new ArgumentException("Unknown system class: " + cls, nameof(cls)),
			};
		}

		// Overload resolution is the slow part; remember the last answer for this call site.
		// A single immutable entry is swapped in, so concurrent renders can share this expression.
		sealed class MethodCacheEntry
		{
			internal MethodCacheEntry(string cls, string func, Type[] argTypes, MethodInfo method)
			{
				Cls = cls; Func = func; ArgTypes = argTypes; Method = method;
			}
			internal readonly string Cls, Func;
			internal readonly Type[] ArgTypes;
			internal readonly MethodInfo Method;
		}
		MethodCacheEntry? _cache;

		MethodInfo GetCachedMethod(
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type theClassType,
			Type[] argTypes)
		{
			MethodCacheEntry? c = _cache;
			if (c != null && c.Cls == _Cls && c.Func == _Func && c.ArgTypes.AsSpan().SequenceEqual(argTypes))
				return c.Method;

			MethodInfo mInfo = XmlUtil.GetMethod(theClassType, _Func, argTypes);
			if (mInfo != null)
				_cache = new MethodCacheEntry(_Cls, _Func, argTypes, mInfo);
			return mInfo;
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
