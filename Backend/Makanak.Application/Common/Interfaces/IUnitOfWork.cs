namespace Makanak.Application.Common.Interfaces;

/// <summary>
/// Unit of Work pattern interface guaranteeing transactional integrity across repositories.
/// </summary>
public interface IUnitOfWork
{
    IPropertyRepository Properties { get; }

    IOfferRepository Offers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IDbTransactionProxy> BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task ExecuteTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}
