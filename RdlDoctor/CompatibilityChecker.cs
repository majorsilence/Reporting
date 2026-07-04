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
    /// Checks a single RDL/RDLC file. Returns findings from the structural scan always; also
    /// attempts a real parse for the unknown-element sweep (DOC004), appending a parse-failure
    /// finding instead if that throws.
    /// </summary>
    /// <summary>
    /// Checks a file. Throws if the file isn't well-formed XML at all -- callers should treat
    /// that as a harder failure (exit code 2) than an ordinary compatibility finding (exit 1).
    /// </summary>
    public static async Task<IReadOnlyList<Finding>> CheckAsync(string filePath)
    {
        string xml = await File.ReadAllTextAsync(filePath);
        var findings = new List<Finding>();

        // Let a malformed-XML exception propagate -- the caller distinguishes "not even XML"
        // (exit 2) from "valid XML with compatibility issues" (exit 1, an ordinary Finding).
        XDocument doc = XDocument.Parse(xml, LoadOptions.SetLineInfo);

        CheckNamespace(doc, findings);       // DOC002
        CheckTablix(doc, findings);          // DOC001
        CheckUnsupportedItems(doc, findings); // DOC003
        CheckExpressionFunctions(doc, findings); // DOC005
        await CheckUnknownElementsAsync(xml, filePath, findings); // DOC004

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
}
