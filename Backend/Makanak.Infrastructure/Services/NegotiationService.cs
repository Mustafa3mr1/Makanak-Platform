using Makanak.Application.Common.Interfaces;
using Makanak.Application.Common.Models;
using Makanak.Application.DTOs.Offers;
using Makanak.Domain.Entities;
using Makanak.Domain.Enums;
using Makanak.Domain.Exceptions;

namespace Makanak.Infrastructure.Services;

/// <summary>
/// Negotiation service orchestrating offer workflows with transactional consistency.
/// DbContext leak eliminated: all operations go strictly through IUnitOfWork abstractions.
/// IDOR protection: not-party callers receive 404 (not 403).
/// </summary>
public sealed class NegotiationService : INegotiationService
{
    private readonly IUnitOfWork _unitOfWork;

    public NegotiationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OfferDetailsDto> CreateInitialOfferAsync(
        CreateOfferRequestDto request,
        long seekerInternalId,
        CancellationToken cancellationToken = default)
    {
        var property = await _unitOfWork.Properties.GetByPublicIdWithImagesAndOwnerAsync(request.PropertyPublicId, cancellationToken);
        if (property == null || property.IsDeleted)
        {
            throw new NotFoundException($"Property with identifier '{request.PropertyPublicId}' was not found.");
        }

        // Rule: Offers are only allowed on Available properties
        property.EnsureIsAvailable();

        // Rule: A seeker may not offer on their own property
        if (property.OwnerId == seekerInternalId)
        {
            throw new OwnPropertyOfferException();
        }

        // Rule: Only one active (Pending/Countered) offer per seeker per property
        bool hasActiveOffer = await _unitOfWork.Offers.HasActiveOfferAsync(property.Id, seekerInternalId, cancellationToken);
        if (hasActiveOffer)
        {
            throw new DuplicateActiveOfferException();
        }

        var offer = Offer.Create(property.Id, seekerInternalId, request.InitialPrice);
        await _unitOfWork.Offers.AddAsync(offer, cancellationToken);

        // Record submission in immutable negotiation audit trail
        _unitOfWork.NegotiationHistories.Add(NegotiationHistory.Record(
            offer.Id,
            seekerInternalId,
            NegotiationActionType.Submitted,
            offer.InitialPrice,
            offer.RoundNumber,
            offer.CreatedAt));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with details for full response
        var createdOffer = await _unitOfWork.Offers.GetByPublicIdWithDetailsAsync(offer.PublicId, cancellationToken);
        return await MapToOfferDetailsDtoAsync(createdOffer!, seekerInternalId, cancellationToken);
    }

    public async Task<OfferDetailsDto> CounterOfferAsync(
        Guid offerPublicId,
        CounterOfferRequestDto request,
        long actorInternalId,
        CancellationToken cancellationToken = default)
    {
        var offer = await _unitOfWork.Offers.GetByPublicIdWithDetailsAsync(offerPublicId, cancellationToken);
        if (offer == null || offer.Property.IsDeleted)
        {
            throw new NotFoundException($"Offer '{offerPublicId}' was not found.");
        }

        // IDOR protection: only property owner can counter
        if (offer.Property.OwnerId != actorInternalId)
        {
            throw new NotFoundException($"Offer '{offerPublicId}' was not found.");
        }

        // Check if property is sold
        if (offer.Property.Status == PropertyStatus.Sold)
        {
            throw new PropertyUnavailableException("Cannot counter an offer on a sold property.");
        }

        // Invariant: Counter enforces max rounds (<= 2)
        offer.Counter(request.CounterPrice);
        _unitOfWork.Offers.Update(offer);

        _unitOfWork.NegotiationHistories.Add(NegotiationHistory.Record(
            offer.Id,
            actorInternalId,
            NegotiationActionType.Countered,
            request.CounterPrice,
            offer.RoundNumber,
            offer.UpdatedAt));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapToOfferDetailsDtoAsync(offer, actorInternalId, cancellationToken);
    }

