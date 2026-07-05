using System;
using System.Collections.Generic;
using Majorsilence.Forms;

namespace Majorsilence.Reporting.RdlMapFile
{
    static class RdlMapFile
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string[] args = Environment.GetCommandLineArgs();   // we assume the arg line contains a filename if any

            string file = null;
            bool bnext = false;
            for (int i = 0; i < args.Length && file == null; i++)
            {
                if (bnext)
                    file = args[i];
                else if (args[i] == "/f")
                    bnext = true;
                else if (args[i].StartsWith("/f"))
                    file = args[i].Substring(2);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MapFile(file));
        }
    }
}