using Majorsilence.Reporting.Rdl;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Majorsilence.Reporting.RdlCodeGen;

/// <summary>
/// Turns report &lt;Code&gt; blocks into a VB source file that is compiled with the application, so
/// the VB compiler runs at build time and the engine never has to compile VB at runtime.
/// </summary>
internal sealed class Generator
{
    readonly List<string> _messages = new();

    /// <summary>Diagnostics in MSBuild canonical format ("origin: error CODE: text").</summary>
    public IReadOnlyList<string> Messages => _messages;
    public bool HasErrors { get; private set; }

    void Error(string origin, string code, string text)
    {
        HasErrors = true;
        _messages.Add($"{origin}: error {code}: {text}");
    }

    void Warn(string origin, string code, string text) => _messages.Add($"{origin}: warning {code}: {text}");

    /// <summary>Collects the distinct code blocks across the reports.</summary>
    public List<CodeBlock> Collect(IEnumerable<string> reports)
    {
        var byHash = new Dictionary<string, CodeBlock>();
        foreach (string report in reports)
        {
            string? source;
            try { source = CodeBlock.ReadSource(report); }
            catch (Exception ex)
            {
                Error(report, "RDLCODE001", "cannot read report: " + ex.Message);
                continue;
            }
            if (source == null)
                continue;
            string hash = RdlCodeHash.Compute(source);
            if (!byHash.TryGetValue(hash, out var block))
                byHash[hash] = block = new CodeBlock { Hash = hash, Source = source };
            block.Reports.Add(report);
        }
        return byHash.Values.OrderBy(b => b.Hash, StringComparer.Ordinal).ToList();
    }

    static string ClassName(CodeBlock b) => "MyClass_" + b.Hash;

    // Same shape as the wrapper the engine builds at runtime (Code.GetAssembly), so a report's
    // code behaves identically whether it is compiled at runtime or ahead of time.
    static string ClassHeader(string cls) => string.Join("\r\n",
        "Public Class " + cls,
        "Private Shared _report As CodeReport",
        "Sub New()",
        "End Sub",
        "Sub New(byVal def As Report)",
        cls + "._report = New CodeReport(def)",
        "End Sub",
        "Public Shared ReadOnly Property Report As CodeReport",
        "Get",
        "Return " + cls + "._report",
        "End Get",
        "End Property") + "\r\n";

    sealed record Param(string Type);
    sealed record Method(string Name, bool IsFunction, List<Param> Params);

    List<Method> FindMethods(CodeBlock block, string cls)
    {
        string text = "Imports System\r\nImports Microsoft.VisualBasic\r\nImports System.Convert\r\nImports System.Math\r\n" +
                      "Imports Majorsilence.Reporting.Rdl\r\nNamespace Majorsilence.Reporting.vbgen\r\n" +
                      ClassHeader(cls) + block.Source.Replace("\r\n", "\n").Replace("\n", "\r\n") + "\r\nEnd Class\r\nEnd Namespace\r\n";
        var tree = VisualBasicSyntaxTree.ParseText(text);
        string origin = block.Reports[0];
        foreach (var d in tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error))
            Error(origin, "RDLCODE002", $"<Code> syntax: {d.GetMessage()} (generated line {d.Location.GetLineSpan().StartLinePosition.Line + 1})");

