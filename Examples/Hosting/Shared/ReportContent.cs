using System;
using System.IO;
using Majorsilence.Forms;
using Majorsilence.Reporting.Rdl;
using Viewer = Majorsilence.Reporting.RdlViewer.RdlViewer;
using Toolstrip = Majorsilence.Reporting.RdlViewer.ViewerToolstrip;

namespace Hosting.Shared;

/// <summary>
/// The Majorsilence.Forms content every hosting example embeds: the report viewer and its toolstrip.
/// The viewer is an ordinary Majorsilence.Forms control, so each host only has to turn <see cref="Root"/>
/// into its own native element (ToWinFormsControl, ToWpfElement, ToAvaloniaControl, ...).
/// </summary>
public sealed class ReportContent
{
    public ReportContent()
    {
        Viewer = new Viewer { Dock = DockStyle.Fill };
        Root = new Panel { Dock = DockStyle.Fill };
        // Fill goes in first so the toolstrip docks above it.
        Root.Controls.Add(Viewer);
        Root.Controls.Add(new Toolstrip(Viewer) { Dock = DockStyle.Top });
    }

    /// <summary>The control to hand to the host's To...() extension method.</summary>
    public Control Root { get; }

    /// <summary>Keep this: the host element is only a container, the viewer API is called on this.</summary>
    public Viewer Viewer { get; }

    /// <summary>Call once at startup, before the first report is shown.</summary>
    public static void InitEngine()
    {
        RdlEngineConfig.RdlEngineConfigInit();
        // Products.rdl uses a relative SQLite path; resolve it from the app folder.
        Environment.CurrentDirectory = AppContext.BaseDirectory;
    }

    public static string DefaultReportPath(string[] args)
        => args.Length > 0 ? Path.GetFullPath(args[0]) : Path.Combine(AppContext.BaseDirectory, "Products.rdl");

    /// <summary>Loads a report into the viewer. Call after the host window is up.</summary>
    public async void Open(string reportPath)
    {
        try
        {
            await Viewer.SetSourceFile(new Uri(reportPath));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Could not open {reportPath}: {ex.Message}");
        }
    }
}
