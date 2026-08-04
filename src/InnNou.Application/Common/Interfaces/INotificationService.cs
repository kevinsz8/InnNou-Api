using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    // In-app notifications, pushed in real time over SignalR (see INotificationPusher) and
    // backed by the Notifications table as the source of truth (so a client that was offline
    // when a push fired still sees it on next login/reconnect). Deliberately never resolves
    // display text server-side — DataJson carries only raw interpolation params, rendered by the
    // frontend via i18next, same "never fix one language server-side" convention as
    // CatalogTranslations/GoodsReceipt tax preview.
    public interface INotificationService
    {
        // userToken is the RECIPIENT's own UserToken — always resolved by the caller from a
        // concrete source (e.g. OrderApprovalStep.ApproverUserToken), never context.EffectiveUserToken
        // (the caller is virtually always notifying someone OTHER than themselves). data is
        // serialized to JSON internally. Best-effort is the CALLER's responsibility (same
        // try/catch convention as every IEmailSender call site) — this method itself still throws
        // on a genuine failure so the caller's existing catch block covers both email and
        // notification failures uniformly.
        Task NotifyAsync(Guid userToken, NotificationType type, object data, string? linkUrl, IRequestContext context, CancellationToken cancellationToken);

        Task<PagedResult<NotificationDto>> GetPagedAsync(bool unreadOnly, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken);
        Task<int> GetUnreadCountAsync(IRequestContext context, CancellationToken cancellationToken);
        Task MarkAsReadAsync(Guid notificationToken, IRequestContext context, CancellationToken cancellationToken);
        Task MarkAllAsReadAsync(IRequestContext context, CancellationToken cancellationToken);
    }
}
