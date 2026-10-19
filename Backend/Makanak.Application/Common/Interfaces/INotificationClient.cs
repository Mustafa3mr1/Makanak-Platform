using Makanak.Application.DTOs.Offers;

namespace Makanak.Application.Common.Interfaces;

/// <summary>
/// Strongly-typed client contract for SignalR NotificationHub.
/// </summary>
public interface INotificationClient
{
    Task ReceiveOfferUpdate(OfferNotificationDto notification);

    Task ReceiveOfferAccepted(Guid propertyId, Guid offerId, string message);

    Task ReceiveOfferRejected(Guid propertyId, Guid offerId, string message);

    Task ReceiveCounterOffer(OfferNotificationDto notification);
}
