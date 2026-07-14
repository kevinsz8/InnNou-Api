using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

public class ArticlePricesEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/articlePrices").RequireAuthorization();

        group.MapPost("/create",     HandleCreate).Produces<ApiResponse<CreateArticlePriceCommandResponse>>(201);
        group.MapPost("/getCurrent", HandleGetCurrent).Produces<ApiResponse<GetCurrentArticlePriceQueryResponse>>(200);
        group.MapPost("/getHistory", HandleGetHistory).Produces<ApiResponse<GetArticlePriceHistoryQueryResponse>>(200);

        group.MapPost("/export",     HandleExport);
        group.MapPost("/bulkImport", HandleBulkImport)
            .Produces<ApiResponse<BulkImportArticlePricesCommandResponse>>(200)
            .DisableAntiforgery();
    }

    private static async Task<IResult> HandleCreate([FromBody] CreateArticlePriceCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Created("/articlePrices", result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetCurrent([FromBody] GetCurrentArticlePriceQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetHistory([FromBody] GetArticlePriceHistoryQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleExport([FromBody] ExportArticlePricesQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return Results.File(result.FileBytes, result.ContentType, result.FileName);
    }

    private static async Task<IResult> HandleBulkImport(HttpRequest httpRequest, ISender sender, CancellationToken ct)
    {
        // Kestrel disables synchronous I/O by default — the synchronous HttpRequest.Form
        // getter throws; ReadFormAsync is the async-safe way to bind multipart form data.
        var form = await httpRequest.ReadFormAsync(ct);
        var file = form.Files["file"];

        if (file is null || file.Length == 0)
        {
            var failure = ApiResponse<BulkImportArticlePricesCommandResponse>.FailureResponse(
                ErrorCodes.ArticlePriceBulkImportInvalidFile, "No file was uploaded.", 400);
            return Results.Json(failure, statusCode: 400);
        }

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, ct);

        var request = new BulkImportArticlePricesCommandRequest { FileBytes = memoryStream.ToArray(), FileName = file.FileName };
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }
}
