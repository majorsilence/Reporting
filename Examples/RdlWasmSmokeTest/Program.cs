// RdlWasmSmokeTest -- renders reports with Majorsilence.Reporting.RdlEngine as a browser-wasm app.
//
//   dotnet publish -c Release
//   node run-node.mjs
//
// Exit code 0 and "RDL-WASM-SMOKE-TEST-OK" mean every scenario rendered the expected output. All
// data is pushed in: the browser cannot open database sockets, so there are no database scenarios.

using System.Text;
using Majorsilence.Reporting.Rdl;
using RdlAotSmokeTest;

RdlEngineConfig.RegisterType("AotSmoke.Helpers", typeof(Helpers));
RdlEngineConfig.RegisterType("AotSmoke.Greeter", typeof(Greeter));
RdlEngineConfig.RegisterInstanceFactory("AotSmoke.Greeter", () => new Greeter());
RdlEngineConfig.RdlEngineConfigInit();

var work = "/work";                       // the runtime's in-memory file system
Directory.CreateDirectory(work);

var failures = new List<string>();
int passed = 0;
async Task Scenario(string name, Func<Task> body)
{
    try { await body(); passed++; Console.WriteLine($"  ok    {name}"); }
    catch (Exception e)
    {
        failures.Add(name);
        var inner = e; while (inner.InnerException != null) inner = inner.InnerException;
        Console.WriteLine($"  FAIL  {name}: {e.GetType().Name}: {e.Message} -> {inner.GetType().Name}: {inner.Message}");
        Console.WriteLine(inner.StackTrace);
    }
}

Task Items(Report r) { r.DataSets["Data"].SetData(new List<Item> { new("Chai", 3), new("Ikura", 5) }); return Task.CompletedTask; }
var itemsRdl = ReportBuilder.Table(new[] { "Name:String", "Qty:Int32" }, new[] { "=Fields!Name.Value", "=Fields!Qty.Value" });
var salesRows = new List<SalesRow>
{
    new("North", "2023", 10), new("North", "2024", 20), new("South", "2023", 5), new("South", "2024", 7),
};
Task Sales(Report r) { r.DataSets["Data"].SetData(salesRows); return Task.CompletedTask; }
Task One(Report r) { r.DataSets["Data"].SetCollectionData(new[] { new Dictionary<string, object> { ["N"] = 7, ["S"] = "abcdef", ["Who"] = "WASM" } }); return Task.CompletedTask; }

await Scenario("built-in functions", async () =>
{
    var rdl = ReportBuilder.Table(new[] { "N:Int32", "S:String" }, new[]
    {
        "=Math.Max(Fields!N.Value, 9)", "=Left(Fields!S.Value, 3)", "=UCase(Fields!S.Value)", "=Mid(Fields!S.Value, 2, 2)",
        "=Len(Fields!S.Value)", "=Replace(Fields!S.Value, \"cd\", \"XY\")", "=IIf(Fields!N.Value > 5, \"big\", \"small\")",
        "=Format(1234.5, \"0.0\")", "=Convert.ToInt32(\"42\") + 1", "=Math.Round(2.567, 1)",
    });
    Expect.Contains(await Render.Csv(work, rdl, One), "9", "abc", "ABCDEF", "bc", "6", "abXYef", "big", "1234.5", "43", "2.6");
});

await Scenario("registered static helper class", async () =>
{
    var rdl = ReportBuilder.Table(new[] { "N:Int32" }, new[] { "=AotSmoke.Helpers.Triple(Fields!N.Value)" });
    Expect.Contains(await Render.Csv(work, rdl, One), "21");
});

await Scenario("grouping and aggregates", async () =>
{
    var csv = await Render.Csv(work, ReportBuilder.GroupedTable(), r =>
    {
        r.DataSets["Data"].SetData(new List<Sale> { new("North", 10), new("North", 20), new("South", 5), new("South", 7), new("South", 8) });
        return Task.CompletedTask;
    });
    Expect.Contains(csv, "North", "South", "30", "20", "50");
});

await Scenario("subreport", async () =>
{
    File.WriteAllText(Path.Combine(work, "Sub.rdl"), ReportBuilder.StaticReport("SUBREPORT-OK"));
    Expect.Contains(await Render.Text(work, ReportBuilder.WithSubreport("Sub"), OutputPresentationType.HTML, One), "SUBREPORT-OK");
});

await Scenario("matrix", async () =>
    Expect.Contains(await Render.Text(work, ReportBuilder.Matrix(), OutputPresentationType.HTML, Sales), "North", "South", "2023", "2024", "20", "7"));

await Scenario("list", async () =>
    Expect.Contains(await Render.Text(work, ReportBuilder.ListReport(), OutputPresentationType.HTML, Sales), "North / 2023", "South / 2024"));

await Scenario("PDF output", async () => Expect.ValidPdf(await Render.Bytes(work, itemsRdl, OutputPresentationType.PDF, Items)));

foreach (var format in new[] { OutputPresentationType.HTML, OutputPresentationType.XML, OutputPresentationType.RTF, OutputPresentationType.CSV })
    await Scenario($"{format} output", async () =>
        Expect.Contains(Encoding.UTF8.GetString(await Render.Bytes(work, itemsRdl, format, Items)), "Chai", "Ikura"));

await Scenario("Excel2007 output", async () =>
    Expect.ZipHeader(await Render.Bytes(work, itemsRdl, OutputPresentationType.Excel2007, Items)));

await Scenario("chart in a PDF", async () =>
    Expect.ValidPdf(await Render.Bytes(work, ReportBuilder.Chart(), OutputPresentationType.PDF, Sales)));

