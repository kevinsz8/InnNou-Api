using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class UploadSupplierLogoCommandHandler(ISupplierService supplierService, IRequestContext context)
        : IRequestHandler<UploadSupplierLogoCommandRequest, ApiResponse<UploadSupplierLogoCommandResponse>>
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".webp" };
        private const int MaxFileSizeBytes = 2 * 1024 * 1024; // 2 MB

        public async Task<ApiResponse<UploadSupplierLogoCommandResponse>> Handle(UploadSupplierLogoCommandRequest request, CancellationToken cancellationToken)
        {
            if (request.SupplierToken == Guid.Empty)
                return ApiResponse<UploadSupplierLogoCommandResponse>.FailureResponse(ErrorCodes.InvalidRequest, "SupplierToken is required.", 400);

            var extension = Path.GetExtension(request.FileName);
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
                return ApiResponse<UploadSupplierLogoCommandResponse>.FailureResponse(ErrorCodes.SupplierLogoInvalidFile, "Logo must be a PNG, JPG, or WEBP image.", 400);

            if (request.FileBytes.Length == 0)
                return ApiResponse<UploadSupplierLogoCommandResponse>.FailureResponse(ErrorCodes.SupplierLogoInvalidFile, "No file was uploaded.", 400);

            if (request.FileBytes.Length > MaxFileSizeBytes)
                return ApiResponse<UploadSupplierLogoCommandResponse>.FailureResponse(ErrorCodes.SupplierLogoTooLarge, "Logo file must be 2 MB or smaller.", 400);

            using var stream = new MemoryStream(request.FileBytes);
            var updated = await supplierService.UploadLogoAsync(request.SupplierToken, stream, extension.ToLowerInvariant(), context, cancellationToken);

            if (updated is null)
                return ApiResponse<UploadSupplierLogoCommandResponse>.FailureResponse(ErrorCodes.SupplierNotFound, "Supplier not found.", 404);

            return ApiResponse<UploadSupplierLogoCommandResponse>.SuccessResponse(new UploadSupplierLogoCommandResponse
            {
                SupplierToken = updated.SupplierToken,
                LogoUrl = updated.LogoUrl
            });
        }
    }
}
