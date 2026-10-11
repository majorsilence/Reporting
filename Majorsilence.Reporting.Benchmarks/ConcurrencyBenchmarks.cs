using BenchmarkDotNet.Attributes;
using Majorsilence.Reporting.Rdl;

namespace Majorsilence.Reporting.Benchmarks;

/// <summary>
/// Web-server shape: N concurrent requests, each parsing its own Report and rendering a PDF. One operation is
/// "all N requests finished"; compare time per op across Threads to see how well the engine scales.
/// </summary>
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class ConcurrencyBenchmarks
{
    private string _source = "";

    [Params(Workloads.Small, Workloads.Tabular)]
    public string Workload { get; set; } = "";

    [Params(1, 4, 8, 16)]
    public int Threads { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        WorkloadSupport.Init();
        _source = WorkloadSupport.ReadRdl(Workload);
    }

    [Benchmark(OperationsPerInvoke = 16)]
    public void Requests()
    {
        // 16 requests in total regardless of Threads so the numbers are comparable per request.
        Parallel.For(0, 16, new ParallelOptions { MaxDegreeOfParallelism = Threads }, _ =>
        {
            var report = WorkloadSupport.Parse(_source).GetAwaiter().GetResult();
            report.RunGetData().GetAwaiter().GetResult();
            WorkloadSupport.Render(report, OutputPresentationType.PDF).GetAwaiter().GetResult();
        });
    }
}
