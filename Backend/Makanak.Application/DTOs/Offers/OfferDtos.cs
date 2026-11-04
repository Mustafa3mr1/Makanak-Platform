using Makanak.Domain.Enums;

namespace Makanak.Application.DTOs.Offers;

public record CreateOfferRequestDto(
    Guid PropertyPublicId,   // Client always sends the PublicId (Guid) — never the internal long Id
    decimal InitialPrice
);

public record CounterOfferRequestDto(
    decimal CounterPrice
);

/// <summary>
/// Offer DTO returned to API clients.
/// Only Guid PublicIds are exposed — no internal long IDs.
/// </summary>
public record OfferDto(
    Guid Id,              // Offer.PublicId
    Guid PropertyId,      // Property.PublicId
    Guid SeekerId,        // ApplicationUser.PublicId
    decimal CurrentPrice,
    OfferStatus Status,
    int RoundCount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);
