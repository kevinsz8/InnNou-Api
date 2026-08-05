using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

public class DepartmentParLevelsEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/departmentParLevels").RequireAuthorization();

        group.MapPost("/create",              HandleCreate).Produces<ApiResponse<CreateDepartmentParLevelCommandResponse>>(201);
        group.MapPost("/edit",                HandleEdit).Produces<ApiResponse<EditDepartmentParLevelCommandResponse>>(200);
        group.MapPost("/setActive",           HandleSetActive).Produces<ApiResponse<SetActiveDepartmentParLevelCommandResponse>>(200);
        group.MapPost("/getByDepartmentAndArticle", HandleGetByDepartmentAndArticle).Produces<ApiResponse<GetDepartmentParLevelByDepartmentAndArticleQueryResponse>>(200);
        group.MapPost("/getSuggested",        HandleGetSuggested).Produces<ApiResponse<GetSuggestedRequisitionsQueryResponse>>(200);
    }

    private static async Task<IResult> HandleCreate([FromBody] CreateDepartmentParLevelCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Json(result, statusCode: result.StatusCode ?? 201) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleEdit([FromBody] EditDepartmentParLevelCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleSetActive([FromBody] SetActiveDepartmentParLevelCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetByDepartmentAndArticle([FromBody] GetDepartmentParLevelByDepartmentAndArticleQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetSuggested([FromBody] GetSuggestedRequisitionsQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }
}
