using Microsoft.EntityFrameworkCore.Storage;
using Makanak.Application.Common.Interfaces;

namespace Makanak.Infrastructure.Repositories;

public sealed class DbTransactionProxy : IDbTransactionProxy
{
    private readonly IDbContextTransaction _transaction;

    public DbTransactionProxy(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return _transaction.CommitAsync(cancellationToken);
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        return _transaction.RollbackAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _transaction.DisposeAsync();
    }
}
