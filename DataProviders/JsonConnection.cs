
using System;
using System.Xml;
using System.Data;
using System.Net.Http;

namespace Majorsilence.Reporting.Data
{
    /// <summary>
    /// LogConnection handles connections to web log.
    /// </summary>
    public class JsonConnection : IDbConnection
    {
        string _Connection;             // the connection string; of format file=
        bool bOpen = false;
        public HttpClient Client { get; private set; }
        private bool shouldDisposeClient = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <example>
        /// <code>
        /// var conn1 = new JsonConnection("file=TestData.json");
        /// var conn2 = new JsonConnection("url=https://raw.githubusercontent.com/majorsilence/Reporting/refs/heads/main/RdlCreator.Tests/TestData.json");
        /// var conn3 = new JsonConnection("url=https://example.com/path/to/json/TestData.json;auth=Basic: <credentials>");
        /// var conn4 = new JsonConnection("url=https://example.com/path/to/json/TestData.json;auth=Bearer: <Token>");
        /// </code>
        /// </example>
        public JsonConnection(string conn) : this(conn, new HttpClient())
        {
            shouldDisposeClient = true;
        }

        public JsonConnection(string conn, HttpClient httpClient)
        {
            ConnectionString = conn;
            Client = httpClient ?? throw new ArgumentNullException(nameof(httpClient), "HttpClient cannot be null");
        }
        
        private string _tableName = null; // Add this field with default value

        // Add property to access the table name
        public string TableName
        {
            get { return _tableName; }
        }

        internal bool IsOpen
        {
            get { return bOpen; }
        }

        public string Url { get; private set; }
        public string Auth { get; private set; }

        #region IDbConnection Members

        public void ChangeDatabase(string databaseName)
        {
            throw new NotImplementedException("ChangeDatabase method not supported.");
        }

        public IDbTransaction BeginTransaction(System.Data.IsolationLevel il)
        {
            throw new NotImplementedException("BeginTransaction method not supported.");
        }

        IDbTransaction System.Data.IDbConnection.BeginTransaction()
        {
            throw new NotImplementedException("BeginTransaction method not supported.");
        }

        public System.Data.ConnectionState State
        {
            get
            {
                throw new NotImplementedException("State not implemented");
            }
        }

        public string ConnectionString
        {
            get
            {
                return _Connection;
            }
            set
            {
                _Connection = value;
                SetUrlFromConnection();
            }
        }

        public IDbCommand CreateCommand()
        {
            return new JsonCommand(this);
        }

        public void Open()
        {
            bOpen = true;
        }

        public void Close()
        {
            bOpen = false;
        }

        public string Database
        {
            get
            {
                return null;            // don't really have a database
            }
        }

        public int ConnectionTimeout
        {
            get
            {
                return 0;
            }
        }

        #endregion

        private void SetUrlFromConnection()
        {
            string[] args = ConnectionString.Split(';');
            string url = null;
            foreach (string arg in args)
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
                    case "memory:":
                        // Memory is not supported in JsonConnection
                        throw new NotSupportedException("Memory parameter is not supported in JsonConnection.");
                    case "auth":
                    case "authorization":
                        Auth=val.Trim();
                        break;
                    case "table":
                        _tableName = val;
                        break;
                    default:
                        throw new ArgumentException(string.Format("{0} is an unknown parameter key", param[0]));
                }
            }

            // User must specify both the url and the RowsXPath
            if (url == null)
                throw new ArgumentException("CommandText requires a 'Url=' parameter.");

            Url = url.Trim();
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (shouldDisposeClient)
            {
                Client?.Dispose();
                Client = null;
            }
            this.Close();
        }

        #endregion
    }
}
