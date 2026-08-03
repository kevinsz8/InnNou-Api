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

        // "Caso B" — closes a PARTIALLY_RECEIVED PurchaseOrder the buyer has stopped chasing,
        // without touching PurchaseOrderLine/Quantity. See IPurchaseOrderService.CloseShortAsync.
        group.MapPost("/closeShort", HandleCloseShort).Produces<ApiResponse<CloseShortPurchaseOrderCommandResponse>>(200);

        // Rectifications ("rectificacion de pedido") — see .claude/PurchaseOrderRectificationModule.md.
        // Approve/reject reuse the existing /orders/approveStep and /orders/rejectStep endpoints —
        // a rectification-triggered OrderApprovalStep is decided through the same unified flow an
        // approver already uses for a regular Order submission (see OrderService).
        group.MapPost("/rectify",            HandleRectify).Produces<ApiResponse<CreatePurchaseOrderRectificationCommandResponse>>(201);
        group.MapPost("/getRectifications",  HandleGetRectifications).Produces<ApiResponse<GetPurchaseOrderRectificationsQueryResponse>>(200);

        // Goods Receipts ("recepcion de mercaderia") — see .claude/GoodsReceiptsModule.md.
        group.MapPost("/receiveGoods",           HandleReceiveGoods).Produces<ApiResponse<CreateGoodsReceiptCommandResponse>>(201);
        group.MapPost("/getGoodsReceipts",       HandleGetGoodsReceipts).Produces<ApiResponse<GetGoodsReceiptsQueryResponse>>(200);

        // Read-only preview of every eligible line's effective tax category/rate, before the
        // receipt is actually submitted — lets the receiving page show %IVA and a net+gross
        // total live. Never throws on unconfigured tax data (unlike /receiveGoods itself).
        group.MapPost("/getGoodsReceiptTaxPreview", HandleGetGoodsReceiptTaxPreview).Produces<ApiResponse<GetGoodsReceiptTaxPreviewQueryResponse>>(200);

        // Standalone "Recepciones" history/search page — every GoodsReceipt across an
        // organization's purchase orders, not scoped to one PurchaseOrder. See
        // .claude/GoodsReceiptsModule.md's "Recepciones page" section.
        group.MapPost("/getGoodsReceiptsPaged",  HandleGetGoodsReceiptsPaged).Produces<ApiResponse<GetGoodsReceiptsPagedQueryResponse>>(200);
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

    private static async Task<IResult> HandleCloseShort([FromBody] CloseShortPurchaseOrderCommandRequest request, ISender sender, CancellationToken ct)
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

    private static async Task<IResult> HandleReceiveGoods([FromBody] CreateGoodsReceiptCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Json(result, statusCode: result.StatusCode ?? 201) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetGoodsReceipts([FromBody] GetGoodsReceiptsQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetGoodsReceiptsPaged([FromBody] GetGoodsReceiptsPagedQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetGoodsReceiptTaxPreview([FromBody] GetGoodsReceiptTaxPreviewQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

}
