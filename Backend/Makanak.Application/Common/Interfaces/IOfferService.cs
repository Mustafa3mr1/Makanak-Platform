using Makanak.Application.Common.Models;
using Makanak.Application.DTOs.Offer;

namespace Makanak.Application.Common.Interfaces;

public interface IOfferService
{
    Task<OfferDetailDto> SubmitAsync(Guid propertyId, Guid seekerId, SubmitOfferRequest request);
    Task<OfferDetailDto> GetByIdAsync(Guid offerId, Guid userId, bool isAdmin);
    Task<IReadOnlyList<NegotiationHistoryDto>> GetHistoryAsync(Guid offerId, Guid userId, bool isAdmin);
    Task<PagedResult<OfferListItemDto>> GetMyOffersAsync(Guid seekerId, OfferListQuery query);
    Task<PagedResult<OfferListItemDto>> GetOwnerOffersAsync(Guid ownerId, OfferListQuery query);
    Task<OfferDetailDto> AcceptAsync(Guid offerId, Guid userId);
    Task<OfferDetailDto> RejectAsync(Guid offerId, Guid userId);
    Task<OfferDetailDto> CounterAsync(Guid offerId, Guid userId, CounterOfferRequest request);
}
