
using System;
using System.Data;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Net;

namespace Majorsilence.Reporting.Data
{
	/// <summary>
	/// FileDirDataReader handles reading log files
	/// </summary>
	public class FileDirDataReader : IDataReader
	{
		FileDirConnection _fdconn;
		FileDirCommand _fdcmd;
		System.Data.CommandBehavior _behavior;

		IEnumerator _ie;			//  enumerator thru rows
		string _FilePattern;		// FilePattern from _fdcmd
		string _DirectoryPattern;	// DirectoryPattern from _fdcmd
		bool _TrimEmpty;			// Directories with no files will be omitted from result set

		// file data
		object[] _Data;				// data values of the columns
		ArrayList _RowData;			// row data
		// column information; this is fixed for all instances
		static string[] _Names;		// names of the columns
		static Type[] _Types;		// types of the columns
		// the location of the columns
		static readonly int COLUMN_NAME=0;
		static readonly int COLUMN_SIZE=1;
		static readonly int COLUMN_CREATIONTIME=2;
		static readonly int COLUMN_LASTACCESSTIME=3;
		static readonly int COLUMN_LASTWRITETIME=4;
		static readonly int COLUMN_ID=5;
		static readonly int COLUMN_PARENTID=6;
		static readonly int COLUMN_ISDIRECTORY=7;
		static readonly int COLUMN_EXTENSION=8;
		static readonly int COLUMN_FULLNAME=9;
		static readonly int COLUMN_COUNT=10;

		static FileDirDataReader()
		{
			// Add the names (fixed for type of DataReader depending on the columns parameter)
			Type dttype = DateTime.MinValue.GetType();		// work variable for getting the type
			Type stype = "".GetType();
			Type itype = int.MinValue.GetType();
			Type ltype = long.MinValue.GetType();
			Type btype = new bool().GetType();
			_Names = new string[COLUMN_COUNT]; 
			_Names[COLUMN_NAME]="Name";
			_Names[COLUMN_SIZE]="Size";
			_Names[COLUMN_CREATIONTIME]="CreationTime";
			_Names[COLUMN_LASTACCESSTIME]="LastAccessTime";
			_Names[COLUMN_LASTWRITETIME]="LastWriteTime";
			_Names[COLUMN_ID]="ID";
			_Names[COLUMN_PARENTID]="ParentID";
			_Names[COLUMN_ISDIRECTORY]="IsDirectory";
			_Names[COLUMN_EXTENSION]="Extension";
			_Names[COLUMN_FULLNAME]="FullName";
		
			_Types = new Type[COLUMN_COUNT];
			_Types[COLUMN_NAME]=stype;
			_Types[COLUMN_SIZE]=ltype;
			_Types[COLUMN_CREATIONTIME]=dttype;
			_Types[COLUMN_LASTACCESSTIME]=dttype;
			_Types[COLUMN_LASTWRITETIME]=dttype;
			_Types[COLUMN_ID]=itype;
			_Types[COLUMN_PARENTID]=itype;
			_Types[COLUMN_ISDIRECTORY]=btype;
			_Types[COLUMN_EXTENSION]=stype;
			_Types[COLUMN_FULLNAME]=stype;
		}

		public FileDirDataReader(System.Data.CommandBehavior behavior, FileDirConnection conn, FileDirCommand cmd)
		{
			_fdconn = conn;
			_fdcmd = cmd;
			_behavior = behavior;
			_FilePattern = _fdcmd.FilePattern;
			_DirectoryPattern = _fdcmd.DirectoryPattern;
			_TrimEmpty = _fdcmd.TrimEmpty;

			_Data = new object[_Names.Length];			// allocate enough room for data

			if (CommandBehaviorSupport.IsSchemaOnly(behavior))
				return;

			string dir = _fdcmd.Directory;
			if (dir == null)
				throw new Exception("Directory parameter must be specified.");

			// Populate the data array
			_RowData = new ArrayList();
			PopulateData(new DirectoryInfo(dir), -1);
			_ie = _RowData.GetEnumerator();
		}

		long PopulateData(DirectoryInfo di, int parent)
		{
			long size=0;

			// Create a new row for this directory
			object[] prow = new object[_Names.Length];
			_RowData.Add(prow);
			int rowcount = _RowData.Count - 1;
			prow[COLUMN_NAME] = di.Name;
			prow[COLUMN_ISDIRECTORY] = true;
			prow[COLUMN_ID] = rowcount;
			prow[COLUMN_PARENTID] = parent >= 0? (object) parent: (object) null;
			prow[COLUMN_CREATIONTIME] = di.CreationTime;
			prow[COLUMN_LASTACCESSTIME] = di.LastAccessTime;
			prow[COLUMN_LASTWRITETIME] = di.LastWriteTime;
			prow[COLUMN_EXTENSION] = di.Extension;
			prow[COLUMN_FULLNAME] = di.FullName;
			
			parent = rowcount;		// set the new parent
			
			FileInfo[] afi = _FilePattern == null? di.GetFiles(): di.GetFiles(_FilePattern);
			foreach (FileInfo fi in afi)
			{	   
				// Create a new row for this file
				object[] row = new object[_Names.Length];
				_RowData.Add(row);
				row[COLUMN_NAME] = fi.Name;
				row[COLUMN_ISDIRECTORY] = false;
				row[COLUMN_ID] = _RowData.Count - 1;
				row[COLUMN_PARENTID] = (object) parent;
				row[COLUMN_CREATIONTIME] = fi.CreationTime;
				row[COLUMN_LASTACCESSTIME] = fi.LastAccessTime;
				row[COLUMN_LASTWRITETIME] = fi.LastWriteTime;
				row[COLUMN_EXTENSION] = fi.Extension;
				row[COLUMN_FULLNAME] = fi.FullName;
				row[COLUMN_SIZE] = fi.Length;
				size += fi.Length;
			}

			DirectoryInfo[] adi = _DirectoryPattern == null? di.GetDirectories(): di.GetDirectories(_DirectoryPattern);
			foreach (DirectoryInfo sdi in adi)
			{
				size += PopulateData(sdi, parent);
			}

			prow[COLUMN_SIZE] = size;

			// If a directory has no files below it we (optionally) can omit the directory as well
			if (_TrimEmpty && parent >= 0 && _RowData.Count - 1 == rowcount)
			{
				_RowData.RemoveAt(rowcount);
			}

			return size;
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
				return _ie == null;
			}
		}

		public bool NextResult()	   
		{
			return false;
		}

		public void Close()
		{
			_ie = null;
			_RowData = null;
			_Data = null;
		}

		public bool Read()
		{
			if (_ie == null || !_ie.MoveNext())
				return false;

			_Data = _ie.Current as object[];

			return true;
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
