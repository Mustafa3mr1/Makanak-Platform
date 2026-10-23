using Makanak.Application.Common.Interfaces;
using Makanak.Application.DTOs.Offers;
using Makanak.Domain.Entities;
using Makanak.Domain.Enums;
using Makanak.Domain.Exceptions;
using Makanak.Infrastructure.Data;

namespace Makanak.Infrastructure.Services;

/// <summary>
/// Negotiation service orchestrating offer workflows with transactional consistency.
/// Every state transition appends an immutable NegotiationHistory record (FR-09).
/// SignalR removed from MVP scope.
/// </summary>
public class NegotiationService : INegotiationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;

    public NegotiationService(IUnitOfWork unitOfWork, ApplicationDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<OfferDto> CreateInitialOfferAsync(
        CreateOfferRequestDto request,
        Guid seekerId,
        CancellationToken cancellationToken = default)
    {
        var property = await _unitOfWork.Properties.GetByIdAsync(request.PropertyId, cancellationToken)
            ?? throw new InvalidOfferOperationException($"Property '{request.PropertyId}' was not found.");

        property.EnsureIsAvailable();

        var offer = new Offer
        {
            PropertyId = request.PropertyId,
            SeekerId = seekerId,
            CurrentPrice = request.InitialPrice,
            Status = OfferStatus.Pending,
            RoundCount = 0
        };

        await _unitOfWork.Offers.AddAsync(offer, cancellationToken);

        // FR-09: Record the initial submission in the negotiation audit trail
        _context.NegotiationHistories.Add(new NegotiationHistory
        {
            OfferId = offer.Id,
            ActionByUserId = seekerId,
            ActionType = NegotiationActionType.Submitted,
            ProposedPrice = offer.CurrentPrice,
            Timestamp = offer.CreatedAtUtc
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(offer);
    }

    public async Task<OfferDto> CounterOfferAsync(
        Guid offerId,
        CounterOfferRequestDto request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var offer = await _unitOfWork.Offers.GetByIdWithPropertyAsync(offerId, cancellationToken)
            ?? throw new InvalidOfferOperationException($"Offer '{offerId}' was not found.");

        offer.Property.EnsureIsAvailable();

        // Enforces domain invariant: max 2 rounds
        offer.Counter(request.CounterPrice);

        _unitOfWork.Offers.Update(offer);

        // FR-09: Record the counter action
        _context.NegotiationHistories.Add(new NegotiationHistory
        {
            OfferId = offer.Id,
            ActionByUserId = actorId,
            ActionType = NegotiationActionType.Countered,
            ProposedPrice = offer.CurrentPrice,
            Timestamp = offer.UpdatedAtUtc ?? DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(offer);
    }

    public async Task<OfferDto> AcceptOfferAsync(
        Guid offerId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        Offer? acceptedOffer = null;

        // Atomic Unit of Work isolated transaction
        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            var offer = await _unitOfWork.Offers.GetByIdWithPropertyAsync(offerId, cancellationToken)
                ?? throw new InvalidOfferOperationException($"Offer '{offerId}' was not found.");

            var property = offer.Property;
            property.EnsureIsAvailable();

            // 1. Mark target offer as Accepted
            offer.Accept();
            _unitOfWork.Offers.Update(offer);

            // 2. Mark property as Unavailable
            property.MarkAsUnavailable();
            _unitOfWork.Properties.Update(property);

            // 3. Reject all competing pending/countered offers for this property
            var competingOffers = await _unitOfWork.Offers.GetPendingOffersByPropertyIdAsync(
                property.Id, offer.Id, cancellationToken);

            foreach (var competingOffer in competingOffers)
            {
                competingOffer.Reject();
                _unitOfWork.Offers.Update(competingOffer);

                // FR-09: History entry for each auto-rejected competing offer
                _context.NegotiationHistories.Add(new NegotiationHistory
                {
                    OfferId = competingOffer.Id,
                    ActionByUserId = actorId,
                    ActionType = NegotiationActionType.Rejected,
                    ProposedPrice = competingOffer.CurrentPrice,
                    Timestamp = DateTime.UtcNow
                });
            }

            // FR-09: History entry for the accepted offer
            _context.NegotiationHistories.Add(new NegotiationHistory
            {
                OfferId = offer.Id,
                ActionByUserId = actorId,
                ActionType = NegotiationActionType.Accepted,
                ProposedPrice = offer.CurrentPrice,
                Timestamp = offer.UpdatedAtUtc ?? DateTime.UtcNow
            });

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            acceptedOffer = offer;
        }, cancellationToken);

        return MapToDto(acceptedOffer!);
    }

    public async Task<OfferDto> RejectOfferAsync(
        Guid offerId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var offer = await _unitOfWork.Offers.GetByIdAsync(offerId, cancellationToken)
            ?? throw new InvalidOfferOperationException($"Offer '{offerId}' was not found.");

        offer.Reject();
        _unitOfWork.Offers.Update(offer);

        // FR-09: Record the rejection
        _context.NegotiationHistories.Add(new NegotiationHistory
        {
            OfferId = offer.Id,
            ActionByUserId = actorId,
            ActionType = NegotiationActionType.Rejected,
            ProposedPrice = offer.CurrentPrice,
            Timestamp = offer.UpdatedAtUtc ?? DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(offer);
    }

    private static OfferDto MapToDto(Offer offer) => new(
        offer.Id,
        offer.PropertyId,
        offer.SeekerId,
        offer.CurrentPrice,
        offer.Status,
        offer.RoundCount,
        offer.CreatedAtUtc,
        offer.UpdatedAtUtc
    );
}
