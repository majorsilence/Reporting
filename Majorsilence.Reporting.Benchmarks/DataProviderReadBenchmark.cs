using System.Data;
using BenchmarkDotNet.Attributes;
using Majorsilence.Reporting.Data;

namespace Majorsilence.Reporting.Benchmarks
{
    /// <summary>
    /// Reads every cell of every row from the Json, Xml and Dictionary data providers, which is what the
    /// report engine does before it renders anything. Compare time and allocated bytes between branches:
    ///   dotnet run -c Release -f net10.0 -- --filter '*DataProviderRead*'
    /// </summary>
    [MemoryDiagnoser]
    [ShortRunJob]
    public class DataProviderReadBenchmark
    {
        string _dir = "";
        string _jsonFile = "";
        string _xmlFile = "";
        List<Dictionary<string, object>> _rows = [];

        [Params(1_000, 100_000)]
        public int Rows { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _dir = Path.Combine(Path.GetTempPath(), "rdl-bench-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _jsonFile = Path.Combine(_dir, "rows.json");
            _xmlFile = Path.Combine(_dir, "rows.xml");
            _rows = ReadBenchmarkData.Create(_jsonFile, _xmlFile, Rows);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            try { Directory.Delete(_dir, true); } catch (IOException) { }
        }

        static long Consume(IDataReader dr)
        {
            long cells = 0;
            int n = dr.FieldCount;
            var values = new object[n];
            while (dr.Read())
            {
                // the engine's Query.SetData reads whole rows with GetValues
                dr.GetValues(values);
                for (int i = 0; i < n; i++)
                    if (values[i] != null && values[i] != DBNull.Value) cells++;
            }
            return cells;
        }

        static async Task<long> ConsumeAsync(IDataReader dr)
        {
            if (dr is not IAsyncDataReader ar)
                return Consume(dr);

            long cells = 0;
            int n = dr.FieldCount;
            var values = new object[n];
            while (await ar.ReadAsync())
            {
                dr.GetValues(values);
                for (int i = 0; i < n; i++)
                    if (values[i] != null && values[i] != DBNull.Value) cells++;
            }
            return cells;
        }

        [Benchmark]
        public long Json()
        {
            using var cn = new JsonConnection("file=" + _jsonFile);
            cn.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "root";
            using var dr = cmd.ExecuteReader(CommandBehavior.SingleResult);
            return Consume(dr);
        }

        [Benchmark]
        public async Task<long> JsonAsync()
        {
            using var cn = new JsonConnection("file=" + _jsonFile);
            cn.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "root";
            using var dr = await ((IAsyncDataCommand)cmd).ExecuteReaderAsync(CommandBehavior.SingleResult);
            return await ConsumeAsync(dr);
        }

        [Benchmark]
        public long JsonThreeColumns()
        {
            using var cn = new JsonConnection("file=" + _jsonFile);
            cn.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "root;columns=Id,Name,City";
            using var dr = cmd.ExecuteReader(CommandBehavior.SingleResult);
            return Consume(dr);
        }

        [Benchmark]
        public long Xml()
        {
            using var cn = new XmlConnection("file=" + _xmlFile);
            cn.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "Url=" + _xmlFile + ";RowsXPath=/rows/row";
            using var dr = cmd.ExecuteReader(CommandBehavior.SingleResult);
            return Consume(dr);
        }

        [Benchmark]
        public long XmlWithColumnXPaths()
        {
            using var cn = new XmlConnection("file=" + _xmlFile);
            cn.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "Url=" + _xmlFile + ";RowsXPath=/rows/row;ColumnsXPath=Id,Name,City";
            using var dr = cmd.ExecuteReader(CommandBehavior.SingleResult);
            return Consume(dr);
        }

        /// <summary>The in-memory reader behind SetData and the Json provider, built from rows that already exist.</summary>
        [Benchmark]
        public long Dictionary()
        {
            using var dr = new DictionaryDataReader(_rows);
            return Consume(dr);
        }

        /// <summary>The engine asks for every field's type once per column; each ask used to scan the rows.</summary>
        [Benchmark]
        public int DictionaryFieldTypes()
        {
            using var dr = new DictionaryDataReader(_rows);
            int n = 0;
            for (int i = 0; i < dr.FieldCount; i++)
                n += dr.GetFieldType(i) == typeof(string) ? 1 : 0;
            return n;
        }
    }
}
