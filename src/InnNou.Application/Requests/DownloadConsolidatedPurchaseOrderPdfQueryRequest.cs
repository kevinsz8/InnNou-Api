using InnNou.Application.Common;
using MediatR;

namespace InnNou.Application.Requests
{
    public class DownloadConsolidatedPurchaseOrderPdfQueryRequest : IRequest<FileResult>
    {
        public Guid ConsolidatedPurchaseOrderToken { get; set; }
    }
}