await Scenario("chart in a TIFF", async () =>
    Expect.Tiff(await Render.Bytes(work, ReportBuilder.Chart(), OutputPresentationType.TIF, Sales)));

await Scenario("SetData(DataTable)", async () =>
{
    var t = new System.Data.DataTable();
    t.Columns.Add("Name", typeof(string)); t.Columns.Add("Qty", typeof(int));
    t.Rows.Add("Chai", 3); t.Rows.Add("Ikura", 5);
    Expect.Contains(await Render.Csv(work, itemsRdl, r => { r.DataSets["Data"].SetData(t); return Task.CompletedTask; }), "Chai", "Ikura", "3", "5");
});

await Scenario("SetData(XmlDocument)", async () =>
{
    var xml = new System.Xml.XmlDocument();
    xml.LoadXml("<Rows><Row><Name>Chai</Name><Qty>3</Qty></Row><Row><Name>Ikura</Name><Qty>5</Qty></Row></Rows>");
    Expect.Contains(await Render.Csv(work, itemsRdl, r => { r.DataSets["Data"].SetData(xml); return Task.CompletedTask; }), "Chai", "Ikura", "3", "5");
});

await Scenario("AES-256 encrypted PDF", async () =>
{
    var bytes = await Render.SecuredPdf(work, itemsRdl, Majorsilence.Pdf.Security.PdfSecurity.Protect("user123", "owner456"), null, Items);
    Expect.ValidPdf(bytes);
    if (Encoding.Latin1.GetString(bytes).Contains("Chai")) throw new InvalidOperationException("content was not encrypted");
});

// Unlike Render.*, this lets the engine run the report's own query (SkipDatabaseSchemaValidation also skips that).
async Task<(string Csv, string Errors)> RunQuery(string rdl)
{
    var parser = new RDLParser(rdl) { Folder = work };
    using var report = await parser.Parse();
    report.Folder = work;
    await report.RunGetData();
    var path = Path.Combine(work, Guid.NewGuid().ToString("N") + ".csv");
    await report.RunRender(new OneFileStreamGen(path, true), OutputPresentationType.CSV);
    var errors = string.Join(" | ", (report.ErrorItems ?? new List<string>()).Cast<object>());
    return (File.ReadAllText(path), errors);
}

// The data providers that read files work against the runtime's in-memory file system.
await Scenario("JSON data provider reading a file", async () =>
{
    File.WriteAllText(Path.Combine(work, "items.json"), "{\"Items\":[{\"Name\":\"Chai\",\"Qty\":3},{\"Name\":\"Ikura\",\"Qty\":5}]}");
    var rdl = itemsRdl
        .Replace("<DataProvider>SQLite</DataProvider>", "<DataProvider>Json</DataProvider>")
        .Replace("Data Source=this-file-does-not-exist.db", "file=" + Path.Combine(work, "items.json"))
        .Replace("<CommandText>SELECT 1</CommandText>", "<CommandText>columns=Name,Qty;table=root_Items</CommandText>");
    Expect.Contains((await RunQuery(rdl)).Csv, "Chai", "Ikura", "3", "5");
});

await Scenario("XML data provider reading a file", async () =>
{
    File.WriteAllText(Path.Combine(work, "items.xml"), "<Rows><Row><Name>Chai</Name><Qty>3</Qty></Row><Row><Name>Ikura</Name><Qty>5</Qty></Row></Rows>");
    var rdl = itemsRdl
        .Replace("<DataProvider>SQLite</DataProvider>", "<DataProvider>xml</DataProvider>")
        .Replace("Data Source=this-file-does-not-exist.db", "")
        .Replace("<CommandText>SELECT 1</CommandText>", "<CommandText>file=" + Path.Combine(work, "items.xml") + ";RowsXPath=/Rows/Row</CommandText>");
    Expect.Contains((await RunQuery(rdl)).Csv, "Chai", "Ikura", "3", "5");
});

// The engine fetches http(s) data through IAsyncDataCommand (HttpClient.SendAsync), which the browser allows, so
// the request is really made. Nothing is reachable from the test, so it must fail with an HTTP error rather than
// the old "synchronous HTTP" restriction, and it must fail rather than hang.
await Scenario("HTTP data source is fetched asynchronously and an unreachable host fails instead of hanging", async () =>
{
    var rdl = itemsRdl
        .Replace("<DataProvider>SQLite</DataProvider>", "<DataProvider>Json</DataProvider>")
        .Replace("Data Source=this-file-does-not-exist.db", "file=https://example.invalid/items.json")
        .Replace("<CommandText>SELECT 1</CommandText>", "<CommandText>columns=Name,Qty;table=root_Items</CommandText>");
    string errors;
    try { errors = (await RunQuery(rdl)).Errors; }
    catch (Exception e) { errors = e.ToString(); }
    if (errors.Contains("synchronous HTTP"))
        throw new InvalidOperationException("the data source still used a synchronous request; got: " + errors);
    if (!errors.Contains("HttpRequestException"))
        throw new InvalidOperationException("expected the failed fetch to be reported as an HttpRequestException; got: " + errors);
});

Console.WriteLine($"{passed} passed, {failures.Count} failed");
if (failures.Count > 0)
{
    Console.WriteLine("RDL-WASM-SMOKE-TEST-FAILED: " + string.Join("; ", failures));
    return 1;
}
Console.WriteLine("RDL-WASM-SMOKE-TEST-OK");
return 0;

record Item(string Name, int Qty);
record Sale(string Region, int Amount);
record SalesRow(string Region, string Year, int Sales);

public static class Helpers { public static int Triple(object value) => Convert.ToInt32(value) * 3; }
public class Greeter { public string Hello(object who) => $"Hello, {who}!"; }
