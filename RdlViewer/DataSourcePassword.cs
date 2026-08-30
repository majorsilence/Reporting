
using System;
using System.Collections;
using System.ComponentModel;
using Majorsilence.Forms;
using Majorsilence.Reporting.Rdl;
using Majorsilence.Reporting.RdlViewer;

namespace Majorsilence.Reporting.RdlViewer
{
    /// <summary>
    /// Summary description for ZoomTo.
    /// </summary>
    public partial class DataSourcePassword 
    {

        public DataSourcePassword()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

        }

        public string PassPhrase
        {
            get { return tbPassPhrase.Text; }
        }
    }
}
