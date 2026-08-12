
using System;
using System.Xml;
using System.Xml.XPath;
using System.Data;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;

namespace Majorsilence.Reporting.Data
{
	/// <summary>
    /// Summary description for iTunesDataReader.
	/// </summary>
	public class iTunesDataReader : IDataReader
	{
		iTunesConnection _xconn;
		iTunesCommand _xcmd;
		System.Data.CommandBehavior _behavior;
		// xpath 
		XPathDocument _xpd;			// the document		
		XPathNavigator _xpn;		// the navigator
		XPathNodeIterator _xpni;	// the main iterator
		XmlNamespaceManager _nsmgr;	// name space manager
		ListDictionary _NameSpaces;		// names spaces used in xml document

		// column information
		object[] _Data;				// data values of the columns
        static readonly string[] _Names = new string[] {
            "Track_ID",
			"Name",
			"Artist",
			"Composer",
			"Album",
			"Album Artist",
			"Genre",
            "Category",
			"Kind",
			"Size",
			"Total_Time",
			"Track_Number",
			"Year",
			"Date_Modified",
			"Date_Added",
            "Beats_Per_Minute",
			"Bit_Rate",
			"Sample_Rate",
			"Comments",
			"Skip_Count",
			"Skip_Date",
			"Artwork_Count",
			"Persistent_ID",
			"Track_Type",
			"Location",
			"File_Folder_Count",
			"Library_Folder_Count",
            "Has_Video",
            "Movie",
            "Play_Count",
            "Play_Date",
            "Play_Date_UTC",
            "Disc_Number",
            "Disc_Count",
            "Compilation",
            "Track_Count"
            
};
        // types of all the columns
        static readonly Type _dttype = DateTime.MinValue.GetType();		// work variable for getting the type
        static readonly Type _stype = "".GetType();
        static readonly Type _btype = true.GetType();
        static readonly Type _itype = int.MaxValue.GetType();
        static readonly Type _ltype = long.MaxValue.GetType();
        static readonly Type _dtype = Double.MinValue.GetType();
        static readonly Type[] _Types = new Type[] 
{
          /*  "Track_ID" */ _itype,
			/* Name */ _stype,
			/* Artist */ _stype,
			/* Composer */ _stype,
			/* Album */ _stype,
			/* Album Artist */ _stype,
			/* Genre */ _stype,
            /* Category */ _stype, 
			/* Kind */ _stype,
			/* Size */ _itype,
			/* Total_Time */ _itype,
			/* Track_Number */ _itype,
			/* Year */ _itype,
			/* Date_Modified */ _dttype,
			/* Date_Added */ _dttype,
            /* Beats_Per_Minute */ _itype,
			/* Bit_Rate */ _itype,
			/* Sample_Rate */ _itype,
			/* Comments */ _stype,
			/* Skip_Count */ _itype,
			/* Skip_Date */ _dttype,
			/* Artwork_Count */ _itype,
			/* Persistent_ID */ _stype,
			/* Track_Type */ _stype,
			/* Location */ _stype,
			/* File_Folder_Count */ _itype,
			/* Library_Folder_Count */ _itype,
            /* Has_Video */ _btype,
            /* Movie */ _btype,
            /* Play_Count */ _itype,
            /* Play_Date */ _ltype,         /* Play_Date will overflow an integer */
            /* Play_Date_UTC */ _dttype,
            /* Disc_Number */ _itype,
            /* Disc_Count */ _itype,
            /* Compilation */ _btype,
            /* Track_Count */ _itype
};

