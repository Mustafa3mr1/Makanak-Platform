using Makanak.Application.Common.Models;
using Makanak.Domain.Enums;

namespace Makanak.Application.DTOs.Admin;

public class AdminUserQuery : PagedQuery
{
    public string? Search { get; set; }
    public UserType? UserType { get; set; }
    public bool? IsActive { get; set; }
}

public class AdminPropertyQuery : PagedQuery
{
    public string? Keyword { get; set; }
    public PropertyStatus? Status { get; set; }
    public Guid? OwnerId { get; set; }
}


public sealed record AdminUserDto(Guid Id, string FullName, string Email, UserType UserType, bool IsActive, DateTime CreatedAtUtc);

public sealed record AdminUserDetailDto(
    AdminUserDto User, int PropertyCount, int OffersSubmitted, int OffersReceived);

public sealed record AdminPropertyDto(
    Guid Id, Guid OwnerId, string OwnerName, string Title, string City, decimal Price,
    PropertyType PropertyType, ListingType ListingType, PropertyStatus Status,
    int TotalOffers, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

public sealed record PlatformStatsDto(
    int TotalUsers, int ActiveUsers, IReadOnlyDictionary<string, int> UsersByType,
    int TotalProperties, IReadOnlyDictionary<string, int> PropertiesByStatus,
    int TotalOffers, IReadOnlyDictionary<string, int> OffersByStatus,
    int OffersCreatedLast7Days, DateTime GeneratedAtUtc);
