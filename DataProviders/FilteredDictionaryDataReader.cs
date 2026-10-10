using System;
using System.Diagnostics.CodeAnalysis;
using System.Data;

namespace Majorsilence.Reporting.Data
{
    public class FilteredDictionaryDataReader : IDataReader
    {
        private readonly DictionaryDataReader _innerReader;
        private readonly string[] _columnNames;
        private readonly int[] _columnMap;

        public FilteredDictionaryDataReader(DictionaryDataReader reader, string[] columnNames)
        {
            _innerReader = reader;
            _columnNames = columnNames;

            // Create a mapping from our filtered indices to the inner reader's indices
            _columnMap = new int[_columnNames.Length];
            for (int i = 0; i < _columnNames.Length; i++)
            {
                _columnMap[i] = reader.GetOrdinal(_columnNames[i]);
            }
        }

        // Override field count and name-related methods
        public int FieldCount => _columnNames.Length;
        public string GetName(int i) => _columnNames[i];
        public int GetOrdinal(string name) => Array.IndexOf(_columnNames, name);

        // Map our filtered indices to the inner reader
        public object GetValue(int i) => _innerReader.GetValue(_columnMap[i]);
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
        public Type GetFieldType(int i) => _innerReader.GetFieldType(_columnMap[i]);

        // Override indexers
        public object this[int i] => GetValue(i);
        public object this[string name] => GetValue(GetOrdinal(name));

        // Delegate all other methods to the inner reader
        public bool Read() => _innerReader.Read();
        public void Close() => _innerReader.Close();
        public void Dispose() => _innerReader.Dispose();
        public bool NextResult() => _innerReader.NextResult();
        public int Depth => _innerReader.Depth;
        public bool IsClosed => _innerReader.IsClosed;
        public int RecordsAffected => _innerReader.RecordsAffected;

        public int GetValues(object[] values)
        {
            int count = Math.Min(values.Length, FieldCount);
            for (int i = 0; i < count; i++)
            {
                values[i] = GetValue(i);
            }

            return count;
        }

        // Implement the rest of the interface methods by mapping indices
        public bool IsDBNull(int i) => _innerReader.IsDBNull(_columnMap[i]);
        public bool GetBoolean(int i) => _innerReader.GetBoolean(_columnMap[i]);
        public byte GetByte(int i) => _innerReader.GetByte(_columnMap[i]);

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) =>
            _innerReader.GetBytes(_columnMap[i], fieldOffset, buffer, bufferoffset, length);

        public char GetChar(int i) => _innerReader.GetChar(_columnMap[i]);

        public long GetChars(int i, long fieldOffset, char[] buffer, int bufferoffset, int length) =>
            _innerReader.GetChars(_columnMap[i], fieldOffset, buffer, bufferoffset, length);

        public IDataReader GetData(int i) => _innerReader.GetData(_columnMap[i]);
        public string GetDataTypeName(int i) => _innerReader.GetDataTypeName(_columnMap[i]);
        public DateTime GetDateTime(int i) => _innerReader.GetDateTime(_columnMap[i]);
        public decimal GetDecimal(int i) => _innerReader.GetDecimal(_columnMap[i]);
        public double GetDouble(int i) => _innerReader.GetDouble(_columnMap[i]);
        public float GetFloat(int i) => _innerReader.GetFloat(_columnMap[i]);
        public Guid GetGuid(int i) => _innerReader.GetGuid(_columnMap[i]);
        public short GetInt16(int i) => _innerReader.GetInt16(_columnMap[i]);
        public int GetInt32(int i) => _innerReader.GetInt32(_columnMap[i]);
        public long GetInt64(int i) => _innerReader.GetInt64(_columnMap[i]);
        public string GetString(int i) => _innerReader.GetString(_columnMap[i]);

        public DataTable GetSchemaTable()
        {
            DataTable originalSchema = _innerReader.GetSchemaTable();
            DataTable filteredSchema = originalSchema.Clone();

            // Only include rows for our filtered columns
            foreach (string columnName in _columnNames)
            {
                foreach (DataRow row in originalSchema.Rows)
                {
                    if ((string)row["ColumnName"] == columnName)
                    {
                        filteredSchema.ImportRow(row);
                        break;
                    }
                }
            }

            return filteredSchema;
        }
    }
}