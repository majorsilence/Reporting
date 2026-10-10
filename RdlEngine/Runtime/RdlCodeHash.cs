using System;
using System.Security.Cryptography;
using System.Text;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// Stable identity of a report's &lt;Code&gt; text. The build-time generator (RdlCodeGen) and the
    /// engine both compute it, so a precompiled code block is matched to the report that owns it.
    /// This file is compiled into both assemblies; keep it dependency-free.
    /// </summary>
    public static class RdlCodeHash
    {
        /// <summary>Hash of the &lt;Code&gt; text, ignoring line-ending style and surrounding whitespace.</summary>
        public static string Compute(string source)
        {
            string normalized = (source ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
            return Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
        }
    }
}
