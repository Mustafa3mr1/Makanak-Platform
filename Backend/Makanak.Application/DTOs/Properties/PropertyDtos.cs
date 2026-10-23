using Makanak.Domain.Enums;

namespace Makanak.Application.DTOs.Properties;

public record CreatePropertyRequestDto(
    string Title,
    string Description,
    decimal Price,
    string City,
    string Address
);

public record PropertyDto(
    Guid Id,
    Guid OwnerId,
    string Title,
    string Description,
    decimal Price,
    PropertyStatus Status,
    string City,
    string Address,
    DateTime CreatedAtUtc
);

/// <summary>
/// DTO representing a single property image (FR-04).
/// </summary>
public record PropertyImageDto(
    Guid Id,
    Guid PropertyId,
    string ImageUrl,
    bool IsPrimary
);

