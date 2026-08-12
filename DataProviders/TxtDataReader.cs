
using System;
using System.Data;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Data
{
    /// <summary>
    /// TxtDataReader handles reading txt files
    /// </summary>
    public class TxtDataReader : IDataReader
    {
        TxtConnection _tconn;
        TxtCommand _tcmd;
        System.Data.CommandBehavior _behavior;

        StreamReader _sr;           // StreamReader for _Url
                                    // column information
        object[] _Data;             // data values of the columns
        bool bFirstRow;             // indicates that _Data has been filled with data of the 1st row
        string[] _Names;            // names of the columns
        Type[] _Types;              // types of the columns

        public TxtDataReader(System.Data.CommandBehavior behavior, TxtConnection conn, TxtCommand cmd)
        {
            bFirstRow = false;
            _tconn = conn;
            _tcmd = cmd;
            _behavior = behavior;

            string fname = _tcmd.Url;
            bool header = _tcmd.Header;
            char separator = _tcmd.Separator;

            // HACK: async
            Task.Run(async () => _sr = await GetStream()).GetAwaiter().GetResult();             // get the main stream

            Type tstring = "".GetType();

            LexTokenList ll = GetLine();
            int colcount = ll == null ? 0 : ll.Count - 1;       // don't count the end of line
            _Names = _tcmd.Columns;
            if (colcount == 0)
            {
                _sr.Close();
                _sr = null;
                if (_Names == null)
                    return;
                _Types = new Type[_Names.Length];
                for (int ci = 0; ci < _Types.Length; ci++)
                    _Types[ci] = tstring;
                return;
            }

            if (_Names != null && _Names.Length != colcount)
                throw new Exception(string.Format("{0} column names specified but {1} columns found.", _Names.Length, colcount));

            if (header)
            {
                if (_Names == null)
                {   // uses the first row as the names of the columns
                    _Names = new string[colcount];
                    int ci = 0;
                    foreach (LexToken lt in ll)
                    {
                        if (lt.Type == LexTokenTypes.EOF)
                            break;
                        _Names[ci++] = lt.Value;
                    }
                }
                ll = GetLine();
            }
            else if (_Names == null)
            {   // just name the columns 'column1', 'column2', ...
                _Names = new string[colcount];
                for (int ci = 0; ci < _Names.Length; ci++)
                    _Names[ci] = "column" + (ci + 1).ToString();
            }

            _Data = new object[_Names.Length];          // allocate enough room for data
            _Types = new Type[_Names.Length];
            if (ll != null)         // we have a datarow
            {
                bFirstRow = true;
                // loop thru determining the types of all data
                int ci = 0;
                foreach (LexToken lt in ll)
                {
                    if (ci >= _Types.Length || lt.Type == LexTokenTypes.EOF)
                        break;
                    _Types[ci++] = GetTypeOfString(lt.Value);
                }
                FillData(ll);
            }
            else
            {   // no first row! assume all the column types are string
                for (int ci = 0; ci < _Types.Length; ci++)
                    _Types[ci] = tstring;
            }

            if (CommandBehaviorSupport.IsSchemaOnly(behavior))
            {
                _sr.Close();
                _sr = null;
            }

        }

        void FillData(LexTokenList ll)
        {
            Type stype = "".GetType();
            int ci = 0;
            foreach (LexToken lt in ll)
            {
                if (ci >= _Data.Length || lt.Type == LexTokenTypes.EOF)
                    break;
                // Optimize for no conversion
                if (_Types[ci] == stype || lt.Value == null)
                {
                    _Data[ci++] = lt.Value;
                    continue;
                }
                // We need to do conversion
                try
                {   // in case of conversion error 
                    _Data[ci] = Convert.ChangeType(lt.Value, _Types[ci]);
                }
                catch
                {
                    _Data[ci] = null;
                }
                ci++;
            }

            while (ci < _Data.Length)
                _Data[ci++] = null;
        }

        LexTokenList GetLine()
        {
            if (_sr == null)
                return null;

            // read a line of the log
            string line = _sr.ReadLine();
            if (line == null)
                return null;

            // obtain the data from each column and put the data array
            Lexer l = new Lexer(new StringReader(line));
            l.SeparateDatetime = false;
            l.SeparatorChar = _tcmd.Separator;
            LexTokenList ll = l.Lex();
            return ll;
        }

        Type GetTypeOfString(string v)
        {
            return "".GetType();                // just assume string for now
        }

        async Task<StreamReader> GetStream()
        {
            string fname = _tcmd.Url;
            Stream strm = null;

            if (fname.StartsWith("http:") || fname.StartsWith("https:"))
            {
                using (HttpClient client = new HttpClient())
                {
                    client.AddMajorsilenceReportingUserAgent();
                    HttpResponseMessage response = await client.GetAsync(fname);
                    response.EnsureSuccessStatusCode();
                    strm = await response.Content.ReadAsStreamAsync();
                }
            }
            else if (fname.StartsWith("file:"))
            {
                strm = new FileStream(fname.Substring(5), System.IO.FileMode.Open, FileAccess.Read);
            }
            else
            {
                strm = new FileStream(fname, System.IO.FileMode.Open, FileAccess.Read);
            }

            _sr = new StreamReader(strm);

            return _sr;
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
            if (_sr != null)
            {
                _sr.Close();
                _sr = null;
            }
            _Data = null;
            _Names = null;
            _Types = null;
        }

        public bool Read()
        {
            if (this._sr == null || _Data == null)
                return false;

            // Do we already have a row primed?
            if (bFirstRow)
            {   // yes; but no more
                bFirstRow = false;
                return true;
            }

            // read a line of the log
            LexTokenList ll = this.GetLine();
            if (ll == null)
                return false;

            // take line and fill the data
            this.FillData(ll);

            return true;
        }

        object GetDateTime(string v)
        {
            object result;
            if (v.Length != 26)
                return null;
            try
            {
                string dd = v.Substring(0, 2);  // the day of the month
                string MMM = v.Substring(3, 3); // the month
                int month = 1;
                switch (MMM.ToLower())
                {
                    case "jan":
                        month = 1;
                        break;
                    case "feb":
                        month = 2;
                        break;
                    case "mar":
                        month = 3;
                        break;
                    case "apr":
                        month = 4;
                        break;
                    case "may":
                        month = 5;
                        break;
                    case "jun":
                        month = 6;
                        break;
                    case "jul":
                        month = 7;
                        break;
                    case "aug":
                        month = 8;
                        break;
                    case "sep":
                        month = 9;
                        break;
                    case "oct":
                        month = 10;
                        break;
                    case "nov":
                        month = 11;
                        break;
                    case "dec":
                        month = 12;
                        break;
                    default:
                        break;
                }
                string yyyy = v.Substring(7, 4);    // the year
                string hh = v.Substring(12, 2); // the hour
                string mm = v.Substring(15, 2); // the minute
                string ss = v.Substring(18, 2); // the seconds
                bool bPlus = v[21] == '+';
                int hhmm = Convert.ToInt32(v.Substring(22, 4)); // the time zone
                if (!bPlus)
                    hhmm = -hhmm;
                DateTime dt =
                    new DateTime(Convert.ToInt32(yyyy),
                    month,
                    Convert.ToInt32(dd),
                    Convert.ToInt32(hh),
                    Convert.ToInt32(mm),
                    Convert.ToInt32(ss), 0);
                result = dt.AddHours(hhmm / 100.0);
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
            return this._Types[i] as Type;
        }

        public decimal GetDecimal(int i)
        {
            return Convert.ToDecimal(_Data[i]);
        }

        public int GetValues(object[] values)
        {
            int i;
            for (i = 0; i < values.Length; i++)
            {
                values[i] = i >= _Data.Length ? System.DBNull.Value : _Data[i];
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
            int ci = 0;
            // do case sensitive lookup
            foreach (string cname in _Names)
            {
                if (cname == name)
                    return ci;
                ci++;
            }

            // do case insensitive lookup
            ci = 0;
            foreach (string cname in _Names)
            {
                if (String.Compare(cname, name, true) == 0)
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
