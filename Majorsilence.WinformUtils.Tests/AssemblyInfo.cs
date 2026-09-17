using System.Runtime.CompilerServices;
using Majorsilence.Forms.Backends;
using Majorsilence.Forms.Headless;

namespace Majorsilence.WinformUtils.Forms.Tests;

internal static class TestBackend
{
    // Run on the dependency-free Headless backend: no windowing toolkit, no UI-thread dispatcher
    // affinity. Runs once before any test in this assembly (same pattern Majorsilence.Forms's own
    // test suite uses).
    [ModuleInitializer]
    internal static void Initialize() => Platform.Backend = new HeadlessPlatformBackend();
}
