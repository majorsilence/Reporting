using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// AOT-safe replacement for the RDL &lt;Code&gt; element.
    /// Register named method delegates at startup instead of embedding inline VB source in the report.
    /// </summary>
    /// <remarks>
    /// Usage:
    /// <code>
    /// RdlEngineConfig.RegisterCodeProvider(report =>
    ///     new RdlCodeFunctions()
    ///         .Add("FormatCurrency", args => string.Format("{0:C}", args[0]))
    ///         .Add("IsEligible",     args => (int)args[0] >= 18)
    /// );
    /// </code>
    /// </remarks>
    public sealed class RdlCodeFunctions
    {
        private readonly Dictionary<string, Func<object?[], Task<object?>>> _methods =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Exposes report globals, parameters, and user — mirrors the VB-generated <c>Report</c> property.</summary>
        public CodeReport? Report { get; internal set; }

        /// <summary>Register a synchronous method.</summary>
        public RdlCodeFunctions Add(string name, Func<object?[], object?> impl)
        {
            _methods[name] = args => Task.FromResult(impl(args));
            return this;
        }

        /// <summary>Register an asynchronous method.</summary>
        public RdlCodeFunctions Add(string name, Func<object?[], Task<object?>> impl)
        {
            _methods[name] = impl;
            return this;
        }

        internal bool TryInvoke(string name, object?[] args, out Task<object?> result)
        {
            if (_methods.TryGetValue(name, out var fn))
            {
                result = fn(args);
                return true;
            }
            result = Task.FromResult<object?>(null);
            return false;
        }
    }
}
