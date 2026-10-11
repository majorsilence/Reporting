using System.Security.Cryptography;
using Majorsilence.Reporting.Rdl;

namespace Majorsilence.Reporting.Benchmarks;

/// <summary>Renders each workload once per format. Used to validate the workloads and to compare output before/after an optimisation.</summary>
public static class Smoke
{
    public static async Task Run()
    {
        WorkloadSupport.Init();
        string[] names = [Workloads.Small, Workloads.Tabular, Workloads.Matrix, Workloads.Paging, Workloads.CanGrow, Workloads.Charts, Workloads.Images, "GroupSortTest", "RecursiveGroupTest", "MatrixSortTest"];
        OutputPresentationType[] formats = [OutputPresentationType.PDF, OutputPresentationType.HTML, OutputPresentationType.CSV, OutputPresentationType.XML, OutputPresentationType.RTF, OutputPresentationType.Excel2007];
        Console.WriteLine("|Workload|Format|Bytes|SHA256 (first 12)|Status|");
        Console.WriteLine("|--------|------|----:|-----------------|------|");
        foreach (string name in names)
        {
            foreach (var format in formats)
            {
                try
                {
                    var report = await WorkloadSupport.Load(name);
                    await report.RunGetData();
                    using var ms = new MemoryStreamGen();
                    await report.RunRender(ms, format);
                    var stream = ms.GetStream();
                    stream.Position = 0;
                    using var copy = new MemoryStream();
                    stream.CopyTo(copy);
                    var bytes = copy.ToArray();
                    string hash = Convert.ToHexString(SHA256.HashData(Normalise(format, bytes)))[..12];
                    string status = report.ErrorMaxSeverity > 0 ? "errors: " + string.Join(" / ", report.ErrorItems.Cast<string>().Take(2)).ReplaceLineEndings(" ") : "ok";
                    Console.WriteLine($"|{name}|{format}|{bytes.Length:N0}|{hash}|{status}|");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"|{name}|{format}|||{ex.GetType().Name}: {ex.Message.ReplaceLineEndings(" ")}|");
                }
            }
        }
    }

    /// <summary>
    /// Renders every .rdl under a folder to PDF and reports the ones that throw (data sources the machine lacks
    /// fail with a connection error; those are listed too). <c>-- sweep ../Examples [text]</c> only lists
    /// failures whose message contains the text.
    /// Careful: a report whose sqlite connect string names a missing file makes the provider create an empty
    /// database there, so run it from a scratch copy of the folder or check <c>git status</c> afterwards.
    /// </summary>
    public static async Task Sweep(string folder, string? contains)
    {
        WorkloadSupport.Init();
        int ok = 0, failed = 0;
        foreach (string file in Directory.EnumerateFiles(folder, "*.rdl", SearchOption.AllDirectories).OrderBy(f => f))
        {
            try
            {
                Directory.SetCurrentDirectory(Path.GetDirectoryName(file)!);
                var report = await WorkloadSupport.Parse(File.ReadAllText(file));
                report.Folder = Path.GetDirectoryName(file)!;
                await report.RunGetData();
                await WorkloadSupport.Render(report, OutputPresentationType.PDF);
                ok++;
            }
            catch (Exception ex)
            {
                failed++;
                if (contains == null || ex.ToString().Contains(contains))
                    Console.WriteLine($"{Path.GetRelativePath(folder, file)}: {ex.GetType().Name}: {ex.Message.ReplaceLineEndings(" ")[..Math.Min(140, ex.Message.Length)]}");
            }
        }

        Console.WriteLine($"{ok} rendered, {failed} threw");
    }

    /// <summary>Renders one workload and prints the text output (CSV / XML / HTML): <c>-- show GroupSortTest csv</c>.</summary>
    public static async Task Show(string name, OutputPresentationType format)
    {
        WorkloadSupport.Init();
        var report = await WorkloadSupport.Load(name);
        await report.RunGetData();
        using var ms = new MemoryStreamGen();
        await report.RunRender(ms, format);
        Console.WriteLine(ms.GetText());
    }

    // PDF and OOXML embed creation times / random ids; mask them so equal output hashes equally.
    private static byte[] Normalise(OutputPresentationType format, byte[] bytes)
    {
        if (format != OutputPresentationType.PDF)
        {
            return bytes;
        }

        string text = System.Text.Encoding.Latin1.GetString(bytes);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"/(CreationDate|ModDate)\s*\([^)]*\)", "/$1()");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"/ID\s*\[[^\]]*\]", "/ID[]");
        return System.Text.Encoding.Latin1.GetBytes(text);
    }
}
