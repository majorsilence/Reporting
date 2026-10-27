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
            //
            // issue #471: a "Toggle theme" button on each side flips that designer's own ThemeScope
            // between Light and Dark without restarting, so every surface it draws -- toolbar, menus,
            // PropertyGrid, canvas, bands, selection handles, the Edit tab's SQL/XML syntax highlighting --
            // can be compared in both themes from the sample's own UI. See Examples/Hosting/Readme.md's
            // "Theming and white-labelling" section for what still has to be reached through the designer's
            // own UI (e.g. selecting a Value expression's "..." button for the expression editor) rather
            // than a button here.
            //
            // issue #469/#470: RdlUserControl has no built-in toolbar/menu entry for the Data Sources and
            // Dataset dialogs (only the stand-alone RdlDesigner shell does), so this sample adds its own
            // "Data Sources..." and "Datasets..." buttons calling the designer's ShowDataSourcesDialog()
            // and ShowDataSetDialog() directly.
            string report = System.IO.Path.Combine(AppContext.BaseDirectory, "SampleApp2-TestReport.rdl");
            string startupTheme = args.Length > 0 ? args[0] : "Dark";

            var left = new Majorsilence.Reporting.RdlDesign.RdlUserControl { Dock = DockStyle.Fill };
            var right = new Majorsilence.Reporting.RdlDesign.RdlUserControl { Dock = DockStyle.Fill };
            ApplyTheme(right, startupTheme);

            var leftPanel = new Panel { Dock = DockStyle.Fill };
            var rightPanel = new Panel { Dock = DockStyle.Fill };
            leftPanel.Controls.Add(DatasetsButton(left));
            leftPanel.Controls.Add(DataSourcesButton(left));
            leftPanel.Controls.Add(ToggleThemeButton(left));
            leftPanel.Controls.Add(left);
            rightPanel.Controls.Add(DatasetsButton(right));
            rightPanel.Controls.Add(DataSourcesButton(right));
            rightPanel.Controls.Add(ToggleThemeButton(right, startWithDark: true));
            rightPanel.Controls.Add(right);

            var split = new SplitContainer { Dock = DockStyle.Fill };
            split.Panel1.Controls.Add(leftPanel);
            split.Panel2.Controls.Add(rightPanel);

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

        private static void ApplyTheme(Majorsilence.Reporting.RdlDesign.RdlUserControl designer, string theme)
        {
            var result = theme.EndsWith(".css", StringComparison.OrdinalIgnoreCase)
                ? designer.UseTheme(Majorsilence.Reporting.RdlDesign.DesignerTheme.Custom, theme)
                : designer.UseTheme(theme);
            foreach (string message in result.Messages)
                Console.Error.WriteLine(message);
        }

        private static Button DataSourcesButton(Majorsilence.Reporting.RdlDesign.RdlUserControl designer)
        {
            var button = new Button { Text = "Data Sources...", Dock = DockStyle.Top, Height = 28 };
            button.Click += (s, e) => designer.ShowDataSourcesDialog();
            return button;
        }

        private static Button DatasetsButton(Majorsilence.Reporting.RdlDesign.RdlUserControl designer)
        {
            var button = new Button { Text = "Datasets...", Dock = DockStyle.Top, Height = 28 };
            var menu = new ContextMenuStrip();
            button.Click += (s, e) =>
            {
                menu.Items.Clear();
                foreach (string name in designer.GetDataSetNames())
                {
                    string dataSetName = name;
                    menu.Items.Add(name, null, (ms, me) => designer.ShowDataSetDialog(dataSetName));
                }
                menu.Items.Add("New...", null, (ms, me) => designer.ShowDataSetDialog());
                menu.Show(button, new System.Drawing.Point(0, button.Height));
            };
            return button;
        }

        private static Button ToggleThemeButton(Majorsilence.Reporting.RdlDesign.RdlUserControl designer, bool startWithDark = false)
        {
            bool dark = startWithDark;
            var button = new Button { Text = dark ? "Theme: Dark (click for Light)" : "Theme: Light (click for Dark)", Dock = DockStyle.Top, Height = 28 };
            button.Click += (s, e) =>
            {
                dark = !dark;
                ApplyTheme(designer, dark ? "Dark" : "Light");
                button.Text = dark ? "Theme: Dark (click for Light)" : "Theme: Light (click for Dark)";
            };
            return button;
        }
    }
}
