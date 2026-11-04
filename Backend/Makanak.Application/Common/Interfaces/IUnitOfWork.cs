namespace Makanak.Application.Common.Interfaces;

/// <summary>
/// Unit of Work: coordinates all repository instances over a single DbContext.
/// Exposes <see cref="INegotiationHistoryRepository"/> so services never need to inject DbContext directly.
/// </summary>
public interface IUnitOfWork
{
    IPropertyRepository Properties { get; }
    IOfferRepository Offers { get; }
    INegotiationHistoryRepository NegotiationHistories { get; }
    IUserRepository Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbTransactionProxy> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task ExecuteTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}
