using InnNou.Application.Responses.Common;

namespace InnNou.Application.Responses
{
    public class SetSupplierPriceChangeSubscriptionsCommandResponse
    {
        public List<SupplierPriceChangeSubscription> Subscriptions { get; set; } = [];
    }
}
