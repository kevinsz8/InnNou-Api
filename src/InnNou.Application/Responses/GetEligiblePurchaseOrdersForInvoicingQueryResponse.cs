using InnNou.Application.Responses.Common;

namespace InnNou.Application.Responses
{
    public class GetEligiblePurchaseOrdersForInvoicingQueryResponse
    {
        public List<PurchaseOrder> PurchaseOrders { get; set; } = [];
    }
}
