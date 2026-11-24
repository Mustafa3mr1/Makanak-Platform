using Makanak.Domain.Enums;

namespace Makanak.Application.DTOs.Offers;

public record OfferPropertySummaryDto(
    Guid PublicId,
    string Title,
    string City,
    string? PrimaryImageUrl,
    ListingType ListingType,
    PropertyStatus Status
);

public record OfferCounterpartyDto(
    Guid PublicId,
    string FullName
);

public record NegotiationHistoryItemDto(
    string ActorFullName,
    string ActorRole,
    NegotiationActionType ActionType,
    decimal Price,
    int RoundNumber,
    DateTime CreatedAt
);

public record OfferListItemDto(
    Guid PublicId,
    OfferPropertySummaryDto Property,
    OfferCounterpartyDto Counterparty,
    decimal MyOfferPrice,
    decimal LatestPrice,
    int RoundNumber,
    OfferStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record OfferDetailsDto(
    Guid PublicId,
    OfferPropertySummaryDto Property,
    OfferCounterpartyDto Counterparty,
    decimal MyOfferPrice,
    decimal LatestPrice,
    int RoundNumber,
    OfferStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<string> AllowedActions,
    IReadOnlyList<NegotiationHistoryItemDto> History,
    string RowVersion
);

public record CreateOfferRequestDto(
    Guid PropertyPublicId,
    decimal InitialPrice
);

public record CounterOfferRequestDto(
    decimal CounterPrice
);
