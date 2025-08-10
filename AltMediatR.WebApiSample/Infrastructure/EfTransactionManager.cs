using AltMediatR.DDD.Abstractions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;

namespace AltMediatR.WebApiSample.Infrastructure;

public sealed class EfTransactionManager : ITransactionManager
{
    private readonly AppDbContext _db;
    public EfTransactionManager(AppDbContext db) => _db = db;

    public async Task<ITransactionScope> BeginAsync(CancellationToken cancellationToken = default)
    {
        // InMemory provider doesn't support transactions; return no-op scope
        if (_db.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true)
        {
            return new NoOpScope();
        }

        var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        return new Scope(tx);
    }

    private sealed class Scope : ITransactionScope
    {
        private readonly IDbContextTransaction _tx;
        public Scope(IDbContextTransaction tx) => _tx = tx;

        public async ValueTask DisposeAsync() => await _tx.DisposeAsync();
        public async Task CommitAsync(CancellationToken cancellationToken = default) { await _tx.CommitAsync(cancellationToken); }
        public async Task RollbackAsync(CancellationToken cancellationToken = default) { await _tx.RollbackAsync(cancellationToken); }
    }

    private sealed class NoOpScope : ITransactionScope
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
