using Majorsilence.Forms;
using Majorsilence.Reporting.RdlDesign;
using Majorsilence.Reporting.RdlViewer;
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

            // ReportingBranding.InstanceName keeps a white-labelled build (one that copies this Main) and the
            // stock designer from finding each other; the stock names are unchanged.
            string instance = ReportingBranding.Current.InstanceName;
            string ipcChannelPortName = instance == "RdlDesigner"
                ? string.Format("RdlProject{0}", version)
                : string.Format("{0}Ipc{1}", instance, version);
            // Determine if an instance is already running?
            bool firstInstance;
            string mName = string.Format("Local\\{0}{1}", instance, version);
            //   can't use Assembly in this context
            System.Threading.Mutex mutex = new System.Threading.Mutex(false, mName, out firstInstance);

            if (firstInstance)
            {// just start up the designer when we're first in line
                var thread = System.Threading.Thread.CurrentThread;

                DesktopConfig config = null;
                try
                {
                    config = DialogToolOptions.DesktopConfiguration;    // reads config.xml; throws when there is none
                }
                catch
                {
                }

                try
                {
                    if (config?.Language != null)
                    {
                        thread.CurrentCulture = new CultureInfo(config.Language);
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

                // before the first window exists, so nothing paints in the default theme first
                var theme = DesignerTheme.Apply(config?.Theme, config?.ThemeCss);

                var designer = new RdlDesigner(ipcChannelPortName, true);
                if (theme.FellBack)
                {   // a theme that was chosen but can't be used: say so once the window is up, don't block startup
                    designer.Shown += (s, e) => MessageBox.Show(designer, string.Join(Environment.NewLine, theme.Messages), designer.Text,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                Application.Run(designer);
                return;
            }

            // Process already running.   Notify other process that is might need to open another file
            string[] args = Environment.GetCommandLineArgs();

        }
    }
}
