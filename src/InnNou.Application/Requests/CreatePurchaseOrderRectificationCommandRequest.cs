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

    // A brand-new article never on the original PO — same supplier only. ManualUnitPrice/
    // ManualCurrencyCode are only used when the article has no resolvable catalog price
    // (SERVICE/MIXED supplier).
    public class RectifyPurchaseOrderNewLineRequestItem
    {
        public Guid ArticleToken { get; set; }
        public decimal Quantity { get; set; }
        public decimal? ManualUnitPrice { get; set; }
        public string? ManualCurrencyCode { get; set; }
    }

    public class CreatePurchaseOrderRectificationCommandRequest : IRequest<ApiResponse<CreatePurchaseOrderRectificationCommandResponse>>
    {
        public Guid PurchaseOrderToken { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public List<RectifyPurchaseOrderLineRequestItem> Lines { get; set; } = [];
        public List<RectifyPurchaseOrderNewLineRequestItem> NewLines { get; set; } = [];
    }
}
