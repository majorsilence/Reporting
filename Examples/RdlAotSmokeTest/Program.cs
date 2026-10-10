// RdlAotSmokeTest -- renders reports with Majorsilence.Reporting.RdlEngine under Native AOT.
//
// Publish and run as a self-contained AOT binary to verify the engine actually works when it is
// compiled ahead of time (not just that it builds without trim/AOT analyzer warnings):
//
//   dotnet publish -c Release -r linux-x64 -p:PublishAot=true --self-contained true
//   ./bin/Release/net10.0/linux-x64/publish/RdlAotSmokeTest
//
// Exit code 0 and "RDL-AOT-SMOKE-TEST-OK" on stdout mean every scenario below rendered the
// expected output. Any failure prints the scenario and exits 1.
//
// Covers: built-in expression functions (Math, Convert, String and the VB functions), static and
// instance helper classes registered with RegisterType / RegisterInstanceFactory, a custom report
// item registered with RegisterCustomReportItem<T>, pushed data (SetData<T>, SetCollectionData),
// grouping and aggregates, a subreport, and the PDF / HTML / XML / CSV / RTF / Excel renderers.
//
// The reports declare a data source only because the schema needs one; no database is opened
// (SkipDatabaseSchemaValidation) and all data is pushed in.

using System.Text;
using Majorsilence.Reporting.Cri;
using Majorsilence.Reporting.Rdl;
using RdlAotSmokeTest;
#pragma warning disable CS8321

// Everything the reports reference by name must be registered before RdlEngineConfigInit, and
// before parsing: this is what keeps the trimmer from removing it.
RdlEngineConfig.RegisterType("AotSmoke.Helpers", typeof(Helpers));
RdlEngineConfig.RegisterType("AotSmoke.Greeter", typeof(Greeter));
RdlEngineConfig.RegisterInstanceFactory("AotSmoke.Greeter", () => new Greeter());
RdlEngineConfig.RegisterCustomReportItem<QrCode>("QRCode");
RdlEngineConfig.RdlEngineConfigInit();

