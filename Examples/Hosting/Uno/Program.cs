using System;
using Hosting.Shared;
using Majorsilence.Forms.Uno;
using Microsoft.UI.Xaml;
using Uno.UI.Hosting;

namespace HostingUno;

// Embeds the Majorsilence.Forms report viewer in an Uno Platform (Skia desktop) window.
// ToUnoControl() returns a Microsoft.UI.Xaml.FrameworkElement.
public static class Program
{
    internal static string[] Args = [];

    [STAThread]
    public static void Main(string[] args)
    {
        Args = args;
        ReportContent.InitEngine();
        UnoPlatformHostBuilder.Create()
            .App(() => new HostApp())
            .UseX11()
            .UseWin32()
            .UseMacOS()
            .Build()
            .Run();
    }
}

public sealed class HostApp : Application
{
    private Window? _window;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var content = new ReportContent();
        _window = new Window { Title = "Majorsilence Reporting in Uno", Content = content.Root.ToUnoControl() };
        _window.Activate();
        content.Open(ReportContent.DefaultReportPath(Program.Args));
    }
}
