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

        static internal CompiledReport GetCachedReport(string file, IMemoryCache c)
        {
            if (!ReportHelper.DoCaching)			// caching is disabled
            {
                ReportHelper.IncrMisses(c);
                return null;
            }

            CompiledReport compiled = c.Get(CacheKey(file)) as CompiledReport;
            if (compiled == null)
            {
                ReportHelper.IncrMisses(c);
                return null;
            }
            ReportHelper.IncrHits(c);
            return compiled;
        }

        static internal void SaveCachedReport(CompiledReport r, string file, IMemoryCache c)
        {
            if (!ReportHelper.DoCaching)			// caching is disabled
                return;

            c.Set(CacheKey(file), r);
        }

        static internal string GetSource(string file)
        {
            return File.ReadAllText(file);
        }

        static internal void SetUserContext(Report r, HttpContext context, NeedPassword np)
        {
            // The password is part of the compiled definition (see RdlReport.LoadReport); a report made from a
            // CompiledReport cannot change it.
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

        private static bool IsUserSortKey(string key) =>
            key == UserSortState.SortIdParameter || key == UserSortState.SortDirectionParameter
            || key == UserSortState.SortStateParameter || key == UserSortState.SortAdditiveParameter;

        /// <summary>
        /// Interactive sorting (#636): applies the sort an HTML sort header link asked for
        /// (rs:SortState, rs:SortId, rs:SortDirection, rs:SortAdditive) to the report, and points
        /// its sort links back at this request's path and query, less those values.
        /// </summary>
        static internal void SetUserSort(Report r, HttpRequest request)
        {
            IQueryCollection q = request.Query;
            r.UserSort = UserSortState.FromQuery(
                q[UserSortState.SortStateParameter].ToString(),
                q[UserSortState.SortIdParameter].ToString(),
                q[UserSortState.SortDirectionParameter].ToString(),
                q[UserSortState.SortAdditiveParameter].ToString());

            var url = new StringBuilder(request.PathBase.Value + request.Path.Value);
            char sep = '?';
            foreach (var kvp in q)
            {
                if (IsUserSortKey(kvp.Key))
                    continue;
                foreach (string v in kvp.Value)
                {
                    url.Append(sep).Append(Uri.EscapeDataString(kvp.Key)).Append('=').Append(Uri.EscapeDataString(v ?? ""));
                    sep = '&';
                }
            }
            r.UserSortUrl = url.ToString();
        }

        // The sorts applied to the report, for the links and forms that run it again; null when none.
        private static string UserSortText(Report r)
        {
            string s = r.UserSort?.ToString();
            return string.IsNullOrEmpty(s) ? null : s;
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
            pHtml.Append("</table>");		// End of table
            string sorted = UserSortText(rpt);		// keep the interactive sort when the report is run again
            if (sorted != null)
                pHtml.AppendFormat("<input type=\"hidden\" name=\"{0}\" value=\"{1}\"/>", H(UserSortState.SortStateParameter), H(sorted));
            pHtml.Append("</form>");		// End of form
            return pHtml.ToString();
        }

        static private void AppendSubmitAndExports(StringBuilder pHtml, Report rpt, HttpContext context, string reportName, bool showReport)
        {
            pHtml.AppendFormat("<td><input type=\"submit\" value=\"Run Report\" tabindex=\"{0}\"/>", rpt.UserReportParameters.Count + 1);
            if (showReport)
                AppendExports(pHtml, context, reportName, UserSortText(rpt));
            pHtml.Append("</td>");
        }

        static private void AppendExports(StringBuilder pHtml, HttpContext context, string reportName, string sorted)
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
            if (sorted != null)     // exported in the order the report is shown in
                args.AppendFormat("{0}{1}={2}", args.Length == 0 ? "?" : "&",
                    Uri.EscapeDataString(UserSortState.SortStateParameter), Uri.EscapeDataString(sorted));
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
