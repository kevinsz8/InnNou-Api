using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

public class ArticleFavoritesEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/articleFavorites").RequireAuthorization();

        group.MapPost("/create", HandleCreate).Produces<ApiResponse<CreateArticleFavoriteCommandResponse>>(201);
        group.MapPost("/delete", HandleDelete).Produces<ApiResponse<DeleteArticleFavoriteCommandResponse>>(200);
        group.MapPost("/getAll", HandleGetAll).Produces<ApiResponse<GetArticleFavoritesQueryResponse>>(200);
    }

    private static async Task<IResult> HandleCreate([FromBody] CreateArticleFavoriteCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Created("/articleFavorites", result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleDelete([FromBody] DeleteArticleFavoriteCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetAll([FromBody] GetArticleFavoritesQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }
}
