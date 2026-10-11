using BenchmarkDotNet.Attributes;
using Majorsilence.Reporting.Rdl;

namespace Majorsilence.Reporting.Benchmarks;

/// <summary>Workloads that need no external database (xml / inline rows / :memory: sqlite / bundled northwindEF.db).</summary>
public static class Workloads
{
    public const string Small = "testdata";
    public const string Tabular = "WorldFacts";
    public const string Matrix = "MatrixExample";
    public const string Paging = "ListPagingTest";
    public const string CanGrow = "CanGrowSplitTest";
    public const string Charts = "ChartTypes";
    public const string Images = "ImageSizing";
}

/// <summary>Cost of turning RDL xml into a Report (done once per request when reports are not cached).</summary>
[MemoryDiagnoser]
public class ParseBenchmarks
{
    private string _source = "";

    [Params(Workloads.Small, Workloads.Tabular, Workloads.Matrix, Workloads.Charts)]
    public string Workload { get; set; } = "";

    [GlobalSetup]
    public void Setup()
    {
        WorkloadSupport.Init();
        _source = WorkloadSupport.ReadRdl(Workload);
    }

    [Benchmark]
    public async Task<Report> Parse() => await WorkloadSupport.Parse(_source);
}

/// <summary>Query execution + row building on an already parsed report.</summary>
[MemoryDiagnoser]
public class GetDataBenchmarks
{
    private Report _report = null!;

    [Params(Workloads.Tabular, Workloads.Matrix)]
    public string Workload { get; set; } = "";

    [GlobalSetup]
    public async Task Setup()
    {
        WorkloadSupport.Init();
        _report = await WorkloadSupport.Load(Workload);
    }

    [Benchmark]
    public async Task RunGetData() => await _report.RunGetData();
}

/// <summary>Layout + output on a parsed report with data loaded (RunRender only).</summary>
[MemoryDiagnoser]
public class RenderBenchmarks
{
    private Report _report = null!;

    [Params(Workloads.Small, Workloads.Tabular, Workloads.Matrix, Workloads.Paging, Workloads.CanGrow, Workloads.Charts, Workloads.Images)]
    public string Workload { get; set; } = "";

    [Params(OutputPresentationType.PDF, OutputPresentationType.HTML)]
    public OutputPresentationType Format { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        WorkloadSupport.Init();
        _report = await WorkloadSupport.Load(Workload);
        await _report.RunGetData();
    }

    [Benchmark]
    public async Task<long> Render() => await WorkloadSupport.Render(_report, Format);
}

/// <summary>Other output formats, on the tabular workload only.</summary>
[MemoryDiagnoser]
public class RenderFormatBenchmarks
{
    private Report _report = null!;

    [Params(OutputPresentationType.CSV, OutputPresentationType.XML, OutputPresentationType.RTF, OutputPresentationType.Excel2007)]
    public OutputPresentationType Format { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        WorkloadSupport.Init();
        _report = await WorkloadSupport.Load(Workloads.Tabular);
        await _report.RunGetData();
    }

    [Benchmark]
    public async Task<long> Render() => await WorkloadSupport.Render(_report, Format);
}

/// <summary>Parse + data + render to PDF, i.e. what a web request does when it does not cache the Report.</summary>
[MemoryDiagnoser]
public class EndToEndBenchmarks
{
    private string _source = "";

    [Params(Workloads.Small, Workloads.Tabular, Workloads.Matrix)]
    public string Workload { get; set; } = "";

    [GlobalSetup]
    public void Setup()
    {
        WorkloadSupport.Init();
        _source = WorkloadSupport.ReadRdl(Workload);
    }

    [Benchmark]
    public async Task<long> ParseDataRenderPdf()
    {
        var report = await WorkloadSupport.Parse(_source);
        await report.RunGetData();
        return await WorkloadSupport.Render(report, OutputPresentationType.PDF);
    }
}

/// <summary>Many rows supplied through SetData (no query), rendered to PDF.</summary>
[MemoryDiagnoser]
[SimpleJob(BenchmarkDotNet.Engines.RunStrategy.Throughput, iterationCount: 5, warmupCount: 1, launchCount: 1)]
public class LargeRowsBenchmarks
{
    private System.Data.DataTable _rows = null!;
    private string _source = "";

    [Params(1_000, 10_000, 100_000)]
    public int Rows { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        WorkloadSupport.Init();
        _rows = WorkloadSupport.MakeRows(Rows);
        _source = WorkloadSupport.ReadRdl("LargeTable");
    }

    [Benchmark]
    public async Task<long> TablePdf()
    {
        var report = await WorkloadSupport.Parse(_source);
        await report.DataSets["Data"].SetData(_rows);
        await report.RunGetData();
        return await WorkloadSupport.Render(report, OutputPresentationType.PDF);
    }
}

/// <summary>A request that parses the report each time, against one that makes a Report from a CompiledReport.</summary>
[MemoryDiagnoser]
public class CompiledReportBenchmarks
{
    private string _source = "";
    private CompiledReport _compiled = null!;

    [Params(Workloads.Small, Workloads.Tabular, Workloads.Matrix)]
    public string Workload { get; set; } = "";

    [GlobalSetup]
    public async Task Setup()
    {
        WorkloadSupport.Init();
        _source = WorkloadSupport.ReadRdl(Workload);
        _compiled = await CompiledReport.CompileAsync(_source, new CompiledReportOptions { Folder = WorkloadSupport.Folder });
    }

    [GlobalCleanup]
    public void Cleanup() => _compiled.Dispose();

    [Benchmark(Baseline = true)]
    public async Task<long> ParseEveryRequest()
    {
        using var report = await WorkloadSupport.Parse(_source);
        report.Folder = WorkloadSupport.Folder;
        await report.RunGetData();
        return await WorkloadSupport.Render(report, OutputPresentationType.PDF);
    }

    [Benchmark]
    public async Task<long> CompiledReportPerRequest()
    {
        using var report = _compiled.CreateReport();
        report.Folder = WorkloadSupport.Folder;
        await report.RunGetData();
        return await WorkloadSupport.Render(report, OutputPresentationType.PDF);
    }
}
