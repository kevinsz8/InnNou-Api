using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

public class CategoryEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/categories").RequireAuthorization();

        group.MapPost("/getAll", HandleGetAll).Produces<ApiResponse<GetCategoriesQueryResponse>>(200);
        group.MapPost("/getByToken", HandleGetByToken).Produces<ApiResponse<GetCategoryByTokenQueryResponse>>(200);
        group.MapPost("/create", HandleCreate).Produces<ApiResponse<CreateCategoryCommandResponse>>(201);
        group.MapPost("/edit", HandleEdit).Produces<ApiResponse<EditCategoryCommandResponse>>(200);
        group.MapPost("/setActive", HandleSetActive).Produces<ApiResponse<SetActiveCategoryCommandResponse>>(200);

        group.MapPost("/export",                 HandleExport);
        group.MapPost("/downloadImportTemplate", HandleDownloadImportTemplate);
        group.MapPost("/bulkImport",             HandleBulkImport)
            .Produces<ApiResponse<BulkImportCategoriesCommandResponse>>(200)
            .DisableAntiforgery();
    }

    private static async Task<IResult> HandleGetAll([FromBody] GetCategoriesQueryRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetByToken([FromBody] GetCategoryByTokenQueryRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleCreate([FromBody] CreateCategoryCommandRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        return result.Success ? Results.Created("/categories", result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleEdit([FromBody] EditCategoryCommandRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleSetActive([FromBody] SetActiveCategoryCommandRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleExport([FromBody] ExportCategoriesQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return Results.File(result.FileBytes, result.ContentType, result.FileName);
    }

    private static async Task<IResult> HandleDownloadImportTemplate([FromBody] GetCategoryImportTemplateQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return Results.File(result.FileBytes, result.ContentType, result.FileName);
    }

    private static async Task<IResult> HandleBulkImport(HttpRequest httpRequest, ISender sender, CancellationToken ct)
    {
        var form = await httpRequest.ReadFormAsync(ct);
        var file = form.Files["file"];

        if (file is null || file.Length == 0)
        {
            var failure = ApiResponse<BulkImportCategoriesCommandResponse>.FailureResponse(
                ErrorCodes.CategoryBulkImportInvalidFile, "No file was uploaded.", 400);
            return Results.Json(failure, statusCode: 400);
        }

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, ct);

        var request = new BulkImportCategoriesCommandRequest { FileBytes = memoryStream.ToArray(), FileName = file.FileName };
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }
}
