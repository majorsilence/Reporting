
using System;
using System.Xml;
using System.Data;
using System.Collections;

namespace Majorsilence.Reporting.Data
{
	/// <summary>
	/// WebServiceCommand 
	/// </summary>
	public class WebServiceCommand : IDbCommand
	{
		WebServiceConnection _wsc;	// connection we're running under
		string _cmd;				// command to execute
		int _Timeout;				// timeout limit on invoking webservice (only applies to invoking service)
		// parsed constituents of the command
		string _Url;				// url of the wsdl file 
		string _Service;			// service name
		string _Operation;			// operation name
		string _RepeatField;		// Specifies the name of the array that should be repeated (only 1 can be)
		ArrayList _Columns;			// Columns specified for the request
		DataParameterCollection _Parameters = new DataParameterCollection();

		public WebServiceCommand(WebServiceConnection conn)
		{
			_wsc = conn;
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

		internal string Operation
		{
			get 
			{
				IDbDataParameter dp= _Parameters["Operation"] as IDbDataParameter;
				if (dp == null)
					dp= _Parameters["@Operation"] as IDbDataParameter;
				// Then check to see if the Operation value is a parameter?
				if (dp == null)
					dp = _Parameters[_Operation] as IDbDataParameter;

				return (dp != null && dp.Value != null)? dp.Value.ToString(): _Operation;	// don't pass null; pass existing value
			}
			set {_Operation = value;}
		}

		internal string RepeatField
		{
			get 
			{
				IDbDataParameter dp= _Parameters["RepeatField"] as IDbDataParameter;
				if (dp == null)
					dp= _Parameters["@RepeatField"] as IDbDataParameter;
				// Then check to see if the RepeatField value is a parameter?
				if (dp == null)
					dp = _Parameters[_RepeatField] as IDbDataParameter;

				return (dp != null && dp.Value != null)? null: _RepeatField;
			}
			set {_RepeatField = value;}
		}

		internal string Service
		{
			get 
			{
				IDbDataParameter dp= _Parameters["Service"] as IDbDataParameter;
				if (dp == null)
					dp= _Parameters["@Service"] as IDbDataParameter;
				// Then check to see if the RowsXPath value is a parameter?
				if (dp == null)
					dp = _Parameters[_Service] as IDbDataParameter;

				return (dp != null && dp.Value != null)? dp.Value.ToString(): _Service;	// don't pass null; pass existing value
			}
			set {_Service = value;}
		}

		internal ArrayList Columns
		{
			get {return _Columns;}
			set {_Columns = value;}
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
			return new WebServiceDataReader(behavior, _wsc, this);
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
				return _Timeout;
			}
			set
			{
				_Timeout = value;
			}
		}

		public IDbDataParameter CreateParameter()
		{
			return new WebServiceDataParameter();
		}

		public IDbConnection Connection
		{
			get
			{
				return this._wsc;
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
				string operation=null;
				string service=null;
				string[] columns=null;
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
						case "service":
							service = val;
							break;
						case "operation":
							operation = val;
							break;
						case "columns":
							// column list is separated by ','
							columns = val.Trim().Split(',');
							break;
						default:
							throw new ArgumentException(string.Format("{0} is an unknown parameter key", param[0]));
					}
				}
				// User must specify both the url and the RowsXPath
				if (url == null || operation == null || service == null)
					throw new ArgumentException("CommandText requires 'Url', 'Service', and 'Operation' parameters.");
				_cmd = value;
				_Url = url;
				_Operation = operation;
				_Service = service;
				if (columns != null)
					_Columns = new ArrayList(columns);
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
