
using System;
using System.Xml;
using System.Data;
using System.Collections;

namespace Majorsilence.Reporting.Data
{
	/// <summary>
	/// TxtCommand allows specifying the command for a text file.
	/// </summary>
	public class TxtCommand : IDbCommand
	{
		TxtConnection _tc;			// connection we're running under
		string _cmd;				// command to execute
		// parsed constituents of the command
		string _Url;				// url of the file 
		string _Header;				// does file contain a header row
		char _Separator;			// separator character
		string[] _Columns;			// hold the column list
		DataParameterCollection _Parameters = new DataParameterCollection();

		public TxtCommand(TxtConnection conn)
		{
			_tc = conn;  
		}

		internal string[] Columns
		{
			get {return _Columns;}
		}

		internal string Url
		{
			get 
			{
				// Check to see if "Url" or "@Url" is a parameter
				IDbDataParameter dp= _Parameters["Url"] as IDbDataParameter;
				if (dp == null)
					dp= _Parameters["@Url"] as IDbDataParameter;
				// Then check to see if the Url value is a parameter?
				if (dp == null)
					dp = _Parameters[_Url] as IDbDataParameter;
				if (dp != null)
					return dp.Value != null? dp.Value.ToString(): _Url;	// don't pass null; pass existing value
				return _Url;	// the value must be a constant
			}
			set {_Url = value;}
		}

		internal char Separator
		{
			get 
			{
				// Check to see if "Separator" or "@Separator" is a parameter
				IDbDataParameter dp= _Parameters["Separator"] as IDbDataParameter;
				if (dp == null)
					dp= _Parameters["@Separator"] as IDbDataParameter;
				// Then check to see if the Separator value is a parameter?
				if (dp == null)
					dp = _Parameters[_Separator.ToString()] as IDbDataParameter;
				if (dp != null && dp.Value != null)
				{
					string v = dp.Value.ToString();
					if (v.Length >= 1)
						return v[0];
				}

				if (_Separator != '\0')		// Initialized?
					return _Separator;		//    yes, return it
				// otherwise we default depending on the Url extension (if any)
				string url = this.Url.ToLower();
				if (url.IndexOf(".txt") >= 0)
					return '\t';
				if (url.IndexOf(".csv") >= 0)
					return ',';
				return _Separator;			// done the best we can; but have no value
			}
			set {_Separator = value;}
		}

		internal bool Header
		{
			get 
			{
				// Check to see if "Header" or "@Header" is a parameter
				IDbDataParameter dp= _Parameters["Header"] as IDbDataParameter;
				if (dp == null)
					dp= _Parameters["@Header"] as IDbDataParameter;
				// Then check to see if the Url value is a parameter?
				if (dp == null)
					dp = _Parameters[_Header] as IDbDataParameter;
				if (dp != null)
				{
					string tf = dp.Value as string;
					if (tf == null)
						return false;
					tf = tf.ToLower();
					return (tf == "true" || tf == "yes");
				}
				return _Header=="yes"? true: false;	// the value must be a constant
			}
			set {_Header = value? "yes": "no";}
		}

		#region IDbCommand Members

		public void Cancel()
		{
			throw new NotImplementedException("Cancel not implemented");
		}

		public void Prepare()
		{
			return;			// Prepare is a noop
		}

		public System.Data.CommandType CommandType
		{
			get
			{
				throw new NotImplementedException("CommandType not implemented");
			}
			set
			{
				throw new NotImplementedException("CommandType not implemented");
			}
		}

		public IDataReader ExecuteReader(System.Data.CommandBehavior behavior)
		{
			CommandBehaviorSupport.Validate(behavior);
			return new TxtDataReader(behavior, _tc, this);
		}

		IDataReader System.Data.IDbCommand.ExecuteReader()
		{
			return ExecuteReader(System.Data.CommandBehavior.SingleResult);
		}

		public object ExecuteScalar()
		{
			throw new NotImplementedException("ExecuteScalar not implemented");
		}

		public int ExecuteNonQuery()
		{
			throw new NotImplementedException("ExecuteNonQuery not implemented");
		}

		public int CommandTimeout
		{
			get
			{
				return 0;
			}
			set
			{
				throw new NotImplementedException("CommandTimeout not implemented");
			}
		}

		public IDbDataParameter CreateParameter()
		{
			return new TxtDataParameter();
		}

		public IDbConnection Connection
		{
			get
			{
				return this._tc;
			}
			set
			{
				throw new NotImplementedException("Setting Connection not implemented");
			}
		}

		public System.Data.UpdateRowSource UpdatedRowSource
		{
			get
			{
				throw new NotImplementedException("UpdatedRowSource not implemented");
			}
			set
			{
				throw new NotImplementedException("UpdatedRowSource not implemented");
			}
		}

		public string CommandText
		{
			get
			{
				return this._cmd;
			}
			set
			{
				// Parse the command string for keyword value pairs separated by ';'
				string[] args = value.Split(';');
				string url=null;
				string[] columns = null;
				string header=null;
				char separator='\0';
				foreach(string arg in args)
				{
					string[] param = arg.Trim().Split('=');
					if (param == null || param.Length != 2)
						continue;
					string key = param[0].Trim().ToLower();
					string val = param[1];
					switch (key)
					{
						case "url":
						case "file":
							url = val;
							break;
						case "columns":
							// column list is separated by ','
							columns = val.Trim().Split(',');
							break;
						case "header":
							header = val.Trim().ToLower();
							if (header == "yes" || header == "no")
							{}
							else if (header == "true")
								header = "yes";
							else if (header == "false")
								header = "no";
							else
								throw new ArgumentException(string.Format("Invalid Header {0}; should be 'yes' 'no'.", val));
							break;
						case "separator":
							if (val.Length != 1)
								throw new ArgumentException(string.Format("Invalid Separator character '{0}'; should be only a single character.", val));
							separator = val[0];
							break;
						default:
							throw new ArgumentException(string.Format("{0} is an unknown parameter key", param[0]));
					}
				}
				// User must specify both the url and the RowsXPath
				if (url == null)
					throw new ArgumentException("CommandText requires a 'Url=' parameter.");
				_cmd = value;
				_Url = url;
				_Columns = columns;
				_Separator = separator;
				_Header = header;
			}
		}

		public IDataParameterCollection Parameters
		{
			get
			{
				return _Parameters;
			}
		}

		public IDbTransaction Transaction
		{
			get
			{
				throw new NotImplementedException("Transaction not implemented");
			}
			set
			{
				throw new NotImplementedException("Transaction not implemented");
			}
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			// nothing to dispose of
		}

		#endregion
	}
}
