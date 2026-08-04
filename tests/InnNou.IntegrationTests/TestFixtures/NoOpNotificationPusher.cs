using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;

namespace InnNou.IntegrationTests.TestFixtures;

/// <summary>
/// Test double for <see cref="INotificationPusher"/> — the real implementation
/// (<c>SignalRNotificationPusher</c>) lives in <c>InnNou.API</c> and depends on
/// <c>IHubContext&lt;NotificationsHub&gt;</c>, which only resolves inside a real SignalR host.
/// This test project builds its DI container from <c>AddInfrastructure()</c>/<c>AddApplication()</c>
/// alone (see <see cref="DatabaseFixture"/>), so <c>NotificationService</c> (which takes
/// <see cref="INotificationPusher"/> in its constructor) has nothing to resolve without this —
/// the DB write path (<c>Notifications</c> table) is still exercised for real; only the live push
/// is a no-op here, which is exactly what these tests need.
/// </summary>
public class NoOpNotificationPusher : INotificationPusher
{
    public Task PushNotificationAsync(Guid userToken, NotificationDto notification, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task PushUnreadCountAsync(Guid userToken, int unreadCount, CancellationToken cancellationToken) => Task.CompletedTask;
}
