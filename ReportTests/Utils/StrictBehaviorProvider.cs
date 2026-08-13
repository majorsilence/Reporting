using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Majorsilence.Reporting.Rdl;
using Microsoft.Data.Sqlite;

namespace ReportTests.Utils
{
    /// <summary>
    /// An ADO.NET provider that delegates to Microsoft.Data.Sqlite but actually honours the
    /// <see cref="CommandBehavior"/> passed to ExecuteReader.
    ///
    /// This exists because Microsoft.Data.Sqlite silently ignores
    /// <see cref="CommandBehavior.SchemaOnly"/> and returns rows anyway, so a plain SQLite test
    /// cannot detect a data pass that wrongly asks for SchemaOnly. Providers that do implement
    /// the contract - Oracle, SQL Server, MySQL, PostgreSQL, ODBC - return column metadata and
    /// no rows, which is what issue #308 was about. This provider reproduces that contract so
    /// the engine's choice of CommandBehavior can be asserted without a real database server.
    /// </summary>
    public static class StrictBehaviorProvider
    {
        public const string ProviderName = "StrictBehaviorTest";

        private static readonly List<CommandBehavior> _behaviors = new();
        private static readonly List<ObservedParameter> _parameters = new();
        private static readonly object _sync = new();

        /// <summary>A parameter as the engine handed it over, captured at ExecuteReader time.</summary>
        public readonly record struct ObservedParameter(string Name, DbType DbType, object Value);

        /// <summary>Every CommandBehavior the engine has asked a reader for since <see cref="Reset"/>.</summary>
        public static IReadOnlyList<CommandBehavior> ObservedBehaviors
        {
            get { lock (_sync) { return _behaviors.ToArray(); } }
        }

        /// <summary>Every parameter the engine has bound since <see cref="Reset"/>, in order.</summary>
        public static IReadOnlyList<ObservedParameter> ObservedParameters
        {
            get { lock (_sync) { return _parameters.ToArray(); } }
        }

        internal static void RecordBehavior(CommandBehavior behavior)
        {
            lock (_sync) { _behaviors.Add(behavior); }
        }

        internal static void RecordParameters(DbParameterCollection parameters)
        {
            lock (_sync)
            {
                foreach (DbParameter p in parameters)
                    _parameters.Add(new ObservedParameter(p.ParameterName, p.DbType, p.Value));
            }
        }

        public static void Reset()
        {
            lock (_sync)
            {
                _behaviors.Clear();
                _parameters.Clear();
            }
        }

        /// <summary>
        /// Makes <see cref="ProviderName"/> resolvable as an RDL &lt;DataProvider&gt;. Call after
        /// RdlEngineConfigInit(), which is what populates the provider table in the first place.
        /// </summary>
        public static void Register()
        {
            RdlEngineConfig.RdlEngineConfigInit();

            var entry = new SqlConfigEntry(
                ProviderName,
                null,
                typeof(StrictBehaviorConnection).FullName,
                typeof(StrictBehaviorConnection).Assembly,
                "SELECT name FROM sqlite_master WHERE type = 'table'",
                null);

            RdlEngineConfig.SqlEntries[ProviderName] = entry;
        }
    }

    public sealed class StrictBehaviorConnection : DbConnection
    {
        private readonly SqliteConnection _inner;

        public StrictBehaviorConnection(string connectionString)
        {
            _inner = new SqliteConnection(connectionString);
        }

        public override string ConnectionString
        {
            get => _inner.ConnectionString;
            set => _inner.ConnectionString = value;
        }

        public override string Database => _inner.Database;
        public override string DataSource => _inner.DataSource;
        public override string ServerVersion => _inner.ServerVersion;
        public override ConnectionState State => _inner.State;

        public override void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);
        public override void Close() => _inner.Close();
        public override void Open() => _inner.Open();

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            => _inner.BeginTransaction(isolationLevel);

