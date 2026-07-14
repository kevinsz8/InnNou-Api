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

            group.MapPost("/exportSuppliers", HandleExportSuppliers);

            group.MapPost("/downloadImportTemplate", HandleDownloadImportTemplate);

            group.MapPost("/bulkImportSuppliers", HandleBulkImportSuppliers)
                .Produces<ApiResponse<BulkImportSuppliersCommandResponse>>(200)
                .DisableAntiforgery();
        }

        private static async Task<IResult> HandleGetSupplierByToken(
            [FromBody] GetSupplierByTokenQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> HandleGetSuppliers(
            [FromBody] GetSuppliersQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> HandleCreateSupplier(
            [FromBody] CreateSupplierCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> HandleEditSupplier(
            [FromBody] EditSupplierCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> HandleDeleteSupplier(
            [FromBody] DeleteSupplierCommandRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }

        private static async Task<IResult> HandleExportSuppliers(
            [FromBody] ExportSuppliersQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.File(result.FileBytes, result.ContentType, result.FileName);
        }

        private static async Task<IResult> HandleDownloadImportTemplate(
            [FromBody] GetSupplierImportTemplateQueryRequest request,
            IMediator mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request, ct);
            return Results.File(result.FileBytes, result.ContentType, result.FileName);
        }

        private static async Task<IResult> HandleBulkImportSuppliers(
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
                var failure = ApiResponse<BulkImportSuppliersCommandResponse>.FailureResponse(
                    ErrorCodes.SupplierBulkImportInvalidFile, "No file was uploaded.", 400);
                return Results.Json(failure, statusCode: 400);
            }

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, ct);

            var request = new BulkImportSuppliersCommandRequest { FileBytes = memoryStream.ToArray(), FileName = file.FileName };
            var result = await mediator.Send(request, ct);
            return Results.Json(result, statusCode: result.StatusCode ?? (result.Success ? 200 : 400));
        }
    }
}
