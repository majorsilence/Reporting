using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace Majorsilence.Reporting.RdlAsp
{
    internal class ReportHelper
    {
        static internal bool DoCaching = true;
        internal int CacheHits;
        internal int CacheMisses;

        private ReportHelper()
        {
            CacheHits = 0;
            CacheMisses = 0;
        }

        static internal ReportHelper Get(IMemoryCache app)
        {
            return app.GetOrCreate("fyistats", e =>
            {
                e.Priority = CacheItemPriority.NeverRemove;
                return new ReportHelper();
            });
        }

        static internal void IncrHits(IMemoryCache app)
        {
            ReportHelper s = Get(app);
            lock (s)
            {
                s.CacheHits++;
            }
        }

        static internal void IncrMisses(IMemoryCache app)
        {
            ReportHelper s = Get(app);
            lock (s)
            {
                s.CacheMisses++;
            }
        }

        // The report file's last write time is part of the key so an edited report is read again.
        private static string CacheKey(string file)
        {
            return "rdl:" + file + "|" + File.GetLastWriteTimeUtc(file).Ticks;
        }

        static internal Report GetCachedReport(string file, IMemoryCache c)
        {
            if (!ReportHelper.DoCaching)			// caching is disabled
            {
                ReportHelper.IncrMisses(c);
                return null;
            }

            ReportDefn rd = c.Get(CacheKey(file)) as ReportDefn;
            if (rd == null)
            {
                ReportHelper.IncrMisses(c);
                return null;
            }
            ReportHelper.IncrHits(c);
            return new Report(rd);
        }

        static internal void SaveCachedReport(Report r, string file, IMemoryCache c)
        {
            if (!ReportHelper.DoCaching)			// caching is disabled
                return;

            c.Set(CacheKey(file), r.ReportDefinition);
        }

        static internal string GetSource(string file)
        {
            return File.ReadAllText(file);
        }

        static internal void SetUserContext(Report r, HttpContext context, NeedPassword np)
        {
            r.GetDataSourceReferencePassword = np;
            if (context == null)                // may not always have a context
                return;

            // the first language of the Accept-Language header, e.g. "en-CA,en;q=0.9" gives "en-CA"
            string accept = context.Request.Headers["Accept-Language"].ToString();
            if (!string.IsNullOrWhiteSpace(accept))
            {
                string l = accept.Split(',')[0].Split(';')[0].Trim();
                if (l.Length > 0)
                    r.SetClientLanguage(l);
            }

            System.Security.Principal.IIdentity id = context.User?.Identity;
            if (id != null && id.IsAuthenticated)
                r.UserID = id.Name;
        }

        /// <summary>
        /// The query string values that are report parameters (the first value of each).
        /// </summary>
        static internal ListDictionary GetParameters(IQueryCollection query)
        {
            ListDictionary ld = new ListDictionary();
            foreach (var kvp in query)
            {
                if (kvp.Key.StartsWith("rs:"))
                    continue;
                if (!ld.Contains(kvp.Key))
                    ld.Add(kvp.Key, kvp.Value.ToString());
            }
            return ld;
        }

        private static string H(string s)
        {
            return WebUtility.HtmlEncode(s ?? "");
        }

        /// <summary>
        /// Returns the HTML needed to represent the parameters of a report.
        /// </summary>
        /// <param name="reportName">Name of the report as it is in the url (no extension).</param>
        /// <param name="showReport">True when the report itself is shown, which makes the export links useful.</param>
        static internal async Task<string> GetParameterHtml(Report rpt, IDictionary pd, HttpContext context, string reportName, bool showReport)
        {
            if (rpt == null)
                return "";

            StringBuilder pHtml = new StringBuilder();

            pHtml.AppendFormat("<form target=\"_self\" method=\"get\" action=\"{0}\">", H(context.Request.Path.Value));
            pHtml.Append("<table width=\"100%\">");

            int row = 0;
            foreach (UserReportParameter rp in rpt.UserReportParameters)
            {
                if (rp.Prompt == null)		// skip parameters that don't have a prompt
                    continue;

                pHtml.Append("<tr>");		// Create a row for each parameter

                pHtml.Append("<td>");		// Label definition
                pHtml.Append(H(rp.Prompt));
                pHtml.Append("</td><td>");	// end of label; start of control

                string defaultValue = "";
                object[] defaultValues = await rp.GetDefaultValueAsync();
                string[] displayValues = await rp.GetDisplayValuesAsync();
                object[] dataValues = await rp.GetDataValuesAsync();
                if (defaultValues != null)
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < defaultValues.Length; i++)
                    {
                        if (i > 0)
                            sb.Append(", ");
                        sb.Append(Convert.ToString(defaultValues[i], CultureInfo.InvariantCulture));
                    }
                    defaultValue = sb.ToString();
                }

                string pv = pd[rp.Name] as string;
                if (displayValues == null)
                {
                    // provide actual value if passed as parm otherwise default
                    pHtml.AppendFormat("<input type=\"text\" name=\"{0}\" value=\"{1}\" tabindex=\"{2}\" size=\"32\"/></td>",
                        H(rp.Name), H(pv ?? defaultValue), row + 1);
                }
                else
                {
                    pHtml.AppendFormat("<select name=\"{0}\" tabindex=\"{1}\" size=\"1\">", H(rp.Name), row + 1);
                    if (pv == null)
                        pv = defaultValue;
                    for (int i = 0; i < displayValues.Length; i++)
                    {
                        string selected = pv == Convert.ToString(dataValues[i], CultureInfo.InvariantCulture) ? " selected" : "";
                        pHtml.AppendFormat("<option value=\"{0}\"{2}>{1}</option>",
                            H(Convert.ToString(dataValues[i], CultureInfo.InvariantCulture)), H(displayValues[i]), selected);
                    }
                    pHtml.Append("</select></td>");
                }

                if (row == 0)
                    AppendSubmitAndExports(pHtml, rpt, context, reportName, showReport);

                pHtml.Append("</tr>");		// End of row for each parameter
                row++;
            }
            if (row == 0)
            {
                pHtml.Append("<tr>");
                AppendSubmitAndExports(pHtml, rpt, context, reportName, showReport);
                pHtml.Append("</tr>");
            }
            pHtml.Append("</table></form>");		// End of table, form
            return pHtml.ToString();
        }

        static private void AppendSubmitAndExports(StringBuilder pHtml, Report rpt, HttpContext context, string reportName, bool showReport)
        {
            pHtml.AppendFormat("<td><input type=\"submit\" value=\"Run Report\" tabindex=\"{0}\"/>", rpt.UserReportParameters.Count + 1);
            if (showReport)
                AppendExports(pHtml, context, reportName);
            pHtml.Append("</td>");
        }

        static private void AppendExports(StringBuilder pHtml, HttpContext context, string reportName)
        {
            StringBuilder args = new StringBuilder();
            foreach (var kvp in context.Request.Query)
            {
                if (kvp.Key.StartsWith("rs:"))
                    continue;
                foreach (string v in kvp.Value)
                    args.AppendFormat("{0}{1}={2}", args.Length == 0 ? "?" : "&",
                        Uri.EscapeDataString(kvp.Key), Uri.EscapeDataString(v ?? ""));
            }
            string sargs = args.ToString();
            string name = Uri.EscapeDataString(reportName);

            foreach (string type in new[] { "pdf", "xml", "csv", "xlsx" })
            {
                pHtml.AppendFormat(" <a href=\"{0}\" target=\"_blank\">{1}</a>",
                    H($"/msr/RdlReport/ShowFile/{name}/{type}{sargs}"), type.ToUpperInvariant());
            }
        }
    }
}
