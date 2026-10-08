using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Text;
using Majorsilence.Reporting.Rdl;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.RdlAsp
{
    /// <summary>
    /// Summary description for Class1.
    /// </summary>
    public class RdlReport : Controller
    {
        /// <summary>
        /// RdlReport generates an HTML report from a RDL file.
        /// </summary>
        /// 
        private const string STATISTICS = "statistics";

        private string _ReportFile = null;
        private ArrayList _Errors = null;
        private int _MaxSeverity = 0;
        private string _CSS = null;
        private string _JavaScript = null;
        private string _Html = null;
        private string _Xml = null;
        private string _Csv = null;
        private byte[] _Object = null;
        private string _ParameterHtml = null;
        private OutputPresentationType _RenderType = OutputPresentationType.HTML;
        private string contentType = "text/html";
        private string _PassPhrase = null;
        private bool _NoShow;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IMemoryCache _cache;
        private readonly Settings _settings;

        public RdlReport(IWebHostEnvironment webHostEnvironment, IMemoryCache cache,
            Settings settings)
        {
            _webHostEnvironment = webHostEnvironment;
            _cache = cache;
            _settings = settings;
        }

        [HttpGet]
        [Route("msr/RdlReport/ShowFile/{reportFile}/{type?}")]
        public async Task<IActionResult> Render(string reportFile, string type = "html")
        {
            this.RenderType = type;
            await SetReportFile(reportFile);

            var htmlContent = new StringBuilder();
            if (_ReportFile == null)
            {
                this.AddError(8, "ReportFile not specified.");
                return Content("", contentType);
            }
            else if (_ReportFile == STATISTICS)
            {
                DoStatistics(ref htmlContent);
                return Content(htmlContent.ToString());
            }
            else if (_Html != null)
                htmlContent.AppendLine(_Html);
            else if (_Object != null)
            {
                return File(_Object, contentType);
            }
            else // we never generated anything!
            {
                if (_Errors != null)
                {
                    htmlContent.AppendLine("<table>");
                    htmlContent.AppendLine("<tr>");
                    htmlContent.AppendLine("<td>");
                    htmlContent.AppendLine("Errors");
                    htmlContent.AppendLine("</td>");
                    htmlContent.AppendLine("</tr>");

                    foreach (string e in _Errors)
                    {
                        htmlContent.AppendLine("<tr>");
                        htmlContent.AppendLine("<td>");
                        htmlContent.AppendLine(e);
                        htmlContent.AppendLine("</td>");
                        htmlContent.AppendLine("</tr>");
                    }

                    htmlContent.AppendLine("</table>");
                }
            }

            return Content(htmlContent.ToString(), contentType);
        }

        /// <summary>
        /// When true report won't be shown but parameters (if any) will be
        /// </summary>
        public bool NoShow
        {
            get { return _NoShow; }
            set { _NoShow = value; }
        }

        public string RenderType
        {
            get
            {
                switch (_RenderType)
                {
                    case OutputPresentationType.ASPHTML:
                    case OutputPresentationType.HTML:
                        return "html";
                    case OutputPresentationType.PDF:
                        return "pdf";
                    case OutputPresentationType.XML:
                        return "xml";
                    case OutputPresentationType.CSV:
                        return "csv";
                    case OutputPresentationType.ExcelTableOnly:
                    case OutputPresentationType.Excel2007:
                        return "xlsx";
                    case OutputPresentationType.RTF:
                        return "rtf";
                    default:
                        return "html";
                }
            }
            set
            {
                _RenderType = this.GetRenderType(value);
            }
        }

        public string ReportFile
        {
            get { return _ReportFile; }
        }

        private async Task SetReportFile(string value)
        {
            if (!value.EndsWith(".rdl"))
            {
                value += ".rdl"; // assume it's a rdl file
            }

            _ReportFile = FindReportFile(value);
            // Clear out old report information (if any)
            this._Errors = null;
            this._MaxSeverity = 0;
            _CSS = null;
            _JavaScript = null;
            _Html = null;
            _ParameterHtml = null;

            if (_ReportFile == STATISTICS)
            {
                var sb = new StringBuilder();

                DoStatistics(ref sb);
                _Html = sb.ToString();

                return;
            }

            // Build the new report
            string contentRootPath = _webHostEnvironment.ContentRootPath;
            string pfile = Path.Combine(contentRootPath, _ReportFile);
            
            await DoRender(pfile);
        }


        public string PassPhrase
        {
            set
            {
                _PassPhrase = value;
            }
        }

        private string GetPassword()
        {
            return _PassPhrase;
        }


        public string Html
        {
            get
            {
                return _Html;
            }
        }

        public string Xml
        {
            get
            {
                return _Xml;
            }
        }

        public string CSV
        {
            get
            {
                return _Csv;
            }
        }

        public byte[] Object
        {
            get
            {
                return _Object;
            }
        }

        public ArrayList Errors
        {
            get { return _Errors; }
        }

        public int MaxErrorSeverity
        {
            get
            {
                return _MaxSeverity;
            }
        }

        public string CSS
        {
            get
            {
                return _CSS;
            }
        }

        public string JavaScript
        {
            get
            {
                return _JavaScript;
            }
        }

        public string ParameterHtml
        {
            get
            {
                return _ParameterHtml;
            }
        }


        // Render the report files with the requested types
        private async Task DoRender(string file)
        {
            string source;
            Report report = null;

            var nvc = this.HttpContext.Request.Query; // parameters
            ListDictionary ld = new ListDictionary();
            try
            {
                foreach (var kvp in nvc)
                {
                    ld.Add(kvp.Key, kvp.Value);
                }
                
                report = ReportHelper.GetCachedReport(file, _cache);

                if (report == null) // couldn't obtain report definition from cache
                {
                    // Obtain the source
                    source = ReportHelper.GetSource(file);
                    if (source == null)
                        return; // GetSource reported the error

                    // Compile the report
                    report = await this.GetReport(source, file);
                    if (report == null)
                        return;

                    ReportHelper.SaveCachedReport(report, file, _cache);
                }

                // Set the user context information: ID, language
                ReportHelper.SetUserContext(report, this.HttpContext, new Rdl.NeedPassword(GetPassword));

                // Obtain the data if report is being generated
                if (!_NoShow)
                {
                    await report.RunGetData(ld);
                    await Generate(report);
                }
            }
            catch (Exception exe)
            {
                AddError(8, "Error: {0}", exe.Message);
            }

            if (_ParameterHtml == null)
                _ParameterHtml =
                    ReportHelper.GetParameterHtml(report, ld, this.HttpContext, _ReportFile,
                        _NoShow); // build the parameter html
        }

        private void AddError(int severity, string err, params object[] args)
        {
            if (_MaxSeverity < severity)
                _MaxSeverity = severity;

            string error = string.Format(err, args);
            if (_Errors == null)
                _Errors = new ArrayList();
            _Errors.Add(error);
        }

        private void AddError(int severity, IList errors)
        {
            if (_MaxSeverity < severity)
                _MaxSeverity = severity;
            if (_Errors == null)
            {
                // if we don't have any we can just start with this list
                _Errors = new ArrayList(errors);
                return;
            }

            // Need to copy all items in the errors array
            foreach (string err in errors)
                _Errors.Add(err);
        }

        private void DoStatistics(ref StringBuilder htmlContent)
        {
            RdlSession rs = _cache.Get(RdlSession.SessionStat) as RdlSession;
            ReportHelper s = ReportHelper.Get(_cache);
            IMemoryCache c = _cache;

            int sessions = 0;
            if (rs != null)
                sessions = rs.Count;


            var cacheEntries = GetCacheEntries(c);
            htmlContent.AppendLine($"<p>{sessions} sessions");
            htmlContent.AppendLine($"<p>{cacheEntries.Count} items are in the cache");
            htmlContent.AppendLine($"<p>{s.CacheHits} cache hits");
            htmlContent.AppendLine($"<p>{s.CacheMisses} cache misses");
            
        }

        private List<string> GetCacheEntries(IMemoryCache cache)
        {
            var field = cache.GetType()
                .GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance);
            var collection = field.GetValue(cache) as ICollection;
            var items = new List<string>();
            if (collection != null)
                foreach (var item in collection)
                {
                    var methodInfo = item.GetType().GetProperty("Key");
                    var val = methodInfo.GetValue(item);
                    items.Add(val.ToString());
                }

            return items;
        }

        private async Task Generate(Report report)
        {
            MemoryStreamGen sg = null;
            try
            {
                sg = new MemoryStreamGen("ShowFile?type=", null, this.RenderType);

                await report.RunRender(sg, _RenderType, Guid.NewGuid().ToString());
                _CSS = "";
                _JavaScript = "";
                switch (_RenderType)
                {
                    case OutputPresentationType.ASPHTML:
                    case OutputPresentationType.HTML:
                        _CSS = report.CSS; //.Replace("position: relative;", "position: absolute;");
                        _JavaScript = report.JavaScript;
                        _Html = sg.GetText();
                        break;
                    case OutputPresentationType.XML:
                        _Xml = sg.GetText();
                        break;
                    case OutputPresentationType.CSV:
                        _Csv = sg.GetText();
                        break;
                    case OutputPresentationType.PDF:
                        {
                            MemoryStream ms = sg.MemoryList[0] as MemoryStream;
                            _Object = ms.ToArray();
                            break;
                        }
                }

                // Now save off the other streams in the session context for later use
                IList strms = sg.MemoryList;
                IList names = sg.MemoryNames;
                for (int i = 1; i < sg.MemoryList.Count; i++) // we skip the first one
                {
                    string n = names[i] as string;
                    MemoryStream ms = strms[i] as MemoryStream;
                    HttpContext.Session.Set(n, ms.ToArray());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                if (sg != null)
                {
                    sg.CloseMainStream();
                }
            }

            if (report.ErrorMaxSeverity > 0)
            {
                AddError(report.ErrorMaxSeverity, report.ErrorItems);
                report.ErrorReset();
            }

            return;
        }

        private OutputPresentationType GetRenderType(string type)
        {
            switch (type.ToLower())
            {
                case "htm":
                case "html":
                    contentType = "text/html";
                    return OutputPresentationType.HTML;
                case "pdf":
                    contentType = "application/pdf";
                    return OutputPresentationType.PDF;
                case "xml":
                    contentType = "text/xml";
                    return OutputPresentationType.XML;
                case "csv":
                    contentType = "text/csv";
                    return OutputPresentationType.CSV;
                case "xlsx":
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    return OutputPresentationType.ExcelTableOnly;
                case "rtf":
                    contentType = "application/rtf";
                    return OutputPresentationType.RTF;
                default:
                    contentType = "text/html";
                    return OutputPresentationType.HTML;
            }
        }

        private string FindReportFile(string file)
        {
            // The file name comes from the request: it must stay inside the reports folder.
            string foundFile = ReportPathResolver.ResolveWithin(_settings.ReportsFolder, file);

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
                AddError(8, "Report file '{0}' does not exist.", file);
                return null;
            }

            return foundFile;
        }

        private async Task<Report> GetReport(string prog, string file)
        {
            // Now parse the file
            RDLParser rdlp;
            Report r;
            try
            {
                // Make sure RdlEngine is configured before we ever parse a program
                //   The config file must exist in the Bin directory.
                string[] searchDir =
                [
                    this.ReportFile.StartsWith("~") ? "~/Bin" : "/Bin" + Path.DirectorySeparatorChar,
                    System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                ];
                RdlEngineConfig.RdlEngineConfigInit(searchDir);

                rdlp = new RDLParser(prog);
                string folder = Path.GetDirectoryName(file);
                if (folder == "")
                    folder = Environment.CurrentDirectory;
                rdlp.Folder = folder;
                rdlp.DataSourceReferencePassword = new NeedPassword(this.GetPassword);

                r = await rdlp.Parse();
                if (r.ErrorMaxSeverity > 0)
                {
                    AddError(r.ErrorMaxSeverity, r.ErrorItems);
                    if (r.ErrorMaxSeverity >= 8)
                        r = null;
                    r.ErrorReset();
                }

                // If we've loaded the report; we should tell it where it got loaded from
                if (r != null)
                {
                    r.Folder = folder;
                    r.Name = Path.GetFileNameWithoutExtension(file);
                    r.GetDataSourceReferencePassword = new Rdl.NeedPassword(GetPassword);
                }
            }
            catch (Exception e)
            {
                r = null;
                AddError(8, "Exception parsing report {0}.  {1}", file, e.Message);
            }

            return r;
        }
    }
}