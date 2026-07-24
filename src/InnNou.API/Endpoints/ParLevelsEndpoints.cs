using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

public class ParLevelsEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/parLevels").RequireAuthorization();

        group.MapPost("/create",              HandleCreate).Produces<ApiResponse<CreateParLevelCommandResponse>>(201);
        group.MapPost("/edit",                HandleEdit).Produces<ApiResponse<EditParLevelCommandResponse>>(200);
        group.MapPost("/delete",              HandleDelete).Produces<ApiResponse<DeleteParLevelCommandResponse>>(200);
        group.MapPost("/createOverride",      HandleCreateOverride).Produces<ApiResponse<CreateParLevelOverrideCommandResponse>>(201);
        group.MapPost("/deleteOverride",      HandleDeleteOverride).Produces<ApiResponse<DeleteParLevelOverrideCommandResponse>>(200);
        group.MapPost("/getConfiguration",    HandleGetConfiguration).Produces<ApiResponse<GetParLevelConfigurationQueryResponse>>(200);
        group.MapPost("/getBelowPar",         HandleGetBelowPar).Produces<ApiResponse<GetBelowParQueryResponse>>(200);
    }

    private static async Task<IResult> HandleCreate([FromBody] CreateParLevelCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Json(result, statusCode: result.StatusCode ?? 201) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleEdit([FromBody] EditParLevelCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleDelete([FromBody] DeleteParLevelCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleCreateOverride([FromBody] CreateParLevelOverrideCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Json(result, statusCode: result.StatusCode ?? 201) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleDeleteOverride([FromBody] DeleteParLevelOverrideCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetConfiguration([FromBody] GetParLevelConfigurationQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetBelowPar([FromBody] GetBelowParQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }
}
