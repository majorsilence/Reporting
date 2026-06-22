// SetDataFromCode — feed a List<T> directly into a report with no database at all.
//
// Run:  dotnet run
// Output: sales-report.pdf in the output directory
//
// Key patterns shown:
//   - SkipDatabaseSchemaValidation = true eliminates the parse-time DB requirement
//   - DataSets["Data"].SetData(IEnumerable<T>) injects data from any .NET collection
//   - The public property names of T must exactly match the field names in the RDL
//   - RunGetData(null) is still required — it resolves parameters and sub-reports
//
// This pattern is useful when your data comes from an API, a service layer,
// a LINQ query, or any in-memory source and you have no database available.

using Majorsilence.Reporting.Rdl;

RdlEngineConfig.RdlEngineConfigInit();

var baseDir = AppContext.BaseDirectory;
var rdlPath = Path.Combine(baseDir, "SalesReport.rdl");
var outPath = Path.Combine(baseDir, "sales-report.pdf");

// SkipDatabaseSchemaValidation = true: no DB connection is opened at parse time
// or runtime. The connection string in the RDL is ignored entirely.
var rdlXml = await File.ReadAllTextAsync(rdlPath);
var rdlp = new RDLParser(rdlXml)
{
    Folder = baseDir,
    SkipDatabaseSchemaValidation = true
};
using var report = await rdlp.Parse();

if (report.ErrorMaxSeverity > 4)
{
    Console.Error.WriteLine("Report parse errors:");
    foreach (var err in report.ErrorItems)
        Console.Error.WriteLine($"  {err}");
    return 1;
}

// Build the data in code — could come from an API, service, LINQ query, etc.
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
    new("Queso Manchego La",     "Europe",          720.00m, 30),
    new("Konbu",                 "Asia Pacific",    180.00m, 24),
    new("Tofu",                  "Asia Pacific",    560.00m, 40),
    new("Genen Shouyu",          "Asia Pacific",    310.00m, 26),
    new("Pavlova",               "Asia Pacific",    825.00m, 55),
    new("Alice Mutton",          "Europe",         2340.00m, 26),
    new("Carnarvon Tigers",      "Asia Pacific",   6200.00m, 31),
    new("Teatime Biscuits",      "Europe",          291.60m, 36),
    new("Sir Rodney's Marmalade","Europe",         1245.00m, 45),
    new("Sir Rodney's Scones",   "Europe",          350.00m, 50),
};

// Inject data directly — property names must match RDL field names exactly
await report.DataSets["Data"].SetData(salesData);

// RunGetData still required to complete parameter processing
await report.RunGetData(null);

var ofs = new OneFileStreamGen(outPath, true);
await report.RunRender(ofs, OutputPresentationType.PDF);

Console.WriteLine($"Written: {outPath}");
return 0;

// Property names must match the <Field Name="..."> values in SalesReport.rdl exactly
record SaleRecord(string Product, string Region, decimal Amount, int Quantity);
