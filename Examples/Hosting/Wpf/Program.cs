using System;
using Hosting.Shared;
using Majorsilence.Forms.Wpf;

namespace HostingWpf;

// Embeds the Majorsilence.Forms report viewer in a WPF window.
// ToWpfElement() returns a System.Windows.FrameworkElement, so it can be a Window's Content or a child of any panel.
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ReportContent.InitEngine();

        var content = new ReportContent();
        var window = new System.Windows.Window
        {
            Title = "Majorsilence Reporting in WPF",
            Width = 1024,
            Height = 700,
            Content = content.Root.ToWpfElement(),
        };
        window.Loaded += (_, _) => content.Open(ReportContent.DefaultReportPath(args));

        new System.Windows.Application().Run(window);
    }
}
