using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InnNou.API.Endpoints
{
    public class AuthEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            // Stricter, IP-partitioned rate limit than the rest of the API (see Program.cs's
            // "auth" policy) — this whole group is the brute-force/token-replay surface.
            var group = app.MapGroup("/auth").RequireRateLimiting("auth");

            group.MapPost("/login", HandleLogin)
                .Produces<ApiResponse<LoginResponse>>(200);

            group.MapPost("/refresh", Refresh)
                .Produces<ApiResponse<LoginResponse>>(200);

            group.MapPost("/impersonate", Impersonate)
                .RequireAuthorization()
                .Produces<ApiResponse<ImpersonateResponse>>(200);

            group.MapPost("/impersonate-supplier", ImpersonateSupplier)
                .RequireAuthorization()
                .Produces<ApiResponse<ImpersonateResponse>>(200);

            group.MapPost("/impersonate-warehouse-contact", ImpersonateWarehouseContact)
                .RequireAuthorization()
                .Produces<ApiResponse<ImpersonateResponse>>(200);

            group.MapPost("/stop-impersonate", StopImpersonate)
                .RequireAuthorization();
        }

        private static async Task<IResult> HandleLogin(
            [FromBody] LoginCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> Refresh(
            [FromBody] RefreshTokenRequest request,
             IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> Impersonate(
            [FromBody] ImpersonateRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);

            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> ImpersonateSupplier(
            [FromBody] ImpersonateSupplierRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);

            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> ImpersonateWarehouseContact(
            [FromBody] ImpersonateWarehouseContactRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);

            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> StopImpersonate(
            IMediator mediator,
            CancellationToken cancellationToken)
        {
            var response = await mediator.Send(new StopImpersonateCommandRequest(), cancellationToken);

            return Results.Json(response, statusCode: response.StatusCode ?? (response.Success ? 200 : 400));
        }
    }
}
