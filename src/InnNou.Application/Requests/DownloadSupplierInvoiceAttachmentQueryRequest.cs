using InnNou.Application.Common;
using MediatR;

namespace InnNou.Application.Requests
{
    public class DownloadSupplierInvoiceAttachmentQueryRequest : IRequest<FileResult>
    {
        public Guid SupplierInvoiceToken { get; set; }
    }
}
