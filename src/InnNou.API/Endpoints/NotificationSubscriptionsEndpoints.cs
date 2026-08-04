using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

public class NotificationSubscriptionsEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/notificationSubscriptions/supplierPriceChange").RequireAuthorization();

        group.MapPost("/set",     HandleSet).Produces<ApiResponse<SetSupplierPriceChangeSubscriptionsCommandResponse>>(200);
        group.MapPost("/getMine", HandleGetMine).Produces<ApiResponse<GetMySupplierPriceChangeSubscriptionsQueryResponse>>(200);
    }

    private static async Task<IResult> HandleSet([FromBody] SetSupplierPriceChangeSubscriptionsCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetMine([FromBody] GetMySupplierPriceChangeSubscriptionsQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }
}
