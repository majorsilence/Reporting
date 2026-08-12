
using System;
using System.Xml;
using System.Data;
using System.Collections;

namespace Majorsilence.Reporting.Data
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class XmlCommand : IDbCommand
	{
		XmlConnection _xc;			// connection we're running under
		string _cmd;				// command to execute
		// parsed constituents of the command
		string _Url;				// url of the xml file 
		string _RowsXPath;			// XPath specifying rows selection
		ArrayList _ColumnsXPath;		// XPath for each column
		string _Type;				// type of request
		DataParameterCollection _Parameters = new DataParameterCollection();

		public XmlCommand(XmlConnection conn)
		{
			_xc = conn;
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

		internal string RowsXPath
		{
			get 
			{
				IDbDataParameter dp= _Parameters["RowsXPath"] as IDbDataParameter;
				if (dp == null)
					dp= _Parameters["@RowsXPath"] as IDbDataParameter;
				// Then check to see if the RowsXPath value is a parameter?
				if (dp == null)
					dp = _Parameters[_RowsXPath] as IDbDataParameter;
				if (dp != null)
					return dp.Value != null? dp.Value.ToString(): _RowsXPath;	// don't pass null; pass existing value
				return _RowsXPath;	// the value must be a constant
			}
			set {_RowsXPath = value;}
		}

		internal ArrayList ColumnsXPath
		{
			get {return _ColumnsXPath;}
			set {_ColumnsXPath = value;}
		}

		internal string Type
		{
			get {return _Type;}
			set {_Type = value;}
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
			return new XmlDataReader(behavior, _xc, this);
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
			return new XmlDataParameter();
		}

		public IDbConnection Connection
		{
			get
			{
				return this._xc;
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
				string[] colxpaths=null;
				string rowsxpath=null;
				string type="both";		// assume we want both attributes and elements
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
						case "rowsxpath":
							rowsxpath = val;
							break;
						case "type":
						{
							type = val.Trim().ToLower();
							if (!(type == "attributes" || type == "elements" || type == "both"))
								throw new ArgumentException(string.Format("type '{0}' is invalid.  Must be 'attributes', 'elements' or 'both'", val));
							break;
						}
						case "columnsxpath":
							// column list is separated by ','
							colxpaths = val.Trim().Split(',');
							break;
						default:
							throw new ArgumentException(string.Format("{0} is an unknown parameter key", param[0]));
					}
				}
				// User must specify both the url and the RowsXPath
				if (url == null)
					throw new ArgumentException("CommandText requires a 'Url=' parameter.");
				if (rowsxpath == null)
					throw new ArgumentException("CommandText requires a 'RowsXPath=' parameter.");
				_cmd = value;
				_Url = url;
				_Type = type;
				_RowsXPath = rowsxpath;
				if (colxpaths != null)
					_ColumnsXPath = new ArrayList(colxpaths);
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
