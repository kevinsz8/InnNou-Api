using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

public class ArticlesEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/articles").RequireAuthorization();

        group.MapPost("/getAll",       HandleGetAll).Produces<ApiResponse<GetArticlesQueryResponse>>(200);
        group.MapPost("/getByToken",   HandleGetByToken).Produces<ApiResponse<GetArticleByTokenQueryResponse>>(200);
        group.MapPost("/create",       HandleCreate).Produces<ApiResponse<CreateArticleCommandResponse>>(201);
        group.MapPost("/edit",         HandleEdit).Produces<ApiResponse<EditArticleCommandResponse>>(200);
        group.MapPost("/supersede",    HandleSupersede).Produces<ApiResponse<SupersedeArticleCommandResponse>>(201);
        group.MapPost("/setActive",    HandleSetActive).Produces<ApiResponse<SetActiveArticleCommandResponse>>(200);
        group.MapPost("/delete",       HandleDelete).Produces<ApiResponse<DeleteArticleCommandResponse>>(200);

        group.MapPost("/export",                 HandleExport);
        group.MapPost("/downloadImportTemplate", HandleDownloadImportTemplate);
        group.MapPost("/bulkImport",             HandleBulkImport)
            .Produces<ApiResponse<BulkImportArticlesCommandResponse>>(200)
            .DisableAntiforgery();
    }

    private static async Task<IResult> HandleGetAll([FromBody] GetArticlesQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetByToken([FromBody] GetArticleByTokenQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleCreate([FromBody] CreateArticleCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Created("/articles", result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleEdit([FromBody] EditArticleCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleSupersede([FromBody] SupersedeArticleCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Created("/articles", result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleSetActive([FromBody] SetActiveArticleCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleDelete([FromBody] DeleteArticleCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleExport([FromBody] ExportArticlesQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return Results.File(result.FileBytes, result.ContentType, result.FileName);
    }

    private static async Task<IResult> HandleDownloadImportTemplate([FromBody] GetArticleImportTemplateQueryRequest request, ISender sender, CancellationToken ct)
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
            var failure = ApiResponse<BulkImportArticlesCommandResponse>.FailureResponse(
                ErrorCodes.ArticleBulkImportInvalidFile, "No file was uploaded.", 400);
            return Results.Json(failure, statusCode: 400);
        }

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, ct);

        var request = new BulkImportArticlesCommandRequest { FileBytes = memoryStream.ToArray(), FileName = file.FileName };
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }
}
