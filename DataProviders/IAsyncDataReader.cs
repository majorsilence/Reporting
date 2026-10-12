using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Data
{
    /// <summary>
    /// Implemented by an <see cref="IDataReader"/> that can read its next row without blocking. It lives
    /// side by side with <see cref="IDataReader.Read"/>, which keeps working; the report engine prefers
    /// this method when it is available.
    /// </summary>
    /// <remarks>
    /// Only readers that read from a stream (text, log, gedcom) gain anything from it. Readers over data
    /// that is already in memory do not need to implement it: the engine falls back to
    /// <see cref="IDataReader.Read"/>.
    /// </remarks>
    public interface IAsyncDataReader : IDataReader
    {
        Task<bool> ReadAsync(CancellationToken cancellationToken = default);
    }
}
