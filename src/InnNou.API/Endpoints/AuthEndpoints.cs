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
        }

        private static async Task<ApiResponse<LoginResponse>> HandleLogin(
            [FromBody] LoginCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<LoginResponse>.FailureResponse(result.Errors);
            return result;
        }

        private static async Task<ApiResponse<LoginResponse>> Refresh(
            [FromBody] RefreshTokenRequest request,
             IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<LoginResponse>.FailureResponse(result.Errors);
            return result;
        }

        private static async Task<ApiResponse<ImpersonateResponse>> Impersonate(
            [FromBody] ImpersonateRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);

            if (!result.Success)
                return ApiResponse<ImpersonateResponse>.FailureResponse(result.Errors);

            return result;
        }
    }
}
