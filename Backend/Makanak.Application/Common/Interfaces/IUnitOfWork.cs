using Makanak.Domain.Entities;

namespace Makanak.Application.Common.Interfaces;

public interface IUnitOfWork
{
    IPropertyRepository Properties { get; }
    IRepository<PropertyImage> PropertyImages { get; }
    IOfferRepository Offers { get; }
    IRepository<NegotiationHistory> NegotiationHistories { get; }

    Task<int> SaveChangesAsync();

    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action);

    void SetOriginalRowVersion<TEntity>(TEntity entity, byte[] rowVersion) where TEntity : class;
}
