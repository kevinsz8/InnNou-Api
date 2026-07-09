using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints
{
    public class AuthEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/auth/login", HandleLogin)
                .Produces<ApiResponse<LoginResponse>>(200);

            app.MapPost("/auth/refresh", Refresh)
                .Produces<ApiResponse<LoginResponse>>(200);

            app.MapPost("/auth/impersonate", Impersonate)
                .RequireAuthorization()
                .Produces<ApiResponse<ImpersonateResponse>>(200);

            app.MapPost("/auth/impersonate-supplier", ImpersonateSupplier)
                .RequireAuthorization()
                .Produces<ApiResponse<ImpersonateResponse>>(200);

            app.MapPost("/auth/stop-impersonate", StopImpersonate)
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

        private static async Task<IResult> StopImpersonate(
            IMediator mediator,
            CancellationToken cancellationToken)
        {
            var response = await mediator.Send(new StopImpersonateCommandRequest(), cancellationToken);

            return Results.Json(response, statusCode: response.StatusCode ?? (response.Success ? 200 : 400));
        }
    }
}
