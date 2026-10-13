using System.Collections.Generic;
using System.Reflection;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// Compiles a report's &lt;Code&gt; element at runtime. The engine's own compiler,
    /// <c>VBCodeProvider</c>, exists only on .NET Framework; on .NET 5 and later a report with
    /// a &lt;Code&gt; element logs an error unless a compiler is registered with
    /// <see cref="RdlEngineConfig.RegisterCodeCompiler"/>. The Majorsilence.Reporting.RoslynCode
    /// package supplies one built on Roslyn's VB compiler.
    /// </summary>
    public interface IRdlCodeCompiler
    {
        /// <summary>
        /// Compiles <paramref name="source"/>, a complete VB source file the engine generates
        /// around the &lt;Code&gt; element's text, against <paramref name="references"/>: the
        /// engine itself and any CodeModules the report names. Returns the loaded assembly, or
        /// null with at least one entry in <paramref name="errors"/>, each one line, prefixed
        /// with its 1-based line number in <paramref name="source"/> as "line: message".
        /// </summary>
        Assembly? Compile(string source, IReadOnlyList<Assembly> references, IList<string> errors);
    }
}
