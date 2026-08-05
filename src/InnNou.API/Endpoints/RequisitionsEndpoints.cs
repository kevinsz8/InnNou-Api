using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

public class RequisitionsEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/requisitions").RequireAuthorization();

        group.MapPost("/getAll",       HandleGetAll).Produces<ApiResponse<GetRequisitionsQueryResponse>>(200);
        group.MapPost("/getByToken",   HandleGetByToken).Produces<ApiResponse<GetRequisitionByTokenQueryResponse>>(200);
        group.MapPost("/create",       HandleCreate).Produces<ApiResponse<CreateRequisitionCommandResponse>>(201);
        group.MapPost("/addLine",      HandleAddLine).Produces<ApiResponse<AddRequisitionLineCommandResponse>>(201);
        group.MapPost("/editLine",     HandleEditLine).Produces<ApiResponse<EditRequisitionLineCommandResponse>>(200);
        group.MapPost("/deleteLine",   HandleDeleteLine).Produces<ApiResponse<DeleteRequisitionLineCommandResponse>>(200);
        group.MapPost("/approve",      HandleApprove).Produces<ApiResponse<ApproveRequisitionCommandResponse>>(200);
        group.MapPost("/reject",       HandleReject).Produces<ApiResponse<RejectRequisitionCommandResponse>>(200);
        group.MapPost("/cancel",       HandleCancel).Produces<ApiResponse<CancelRequisitionCommandResponse>>(200);
        group.MapPost("/closeShort",   HandleCloseShort).Produces<ApiResponse<CloseShortRequisitionCommandResponse>>(200);
        group.MapPost("/createIssue",  HandleCreateIssue).Produces<ApiResponse<CreateRequisitionIssueCommandResponse>>(201);
    }

    private static async Task<IResult> HandleGetAll([FromBody] GetRequisitionsQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetByToken([FromBody] GetRequisitionByTokenQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleCreate([FromBody] CreateRequisitionCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Created("/requisitions", result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleAddLine([FromBody] AddRequisitionLineCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Created("/requisitions", result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleEditLine([FromBody] EditRequisitionLineCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleDeleteLine([FromBody] DeleteRequisitionLineCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleApprove([FromBody] ApproveRequisitionCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleReject([FromBody] RejectRequisitionCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleCancel([FromBody] CancelRequisitionCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleCloseShort([FromBody] CloseShortRequisitionCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleCreateIssue([FromBody] CreateRequisitionIssueCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Created("/requisitions", result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }
}
