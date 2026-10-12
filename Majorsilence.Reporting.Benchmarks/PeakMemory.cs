using System.Data;
using System.Diagnostics;
using Majorsilence.Reporting.Data;

namespace Majorsilence.Reporting.Benchmarks
{
    /// <summary>
    /// Peak memory of reading a whole document, which BenchmarkDotNet's allocation column does not show
    /// (it adds up everything allocated, not what was alive at once). Each read runs in its own process:
    ///   peak gen &lt;dir&gt; &lt;rows&gt;                      writes rows.json and rows.xml
    ///   peak read json|xml|jsonasync|xmlasync &lt;dir&gt;   reads them and prints the peak working set
    /// </summary>
    public static class PeakMemory
    {
        public static async Task<int> Run(string[] args)
        {
            if (args.Length >= 4 && args[1] == "gen")
            {
                Directory.CreateDirectory(args[2]);
                ReadBenchmarkData.Create(Path.Combine(args[2], "rows.json"), Path.Combine(args[2], "rows.xml"), int.Parse(args[3]));
                return 0;
            }

            if (args.Length < 4 || args[1] != "read")
            {
                Console.Error.WriteLine("usage: peak gen <dir> <rows> | peak read json|xml|jsonasync|xmlasync <dir>");
                return 1;
            }

            string kind = args[2];
            string jsonFile = Path.Combine(args[3], "rows.json");
            string xmlFile = Path.Combine(args[3], "rows.xml");

            GC.Collect();
            long before = GC.GetTotalAllocatedBytes(true);
            var sw = Stopwatch.StartNew();
            long cells;
            bool json = kind.StartsWith("json");
            using (var cn = json ? (IDbConnection)new JsonConnection("file=" + jsonFile) : new XmlConnection("file=" + xmlFile))
            {
                cn.Open();
                using var cmd = cn.CreateCommand();
                cmd.CommandText = json ? "root" : "Url=" + xmlFile + ";RowsXPath=/rows/row";
                using var dr = kind.EndsWith("async")
                    ? await ((IAsyncDataCommand)cmd).ExecuteReaderAsync(CommandBehavior.SingleResult)
                    : cmd.ExecuteReader(CommandBehavior.SingleResult);

                // like the engine, keep every row (Query.GetData collects them all)
                var kept = new List<object[]>();
                while (dr.Read())
                {
                    var values = new object[dr.FieldCount];
                    dr.GetValues(values);
                    kept.Add(values);
                }
                cells = (long)kept.Count * dr.FieldCount;
                GC.KeepAlive(kept);
            }
            sw.Stop();

            long allocated = GC.GetTotalAllocatedBytes(true) - before;
            long peak = Process.GetCurrentProcess().PeakWorkingSet64;
            Console.WriteLine($"{kind,-10} time={sw.ElapsedMilliseconds,6} ms  allocated={allocated / 1048576.0,8:N1} MB  peakWorkingSet={peak / 1048576.0,8:N1} MB  cells={cells:N0}");
            return 0;
        }
    }
}
