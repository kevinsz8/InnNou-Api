using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

public class InventoryPeriodEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/inventoryPeriods").RequireAuthorization();

        group.MapPost("/open",             HandleOpen).Produces<ApiResponse<OpenInventoryPeriodCommandResponse>>(201);
        group.MapPost("/submitCount",      HandleSubmitCount).Produces<ApiResponse<SubmitInventoryPeriodCountCommandResponse>>(200);
        group.MapPost("/close",            HandleClose).Produces<ApiResponse<CloseInventoryPeriodCommandResponse>>(200);
        group.MapPost("/reopen",           HandleReopen).Produces<ApiResponse<ReopenInventoryPeriodCommandResponse>>(200);
        group.MapPost("/getPeriods",       HandleGetPeriods).Produces<ApiResponse<GetInventoryPeriodsQueryResponse>>(200);
        group.MapPost("/getPeriodByToken", HandleGetPeriodByToken).Produces<ApiResponse<GetInventoryPeriodByTokenQueryResponse>>(200);
    }

    private static async Task<IResult> HandleOpen([FromBody] OpenInventoryPeriodCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Json(result, statusCode: result.StatusCode ?? 201) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleSubmitCount([FromBody] SubmitInventoryPeriodCountCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleClose([FromBody] CloseInventoryPeriodCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleReopen([FromBody] ReopenInventoryPeriodCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetPeriods([FromBody] GetInventoryPeriodsQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetPeriodByToken([FromBody] GetInventoryPeriodByTokenQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }
}
