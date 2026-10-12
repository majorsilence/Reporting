using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Data
{
    /// <summary>
    /// Implemented by an <see cref="IDbCommand"/> that can fetch its data without blocking. It lives
    /// side by side with the synchronous <see cref="IDbCommand.ExecuteReader(CommandBehavior)"/>, which
    /// keeps working; the report engine prefers this method when it is available.
    /// </summary>
    /// <remarks>
    /// The returned reader is an ordinary <see cref="IDataReader"/> over data that has already been
    /// fetched, so reading rows never blocks. This is what lets HTTP backed providers run in the
    /// browser, where a synchronous request is not possible.
    /// </remarks>
    public interface IAsyncDataCommand
    {
        Task<IDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken = default);
    }
}
