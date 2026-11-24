using Makanak.Domain.Enums;

namespace Makanak.Application.DTOs.Properties;

public record PropertyOwnerDto(
    Guid PublicId,
    string FullName
);

public record PropertyImageItemDto(
    Guid PublicId,
    string Url,
    bool IsPrimary
);

public record PropertyListItemDto(
    Guid PublicId,
    string Title,
    decimal Price,
    ListingType ListingType,
    PropertyType PropertyType,
    PropertyStatus Status,
    string City,
    int Bedrooms,
    int Bathrooms,
    decimal AreaSqm,
    string? PrimaryImageUrl,
    DateTime CreatedAt
);

public record PropertyDetailsDto(
    Guid PublicId,
    string Title,
    decimal Price,
    ListingType ListingType,
    PropertyType PropertyType,
    PropertyStatus Status,
    string City,
    int Bedrooms,
    int Bathrooms,
    decimal AreaSqm,
    string? PrimaryImageUrl,
    DateTime CreatedAt,
    string Description,
    string Address,
    decimal? Latitude,
    decimal? Longitude,
    IReadOnlyList<string> Features,
    IReadOnlyList<PropertyImageItemDto> Images,
    PropertyOwnerDto Owner,
    string? RowVersion
);

public record CreatePropertyRequestDto(
    string Title,
    string Description,
    decimal Price,
    ListingType ListingType,
    PropertyType PropertyType,
    string City,
    string Address,
    int Bedrooms,
    int Bathrooms,
    decimal AreaSqm,
    decimal? Latitude,
    decimal? Longitude,
    IReadOnlyList<string>? Features
);

public record UpdatePropertyRequestDto(
    string Title,
    string Description,
    decimal Price,
    ListingType ListingType,
    PropertyType PropertyType,
    string City,
    string Address,
    int Bedrooms,
    int Bathrooms,
    decimal AreaSqm,
    decimal? Latitude,
    decimal? Longitude,
    IReadOnlyList<string>? Features,
    string RowVersion,
    PropertyStatus? Status = null
);

public record PropertyFilterQueryDto(
    string? Q = null,
    string? City = null,
    List<PropertyType>? PropertyType = null,
    ListingType? ListingType = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? Sort = "newest",
    int Page = 1,
    int PageSize = 12
);
