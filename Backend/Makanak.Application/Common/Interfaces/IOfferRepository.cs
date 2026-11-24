using Makanak.Application.Common.Models;
using Makanak.Application.DTOs.Offers;
using Makanak.Domain.Entities;
using Makanak.Domain.Enums;

namespace Makanak.Application.Common.Interfaces;

/// <summary>
/// Repository for <see cref="Offer"/> aggregates.
/// All lookup methods by external identifier use <c>Guid publicId</c>.
/// Internal <c>long Id</c> is never exposed across this boundary.
/// </summary>
public interface IOfferRepository
{
    Task<Offer?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);
    Task<Offer?> GetByPublicIdWithDetailsAsync(Guid publicId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveOfferAsync(long propertyId, long seekerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Offer>> GetPendingOffersByPropertyIdAsync(long propertyId, long excludeOfferId, CancellationToken cancellationToken = default);
    Task<PagedResult<OfferListItemDto>> GetSeekerOffersAsync(long seekerId, OfferStatus? statusFilter, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<OfferListItemDto>> GetOwnerIncomingOffersAsync(long ownerId, OfferStatus? statusFilter, Guid? propertyPublicId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(int PendingOffers, int TotalOffers)> GetOwnerOfferCountsAsync(long ownerId, CancellationToken cancellationToken = default);
    Task<DTOs.Dashboard.SeekerDashboardDto> GetSeekerDashboardCountsAsync(long seekerId, CancellationToken cancellationToken = default);
    Task AddAsync(Offer offer, CancellationToken cancellationToken = default);
    void Update(Offer offer);
}
