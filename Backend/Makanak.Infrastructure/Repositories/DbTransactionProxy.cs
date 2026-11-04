using Microsoft.EntityFrameworkCore.Storage;
using Makanak.Application.Common.Interfaces;

namespace Makanak.Infrastructure.Repositories;

/// <summary>
/// Wraps EF Core's IDbContextTransaction behind the application-layer abstraction.
/// Implements IAsyncDisposable so callers can safely use <c>await using</c>.
/// </summary>
public sealed class DbTransactionProxy : IDbTransactionProxy
{
    private readonly IDbContextTransaction _transaction;

    public DbTransactionProxy(IDbContextTransaction transaction) => _transaction = transaction;

    public Task CommitAsync(CancellationToken cancellationToken = default)
        => _transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default)
        => _transaction.RollbackAsync(cancellationToken);

    public ValueTask DisposeAsync()
        => _transaction.DisposeAsync();
}
