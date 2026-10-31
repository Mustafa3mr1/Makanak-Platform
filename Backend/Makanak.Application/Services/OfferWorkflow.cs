using Makanak.Application.Common.Exceptions;
using Makanak.Domain.Entities;
using Makanak.Domain.Enums;

namespace Makanak.Application.Services;

public enum OfferParticipant { Owner, Seeker }

public static class OfferWorkflow
{
    public static bool IsOpen(OfferStatus status)
        => status is OfferStatus.PendingOwnerResponse or OfferStatus.PendingBuyerResponse;

    public static OfferParticipant? AwaitedParticipant(OfferStatus status) => status switch
    {
        OfferStatus.PendingOwnerResponse => OfferParticipant.Owner,
        OfferStatus.PendingBuyerResponse => OfferParticipant.Seeker,
        _ => null
    };

    public static string? AwaitingPartyLabel(OfferStatus status) => AwaitedParticipant(status)?.ToString();


    public static void EnsureCanRespond(Offer offer, OfferParticipant actor)
    {
        if (!IsOpen(offer.Status))
            throw new BusinessRuleException($"This offer is already {offer.Status} and can no longer be changed.");

        if (AwaitedParticipant(offer.Status) != actor)
            throw new ForbiddenException("It is not your turn to respond to this offer.");
    }

    public static OfferStatus StatusAfterCounter(OfferParticipant actor)
        => actor == OfferParticipant.Owner ? OfferStatus.PendingBuyerResponse : OfferStatus.PendingOwnerResponse;

    public static NegotiationActionType AcceptAction(OfferParticipant actor)
        => actor == OfferParticipant.Owner ? NegotiationActionType.OwnerAccepted : NegotiationActionType.BuyerAccepted;

    public static NegotiationActionType RejectAction(OfferParticipant actor)
        => actor == OfferParticipant.Owner ? NegotiationActionType.OwnerRejected : NegotiationActionType.BuyerRejected;

    public static NegotiationActionType CounterAction(OfferParticipant actor)
        => actor == OfferParticipant.Owner ? NegotiationActionType.OwnerCounterOffer : NegotiationActionType.BuyerCounterOffer;
}
