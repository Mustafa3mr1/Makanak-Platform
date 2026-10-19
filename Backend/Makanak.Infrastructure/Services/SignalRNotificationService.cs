using Microsoft.AspNetCore.SignalR;
using Makanak.Application.Common.Interfaces;
using Makanak.Application.DTOs.Offers;
using Makanak.Infrastructure.Hubs;

namespace Makanak.Infrastructure.Services;

public class SignalRNotificationService : ISignalRNotificationService
{
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;

    public SignalRNotificationService(IHubContext<NotificationHub, INotificationClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyOfferCreatedAsync(OfferNotificationDto notification, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"Property_{notification.PropertyId}")
            .ReceiveOfferUpdate(notification);
    }

    public async Task NotifyOfferCounteredAsync(OfferNotificationDto notification, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"Property_{notification.PropertyId}")
            .ReceiveCounterOffer(notification);
    }

    public async Task NotifyOfferAcceptedAsync(Guid propertyId, Guid offerId, string message, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"Property_{propertyId}")
            .ReceiveOfferAccepted(propertyId, offerId, message);
    }

    public async Task NotifyOfferRejectedAsync(Guid propertyId, Guid offerId, string message, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"Property_{propertyId}")
            .ReceiveOfferRejected(propertyId, offerId, message);
    }
}
