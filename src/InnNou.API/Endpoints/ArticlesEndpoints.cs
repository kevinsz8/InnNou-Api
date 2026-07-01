using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

public class ArticlesEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/articles").RequireAuthorization();

        group.MapPost("/getAll",       HandleGetAll).Produces<ApiResponse<GetArticlesQueryResponse>>(200);
        group.MapPost("/getByToken",   HandleGetByToken).Produces<ApiResponse<GetArticleByTokenQueryResponse>>(200);
        group.MapPost("/create",       HandleCreate).Produces<ApiResponse<CreateArticleCommandResponse>>(201);
        group.MapPost("/edit",         HandleEdit).Produces<ApiResponse<EditArticleCommandResponse>>(200);
        group.MapPost("/delete",       HandleDelete).Produces<ApiResponse<DeleteArticleCommandResponse>>(200);
    }

    private static async Task<IResult> HandleGetAll([FromBody] GetArticlesQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }

    private static async Task<IResult> HandleGetByToken([FromBody] GetArticleByTokenQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }

    private static async Task<IResult> HandleCreate([FromBody] CreateArticleCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Created("/articles", result) : Results.BadRequest(result);
    }

    private static async Task<IResult> HandleEdit([FromBody] EditArticleCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }

    private static async Task<IResult> HandleDelete([FromBody] DeleteArticleCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }
}
