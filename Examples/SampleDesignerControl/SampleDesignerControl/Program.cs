using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Forms;

namespace SampleApp2_SetData
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Two designers in one window, each with its own theme (issue #448). UseTheme gives a designer
            // a ThemeScope, so it is themed independently of the rest of the host application; the left one
            // keeps the application theme. Pass a theme name (Default, Light, Dark, Classic, Aero) or the
            // path of a CSS theme file to use instead of Dark, e.g. ReportDesigner/Themes/graphite.css.
            string report = System.IO.Path.Combine(AppContext.BaseDirectory, "SampleApp2-TestReport.rdl");
            string theme = args.Length > 0 ? args[0] : "Dark";

            var left = new Majorsilence.Reporting.RdlDesign.RdlUserControl { Dock = DockStyle.Fill };
            var right = new Majorsilence.Reporting.RdlDesign.RdlUserControl { Dock = DockStyle.Fill };
            var result = theme.EndsWith(".css", StringComparison.OrdinalIgnoreCase)
                ? right.UseTheme(Majorsilence.Reporting.RdlDesign.DesignerTheme.Custom, theme)
                : right.UseTheme(theme);
            foreach (string message in result.Messages)
                Console.Error.WriteLine(message);

            var split = new SplitContainer { Dock = DockStyle.Fill };
            split.Panel1.Controls.Add(left);
            split.Panel2.Controls.Add(right);

            Form frm = new Form { Text = "Two designers, two themes", Width = 1600, Height = 900 };
            frm.Controls.Add(split);
            frm.Load += (s, e) =>
            {
                split.SplitterDistance = frm.ClientSize.Width / 2;
                left.OpenFile(report);
                right.OpenFile(report);
            };

            Application.Run(frm);
        }
    }
}
