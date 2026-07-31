using Carter;
using InnNou.Application.Common;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InnNou.API.Endpoints;

public class SupplierInvoicesEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/supplierInvoices").RequireAuthorization();

        group.MapPost("/getPaged", HandleGetPaged).Produces<ApiResponse<GetSupplierInvoicesQueryResponse>>(200);
        group.MapPost("/getByToken", HandleGetByToken).Produces<ApiResponse<GetSupplierInvoiceByTokenQueryResponse>>(200);
        group.MapPost("/getEligiblePurchaseOrders", HandleGetEligiblePurchaseOrders).Produces<ApiResponse<GetEligiblePurchaseOrdersForInvoicingQueryResponse>>(200);
        group.MapPost("/create", HandleCreate).Produces<ApiResponse<CreateSupplierInvoiceCommandResponse>>(201);

        group.MapPost("/uploadAttachment", HandleUploadAttachment)
            .Produces<ApiResponse<UploadSupplierInvoiceAttachmentCommandResponse>>(200)
            .DisableAntiforgery();

        group.MapPost("/downloadAttachment", HandleDownloadAttachment);

        group.MapPost("/getEffectiveTolerance", HandleGetEffectiveTolerance).Produces<ApiResponse<GetSupplierInvoiceMatchToleranceQueryResponse>>(200);
        group.MapPost("/upsertTolerance", HandleUpsertTolerance).Produces<ApiResponse<UpsertSupplierInvoiceMatchToleranceCommandResponse>>(200);

        group.MapPost("/getEffectivePurchaseOrderPolicy", HandleGetEffectivePurchaseOrderPolicy).Produces<ApiResponse<GetSupplierInvoicePurchaseOrderPolicyQueryResponse>>(200);
        group.MapPost("/upsertPurchaseOrderPolicy", HandleUpsertPurchaseOrderPolicy).Produces<ApiResponse<UpsertSupplierInvoicePurchaseOrderPolicyCommandResponse>>(200);
    }

    private static async Task<IResult> HandleGetPaged([FromBody] GetSupplierInvoicesQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetByToken([FromBody] GetSupplierInvoiceByTokenQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetEligiblePurchaseOrders([FromBody] GetEligiblePurchaseOrdersForInvoicingQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleCreate([FromBody] CreateSupplierInvoiceCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Created("/supplierInvoices", result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleUploadAttachment(HttpRequest httpRequest, ISender sender, CancellationToken ct)
    {
        // Kestrel disables synchronous I/O by default — ReadFormAsync is the async-safe way
        // to bind multipart form data (same pattern as SupplierEndpoints.HandleUploadLogo).
        var form = await httpRequest.ReadFormAsync(ct);
        var file = form.Files["file"];

        if (file is null || file.Length == 0)
        {
            var failure = ApiResponse<UploadSupplierInvoiceAttachmentCommandResponse>.FailureResponse(
                ErrorCodes.SupplierInvoiceAttachmentInvalidFile, "No file was uploaded.", 400);
            return Results.Json(failure, statusCode: 400);
        }

        if (!Guid.TryParse(form["supplierInvoiceToken"], out var supplierInvoiceToken))
        {
            var failure = ApiResponse<UploadSupplierInvoiceAttachmentCommandResponse>.FailureResponse(
                ErrorCodes.InvalidRequest, "supplierInvoiceToken is required.", 400);
            return Results.Json(failure, statusCode: 400);
        }

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, ct);

        var request = new UploadSupplierInvoiceAttachmentCommandRequest
        {
            SupplierInvoiceToken = supplierInvoiceToken,
            FileBytes = memoryStream.ToArray(),
            FileName = file.FileName
        };

        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleDownloadAttachment([FromBody] DownloadSupplierInvoiceAttachmentQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return Results.File(result.FileBytes, result.ContentType, result.FileName);
    }

    private static async Task<IResult> HandleGetEffectiveTolerance([FromBody] GetSupplierInvoiceMatchToleranceQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleUpsertTolerance([FromBody] UpsertSupplierInvoiceMatchToleranceCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleGetEffectivePurchaseOrderPolicy([FromBody] GetSupplierInvoicePurchaseOrderPolicyQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }

    private static async Task<IResult> HandleUpsertPurchaseOrderPolicy([FromBody] UpsertSupplierInvoicePurchaseOrderPolicyCommandRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: result.StatusCode ?? 400);
    }
}
