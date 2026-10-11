using BenchmarkDotNet.Running;
using Majorsilence.Reporting.Benchmarks;
using System.Diagnostics;

// Benchmarks:  dotnet run -c Release -f net10.0 -- --filter '*Json*'
// Render every workload once and print size + hash:  -- smoke
// Thread scaling (not a BDN benchmark):  dotnet run -c Release -f net10.0 -- scale [seconds] [threads...]
if (args.Length > 0 && args[0] == "scale")
{
    int seconds = args.Length > 1 ? int.Parse(args[1]) : 30;
    int[] threadCounts = args.Length > 2 ? args.Skip(2).Select(int.Parse).ToArray() : [1, 4, 8, 16];

    var bench = new JsonDataProviderBenchmark();
    Console.WriteLine("|Threads|Duration|Source|Calls/Sec|Total Calls|Errors|");
    Console.WriteLine("|-------|--------|------|---------:|----------:|-----:|");
    foreach (int threads in threadCounts)
    {
        ConcurrentCallCount(bench, threads, seconds);
    }

    return;
}

if (args.Length > 0 && args[0] == "smoke")
{
    await Smoke.Run();
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

static void ConcurrentCallCount(JsonDataProviderBenchmark inst, int threadCount, int durationInSeconds)
{
    int calls = 0;
    int errors = 0;
    var stopwatch = Stopwatch.StartNew();

    var threads = new Thread[threadCount];
    for (int i = 0; i < threadCount; i++)
    {
        threads[i] = new Thread(() =>
        {
            while (stopwatch.Elapsed.TotalSeconds < durationInSeconds)
            {
                try
                {
                    inst.NestedJson().GetAwaiter().GetResult();
                    Interlocked.Increment(ref calls);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref errors);
                    Console.Error.WriteLine(ex);
                }
            }
        });
        threads[i].Start();
    }

    foreach (var thread in threads)
    {
        thread.Join();
    }

    stopwatch.Stop();
    double callRate = calls / stopwatch.Elapsed.TotalSeconds;
    Console.WriteLine($"|{threadCount}|{durationInSeconds}|{inst.GetType().Name}|{callRate:N0}|{calls:N0}|{errors:N0}|");
}
