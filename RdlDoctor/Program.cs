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
          rdl-doctor fix <path> [--in-place] [--strip-unknown]

        check options:
          --format text|json   Output format (default: text).
          --strict             Exit non-zero on warnings too, not just errors.

        fix options:
          --in-place            Overwrite the input file instead of writing <file>.fixed.rdl.
          --strip-unknown        Also remove elements the parser would silently ignore anyway.
                                  Never touches <Tablix> -- that needs a real rewrite, not deletion.

        Exit codes (check):
          0  no findings (or only informational notes)
          1  findings were reported (warnings and/or errors)
          2  a file could not be read or parsed as XML at all

        A .rpt file (Crystal Reports) can't be checked directly -- convert it to RDL first with
        the Majorsilence.Crystal library: https://github.com/majorsilence/majorsilence.crystal
        """);
}

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

switch (args[0])
{
    case "check": return await RunCheckAsync(args);
    case "fix": return await RunFixAsync(args);
    default:
        PrintUsage();
        return 1;
}

async Task<int> RunCheckAsync(string[] cliArgs)
{
    string? pathArg = null;
    string format = "text";
    bool strict = false;

    for (int i = 1; i < cliArgs.Length; i++)
    {
        switch (cliArgs[i])
        {
            case "--format" when i + 1 < cliArgs.Length: format = cliArgs[++i]; break;
            case "--strict": strict = true; break;
            default:
                if (pathArg == null) pathArg = cliArgs[i];
                break;
        }
    }

    if (pathArg == null)
    {
        Console.Error.WriteLine("error: a file path or glob is required.");
        PrintUsage();
        return 1;
    }

    if (Path.GetExtension(pathArg).Equals(".rpt", StringComparison.OrdinalIgnoreCase))
    {
        PrintCrystalPointer(pathArg);
        return 2;
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
}

async Task<int> RunFixAsync(string[] cliArgs)
{
    string? pathArg = null;
    bool inPlace = false;
    bool stripUnknown = false;

    for (int i = 1; i < cliArgs.Length; i++)
    {
        switch (cliArgs[i])
        {
            case "--in-place": inPlace = true; break;
            case "--strip-unknown": stripUnknown = true; break;
            default:
                if (pathArg == null) pathArg = cliArgs[i];
                break;
        }
    }

    if (pathArg == null || !File.Exists(pathArg))
    {
        Console.Error.WriteLine("error: a single existing file path is required.");
        PrintUsage();
        return 1;
    }

    if (Path.GetExtension(pathArg).Equals(".rpt", StringComparison.OrdinalIgnoreCase))
    {
        PrintCrystalPointer(pathArg);
        return 2;
    }

    RdlEngineConfig.RdlEngineConfigInit();

    string xml = await File.ReadAllTextAsync(pathArg);

    IReadOnlySet<string>? unknownNames = null;
    if (stripUnknown)
    {
        var findings = await CompatibilityChecker.CheckAsync(pathArg);
        unknownNames = findings
            .Where(f => f.Id == "DOC004" && f.Message.StartsWith("Unknown element <"))
            .Select(f => f.Message[("Unknown element <".Length)..f.Message.IndexOf('>')])
            .ToHashSet();
    }

    RdlFixer.FixResult result;
    try
    {
        result = RdlFixer.Fix(xml, stripUnknown, unknownNames);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"error: could not parse '{pathArg}' as XML: {ex.Message}");
        return 2;
    }

    if (result.ChangesMade.Count == 0)
    {
        Console.WriteLine("No changes to make.");
        return 0;
    }

    string outPath = inPlace ? pathArg : Path.Combine(
        Path.GetDirectoryName(Path.GetFullPath(pathArg)) ?? ".",
        Path.GetFileNameWithoutExtension(pathArg) + ".fixed" + Path.GetExtension(pathArg));

    await File.WriteAllTextAsync(outPath, result.Xml);

    Console.WriteLine($"Wrote {outPath}");
    foreach (var change in result.ChangesMade)
        Console.WriteLine($"  - {change}");
    return 0;
}

void PrintCrystalPointer(string rptPath)
{
    const string template = """
        '{0}' is a Crystal Reports file -- rdl-doctor only understands RDL/RDLC XML.

        Convert it to RDL first with Majorsilence.Crystal (a runtime-free Crystal .rpt reader
        and RDL converter -- no SAP Crystal Reports runtime or SDK required):

          https://github.com/majorsilence/majorsilence.crystal

            using Majorsilence.Crystal.Parser;
            using Majorsilence.Crystal.Converter;

            var result = RptParser.Parse("{1}");
            if (result.Success)
            {{
                string rdl = new RdlConverter().Convert(result.Report!);
                File.WriteAllText("{2}.rdl", rdl);
            }}

        Then run rdl-doctor check against the converted .rdl file. Subreports, cross-tabs, and
        charts are still on that converter's roadmap -- see its BACKLOG.md for current coverage.
        Connection strings can never be recovered from a .rpt file (Crystal encrypts them with a
        key the file doesn't carry), so the converted RDL's <ConnectString> always needs to be
        filled in by hand.
        """;
    Console.WriteLine(string.Format(template, rptPath, Path.GetFileName(rptPath), Path.GetFileNameWithoutExtension(rptPath)));
}

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
