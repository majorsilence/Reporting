using System;
using System.IO;
using System.Xml;
using Majorsilence.Forms;
using Majorsilence.Reporting.RdlDesign.Resources;

namespace Majorsilence.Reporting.RdlDesign
{
    // Options > Appearance: the designer's theme (issue #448), saved in config.xml as
    // <theme> (a built-in name or "Custom") and <themecss> (the CSS file for Custom).
    public partial class DialogToolOptions
    {
        private TabPage tpAppearance;
        private ComboBox cbTheme;
        private TextBox tbThemeFile;
        private Button bThemeBrowse;
        private TextBox tbThemeMessages;
        private XmlNode _DesktopTheme;
        private XmlNode _DesktopThemeCss;
        private bool bTheme = false;

        // A ComboBox entry: what is shown, and the (theme, cssPath) setting it stands for.
        private sealed class ThemeChoice
        {
            public string Text;
            public string Theme;
            public string CssPath;      // null for a built-in; "" for "Custom" until a file is chosen
            public override string ToString() => Text;
        }

        private void InitTheme()
        {
            tpAppearance = new TabPage { Text = Strings.DialogToolOptions_Appearance, Name = "tpAppearance", Tag = "appearance" };

            var lTheme = new Label { Text = Strings.DialogToolOptions_Theme, AutoSize = true, Location = new System.Drawing.Point(12, 18) };
            cbTheme = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new System.Drawing.Point(110, 14), Width = 260 };
            var lFile = new Label { Text = Strings.DialogToolOptions_ThemeFile, AutoSize = true, Location = new System.Drawing.Point(12, 54) };
            tbThemeFile = new TextBox { Location = new System.Drawing.Point(110, 50), Width = 260 };
            bThemeBrowse = new Button { Text = "...", Location = new System.Drawing.Point(376, 49), Width = 32 };
            tbThemeMessages = new TextBox
            {
                Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
                Location = new System.Drawing.Point(12, 90), Size = new System.Drawing.Size(396, 150),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
            };
            tpAppearance.Controls.AddRange(new Control[] { lTheme, cbTheme, lFile, tbThemeFile, bThemeBrowse, tbThemeMessages });
            tabControl1.Controls.Add(tpAppearance);

            foreach (string name in DesignerTheme.BuiltInNames)
                cbTheme.Items.Add(new ThemeChoice { Text = name, Theme = name });
            foreach (string file in DesignerTheme.ShippedThemeFiles())
                cbTheme.Items.Add(new ThemeChoice { Text = Path.GetFileNameWithoutExtension(file), Theme = DesignerTheme.Custom, CssPath = file });
            var custom = new ThemeChoice { Text = Strings.DialogToolOptions_ThemeCustom, Theme = DesignerTheme.Custom, CssPath = "" };
            cbTheme.Items.Add(custom);

            // select what config.xml says (InitDesktop has already found the nodes)
            string theme = _DesktopTheme?.InnerText;
            string css = _DesktopThemeCss?.InnerText ?? "";
            ThemeChoice selected = null;
            foreach (ThemeChoice c in cbTheme.Items)
            {
                bool match = string.Equals(theme, DesignerTheme.Custom, StringComparison.OrdinalIgnoreCase)
                    ? c.CssPath != null && string.Equals(c.CssPath, css, StringComparison.OrdinalIgnoreCase)
                    : c.CssPath == null && string.Equals(c.Theme, theme, StringComparison.OrdinalIgnoreCase);
                if (match) { selected = c; break; }
            }
            if (selected == null && string.Equals(theme, DesignerTheme.Custom, StringComparison.OrdinalIgnoreCase))
                selected = custom;
            cbTheme.SelectedItem = selected ?? cbTheme.Items[0];
            tbThemeFile.Text = css;
            UpdateThemeFileEnabled();

            cbTheme.SelectedIndexChanged += (s, e) =>
            {
                if (cbTheme.SelectedItem is ThemeChoice c && !string.IsNullOrEmpty(c.CssPath))
                    tbThemeFile.Text = c.CssPath;
                UpdateThemeFileEnabled();
                bTheme = true;
            };
            tbThemeFile.TextChanged += (s, e) => bTheme = true;
            bThemeBrowse.Click += bThemeBrowse_Click;
        }

        private void UpdateThemeFileEnabled()
        {
            bool custom = cbTheme.SelectedItem is ThemeChoice c && c.CssPath == "";
            tbThemeFile.Enabled = bThemeBrowse.Enabled = custom;
        }

        private async void bThemeBrowse_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = Strings.DialogToolOptions_ThemeFilter,
                FilterIndex = 1,
                CheckFileExists = true,
                InitialDirectory = Directory.Exists(DesignerTheme.ThemesDirectory) ? DesignerTheme.ThemesDirectory : AppContext.BaseDirectory,
            };
            try
            {
                if (await ofd.ShowDialogAsync(this) == DialogResult.OK)
                    tbThemeFile.Text = ofd.FileName;
            }
            finally
            {
                ofd.Dispose();
            }
        }

        private void HandleTheme()
        {
            var choice = cbTheme.SelectedItem as ThemeChoice;
            string theme = choice?.Theme ?? DesignerTheme.BuiltInNames[0];
            string css = choice?.CssPath == null ? "" : tbThemeFile.Text.Trim();

            var messages = DesignerTheme.Apply(theme, css).Messages;
            tbThemeMessages.Text = messages.Count == 0 ? Strings.DialogToolOptions_ThemeApplied : string.Join(Environment.NewLine, messages);

            EnsureDesktopDocument();
            _DesktopTheme = SetConfigValue(_DesktopTheme, "theme", theme);
            _DesktopThemeCss = SetConfigValue(_DesktopThemeCss, "themecss", css);
            Directory.CreateDirectory(Path.GetDirectoryName(optFileName));
            _DesktopDocument.Save(optFileName);
        }

        private XmlNode SetConfigValue(XmlNode node, string name, string value)
        {
            if (node == null)
            {
                node = _DesktopDocument.CreateElement(name);
                _DesktopConfig.AppendChild(node);
            }
            node.InnerText = value;
            return node;
        }
    }
}
