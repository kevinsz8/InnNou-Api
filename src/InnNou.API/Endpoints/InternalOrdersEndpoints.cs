using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

public class InternalOrdersEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/internalOrders").RequireAuthorization();

        group.MapPost("/create",           HandleCreate).Produces<ApiResponse<CreateInternalOrderCommandResponse>>(201);
        group.MapPost("/getByToken",       HandleGetByToken).Produces<ApiResponse<GetInternalOrderByTokenQueryResponse>>(200);
        group.MapPost("/getAll",           HandleGetAll).Produces<ApiResponse<GetInternalOrdersQueryResponse>>(200);
        group.MapPost("/cancel",           HandleCancel).Produces<ApiResponse<CancelInternalOrderCommandResponse>>(200);
        group.MapPost("/createShipment",   HandleCreateShipment).Produces<ApiResponse<CreateInternalOrderShipmentCommandResponse>>(201);
        group.MapPost("/createReceipt",    HandleCreateReceipt).Produces<ApiResponse<CreateInternalOrderReceiptCommandResponse>>(201);
        group.MapPost("/getEligibleSourceOrganizations", HandleGetEligibleSourceOrganizations).Produces<ApiResponse<GetInternalOrderEligibleSourceOrganizationsQueryResponse>>(200);
    }

    private static async Task<IResult> HandleCreate([FromBody] CreateInternalOrderCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Json(result, statusCode: result.StatusCode ?? 201) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetByToken([FromBody] GetInternalOrderByTokenQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetAll([FromBody] GetInternalOrdersQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleCancel([FromBody] CancelInternalOrderCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleCreateShipment([FromBody] CreateInternalOrderShipmentCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Json(result, statusCode: result.StatusCode ?? 201) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleCreateReceipt([FromBody] CreateInternalOrderReceiptCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Json(result, statusCode: result.StatusCode ?? 201) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetEligibleSourceOrganizations([FromBody] GetInternalOrderEligibleSourceOrganizationsQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }
}
