using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl.Utility
{
    public static class Paths
    {
        public static string MajorsilenceRoamingFolder()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MajorsilenceReporting");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        public static string MajorsilenceLocalFolder()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MajorsilenceReporting");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        public static string MajorsilenceLocalStreamHacksFolder()
        {
            string path = System.IO.Path.Combine(MajorsilenceLocalFolder(), "stream-hacks");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }
    }
}
