using Hosting.Shared;
using Majorsilence.Forms.Gtk4;

namespace HostingGtk4;

// Embeds the Majorsilence.Forms report viewer in a GTK 4 (gir.core) window.
// ToGtkWidget() returns a Gtk.Widget. The GTK host owns the Gtk.Application and its main loop.
public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        ReportContent.InitEngine();

        var app = Gtk.Application.New("com.majorsilence.reporting.hostinggtk4", Gio.ApplicationFlags.DefaultFlags);
        app.OnActivate += (sender, _) =>
        {
            var content = new ReportContent();
            var widget = content.Root.ToGtkWidget();
            widget.SetHexpand(true);
            widget.SetVexpand(true);

            var window = Gtk.ApplicationWindow.New((Gtk.Application)sender);
            window.SetTitle("Majorsilence Reporting in GTK 4");
            window.SetDefaultSize(1024, 700);
            window.SetChild(widget);
            window.Present();

            content.Open(ReportContent.DefaultReportPath(args));
        };
        return app.RunWithSynchronizationContext(args);
    }
}
