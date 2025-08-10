using System.Threading;
using System.Threading.Tasks;

namespace AltMediatR.Core.Abstractions
{
    /// <summary>
    /// Abstraction for starting and controlling a database transaction.
    /// Provide an implementation (e.g., EF Core) to enforce real transactional boundaries.
    /// </summary>
    public interface ITransactionManager
    {
        Task<ITransactionScope> BeginAsync(CancellationToken cancellationToken = default);
    }

    public interface ITransactionScope : IAsyncDisposable
    {
        Task CommitAsync(CancellationToken cancellationToken = default);
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}
