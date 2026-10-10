// AotPrecompiledCode — the report's VB <Code> block runs under Native AOT with no hand-written C#.
//
// ReportCode.vbproj compiles the <Code> element of ScoreCard.rdl at BUILD time (RdlCodeGen), so the
// VB compiler is never needed at runtime. RdlPrecompiledCode.Register() is generated; call it once
// at startup, before parsing. Compare with ../AotCodeProvider, which registers C# delegates by hand.

using Majorsilence.Reporting.Rdl;

ReportCode.RdlPrecompiledCode.Register();

RdlEngineConfig.RdlEngineConfigInit();

var baseDir = AppContext.BaseDirectory;
var rdlPath = Path.Combine(baseDir, "ScoreCard.rdl");
var dbPath  = Path.Combine(baseDir, "sqlitetestdb2.db");
var outPath = Path.Combine(baseDir, "score-card.pdf");

// The RDL uses sqlitetestdb2.db only for parse-time schema validation.
// At runtime, SetData below provides all the data — the query never runs.
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

// Build the data in code — could come from an API, service, LINQ query, etc.
var students = new List<StudentScore>
{
    new("Alice Chen",      95.5),
    new("Bob Smith",       82.0),
    new("Carol Jones",     74.5),
    new("David Kim",       61.0),
    new("Eva Müller",      88.5),
    new("Frank Okafor",    45.0),
    new("Grace Lin",       91.0),
    new("Henry Patel",     67.5),
    new("Isabelle Blanc",  77.0),
    new("Jorge Silva",     53.0),
};

await report.DataSets["Data"].SetData(students);
await report.RunGetData(null);

var ofs = new OneFileStreamGen(outPath, true);
await report.RunRender(ofs, OutputPresentationType.PDF);

Console.WriteLine($"Written: {outPath}");
return 0;

// Property names must match the <Field Name="..."> values in ScoreCard.rdl exactly
record StudentScore(string Name, double Score);
