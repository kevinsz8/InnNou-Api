using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.API.Endpoints
{
    public class TenantEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/tenants");

            group.MapPost("/", HandleCreateTenant)
                .Produces<ApiResponse<CreateTenantCommandResponse>>(201);

            group.MapGet("/", HandleGetTenants)
                .Produces<ApiResponse<GetTenantsQueryResponse>>(200);

            group.MapGet("/{id:guid}", HandleGetTenantById)
                .Produces<ApiResponse<GetTenantByIdQueryResponse>>(200);
        }

        private static async Task<ApiResponse<CreateTenantCommandResponse>> HandleCreateTenant(
            CreateTenantCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<CreateTenantCommandResponse>.FailureResponse(result.Errors);
            return result;
        }

        private static async Task<ApiResponse<GetTenantsQueryResponse>> HandleGetTenants(
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(new GetTenantsQueryRequest(), ct);
            if (!result.Success)
                return ApiResponse<GetTenantsQueryResponse>.FailureResponse(result.Errors);
            return result;
        }

        private static async Task<ApiResponse<GetTenantByIdQueryResponse>> HandleGetTenantById(
            Guid id,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(new GetTenantByIdQueryRequest(id), ct);
            if (!result.Success)
                return ApiResponse<GetTenantByIdQueryResponse>.FailureResponse(result.Errors);
            return result;
        }

    }
}
