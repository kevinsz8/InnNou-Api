using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace InnNou.API.Hubs;

// The only concrete implementation of INotificationPusher — lives here (not Infrastructure)
// because IHubContext<NotificationsHub> can only be resolved where the Hub itself is registered
// (the Presentation layer). See INotificationPusher's own comment for the full rationale.
public class SignalRNotificationPusher(IHubContext<NotificationsHub> hubContext) : INotificationPusher
{
    public Task PushNotificationAsync(Guid userToken, NotificationDto notification, CancellationToken cancellationToken) =>
        hubContext.Clients.Group(NotificationsHub.GroupNameForUser(userToken))
            .SendAsync("ReceiveNotification", notification, cancellationToken);

    public Task PushUnreadCountAsync(Guid userToken, int unreadCount, CancellationToken cancellationToken) =>
        hubContext.Clients.Group(NotificationsHub.GroupNameForUser(userToken))
            .SendAsync("UnreadCountChanged", unreadCount, cancellationToken);
}
