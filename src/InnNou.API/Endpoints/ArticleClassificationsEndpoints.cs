using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

public class ArticleClassificationsEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/articleClassifications").RequireAuthorization();

        group.MapPost("/assign", HandleAssign).Produces<ApiResponse<AssignArticleClassificationCommandResponse>>(201);
        group.MapPost("/unassign", HandleUnassign).Produces<ApiResponse<UnassignArticleClassificationCommandResponse>>(200);
        group.MapPost("/bulkAssign", HandleBulkAssign).Produces<ApiResponse<BulkAssignArticleClassificationCommandResponse>>(200);
    }

    private static async Task<IResult> HandleAssign([FromBody] AssignArticleClassificationCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Created("/articleClassifications", result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleUnassign([FromBody] UnassignArticleClassificationCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleBulkAssign([FromBody] BulkAssignArticleClassificationCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }
}
