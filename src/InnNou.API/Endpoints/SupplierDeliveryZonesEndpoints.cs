using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

public class SupplierDeliveryZonesEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/supplierDeliveryZones").RequireAuthorization();

        group.MapPost("/getBySupplier", HandleGetBySupplier).Produces<ApiResponse<GetSupplierDeliveryZonesQueryResponse>>(200);
        group.MapPost("/create", HandleCreate).Produces<ApiResponse<CreateSupplierDeliveryZoneCommandResponse>>(201);
        group.MapPost("/delete", HandleDelete).Produces<ApiResponse<DeleteSupplierDeliveryZoneCommandResponse>>(200);
    }

    private static async Task<IResult> HandleGetBySupplier([FromBody] GetSupplierDeliveryZonesQueryRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleCreate([FromBody] CreateSupplierDeliveryZoneCommandRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        return result.Success ? Results.Created("/supplierDeliveryZones", result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleDelete([FromBody] DeleteSupplierDeliveryZoneCommandRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }
}
