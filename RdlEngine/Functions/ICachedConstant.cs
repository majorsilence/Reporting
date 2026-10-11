using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// A constant expression that keeps the completed tasks for its value, made once when it is parsed. A constant
    /// is evaluated for every row (and for every style property of every cell), and a completed Task is immutable,
    /// so the same instance can be handed out any number of times and from any thread.
    /// </summary>
    internal interface ICachedConstant
    {
        Task<object> ObjectTask { get; }
        Task<string> StringTask { get; }
    }
}
