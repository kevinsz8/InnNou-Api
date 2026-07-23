using InnNou.Application.Responses.Common;

namespace InnNou.Application.Responses
{
    public class GetPurchaseOrderRectificationsQueryResponse
    {
        public List<PurchaseOrderRectification> Rectifications { get; set; } = [];
    }
}
