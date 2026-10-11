
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Obtain the runtime value of a report parameter label.
	/// </summary>
	[Serializable]
	internal class FunctionReportParameterLabel : FunctionReportParameter
	{
		/// <summary>
		/// obtain value of ReportParameter
		/// </summary>
		public FunctionReportParameterLabel(ReportParameter parm): base(parm) 
		{
		}

		public override TypeCode GetTypeCode()
		{
            if (this.ParameterMethod == ReportParameterMethodEnum.Value)
                return TypeCode.String;
            else
                return base.GetTypeCode();
		}

		public override Task<bool> IsConstant()
		{
			return Task.FromResult(false);
		}

		public override Task<IExpr> ConstantOptimization()
		{	// not a constant expression
			return Task.FromResult(this as IExpr);
		}

		// Evaluate is for interpretation  (and is relatively slow)
		public async override Task<object> Evaluate(Report rpt, Row row)
		{
			string v = await base.EvaluateString(rpt, row).ConfigureAwait(false);

			if (p.ValidValues == null)
				return v;

			string[] displayValues = await p.ValidValues.DisplayValues(rpt).ConfigureAwait(false);
			object[] dataValues = await p.ValidValues.DataValues(rpt).ConfigureAwait(false);

			for (int i=0; i < dataValues.Length; i++)
			{
				if (dataValues[i].ToString() == v)
					return displayValues[i];
			}

			return v;
		}
		
		public async override Task<double> EvaluateDouble(Report rpt, Row row)
		{	
			string r = await EvaluateString(rpt, row).ConfigureAwait(false);

			return r == null? double.MinValue: Convert.ToDouble(r);
		}
		
		public override async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			string r = await EvaluateString(rpt, row).ConfigureAwait(false);

			return r == null? decimal.MinValue: Convert.ToDecimal(r);
		}

		public async override Task<string> EvaluateString(Report rpt, Row row)
		{
			return (string)await Evaluate(rpt, row).ConfigureAwait(false);
		}

		public async override Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			string r = await EvaluateString(rpt, row).ConfigureAwait(false);

			return r == null? DateTime.MinValue: Convert.ToDateTime(r);
		}

		public async override Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			string r = await EvaluateString(rpt, row).ConfigureAwait(false);

			return r.ToLower() == "true"? true: false;
		}
	}
}
