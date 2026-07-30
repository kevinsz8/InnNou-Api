using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class DownloadSupplierInvoiceAttachmentQueryHandler(ISupplierInvoiceService supplierInvoiceService, IRequestContext context)
        : IRequestHandler<DownloadSupplierInvoiceAttachmentQueryRequest, FileResult>
    {
        private static string ResolveContentType(string extension) => extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };

        public async Task<FileResult> Handle(DownloadSupplierInvoiceAttachmentQueryRequest request, CancellationToken cancellationToken)
        {
            var file = await supplierInvoiceService.DownloadAttachmentAsync(request.SupplierInvoiceToken, context, cancellationToken);

            if (file is null)
                throw new ApiException(ErrorCodes.SupplierInvoiceNotFound, "No attachment is available for this supplier invoice.", 404);

            return new FileResult
            {
                FileBytes = file.Value.Bytes,
                FileName = $"invoice{file.Value.Extension}",
                ContentType = ResolveContentType(file.Value.Extension)
            };
        }
    }
}
