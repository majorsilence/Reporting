using System.Data;
using Majorsilence.Reporting.Rdl;

namespace Majorsilence.Reporting.Benchmarks;

/// <summary>Shared helpers for the report workloads copied to the "Workloads" output folder.</summary>
public static class WorkloadSupport
{
    public static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "Workloads");

    private static int _initialised;

    /// <summary>Engine config + working directory (relative sqlite/xml paths in the RDL files resolve against it).</summary>
    public static void Init()
    {
        if (Interlocked.Exchange(ref _initialised, 1) == 0)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            Directory.SetCurrentDirectory(Folder);
        }
    }

    /// <summary>Some sample reports name System.Data.SQLite (Windows only); use the cross-platform Microsoft.Data.Sqlite instead.</summary>
    public static string ReadRdl(string name) =>
        File.ReadAllText(Path.Combine(Folder, name + ".rdl"))
            .Replace("<DataProvider>SQLite</DataProvider>", "<DataProvider>Microsoft.Data.Sqlite</DataProvider>");

    public static async Task<Report> Parse(string rdlSource)
    {
        var report = await new RDLParser(rdlSource) { Folder = Folder }.Parse();
        if (report.ErrorMaxSeverity > 4)
        {
            throw new InvalidOperationException("Report failed to parse: " + string.Join("; ", report.ErrorItems.Cast<string>()));
        }

        return report;
    }

    public static async Task<Report> Load(string name, DataTable? data = null)
    {
        var report = await Parse(ReadRdl(name));
        report.Folder = Folder;
        if (data != null)
        {
            await report.DataSets["Data"].SetData(data);
        }

        return report;
    }

    /// <summary>Renders into memory and returns the output size (also keeps the work from being optimised away).</summary>
    public static async Task<long> Render(Report report, OutputPresentationType type)
    {
        using var ms = new MemoryStreamGen();
        await report.RunRender(ms, type);
        return ms.GetStream().Length;
    }

    public static DataTable MakeRows(int rows)
    {
        var dt = new DataTable();
        dt.Columns.Add("Id", typeof(int));
        dt.Columns.Add("Name", typeof(string));
        dt.Columns.Add("Amount", typeof(double));
        dt.Columns.Add("Region", typeof(string));
        string[] regions = ["North", "South", "East", "West", "Central"];
        for (int i = 0; i < rows; i++)
        {
            dt.Rows.Add(i, "Customer number " + i, (i * 37 % 10000) / 100.0 + 0.5, regions[i % regions.Length]);
        }

        return dt;
    }
}
