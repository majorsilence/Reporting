// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;
using System.Xml.Linq;
using Majorsilence.Reporting.Rdl;

namespace Majorsilence.Reporting.RdlDoctor;

/// <summary>
/// Scans an RDL/RDLC file for constructs Majorsilence Reporting's RDL engine doesn't support.
/// Runs two passes: a raw <see cref="XDocument"/> scan (works even on files the real parser
/// can't load) for structural checks, and a real <see cref="RDLParser"/> parse to relay the
/// engine's own "unknown element" warnings verbatim.
/// </summary>
public static class CompatibilityChecker
{
    // Verified against RdlEngine/Definition/ReportItems.cs and ChartType.cs: these are 2008 R2+
    // data regions with zero handling anywhere in the engine. "Map" as a standalone report item
    // (this data region) is checked by element name only, which does not collide with the
    // supported legacy <ChartType>Map</ChartType> chart subtype (that's text content of a
    // <ChartType> element, not an element literally named <Map>).
    private static readonly string[] UnsupportedReportItems = { "Gauge", "Indicator", "Sparkline", "Map" };

    // Verified against RdlEngine/ExprParser/Parser.cs's function switch (lines ~657-903): these
    // three lookup functions are not implemented anywhere and any expression calling them throws
    // ParserException at FinalPass time.
    private static readonly string[] UnsupportedFunctions = { "Lookup", "LookupSet", "MultiLookup" };

    private static readonly Regex ExpressionFunctionCall = new(
        @"=(?:.*?[^A-Za-z0-9_.])?(Lookup|LookupSet|MultiLookup)\s*\(",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex UnknownElementMessage = new(
        @"^Severity:\s*\d+\s*-\s*Unknown (?<context>\w+) element '(?<name>[^']+)' ignored\.$",
        RegexOptions.Compiled);

    private static readonly (string Uri, string Label)[] KnownNamespaces =
    {
        ("http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition", "2005"),
        ("http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition", "2008"),
        ("http://schemas.microsoft.com/sqlserver/reporting/2008/10/reportdefinition", "2008 R2"),
        ("http://schemas.microsoft.com/sqlserver/reporting/2010/01/reportdefinition", "2010"),
        ("http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition", "2016"),
    };

    /// <summary>
    /// Checks a file. Throws if the file isn't well-formed XML at all -- callers should treat
    /// that as a harder failure (exit code 2) than an ordinary compatibility finding (exit 1).
    /// External resources (subreports, images) and data providers are resolved relative to the
    /// file's own directory.
    /// </summary>
    public static async Task<IReadOnlyList<Finding>> CheckAsync(string filePath)
    {
        string xml = await File.ReadAllTextAsync(filePath);
        var findings = new List<Finding>();
        string folder = Path.GetDirectoryName(Path.GetFullPath(filePath)) ?? ".";

        // Let a malformed-XML exception propagate -- the caller distinguishes "not even XML"
        // (exit 2) from "valid XML with compatibility issues" (exit 1, an ordinary Finding).
        XDocument doc = XDocument.Parse(xml, LoadOptions.SetLineInfo);

        CheckNamespace(doc, findings);            // DOC002
        CheckTablix(doc, findings);               // DOC001
        CheckUnsupportedItems(doc, findings);      // DOC003
        CheckExpressionFunctions(doc, findings);   // DOC005
        await CheckUnknownElementsAsync(xml, filePath, findings); // DOC004
        CheckSharedDataSetReference(doc, findings); // DOC006
        CheckDataProviders(doc, findings);          // DOC007
        CheckCodeModulesAndClasses(doc, findings);  // DOC008
        CheckExternalResources(doc, folder, findings); // DOC009
        CheckRdlcQuirks(doc, filePath, findings);   // DOC010

        return findings;
    }

    private static void CheckTablix(XDocument doc, List<Finding> findings)
    {
        if (doc.Descendants().Any(e => e.Name.LocalName == "Tablix"))
        {
            findings.Add(new Finding("DOC001", FindingSeverity.Error,
                "<Tablix> is not supported. Majorsilence Reporting's RDL engine has no handling for the SSRS 2008+ Tablix data region -- convert it to a Table or Matrix (SSRS 2005-style) report item."));
        }
    }

    private static void CheckNamespace(XDocument doc, List<Finding> findings)
    {
        string? uri = doc.Root?.Name.NamespaceName;
        if (string.IsNullOrEmpty(uri))
        {
            findings.Add(new Finding("DOC002", FindingSeverity.Info,
                "Report has no declared RDL namespace. Majorsilence Reporting's parser checks only the root element's local name (\"Report\"), not its namespace, so this has no effect on compatibility."));
            return;
        }

        var known = KnownNamespaces.FirstOrDefault(k => k.Uri == uri);
        if (known.Uri == null)
        {
            findings.Add(new Finding("DOC002", FindingSeverity.Info,
                $"Report declares an unrecognized RDL namespace ({uri}). This has no effect on parsing -- Majorsilence Reporting's parser only checks the root element's local name, not the namespace version."));
        }
        else if (known.Label != "2005")
        {
            findings.Add(new Finding("DOC002", FindingSeverity.Info,
                $"Report declares the RDL {known.Label} namespace. Note: the declared schema version has no effect here -- Majorsilence Reporting's parser is namespace-agnostic and only checks the root element's local name. Compatibility depends entirely on which specific elements the report actually uses (see the other findings in this report), not the declared version."));
        }
    }

    private static void CheckUnsupportedItems(XDocument doc, List<Finding> findings)
    {
        var found = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var element in doc.Descendants())
        {
            if (UnsupportedReportItems.Contains(element.Name.LocalName, StringComparer.OrdinalIgnoreCase))
                found.Add(element.Name.LocalName);
        }

        foreach (var name in found)
        {
            findings.Add(new Finding("DOC003", FindingSeverity.Error,
                $"<{name}> is not supported. Gauge, Indicator, Sparkline, and the standalone Map data region are SSRS 2008 R2+ report items with no handling in Majorsilence Reporting's RDL engine."));
        }
    }

