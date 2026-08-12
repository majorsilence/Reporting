
using System;
using System.Xml;
using System.Data;
using System.Collections;

namespace Majorsilence.Reporting.Data
{
	/// <summary>
	/// LogCommand allows specifying the command for the web log.
	/// </summary>
	public class LogCommand : IDbCommand
	{
		LogConnection _lc;			// connection we're running under
		string _cmd;				// command to execute
		// parsed constituents of the command
		string _Url;				// url of the file 
		string _Domain;				
		string _IndexFile;			// name of the index file; e.g. what's the default page when none (e.g. index.html)
		DataParameterCollection _Parameters = new DataParameterCollection();

		public LogCommand(LogConnection conn)
		{
			_lc = conn;
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

		internal string Domain
		{
			get 
			{
				// Check to see if "Domain" or "@Domain" is a parameter
				IDbDataParameter dp= _Parameters["Domain"] as IDbDataParameter;
				if (dp == null)
					dp= _Parameters["@Domain"] as IDbDataParameter;
				// Then check to see if the Domain value is a parameter?
				if (dp == null)
					dp = _Parameters[_Domain] as IDbDataParameter;
				return (dp == null || dp.Value == null)? _Domain: dp.Value.ToString();
			}
			set {_Domain = value;}
		}

		internal string IndexFile
		{
			get 
			{
				// Check to see if "IndexFile" or "@IndexFile" is a parameter
				IDbDataParameter dp= _Parameters["IndexFile"] as IDbDataParameter;
				if (dp == null)
					dp= _Parameters["@IndexFile"] as IDbDataParameter;
				// Then check to see if the IndexFile value is a parameter?
				if (dp == null)
					dp = _Parameters[_IndexFile] as IDbDataParameter;
				return (dp == null || dp.Value == null)? _IndexFile: dp.Value.ToString();
			}
			set {_IndexFile = value;}
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
			return new LogDataReader(behavior, _lc, this);
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
			return new LogDataParameter();
		}

		public IDbConnection Connection
		{
			get
			{
				return this._lc;
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
				string domain=null;
				string indexfile=null;
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
						case "domain":
							domain = val;
							break;
						case "indexfile":
							indexfile = val;
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
				_Domain = domain;
				_IndexFile = indexfile;
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