        public iTunesDataReader(System.Data.CommandBehavior behavior, iTunesConnection conn, iTunesCommand cmd)
		{
			_xconn = conn;
			_xcmd = cmd;
			_behavior = behavior;

            _Data = new object[_Names.Length];			// allocate enough room for data

            if (CommandBehaviorSupport.IsSchemaOnly(behavior))
                return;

            // create an iterator to the selected rows
            _xpd = new XPathDocument(_xconn.File);
            _xpn = _xpd.CreateNavigator();
            _xpni = _xpn.Select("/plist/dict");	// select the rows
            _NameSpaces = new ListDictionary();

			// Now determine the actual structure of the row depending on the command
			switch (_xcmd.Table)
			{
				case "Tracks":
                default:
                    _xpni = GetTracksColumns();
					break;
			}

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

        XPathNodeIterator GetTracksColumns()
		{
            /*  this routine figures out based on first row in file; the problem is not
             * all rows contain all the fields.   Now fields are hard coded. 
            XPathNodeIterator ti = PositionToTracks();
            while (ti.MoveNext())
            {
                if (ti.Current.Name != "key")
                    continue;
                if (AddName(ti.Current.Value))  // when duplicate we've gone too far
                    break;
                ti.MoveNext();
                Type t;
                switch (ti.Current.Name)
                {
                    case "integer":
                        t = long.MaxValue.GetType();
                        break;
                    case "string":
                        t = string.Empty.GetType();
                        break;
                    case "real":
                        t = Double.MaxValue.GetType();
                        break;
                    case "true":
                    case "false":
                        t = true.GetType();
                        break;
                    case "date":
                        t = DateTime.MaxValue.GetType();
                        break;
                    default:
                        t = string.Empty.GetType();
                        break;
                }
                _Types.Add(t);
            }
            _Names.TrimExcess();        // no longer need extra space
             */ 
            return PositionToTracks();  // return iterator to first row
		}

        XPathNodeIterator PositionToTracks()
        {
            //go to the first row to get info 
            XPathNodeIterator temp_xpni = _xpni.Clone();	// temporary iterator for determining columns
            temp_xpni.MoveNext();

            XPathNodeIterator ni = temp_xpni.Current.Select("*");
            while (ni.MoveNext())
            {
                if (ni.Current.Name != "key")
                    continue;
                if (ni.Current.Value == "Tracks")
                {
                    ni.MoveNext();
                    if (ni.Current.Name != "dict")  // next item should be "dict"
                        break;
                    string sel = "dict/*";
                    XPathNodeIterator ti = ni.Current.Select(sel);
                    return ti;
                }
            }
            return null;
        }

		// adds name to array; return true when duplicate
        //bool AddName(string name)
        //{
        //    string wname = name.Replace(' ', '_');
        //    if (_Names.ContainsKey(wname))
        //        return true;

        //    _Names.Add(wname, _Names.Count);
        //    return false;
        //}

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
			
		}

		public bool Read()
		{
            if (_xpni == null)
                return false;

            XPathNodeIterator ti = _xpni;

            Hashtable ht = new Hashtable(_Names.Length);
            
            // clear out previous values;  previous row might have values this row doesn't
            for (int i = 0; i < _Data.Length; i++)
                _Data[i] = null;

            XPathNodeIterator save_ti = ti.Clone();
            bool rc = false;
            while (ti.MoveNext())
            {
                if (ti.Current.Name != "key")
                    continue;
                string name = ti.Current.Value.Replace(' ', '_');
                // we only know we're on the next row when a column repeats
                if (ht.Contains(name))
                {
                    break;
                }
                ht.Add(name, name);

                int ix;
                try
                {
                    ix = this.GetOrdinal(name);
                    rc = true;                      // we know we got at least one column value
                }
                catch   // this isn't a know column; skip it
                {
                    ti.MoveNext();
                    save_ti = ti.Clone();
                    continue;                   // but keep trying
                }


                ti.MoveNext();
                save_ti = ti.Clone();
                try
                {
                    switch (ti.Current.Name)
                    {
                        case "integer":
                            if (_Names[ix] == "Play_Date")  // will overflow a long
                                _Data[ix] = ti.Current.ValueAsLong;
                            else
                                _Data[ix] = ti.Current.ValueAsInt;
                            break;
                        case "string":
                            _Data[ix] = ti.Current.Value;
                            break;
                        case "real":
                            _Data[ix] = ti.Current.ValueAsDouble;
                            break;
                        case "true":
                            _Data[ix] = true;
                            break;
                        case "false":
                            _Data[ix] = false;
                            break;
                        case "date":
                            _Data[ix] = ti.Current.ValueAsDateTime;
                            break;
                        default:
                            _Data[ix] = ti.Current.Value;
                            break;
                    }
                }
                catch { _Data[ix] = null; }
            }

            _xpni = save_ti;
            return rc;
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
            return _Names[i];
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
			// do case sensitive lookup
            int ci = 0;
            foreach (string cname in _Names)
            {
                if (cname == name)
                    return ci;
                ci++;
            }

			// do case insensitive lookup
			ci=0;
			foreach (string cname in _Names)
			{
				if (string.Compare(cname, name, true) == 0)
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
