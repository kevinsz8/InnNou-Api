using InnNou.Application.Responses.Common;

namespace InnNou.Application.Responses
{
    public class GetConsolidatedPurchaseOrderCandidatesQueryResponse
    {
        public List<PurchaseOrder> PurchaseOrders { get; set; } = [];
    }
}
