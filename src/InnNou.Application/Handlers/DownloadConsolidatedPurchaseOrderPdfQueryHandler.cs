using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class DownloadConsolidatedPurchaseOrderPdfQueryHandler(IConsolidatedPurchaseOrderService consolidatedPurchaseOrderService, IRequestContext context)
        : IRequestHandler<DownloadConsolidatedPurchaseOrderPdfQueryRequest, FileResult>
    {
        public async Task<FileResult> Handle(DownloadConsolidatedPurchaseOrderPdfQueryRequest request, CancellationToken cancellationToken)
        {
            var file = await consolidatedPurchaseOrderService.GetPdfAsync(request.ConsolidatedPurchaseOrderToken, context, cancellationToken);

            if (file is null)
                throw new ApiException(ErrorCodes.ConsolidatedPurchaseOrderNotFound, "Consolidated purchase order not found.", 404);

            return new FileResult { FileBytes = file.Value.FileBytes, FileName = file.Value.FileName, ContentType = "application/pdf" };
        }
    }
}
