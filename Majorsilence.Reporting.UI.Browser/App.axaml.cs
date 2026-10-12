using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Majorsilence.Reporting.Rdl;
using Majorsilence.Reporting.UI.RdlAvalonia.Viewer;

namespace Majorsilence.Reporting.UI.Browser;

public class App : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is ISingleViewApplicationLifetime single)
        {
            var viewer = new AvaloniaReportViewer();
            single.MainView = viewer;
            Avalonia.Threading.Dispatcher.UIThread.Post(async () => await ShowSampleAsync(viewer));
        }
        base.OnFrameworkInitializationCompleted();
    }

    // Renders SampleReport.Rdl with rows pushed in from code. Nothing here opens a database or a file.
    private static async Task ShowSampleAsync(AvaloniaReportViewer viewer)
    {
        try
        {
            var parser = new RDLParser(SampleReport.Rdl) { SkipDatabaseSchemaValidation = true };
            var report = await parser.Parse();
            report.DataSets["Data"].SetData(SampleReport.Rows);
            await viewer.SetReportAsync(report);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("VIEWER-SAMPLE-FAILED: " + e);
        }
    }
}
