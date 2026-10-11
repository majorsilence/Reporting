using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Majorsilence.Reporting.RdlAsp
{
    /// <summary>
    /// RdlListReports generates a list of reports available on the server
    /// </summary>
    public class RdlListReports : Controller
    {
        public string Frame = null;

        private readonly Settings _settings;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public RdlListReports(Settings settings, IWebHostEnvironment webHostEnvironment)
        {
            _settings = settings;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        [Route("msr/RdlList")]
        public ContentResult Index()
        {
            var html = new StringBuilder();

            FileInfo[] files;
            try
            {
                var di = new DirectoryInfo(_settings.ResolveReportsFolder(_webHostEnvironment.ContentRootPath));
                files = di.GetFiles("*.rdl").OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToArray();
            }
            catch (Exception ex)
            {
                html.AppendLine($"<p> No reports!  Exception={WebUtility.HtmlEncode(ex.Message)}");
                return Content(html.ToString(), "text/html");
            }

            string target = this.Frame ?? "_self";
            html.AppendLine("<table>");
            foreach (FileInfo file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file.Name);
                html.AppendLine(
                    $"<tr><td><a href=\"/msr/RdlReport/ShowFile/{Uri.EscapeDataString(name)}/html\" target=\"{WebUtility.HtmlEncode(target)}\">{WebUtility.HtmlEncode(name)}</a></td></tr>");
            }
            html.AppendLine("</table>");

            return Content(html.ToString(), "text/html");
        }
    }
}
