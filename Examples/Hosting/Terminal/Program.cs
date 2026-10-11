using Hosting.Shared;
using Majorsilence.Forms;
using Majorsilence.Forms.Terminal;

namespace HostingTerminal;

// Runs the Majorsilence.Forms report viewer as the whole application, drawn in the terminal.
// There is no To...() call here: TerminalApplication.Use() installs the terminal backend and
// Application.Run() shows an ordinary Majorsilence.Forms Form. Ctrl+C exits.
// Use a truecolor terminal (Kitty or Sixel graphics are detected automatically).
internal static class Program
{
    private static void Main(string[] args)
    {
        ReportContent.InitEngine();
        TerminalApplication.Use(new TerminalOptions());

        var content = new ReportContent();
        var form = new Form { Text = "Majorsilence Reporting in the terminal" };
        form.Controls.Add(content.Root);
        form.Shown += (_, _) => content.Open(ReportContent.DefaultReportPath(args));

        Application.Run(form);
    }
}
