using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateInternalOrderReceiptLineRequestItem
    {
        public Guid InternalOrderShipmentLineToken { get; set; }
        public decimal QuantityAccepted { get; set; }
        public decimal QuantityRejected { get; set; }
        public string? RejectionReason { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateInternalOrderReceiptCommandRequest : IRequest<ApiResponse<CreateInternalOrderReceiptCommandResponse>>
    {
        public Guid InternalOrderToken { get; set; }
        public string? Notes { get; set; }
        public List<CreateInternalOrderReceiptLineRequestItem> Lines { get; set; } = [];
    }
}
