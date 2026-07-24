using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class CreateGoodsReceiptLineRequestItem
    {
        public Guid PurchaseOrderLineToken { get; set; }
        public decimal QuantityAccepted { get; set; }
        public decimal QuantityCourtesy { get; set; }
        public decimal QuantityRejected { get; set; }
        public string? RejectionReason { get; set; }
        public string? LotNumber { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string? SerialNumber { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateGoodsReceiptCommandRequest : IRequest<ApiResponse<CreateGoodsReceiptCommandResponse>>
    {
        public Guid PurchaseOrderToken { get; set; }
        public string? Notes { get; set; }
        public List<CreateGoodsReceiptLineRequestItem> Lines { get; set; } = [];
    }
}
