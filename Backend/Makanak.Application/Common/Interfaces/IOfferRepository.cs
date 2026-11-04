using Makanak.Domain.Entities;

namespace Makanak.Application.Common.Interfaces;

/// <summary>
/// Repository for <see cref="Offer"/> aggregates.
/// All lookup methods by external identifier use <c>Guid publicId</c>.
/// Internal <c>long Id</c> is never exposed across this boundary.
/// </summary>
public interface IOfferRepository
{
    Task<Offer?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);
    Task<Offer?> GetByPublicIdWithPropertyAsync(Guid publicId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all Pending or Countered offers for a property (by internal long FK),
    /// excluding a specific offer by its internal long Id.
    /// Used during offer acceptance to bulk-reject competing offers.
    /// </summary>
    Task<IReadOnlyList<Offer>> GetPendingOffersByPropertyIdAsync(long propertyId, long excludeOfferId, CancellationToken cancellationToken = default);

    Task AddAsync(Offer offer, CancellationToken cancellationToken = default);
    void Update(Offer offer);
}
