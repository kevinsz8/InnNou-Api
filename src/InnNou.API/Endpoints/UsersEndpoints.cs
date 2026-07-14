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

            group.MapPost("/getUserByToken", HandleGetUserByToken)
                .Produces<ApiResponse<GetUserByTokenQueryResponse>>(200);

            group.MapPost("/editUser", HandleEditUser)
                .Produces<ApiResponse<EditUserCommandResponse>>(200);

            group.MapPost("/deleteUser", HandleDeleteUser)
                .Produces<ApiResponse<DeleteUserCommandResponse>>(200);

            group.MapPost("/exportUsers", HandleExportUsers);

            group.MapPost("/downloadImportTemplate", HandleDownloadImportTemplate);

            group.MapPost("/bulkImportUsers", HandleBulkImportUsers)
                .Produces<ApiResponse<BulkImportUsersCommandResponse>>(200)
                .DisableAntiforgery();
        }

        private static async Task<IResult> HandleCreateUser(
            [FromBody] CreateUserCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> HandleGetUserByToken(
            [FromBody] GetUserByTokenQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> HandleGetUsers(
            [FromBody] GetUsersQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> HandleEditUser(
            [FromBody] EditUserCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> HandleDeleteUser(
            [FromBody] DeleteUserCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> HandleExportUsers(
            [FromBody] ExportUsersQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.File(result.FileBytes, result.ContentType, result.FileName);
        }

        private static async Task<IResult> HandleDownloadImportTemplate(
            [FromBody] GetUserImportTemplateQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.File(result.FileBytes, result.ContentType, result.FileName);
        }

        private static async Task<IResult> HandleBulkImportUsers(
            HttpRequest httpRequest,
            IMediator mediator,
            CancellationToken ct)
        {
            // Kestrel disables synchronous I/O by default — the synchronous HttpRequest.Form
            // getter throws; ReadFormAsync is the async-safe way to bind multipart form data.
            var form = await httpRequest.ReadFormAsync(ct);
            var file = form.Files["file"];

            if (file is null || file.Length == 0)
            {
                var failure = ApiResponse<BulkImportUsersCommandResponse>.FailureResponse(
                    ErrorCodes.UserBulkImportInvalidFile, "No file was uploaded.", 400);
                return Results.Json(failure, statusCode: 400);
            }

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, ct);

            var request = new BulkImportUsersCommandRequest { FileBytes = memoryStream.ToArray(), FileName = file.FileName };
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }
    }
}