        protected override DbCommand CreateDbCommand()
            => new StrictBehaviorCommand(this, _inner.CreateCommand());

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _inner.Dispose();
            base.Dispose(disposing);
        }
    }

    public sealed class StrictBehaviorCommand : DbCommand
    {
        private readonly StrictBehaviorConnection _connection;
        private readonly SqliteCommand _inner;

        internal StrictBehaviorCommand(StrictBehaviorConnection connection, SqliteCommand inner)
        {
            _connection = connection;
            _inner = inner;
        }

        public override string CommandText
        {
            get => _inner.CommandText;
            set => _inner.CommandText = value;
        }

        public override int CommandTimeout
        {
            get => _inner.CommandTimeout;
            set => _inner.CommandTimeout = value;
        }

        public override CommandType CommandType
        {
            get => _inner.CommandType;
            set => _inner.CommandType = value;
        }

        public override UpdateRowSource UpdatedRowSource
        {
            get => _inner.UpdatedRowSource;
            set => _inner.UpdatedRowSource = value;
        }

        public override bool DesignTimeVisible { get; set; }

        protected override DbConnection DbConnection
        {
            get => _connection;
            set { /* the wrapped command keeps its own connection */ }
        }

        protected override DbParameterCollection DbParameterCollection => _inner.Parameters;

        protected override DbTransaction DbTransaction
        {
            get => _inner.Transaction;
            set => _inner.Transaction = value as SqliteTransaction;
        }

        public override void Cancel() => _inner.Cancel();
        public override int ExecuteNonQuery() => _inner.ExecuteNonQuery();
        public override object ExecuteScalar() => _inner.ExecuteScalar();
        public override void Prepare() => _inner.Prepare();

        protected override DbParameter CreateDbParameter() => _inner.CreateParameter();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            StrictBehaviorProvider.RecordBehavior(behavior);
            StrictBehaviorProvider.RecordParameters(_inner.Parameters);

            // Strip SchemaOnly before handing the behaviour to SQLite: it ignores the flag, and
            // the point here is to apply the documented semantics ourselves.
            DbDataReader reader = _inner.ExecuteReader(behavior & ~CommandBehavior.SchemaOnly);

            return (behavior & CommandBehavior.SchemaOnly) != 0
                ? new SchemaOnlyDataReader(reader)
                : reader;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _inner.Dispose();
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Applies CommandBehavior.SchemaOnly semantics to an inner reader: all column metadata is
    /// available, but the reader returns no rows.
    /// </summary>
    internal sealed class SchemaOnlyDataReader : DbDataReader
    {
        private readonly DbDataReader _inner;

        internal SchemaOnlyDataReader(DbDataReader inner) => _inner = inner;

        public override bool Read() => false;
        public override bool NextResult() => false;
        public override bool HasRows => false;
        public override int RecordsAffected => -1;

        public override int Depth => _inner.Depth;
        public override int FieldCount => _inner.FieldCount;
        public override bool IsClosed => _inner.IsClosed;

        public override string GetName(int ordinal) => _inner.GetName(ordinal);
        public override int GetOrdinal(string name) => _inner.GetOrdinal(name);
        public override string GetDataTypeName(int ordinal) => _inner.GetDataTypeName(ordinal);
        public override Type GetFieldType(int ordinal) => _inner.GetFieldType(ordinal);
        public override DataTable GetSchemaTable() => _inner.GetSchemaTable();

        // No row is ever current, so every value accessor is invalid.
        public override object this[int ordinal] => throw NoCurrentRow();
        public override object this[string name] => throw NoCurrentRow();
        public override object GetValue(int ordinal) => throw NoCurrentRow();
        public override int GetValues(object[] values) => throw NoCurrentRow();
        public override bool IsDBNull(int ordinal) => throw NoCurrentRow();
        public override bool GetBoolean(int ordinal) => throw NoCurrentRow();
        public override byte GetByte(int ordinal) => throw NoCurrentRow();
        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) => throw NoCurrentRow();
        public override char GetChar(int ordinal) => throw NoCurrentRow();
        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length) => throw NoCurrentRow();
        public override DateTime GetDateTime(int ordinal) => throw NoCurrentRow();
        public override decimal GetDecimal(int ordinal) => throw NoCurrentRow();
        public override double GetDouble(int ordinal) => throw NoCurrentRow();
        public override float GetFloat(int ordinal) => throw NoCurrentRow();
        public override Guid GetGuid(int ordinal) => throw NoCurrentRow();
        public override short GetInt16(int ordinal) => throw NoCurrentRow();
        public override int GetInt32(int ordinal) => throw NoCurrentRow();
        public override long GetInt64(int ordinal) => throw NoCurrentRow();
        public override string GetString(int ordinal) => throw NoCurrentRow();

        public override IEnumerator GetEnumerator() => Array.Empty<object>().GetEnumerator();

        private static InvalidOperationException NoCurrentRow()
            => new InvalidOperationException("No current row: the reader was opened with CommandBehavior.SchemaOnly.");

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
