using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

public class TaxEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tax").RequireAuthorization();

        group.MapPost("/getCategories", HandleGetCategories).Produces<ApiResponse<GetTaxCategoriesQueryResponse>>(200);
        group.MapPost("/getJurisdictions", HandleGetJurisdictions).Produces<ApiResponse<GetTaxJurisdictionsQueryResponse>>(200);
        group.MapPost("/getRateGrid", HandleGetRateGrid).Produces<ApiResponse<GetTaxRateGridQueryResponse>>(200);
        group.MapPost("/upsertRate", HandleUpsertRate).Produces<ApiResponse<UpsertTaxRateCommandResponse>>(200);
    }

    private static async Task<IResult> HandleGetCategories(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTaxCategoriesQueryRequest(), cancellationToken);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetJurisdictions(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTaxJurisdictionsQueryRequest(), cancellationToken);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetRateGrid(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTaxRateGridQueryRequest(), cancellationToken);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleUpsertRate([FromBody] UpsertTaxRateCommandRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }
}
