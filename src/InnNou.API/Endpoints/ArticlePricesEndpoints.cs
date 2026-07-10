using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

public class ArticlePricesEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/articlePrices").RequireAuthorization();

        group.MapPost("/create",     HandleCreate).Produces<ApiResponse<CreateArticlePriceCommandResponse>>(201);
        group.MapPost("/getCurrent", HandleGetCurrent).Produces<ApiResponse<GetCurrentArticlePriceQueryResponse>>(200);
        group.MapPost("/getHistory", HandleGetHistory).Produces<ApiResponse<GetArticlePriceHistoryQueryResponse>>(200);
    }

    private static async Task<IResult> HandleCreate([FromBody] CreateArticlePriceCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Created("/articlePrices", result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetCurrent([FromBody] GetCurrentArticlePriceQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetHistory([FromBody] GetArticlePriceHistoryQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }
}
