using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

public class OrderTemplatesEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orderTemplates").RequireAuthorization();

        group.MapPost("/create",       HandleCreate).Produces<ApiResponse<CreateOrderTemplateCommandResponse>>(201);
        group.MapPost("/rename",       HandleRename).Produces<ApiResponse<RenameOrderTemplateCommandResponse>>(200);
        group.MapPost("/delete",       HandleDelete).Produces<ApiResponse<DeleteOrderTemplateCommandResponse>>(200);
        group.MapPost("/addLine",      HandleAddLine).Produces<ApiResponse<AddOrderTemplateLineCommandResponse>>(201);
        group.MapPost("/editLine",     HandleEditLine).Produces<ApiResponse<EditOrderTemplateLineCommandResponse>>(200);
        group.MapPost("/deleteLine",   HandleDeleteLine).Produces<ApiResponse<DeleteOrderTemplateLineCommandResponse>>(200);
        group.MapPost("/getAll",       HandleGetAll).Produces<ApiResponse<GetOrderTemplatesQueryResponse>>(200);
        group.MapPost("/getByToken",   HandleGetByToken).Produces<ApiResponse<GetOrderTemplateByTokenQueryResponse>>(200);
        group.MapPost("/applyToOrder", HandleApplyToOrder).Produces<ApiResponse<ApplyOrderTemplateCommandResponse>>(200);

        group.MapPost("/export", HandleExport);
    }

    private static async Task<IResult> HandleCreate([FromBody] CreateOrderTemplateCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Created("/orderTemplates", result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleRename([FromBody] RenameOrderTemplateCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleDelete([FromBody] DeleteOrderTemplateCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleAddLine([FromBody] AddOrderTemplateLineCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Created("/orderTemplates", result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleEditLine([FromBody] EditOrderTemplateLineCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleDeleteLine([FromBody] DeleteOrderTemplateLineCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetAll([FromBody] GetOrderTemplatesQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetByToken([FromBody] GetOrderTemplateByTokenQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleApplyToOrder([FromBody] ApplyOrderTemplateCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleExport([FromBody] ExportOrderTemplateQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return Results.File(result.FileBytes, result.ContentType, result.FileName);
    }
}
