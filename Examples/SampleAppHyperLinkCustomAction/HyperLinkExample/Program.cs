using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Forms;

namespace HyperLinkExample
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
            Application.Run(new Form1());
        }
    }
}
