
using System;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.XPath;
using System.Data;
using System.Collections;
using System.Collections.Specialized;
using System.IO;

namespace Majorsilence.Reporting.Data
{
	/// <summary>
	/// Summary description for XmlDataReader.
	/// </summary>
	public class XmlDataReader : IDataReader
	{
		XmlConnection _xconn;
		XmlCommand _xcmd;
		System.Data.CommandBehavior _behavior;
		// xpath 
		XPathDocument _xpd;			// the document		
		XPathNavigator _xpn;		// the navigator
		XPathNodeIterator _xpni;	// the main iterator
		XmlNamespaceManager _nsmgr;	// name space manager
		ListDictionary _NameSpaces;		// names spaces used in xml document

		// column information
		object[] _Data;				// data values of the columns
		ArrayList _Names;				// names of the columns
		ArrayList _Types;				// types of the columns

		public XmlDataReader(System.Data.CommandBehavior behavior, XmlConnection conn, XmlCommand cmd)
		{
			_xconn = conn;
			_xcmd = cmd;
			_behavior = behavior;

            // create an iterator to the selected rows
            if (_xcmd.Url.Trim() == "") {
                //Use an empty XML to allow rendering from memory
                _xpd = new XPathDocument(new StringReader("<empty/>"));
            }
            else {
                _xpd = new XPathDocument(_xcmd.Url);
            }
			_xpn =  _xpd.CreateNavigator();
			_xpni =  _xpn.Select(_xcmd.RowsXPath);	// select the rows
			_NameSpaces = new ListDictionary();
			_Names = new ArrayList();
			_Types = new ArrayList();

			// Now determine the actual structure of the row depending on the command
			if (_xcmd.ColumnsXPath != null)
				ColumnsSpecifiedInit();			// xpaths to all columns specified
			else 
			{
				_xcmd.ColumnsXPath = new ArrayList();			// xpath of all columns will simply be the name

				switch (_xcmd.Type)
				{
					case "both":
						ColumnsAttributes();
						ColumnsElements();
						break;
					case "attributes":
						ColumnsAttributes();
						break;
					case "elements":
						ColumnsElements();
						break;
				}
			}
			_Data = new object[_Names.Count];			// allocate enough room for data

			if (_NameSpaces.Count > 0)
			{
				_nsmgr = new XmlNamespaceManager(new NameTable()); 
				foreach (string nsprefix in _NameSpaces.Keys)
				{
					_nsmgr.AddNamespace(nsprefix, _NameSpaces[nsprefix] as string); // setup namespaces
				}
			}
			else
				_nsmgr = null;
		}

		void ColumnsAttributes()
		{
			//go to the first row to get info 
			XPathNodeIterator temp_xpni = _xpni.Clone();	// temporary iterator for determining columns
			temp_xpni.MoveNext();

			XPathNodeIterator ni = temp_xpni.Current.Select("@*");	// select for attributes
			while (ni.MoveNext()) 
			{
				_Types.Add(ni.Current.Value.GetType());
				AddName(ni.Current.Name);
				_xcmd.ColumnsXPath.Add("@" + ni.Current.Name);
			}
		}

		void ColumnsElements()
		{
			//go to the first row to get info 
			XPathNodeIterator temp_xpni = _xpni.Clone();	// temporary iterator for determining columns
			temp_xpni.MoveNext();

			XPathNodeIterator ni = temp_xpni.Current.Select("*"); 
			while (ni.MoveNext()) 
			{
				_Types.Add(ni.Current.Value.GetType());
				AddName(ni.Current.Name);
				_xcmd.ColumnsXPath.Add(ni.Current.Name);
				if (ni.Current.NamespaceURI != String.Empty && 
					ni.Current.Prefix != String.Empty &&
					_NameSpaces[ni.Current.Prefix] == null)
				{
					_NameSpaces.Add(ni.Current.Prefix, ni.Current.NamespaceURI);
				}
			}
		}

		void ColumnsSpecifiedInit()
		{
			XPathNodeIterator temp_xpni = _xpni.Clone();				// temporary iterator for determining columns

			temp_xpni.MoveNext ();
			foreach (string colxpath in _xcmd.ColumnsXPath ) 
			{
				XPathNodeIterator ni = temp_xpni.Current.Select(colxpath); 
				ni.MoveNext ();
				if (ni.Count < 1)
				{	// didn't get anything in the first row; this is ok because 
					//   the element might not exist in the first row.
					_Types.Add("".GetType());	// assume string
					AddName(colxpath);			// just use the name of the path
				}
				else
				{
					_Types.Add(ni.Current.Value.GetType());
					AddName(ni.Current.Name);
				}
			}
		}

		// adds name to array; ensure name is unique in the table
		void AddName(string name)
		{
			int ci=0;
			string wname = name;			
			while (_Names.IndexOf(wname) >= 0)
			{
				wname = name + "_" + (++ci).ToString();
			}
			_Names.Add(wname);
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
				return _xconn.IsOpen;
			}
		}

		public bool NextResult()
		{
			return false;
		}

		public void Close()
		{
			_xpd = null;
			_xpn = null;
			_xpni = null;

			_Data = null;
			_Names = null;
			_Types = null;
		}

		public bool Read()
		{
			if (!_xpni.MoveNext())
				return false;

			// obtain the data from each column
			int ci=0;
			foreach (string colxpath in _xcmd.ColumnsXPath ) 
			{
				XPathNodeIterator ni;
				if (_nsmgr == null)
					ni = _xpni.Current.Select(colxpath); 
				else
				{
					XPathExpression xp = _xpni.Current.Compile(colxpath);
					xp.SetContext(_nsmgr);
					ni = _xpni.Current.Select(xp); 
				}
				ni.MoveNext ();
				_Data[ci++] = ni.Count == 0? null: ni.Current.Value;
			}

			return true;
		}

		public int Depth
		{
			get
			{
				// TODO:  Add XmlDataReader.Depth getter implementation
				return 0;
			}
		}

		public DataTable GetSchemaTable()
		{
			// TODO:  Add XmlDataReader.GetSchemaTable implementation
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

		[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
		[UnconditionalSuppressMessage("Trimming", "IL2073",
		    Justification = "The Type only describes a column's data type; the engine reads its TypeCode and never reflects over its members.")]
		public Type GetFieldType(int i)
		{
			return this._Types[i] as Type;
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
