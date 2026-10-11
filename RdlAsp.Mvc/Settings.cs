using System.IO;

namespace Majorsilence.Reporting.RdlAsp
{
    public class Settings
    {
        /// <summary>
        /// Folder the reports are read from. A relative folder is relative to the content root of the application.
        /// </summary>
        public string ReportsFolder { get; set; } = "Reports/";

        /// <summary>The reports folder as a full path.</summary>
        internal string ResolveReportsFolder(string contentRootPath)
        {
            string folder = string.IsNullOrWhiteSpace(ReportsFolder) ? "Reports" : ReportsFolder;
            return Path.GetFullPath(Path.IsPathRooted(folder) ? folder : Path.Combine(contentRootPath, folder));
        }
    }
}
