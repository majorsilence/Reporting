using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SampleApp2_SetData
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Form frm = new Form();
            var ctl = new Majorsilence.Reporting.RdlDesign.RdlUserControl();
            ctl.OpenFile(@"C:\Users\peter\source\repos\My-FyiReporting\Examples\Examples\FileDirectoryTest.rdl");
            ctl.Dock = (Majorsilence.Forms.DockStyle)DockStyle.Fill;
            frm.Controls.Add(ctl);

            Application.Run(frm);

        }
    }
}
