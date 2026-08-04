using InnNou.Application.Responses.Common;

namespace InnNou.Application.Responses
{
    public class GetMySupplierPriceChangeSubscriptionsQueryResponse
    {
        public List<SupplierPriceChangeSubscription> Subscriptions { get; set; } = [];
    }
}
