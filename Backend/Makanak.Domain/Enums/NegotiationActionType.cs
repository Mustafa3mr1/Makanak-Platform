namespace Makanak.Domain.Enums;

public enum NegotiationActionType
{
    InitialOffer = 1,
    OwnerAccepted = 2,
    OwnerRejected = 3,
    OwnerCounterOffer = 4,
    BuyerAccepted = 5,
    BuyerRejected = 6,
    BuyerCounterOffer = 7,
    ClosedBecausePropertyUnavailable = 8
}
