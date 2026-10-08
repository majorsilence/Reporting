using System.Runtime.CompilerServices;
using Majorsilence.Forms.Backends;
using Majorsilence.Forms.Headless;

namespace Majorsilence.Reporting.RdlReader.Tests;

internal static class TestBackend
{
    // Same pattern as D1/D2's test projects: run on the dependency-free Headless backend so
    // these tests need no windowing toolkit or UI-thread affinity.
    [ModuleInitializer]
    internal static void Initialize() => Platform.Backend = new HeadlessPlatformBackend();
}
