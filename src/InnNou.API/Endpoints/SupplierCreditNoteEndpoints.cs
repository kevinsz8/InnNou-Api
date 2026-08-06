using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints
{
    public class SupplierCreditNoteEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/supplierCreditNotes")
                           .RequireAuthorization();

            group.MapPost("/create", HandleCreate)
                .Produces<ApiResponse<CreateSupplierCreditNoteCommandResponse>>(201);

            group.MapPost("/getByToken", HandleGetByToken)
                .Produces<ApiResponse<GetSupplierCreditNoteByTokenQueryResponse>>(200);

            group.MapPost("/getBySupplierReturnToken", HandleGetBySupplierReturnToken)
                .Produces<ApiResponse<GetSupplierCreditNoteByTokenQueryResponse>>(200);

            group.MapPost("/getAll", HandleGetAll)
                .Produces<ApiResponse<GetSupplierCreditNotesQueryResponse>>(200);
        }

        private static async Task<IResult> HandleCreate(
            [FromBody] CreateSupplierCreditNoteCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> HandleGetByToken(
            [FromBody] GetSupplierCreditNoteByTokenQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> HandleGetBySupplierReturnToken(
            [FromBody] GetSupplierCreditNoteBySupplierReturnTokenQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> HandleGetAll(
            [FromBody] GetSupplierCreditNotesQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }
    }
}
