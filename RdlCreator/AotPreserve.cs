using System.Runtime.CompilerServices;

namespace Majorsilence.Reporting.RdlCreator
{
#if AOT
    internal static class AotPreserve
    {
        [ModuleInitializer]
        internal static void PreserveProviderTypes()
        {
            // Touch types to keep them from being trimmed.
            // Requires you to reference the provider packages at compile time.
            _ = typeof(Page);
            _ = typeof(Report);
            _ = typeof(Document);
        }
    }
#endif
}