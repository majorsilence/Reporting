using System.Reflection;
using System.Runtime.Loader;
using Majorsilence.Reporting.Rdl;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;

namespace Majorsilence.Reporting.RoslynCode;

/// <summary>
/// Compiles a report's &lt;Code&gt; element with Roslyn's VB compiler, so the element works on
/// .NET 8 and later, where the engine's own <c>VBCodeProvider</c> path is .NET Framework only.
/// Register it once at startup:
/// <code>RoslynCodeCompiler.Register();</code>
/// Each parse of a report compiles its block once and loads the result into the default
/// load context, which is what the .NET Framework path did as well: a process that parses
/// many distinct reports holds one small assembly per parse.
/// </summary>
public sealed class RoslynCodeCompiler : IRdlCodeCompiler
{
    /// <summary>Registers this compiler with the engine (<see cref="RdlEngineConfig.RegisterCodeCompiler"/>).</summary>
    public static void Register() => RdlEngineConfig.RegisterCodeCompiler(new RoslynCodeCompiler());

    public Assembly? Compile(string source, IReadOnlyList<Assembly> references, IList<string> errors)
    {
        var tree = VisualBasicSyntaxTree.ParseText(source, new VisualBasicParseOptions(LanguageVersion.Latest));
        var compilation = VisualBasicCompilation.Create(
            "RdlCode_" + Guid.NewGuid().ToString("N"),
            [tree],
            MetadataReferences(references),
            // The settings VBCodeProvider compiled with by default: late binding allowed, every
            // variable declared.
            new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                optionStrict: OptionStrict.Off,
                optionInfer: true,
                optionExplicit: true,
                optimizationLevel: OptimizationLevel.Release));

        using var image = new MemoryStream();
        var emitted = compilation.Emit(image);
        if (!emitted.Success)
        {
            foreach (var d in emitted.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                errors.Add($"{d.Location.GetLineSpan().StartLinePosition.Line + 1}: {d.Id} {d.GetMessage()}");
            if (errors.Count == 0) errors.Add("0: compilation failed");
            return null;
        }
        image.Position = 0;
        return AssemblyLoadContext.Default.LoadFromStream(image);
    }

    // Everything the running application can see (the shared framework, and the VB runtime,
    // Microsoft.VisualBasic.Core, among it), plus the engine and the report's CodeModules.
    private static IEnumerable<MetadataReference> MetadataReferences(IReadOnlyList<Assembly> references)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string tpa)
            foreach (var p in tpa.Split(Path.PathSeparator))
                if (p.Length > 0) paths.Add(p);
        foreach (var r in references)
            if (!string.IsNullOrEmpty(r.Location)) paths.Add(r.Location);
        return paths.Where(File.Exists).Select(p => MetadataReference.CreateFromFile(p));
    }
}
