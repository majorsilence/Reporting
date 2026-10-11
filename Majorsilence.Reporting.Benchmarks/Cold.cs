using System.Diagnostics;
using System.Runtime;
using Majorsilence.Reporting.Rdl;

namespace Majorsilence.Reporting.Benchmarks;

/// <summary>
/// First-render cost in a fresh process: wall time and JIT time of each step. Run it several times
/// (a new process each time) and compare:  -- cold WorldFacts pdf
/// </summary>
public static class Cold
{
    public static async Task Run(string[] args)
    {
        string workload = args.Length > 1 ? args[1] : "WorldFacts";
        var format = Enum.Parse<OutputPresentationType>(args.Length > 2 ? args[2] : "pdf", ignoreCase: true);

        var sinceStart = DateTime.Now - Process.GetCurrentProcess().StartTime;
        Console.WriteLine($"runtime start to Main: {sinceStart.TotalMilliseconds:F0} ms");

        TimeSpan jit0 = JitInfo.GetCompilationTime();
        long methods0 = JitInfo.GetCompiledMethodCount();

        async Task Step(string name, Func<Task> work)
        {
            TimeSpan j = JitInfo.GetCompilationTime();
            long m = JitInfo.GetCompiledMethodCount();
            var sw = Stopwatch.StartNew();
            await work();
            sw.Stop();
            Console.WriteLine($"{name,-26} {sw.Elapsed.TotalMilliseconds,8:F1} ms   jit {(JitInfo.GetCompilationTime() - j).TotalMilliseconds,7:F1} ms   {JitInfo.GetCompiledMethodCount() - m,5} methods");
        }

        // What the first render pays for text and fonts, measured on its own before anything else uses them
        await Step("skia font manager", () => { _ = SkiaSharp.SKFontManager.Default.FontFamilyCount; return Task.CompletedTask; });
        await Step("first MeasureString", () =>
        {
            using var bmp = new Majorsilence.Forms.Drawing.Bitmap(10, 10);
            using var g = Majorsilence.Forms.Drawing.Graphics.FromImage(bmp);
            using var f = new Majorsilence.Forms.Drawing.Font("Arial", 10f);
            _ = g.MeasureString("Hello", f);
            return Task.CompletedTask;
        });
        await Step("second MeasureString", () =>
        {
            using var bmp = new Majorsilence.Forms.Drawing.Bitmap(10, 10);
            using var g = Majorsilence.Forms.Drawing.Graphics.FromImage(bmp);
            using var f = new Majorsilence.Forms.Drawing.Font("Arial", 10f);
            _ = g.MeasureString("Hello again", f);
            return Task.CompletedTask;
        });

        // The PDF renderer reads the name table of every installed font once per process (InstalledFontScanner)
        await Step("font scan (all fonts)", () =>
        {
            var scanner = typeof(Majorsilence.Pdf.PdfDocument).Assembly.GetType("Majorsilence.Pdf.InstalledFontScanner")!;
            var scan = scanner.GetMethod("Scan", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
            int faces = 0, dirs = 0;
            foreach (var root in new[] { "/usr/share/fonts", "/usr/local/share/fonts" })
            {
                if (!Directory.Exists(root)) continue;
                foreach (var dir in new[] { root }.Concat(Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories)))
                {
                    dirs++;
                    faces += ((System.Collections.ICollection)scan.Invoke(null, [dir])!).Count;
                }
            }
            Console.WriteLine($"  ({dirs} folders, {faces} faces)");
            return Task.CompletedTask;
        });

        Report report = null!;
        string source = "";
        await Step("config init", () => { RdlEngineConfig.RdlEngineConfigInit(); Directory.SetCurrentDirectory(WorkloadSupport.Folder); return Task.CompletedTask; });
        await Step("read rdl", () => { source = WorkloadSupport.ReadRdl(workload); return Task.CompletedTask; });
        await Step("parse", async () => report = await WorkloadSupport.Parse(source));
        report.Folder = WorkloadSupport.Folder;
        await Step("get data", () => report.RunGetData());
        await Step("render (first)", async () => await WorkloadSupport.Render(report, format));
        report.Dispose();

        for (int i = 2; i <= 3; i++)
        {
            Report r = null!;
            await Step($"parse #{i}", async () => r = await WorkloadSupport.Parse(source));
            r.Folder = WorkloadSupport.Folder;
            await Step($"get data #{i}", () => r.RunGetData());
            await Step($"render #{i}", async () => await WorkloadSupport.Render(r, format));
            r.Dispose();
        }

        Console.WriteLine($"total jit: {(JitInfo.GetCompilationTime() - jit0).TotalMilliseconds:F0} ms, {JitInfo.GetCompiledMethodCount() - methods0} methods");
    }
}
