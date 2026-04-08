using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints
{
    public class UsersEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/users")
                       .RequireAuthorization();

            group.MapPost("/createUser", HandleCreateUser)
                .Produces<ApiResponse<CreateUserCommandResponse>>(201);

            group.MapPost("/getUsers", HandleGetUsers)
                .Produces<ApiResponse<GetUsersQueryResponse>>(200);

            group.MapPost("/editUser", HandleEditUser)
                .Produces<ApiResponse<EditUserCommandResponse>>(200);

            group.MapPost("/deleteUser", HandleDeleteUser)
                .Produces<ApiResponse<DeleteUserCommandResponse>>(200);
        }

        private static async Task<ApiResponse<CreateUserCommandResponse>> HandleCreateUser(
            [FromBody] CreateUserCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<CreateUserCommandResponse>.FailureResponse(result.Errors);
            return result;
        }

        private static async Task<ApiResponse<GetUsersQueryResponse>> HandleGetUsers(
            [FromBody] GetUsersQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<GetUsersQueryResponse>.FailureResponse(result.Errors);
            return result;
        }

        private static async Task<ApiResponse<EditUserCommandResponse>> HandleEditUser(
            [FromBody] EditUserCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<EditUserCommandResponse>.FailureResponse(result.Errors);
            return result;
        }

        private static async Task<ApiResponse<DeleteUserCommandResponse>> HandleDeleteUser(
            [FromBody] DeleteUserCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            if (!result.Success)
                return ApiResponse<DeleteUserCommandResponse>.FailureResponse(result.Errors);
            return result;
        }
    }
}
