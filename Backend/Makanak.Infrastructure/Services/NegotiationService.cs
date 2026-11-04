using Makanak.Application.Common.Interfaces;
using Makanak.Application.DTOs.Offers;
using Makanak.Domain.Entities;
using Makanak.Domain.Enums;
using Makanak.Domain.Exceptions;

namespace Makanak.Infrastructure.Services;

/// <summary>
/// Negotiation service orchestrating offer workflows with transactional consistency.
/// DbContext leak eliminated: all reads, writes, and audit logs go strictly through IUnitOfWork.
/// Works with Hybrid ID: resolves external PublicId (Guid) to internal Id (long) for joins.
/// </summary>
public sealed class NegotiationService : INegotiationService
{
    private readonly IUnitOfWork _unitOfWork;

    public NegotiationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OfferDto> CreateInitialOfferAsync(
        CreateOfferRequestDto request,
        long seekerInternalId,
        CancellationToken cancellationToken = default)
    {
        var property = await _unitOfWork.Properties.GetByPublicIdAsync(request.PropertyPublicId, cancellationToken)
            ?? throw new InvalidOfferOperationException($"Property with identifier '{request.PropertyPublicId}' was not found.");

        property.EnsureIsAvailable();

        var offer = Offer.Create(property.Id, seekerInternalId, request.InitialPrice);

        await _unitOfWork.Offers.AddAsync(offer, cancellationToken);

        // FR-09: Record submission in negotiation audit trail via IUnitOfWork (not direct DbContext)
        _unitOfWork.NegotiationHistories.Add(NegotiationHistory.Record(
            offer.Id,
            seekerInternalId,
            NegotiationActionType.Submitted,
            offer.CurrentPrice,
            offer.CreatedAtUtc));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var seekerUser = await _unitOfWork.Users.GetByIdAsync(seekerInternalId, cancellationToken);
        return MapToDto(offer, property.PublicId, seekerUser?.PublicId ?? Guid.Empty);
    }

    public async Task<OfferDto> CounterOfferAsync(
        Guid offerPublicId,
        CounterOfferRequestDto request,
        long actorInternalId,
        CancellationToken cancellationToken = default)
    {
        var offer = await _unitOfWork.Offers.GetByPublicIdWithPropertyAsync(offerPublicId, cancellationToken)
            ?? throw new InvalidOfferOperationException($"Offer '{offerPublicId}' was not found.");

        offer.Property.EnsureIsAvailable();

        // Enforces domain invariant (max 2 rounds)
        offer.Counter(request.CounterPrice);
        _unitOfWork.Offers.Update(offer);

        // FR-09: Record counter via IUnitOfWork
        _unitOfWork.NegotiationHistories.Add(NegotiationHistory.Record(
            offer.Id,
            actorInternalId,
            NegotiationActionType.Countered,
            offer.CurrentPrice,
            offer.UpdatedAtUtc));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var seekerUser = await _unitOfWork.Users.GetByIdAsync(offer.SeekerId, cancellationToken);
        return MapToDto(offer, offer.Property.PublicId, seekerUser?.PublicId ?? Guid.Empty);
    }

    public async Task<OfferDto> AcceptOfferAsync(
        Guid offerPublicId,
        long actorInternalId,
        CancellationToken cancellationToken = default)
    {
        Offer? acceptedOffer = null;
        Guid propertyPublicId = Guid.Empty;
        Guid seekerPublicId = Guid.Empty;

        // Atomic Unit of Work isolated transaction
        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            var offer = await _unitOfWork.Offers.GetByPublicIdWithPropertyAsync(offerPublicId, cancellationToken)
                ?? throw new InvalidOfferOperationException($"Offer '{offerPublicId}' was not found.");

            var property = offer.Property;
            property.EnsureIsAvailable();

            // 1. Mark target offer as Accepted
            offer.Accept();
            _unitOfWork.Offers.Update(offer);

            // 2. Mark property as Unavailable
            property.MarkAsUnavailable();
            _unitOfWork.Properties.Update(property);

            // 3. Reject competing pending/countered offers using internal clustered key joins
            var competingOffers = await _unitOfWork.Offers.GetPendingOffersByPropertyIdAsync(
                property.Id, offer.Id, cancellationToken);

            foreach (var competingOffer in competingOffers)
            {
                competingOffer.Reject();
                _unitOfWork.Offers.Update(competingOffer);

                // FR-09: Audit record for competing rejection
                _unitOfWork.NegotiationHistories.Add(NegotiationHistory.Record(
                    competingOffer.Id,
                    actorInternalId,
                    NegotiationActionType.Rejected,
                    competingOffer.CurrentPrice,
                    DateTime.UtcNow));
            }

            // FR-09: Audit record for the accepted offer
            _unitOfWork.NegotiationHistories.Add(NegotiationHistory.Record(
                offer.Id,
                actorInternalId,
                NegotiationActionType.Accepted,
                offer.CurrentPrice,
                offer.UpdatedAtUtc));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            acceptedOffer = offer;
            propertyPublicId = property.PublicId;
        }, cancellationToken);

        var seeker = await _unitOfWork.Users.GetByIdAsync(acceptedOffer!.SeekerId, cancellationToken);
        seekerPublicId = seeker?.PublicId ?? Guid.Empty;

        return MapToDto(acceptedOffer!, propertyPublicId, seekerPublicId);
    }

    public async Task<OfferDto> RejectOfferAsync(
        Guid offerPublicId,
        long actorInternalId,
        CancellationToken cancellationToken = default)
    {
        var offer = await _unitOfWork.Offers.GetByPublicIdWithPropertyAsync(offerPublicId, cancellationToken)
            ?? throw new InvalidOfferOperationException($"Offer '{offerPublicId}' was not found.");

        offer.Reject();
        _unitOfWork.Offers.Update(offer);

        // FR-09: Record rejection via IUnitOfWork
        _unitOfWork.NegotiationHistories.Add(NegotiationHistory.Record(
            offer.Id,
            actorInternalId,
            NegotiationActionType.Rejected,
            offer.CurrentPrice,
            offer.UpdatedAtUtc));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var seeker = await _unitOfWork.Users.GetByIdAsync(offer.SeekerId, cancellationToken);
        return MapToDto(offer, offer.Property.PublicId, seeker?.PublicId ?? Guid.Empty);
    }

    private static OfferDto MapToDto(Offer offer, Guid propertyPublicId, Guid seekerPublicId) => new(
        offer.PublicId,
        propertyPublicId,
        seekerPublicId,
        offer.CurrentPrice,
        offer.Status,
        offer.RoundCount,
        offer.CreatedAtUtc,
        offer.UpdatedAtUtc
    );
}
