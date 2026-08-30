// SPDX-License-Identifier: Apache-2.0

using System.Xml.Linq;

namespace Majorsilence.Reporting.RdlDoctor;

/// <summary>
/// Conservative, best-effort fixes for RDL/RDLC compatibility issues. Deliberately does not
/// attempt anything structural (e.g. converting Tablix to Table/Matrix) -- only hygiene changes
/// that are safe because Majorsilence Reporting's parser doesn't care about them either way.
/// </summary>
public static class RdlFixer
{
    private const string Rdl2005Namespace = "http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition";

    public sealed record FixResult(string Xml, IReadOnlyList<string> ChangesMade);

    /// <param name="stripUnknown">
    /// Also remove elements the engine would silently ignore anyway (as identified by
    /// <paramref name="knownUnknownElementNames"/>, typically from a prior DOC004 check).
    /// Never removes &lt;Tablix&gt; even if requested -- that's a structural incompatibility,
    /// not something safe to just delete.
    /// </param>
    public static FixResult Fix(string xml, bool stripUnknown, IReadOnlySet<string>? knownUnknownElementNames = null)
    {
        var doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        var changes = new List<string>();

        StripDesignerNamespaceContent(doc, changes);
        NormalizeNamespaceIfSafe(doc, changes);

        if (stripUnknown && knownUnknownElementNames != null)
            StripKnownUnknownElements(doc, knownUnknownElementNames, changes);

        return new FixResult(doc.Declaration != null ? doc.Declaration + "\n" + doc.ToString() : doc.ToString(), changes);
    }

    // The "rd:" namespace (http://schemas.microsoft.com/SQLServer/reporting/reportdesigner) holds
    // Report Designer/SSDT design-time metadata (layout hints, IntelliSense caches) with no
    // runtime meaning at all -- safe to strip unconditionally.
    private static void StripDesignerNamespaceContent(XDocument doc, List<string> changes)
    {
        var designerAttrs = doc.Descendants()
            .SelectMany(e => e.Attributes())
            .Where(a => a.Name.NamespaceName == "http://schemas.microsoft.com/SQLServer/reporting/reportdesigner")
            .ToList();
        var designerElements = doc.Descendants()
            .Where(e => e.Name.NamespaceName == "http://schemas.microsoft.com/SQLServer/reporting/reportdesigner")
            .ToList();

        foreach (var attr in designerAttrs) attr.Remove();
        foreach (var el in designerElements) el.Remove();

        if (designerAttrs.Count > 0 || designerElements.Count > 0)
            changes.Add($"Removed {designerAttrs.Count} designer-namespace attribute(s) and {designerElements.Count} designer-namespace element(s) (rd: -- design-time only, no runtime effect).");
    }

    // Only normalize the namespace when nothing else in the file is a known-unsupported
    // construct -- if it is, the file isn't actually 2005-compatible yet regardless of what its
    // namespace says, and rewriting the namespace would be misleading.
    private static void NormalizeNamespaceIfSafe(XDocument doc, List<string> changes)
    {
        if (doc.Root == null) return;
        string current = doc.Root.Name.NamespaceName;
        if (current == Rdl2005Namespace) return;

        bool hasKnownUnsupported = doc.Descendants().Any(e =>
            e.Name.LocalName is "Tablix" or "Gauge" or "Indicator" or "Sparkline" or "Map" or "SharedDataSetReference");
        if (hasKnownUnsupported) return;

        // Remove the existing default-namespace declaration attribute (if any) before renaming
        // element Names below -- otherwise the stale "xmlns=<old>" attribute conflicts with the
        // new namespace at serialization time ("prefix '' cannot be redefined"). XLinq
        // regenerates the correct xmlns declaration automatically from the renamed elements.
        doc.Root.Attributes().FirstOrDefault(a => a.IsNamespaceDeclaration && a.Name.LocalName == "xmlns")?.Remove();

        RenameNamespaceRecursive(doc.Root, current, Rdl2005Namespace);
        changes.Add(current.Length == 0
            ? "Added the RDL 2005 namespace to the root <Report> element (had none)."
            : $"Normalized the RDL namespace from {current} to the 2005 namespace (Majorsilence Reporting ignores the declared version either way; this is cosmetic).");
    }

    private static void RenameNamespaceRecursive(XElement element, string fromNs, string toNs)
    {
        if (element.Name.NamespaceName == fromNs)
            element.Name = XName.Get(element.Name.LocalName, toNs);
        foreach (var child in element.Elements())
            RenameNamespaceRecursive(child, fromNs, toNs);
    }

    private static void StripKnownUnknownElements(XDocument doc, IReadOnlySet<string> unknownNames, List<string> changes)
    {
        int removed = 0;
        foreach (var element in doc.Descendants().ToList())
        {
            if (element.Name.LocalName == "Tablix") continue; // never -- structural, not just noise
            if (!unknownNames.Contains(element.Name.LocalName)) continue;

            element.Remove();
            removed++;
        }
        if (removed > 0)
            changes.Add($"Removed {removed} element(s) matching names the parser would silently ignore anyway ({string.Join(", ", unknownNames.Where(n => n != "Tablix"))}).");
    }
}
