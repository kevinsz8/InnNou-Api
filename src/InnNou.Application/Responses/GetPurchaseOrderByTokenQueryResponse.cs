using InnNou.Application.Responses.Common;

namespace InnNou.Application.Responses
{
    public class GetPurchaseOrderByTokenQueryResponse
    {
        public PurchaseOrder PurchaseOrder { get; set; } = default!;
    }
}
