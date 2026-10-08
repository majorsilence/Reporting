using System;
using System.IO;

namespace Majorsilence.Reporting.RdlAsp
{
    /// <summary>
    /// Maps a requested report file name to a path while keeping it inside an allowed folder.
    /// The requested name comes from the request, so it must never be able to leave the folder.
    /// </summary>
    internal static class ReportPathResolver
    {
        /// <summary>
        /// Returns the full path of <paramref name="file"/> under <paramref name="root"/>,
        /// or null when the name is empty, rooted, contains wildcards, or resolves outside the root.
        /// </summary>
        public static string ResolveWithin(string root, string file)
        {
            if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(file)
                || file.IndexOf('\0') >= 0 || file.IndexOfAny(['*', '?']) >= 0
                || Path.IsPathRooted(file))
            {
                return null;
            }

            string rootFull = Path.GetFullPath(root);
            char last = rootFull[rootFull.Length - 1];
            if (last != Path.DirectorySeparatorChar && last != Path.AltDirectorySeparatorChar)
            {
                rootFull += Path.DirectorySeparatorChar;
            }

            string full = Path.GetFullPath(Path.Combine(rootFull, file));
            var comparison = Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return full.StartsWith(rootFull, comparison) ? full : null;
        }

        /// <summary>True when <paramref name="file"/> is a bare file name with no directory part or wildcards.</summary>
        public static bool IsBareFileName(string file)
        {
            return !string.IsNullOrWhiteSpace(file)
                && file.IndexOfAny(['*', '?', '\0', '/', '\\']) < 0
                && file != "." && file != "..";
        }
    }
}