        var methods = new List<Method>();
        var cb = tree.GetRoot().DescendantNodes().OfType<ClassBlockSyntax>().FirstOrDefault(c => c.ClassStatement.Identifier.Text == cls);
        if (cb == null)
            return methods;
        foreach (var m in cb.Members.OfType<MethodBlockSyntax>())
        {
            var st = m.SubOrFunctionStatement;
            string name = st.Identifier.Text;
            if (name == "New")
                continue;
            var mods = st.Modifiers.Select(x => x.Text.ToLowerInvariant()).ToList();
            if (mods.Contains("private") || mods.Contains("friend") || mods.Contains("protected"))
                continue;
            var ps = new List<Param>();
            bool callable = true;
            foreach (var p in st.ParameterList?.Parameters ?? default)
            {
                var pm = p.Modifiers.Select(x => x.Text.ToLowerInvariant()).ToList();
                if (pm.Contains("byref") || pm.Contains("optional") || pm.Contains("paramarray"))
                {
                    Warn(origin, "RDLCODE003", $"<Code> method '{name}' uses ByRef/Optional/ParamArray parameters and cannot be called from report expressions under AOT; it was skipped.");
                    callable = false;
                    break;
                }
                ps.Add(new Param(p.AsClause?.Type.ToString().Trim() ?? "Object"));
            }
            if (callable)
                methods.Add(new Method(name, st.IsKind(SyntaxKind.FunctionStatement), ps));
        }
        return methods;
    }

    public string Generate(List<CodeBlock> blocks)
    {
        var sb = new StringBuilder();
        sb.Append("' <auto-generated>\r\n' Generated by RdlCodeGen from the <Code> elements of the reports listed in RdlReports.\r\n' Do not edit; changes are overwritten on every build.\r\n' </auto-generated>\r\n");
        sb.Append("Option Strict Off\r\nOption Explicit On\r\n");
        sb.Append("Imports System\r\nImports Microsoft.VisualBasic\r\nImports System.Convert\r\nImports System.Math\r\nImports Majorsilence.Reporting.Rdl\r\n\r\n");
        sb.Append("Namespace Majorsilence.Reporting.vbgen\r\n");
        var registry = new StringBuilder();
        foreach (var block in blocks)
        {
            string cls = ClassName(block);
            var methods = FindMethods(block, cls);
            sb.Append("' Reports: ").Append(string.Join(", ", block.Reports.Select(Path.GetFileName))).Append("\r\n");
            sb.Append(ClassHeader(cls)).Append(block.Source.Replace("\r\n", "\n").Replace("\n", "\r\n")).Append("\r\nEnd Class\r\n\r\n");

            registry.Append("    Private Function Create_").Append(block.Hash).Append("(rpt As Report) As RdlCodeFunctions\r\n");
            registry.Append("        Dim inst As New Majorsilence.Reporting.vbgen.").Append(cls).Append("(rpt)\r\n");
            registry.Append("        Dim f As New RdlCodeFunctions()\r\n");
            foreach (var g in methods.GroupBy(m => m.Name, StringComparer.OrdinalIgnoreCase))
            {
                registry.Append("        f.Add(\"").Append(g.Key).Append("\", Function(a As Object()) As Object\r\n");
                registry.Append("            Select Case a.Length\r\n");
                foreach (var m in g.GroupBy(x => x.Params.Count).Select(x => x.First()))
                {
                    string args = string.Join(", ", m.Params.Select((p, i) =>
                        p.Type.Equals("Object", StringComparison.OrdinalIgnoreCase) ? $"a({i})" : $"CType(a({i}), {p.Type})"));
                    registry.Append("                Case ").Append(m.Params.Count).Append("\r\n");
                    if (m.IsFunction)
                        registry.Append("                    Return inst.").Append(m.Name).Append('(').Append(args).Append(")\r\n");
                    else
                        registry.Append("                    inst.").Append(m.Name).Append('(').Append(args).Append(")\r\n                    Return Nothing\r\n");
                }
                registry.Append("            End Select\r\n");
                registry.Append("            Throw New ArgumentException(\"No overload of ").Append(g.Key).Append(" takes \" & a.Length & \" argument(s).\")\r\n");
                registry.Append("        End Function)\r\n");
            }
            registry.Append("        Return f\r\n    End Function\r\n\r\n");
        }
        sb.Append("End Namespace\r\n\r\n");
        sb.Append("''' <summary>Registers the precompiled report code blocks with the engine. Call once at startup, before parsing reports.</summary>\r\n");
        sb.Append("Public Module RdlPrecompiledCode\r\n");
        sb.Append("    Public Sub Register()\r\n");
        foreach (var b in blocks)
            sb.Append("        RdlEngineConfig.RegisterPrecompiledCode(\"").Append(b.Hash).Append("\", AddressOf Create_").Append(b.Hash).Append(")\r\n");
        sb.Append("    End Sub\r\n\r\n");
        sb.Append(registry);
        sb.Append("End Module\r\n");
        return sb.ToString();
    }
}
