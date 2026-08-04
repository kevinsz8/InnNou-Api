namespace InnNou.Application.Responses.Common
{
    public class SupplierPriceChangeSubscription
    {
        public Guid SupplierPriceChangeSubscriptionToken { get; set; }
        public Guid SupplierToken { get; set; }
        public string SupplierName { get; set; } = default!;
        public DateTime CreatedUtc { get; set; }
    }
}
