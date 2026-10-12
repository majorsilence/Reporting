using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Data
{
    internal static class RemoteContent
    {
        public static bool IsHttp(string url) =>
            url != null && (url.StartsWith("http:", StringComparison.OrdinalIgnoreCase) ||
                            url.StartsWith("https:", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Downloads an http(s) url as text. Returns null for any other kind of url (files), which the
        /// readers open themselves.
        /// </summary>
        /// <param name="auth">Optional "scheme:parameters" Authorization header value.</param>
        public static async Task<string> TryReadTextAsync(HttpClient client, string url, string auth, CancellationToken cancellationToken)
        {
            if (!IsHttp(url))
                return null;

            client.AddMajorsilenceReportingUserAgent();
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrWhiteSpace(auth))
            {
                var parts = auth.Split(':');
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(parts[0], parts[1]);
            }

            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
