using System;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Data
{
    public class JsonDataReader : IDataReader
    {
        private readonly Dictionary<string, IDataReader> _readers;
        private readonly IDataReader _rootReader;

        JsonConnection _tconn;
        JsonCommand _tcmd;
        System.Data.CommandBehavior _behavior;
        private readonly string[] _requestedColumns;

        public JsonDataReader(System.Data.CommandBehavior behavior, JsonConnection conn, JsonCommand cmd)
        {
            _behavior = behavior;
            _tcmd = cmd ?? throw new ArgumentNullException(nameof(cmd), "Command cannot be null.");
            _tconn = conn ?? throw new ArgumentNullException(nameof(conn), "Connection cannot be null.");
            _requestedColumns = cmd.Columns;
        
            string json = Task.Run(async () => await ReadAllJsonAsync()).GetAwaiter().GetResult();

            var extractor = new JsonTableExtractor();
            var allReaders = extractor.Extract(json);
    
            _readers = allReaders;
    
            // Use the table name from the command
            string tableName = cmd.TableName ??  conn.TableName ?? "root";
            if (!_readers.TryGetValue(tableName, out _rootReader))
            {
                throw new InvalidOperationException(
                    $"Table '{tableName}' not found in JSON data. Available tables: {string.Join(", ", _readers.Keys)}");
            }
    
            // Apply column filtering if needed
            if (_requestedColumns != null && _requestedColumns.Length > 0)
            {
                _rootReader = new FilteredDictionaryDataReader(
                    (DictionaryDataReader)_rootReader, 
                    _requestedColumns);
            }
        }

        /// <summary>
        /// The main reader for the top-level JSON array.
        /// </summary>
        public IDataReader Root => _rootReader;

        /// <summary>
        /// Gets a child or nested reader (e.g., "root_Dependents").
        /// </summary>
        public IDataReader? GetTable(string name) =>
            _readers.TryGetValue(name, out var reader) ? reader : null;

        /// <summary>
        /// List of available table names.
        /// </summary>
        public IEnumerable<string> TableNames => _readers.Keys;

        // IDataReader - Delegate all to _rootReader
        public bool Read() => _rootReader.Read();
        public int FieldCount => _rootReader.FieldCount;
        public string GetName(int i) => _rootReader.GetName(i);
        public int GetOrdinal(string name) => _rootReader.GetOrdinal(name);
        public object GetValue(int i) => _rootReader.GetValue(i);

        public int GetValues(object[] values)
        {
            return _rootReader.GetValues(values);
        }

        public object this[int i] => _rootReader[i];
        public object this[string name] => _rootReader[name];
        public bool IsDBNull(int i) => _rootReader.IsDBNull(i);
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
        public Type GetFieldType(int i) => _rootReader.GetFieldType(i);

        public void Close() => _rootReader.Close();
        public void Dispose() => _rootReader.Dispose();
        public bool NextResult() => false;
        public int Depth => 0;
        public bool IsClosed => false;
        public int RecordsAffected => -1;

        public DataTable GetSchemaTable() => _rootReader.GetSchemaTable();
        public bool GetBoolean(int i) => (bool)GetValue(i);
        public byte GetByte(int i) => (byte)GetValue(i);

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) =>
            _rootReader.GetBytes(i, fieldOffset, buffer, bufferoffset, length);

        public char GetChar(int i) => (char)GetValue(i);

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length) =>
            _rootReader.GetChars(i, fieldoffset, buffer, bufferoffset, length);

        public IDataReader GetData(int i) => _rootReader.GetData(i);
        public string GetDataTypeName(int i) => GetFieldType(i).Name;
        public DateTime GetDateTime(int i) => (DateTime)GetValue(i);
        public decimal GetDecimal(int i) => (decimal)GetValue(i);
        public double GetDouble(int i) => (double)GetValue(i);
        public float GetFloat(int i) => (float)GetValue(i);
        public Guid GetGuid(int i) => (Guid)GetValue(i);
        public short GetInt16(int i) => (short)GetValue(i);
        public int GetInt32(int i) => (int)GetValue(i);
        public long GetInt64(int i) => (long)GetValue(i);
        public string GetString(int i) => GetValue(i)?.ToString();

        async Task<StreamReader> GetStream()
        {
            string fname = _tcmd.Url;
            Stream strm = null;

            if (fname.StartsWith("http:") || fname.StartsWith("https:"))
            {
                _tconn.Client.AddMajorsilenceReportingUserAgent();
                // set auth if found in connection string
                // via a request object

                var request = new HttpRequestMessage(HttpMethod.Get, fname);

                if (!string.IsNullOrWhiteSpace(_tconn.Auth))
                {
                    var authParts = _tconn.Auth.Split(':');
                    string authScheme = authParts[0];
                    string authParameters = authParts[1];
                    request.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue(authScheme, authParameters);
                }

                HttpResponseMessage response = await _tconn.Client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                strm = await response.Content.ReadAsStreamAsync();
            }
            else if (fname.StartsWith("file:"))
            {
                strm = new FileStream(fname.Substring(5), System.IO.FileMode.Open, FileAccess.Read);
            }
            else
            {
                strm = new FileStream(fname, System.IO.FileMode.Open, FileAccess.Read);
            }

            return new StreamReader(strm);
        }

        private async Task<string> ReadAllJsonAsync()
        {
            using var sr = await GetStream();
            return await sr.ReadToEndAsync();
        }
    }
}