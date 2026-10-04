using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Majorsilence.Forms;

namespace HyperLinkExample
{
    public partial class Form1 : Form
    {
        private string file = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            @"..\", @"..\", @"..\", @"..\", @"..\",  @"SqliteExamples\SimpleTest1.rdl");

        public Form1()
        {
            InitializeComponent();
        }

        private async void ButtonReloadReport_Click(object sender, EventArgs e)
        {
           await LoadReport();
        }

        private async Task LoadReport()
        {
            await rdlViewer1.SetSourceFile(new Uri(file));
            await rdlViewer1.Rebuild();
        }

        private void rdlViewer1_Hyperlink(object source, Majorsilence.Reporting.RdlViewer.HyperlinkEventArgs e)
        {
            // report LastName field as HyperLink action
            var url = new Uri(e.Hyperlink);
            if (url.Scheme == "lastname" )
            {
                e.Cancel = true;
                MessageBox.Show(url.ToString());
            }

        }
    }
}
