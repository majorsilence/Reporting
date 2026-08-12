
using System;
using System.Data;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Net;

namespace Majorsilence.Reporting.Data
{
	/// <summary>
	/// LogDataReader handles reading log files
	/// </summary>
	public class LogDataReader : IDataReader
	{
		LogConnection _lconn;
		LogCommand _lcmd;
		System.Data.CommandBehavior _behavior;
		Hashtable _CompressStrings;	// we keep hashtable of strings so that we can reuse to save memory
									// most web sites won't have huge duplicates.

		MultipleStreamReader _sr;	// StreamReader for _Url
		string _Domain;				// domain name from LogCommand
		string _IndexFile;			// index file name
		object[] _Data;				// data values of the columns

		// column information: is fixed for all instances
		// names of all the columns
		static string[] _Names= new string[] {"host","client_identifier","user","datetime",
												 "request_cmd","request_url","request_type", "request_parameters","status_code",
												 "bytes", "referrer", "user_agent", "cookie"};
		// types of all the columns
		static Type _dttype = DateTime.MinValue.GetType();		// work variable for getting the type
		static Type _tstring = "".GetType();
		static Type _dtype = Double.MinValue.GetType();
		static Type[] _Types = new Type[] {_tstring,_tstring,_tstring,_dttype,
											  _tstring,_tstring,_tstring,_tstring,_tstring,
											  _dtype,_tstring,_tstring,_tstring};
		const int DATETIME_FIELD=3;	// the date time field
		const int REQUEST_FIELD=4;	// the request field
		const int BYTES_FIELD=9;	// the bytes field


		public LogDataReader(System.Data.CommandBehavior behavior, LogConnection conn, LogCommand cmd)
		{
			_lconn = conn;
			_lcmd = cmd;
			_behavior = behavior;
			_CompressStrings = new Hashtable(10000);		// compress strings to save memory
			
			string fname = _lcmd.Url;
			_Domain = _lcmd.Domain;
			if (_Domain == null)
				_Domain = "";
			_IndexFile = _lcmd.IndexFile;
			if (_IndexFile == null)
				_IndexFile = "";

			if (!CommandBehaviorSupport.IsSchemaOnly(behavior))
				_sr = new MultipleStreamReader(_lcmd.Url);	// get the main stream

			_Data = new object[_Names.Length];			// allocate enough room for data
		}

		#region IDataReader Members

		public int RecordsAffected
		{
			get
			{
				return 0;
			}
		}

		public bool IsClosed
		{
			get
			{
				return _sr == null;
			}
		}

		public bool NextResult()	   
		{
			return false;
		}

		public void Close()
		{
			_CompressStrings = null;
			if (_sr != null)
			{
				_sr.Close();
				_sr = null;
			}
			_Data = null;
		}

		public bool Read()
		{
			if (this._sr == null)
				return false;

			// read a line of the log
			string line = _sr.ReadLine();
			if (line == null)
				return false;

			// obtain the data from each column and put the data array
			Lexer l = new Lexer(new StringReader(line));
			LexTokenList ll = l.Lex();
			int ci=0;					// start at first column
			if (ll.Count > 11)
				ci = 0;
			foreach (LexToken lt in ll)
			{
				if (ci >= _Data.Length || lt.Type == LexTokenTypes.EOF)
					break;
				
				if (ci == DATETIME_FIELD)
				{
					_Data[ci] =GetDateTime(lt.Value);
				}
				else if (ci == REQUEST_FIELD)
				{	// break the request into multiple fields; command, url, http type
					string[] reqs = lt.Value.Split(' ');

					string req_cmd=null;
					string req_url=null;
					string req_type=null;
					string req_parameters=null;

					if (reqs == null)
					{}
					else if (reqs.Length >= 3)
					{
						req_cmd = reqs[0];
						req_url = reqs[1];
						req_type = reqs[2];
					}
					else if (reqs.Length == 2)
					{
						req_cmd = reqs[0];
						req_url = reqs[1];
					}
					else if (reqs.Length == 1)
						req_url = reqs[0];

					if (req_url != null)
					{
						string [] up = req_url.Split('?');
						if (up.Length > 1)
						{
							req_url = up[0];
							req_parameters = up[1];
						}
					}
					_Data[ci++] = req_type;
					if (req_url != null && req_url.Length > 0)
					{
						if (req_url[0] == '/')
							req_url = _Domain + req_url;
						if (req_url[req_url.Length-1] == '/')
							req_url += _IndexFile;
					}
					_Data[ci++] = CompressString(req_url);
					_Data[ci++] = req_type == "HTTP/1.1"? "HTTP/1.1": CompressString(req_type);
					_Data[ci++] = req_parameters;

					continue;
				}
				else if (ci == BYTES_FIELD)
				{
					double v=0;
					if (lt.Value.Length == 0 || lt.Value == "-")
					{}
					else
					{
						try
						{
							v = Convert.ToDouble(lt.Value);
						}
						catch
						{
						}
					}
					_Data[ci] = v;
				}
				else
					_Data[ci] = CompressString(lt.Value);
				ci++;					// go to next column
			}

			while (ci < _Data.Length)
				_Data[ci++] = null;

			return true;
		}

