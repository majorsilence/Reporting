using System.Xml;

namespace Majorsilence.Reporting.RdlCodeGen;

/// <summary>One distinct &lt;Code&gt; block found in the input reports.</summary>
internal sealed class CodeBlock
{
    public required string Hash { get; init; }
    public required string Source { get; init; }
    public List<string> Reports { get; } = new();

    /// <summary>Reads the &lt;Code&gt; element of a report, ignoring the RDL namespace version.</summary>
    public static string? ReadSource(string reportPath)
    {
        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.Load(reportPath);
        XmlElement? root = doc.DocumentElement;
        if (root == null || root.LocalName != "Report")
            return null;
        foreach (XmlNode child in root.ChildNodes)
        {
            if (child is XmlElement { LocalName: "Code" } e && !string.IsNullOrWhiteSpace(e.InnerText))
                return e.InnerText;
        }
        return null;
    }
}
