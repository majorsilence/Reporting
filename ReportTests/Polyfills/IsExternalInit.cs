// Polyfill for the IsExternalInit type required by `init` accessors and
// `record` types when compiling for .NET Framework (net48). It ships in the
// BCL for net5.0+ but not for net48.
#if NETFRAMEWORK
namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}
#endif
