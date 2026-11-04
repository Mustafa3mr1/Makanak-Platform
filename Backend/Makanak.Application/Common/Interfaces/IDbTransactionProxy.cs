namespace Makanak.Application.Common.Interfaces;

/// <summary>
/// Abstracts EF Core's IDbContextTransaction to prevent Infrastructure leaking into Application.
/// Implements <see cref="IAsyncDisposable"/> so callers can safely use <c>await using</c>.
/// </summary>
public interface IDbTransactionProxy : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
