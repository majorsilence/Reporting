using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Majorsilence.Reporting.RdlAsp
{
    /// <summary>
    /// RdlReport generates a report from a RDL file and sends it as html, pdf, xml, csv, xlsx or rtf.
    /// </summary>
    public class RdlReport : Controller
    {
        private const string STATISTICS = "statistics";

        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IMemoryCache _cache;
        private readonly Settings _settings;

        // The result of the request being served.
        private List<string> _errors;
        private int _maxSeverity;
        private string _passPhrase;

        public RdlReport(IWebHostEnvironment webHostEnvironment, IMemoryCache cache,
            Settings settings)
        {
            _webHostEnvironment = webHostEnvironment;
            _cache = cache;
            _settings = settings;
        }

        /// <summary>
        /// Pass phrase used to read data source references that are encrypted.
        /// </summary>
        public string PassPhrase
        {
            set { _passPhrase = value; }
        }

        [HttpGet]
        [Route("msr/RdlReport/ShowFile/{reportFile}/{type?}")]
        public async Task<IActionResult> Render(string reportFile, string type = "html")
        {
            if (string.IsNullOrWhiteSpace(reportFile))
                return Failure(400, "ReportFile not specified.");

            if (reportFile == STATISTICS)
                return Content(DoStatistics(), "text/html");

            var format = GetFormat(type);
            if (format == null)
                return Failure(400, $"Unknown report type '{type}'.");

            string file = FindReportFile(reportFile.EndsWith(".rdl", StringComparison.OrdinalIgnoreCase) ? reportFile : reportFile + ".rdl");
            if (file == null)
                return Failure(404, null);

            var parameters = ReportHelper.GetParameters(Request.Query);
            Report report = null;
            RenderedReport rendered = null;
            try
            {
                report = await LoadReport(file);
                if (report != null)
                {
                    ReportHelper.SetUserContext(report, this.HttpContext, new NeedPassword(GetPassword));
                    await report.RunGetData(parameters);
                    rendered = await Generate(report, format);
                }
            }
            catch (Exception exe)
            {
                AddError(8, "Error: " + exe.Message);
            }

            if (report != null && report.ErrorMaxSeverity > 0)
            {
                AddError(report.ErrorMaxSeverity, report.ErrorItems);
                report.ErrorReset();
            }

            if (rendered == null)
                return Failure(500, null);

            if (format.Type != OutputPresentationType.HTML)
                return rendered.Bytes != null
                    ? File(rendered.Bytes, format.ContentType)
                    : Content(rendered.Text, format.ContentType);

            string name = Path.GetFileNameWithoutExtension(file);
            return Content(HtmlPage(report, rendered, parameters, name), format.ContentType);
        }

        private string GetPassword()
        {
            return _passPhrase;
        }

        // The report is a whole html document (its styles and images are in it). The parameters
        // and any errors are put at the top of its body.
        private string HtmlPage(Report report, RenderedReport rendered, IDictionary parameters, string name)
        {
            var top = new StringBuilder();
            top.AppendLine(ReportHelper.GetParameterHtml(report, parameters, HttpContext, name, true));
            AppendErrors(top);

            string html = rendered.Text;
            var body = Regex.Match(html, "<body[^>]*>", RegexOptions.IgnoreCase);
            return body.Success
                ? html.Insert(body.Index + body.Length, top.ToString())
                : top + html;
        }

        private void AppendErrors(StringBuilder sb)
        {
            if (_errors == null)
                return;
            sb.AppendLine("<table><tr><td>Errors</td></tr>");
            foreach (string e in _errors)
                sb.AppendLine($"<tr><td>{WebUtility.HtmlEncode(e)}</td></tr>");
            sb.AppendLine("</table>");
        }

        private IActionResult Failure(int statusCode, string message)
        {
            if (message != null)
                AddError(8, message);
            var sb = new StringBuilder("<!DOCTYPE html><html><body>");
            AppendErrors(sb);
            sb.Append("</body></html>");
            return new ContentResult { Content = sb.ToString(), ContentType = "text/html", StatusCode = statusCode };
        }

        private string DoStatistics()
        {
            ReportHelper s = ReportHelper.Get(_cache);
            int cacheEntries = (_cache as MemoryCache)?.Count ?? 0;

            var html = new StringBuilder();
            html.AppendLine($"<p>{cacheEntries} items are in the cache");
            html.AppendLine($"<p>{s.CacheHits} cache hits");
            html.AppendLine($"<p>{s.CacheMisses} cache misses");
            return html.ToString();
        }

        private async Task<RenderedReport> Generate(Report report, Format format)
        {
            var sg = new MemoryStreamGen(null, null, format.Extension);
            try
            {
                await report.RunRender(sg, format.Type, Guid.NewGuid().ToString());

                var rendered = new RenderedReport();
                if (format.IsText)
                    rendered.Text = sg.GetText();
                else
                    rendered.Bytes = ((MemoryStream)sg.MemoryList[0]).ToArray();
                return rendered;
            }
            finally
            {
                sg.CloseMainStream();
            }
        }

        private void AddError(int severity, string err)
        {
            if (_maxSeverity < severity)
                _maxSeverity = severity;
            (_errors ??= new List<string>()).Add(err);
        }

        private void AddError(int severity, IList errors)
        {
            if (_maxSeverity < severity)
                _maxSeverity = severity;
            _errors ??= new List<string>();
            foreach (string err in errors)
                _errors.Add(err);
        }

        private static Format GetFormat(string type)
        {
            switch ((type ?? "html").ToLowerInvariant())
            {
                case "htm":
                case "html":
                    return new Format(OutputPresentationType.HTML, "html", "text/html", true);
                case "pdf":
                    return new Format(OutputPresentationType.PDF, "pdf", "application/pdf", false);
                case "xml":
                    return new Format(OutputPresentationType.XML, "xml", "text/xml", true);
                case "csv":
                    return new Format(OutputPresentationType.CSV, "csv", "text/csv", true);
                case "xlsx":
                    return new Format(OutputPresentationType.ExcelTableOnly, "xlsx",
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", false);
                case "rtf":
                    return new Format(OutputPresentationType.RTF, "rtf", "application/rtf", true);
                default:
                    return null;
            }
        }

        private string FindReportFile(string file)
        {
            // The file name comes from the request: it must stay inside the reports folder.
            string foundFile = ReportPathResolver.ResolveWithin(
                _settings.ResolveReportsFolder(_webHostEnvironment.ContentRootPath), file);

            if (foundFile == null || !System.IO.File.Exists(foundFile))
            {
                foundFile = null;

                // Fall back to searching the content root for a bare file name. The search is not
                // given directory parts or wildcards, so it can only find a file by its exact name.
                // TODO: read search directory from configuration
                if (ReportPathResolver.IsBareFileName(file))
                {
                    var di = new DirectoryInfo(_webHostEnvironment.ContentRootPath);
                    FileInfo[] files = di.GetFiles(file, SearchOption.AllDirectories);
                    if (files.Length > 0)
                    {
                        foundFile = files[0].FullName;
                    }
                }
            }

            if (foundFile == null)
            {
                AddError(8, $"Report file '{file}' does not exist.");
                return null;
            }

            return foundFile;
        }

        private async Task<Report> LoadReport(string file)
        {
            Report report = ReportHelper.GetCachedReport(file, _cache);
            if (report != null)
                return report;

            try
            {
                // Make sure RdlEngine is configured before we ever parse a report
                RdlEngineConfig.RdlEngineConfigInit();

                var rdlp = new RDLParser(ReportHelper.GetSource(file));
                string folder = Path.GetDirectoryName(file);
                rdlp.Folder = folder;
                rdlp.DataSourceReferencePassword = new NeedPassword(this.GetPassword);

                report = await rdlp.Parse();
                if (report.ErrorMaxSeverity > 0)
                {
                    AddError(report.ErrorMaxSeverity, report.ErrorItems);
                    bool failed = report.ErrorMaxSeverity >= 8;
                    report.ErrorReset();
                    if (failed)
                        return null;
                }

                // we should tell the report where it got loaded from
                report.Folder = folder;
                report.Name = Path.GetFileNameWithoutExtension(file);
                report.GetDataSourceReferencePassword = new NeedPassword(GetPassword);
                ReportHelper.SaveCachedReport(report, file, _cache);
                return report;
            }
            catch (Exception e)
            {
                AddError(8, $"Exception parsing report {Path.GetFileName(file)}.  {e.Message}");
                return null;
            }
        }

        private sealed class Format
        {
            public Format(OutputPresentationType type, string extension, string contentType, bool isText)
            {
                Type = type;
                Extension = extension;
                ContentType = contentType;
                IsText = isText;
            }

            public OutputPresentationType Type { get; }
            public string Extension { get; }
            public string ContentType { get; }
            public bool IsText { get; }
        }

        private sealed class RenderedReport
        {
            public string Text;
            public byte[] Bytes;
        }
    }
}
