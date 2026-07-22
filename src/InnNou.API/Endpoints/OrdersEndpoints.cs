using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

public class OrdersEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders").RequireAuthorization();

        group.MapPost("/create",     HandleCreate).Produces<ApiResponse<CreateOrderCommandResponse>>(201);
        group.MapPost("/addLine",    HandleAddLine).Produces<ApiResponse<AddOrderLineCommandResponse>>(201);
        group.MapPost("/editLine",   HandleEditLine).Produces<ApiResponse<EditOrderLineCommandResponse>>(200);
        group.MapPost("/deleteLine", HandleDeleteLine).Produces<ApiResponse<DeleteOrderLineCommandResponse>>(200);
        group.MapPost("/submit",     HandleSubmit).Produces<ApiResponse<SubmitOrderCommandResponse>>(200);
        group.MapPost("/delete",     HandleDelete).Produces<ApiResponse<DeleteOrderCommandResponse>>(200);
        group.MapPost("/cancel",     HandleCancel).Produces<ApiResponse<CancelOrderCommandResponse>>(200);
        group.MapPost("/copy",       HandleCopy).Produces<ApiResponse<CopyOrderCommandResponse>>(201);
        group.MapPost("/getAll",     HandleGetAll).Produces<ApiResponse<GetOrdersQueryResponse>>(200);
        group.MapPost("/getByToken", HandleGetByToken).Produces<ApiResponse<GetOrderByTokenQueryResponse>>(200);

        group.MapPost("/importLines", HandleImportLines)
            .Produces<ApiResponse<ImportOrderLinesCommandResponse>>(200)
            .DisableAntiforgery();

        group.MapPost("/approveStep", HandleApproveStep).Produces<ApiResponse<ApproveOrderApprovalStepCommandResponse>>(200);
        group.MapPost("/rejectStep", HandleRejectStep).Produces<ApiResponse<RejectOrderApprovalStepCommandResponse>>(200);
        group.MapPost("/pendingApprovals", HandlePendingApprovals).Produces<ApiResponse<GetPendingOrderApprovalsQueryResponse>>(200);
    }

    private static async Task<IResult> HandleApproveStep([FromBody] ApproveOrderApprovalStepCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleRejectStep([FromBody] RejectOrderApprovalStepCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandlePendingApprovals([FromBody] GetPendingOrderApprovalsQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleCreate([FromBody] CreateOrderCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Created("/orders", result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleAddLine([FromBody] AddOrderLineCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Created("/orders", result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleEditLine([FromBody] EditOrderLineCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleDeleteLine([FromBody] DeleteOrderLineCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleSubmit([FromBody] SubmitOrderCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleDelete([FromBody] DeleteOrderCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleCancel([FromBody] CancelOrderCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleCopy([FromBody] CopyOrderCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Created("/orders", result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetAll([FromBody] GetOrdersQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetByToken([FromBody] GetOrderByTokenQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleImportLines(HttpRequest httpRequest, ISender sender, CancellationToken ct)
    {
        // Kestrel disables synchronous I/O by default — ReadFormAsync is the async-safe way
        // to bind multipart form data (same pattern as ArticlePricesEndpoints.HandleBulkImport).
        var form = await httpRequest.ReadFormAsync(ct);
        var file = form.Files["file"];

        if (file is null || file.Length == 0)
        {
            var failure = ApiResponse<ImportOrderLinesCommandResponse>.FailureResponse(
                ErrorCodes.OrderImportLinesInvalidFile, "No file was uploaded.", 400);
            return Results.Json(failure, statusCode: 400);
        }

        if (!Guid.TryParse(form["orderToken"], out var orderToken))
        {
            var failure = ApiResponse<ImportOrderLinesCommandResponse>.FailureResponse(
                ErrorCodes.InvalidRequest, "orderToken is required.", 400);
            return Results.Json(failure, statusCode: 400);
        }

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, ct);

        var request = new ImportOrderLinesCommandRequest { OrderToken = orderToken, FileBytes = memoryStream.ToArray(), FileName = file.FileName };
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }
}
