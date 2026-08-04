using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

public class NotificationsEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/notifications").RequireAuthorization();

        group.MapPost("/getAll",           HandleGetAll).Produces<ApiResponse<GetNotificationsQueryResponse>>(200);
        group.MapPost("/getUnreadCount",   HandleGetUnreadCount).Produces<ApiResponse<GetUnreadNotificationCountQueryResponse>>(200);
        group.MapPost("/markAsRead",       HandleMarkAsRead).Produces<ApiResponse<MarkNotificationAsReadCommandResponse>>(200);
        group.MapPost("/markAllAsRead",    HandleMarkAllAsRead).Produces<ApiResponse<MarkAllNotificationsAsReadCommandResponse>>(200);
    }

    private static async Task<IResult> HandleGetAll([FromBody] GetNotificationsQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetUnreadCount([FromBody] GetUnreadNotificationCountQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleMarkAsRead([FromBody] MarkNotificationAsReadCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleMarkAllAsRead([FromBody] MarkAllNotificationsAsReadCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }
}
