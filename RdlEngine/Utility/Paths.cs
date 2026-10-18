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
        private static string _applicationFolderName = "MajorsilenceReporting";

        /// <summary>
        /// The name of the folder, under the user's application-data folders, that holds the designer's
        /// settings and caches. A white-labelled application sets its own before creating any designer.
        /// </summary>
        public static string ApplicationFolderName
        {
            get => _applicationFolderName;
            set
            {
                if (string.IsNullOrWhiteSpace(value) || value == "." || value == ".." || value.IndexOfAny(new[] { '/', '\\' }) >= 0 || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    throw new ArgumentException("A folder name is required: no path separators or invalid characters.", nameof(value));
                _applicationFolderName = value;
            }
        }

        public static string MajorsilenceRoamingFolder()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ApplicationFolderName);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        public static string MajorsilenceLocalFolder()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ApplicationFolderName);
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
