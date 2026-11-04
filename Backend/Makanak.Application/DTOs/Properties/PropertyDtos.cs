using Makanak.Domain.Enums;

namespace Makanak.Application.DTOs.Properties;

public record CreatePropertyRequestDto(
    string Title,
    string Description,
    decimal Price,
    string City,
    string Address
);

/// <summary>
/// Property DTO returned to API clients.
/// Only Guid PublicIds are exposed — no internal long IDs.
/// </summary>
public record PropertyDto(
    Guid Id,          // Property.PublicId
    Guid OwnerId,     // ApplicationUser.PublicId
    string Title,
    string Description,
    decimal Price,
    PropertyStatus Status,
    string City,
    string Address,
    DateTime CreatedAtUtc
);

/// <summary>DTO representing a single property image (FR-04).</summary>
public record PropertyImageDto(
    Guid Id,          // PropertyImage.PublicId
    Guid PropertyId,  // Property.PublicId
    string ImageUrl,
    bool IsPrimary
);
