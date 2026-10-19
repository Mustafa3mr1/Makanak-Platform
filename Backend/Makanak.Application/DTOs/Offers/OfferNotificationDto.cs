using Makanak.Domain.Enums;

namespace Makanak.Application.DTOs.Offers;

public record OfferNotificationDto(
    Guid OfferId,
    Guid PropertyId,
    Guid SeekerId,
    decimal CurrentPrice,
    OfferStatus Status,
    int RoundCount,
    string Message,
    DateTime TimestampUtc
);
