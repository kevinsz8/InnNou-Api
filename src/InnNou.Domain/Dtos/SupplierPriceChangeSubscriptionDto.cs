namespace InnNou.Domain.Dtos
{
    public class SupplierPriceChangeSubscriptionDto
    {
        public int SupplierPriceChangeSubscriptionId { get; set; }
        public Guid SupplierPriceChangeSubscriptionToken { get; set; }
        public int SupplierId { get; set; }
        public Guid SupplierToken { get; set; }
        public string SupplierName { get; set; } = default!;
        public DateTime CreatedUtc { get; set; }
    }
}
