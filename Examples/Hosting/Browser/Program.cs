using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Browser;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Majorsilence.Forms;
using Majorsilence.Reporting.Rdl;
using Viewer = Majorsilence.Reporting.RdlViewer.RdlViewer;
using Toolstrip = Majorsilence.Reporting.RdlViewer.ViewerToolstrip;

[assembly: SupportedOSPlatform("browser")]

namespace HostingBrowser;

internal static class Program
{
    private static async Task Main()
    {
        try
        {
            RdlEngineConfig.RdlEngineConfigInit();
            await AppBuilder.Configure<HostApp>().WithInterFont().StartBrowserAppAsync("out");
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("HOSTING-STARTUP-FAILED: " + e);
        }
    }
}

internal sealed class HostApp : Avalonia.Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is ISingleViewApplicationLifetime single)
        {
            // The same content the desktop hosting examples embed: the viewer and its toolstrip.
            var viewer = new Viewer { Dock = DockStyle.Fill };
            var root = new Majorsilence.Forms.Panel { Dock = DockStyle.Fill };
            root.Controls.Add(viewer);                                        // Fill first so the toolstrip docks above it
            root.Controls.Add(new Toolstrip(viewer) { Dock = DockStyle.Top });
            single.MainView = root.ToAvaloniaControl();

            Avalonia.Threading.Dispatcher.UIThread.Post(async () => await ShowSampleAsync(viewer));
        }
        base.OnFrameworkInitializationCompleted();
    }

    private static async Task ShowSampleAsync(Viewer viewer)
    {
        try
        {
            Directory.CreateDirectory("/work");
            File.WriteAllText("/work/items.json", SampleReport.Json);
            await viewer.SetSourceRdl(SampleReport.Rdl("/work/items.json"));
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("VIEWER-SAMPLE-FAILED: " + e);
        }
    }
}
