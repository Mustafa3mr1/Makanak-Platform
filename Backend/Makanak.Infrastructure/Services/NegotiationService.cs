using Makanak.Application.Common.Interfaces;
using Makanak.Application.DTOs.Offers;
using Makanak.Domain.Entities;
using Makanak.Domain.Enums;
using Makanak.Domain.Exceptions;

namespace Makanak.Infrastructure.Services;

/// <summary>
/// Negotiation service orchestrating offer workflows with transactional consistency and SignalR event streaming.
/// </summary>
public class NegotiationService : INegotiationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISignalRNotificationService _notificationService;

    public NegotiationService(IUnitOfWork unitOfWork, ISignalRNotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Real-time dispatch
        await _notificationService.NotifyOfferCreatedAsync(new OfferNotificationDto(
            offer.Id,
            offer.PropertyId,
            offer.SeekerId,
            offer.CurrentPrice,
            offer.Status,
            offer.RoundCount,
            "A new offer has been submitted for this property.",
            offer.CreatedAtUtc
        ), cancellationToken);

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

        // Enforces invariant: max 2 rounds
        offer.Counter(request.CounterPrice);

        _unitOfWork.Offers.Update(offer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Real-time dispatch
        await _notificationService.NotifyOfferCounteredAsync(new OfferNotificationDto(
            offer.Id,
            offer.PropertyId,
            offer.SeekerId,
            offer.CurrentPrice,
            offer.Status,
            offer.RoundCount,
            $"Counter offer of {offer.CurrentPrice:C} submitted (Round {offer.RoundCount}/2).",
            offer.UpdatedAtUtc ?? DateTime.UtcNow
        ), cancellationToken);

        return MapToDto(offer);
    }

    public async Task<OfferDto> AcceptOfferAsync(
        Guid offerId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        Offer? acceptedOffer = null;
        Guid targetPropertyId = Guid.Empty;

        // Atomic Unit of Work isolated transaction
        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            var offer = await _unitOfWork.Offers.GetByIdWithPropertyAsync(offerId, cancellationToken)
                ?? throw new InvalidOfferOperationException($"Offer '{offerId}' was not found.");

            var property = offer.Property;
            property.EnsureIsAvailable();

            // 1. Mark target offer as accepted
            offer.Accept();
            _unitOfWork.Offers.Update(offer);

            // 2. Mark property as unavailable
            property.MarkAsUnavailable();
            _unitOfWork.Properties.Update(property);

            // 3. Concurrently reject all competing pending/countered offers for this specific property
            var competingOffers = await _unitOfWork.Offers.GetPendingOffersByPropertyIdAsync(
                property.Id,
                offer.Id,
                cancellationToken);

            foreach (var competingOffer in competingOffers)
            {
                competingOffer.Reject();
                _unitOfWork.Offers.Update(competingOffer);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            acceptedOffer = offer;
            targetPropertyId = property.Id;
        }, cancellationToken);

        // Post-commit SignalR event notifications
        if (acceptedOffer != null)
        {
            await _notificationService.NotifyOfferAcceptedAsync(
                targetPropertyId,
                acceptedOffer.Id,
                "Offer accepted. Property is now unavailable for further offers.",
                cancellationToken);
        }

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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationService.NotifyOfferRejectedAsync(
            offer.PropertyId,
            offer.Id,
            "Offer was rejected.",
            cancellationToken);

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
