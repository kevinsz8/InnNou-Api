using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class UploadSupplierInvoiceAttachmentCommandRequest : IRequest<ApiResponse<UploadSupplierInvoiceAttachmentCommandResponse>>
    {
        public Guid SupplierInvoiceToken { get; set; }
        public required byte[] FileBytes { get; set; }
        public required string FileName { get; set; }
    }
}
