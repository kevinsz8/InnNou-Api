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

        private static async Task<IResult> HandleGetRoleByToken(
            [FromBody] GetRoleByTokenQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> HandleGetRoles(
            [FromBody] GetRolesQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }
    }
}
