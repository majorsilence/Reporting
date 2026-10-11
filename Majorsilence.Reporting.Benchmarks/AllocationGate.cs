using System.Text.Json;
using Majorsilence.Reporting.Rdl;

namespace Majorsilence.Reporting.Benchmarks;

/// <summary>
/// Allocation regression gate for CI. Renders a fixed set of workloads and compares the managed bytes
/// allocated per render (parse + data + PDF) with docs/performance/allocation-baseline.json.
/// Allocated bytes barely vary from run to run, unlike timings, so they can be checked on shared CI runners.
/// Usage: -- gate [--update] [--tolerance 0.20] [--baseline path]
/// </summary>
public static class AllocationGate
{
    public static async Task<int> Run(string[] args)
    {
        bool update = args.Contains("--update");
        double tolerance = ArgValue(args, "--tolerance") is { } t ? double.Parse(t, System.Globalization.CultureInfo.InvariantCulture) : 0.20;
        string baselinePath = ArgValue(args, "--baseline") ?? FindBaseline();

        WorkloadSupport.Init();
        var workloads = new (string Name, string Rdl, System.Data.DataTable? Rows, OutputPresentationType Format)[]
        {
            ("testdata", Workloads.Small, null, OutputPresentationType.PDF),
            ("WorldFacts", Workloads.Tabular, null, OutputPresentationType.PDF),
            ("MatrixExample", Workloads.Matrix, null, OutputPresentationType.PDF),
            ("ListPagingTest", Workloads.Paging, null, OutputPresentationType.PDF),
            ("large:1000", "LargeTable", WorkloadSupport.MakeRows(1000), OutputPresentationType.PDF),
            // the other writers, on the same report as the PDF row above
            ("WorldFacts html", Workloads.Tabular, null, OutputPresentationType.HTML),
            ("WorldFacts csv", Workloads.Tabular, null, OutputPresentationType.CSV),
            ("WorldFacts xml", Workloads.Tabular, null, OutputPresentationType.XML),
            ("WorldFacts rtf", Workloads.Tabular, null, OutputPresentationType.RTF),
            ("WorldFacts xlsx", Workloads.Tabular, null, OutputPresentationType.Excel2007),
        };

        var measured = new SortedDictionary<string, long>(StringComparer.Ordinal);
        foreach (var (name, rdl, rows, format) in workloads)
        {
            string source = WorkloadSupport.ReadRdl(rdl);

            async Task Once()
            {
                var report = await WorkloadSupport.Parse(source);
                if (rows != null)
                {
                    await report.DataSets["Data"].SetData(rows);
                }

                await report.RunGetData();
                await WorkloadSupport.Render(report, format);
            }

            for (int i = 0; i < 5; i++)
            {
                await Once();      // fills the shared font caches and JITs
            }

            const int iterations = 20;
            long before = GC.GetTotalAllocatedBytes(true);
            for (int i = 0; i < iterations; i++)
            {
                await Once();
            }

            long perOp = (GC.GetTotalAllocatedBytes(true) - before) / iterations;
            measured[name] = perOp;
            Console.WriteLine($"{name,-18} {perOp / 1024.0,10:N0} KB/op");
        }

        if (update)
        {
            File.WriteAllText(baselinePath, JsonSerializer.Serialize(measured, new JsonSerializerOptions { WriteIndented = true }) + "\n");
            Console.WriteLine($"Baseline written to {baselinePath}");
            return 0;
        }

        if (!File.Exists(baselinePath))
        {
            Console.Error.WriteLine($"No baseline at {baselinePath}; run with --update to create it.");
            return 2;
        }

        var baseline = JsonSerializer.Deserialize<Dictionary<string, long>>(File.ReadAllText(baselinePath)) ?? new();
        int failures = 0;
        Console.WriteLine();
        Console.WriteLine($"|Workload|Baseline KB|Now KB|Change|Result (tolerance +{tolerance:P0})|");
        Console.WriteLine("|--------|----------:|-----:|-----:|------|");
        foreach (var (name, now) in measured)
        {
            if (!baseline.TryGetValue(name, out long expected))
            {
                Console.WriteLine($"|{name}|n/a|{now / 1024.0:N0}|n/a|not in baseline|");
                continue;
            }

            double change = (now - expected) / (double)expected;
            bool bad = change > tolerance;
            if (bad)
            {
                failures++;
            }

            Console.WriteLine($"|{name}|{expected / 1024.0:N0}|{now / 1024.0:N0}|{change:+0.0%;-0.0%;0.0%}|{(bad ? "FAIL" : change < -tolerance ? "better - run --update" : "ok")}|");
        }

        if (failures > 0)
        {
            Console.Error.WriteLine($"{failures} workload(s) allocate more than the baseline by over {tolerance:P0}. If the increase is intended, run '-- gate --update' and commit docs/performance/allocation-baseline.json.");
            return 1;
        }

        return 0;
    }

    private static string? ArgValue(string[] args, string name)
    {
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    // Walk up from the output folder to the repository root.
    private static string FindBaseline()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "docs", "performance")))
            {
                return Path.Combine(dir.FullName, "docs", "performance", "allocation-baseline.json");
            }
        }

        return Path.Combine(Directory.GetCurrentDirectory(), "allocation-baseline.json");
    }
}
