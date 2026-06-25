using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints
{
    public class SupplierEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/suppliers")
                           .RequireAuthorization();

            group.MapPost("/getSuppliers", HandleGetSuppliers)
                .Produces<ApiResponse<GetSuppliersQueryResponse>>(200);

            group.MapPost("/getSupplierByToken", HandleGetSupplierByToken)
                .Produces<ApiResponse<GetSupplierByTokenQueryResponse>>(200);

            group.MapPost("/createSupplier", HandleCreateSupplier)
                .Produces<ApiResponse<CreateSupplierCommandResponse>>(201);

            group.MapPost("/editSupplier", HandleEditSupplier)
                .Produces<ApiResponse<EditSupplierCommandResponse>>(200);

            group.MapPost("/deleteSupplier", HandleDeleteSupplier)
                .Produces<ApiResponse<DeleteSupplierCommandResponse>>(200);
        }

        private static async Task<ApiResponse<GetSupplierByTokenQueryResponse>> HandleGetSupplierByToken(
            [FromBody] GetSupplierByTokenQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<GetSupplierByTokenQueryResponse>.FailureResponse(result.Errors);
            return result;
        }

        private static async Task<ApiResponse<GetSuppliersQueryResponse>> HandleGetSuppliers(
            [FromBody] GetSuppliersQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<GetSuppliersQueryResponse>.FailureResponse(result.Errors);
            return result;
        }

        private static async Task<ApiResponse<CreateSupplierCommandResponse>> HandleCreateSupplier(
            [FromBody] CreateSupplierCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<CreateSupplierCommandResponse>.FailureResponse(result.Errors);
            return result;
        }

        private static async Task<ApiResponse<EditSupplierCommandResponse>> HandleEditSupplier(
            [FromBody] EditSupplierCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<EditSupplierCommandResponse>.FailureResponse(result.Errors);
            return result;
        }

        private static async Task<ApiResponse<DeleteSupplierCommandResponse>> HandleDeleteSupplier(
            [FromBody] DeleteSupplierCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<DeleteSupplierCommandResponse>.FailureResponse(result.Errors);
            return result;
        }
    }
}