var work = Path.Combine(Path.GetTempPath(), "rdl-aot-smoke-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(work);

var failures = new List<string>();
int passed = 0;

async Task Scenario(string name, Func<Task> body)
{
    try
    {
        await body();
        passed++;
        Console.WriteLine($"  ok    {name}");
    }
    catch (Exception e)
    {
        failures.Add(name);
        Console.WriteLine($"  FAIL  {name}: {e.GetType().Name}: {e.Message}");
    }
}

await Scenario("built-in functions", async () =>
{
    // 7 and "abcdef" are pushed in, so the engine cannot fold the expressions at parse time.
    var rdl = ReportBuilder.Table(
        fields: new[] { "N:Int32", "S:String" },
        cells: new[]
        {
            "=Math.Max(Fields!N.Value, 9)",
            "=Left(Fields!S.Value, 3)",
            "=UCase(Fields!S.Value)",
            "=Mid(Fields!S.Value, 2, 2)",
            "=Len(Fields!S.Value)",
            "=Replace(Fields!S.Value, \"cd\", \"XY\")",
            "=IIf(Fields!N.Value > 5, \"big\", \"small\")",
            "=Format(1234.5, \"0.0\")",
            "=Convert.ToInt32(\"42\") + 1",
            "=Math.Round(2.567, 1)",
        });
    var csv = await Render.Csv(work, rdl, r =>
        r.DataSets["Data"].SetCollectionData(new[] { new Dictionary<string, object> { ["N"] = 7, ["S"] = "abcdef" } }));
    Expect.Contains(csv, "9", "abc", "ABCDEF", "bc", "6", "abXYef", "big", "1234.5", "43", "2.6");
});

await Scenario("registered static helper class", async () =>
{
    var rdl = ReportBuilder.Table(new[] { "N:Int32" }, new[] { "=AotSmoke.Helpers.Triple(Fields!N.Value)" });
    var csv = await Render.Csv(work, rdl, r =>
        r.DataSets["Data"].SetCollectionData(new[] { new Dictionary<string, object> { ["N"] = 7 } }));
    Expect.Contains(csv, "21");
});

await Scenario("registered instance helper class", async () =>
{
    var rdl = ReportBuilder.Table(new[] { "Who:String" }, new[] { "=Greeter.Hello(Fields!Who.Value)" },
        classes: "<Classes><Class><ClassName>AotSmoke.Greeter</ClassName><InstanceName>Greeter</InstanceName></Class></Classes>");
    var csv = await Render.Csv(work, rdl, r =>
        r.DataSets["Data"].SetCollectionData(new[] { new Dictionary<string, object> { ["Who"] = "AOT" } }));
    Expect.Contains(csv, "Hello, AOT!");
});

await Scenario("SetData<T> maps properties of T", async () =>
{
    var rdl = ReportBuilder.Table(new[] { "Name:String", "Qty:Int32" }, new[] { "=Fields!Name.Value", "=Fields!Qty.Value" });
    var csv = await Render.Csv(work, rdl, r =>
        r.DataSets["Data"].SetData(new List<Item> { new("Chai", 3), new("Ikura", 5) }));
    Expect.Contains(csv, "Chai", "Ikura", "3", "5");
});

await Scenario("grouping and aggregates", async () =>
{
    var rdl = ReportBuilder.GroupedTable();
    var csv = await Render.Csv(work, rdl, r =>
        r.DataSets["Data"].SetData(new List<Sale>
        {
            new("North", 10), new("North", 20), new("South", 5), new("South", 7), new("South", 8),
        }));
    // Group totals 30 and 20, group counts 2 and 3, grand total 50.
    Expect.Contains(csv, "North", "South", "30", "20", "50");
});

await Scenario("subreport", async () =>
{
    File.WriteAllText(Path.Combine(work, "Sub.rdl"), ReportBuilder.StaticReport("SUBREPORT-OK"));
    var rdl = ReportBuilder.WithSubreport("Sub");
    var html = await Render.Text(work, rdl, OutputPresentationType.HTML, r =>
        r.DataSets["Data"].SetCollectionData(new[] { new Dictionary<string, object> { ["N"] = 1 } }));
    Expect.Contains(html, "SUBREPORT-OK");
});

await Scenario("custom report item (QR code) in a PDF", async () =>
{
    var rdl = ReportBuilder.WithQrCode();
    var bytes = await Render.Bytes(work, rdl, OutputPresentationType.PDF, r =>
        r.DataSets["Data"].SetCollectionData(new[] { new Dictionary<string, object> { ["N"] = 1 } }));
    Expect.ValidPdf(bytes);
});

await Scenario("PDF output", async () =>
{
    var rdl = ReportBuilder.Table(new[] { "Name:String", "Qty:Int32" }, new[] { "=Fields!Name.Value", "=Fields!Qty.Value" });
    var bytes = await Render.Bytes(work, rdl, OutputPresentationType.PDF, r =>
        r.DataSets["Data"].SetData(new List<Item> { new("Chai", 3), new("Ikura", 5) }));
    Expect.ValidPdf(bytes);
});

foreach (var format in new[]
{
    OutputPresentationType.HTML, OutputPresentationType.XML, OutputPresentationType.RTF,
    OutputPresentationType.Excel2007, OutputPresentationType.CSV,
})
{
    await Scenario($"{format} output", async () =>
    {
        var rdl = ReportBuilder.Table(new[] { "Name:String", "Qty:Int32" }, new[] { "=Fields!Name.Value", "=Fields!Qty.Value" });
        var bytes = await Render.Bytes(work, rdl, format, r =>
            r.DataSets["Data"].SetData(new List<Item> { new("Chai", 3), new("Ikura", 5) }));
        if (format == OutputPresentationType.Excel2007)
            Expect.ZipHeader(bytes);                            // xlsx is a zip
        else
            Expect.Contains(Encoding.UTF8.GetString(bytes), "Chai", "Ikura");
    });
}

try { Directory.Delete(work, true); } catch { /* temp files; not worth failing over */ }

Console.WriteLine($"{passed} passed, {failures.Count} failed");
if (failures.Count > 0)
{
    Console.Error.WriteLine("RDL-AOT-SMOKE-TEST-FAILED: " + string.Join("; ", failures));
    return 1;
}
Console.WriteLine("RDL-AOT-SMOKE-TEST-OK");
return 0;

namespace RdlAotSmokeTest
{
    record Item(string Name, int Qty);
    record Sale(string Region, int Amount);

    /// <summary>Static helpers called from RDL as =AotSmoke.Helpers.Triple(...).</summary>
    public static class Helpers
    {
        public static int Triple(object value) => Convert.ToInt32(value) * 3;
    }

    /// <summary>Instance helper declared in the report's &lt;Classes&gt; element.</summary>
    public class Greeter
    {
        public string Hello(object who) => $"Hello, {who}!";
    }
}
