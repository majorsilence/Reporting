using System;
using Hosting.Shared;
using Majorsilence.Forms.WinForms;

namespace HostingWinForms;

// Embeds the Majorsilence.Forms report viewer in a classic System.Windows.Forms form.
// ToWinFormsControl() returns a real System.Windows.Forms.Control, so it drops into any WinForms container.
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ReportContent.InitEngine();
        System.Windows.Forms.Application.EnableVisualStyles();

        var content = new ReportContent();
        var host = content.Root.ToWinFormsControl();
        host.Dock = System.Windows.Forms.DockStyle.Fill;

        var form = new System.Windows.Forms.Form { Text = "Majorsilence Reporting in WinForms", Width = 1024, Height = 700 };
        form.Controls.Add(host);
        form.Shown += (_, _) => content.Open(ReportContent.DefaultReportPath(args));

        System.Windows.Forms.Application.Run(form);
    }
}
