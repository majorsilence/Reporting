// AotStaticHelpers — call static helper methods from RDL expressions in an AOT-safe way.
//
// Run:  dotnet run
// Output: sales-report.pdf in the output directory
//
// Key patterns shown:
//   - RdlEngineConfig.RegisterType pre-registers a static helper class so the trimmer
//     preserves its public methods when publishing with PublishAot=true or PublishTrimmed=true
//   - RDL expressions reference the class as =Namespace.ClassName.MethodName(args)
//   - No <CodeModules> element is required — registration bypasses assembly loading entirely
//   - RegisterType must be called BEFORE RdlEngineConfigInit / RDLParser.Parse
//
// Without RegisterType the trimmer may remove SalesHelpers methods, causing a
// MissingMethodException at runtime.  With RegisterType the methods are pinned.
//
// NOTE: method dispatch still uses reflection internally (FunctionCustomStatic), so
// this pattern covers trim-safety but not full native-AOT compatibility.  For fully
// AOT-compatible custom logic use RegisterCodeProvider + RdlCodeFunctions instead.

using System.Diagnostics.CodeAnalysis;
using Majorsilence.Reporting.Rdl;

// Pre-register the static helper class.
// The [DynamicallyAccessedMembers] annotation on RegisterType tells the trimmer to
// preserve all public methods and constructors on typeof(SalesHelpers).
RdlEngineConfig.RegisterType(
    "AotStaticHelpers.SalesHelpers",
    typeof(SalesHelpers));

RdlEngineConfig.RdlEngineConfigInit();

var baseDir = AppContext.BaseDirectory;
var rdlPath = Path.Combine(baseDir, "SalesReport.rdl");
var dbPath  = Path.Combine(baseDir, "sqlitetestdb2.db");
var outPath = Path.Combine(baseDir, "sales-report.pdf");

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

var salesData = new List<SaleRecord>
{
    new("Chai",                  "North America",  1250.00m, 50),
    new("Chang",                 "North America",   980.50m, 42),
    new("Aniseed Syrup",         "Europe",          432.00m, 24),
    new("Chef Anton's Cajun",    "Europe",         1875.25m, 75),
    new("Grandma's Boysenberry", "Asia Pacific",    640.00m, 32),
    new("Uncle Bob's Organic",   "North America",   315.60m, 18),
    new("Northwoods Cranberry",  "North America",   560.00m, 20),
    new("Mishi Kobe Niku",       "Asia Pacific",   4500.00m, 30),
    new("Ikura",                 "Asia Pacific",   1980.00m, 36),
    new("Queso Cabrales",        "Europe",          850.00m, 25),
};

await report.DataSets["Data"].SetData(salesData);
await report.RunGetData(null);

var ofs = new OneFileStreamGen(outPath, true);
await report.RunRender(ofs, OutputPresentationType.PDF);

Console.WriteLine($"Written: {outPath}");
return 0;

record SaleRecord(string Product, string Region, decimal Amount, int Quantity);

/// <summary>
/// Static formatting helpers used from RDL expressions.
/// RegisterType ensures these public methods survive trimming.
/// </summary>
public static class SalesHelpers
{
    public static string FormatMoney(object value) =>
        $"{Convert.ToDecimal(value):C}";

    public static string Tier(object amount)
    {
        decimal v = Convert.ToDecimal(amount);
        return v >= 2000 ? "Platinum" :
               v >= 1000 ? "Gold" :
               v >= 500  ? "Silver" : "Bronze";
    }

    public static string TierColor(object amount)
    {
        decimal v = Convert.ToDecimal(amount);
        return v >= 2000 ? "#4B0082" :   // dark violet
               v >= 1000 ? "#B8860B" :   // dark goldenrod
               v >= 500  ? "#708090" :   // slate gray
                           "#8B4513";    // saddle brown
    }
}
