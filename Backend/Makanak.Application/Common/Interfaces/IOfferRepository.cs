using Makanak.Domain.Entities;

namespace Makanak.Application.Common.Interfaces;

public interface IOfferRepository
{
    Task<Offer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Offer?> GetByIdWithPropertyAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Offer>> GetPendingOffersByPropertyIdAsync(Guid propertyId, Guid excludeOfferId, CancellationToken cancellationToken = default);
    Task AddAsync(Offer offer, CancellationToken cancellationToken = default);
    void Update(Offer offer);
}
