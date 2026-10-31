using Makanak.Application.Common.Models;
using Makanak.Application.DTOs.Offer;
using Makanak.Domain.Enums;

namespace Makanak.Application.DTOs.User;

public sealed record UserProfileDto(Guid Id, string FullName, string Email, UserType UserType, DateTime CreatedAtUtc);

public sealed record UpdateProfileRequest(string FullName);

public sealed record OwnerPropertySummaryDto(
    Guid Id, string Title, string City, decimal Price, PropertyStatus Status,
    PropertyType PropertyType, ListingType ListingType,
    int TotalOffers, int OpenOffers, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

public sealed record OwnerDashboardDto(
    IReadOnlyDictionary<string, int> PropertiesByStatus,
    int OffersAwaitingYourResponse,
    PagedResult<OwnerPropertySummaryDto> Properties,
    IReadOnlyList<OfferListItemDto> RecentOffers);

public sealed record BuyerDashboardDto(
    IReadOnlyDictionary<string, int> OffersByStatus,
    int OffersAwaitingYourResponse,
    PagedResult<OfferListItemDto> Offers);
