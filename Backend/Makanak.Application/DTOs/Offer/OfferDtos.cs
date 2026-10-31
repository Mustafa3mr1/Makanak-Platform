using Makanak.Application.Common.Models;
using Makanak.Domain.Enums;

namespace Makanak.Application.DTOs.Offer;

public sealed record SubmitOfferRequest(decimal Price);
public sealed record CounterOfferRequest(decimal Price);

public class OfferListQuery : PagedQuery
{
    public OfferStatus? Status { get; set; }
}

public sealed record OfferListItemDto(
    Guid Id, Guid PropertyId, string PropertyTitle, string PropertyCity, PropertyStatus PropertyStatus,
    Guid OwnerId, Guid SeekerId, string SeekerName, decimal AskingPrice, decimal CurrentPrice,
    OfferStatus Status, int RoundCount, string? AwaitingParty, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

public sealed record OfferDetailDto(
    Guid Id, Guid PropertyId, string PropertyTitle, PropertyStatus PropertyStatus,
    Guid OwnerId, string OwnerName, Guid SeekerId, string SeekerName,
    decimal AskingPrice, decimal CurrentPrice, OfferStatus Status, int RoundCount,
    string? AwaitingParty, string RowVersion, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

public sealed record NegotiationHistoryDto(
    Guid Id, Guid OfferId, Guid ActionByUserId, string ActionByName,
    NegotiationActionType ActionType, decimal ProposedPrice, DateTime Timestamp);