    public async Task<OfferDetailsDto> AcceptOfferAsync(
        Guid offerPublicId,
        long actorInternalId,
        CancellationToken cancellationToken = default)
    {
        var offer = await _unitOfWork.Offers.GetByPublicIdWithDetailsAsync(offerPublicId, cancellationToken);
        if (offer == null || offer.Property.IsDeleted)
        {
            throw new NotFoundException($"Offer '{offerPublicId}' was not found.");
        }

        bool isOwner = offer.Property.OwnerId == actorInternalId;
        bool isSeeker = offer.SeekerId == actorInternalId;

        if (!isOwner && !isSeeker)
        {
            throw new NotFoundException($"Offer '{offerPublicId}' was not found.");
        }

        // Allowed role matrix check
        string role = isOwner ? "Owner" : "Seeker";
        var allowedActions = offer.GetAllowedActions(role, offer.Property.Status, offer.Property.IsDeleted);
        if (!allowedActions.Contains("accept"))
        {
            throw new ForbiddenActionException($"You are not permitted to accept this offer in its current state ('{offer.Status}').");
        }

        var property = offer.Property;
        decimal finalPrice = offer.CounterPrice ?? offer.InitialPrice;

        // Atomic Unit of Work isolated transaction
        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            // 1. Mark target offer as Accepted
            offer.Accept();
            _unitOfWork.Offers.Update(offer);

            // 2. Mark property as Sold atomically
            property.MarkAsSold();
            _unitOfWork.Properties.Update(property);

            // 3. Reject competing pending/countered offers using internal clustered key joins
            var competingOffers = await _unitOfWork.Offers.GetPendingOffersByPropertyIdAsync(
                property.Id, offer.Id, cancellationToken);

            foreach (var competing in competingOffers)
            {
                competing.Reject();
                _unitOfWork.Offers.Update(competing);

                _unitOfWork.NegotiationHistories.Add(NegotiationHistory.Record(
                    competing.Id,
                    actorInternalId,
                    NegotiationActionType.Rejected,
                    competing.CounterPrice ?? competing.InitialPrice,
                    competing.RoundNumber,
                    DateTime.UtcNow));
            }

            // 4. Audit record for the accepted offer
            _unitOfWork.NegotiationHistories.Add(NegotiationHistory.Record(
                offer.Id,
                actorInternalId,
                NegotiationActionType.Accepted,
                finalPrice,
                offer.RoundNumber,
                offer.UpdatedAt));

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        return await MapToOfferDetailsDtoAsync(offer, actorInternalId, cancellationToken);
    }

    public async Task<OfferDetailsDto> RejectOfferAsync(
        Guid offerPublicId,
        long actorInternalId,
        CancellationToken cancellationToken = default)
    {
        var offer = await _unitOfWork.Offers.GetByPublicIdWithDetailsAsync(offerPublicId, cancellationToken);
        if (offer == null || offer.Property.IsDeleted)
        {
            throw new NotFoundException($"Offer '{offerPublicId}' was not found.");
        }

        bool isOwner = offer.Property.OwnerId == actorInternalId;
        bool isSeeker = offer.SeekerId == actorInternalId;

        if (!isOwner && !isSeeker)
        {
            throw new NotFoundException($"Offer '{offerPublicId}' was not found.");
        }

        string role = isOwner ? "Owner" : "Seeker";
        var allowedActions = offer.GetAllowedActions(role, offer.Property.Status, offer.Property.IsDeleted);
        if (!allowedActions.Contains("reject"))
        {
            throw new ForbiddenActionException($"You are not permitted to reject this offer in its current state ('{offer.Status}').");
        }

        decimal priceAtAction = offer.CounterPrice ?? offer.InitialPrice;

        offer.Reject();
        _unitOfWork.Offers.Update(offer);

        _unitOfWork.NegotiationHistories.Add(NegotiationHistory.Record(
            offer.Id,
            actorInternalId,
            NegotiationActionType.Rejected,
            priceAtAction,
            offer.RoundNumber,
            offer.UpdatedAt));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapToOfferDetailsDtoAsync(offer, actorInternalId, cancellationToken);
    }

