using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Application.Requests;
using MediatR;

namespace InnNou.Application.Handlers
{
    public class DownloadOrderPdfQueryHandler(IOrderService orderService, IRequestContext context)
        : IRequestHandler<DownloadOrderPdfQueryRequest, FileResult>
    {
        public async Task<FileResult> Handle(DownloadOrderPdfQueryRequest request, CancellationToken cancellationToken)
        {
            var file = await orderService.GetPdfAsync(request.OrderToken, context, cancellationToken);

            if (file is null)
                throw new ApiException(ErrorCodes.OrderPdfNotAvailable, "No PDF is available for this order yet.", 404);

            return new FileResult { FileBytes = file.Value.FileBytes, FileName = file.Value.FileName, ContentType = "application/pdf" };
        }
    }
}
