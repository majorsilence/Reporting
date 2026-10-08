using Majorsilence.Reporting.RdlDesign.Resources;

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

            tbLicense.Text = Strings.DialogAbout_DialogAbout_About;

            lVersion.Text = string.Format(Strings.DialogAbout_DialogAbout_Version, Assembly.GetExecutingAssembly().GetName().Version);
            lVMVersion.Text = string.Format(".NET {0}", Environment.Version);
        }

        private void lnk_LinkClicked(object sender, LinkLabelLinkClickedEventArgs ea)
        {
            var lnk = (LinkLabel)sender;
            lnk.Links[lnk.Links.IndexOf(ea.Link)].Visited = true;
            System.Diagnostics.Process.Start(lnk.Tag.ToString());
        }
    }
}
