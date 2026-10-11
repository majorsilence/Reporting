
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Format function: Format(expr, string expr format)
	/// </summary>
	[Serializable]
	internal class FunctionFormat : IExpr
	{
		IExpr _Formatee;	// object to format
		IExpr _Format;		// format string
		/// <summary>
		/// Format an object
		/// </summary>
		public FunctionFormat(IExpr formatee, IExpr format) 
		{
			_Formatee = formatee;
			_Format= format;
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.String;
		}

		// 
		public async Task<object> Evaluate(Report rpt, Row row)
		{
			return await EvaluateString(rpt, row).ConfigureAwait(false);
		}

		public async Task<bool> IsConstant()
		{
			if (await _Formatee.IsConstant().ConfigureAwait(false))
				return await _Format.IsConstant().ConfigureAwait(false);

			return false;
		}
	
		public async Task<IExpr> ConstantOptimization()
		{
			_Formatee = await _Formatee.ConstantOptimization().ConfigureAwait(false);
			_Format = await _Format.ConstantOptimization().ConfigureAwait(false);
			if (await _Formatee.IsConstant().ConfigureAwait(false) && await _Format.IsConstant().ConfigureAwait(false))
			{
				string s = await EvaluateString(null, null).ConfigureAwait(false);
				return new ConstantString(s);
			}

			return this;
		}
		
		public async Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			string result = await EvaluateString(rpt, row).ConfigureAwait(false);

			return Convert.ToBoolean(result);
		}
		
		public async Task<double> EvaluateDouble(Report rpt, Row row)
		{
			string result = await EvaluateString(rpt, row).ConfigureAwait(false);

			return Convert.ToDouble(result);
		}
		
		public async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			string result = await EvaluateString(rpt, row).ConfigureAwait(false);
			return Convert.ToDecimal(result);
		}

        public async Task<int> EvaluateInt32(Report rpt, Row row)
        {
            string result = await EvaluateString(rpt, row).ConfigureAwait(false);
            return Convert.ToInt32(result);
        }

		public async Task<string> EvaluateString(Report rpt, Row row)
		{
			object o = await _Formatee.Evaluate(rpt, row).ConfigureAwait(false);
			if (o == null)
				return null;
			string format = await _Format.EvaluateString(rpt, row).ConfigureAwait(false);
			if (format == null)
				return o.ToString();	// just return string version of object

			string result=null;
			try
			{
				// Resolve the report's language the same way Style.GetFormatedString does, so
				// that locale-sensitive formats (C, N, P, etc.) honour the RDL <Language> element
				// instead of falling back to the ambient/thread culture (which is the invariant
				// culture -- generic "¤" currency symbol -- in many server/container deployments).
				string language = rpt != null ? await rpt.ReportDefinition.EvalLanguage(rpt, row).ConfigureAwait(false) : null;
				result = Style.FormatValue(o, Convert.GetTypeCode(o), format, language);
			}
			catch (Exception ex) 		// invalid format string specified
			{           //    treat as a weak error
				rpt.rl.LogError(2, String.Format("Format string:{1} Value Type:{2} Exception:{0}", ex.Message, format, o.GetType().Name));
				result = o.ToString();
			}
			return result;
		}

		public async Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			string result = await EvaluateString(rpt, row).ConfigureAwait(false);
			return Convert.ToDateTime(result);
		}
	}
}
