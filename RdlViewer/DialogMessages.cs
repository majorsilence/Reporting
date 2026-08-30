
using System;
using System.Collections;
using System.ComponentModel;
using Majorsilence.Forms;

namespace Majorsilence.Reporting.RdlViewer
{
    /// <summary>
    /// DialogMessage is used in place of a message box when the text can be large
    /// </summary>
    public partial class DialogMessages 
    {

        public DialogMessages(IList msgs)
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            string[] lines = new string[msgs.Count];
            int l = 0;
            foreach (string msg in msgs)
            {
                lines[l++] = msg;
            }
            tbMessages.Lines = lines;
            return;
        }
    }
}
