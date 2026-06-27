using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

public class FamilyEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/families").RequireAuthorization();

        group.MapPost("/getAll", HandleGetAll).Produces<ApiResponse<GetFamiliesQueryResponse>>(200);
        group.MapPost("/getByToken", HandleGetByToken).Produces<ApiResponse<GetFamilyByTokenQueryResponse>>(200);
        group.MapPost("/create", HandleCreate).Produces<ApiResponse<CreateFamilyCommandResponse>>(201);
        group.MapPost("/edit", HandleEdit).Produces<ApiResponse<EditFamilyCommandResponse>>(200);
        group.MapPost("/setActive", HandleSetActive).Produces<ApiResponse<SetActiveFamilyCommandResponse>>(200);
    }

    private static async Task<IResult> HandleGetAll([FromBody] GetFamiliesQueryRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }

    private static async Task<IResult> HandleGetByToken([FromBody] GetFamilyByTokenQueryRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }

    private static async Task<IResult> HandleCreate([FromBody] CreateFamilyCommandRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        return result.Success ? Results.Created("/families", result) : Results.BadRequest(result);
    }

    private static async Task<IResult> HandleEdit([FromBody] EditFamilyCommandRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }

    private static async Task<IResult> HandleSetActive([FromBody] SetActiveFamilyCommandRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }
}
