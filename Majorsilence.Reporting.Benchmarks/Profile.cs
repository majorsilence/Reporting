using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using Majorsilence.Reporting.Rdl;

namespace Majorsilence.Reporting.Benchmarks;

/// <summary>
/// Loops one workload so an external profiler can attach (dotnet-trace / perf), and optionally prints the
/// allocation sample by type collected in-process from the runtime's AllocationTick events.
/// Usage: -- profile [workload|large:ROWS] [pdf|html|...] [seconds] [alloc]
/// </summary>
public static class Profile
{
    public static async Task Run(string[] args)
    {
        string workload = args.Length > 1 ? args[1] : Workloads.Tabular;
        var format = args.Length > 2 ? Enum.Parse<OutputPresentationType>(args[2], ignoreCase: true) : OutputPresentationType.PDF;
        int seconds = args.Length > 3 ? int.Parse(args[3]) : 20;
        bool alloc = args.Contains("alloc");

        WorkloadSupport.Init();
        System.Data.DataTable? rows = null;
        string rdl;
        if (workload.StartsWith("large:"))
        {
            rows = WorkloadSupport.MakeRows(int.Parse(workload[6..]));
            rdl = WorkloadSupport.ReadRdl("LargeTable");
        }
        else
        {
            rdl = WorkloadSupport.ReadRdl(workload);
        }

        long parseTicks = 0, dataTicks = 0, renderTicks = 0;
        async Task<long> Once()
        {
            long t0 = Stopwatch.GetTimestamp();
            var report = await WorkloadSupport.Parse(rdl);
            if (rows != null)
            {
                await report.DataSets["Data"].SetData(rows);
            }

            long t1 = Stopwatch.GetTimestamp();
            await report.RunGetData();
            long t2 = Stopwatch.GetTimestamp();
            long size = await WorkloadSupport.Render(report, format);
            long t3 = Stopwatch.GetTimestamp();
            parseTicks += t1 - t0;
            dataTicks += t2 - t1;
            renderTicks += t3 - t2;
            return size;
        }

        for (int i = 0; i < 3; i++)
        {
            await Once();
        }

        using var listener = alloc ? new AllocListener() : null;
        parseTicks = dataTicks = renderTicks = 0;
        long bytes0 = GC.GetTotalAllocatedBytes(true);
        int gc0 = GC.CollectionCount(0), gc1 = GC.CollectionCount(1), gc2 = GC.CollectionCount(2);
        var sw = Stopwatch.StartNew();
        int n = 0;
        while (sw.Elapsed.TotalSeconds < seconds)
        {
            await Once();
            n++;
        }

        sw.Stop();
        long bytes = GC.GetTotalAllocatedBytes(false) - bytes0;
        Console.WriteLine($"{workload} {format}: {n} iterations, {sw.Elapsed.TotalMilliseconds / n:N2} ms/op, {bytes / n / 1024.0:N0} KB/op, GC0/1/2 per 1000 ops: {(GC.CollectionCount(0) - gc0) * 1000.0 / n:N0}/{(GC.CollectionCount(1) - gc1) * 1000.0 / n:N0}/{(GC.CollectionCount(2) - gc2) * 1000.0 / n:N0}");
        double ms(long ticks) => ticks * 1000.0 / Stopwatch.Frequency / n;
        Console.WriteLine($"  phases ms/op: parse {ms(parseTicks):N2}, getdata {ms(dataTicks):N2}, render {ms(renderTicks):N2}");
        listener?.Print(top: 40);
    }

    private sealed class AllocListener : EventListener
    {
        private readonly ConcurrentDictionary<string, long> _bytes = new();
        private EventSource? _runtime;

        protected override void OnEventSourceCreated(EventSource source)
        {
            if (source.Name == "Microsoft-Windows-DotNETRuntime")
            {
                _runtime = source;
                EnableEvents(source, EventLevel.Verbose, (EventKeywords)0x1);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs e)
        {
            if (e.EventName == null || !e.EventName.StartsWith("GCAllocationTick") || e.Payload == null || e.PayloadNames == null)
            {
                return;
            }

            string type = "?";
            long amount = 0;
            for (int i = 0; i < e.PayloadNames.Count; i++)
            {
                switch (e.PayloadNames[i])
                {
                    case "TypeName": type = e.Payload[i]?.ToString() ?? "?"; break;
                    case "AllocationAmount64": amount = Convert.ToInt64(e.Payload[i]); break;
                }
            }

            _bytes.AddOrUpdate(type, amount, (_, v) => v + amount);
        }

        public void Print(int top)
        {
            if (_runtime != null)
            {
                DisableEvents(_runtime);
            }

            long total = _bytes.Values.Sum();
            Console.WriteLine($"\nAllocation sample by type (total {total / 1024.0 / 1024.0:N1} MB sampled)");
            Console.WriteLine("|Type|MB|%|");
            Console.WriteLine("|----|--:|--:|");
            foreach (var kv in _bytes.OrderByDescending(k => k.Value).Take(top))
            {
                Console.WriteLine($"|{kv.Key}|{kv.Value / 1024.0 / 1024.0:N1}|{kv.Value * 100.0 / total:N1}|");
            }
        }
    }
}