    public async Task<OfferDetailsDto> GetOfferDetailsAsync(
        Guid offerPublicId,
        long callerInternalId,
        CancellationToken cancellationToken = default)
    {
        var offer = await _unitOfWork.Offers.GetByPublicIdWithDetailsAsync(offerPublicId, cancellationToken);
        if (offer == null || offer.Property.IsDeleted)
        {
            throw new NotFoundException($"Offer '{offerPublicId}' was not found.");
        }

        bool isOwner = offer.Property.OwnerId == callerInternalId;
        bool isSeeker = offer.SeekerId == callerInternalId;

        // IDOR safe: not-party gets 404
        if (!isOwner && !isSeeker)
        {
            throw new NotFoundException($"Offer '{offerPublicId}' was not found.");
        }

        return await MapToOfferDetailsDtoAsync(offer, callerInternalId, cancellationToken);
    }

    public async Task<PagedResult<OfferListItemDto>> GetSeekerOffersAsync(
        long seekerInternalId,
        OfferStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Offers.GetSeekerOffersAsync(seekerInternalId, status, page, pageSize, cancellationToken);
    }

    public async Task<PagedResult<OfferListItemDto>> GetOwnerIncomingOffersAsync(
        long ownerInternalId,
        OfferStatus? status,
        Guid? propertyPublicId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Offers.GetOwnerIncomingOffersAsync(ownerInternalId, status, propertyPublicId, page, pageSize, cancellationToken);
    }

    private async Task<OfferDetailsDto> MapToOfferDetailsDtoAsync(
        Offer offer,
        long callerInternalId,
        CancellationToken cancellationToken)
    {
        var histories = await _unitOfWork.NegotiationHistories.GetHistoryByOfferIdAsync(offer.Id, cancellationToken);

        bool isOwner = offer.Property.OwnerId == callerInternalId;
        string callerRole = isOwner ? "Owner" : "Seeker";

        var allowedActions = offer.GetAllowedActions(callerRole, offer.Property.Status, offer.Property.IsDeleted);

        var counterparty = isOwner
            ? new OfferCounterpartyDto(offer.Seeker.PublicId, offer.Seeker.FullName)
            : new OfferCounterpartyDto(offer.Property.Owner.PublicId, offer.Property.Owner.FullName);

        var historyDtos = histories.Select(h => new NegotiationHistoryItemDto(
            ActorFullName: h.ActionByUser?.FullName ?? "Unknown",
            ActorRole: h.ActionByUserId == offer.Property.OwnerId ? "Owner" : "Seeker",
            ActionType: h.ActionType,
            Price: h.ProposedPrice,
            RoundNumber: h.RoundNumber,
            CreatedAt: h.Timestamp
        )).ToList();

        var primaryImage = offer.Property.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault()
            ?? offer.Property.Images.Select(i => i.ImageUrl).FirstOrDefault();

        return new OfferDetailsDto(
            PublicId: offer.PublicId,
            Property: new OfferPropertySummaryDto(
                offer.Property.PublicId,
                offer.Property.Title,
                offer.Property.City,
                primaryImage,
                offer.Property.ListingType,
                offer.Property.Status
            ),
            Counterparty: counterparty,
            MyOfferPrice: offer.InitialPrice,
            LatestPrice: offer.CounterPrice ?? offer.InitialPrice,
            RoundNumber: offer.RoundNumber,
            Status: offer.Status,
            CreatedAt: offer.CreatedAt,
            UpdatedAt: offer.UpdatedAt,
            AllowedActions: allowedActions,
            History: historyDtos,
            RowVersion: Convert.ToBase64String(offer.RowVersion)
        );
    }
}
