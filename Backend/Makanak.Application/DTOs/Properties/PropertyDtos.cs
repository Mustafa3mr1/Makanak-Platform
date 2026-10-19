using Makanak.Domain.Enums;

namespace Makanak.Application.DTOs.Properties;

public record CreatePropertyRequestDto(
    string Title,
    string Description,
    decimal Price,
    double Latitude,
    double Longitude
);

public record PropertyDto(
    Guid Id,
    Guid OwnerId,
    string Title,
    string Description,
    decimal Price,
    PropertyStatus Status,
    double Latitude,
    double Longitude,
    DateTime CreatedAtUtc
);
