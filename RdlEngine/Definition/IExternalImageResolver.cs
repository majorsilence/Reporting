using System.Threading;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// Supplied by the host to take over fetching External-source image bytes. When a Report
    /// carries a resolver, every External image value goes through it instead of the engine's
    /// own HttpClient/file handling, still once per distinct value through the per-report
    /// external-image cache. A null result keeps the engine's missing-image behaviour for
    /// that instance.
    /// </summary>
    public interface IExternalImageResolver
    {
        /// <param name="value">The image's evaluated Value: a URL, file name, or whatever the expression produced.</param>
        /// <param name="widthPx">The image item's design width at 96 dpi, or null when unknown.</param>
        /// <param name="heightPx">The image item's design height at 96 dpi, or null when unknown.</param>
        /// <param name="cancellationToken">Cancellation for the fetch.</param>
        Task<ExternalImageResult> Resolve(string value, int? widthPx, int? heightPx, CancellationToken cancellationToken);
    }

    /// <summary>The bytes a resolver produced for an External image value.</summary>
    public sealed class ExternalImageResult
    {
        public byte[] Content { get; init; }
        public string MimeType { get; init; }
    }
}
