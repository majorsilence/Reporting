
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Majorsilence.Reporting.RdlEngine.Resources;
using Majorsilence.Reporting.Rdl;
using System.Threading.Tasks;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Obtain the runtime value of a Report parameter
	/// </summary>
	[Serializable]
	internal class FunctionReportParameter : IExpr
	{
		protected ReportParameter p;
        ReportParameterMethodEnum _type;
        IExpr _arg;                         // when MultiValue parameter; methods may need arguments

		/// <summary>
		/// obtain value of ReportParameter
		/// </summary>
		public FunctionReportParameter(ReportParameter parm) 
		{
			p=parm;
            _type = ReportParameterMethodEnum.Value;
            _arg = null;
		}

        internal ReportParameterMethodEnum ParameterMethod
        {
            get { return _type; }
            set { _type = value; }
        }

        internal void SetParameterMethod(string pm, IExpr[] args)
        {
            if (!this.p.MultiValue)
                throw new ArgumentException(string.Format("{0} must be a MultiValue parameter to accept methods", this.p.Name.Nm));

            if (pm == null)
            {
                _type = ReportParameterMethodEnum.Index;
            }
            else switch (pm)
            {
                case "Contains": _type = ReportParameterMethodEnum.Contains; break;
                case "BinarySearch": _type = ReportParameterMethodEnum.BinarySearch; break;
                case "Count": 
                    _type = ReportParameterMethodEnum.Count; 
                    if (args != null)
                        throw new ArgumentException("Count does not have any arguments.");
                    break;
                case "IndexOf": _type = ReportParameterMethodEnum.IndexOf; break;
                case "LastIndexOf": _type = ReportParameterMethodEnum.LastIndexOf; break;
                case "Value": _type = ReportParameterMethodEnum.Value; break;
                default:
                    throw new ArgumentException(string.Format("{0} is an unknown array method.", pm));
            }

            if (_type != ReportParameterMethodEnum.Count)
            {
                if (args == null || args.Length != 1)
                    throw new ArgumentException(string.Format("{0} must have exactly one argument.", pm));

                _arg = args[0];
            }

            return;
        }

		public virtual TypeCode GetTypeCode()
		{
            switch (_type)
            {
                case ReportParameterMethodEnum.Contains:
                    return TypeCode.Boolean;
                case ReportParameterMethodEnum.BinarySearch:
                case ReportParameterMethodEnum.Count:
                case ReportParameterMethodEnum.IndexOf:
                case ReportParameterMethodEnum.LastIndexOf:
                    return TypeCode.Int32;
                case ReportParameterMethodEnum.Value:
                    return p.MultiValue ? TypeCode.Object : p.dt;   // MultiValue type is really ArrayList
                case ReportParameterMethodEnum.Index:
                default:
                    return p.dt;
            }
		}

		public virtual Task<bool> IsConstant()
		{
			return Task.FromResult(false);
		}

		public virtual Task<IExpr> ConstantOptimization()
		{	// not a constant expression
			return Task.FromResult(this as IExpr);
		}

		// Evaluate is for interpretation  (and is relatively slow)
		public async virtual Task<object> Evaluate(Report rpt, Row row)
		{
            return this.p.MultiValue? await EvaluateMV(rpt, row).ConfigureAwait(false) : await p.GetRuntimeValue(rpt).ConfigureAwait(false);
		}

        private async Task<object> EvaluateMV(Report rpt, Row row)
        {
            ArrayList ar = await p.GetRuntimeValues(rpt).ConfigureAwait(false);
            
            object va = this._arg == null ? null : await _arg.Evaluate(rpt, row).ConfigureAwait(false);

            switch(_type) 
            {
                case ReportParameterMethodEnum.Value:
                    return ar;
                case ReportParameterMethodEnum.Contains:
                    return ar.Contains(va);
                case ReportParameterMethodEnum.BinarySearch:
                    return ar.BinarySearch(va);
                case ReportParameterMethodEnum.Count:
                    return ar.Count;
                case ReportParameterMethodEnum.IndexOf:
                    return ar.IndexOf(va);
                case ReportParameterMethodEnum.LastIndexOf:
                    return ar.LastIndexOf(va);
                case ReportParameterMethodEnum.Index:
                    int i = Convert.ToInt32(va);
                    return ar[i];
                default:
                    throw new Exception(Strings.FunctionReportParameter_Error_UnknownReporParameterMethod);
            }
        }
		
		public virtual async Task<double> EvaluateDouble(Report rpt, Row row)
		{
            object rtv = await Evaluate(rpt, row).ConfigureAwait(false);
			if (rtv == null)
				return Double.NaN;

			switch (this.GetTypeCode())
			{
				case TypeCode.Double:
					return ((double) rtv);
				case TypeCode.Object:
					return Double.NaN;
				case TypeCode.Int32:
					return (double) ((int) rtv);
				case TypeCode.Boolean:
					return Convert.ToDouble((bool) rtv);
				case TypeCode.String:
					return Convert.ToDouble((string) rtv);
				case TypeCode.DateTime:
					return Convert.ToDouble((DateTime) rtv);
				default:
					return Double.NaN;
			}
		}
		
		public virtual async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
            object rtv = await Evaluate(rpt, row).ConfigureAwait(false);
			if (rtv == null)
				return Decimal.MinValue;

            switch (this.GetTypeCode())
			{
				case TypeCode.Double:
					return Convert.ToDecimal((double) rtv);
				case TypeCode.Object:
					return Decimal.MinValue;
				case TypeCode.Int32:
					return Convert.ToDecimal((int) rtv);
				case TypeCode.Boolean:
					return Convert.ToDecimal((bool) rtv);
				case TypeCode.String:
					return Convert.ToDecimal((string) rtv);
				case TypeCode.DateTime:
					return Convert.ToDecimal((DateTime) rtv);
				default:
					return Decimal.MinValue;
			}
		}

        public virtual async Task<int> EvaluateInt32(Report rpt, Row row)
        {
            object rtv = await Evaluate(rpt, row).ConfigureAwait(false);
            if (rtv == null)
                return int.MinValue;

            switch (this.GetTypeCode())
            {
                case TypeCode.Double:
                    return Convert.ToInt32((double)rtv);
                case TypeCode.Decimal:
                    return Convert.ToInt32((decimal)rtv);
                case TypeCode.Object:
                    return int.MinValue;
                case TypeCode.Int32:
                    return (int)rtv;
                case TypeCode.Boolean:
                    return Convert.ToInt32((bool)rtv);
                case TypeCode.String:
                    return Convert.ToInt32((string)rtv);
                case TypeCode.DateTime:
                    return Convert.ToInt32((DateTime)rtv);
                default:
                    return int.MinValue;
            }
        }


		public virtual async  Task<string> EvaluateString(Report rpt, Row row)
		{
            object rtv = this.p.MultiValue ? await EvaluateMV(rpt, row).ConfigureAwait(false) : await p.GetRuntimeValue(rpt).ConfigureAwait(false);
//            object rtv = Evaluate(rpt, row);
			if (rtv == null)
				return null;

			return rtv.ToString();
		}

		public virtual async Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
            object rtv = await Evaluate(rpt, row).ConfigureAwait(false);
			if (rtv == null)
				return DateTime.MinValue;

            switch (this.GetTypeCode())
			{
				case TypeCode.Double:
					return Convert.ToDateTime((double) rtv);
				case TypeCode.Object:
					return DateTime.MinValue;
				case TypeCode.Int32:
					return Convert.ToDateTime((int) rtv);
				case TypeCode.Boolean:
					return Convert.ToDateTime((bool) rtv);
				case TypeCode.String:
					return Convert.ToDateTime((string) rtv);
				case TypeCode.DateTime:
					return (DateTime) rtv;
				default:
					return DateTime.MinValue;
			}
		}

		public virtual async Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
            object rtv = await Evaluate(rpt, row).ConfigureAwait(false);

			if (rtv == null)
				return false;

            switch (this.GetTypeCode())
			{
				case TypeCode.Double:
					return Convert.ToBoolean((double) rtv);
				case TypeCode.Object:
					return false;
				case TypeCode.Int32:
					return Convert.ToBoolean((int) rtv);
				case TypeCode.Boolean:
					return (bool) rtv;
				case TypeCode.String:
					return Convert.ToBoolean((string) rtv);
				case TypeCode.DateTime:
					return Convert.ToBoolean((DateTime) rtv);
				default:
					return false;
			}
		}
	}
    
    public enum ReportParameterMethodEnum
    {
        Value,
        Contains,
        BinarySearch,
        Count,
        IndexOf,
        LastIndexOf,
        Index
    }

}
