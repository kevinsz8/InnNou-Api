using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

public class SubFamilyEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/sub-families").RequireAuthorization();

        group.MapPost("/getAll", HandleGetAll).Produces<ApiResponse<GetSubFamiliesQueryResponse>>(200);
        group.MapPost("/getByToken", HandleGetByToken).Produces<ApiResponse<GetSubFamilyByTokenQueryResponse>>(200);
        group.MapPost("/create", HandleCreate).Produces<ApiResponse<CreateSubFamilyCommandResponse>>(201);
        group.MapPost("/edit", HandleEdit).Produces<ApiResponse<EditSubFamilyCommandResponse>>(200);
        group.MapPost("/setActive", HandleSetActive).Produces<ApiResponse<SetActiveSubFamilyCommandResponse>>(200);

        group.MapPost("/export",                 HandleExport);
        group.MapPost("/downloadImportTemplate", HandleDownloadImportTemplate);
        group.MapPost("/bulkImport",             HandleBulkImport)
            .Produces<ApiResponse<BulkImportSubFamiliesCommandResponse>>(200)
            .DisableAntiforgery();
    }

    private static async Task<IResult> HandleGetAll([FromBody] GetSubFamiliesQueryRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetByToken([FromBody] GetSubFamilyByTokenQueryRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleCreate([FromBody] CreateSubFamilyCommandRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        return result.Success ? Results.Created("/sub-families", result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleEdit([FromBody] EditSubFamilyCommandRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleSetActive([FromBody] SetActiveSubFamilyCommandRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleExport([FromBody] ExportSubFamiliesQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return Results.File(result.FileBytes, result.ContentType, result.FileName);
    }

    private static async Task<IResult> HandleDownloadImportTemplate([FromBody] GetSubFamilyImportTemplateQueryRequest request, ISender sender, CancellationToken ct)
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
            var failure = ApiResponse<BulkImportSubFamiliesCommandResponse>.FailureResponse(
                ErrorCodes.SubFamilyBulkImportInvalidFile, "No file was uploaded.", 400);
            return Results.Json(failure, statusCode: 400);
        }

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, ct);

        var request = new BulkImportSubFamiliesCommandRequest { FileBytes = memoryStream.ToArray(), FileName = file.FileName };
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }
}