		string CompressString(string v)
		{
			if (v == null)
				return null;
			string sv = _CompressStrings[v] as string;
			if (sv != null)
				return sv;
			_CompressStrings.Add(v, v);
			return v;
		}

		object GetDateTime(string v)
		{
			object result;
			if (v.Length != 26)
				return null;
			try 
			{
				string dd = v.Substring(0,2);	// the day of the month
				string MMM = v.Substring(3,3);	// the month
				int month=1;
				switch (MMM.ToLower())
				{
					case "jan": month=1; break;
					case "feb": month=2; break;
					case "mar": month=3; break;
					case "apr": month=4; break;
					case "may": month=5; break;
					case "jun": month=6; break;
					case "jul": month=7; break;
					case "aug": month=8; break;
					case "sep": month=9; break;
					case "oct": month=10; break;
					case "nov": month=11; break;
					case "dec": month=12; break;
					default:
						break;
				}
				string yyyy = v.Substring(7,4);	// the year
				string hh = v.Substring(12,2);	// the hour
				string mm = v.Substring(15,2);	// the minute
				string ss = v.Substring(18,2);	// the seconds
				bool bPlus = v[21] == '+';
				int thh = Convert.ToInt32(v.Substring(22,2));	// the time zone (hh)
				int tmm = Convert.ToInt32(v.Substring(24,2));	// the time zone (mm)
				int tzdiff = thh * 60 + tmm;	// time zone difference in minutes
				if (!bPlus)
					tzdiff = - tzdiff;
				DateTime dt = 
					new DateTime(Convert.ToInt32(yyyy),
					month,
					Convert.ToInt32(dd),
					Convert.ToInt32(hh),
					Convert.ToInt32(mm),
					Convert.ToInt32(ss), 0);
				result = dt.AddMinutes(tzdiff);	
			}
			catch
			{ 
				result = null;
			}
			return result;
		}
	
		public int Depth
		{
			get
			{
				return 0;
			}
		}

		public DataTable GetSchemaTable()
		{
			return null;
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			this.Close();
		}

		#endregion

		#region IDataRecord Members

		public int GetInt32(int i)
		{
			return Convert.ToInt32(_Data[i]);
		}

		public object this[string name]
		{
			get
			{
				int ci = this.GetOrdinal(name);
				return _Data[ci];
			}
		}

		object System.Data.IDataRecord.this[int i]
		{
			get
			{
				return _Data[i];
			}
		}

		public object GetValue(int i)
		{
			return _Data[i];
		}

		public bool IsDBNull(int i)
		{
			return _Data[i] == null;
		}

		public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
		{
			throw new NotImplementedException("GetBytes not implemented.");
		}

		public byte GetByte(int i)
		{
			return Convert.ToByte(_Data[i]);
		}

		public Type GetFieldType(int i)
		{
			return _Types[i];
		}

		public decimal GetDecimal(int i)
		{
			return Convert.ToDecimal(_Data[i]);
		}

		public int GetValues(object[] values)
		{
			int i;
			for (i=0; i < values.Length; i++)
			{
				values[i] = i >= _Data.Length? System.DBNull.Value: _Data[i];
			}

			return Math.Min(values.Length, _Data.Length);
		}

		public string GetName(int i)
		{
			return _Names[i] as string;
		}

		public int FieldCount
		{
			get
			{
				return _Data.Length;
			}
		}

		public long GetInt64(int i)
		{
			return Convert.ToInt64(_Data[i]);
		}

		public double GetDouble(int i)
		{
			return Convert.ToDouble(_Data[i]);
		}

		public bool GetBoolean(int i)
		{
			return Convert.ToBoolean(_Data[i]);
		}

		public Guid GetGuid(int i)
		{
			throw new NotImplementedException("GetGuid not implemented.");
		}

		public DateTime GetDateTime(int i)
		{
			return Convert.ToDateTime(_Data[i]);
		}

		public int GetOrdinal(string name)
		{
			int ci=0;
			// do case sensitive lookup
			foreach (string cname in _Names)
			{
				if (cname == name)
					return ci;
				ci++;
			}

			// do case insensitive lookup
			ci=0;
			name = name.ToLower();
			foreach (string cname in _Names)
			{
				if (cname.ToLower() == name)
					return ci;
				ci++;
			}

			throw new ArgumentException(string.Format("Column '{0}' not known.", name));
		}

		public string GetDataTypeName(int i)
		{
			Type t = _Types[i] as Type;
			return t.ToString();
		}

		public float GetFloat(int i)
		{
			return Convert.ToSingle(_Data[i]);
		}

		public IDataReader GetData(int i)
		{
			throw new NotImplementedException("GetData not implemented.");
		}

		public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
		{
			throw new NotImplementedException("GetChars not implemented.");
		}

		public string GetString(int i)
		{
			return Convert.ToString(_Data[i]);
		}

		public char GetChar(int i)
		{
			return Convert.ToChar(_Data[i]);
		}

		public short GetInt16(int i)
		{
			return Convert.ToInt16(_Data[i]);
		}

		#endregion
	}
}
