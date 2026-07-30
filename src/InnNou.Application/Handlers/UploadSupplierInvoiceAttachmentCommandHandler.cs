using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class UploadSupplierInvoiceAttachmentCommandHandler(ISupplierInvoiceService supplierInvoiceService, IRequestContext context)
        : IRequestHandler<UploadSupplierInvoiceAttachmentCommandRequest, ApiResponse<UploadSupplierInvoiceAttachmentCommandResponse>>
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".png", ".jpg", ".jpeg" };
        private const int MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        public async Task<ApiResponse<UploadSupplierInvoiceAttachmentCommandResponse>> Handle(UploadSupplierInvoiceAttachmentCommandRequest request, CancellationToken cancellationToken)
        {
            var extension = Path.GetExtension(request.FileName);
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
                return ApiResponse<UploadSupplierInvoiceAttachmentCommandResponse>.FailureResponse(ErrorCodes.SupplierInvoiceAttachmentInvalidFile, "The attachment must be a PDF, JPG, or PNG file.", 400);

            if (request.FileBytes.Length == 0)
                return ApiResponse<UploadSupplierInvoiceAttachmentCommandResponse>.FailureResponse(ErrorCodes.SupplierInvoiceAttachmentInvalidFile, "No file was uploaded.", 400);

            if (request.FileBytes.Length > MaxFileSizeBytes)
                return ApiResponse<UploadSupplierInvoiceAttachmentCommandResponse>.FailureResponse(ErrorCodes.SupplierInvoiceAttachmentInvalidFile, "The attachment must be 10 MB or smaller.", 400);

            using var stream = new MemoryStream(request.FileBytes);
            var uploaded = await supplierInvoiceService.UploadAttachmentAsync(request.SupplierInvoiceToken, stream, extension.ToLowerInvariant(), context, cancellationToken);

            if (!uploaded)
                return ApiResponse<UploadSupplierInvoiceAttachmentCommandResponse>.FailureResponse(ErrorCodes.SupplierInvoiceNotFound, "Supplier invoice not found.", 404);

            return ApiResponse<UploadSupplierInvoiceAttachmentCommandResponse>.SuccessResponse(new UploadSupplierInvoiceAttachmentCommandResponse
            {
                SupplierInvoiceToken = request.SupplierInvoiceToken,
                Uploaded = true
            });
        }
    }
}
