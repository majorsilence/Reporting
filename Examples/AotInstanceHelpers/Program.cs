// AotInstanceHelpers — register an instance class (RDL <Classes> element) AOT-safely.
//
// Run:  dotnet run
// Output: orders-report.pdf in the output directory
//
// Key patterns shown:
//   - RdlEngineConfig.RegisterInstanceFactory tells the trimmer the constructor is needed
//     and bypasses the Assembly.CreateInstance call that fails under native AOT
//   - RdlEngineConfig.RegisterType tells the trimmer to preserve the public methods
//     so they survive trimming and are callable via reflection at render time
//   - Both calls are needed for full trim-safety when using <Classes> instance methods
//   - No <CodeModules> element is required — registration bypasses assembly loading
//   - Both calls must happen BEFORE RdlEngineConfigInit / RDLParser.Parse
//
// The RDL <Classes> element declares "Helper" as an instance of OrderFormatter.
// Report expressions then call =Helper.FormatDate(...), =Helper.FormatMoney(...), etc.
// Under traditional (non-AOT) usage the engine resolves the class by scanning loaded
// assemblies; RegisterInstanceFactory + RegisterType replaces that scan entirely.

using System.Diagnostics.CodeAnalysis;
using Majorsilence.Reporting.Rdl;

// Step 1: register the factory so the engine can create the instance without Assembly.CreateInstance.
RdlEngineConfig.RegisterInstanceFactory(
    "AotInstanceHelpers.OrderFormatter",
    () => new OrderFormatter());

// Step 2: register the type so the trimmer preserves all public methods on OrderFormatter.
// Skipping this step means the trimmer may remove FormatDate, FormatMoney, etc.
RdlEngineConfig.RegisterType(
    "AotInstanceHelpers.OrderFormatter",
    typeof(OrderFormatter));

RdlEngineConfig.RdlEngineConfigInit();

var baseDir = AppContext.BaseDirectory;
var rdlPath = Path.Combine(baseDir, "OrdersReport.rdl");
var dbPath  = Path.Combine(baseDir, "sqlitetestdb2.db");
var outPath = Path.Combine(baseDir, "orders-report.pdf");

var rdlXml = (await File.ReadAllTextAsync(rdlPath))
    .Replace("sqlitetestdb2.db", dbPath);

var rdlp = new RDLParser(rdlXml) { Folder = baseDir };
using var report = await rdlp.Parse();

if (report.ErrorMaxSeverity > 4)
{
    Console.Error.WriteLine("Report parse errors:");
    foreach (var err in report.ErrorItems)
        Console.Error.WriteLine($"  {err}");
    return 1;
}

var orders = new List<OrderRecord>
{
    new("ORD-001", "Widget A",      new DateTime(2024, 1, 15),   125.00m, "Shipped"),
    new("ORD-002", "Gadget Pro",    new DateTime(2024, 2,  3),  2499.99m, "Delivered"),
    new("ORD-003", "Cable Pack",    new DateTime(2024, 2, 14),    34.50m, "Delivered"),
    new("ORD-004", "Monitor 27\"",  new DateTime(2024, 3,  1),   549.00m, "Shipped"),
    new("ORD-005", "Keyboard MX",   new DateTime(2024, 3, 12),   139.00m, "Processing"),
    new("ORD-006", "USB Hub",       new DateTime(2024, 3, 20),    49.99m, "Processing"),
    new("ORD-007", "Laptop Stand",  new DateTime(2024, 4,  5),    89.00m, "Delivered"),
    new("ORD-008", "Docking Stn",   new DateTime(2024, 4, 18),   299.00m, "Shipped"),
};

await report.DataSets["Data"].SetData(orders);
await report.RunGetData(null);

var ofs = new OneFileStreamGen(outPath, true);
await report.RunRender(ofs, OutputPresentationType.PDF);

Console.WriteLine($"Written: {outPath}");
return 0;

record OrderRecord(string OrderId, string Product, DateTime OrderDate, decimal Amount, string Status);

/// <summary>
/// Instance helper class referenced in the RDL via the &lt;Classes&gt; element.
/// Registered AOT-safely via RegisterInstanceFactory + RegisterType.
/// </summary>
public class OrderFormatter
{
    public string FormatDate(object value) =>
        Convert.ToDateTime(value).ToString("MMM d, yyyy");

    public string FormatMoney(object value) =>
        $"{Convert.ToDecimal(value):C}";

    public string StatusColor(object status)
    {
        return (status?.ToString() ?? "") switch
        {
            "Delivered"  => "#C6EFCE",
            "Shipped"    => "#FFEB9C",
            "Processing" => "#FFCCCC",
            _            => "White",
        };
    }

    public string StatusTextColor(object status)
    {
        return (status?.ToString() ?? "") switch
        {
            "Delivered"  => "#276221",
            "Shipped"    => "#9C6500",
            "Processing" => "#9C0006",
            _            => "Black",
        };
    }
}
