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

            using var response = await GetAsync(client, url, auth, cancellationToken).ConfigureAwait(false);
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>As <see cref="TryReadTextAsync"/> but returns the raw bytes, so a parser can read them without a string in between.</summary>
        public static async Task<byte[]> TryReadBytesAsync(HttpClient client, string url, string auth, CancellationToken cancellationToken)
        {
            if (!IsHttp(url))
                return null;

            using var response = await GetAsync(client, url, auth, cancellationToken).ConfigureAwait(false);
            return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        }

        private static async Task<HttpResponseMessage> GetAsync(HttpClient client, string url, string auth, CancellationToken cancellationToken)
        {
            client.AddMajorsilenceReportingUserAgent();
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrWhiteSpace(auth))
            {
                var parts = auth.Split(':');
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(parts[0], parts[1]);
            }

            var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return response;
        }
    }
}
