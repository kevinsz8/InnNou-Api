using InnNou.Domain.Dtos;

namespace InnNou.Application.Common.Interfaces
{
    // Real-time transport seam for Notifications — mirrors IEmailSender's own "Application
    // defines the contract, a concrete transport implements it" shape. Unlike IEmailSender
    // (implemented in Infrastructure, since SMTP has no dependency on ASP.NET Core hosting), the
    // real implementation of this one (SignalR's IHubContext<NotificationsHub>) can only be
    // resolved from the Presentation layer where the Hub itself is registered — so it's
    // implemented in InnNou.API, not Infrastructure. NotificationService only ever depends on
    // this abstraction, never on SignalR directly.
    public interface INotificationPusher
    {
        Task PushNotificationAsync(Guid userToken, NotificationDto notification, CancellationToken cancellationToken);
        Task PushUnreadCountAsync(Guid userToken, int unreadCount, CancellationToken cancellationToken);
    }
}
