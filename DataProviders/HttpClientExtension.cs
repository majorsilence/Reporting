using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Data
{
    internal static class HttpClientExtension
    {
        /// <summary>
        /// Synchronous GET for the IDataReader implementations, which cannot be async. The browser
        /// runtime has no synchronous HTTP (blocking its only thread would deadlock), so there the
        /// caller must fetch the data itself and hand it over as a file in the in-memory file
        /// system or with SetData.
        /// </summary>
        public static HttpResponseMessage SendSync(this HttpClient client, HttpRequestMessage request)
        {
            if (OperatingSystem.IsBrowser())
                throw new PlatformNotSupportedException(
                    "Reading '" + request.RequestUri + "' needs a synchronous HTTP request, which browsers do not allow. " +
                    "Fetch the data with HttpClient.GetAsync and either write it to the in-memory file system and point the " +
                    "data source at that file, or push it into the report with DataSet.SetData.");
#pragma warning disable CA1416 // not reached in the browser, see above
            return client.Send(request);
#pragma warning restore CA1416
        }

        public static void AddMajorsilenceReportingUserAgent(this HttpClient client)
        {
            if (client.DefaultRequestHeaders.UserAgent.Count == 0)
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3 MajorsilenceReporting/1.0");
            }
        }
    }
}
