

using System;
using System.Xml;
using System.Collections;
using System.Globalization;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Represent a report parameter (name, default value, runtime value,
	///</summary>
	[Serializable]
	internal class ReportParameter : ReportLink
	{
		Name _Name;			// Name of the parameter 
		// Note: Parameter names need only be
		// unique within the containing Parameters collection
		TypeCode _dt;	// The data type of the parameter
		bool _NumericType=false;	// true if _dt is a numeric type
		bool _Nullable;		// Indicates the value for this parameter is allowed to be Null.
		DefaultValue _DefaultValue;		// Default value to use for the parameter (if not provided by the user)
		// If no value is provided as a part of the
		//	  definition or by the user, the value is
		// null. Required if there is no Prompt and
		//  either Nullable is False or a ValidValues
		// list is provided that does not contain Null
		// (an omitted Value).
		bool _AllowBlank;	// Indicates the value for this parameter is
		// allowed to be the empty string. Ignored
		// if DataType is not string.
		string _Prompt;		// The user prompt to display when asking
		// for parameter values
		// If omitted, the user should not be
		// prompted for a value for this parameter.
		ValidValues _ValidValues; // Possible values for the parameter (for an
		//	end-user prompting interface)
		bool _Hidden=false;					// indicates parameter should not be showed to user
		bool _MultiValue=false;				// indicates parameter is a collection - expressed as 0 based arrays Parameters!p1.Value(0)
		TrueFalseAutoEnum _UsedInQuery; // Enum True | False | Auto (default)
		//	Indicates whether or not the parameter is
		//	used in a query in the report. This is
		//	needed to determine if the queries need
		//	to be re-executed if the parameter
		//	changes. Auto indicates the
		//	UsedInQuery setting should be
		//	autodetected as follows: True if the
		//	parameter is referenced in any query
		//	value expression.		
	
		internal ReportParameter(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_Name=null;
			_dt = TypeCode.Object;
			_Nullable = false;
			_DefaultValue=null;
			_AllowBlank=false;
			_Prompt=null;
			_ValidValues=null;
			_UsedInQuery = TrueFalseAutoEnum.Auto;
			// Run thru the attributes
			foreach(XmlAttribute xAttr in xNode.Attributes)
			{
				switch (xAttr.Name)
				{
					case "Name":
						_Name = new Name(xAttr.Value);
						break;
				}
			}
			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "DataType":
						_dt = DataType.GetStyle(xNodeLoop.InnerText, this.OwnerReport);
						_NumericType = DataType.IsNumeric(_dt);
						break;
					case "Nullable":
						_Nullable = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					case "DefaultValue":
						_DefaultValue = new DefaultValue(r, this, xNodeLoop);
						break;
					case "AllowBlank":
						_AllowBlank = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					case "Prompt":
						_Prompt = xNodeLoop.InnerText;
						break;
					case "Hidden":
						_Hidden = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						OwnerReport.rl.LogError(4, "ReportParameter element Hidden is currently ignored.");	// TODO
						break;
					case "MultiValue":
						_MultiValue = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					case "ValidValues":
						_ValidValues = new ValidValues(r, this, xNodeLoop);
						break;
					case "UsedInQuery":
						_UsedInQuery = TrueFalseAuto.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					default:	
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown ReportParameter element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
			if (_Name == null)
				OwnerReport.rl.LogError(8, "ReportParameter name is required but not specified.");

			if (_dt == TypeCode.Object)
				OwnerReport.rl.LogError(8, string.Format("ReportParameter DataType is required but not specified or invalid for {0}.", _Name==null? "<unknown name>": _Name.Nm));
		}

		async override internal Task FinalPass()
		{
			if (_DefaultValue != null)
                await _DefaultValue.FinalPass();
			if (_ValidValues != null)
                await _ValidValues.FinalPass();
			return;
		}

		internal Name Name
		{
			get { return  _Name; }
			set {  _Name = value; }
		}

		internal async Task<object> GetRuntimeValue(Report rpt)
		{
			object rtv = rpt == null? null:
				rpt.Cache.Get(this, "runtimevalue");

			if (rtv != null)
				return rtv; 
			if (_DefaultValue == null)
				return null;
				
			object[] result = await _DefaultValue.GetValue(rpt);
			if (result == null)
				return null;
			object v = result[0];
			if (v is String && _NumericType)
				v = ConvertStringToNumber((string) v);

			rtv = Convert.ChangeType(v, _dt);
			if (rpt != null)
				rpt.Cache.Add(this, "runtimevalue", rtv);

			return rtv;
		}

        internal async Task <ArrayList> GetRuntimeValues(Report rpt)
        {
            ArrayList rtv = rpt == null ? null :
                (ArrayList) rpt.Cache.Get(this, "rtvs");

            if (rtv != null)
                return rtv;

            if (_DefaultValue == null)
                return null;

            object[] result = await _DefaultValue.GetValue(rpt);
            if (result == null)
                return null;

            ArrayList ar = new ArrayList(result.Length);
            foreach (object v in result)
            {
                object nv = v;
                if (nv is String && _NumericType)
                    nv = ConvertStringToNumber((string)nv);

                ar.Add( Convert.ChangeType(nv, _dt));
            }

            if (rpt != null)
                rpt.Cache.Add(this, "rtvs", ar);

            return ar;
        }

		internal void SetRuntimeValue(Report rpt, object v)
		{
            if (this.MultiValue)
            {   // ok to only set one parameter of multiValue;  but we still save as MultiValue
                ArrayList ar;
                if (v is string)
                {   // when the value is a string we parse it looking for multiple arguments
                    ParameterLexer pl = new ParameterLexer(v as string);
                    ar = pl.Lex();
                }
                else if (v is ICollection)
                {   // when collection put it in local array
                    ar = new ArrayList(v as ICollection);
                }
                else
                {
                    ar = new ArrayList(1);
                    ar.Add(v);
                } 

                SetRuntimeValues(rpt, ar);
                return;
            }

			object rtv;
			// A null value is only an error when the parameter says it is. Nullable was
			// parsed and exposed but never consulted, so a report rendered without values
			// - the normal case for one converted from a format that prompts for them -
			// died converting null to the declared type instead of leaving the parameter
			// empty.
			if (v == null && _Nullable)
			{
				rpt.Cache.AddReplace(this, "runtimevalue", null);
				return;
			}

            if (v is Guid)
            {
                v = ((Guid)v).ToString("B"); 
            }
			if (!AllowBlank && _dt == TypeCode.String && (string) v == "")
				throw new ArgumentException(string.Format("Empty string isn't allowed for {0}.", Name.Nm));
			try 
			{
				if (v is String && _NumericType)
					v = ConvertStringToNumber((string) v);
				rtv = Convert.ChangeType(v, _dt); 
			}
			catch (Exception e)
			{
				// illegal parameter passed. The value can be null - a subreport parameter
				// whose expression evaluated to nothing - and reporting it must not
				// dereference it, or the diagnostic throws in place of the real error
				// and the whole render dies with an unexplained NullReferenceException.
                string err = "Illegal parameter value for '" + Name.Nm + "' provided.  Value =" + (v == null ? "(null)" : v.ToString());
                if (rpt == null)
                    OwnerReport.rl.LogError(4, err);
                else
                    rpt.rl.LogError(4, err);
				throw new ArgumentException(string.Format("Unable to convert '{0}' to {1} for {2}", v ?? "(null)", _dt, Name.Nm),e);
			}
			rpt.Cache.AddReplace(this, "runtimevalue", rtv);
		}

        internal void SetRuntimeValues(Report rpt, ArrayList vs)
        {
            if (!this.MultiValue)
                throw new ArgumentException(string.Format("{0} is not a MultiValue parameter. SetRuntimeValues only valid for MultiValue parameters", this.Name.Nm));

            ArrayList ar = new ArrayList(vs.Count);
            foreach (object v in vs)
            {
                object rtv;
                if (!AllowBlank && _dt == TypeCode.String && v.ToString() == "")
                {
                    string err = string.Format("Empty string isn't allowed for {0}.", Name.Nm);
                    if (rpt == null)
                        OwnerReport.rl.LogError(4, err);
                    else
                        rpt.rl.LogError(4, err);
                    throw new ArgumentException(err);
                }
                try
                {
                    object nv = v;
                    if (nv is String && _NumericType)
                        nv = ConvertStringToNumber((string)nv);
                    rtv = Convert.ChangeType(nv, _dt);
                    ar.Add(rtv);
                }
                catch (Exception e)
                {
                    // illegal parameter passed
                    string err = "Illegal parameter value for '" + Name.Nm + "' provided.  Value =" + v.ToString();
                    if (rpt == null)
                        OwnerReport.rl.LogError(4, err);
                    else
                        rpt.rl.LogError(4, err);
                    throw new ArgumentException(string.Format("Unable to convert '{0}' to {1} for {2}", v, _dt, Name.Nm), e);
                }
            }
            rpt.Cache.AddReplace(this, "rtvs", ar);
        }

		private object ConvertStringToNumber(string newv)
		{
			// remove any commas, currency symbols (internationalized)
			NumberFormatInfo nfi = NumberFormatInfo.CurrentInfo;
			if(!String.IsNullOrEmpty(nfi.NumberGroupSeparator))
				newv = newv.Replace(nfi.NumberGroupSeparator, "");
			if (!String.IsNullOrEmpty(nfi.CurrencySymbol))
				newv = newv.Replace(nfi.CurrencySymbol, "");
			return newv;
		}

		internal TypeCode dt
		{
			get { return  _dt; }
			set {  _dt = value; }
		}

		internal bool Nullable
		{
			get { return  _Nullable; }
			set {  _Nullable = value; }
		}
		
		internal bool Hidden
		{
			get { return  _Hidden; }
			set {  _Hidden = value; }
		}

		internal bool MultiValue
		{
			get { return  _MultiValue; }
			set {  _MultiValue = value; }
		}

		internal DefaultValue DefaultValue
		{
			get { return  _DefaultValue; }
			set {  _DefaultValue = value; }
		}

		internal bool AllowBlank
		{
			get { return  _AllowBlank; }
			set {  _AllowBlank = value; }
		}

		internal string Prompt
		{
			get { return  _Prompt; }
			set {  _Prompt = value; }
		}

		internal ValidValues ValidValues
		{
			get { return  _ValidValues; }
			set {  _ValidValues = value; }
		}
		

		internal TrueFalseAutoEnum UsedInQuery
		{
			get { return  _UsedInQuery; }
			set {  _UsedInQuery = value; }
		}
	}
/// <summary>
/// Public class used to pass user provided report parameters.
/// </summary>
	public class UserReportParameter
	{
		Report _rpt;
		ReportParameter _rp;
		object[] _DefaultValue;
		string[] _DisplayValues;
		object[] _DataValues;

		internal UserReportParameter(Report rpt, ReportParameter rp)
		{
			_rpt = rpt;
			_rp = rp;
		}
		/// <summary>
		/// Name of the report paramenter.
		/// </summary>
		public string Name
		{
			get { return  _rp.Name.Nm; }
		}

		/// <summary>
		/// Type of the report parameter.
		/// </summary>
		public TypeCode dt
		{
			get { return  _rp.dt; }
		}

		/// <summary>
		/// Is parameter allowed to be null.
		/// </summary>
		public bool Nullable
		{
			get { return  _rp.Nullable; }
		}

		/// <summary>
		/// Default value(s) of the parameter.
		/// </summary>
		public object[] DefaultValue
		{
			get 
			{ 
				if (_DefaultValue == null)
				{
					if (_rp.DefaultValue != null)
					{
						// HACK: async
						_DefaultValue = Task.Run(async () => await _rp.DefaultValue.ValuesCalc(this._rpt)).GetAwaiter().GetResult();
					}
				}
				return _DefaultValue;
			}
		}

		/// <summary>
		/// Is parameter allowed to be the empty string?
		/// </summary>
		public bool AllowBlank
		{
			get { return  _rp.AllowBlank; }
		}
        /// <summary>
        /// Does parameters accept multiple values?
        /// </summary>
        public bool MultiValue
        {
            get { return _rp.MultiValue; }
        }

		/// <summary>
		/// Text used to prompt for the parameter.
		/// </summary>
		public string Prompt
		{
			get { return  _rp.Prompt; }
		}

		/// <summary>
		/// The display values for the parameter.  These may differ from the data values.
		/// </summary>
		public string[] DisplayValues
		{
			get 
			{
				if (_DisplayValues == null)
				{
					if (_rp.ValidValues != null)
					{
						// HACK: async
						_DisplayValues = Task.Run(async () => await _rp.ValidValues.DisplayValues(_rpt)).GetAwaiter().GetResult();
					}
				}
				return  _DisplayValues;		 
			}
		}

		/// <summary>
		/// The data values of the parameter.
		/// </summary>
		public object[] DataValues
		{
			get 
			{
				if (_DataValues == null)
				{
					if (_rp.ValidValues != null)
					{
						// HACK: async
						_DataValues = Task.Run(async () => await _rp.ValidValues.DataValues(this._rpt)).GetAwaiter().GetResult();
					}
				}
				return  _DataValues;		 
			}
		}

        /// <summary>
        /// Obtain the data value from a (potentially) display value
        /// </summary>
        /// <param name="dvalue">Display value</param>
        /// <returns>The data value cooresponding to the display value.</returns>
        private object GetDataValueFromDisplay(object dvalue)
        {
            object val = dvalue;

            if (dvalue != null &&
                DisplayValues != null &&
                DataValues != null &&
                DisplayValues.Length == DataValues.Length)		// this should always be true
            {	// if display values are provided then we may need to 
                //  use the provided value with a display value and use
                //  the cooresponding data value
                string sval = dvalue.ToString();
                for (int index = 0; index < DisplayValues.Length; index++)
                {
                    if (DisplayValues[index].CompareTo(sval) == 0)
                    {
                        val = DataValues[index];
                        break;
                    }
                }
            }
            return val;
        }

		/// <summary>
		/// The runtime value of the parameter.
		/// Prefer using GetValueAsync() to avoid synchronous blocking on an async call.
		/// </summary>
		public object Value
		{
            get
            {
                // HACK: async - synchronous wrapper needed for property getter;
                // use GetValueAsync() when an async context is available.
                return Task.Run(async () => await _rp.GetRuntimeValue(this._rpt)).GetAwaiter().GetResult();
            }
			set 
			{
                if (this.MultiValue && value is string)
                {   // treat this as a multiValue request
                    Values = ParseValue(value as string);
                    return;
                }

                object dvalue = GetDataValueFromDisplay(value);

                _rp.SetRuntimeValue(_rpt, dvalue); 
			}
		}

        /// <summary>
        /// Asynchronously gets the runtime value of the parameter.
        /// Prefer this over the synchronous Value getter when an async context is available.
        /// </summary>
        public async Task<object> GetValueAsync()
        {
            return await _rp.GetRuntimeValue(this._rpt);
        }

        /// <summary>
        /// Take a string and parse it into multiple values
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private ArrayList ParseValue(string v)
        {
            ParameterLexer pl = new ParameterLexer(v);
            return pl.Lex();
        }

        /// <summary>
        /// The runtime values of the parameter when MultiValue.
        /// </summary>
        public ArrayList Values
        {
            get { 
				// HACK: async
				return Task.Run(async () => await _rp.GetRuntimeValues(this._rpt)).GetAwaiter().GetResult(); 
			}
            set
            {
                ArrayList ar = new ArrayList(value.Count);
                foreach (object v in value)
                {
                    ar.Add(GetDataValueFromDisplay(v));
                }
                _rp.SetRuntimeValues(_rpt, ar);
            }
        }
    }
}
