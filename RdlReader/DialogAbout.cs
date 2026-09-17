using RdlReader.Resources;

using System;
using System.Reflection;
using Majorsilence.Forms;

namespace Majorsilence.Reporting.RdlReader
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

            tbLicense.Text = Strings.DialogAbout_About;

	        lVersion.Text = string.Format(Strings.DialogAbout_Version, Assembly.GetExecutingAssembly().GetName().Version);
        }

        private void lnk_LinkClicked(object sender, LinkLabelLinkClickedEventArgs ea)
        {
            try
            {
                var lnk = (LinkLabel)sender;
                lnk.Links[lnk.Links.IndexOf(ea.Link)].Visited = true;
                System.Diagnostics.Process.Start(lnk.Tag.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Strings.DialogAbout_ShowD_Exception, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
