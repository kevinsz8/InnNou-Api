using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints
{
    public class OrganizationEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/organizations")
                       .RequireAuthorization();

            group.MapPost("/getOrganizations", HandleGetOrganizations)
                .Produces<ApiResponse<GetOrganizationsQueryResponse>>(200);

            group.MapPost("/getOrganizationByToken", HandleGetOrganizationByToken)
                .Produces<ApiResponse<GetOrganizationByTokenQueryResponse>>(200);

            group.MapPost("/createOrganization", HandleCreateOrganization)
                .Produces<ApiResponse<CreateOrganizationCommandResponse>>(201);

            group.MapPost("/editOrganization", HandleEditOrganization)
                .Produces<ApiResponse<EditOrganizationCommandResponse>>(200);

            group.MapPost("/deleteOrganization", HandleDeleteOrganization)
                .Produces<ApiResponse<DeleteOrganizationCommandResponse>>(200);

            group.MapPost("/exportOrganizations", HandleExportOrganizations);

            group.MapPost("/downloadImportTemplate", HandleDownloadImportTemplate);

            group.MapPost("/bulkImportOrganizations", HandleBulkImportOrganizations)
                .Produces<ApiResponse<BulkImportOrganizationsCommandResponse>>(200)
                .DisableAntiforgery();
        }

        private static async Task<IResult> HandleGetOrganizations(
            [FromBody] GetOrganizationsQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> HandleGetOrganizationByToken(
            [FromBody] GetOrganizationByTokenQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> HandleCreateOrganization(
            [FromBody] CreateOrganizationCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> HandleEditOrganization(
            [FromBody] EditOrganizationCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> HandleDeleteOrganization(
            [FromBody] DeleteOrganizationCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> HandleExportOrganizations(
            [FromBody] ExportOrganizationsQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.File(result.FileBytes, result.ContentType, result.FileName);
        }

        private static async Task<IResult> HandleDownloadImportTemplate(
            [FromBody] GetOrganizationImportTemplateQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.File(result.FileBytes, result.ContentType, result.FileName);
        }

        private static async Task<IResult> HandleBulkImportOrganizations(
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
                var failure = ApiResponse<BulkImportOrganizationsCommandResponse>.FailureResponse(
                    ErrorCodes.OrganizationBulkImportInvalidFile, "No file was uploaded.", 400);
                return Results.Json(failure, statusCode: 400);
            }

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, ct);

            var request = new BulkImportOrganizationsCommandRequest { FileBytes = memoryStream.ToArray(), FileName = file.FileName };
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }
    }
}
