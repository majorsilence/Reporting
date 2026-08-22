
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Majorsilence.Forms;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// DialogListOfStrings: puts up a dialog that lets a user enter a list of strings
    /// </summary>
    public partial class DialogListOfStrings 
    {
        public DialogListOfStrings(List<string> list)
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            if (list == null || list.Count == 0)
                return;

            // Populate textbox with the list of strings
            string[] sa = new string[list.Count];
            int l = 0;
            foreach (string v in list)
            {
                sa[l++] = v;
            }
            tbStrings.Lines = sa;

            return;
        }

        public List<string> ListOfStrings
        {
            get
            {
                if (this.tbStrings.Text.Length == 0)
                    return null;
                List<string> l = new List<string>();
                foreach (string v in tbStrings.Lines)
                {
                    if (v.Length > 0)
                        l.Add(v);
                }
                return l;
            }
        }

        private void lnk_LinkClicked(object sender, LinkLabelLinkClickedEventArgs ea)
        {
            LinkLabel lnk = (LinkLabel)sender;
            lnk.Links[lnk.Links.IndexOf(ea.Link)].Visited = true;
            System.Diagnostics.Process.Start(lnk.Tag.ToString());
        }
    }

}
