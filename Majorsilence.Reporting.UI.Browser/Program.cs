using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Browser;
using Majorsilence.Reporting.UI.Browser;

[assembly: SupportedOSPlatform("browser")]

internal static class Program
{
    private static Task Main() => BuildAvaloniaApp().WithInterFont().StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>();
}
