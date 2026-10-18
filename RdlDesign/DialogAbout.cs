using Majorsilence.Reporting.RdlDesign.Resources;
using Majorsilence.Reporting.RdlViewer;

using System;
using System.Reflection;
using Majorsilence.Forms;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// Summary description for DialogAbout.
    /// </summary>
    public partial class DialogAbout 
    {
        public DialogAbout()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            // A white-labelled application's own text goes above the licence notice, never in its place:
            // the fyiReporting licence requires the notice.
            var branding = ReportingBranding.Current;
            tbLicense.Text = string.IsNullOrWhiteSpace(branding.AboutText)
                ? Strings.DialogAbout_DialogAbout_About
                : branding.AboutText + Environment.NewLine + Environment.NewLine + Strings.DialogAbout_DialogAbout_About;

            if (!string.IsNullOrWhiteSpace(branding.DesignerName))
                label8.Text = branding.DesignerName;
            SetLink(linkLabel3, branding.EffectiveSupportUrl);
            SetLink(linkLabel4, branding.EffectiveWebsiteUrl);

            lVersion.Text = string.Format(Strings.DialogAbout_DialogAbout_Version, Assembly.GetExecutingAssembly().GetName().Version);
            lVMVersion.Text = string.Format(".NET {0}", Environment.Version);
        }

        private static void SetLink(LinkLabel link, string url)
        {
            link.Text = url;
            link.Tag = url;
        }

        private void lnk_LinkClicked(object sender, LinkLabelLinkClickedEventArgs ea)
        {
            var lnk = (LinkLabel)sender;
            lnk.Links[lnk.Links.IndexOf(ea.Link)].Visited = true;
            // .NET Core only opens a URL through the shell when asked to; Process.Start (url) threw
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(lnk.Tag.ToString()) { UseShellExecute = true });
        }
    }
}
