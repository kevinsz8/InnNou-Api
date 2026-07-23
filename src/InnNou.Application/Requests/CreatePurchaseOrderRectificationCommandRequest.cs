using InnNou.Application.Common;
using InnNou.Application.Responses;
using MediatR;

namespace InnNou.Application.Requests
{
    public class RectifyPurchaseOrderLineRequestItem
    {
        public Guid PurchaseOrderLineToken { get; set; }
        public bool Cancel { get; set; }
        public decimal? NewQuantity { get; set; }
        public decimal? NewUnitPrice { get; set; }
        public string? NewCurrencyCode { get; set; }
    }

    public class CreatePurchaseOrderRectificationCommandRequest : IRequest<ApiResponse<CreatePurchaseOrderRectificationCommandResponse>>
    {
        public Guid PurchaseOrderToken { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public List<RectifyPurchaseOrderLineRequestItem> Lines { get; set; } = [];
    }
}
