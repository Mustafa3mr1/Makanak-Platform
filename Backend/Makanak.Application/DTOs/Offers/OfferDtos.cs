using Makanak.Domain.Enums;

namespace Makanak.Application.DTOs.Offers;

public record CreateOfferRequestDto(
    Guid PropertyId,
    decimal InitialPrice
);

public record CounterOfferRequestDto(
    decimal CounterPrice
);

public record OfferDto(
    Guid Id,
    Guid PropertyId,
    Guid SeekerId,
    decimal CurrentPrice,
    OfferStatus Status,
    int RoundCount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);
