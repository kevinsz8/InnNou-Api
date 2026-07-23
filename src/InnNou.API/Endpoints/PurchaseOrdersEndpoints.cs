using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

public class PurchaseOrdersEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/purchaseOrders").RequireAuthorization();

        group.MapPost("/getAll",     HandleGetAll).Produces<ApiResponse<GetPurchaseOrdersQueryResponse>>(200);
        group.MapPost("/getByToken", HandleGetByToken).Produces<ApiResponse<GetPurchaseOrderByTokenQueryResponse>>(200);
        group.MapPost("/cancel",     HandleCancel).Produces<ApiResponse<CancelPurchaseOrderCommandResponse>>(200);

        // Rectifications ("rectificacion de pedido") — see .claude/PurchaseOrderRectificationModule.md.
        // Approve/reject reuse the existing /orders/approveStep and /orders/rejectStep endpoints —
        // a rectification-triggered OrderApprovalStep is decided through the same unified flow an
        // approver already uses for a regular Order submission (see OrderService).
        group.MapPost("/rectify",            HandleRectify).Produces<ApiResponse<CreatePurchaseOrderRectificationCommandResponse>>(201);
        group.MapPost("/getRectifications",  HandleGetRectifications).Produces<ApiResponse<GetPurchaseOrderRectificationsQueryResponse>>(200);
        group.MapPost("/getRectificationByToken", HandleGetRectificationByToken).Produces<ApiResponse<GetPurchaseOrderRectificationByTokenQueryResponse>>(200);
    }

    private static async Task<IResult> HandleGetAll([FromBody] GetPurchaseOrdersQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetByToken([FromBody] GetPurchaseOrderByTokenQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleCancel([FromBody] CancelPurchaseOrderCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleRectify([FromBody] CreatePurchaseOrderRectificationCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Json(result, statusCode: result.StatusCode ?? 201) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetRectifications([FromBody] GetPurchaseOrderRectificationsQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetRectificationByToken([FromBody] GetPurchaseOrderRectificationByTokenQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }
}
