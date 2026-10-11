
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Globalization;
using Majorsilence.Reporting.RdlEngine.Resources;
using Majorsilence.Reporting.Rdl;
using System.Threading.Tasks;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Handle references to the User collection.  e.g. User("UserID")
	/// </summary>
	[Serializable]
	internal class FunctionUserCollection : IExpr
	{
		private IDictionary _User;
		private IExpr _ArgExpr;

		/// <summary>
		/// obtain value of Field
		/// </summary>
		public FunctionUserCollection(IDictionary user, IExpr arg) 
		{
			_User = user;
			_ArgExpr = arg;
		}

		public virtual TypeCode GetTypeCode()
		{
			return TypeCode.String;		// all the user types happen to be string
		}

		public virtual Task<bool> IsConstant()
		{
			return Task.FromResult(false);
		}

		public virtual async Task<IExpr> ConstantOptimization()
		{	
			_ArgExpr = await _ArgExpr.ConstantOptimization().ConfigureAwait(false);

			if (await _ArgExpr.IsConstant().ConfigureAwait(false))
			{
				string o = await _ArgExpr.EvaluateString(null, null).ConfigureAwait(false);
				if (o == null)
					throw new Exception(Strings.FunctionUserCollection_Error_UserCollectionNull); 
				string lo = o.ToLower();
				if (lo == "userid")
					return new FunctionUserID();
				if (lo == "language")
					return new FunctionUserLanguage();
				throw new Exception(string.Format(Strings.FunctionUserCollection_Error_UserCollectionInvalid, o)); 
			}

			return this;
		}

		// 
		public virtual async Task<object> Evaluate(Report rpt, Row row)
		{
			if (rpt == null)
				return null;
			string u = await _ArgExpr.EvaluateString(rpt, row).ConfigureAwait(false);
			if (u == null)
				return null;
			switch (u.ToLower())
			{
				case "userid":
					return rpt.UserID;
				case "language":
					return await rpt.GetClientLanguageAsync().ConfigureAwait(false) ??
						CultureInfo.CurrentCulture.ThreeLetterISOLanguageName;
				default:
					return null;
			}
		}
		
		public virtual async Task<double> EvaluateDouble(Report rpt, Row row)
		{
			if (row == null)
				return Double.NaN;
			return Convert.ToDouble(await Evaluate(rpt, row).ConfigureAwait(false), NumberFormatInfo.InvariantInfo);
		}
		
		public virtual async Task<decimal> EvaluateDecimal(Report rpt, Row row)
		{
			if (row == null)
				return decimal.MinValue;
			return Convert.ToDecimal(await Evaluate(rpt, row).ConfigureAwait(false), NumberFormatInfo.InvariantInfo);
		}

        public virtual async Task<int> EvaluateInt32(Report rpt, Row row)
        {
            if (row == null)
                return int.MinValue;
            return Convert.ToInt32(await Evaluate(rpt, row).ConfigureAwait(false), NumberFormatInfo.InvariantInfo);
        }

		public virtual async Task<string> EvaluateString(Report rpt, Row row)
		{
			if (row == null)
				return null;
			return Convert.ToString(await Evaluate(rpt, row).ConfigureAwait(false));
		}

		public virtual async Task<DateTime> EvaluateDateTime(Report rpt, Row row)
		{
			if (row == null)
				return DateTime.MinValue;
			return Convert.ToDateTime(await Evaluate(rpt, row).ConfigureAwait(false));
		}

		public virtual async Task<bool> EvaluateBoolean(Report rpt, Row row)
		{
			if (row == null)
				return false;
			return Convert.ToBoolean(await Evaluate(rpt, row).ConfigureAwait(false));
		}
	}
}
