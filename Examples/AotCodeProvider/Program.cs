// AotCodeProvider — replace an RDL <Code> VB element with C# delegates via RdlCodeFunctions.
//
// Run:  dotnet run
// Output: score-card.pdf in the output directory
//
// Key patterns shown:
//   - RdlEngineConfig.RegisterCodeProvider registers C# delegates before parsing
//   - The delegates replace the VB functions in the <Code> element at AOT runtime
//   - On non-AOT runtimes the <Code> element's VB source is compiled and used instead
//   - RegisterCodeProvider must be called BEFORE RdlEngineConfigInit / RDLParser.Parse
//
// The <Code> element in ScoreCard.rdl contains VB equivalents (for non-AOT use).
// Under AOT (PublishAot=true) those VB functions cannot be compiled at runtime —
// the registered C# delegates take over transparently.

using Majorsilence.Reporting.Rdl;

// Register C# delegates BEFORE initializing the engine — these replace the VB <Code> element.
// The method names must match exactly what the RDL expressions call (case-insensitive).
RdlEngineConfig.RegisterCodeProvider(report =>
    new RdlCodeFunctions()
        .Add("Grade", args =>
        {
            double score = Convert.ToDouble(args[0]);
            return score >= 90 ? "A" :
                   score >= 80 ? "B" :
                   score >= 70 ? "C" :
                   score >= 60 ? "D" : "F";
        })
        .Add("Commentary", args =>
        {
            double score = Convert.ToDouble(args[0]);
            return score >= 90 ? "Excellent" :
                   score >= 70 ? "Good" :
                   score >= 50 ? "Needs Improvement" : "Failing";
        })
        .Add("IsPassing", args => Convert.ToDouble(args[0]) >= 60)
);

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
