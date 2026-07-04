// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using Majorsilence.Reporting.Rdl;
using Majorsilence.Reporting.RdlDoctor;

void PrintUsage()
{
    Console.WriteLine("""
        rdl-doctor -- scans .rdl/.rdlc files for constructs Majorsilence Reporting's RDL engine
        doesn't support (a compatibility-check tool for migrating from SSRS 2008+ or Crystal
        Reports).

        Usage:
          rdl-doctor check <path-or-glob> [--format text|json] [--strict]

        Options:
          --format text|json   Output format (default: text).
          --strict             Exit non-zero on warnings too, not just errors.

        Exit codes:
          0  no findings (or only informational notes)
          1  findings were reported (warnings and/or errors)
          2  a file could not be read or parsed as XML at all
        """);
}

if (args.Length == 0 || args[0] != "check")
{
    PrintUsage();
    return 1;
}

string? pathArg = null;
string format = "text";
bool strict = false;

for (int i = 1; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--format" when i + 1 < args.Length: format = args[++i]; break;
        case "--strict": strict = true; break;
        default:
            if (pathArg == null) pathArg = args[i];
            break;
    }
}

if (pathArg == null)
{
    Console.Error.WriteLine("error: a file path or glob is required.");
    PrintUsage();
    return 1;
}

RdlEngineConfig.RdlEngineConfigInit();

var files = ResolveFiles(pathArg);
if (files.Count == 0)
{
    Console.Error.WriteLine($"error: no files matched '{pathArg}'.");
    return 2;
}

var results = new List<(string File, IReadOnlyList<Finding> Findings)>();
bool anyParseFailure = false;

foreach (var file in files)
{
    IReadOnlyList<Finding> findings;
    try
    {
        findings = await CompatibilityChecker.CheckAsync(file);
    }
    catch (Exception ex)
    {
        findings = new[] { new Finding("DOC000", FindingSeverity.Error, $"Could not read or parse file: {ex.Message}") };
        anyParseFailure = true;
    }
    results.Add((file, findings));
}

if (format == "json")
{
    var payload = results.ToDictionary(
        r => r.File,
        r => r.Findings.Select(f => new { f.Id, Severity = f.Severity.ToString(), f.Message }).ToList());
    Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
}
else
{
    foreach (var (file, findings) in results)
    {
        Console.WriteLine(file);
        if (findings.Count == 0)
        {
            Console.WriteLine("  (no findings)");
        }
        else
        {
            foreach (var finding in findings)
                Console.WriteLine($"  [{finding.Severity,-7}] {finding.Id}: {finding.Message}");
        }
        Console.WriteLine();
    }
}

if (anyParseFailure) return 2;

bool hasError = results.Any(r => r.Findings.Any(f => f.Severity == FindingSeverity.Error));
bool hasWarning = results.Any(r => r.Findings.Any(f => f.Severity == FindingSeverity.Warning));
if (hasError || (strict && hasWarning)) return 1;
return 0;

static List<string> ResolveFiles(string pathOrGlob)
{
    if (File.Exists(pathOrGlob)) return new List<string> { pathOrGlob };

    string dir = Path.GetDirectoryName(pathOrGlob) is { Length: > 0 } d ? d : ".";
    string pattern = Path.GetFileName(pathOrGlob);
    if (!Directory.Exists(dir)) return new List<string>();

    return Directory.GetFiles(dir, pattern, SearchOption.TopDirectoryOnly)
        .OrderBy(f => f, StringComparer.Ordinal)
        .ToList();
}
