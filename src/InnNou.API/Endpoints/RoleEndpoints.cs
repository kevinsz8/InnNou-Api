using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints
{
    public class RoleEndpoints : ICarterModule
    {

        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/roles")
                       .RequireAuthorization();

            group.MapPost("/getRoles", HandleGetRoles)
                .Produces<ApiResponse<GetRolesQueryResponse>>(200);

            group.MapPost("/getRoleByToken", HandleGetRoleByToken)
                .Produces<ApiResponse<GetRoleByTokenQueryResponse>>(200);
        }

        private static async Task<ApiResponse<GetRoleByTokenQueryResponse>> HandleGetRoleByToken(
            [FromBody] GetRoleByTokenQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<GetRoleByTokenQueryResponse>.FailureResponse(result.Errors);
            return result;
        }

        private static async Task<ApiResponse<GetRolesQueryResponse>> HandleGetRoles(
            [FromBody] GetRolesQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<GetRolesQueryResponse>.FailureResponse(result.Errors);
            return result;
        }
    }
}
