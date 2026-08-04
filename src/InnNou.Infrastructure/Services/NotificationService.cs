using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using System.Data;
using System.Text.Json;

namespace InnNou.Infrastructure.Services;

public class NotificationService(IDbConnectionFactory connectionFactory, IMapper mapper, INotificationPusher pusher) : INotificationService
{
    private sealed class NotificationPageRow : Notification { public int TotalCount { get; set; } }

    private const int MaxPageSize = 50;

    public async Task NotifyAsync(Guid userToken, NotificationType type, object data, string? linkUrl, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var user = await connection.QueryFirstOrDefaultAsync<User>(
            "sp_User_GetByToken", new { UserToken = userToken }, commandType: CommandType.StoredProcedure);
        if (user is null)
            return;

        var dataJson = JsonSerializer.Serialize(data);

        var header = await connection.QueryFirstOrDefaultAsync<Notification>(
            "sp_Notification_Create",
            new
            {
                NotificationToken = Guid.NewGuid(),
                user.UserId,
                Type = NotificationTypeCodes.ToCode(type),
                DataJson = dataJson,
                LinkUrl = linkUrl,
                CreatedBy = context.ActorUserToken.ToString()
            },
            commandType: CommandType.StoredProcedure);

        if (header is null)
            return;

        var dto = mapper.Map<NotificationDto>(header);

        // Best-effort push — a SignalR outage must never fail the DB write above (the row is
        // still there for the recipient's next GetPaged/reconnect either way).
        try
        {
            await pusher.PushNotificationAsync(userToken, dto, cancellationToken);

            var unreadCount = await connection.ExecuteScalarAsync<int>(
                "sp_Notification_GetUnreadCount", new { user.UserId }, commandType: CommandType.StoredProcedure);
            await pusher.PushUnreadCountAsync(userToken, unreadCount, cancellationToken);
        }
        catch
        {
            // Swallowed deliberately — the caller's own try/catch around NotifyAsync already
            // exists for the DB-write path; a push failure specifically is never worth surfacing
            // to the triggering action (same as an SMTP outage).
        }
    }

    public async Task<PagedResult<NotificationDto>> GetPagedAsync(bool unreadOnly, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 20 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();

        var user = await connection.QueryFirstOrDefaultAsync<User>(
            "sp_User_GetByToken", new { UserToken = context.EffectiveUserToken }, commandType: CommandType.StoredProcedure);
        if (user is null)
            return new PagedResult<NotificationDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

        var rows = (await connection.QueryAsync<NotificationPageRow>(
            "sp_Notification_GetPaged",
            new { user.UserId, UnreadOnly = unreadOnly, PageNumber = safePageNumber, PageSize = safePageSize },
            commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<NotificationDto>
        {
            Items = mapper.MapList<NotificationDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<int> GetUnreadCountAsync(IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var user = await connection.QueryFirstOrDefaultAsync<User>(
            "sp_User_GetByToken", new { UserToken = context.EffectiveUserToken }, commandType: CommandType.StoredProcedure);
        if (user is null)
            return 0;

        return await connection.ExecuteScalarAsync<int>(
            "sp_Notification_GetUnreadCount", new { user.UserId }, commandType: CommandType.StoredProcedure);
    }

    public async Task MarkAsReadAsync(Guid notificationToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var user = await connection.QueryFirstOrDefaultAsync<User>(
            "sp_User_GetByToken", new { UserToken = context.EffectiveUserToken }, commandType: CommandType.StoredProcedure);
        if (user is null)
            return;

        await connection.ExecuteAsync(
            "sp_Notification_MarkAsRead", new { NotificationToken = notificationToken, user.UserId }, commandType: CommandType.StoredProcedure);
    }

    public async Task MarkAllAsReadAsync(IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var user = await connection.QueryFirstOrDefaultAsync<User>(
            "sp_User_GetByToken", new { UserToken = context.EffectiveUserToken }, commandType: CommandType.StoredProcedure);
        if (user is null)
            return;

        await connection.ExecuteAsync(
            "sp_Notification_MarkAllAsRead", new { user.UserId }, commandType: CommandType.StoredProcedure);
    }
}
