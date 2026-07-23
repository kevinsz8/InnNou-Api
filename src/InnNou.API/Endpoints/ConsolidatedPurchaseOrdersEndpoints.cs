using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

// Multi-property spend consolidation — a SUPER_ASSOCIATE-only tool, see
// .claude/ConsolidatedPurchaseOrderModule.md. Visibility/authorization is enforced entirely
// inside ConsolidatedPurchaseOrderService, same as every other endpoint group in this API.
public class ConsolidatedPurchaseOrdersEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/consolidatedPurchaseOrders").RequireAuthorization();

        group.MapPost("/getCandidates", HandleGetCandidates).Produces<ApiResponse<GetConsolidatedPurchaseOrderCandidatesQueryResponse>>(200);
        group.MapPost("/create", HandleCreate).Produces<ApiResponse<CreateConsolidatedPurchaseOrderCommandResponse>>(201);
        group.MapPost("/getAll", HandleGetAll).Produces<ApiResponse<GetConsolidatedPurchaseOrdersQueryResponse>>(200);
        group.MapPost("/getByToken", HandleGetByToken).Produces<ApiResponse<GetConsolidatedPurchaseOrderByTokenQueryResponse>>(200);
        group.MapPost("/delete", HandleDelete).Produces<ApiResponse<DeleteConsolidatedPurchaseOrderCommandResponse>>(200);
        group.MapPost("/downloadPdf", HandleDownloadPdf);
    }

    private static async Task<IResult> HandleGetCandidates([FromBody] GetConsolidatedPurchaseOrderCandidatesQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleCreate([FromBody] CreateConsolidatedPurchaseOrderCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Json(result, statusCode: result.StatusCode ?? 201) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetAll([FromBody] GetConsolidatedPurchaseOrdersQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetByToken([FromBody] GetConsolidatedPurchaseOrderByTokenQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleDelete([FromBody] DeleteConsolidatedPurchaseOrderCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleDownloadPdf([FromBody] DownloadConsolidatedPurchaseOrderPdfQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return Results.File(result.FileBytes, result.ContentType, result.FileName);
    }
}
