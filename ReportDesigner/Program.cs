using Majorsilence.Forms;
using Majorsilence.Reporting.RdlDesign;
using System.Globalization;

namespace ReportDesigner
{
    public class Program
    {

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string version = (typeof(Program).Assembly.GetName().Version?.ToString() ?? "0").Replace(".", "");

            string ipcChannelPortName = string.Format("RdlProject{0}", version);
            // Determine if an instance is already running?
            bool firstInstance;
            string mName = string.Format("Local\\RdlDesigner{0}", version);
            //   can't use Assembly in this context
            System.Threading.Mutex mutex = new System.Threading.Mutex(false, mName, out firstInstance);

            if (firstInstance)
            {// just start up the designer when we're first in line
                var thread = System.Threading.Thread.CurrentThread;

                try
                {
                    if (Majorsilence.Reporting.RdlDesign.DialogToolOptions.DesktopConfiguration.Language != null)
                    {
                        thread.CurrentCulture = new CultureInfo(DialogToolOptions.DesktopConfiguration.Language);
                    }
                    else
                    {
                        thread.CurrentCulture = new CultureInfo(thread.CurrentCulture.Name);
                    }
                }
                catch
                {
                    thread.CurrentCulture = new CultureInfo(thread.CurrentCulture.Name);
                }

                if (thread.CurrentCulture.Equals(CultureInfo.InvariantCulture))
                {
                    thread.CurrentCulture = new CultureInfo("en-US");
                }
                // for working in non-default cultures
                thread.CurrentCulture.NumberFormat.NumberDecimalSeparator = ".";
                thread.CurrentUICulture = thread.CurrentCulture;

                Application.EnableVisualStyles();
                // WinForms on .NET draws every control without a font of its own in the system message-box
                // font (Segoe UI 9pt on Windows); Majorsilence.Forms defaults to the .NET Framework's
                // Microsoft Sans Serif 8.25pt. Matching the former keeps this designer the size the
                // System.Windows.Forms one is -- its legacy dialogs scale up from the 8.25pt they were
                // laid out against (AutoScaleBaseSize), as they do there.
                Application.SetDefaultFont(SystemFonts.MessageBoxFont);
                Application.DoEvents();
                Application.Run(new RdlDesigner(ipcChannelPortName, true));
                return;
            }

            // Process already running.   Notify other process that is might need to open another file
            string[] args = Environment.GetCommandLineArgs();

        }
    }
}
