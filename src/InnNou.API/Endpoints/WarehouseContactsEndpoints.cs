using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

public class WarehouseContactsEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/warehouseContacts").RequireAuthorization();

        group.MapPost("/getContactsByWarehouseToken", HandleGetContactsByWarehouseToken)
            .Produces<ApiResponse<GetWarehouseContactsByWarehouseTokenQueryResponse>>(200);

        group.MapPost("/getContactByToken", HandleGetContactByToken)
            .Produces<ApiResponse<GetWarehouseContactByTokenQueryResponse>>(200);

        group.MapPost("/createContact", HandleCreateContact)
            .Produces<ApiResponse<CreateWarehouseContactCommandResponse>>(201);

        group.MapPost("/editContact", HandleEditContact)
            .Produces<ApiResponse<EditWarehouseContactCommandResponse>>(200);

        group.MapPost("/deleteContact", HandleDeleteContact)
            .Produces<ApiResponse<DeleteWarehouseContactCommandResponse>>(200);
    }

    private static async Task<IResult> HandleGetContactsByWarehouseToken(
        [FromBody] GetWarehouseContactsByWarehouseTokenQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetContactByToken(
        [FromBody] GetWarehouseContactByTokenQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleCreateContact(
        [FromBody] CreateWarehouseContactCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Created("/warehouseContacts", result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleEditContact(
        [FromBody] EditWarehouseContactCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleDeleteContact(
        [FromBody] DeleteWarehouseContactCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }
}
