using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints
{
    public class SupplierReturnEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/supplierReturns")
                           .RequireAuthorization();

            group.MapPost("/getEligibleLines", HandleGetEligibleLines)
                .Produces<ApiResponse<GetEligibleReturnLinesQueryResponse>>(200);

            group.MapPost("/create", HandleCreate)
                .Produces<ApiResponse<CreateSupplierReturnCommandResponse>>(201);

            group.MapPost("/close", HandleClose)
                .Produces<ApiResponse<CloseSupplierReturnCommandResponse>>(200);

            group.MapPost("/getByToken", HandleGetByToken)
                .Produces<ApiResponse<GetSupplierReturnByTokenQueryResponse>>(200);

            group.MapPost("/getAll", HandleGetAll)
                .Produces<ApiResponse<GetSupplierReturnsQueryResponse>>(200);
        }

        private static async Task<IResult> HandleGetEligibleLines(
            [FromBody] GetEligibleReturnLinesQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> HandleCreate(
            [FromBody] CreateSupplierReturnCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> HandleClose(
            [FromBody] CloseSupplierReturnCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> HandleGetByToken(
            [FromBody] GetSupplierReturnByTokenQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> HandleGetAll(
            [FromBody] GetSupplierReturnsQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }
    }
}
