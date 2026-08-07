using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

public class ArticleDiscountsEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/articleDiscounts").RequireAuthorization();

        group.MapPost("/getAll", HandleGetAll).Produces<ApiResponse<GetArticleDiscountsQueryResponse>>(200);
        group.MapPost("/getByToken", HandleGetByToken).Produces<ApiResponse<GetArticleDiscountByTokenQueryResponse>>(200);
        group.MapPost("/create", HandleCreate).Produces<ApiResponse<CreateArticleDiscountCommandResponse>>(201);
        group.MapPost("/edit", HandleEdit).Produces<ApiResponse<EditArticleDiscountCommandResponse>>(200);
        group.MapPost("/setActive", HandleSetActive).Produces<ApiResponse<SetActiveArticleDiscountCommandResponse>>(200);
    }

    private static async Task<IResult> HandleGetAll([FromBody] GetArticleDiscountsQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetByToken([FromBody] GetArticleDiscountByTokenQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleCreate([FromBody] CreateArticleDiscountCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Created("/articleDiscounts", result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleEdit([FromBody] EditArticleDiscountCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleSetActive([FromBody] SetActiveArticleDiscountCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }
}
