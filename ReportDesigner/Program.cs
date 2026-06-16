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
                Application.DoEvents();
                Application.Run(new RdlDesigner(ipcChannelPortName, true));
                return;
            }

            // Process already running.   Notify other process that is might need to open another file
            string[] args = Environment.GetCommandLineArgs();

        }
    }
}