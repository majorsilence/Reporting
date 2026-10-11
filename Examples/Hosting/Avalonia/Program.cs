using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Hosting.Shared;
using Majorsilence.Forms;

namespace HostingAvalonia;

// Embeds the Majorsilence.Forms report viewer in an Avalonia window.
// ToAvaloniaControl() returns an Avalonia.Controls.Control (a MajorsilenceFormsPresenter).
internal static class Program
{
    internal static string[] Args = [];

    [STAThread]
    public static int Main(string[] args)
    {
        Args = args;
        ReportContent.InitEngine();
        return AppBuilder.Configure<HostApp>().UsePlatformDetect().UseSkia().StartWithClassicDesktopLifetime(args);
    }
}

internal sealed class HostApp : Avalonia.Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var content = new ReportContent();
            var window = new Window
            {
                Title = "Majorsilence Reporting in Avalonia",
                Width = 1024,
                Height = 700,
                Content = content.Root.ToAvaloniaControl(),
            };
            window.Opened += (_, _) => content.Open(ReportContent.DefaultReportPath(Program.Args));
            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
