using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

public class InventoryEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/inventory").RequireAuthorization();

        group.MapPost("/createAdjustment",     HandleCreateAdjustment).Produces<ApiResponse<CreateInventoryAdjustmentCommandResponse>>(201);
        group.MapPost("/createTransfer",       HandleCreateTransfer).Produces<ApiResponse<CreateInventoryTransferCommandResponse>>(201);
        group.MapPost("/getStockLevels",       HandleGetStockLevels).Produces<ApiResponse<GetStockLevelsQueryResponse>>(200);
        group.MapPost("/getMovements",         HandleGetMovements).Produces<ApiResponse<GetInventoryMovementsQueryResponse>>(200);
        group.MapPost("/getTransfers",         HandleGetTransfers).Produces<ApiResponse<GetInventoryTransfersQueryResponse>>(200);
        group.MapPost("/getTransferByToken",   HandleGetTransferByToken).Produces<ApiResponse<GetInventoryTransferByTokenQueryResponse>>(200);
    }

    private static async Task<IResult> HandleCreateAdjustment([FromBody] CreateInventoryAdjustmentCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Json(result, statusCode: result.StatusCode ?? 201) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleCreateTransfer([FromBody] CreateInventoryTransferCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Json(result, statusCode: result.StatusCode ?? 201) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetStockLevels([FromBody] GetStockLevelsQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetMovements([FromBody] GetInventoryMovementsQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetTransfers([FromBody] GetInventoryTransfersQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetTransferByToken([FromBody] GetInventoryTransferByTokenQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }
}