    private static void CheckExpressionFunctions(XDocument doc, List<Finding> findings)
    {
        var reported = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var element in doc.Descendants())
        {
            string text = element.Value;
            if (string.IsNullOrEmpty(text) || !text.Contains('=')) continue;

            foreach (Match match in ExpressionFunctionCall.Matches(text))
            {
                string fn = match.Groups[1].Value;
                if (reported.Add(fn))
                {
                    findings.Add(new Finding("DOC005", FindingSeverity.Error,
                        $"Expression function \"{fn}\" is not supported. Majorsilence Reporting's expression parser has no implementation for Lookup, LookupSet, or MultiLookup -- an expression calling {fn}(...) will throw at render time. Rewrite the query to join the data instead of looking it up in the expression."));
                }
            }
        }
    }

    private static async Task CheckUnknownElementsAsync(string xml, string filePath, List<Finding> findings)
    {
        try
        {
            var parser = new RDLParser(xml) { Folder = Path.GetDirectoryName(Path.GetFullPath(filePath)) ?? "." };
            var report = await parser.Parse();

            var seen = new HashSet<(string Context, string Name)>();
            foreach (var itemObj in report.ErrorItems)
            {
                string item = itemObj?.ToString() ?? "";
                var match = UnknownElementMessage.Match(item);
                if (!match.Success) continue;

                var key = (Context: match.Groups["context"].Value, Name: match.Groups["name"].Value);
                if (!seen.Add(key)) continue;

                findings.Add(new Finding("DOC004", FindingSeverity.Warning,
                    $"Unknown element <{key.Name}> inside <{key.Context}> is ignored by the parser (not fatal, but silently dropped -- any content in it is lost)."));
            }

            if (report.ErrorMaxSeverity >= 8)
            {
                findings.Add(new Finding("DOC004", FindingSeverity.Error,
                    $"Report has parse errors severe enough to prevent rendering (severity {report.ErrorMaxSeverity}). Run with --format json for the full engine error list, or open the file with RdlEngine directly to see report.ErrorItems."));
            }
        }
        catch (Exception ex)
        {
            findings.Add(new Finding("DOC004", FindingSeverity.Error,
                $"The RDL engine could not parse this file: {ex.Message}"));
        }
    }

    private static void CheckSharedDataSetReference(XDocument doc, List<Finding> findings)
    {
        // Verified: RdlEngine/Definition/DataSetDefn.cs has no "shareddatasetreference" case --
        // it falls into the generic unknown-element bucket, and because _Query then stays null,
        // the dataset additionally gets a severity-8 "Query element must be specified" error
        // (which DOC004's engine-relay will also surface). This check exists to name the actual
        // cause plainly, since the generic DOC004 message alone doesn't explain *why*.
        if (doc.Descendants().Any(e => e.Name.LocalName == "SharedDataSetReference"))
        {
            findings.Add(new Finding("DOC006", FindingSeverity.Error,
                "<SharedDataSetReference> is not supported -- shared datasets have no handling in Majorsilence Reporting. Because the reference is ignored, the DataSet is left with no query at all, which is a fatal error (severity 8: \"Query element must be specified in a DataSet\"). Inline the query directly in the DataSet instead."));
        }
    }

    private static void CheckDataProviders(XDocument doc, List<Finding> findings)
    {
        string[]? known = RdlEngineConfig.GetProviders();
        if (known == null || known.Length == 0) return; // no config loaded; nothing to validate against

        foreach (var element in doc.Descendants().Where(e => e.Name.LocalName == "DataProvider"))
        {
            string? value = element.Value;
            if (string.IsNullOrWhiteSpace(value)) continue;
            if (known.Contains(value, StringComparer.OrdinalIgnoreCase)) continue;

            string? nearest = known
                .OrderBy(p => LevenshteinDistance(p.ToLowerInvariant(), value.ToLowerInvariant()))
                .FirstOrDefault();

            string suggestion = nearest != null ? $" Did you mean \"{nearest}\"?" : "";
            findings.Add(new Finding("DOC007", FindingSeverity.Warning,
                $"<DataProvider>{value}</DataProvider> is not a registered data provider.{suggestion} Registered providers: {string.Join(", ", known)}. This won't fail parsing -- only rendering, when the engine actually tries to connect."));
        }
    }

    private static void CheckCodeModulesAndClasses(XDocument doc, List<Finding> findings)
    {
        foreach (var codeModule in doc.Descendants().Where(e => e.Name.LocalName == "CodeModule"))
        {
            string assemblyRef = codeModule.Value.Trim();
            if (assemblyRef.Length == 0) continue;
            findings.Add(new Finding("DOC008", FindingSeverity.Info,
                $"<CodeModule>{assemblyRef}</CodeModule> requires that assembly to be present alongside the application at render time (Majorsilence Reporting loads it lazily via Assembly.LoadFrom, not at parse time -- a missing assembly only fails when the report actually uses it, not immediately)."));
        }

        bool hasClasses = doc.Descendants().Any(e => e.Name.LocalName == "Classes");
        bool hasCodeModules = doc.Descendants().Any(e => e.Name.LocalName == "CodeModules");
        if (hasClasses && !hasCodeModules)
        {
            findings.Add(new Finding("DOC008", FindingSeverity.Info,
                "Report declares <Classes> without <CodeModules>. The class names in <Classes> must resolve against either a loaded CodeModule assembly or a type pre-registered via RdlEngineConfig.RegisterType/RegisterInstanceFactory (see the Native AOT and Trimming Support docs) -- otherwise instance creation fails at render time."));
        }
    }

    private static void CheckExternalResources(XDocument doc, string folder, List<Finding> findings)
    {
        foreach (var image in doc.Descendants().Where(e => e.Name.LocalName == "Image"))
        {
            var source = image.Elements().FirstOrDefault(e => e.Name.LocalName == "Source");
            var value = image.Elements().FirstOrDefault(e => e.Name.LocalName == "Value");
            if (source?.Value != "External" || value == null) continue;

            string path = value.Value.Trim();
            // Only literal paths can be checked statically; an expression (e.g. a field-driven
            // path) is only resolvable at render time against real data.
            if (path.Length == 0 || path.StartsWith('=')) continue;

            string resolved = Path.IsPathRooted(path) ? path : Path.Combine(folder, path);
            if (!File.Exists(resolved))
            {
                findings.Add(new Finding("DOC009", FindingSeverity.Warning,
                    $"External image \"{path}\" was not found at {resolved}. This will fail at render time, not at parse time."));
            }
        }

        foreach (var subreport in doc.Descendants().Where(e => e.Name.LocalName == "Subreport"))
        {
            var reportName = subreport.Elements().FirstOrDefault(e => e.Name.LocalName == "ReportName");
            string? name = reportName?.Value?.Trim();
            if (string.IsNullOrEmpty(name)) continue;

            string resolved = Path.IsPathRooted(name) ? name : Path.Combine(folder, name);
            bool exists = File.Exists(resolved) || File.Exists(resolved + ".rdl");
            if (!exists)
            {
                findings.Add(new Finding("DOC009", FindingSeverity.Warning,
                    $"Subreport \"{name}\" was not found at {resolved} (also tried with a .rdl extension). This will fail at render time, not at parse time."));
            }
        }
    }

    private static void CheckRdlcQuirks(XDocument doc, string filePath, List<Finding> findings)
    {
        bool isRdlc = Path.GetExtension(filePath).Equals(".rdlc", StringComparison.OrdinalIgnoreCase);
        if (!isRdlc) return;

        foreach (var dataSet in doc.Descendants().Where(e => e.Name.LocalName == "DataSet"))
        {
            bool hasQuery = dataSet.Elements().Any(e => e.Name.LocalName == "Query");
            if (!hasQuery)
            {
                string dsName = dataSet.Attribute("Name")?.Value ?? "(unnamed)";
                findings.Add(new Finding("DOC010", FindingSeverity.Info,
                    $".rdlc DataSet \"{dsName}\" has no inline <Query> -- typical for Visual Studio ReportViewer projects, which supply data at design time via a DataSource control instead. Majorsilence Reporting needs data supplied programmatically: call report.RunGetData(null) after binding fields with your own data via the DataSet API, or add an inline <Query> if the data actually comes from a live connection."));
            }
        }
    }

    // Simple edit distance for "did you mean" suggestions -- fine for the short provider-name
    // strings this is used against; not intended for general text.
    private static int LevenshteinDistance(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) d[0, j] = j;
        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }
        return d[a.Length, b.Length];
    }
}
