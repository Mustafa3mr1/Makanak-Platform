using Microsoft.AspNetCore.SignalR;
using Makanak.Application.Common.Interfaces;

namespace Makanak.Infrastructure.Hubs;

/// <summary>
/// Real-time SignalR Hub pushing negotiation updates and alerts to Property Owners and Seekers.
/// </summary>
public class NotificationHub : Hub<INotificationClient>
{
    public async Task JoinPropertyNegotiationGroup(string propertyId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Property_{propertyId}");
    }

    public async Task LeavePropertyNegotiationGroup(string propertyId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Property_{propertyId}");
    }
}
