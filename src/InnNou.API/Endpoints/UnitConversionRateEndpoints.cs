using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

public class UnitConversionRateEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/unit-conversion-rates").RequireAuthorization();

        group.MapPost("/getAll", HandleGetAll).Produces<ApiResponse<GetUnitConversionRatesQueryResponse>>(200);
        group.MapPost("/getByToken", HandleGetByToken).Produces<ApiResponse<GetUnitConversionRateByTokenQueryResponse>>(200);
        group.MapPost("/create", HandleCreate).Produces<ApiResponse<CreateUnitConversionRateCommandResponse>>(201);
        group.MapPost("/edit", HandleEdit).Produces<ApiResponse<EditUnitConversionRateCommandResponse>>(200);
        group.MapPost("/setActive", HandleSetActive).Produces<ApiResponse<SetActiveUnitConversionRateCommandResponse>>(200);
    }

    private static async Task<IResult> HandleGetAll([FromBody] GetUnitConversionRatesQueryRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }

    private static async Task<IResult> HandleGetByToken([FromBody] GetUnitConversionRateByTokenQueryRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }

    private static async Task<IResult> HandleCreate([FromBody] CreateUnitConversionRateCommandRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        return result.Success ? Results.Created("/unit-conversion-rates", result) : Results.BadRequest(result);
    }

    private static async Task<IResult> HandleEdit([FromBody] EditUnitConversionRateCommandRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }

    private static async Task<IResult> HandleSetActive([FromBody] SetActiveUnitConversionRateCommandRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }
}
