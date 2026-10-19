using Makanak.Application.DTOs.Offers;

namespace Makanak.Application.Common.Interfaces;

public interface ISignalRNotificationService
{
    Task NotifyOfferCreatedAsync(OfferNotificationDto notification, CancellationToken cancellationToken = default);
    Task NotifyOfferCounteredAsync(OfferNotificationDto notification, CancellationToken cancellationToken = default);
    Task NotifyOfferAcceptedAsync(Guid propertyId, Guid offerId, string message, CancellationToken cancellationToken = default);
    Task NotifyOfferRejectedAsync(Guid propertyId, Guid offerId, string message, CancellationToken cancellationToken = default);
}
