using AltMediatR.Core.Abstractions;

namespace AltMediatR.Samples.Infrastructure
{
    public sealed class NoOpTransactionManager : ITransactionManager
    {
        public Task<ITransactionScope> BeginAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<ITransactionScope>(new NoOpScope());

        private sealed class NoOpScope : ITransactionScope
        {
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
            public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }
}
